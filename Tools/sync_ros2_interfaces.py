#!/usr/bin/env python3
"""Sync the custom ROS2 interface packages of the ROS2 workspace into Unity.

Every package below ``ros2_ws_src`` that is a member of ``rosidl_interface_packages`` is
mirrored into the ros2-for-unity interface repo, built with ros2-for-unity, and the
resulting plugins are copied to Assets/Ros2ForUnity/Plugins.

  python3 Tools/sync_ros2_interfaces.py check [--json]
  python3 Tools/sync_ros2_interfaces.py sync [--prune]

Paths and excluded packages live in Tools/ros2_interfaces.json; the paths can be
overridden with PM_ROS2_WS_SRC and PM_ROS2_FOR_UNITY. ``check`` exits with 1 when packages
changed or were added since the last sync (recorded in Tools/ros2_interfaces.lock.json).
Unity runs both commands from the "ROS2" menu (Assets/Scripts/Editor/Ros2InterfaceSync.cs).
"""

import argparse
import hashlib
import json
import os
import re
import shutil
import signal
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from xml.etree import ElementTree

PROJECT = Path(__file__).resolve().parent.parent
CONFIG_FILE = PROJECT / "Tools" / "ros2_interfaces.json"
LOCK_FILE = PROJECT / "Tools" / "ros2_interfaces.lock.json"
LOG_FILE = PROJECT / "Logs" / "ros2_interface_sync.log"
ASSET_DIR = PROJECT / "Assets" / "Ros2ForUnity"

HASHED_DIRS = ("msg", "srv", "action")
TOOL_PREFIXES = ("ament_", "rosidl_")
CLEAN_PATH = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
OUTDATED = ("changed", "new")
SYNCED = ("up_to_date",) + OUTDATED


class SyncError(Exception):
    pass


@dataclass
class Config:
    ws_src: Path
    ros2_for_unity: Path
    mirror: Path
    exclude: set


@dataclass
class Package:
    name: str
    path: Path
    depends: set
    is_interface: bool
    status: str = ""
    detail: str = ""
    hash: str = ""
    source: str = field(default="", repr=False)


def warn(text):
    print(f"WARNING: {text}", file=sys.stderr)


def load_config():
    try:
        config = json.loads(CONFIG_FILE.read_text())
        ws_src = os.environ.get("PM_ROS2_WS_SRC", config["ros2_ws_src"])
        ros2_for_unity_value = os.environ.get("PM_ROS2_FOR_UNITY", config["ros2_for_unity"])
        interface_repo = config["interface_repo"]
    except FileNotFoundError:
        raise SyncError(f"configuration file not found: {CONFIG_FILE}")
    except json.JSONDecodeError as error:
        raise SyncError(f"invalid JSON in {CONFIG_FILE}: line {error.lineno}, column {error.colno}: {error.msg}")
    except KeyError as error:
        raise SyncError(f"missing required key {error} in {CONFIG_FILE}")

    ros2_for_unity = Path(os.path.expanduser(ros2_for_unity_value))
    return Config(
        ws_src=Path(os.path.expanduser(ws_src)),
        ros2_for_unity=ros2_for_unity,
        mirror=ros2_for_unity / interface_repo,
        exclude=set(config.get("exclude", [])),
    )


def configured_by(environment_name, json_key):
    if environment_name in os.environ:
        return f"environment variable {environment_name}"
    return f"'{json_key}' in {CONFIG_FILE.relative_to(PROJECT)}"


def validate_config(config):
    """Fail early when a configured root or required subdirectory is wrong.

    Without this guard a missing ros2-for-unity tree looks like an empty set of
    bundled interfaces. Custom packages then appear ``skipped`` because normal
    dependencies such as geometry_msgs seem unavailable, hiding the path error.
    """
    problems = []
    if not config.ws_src.is_dir():
        problems.append(
            f"ROS2 workspace source directory does not exist: {config.ws_src}\n"
            f"  configured by {configured_by('PM_ROS2_WS_SRC', 'ros2_ws_src')}")

    if not config.ros2_for_unity.is_dir():
        detail = (
            f"ros2-for-unity directory does not exist: {config.ros2_for_unity}\n"
            f"  configured by {configured_by('PM_ROS2_FOR_UNITY', 'ros2_for_unity')}")
        likely = Path.home() / "ros2-for-unity"
        if likely.is_dir() and likely.resolve() != config.ros2_for_unity.resolve():
            detail += f"\n  possible installation found at: {likely}"
        problems.append(detail)
    else:
        expected = (
            (config.ros2_for_unity / "src" / "ros2cs" / "build.sh", True,
             "ros2cs build script"),
            (config.ros2_for_unity / "src" / "ros2cs" / "src", False,
             "bundled ROS2 interface sources"),
            (config.mirror, False, "custom interface mirror"),
        )
        for path, is_file, description in expected:
            exists = path.is_file() if is_file else path.is_dir()
            if not exists:
                problems.append(f"{description} not found: {path}")

    if problems:
        raise SyncError(
            "Interface sync path validation failed:\n- " + "\n- ".join(problems) +
            "\nUpdate Tools/ros2_interfaces.json or set PM_ROS2_WS_SRC / PM_ROS2_FOR_UNITY "
            "before starting Unity.")


def ros_distro():
    metadata = ASSET_DIR / "metadata_ros2_for_unity.xml"
    try:
        return ElementTree.parse(metadata).getroot().findtext("ros2").strip()
    except (OSError, ElementTree.ParseError, AttributeError):
        raise SyncError(f"cannot read the ROS2 distro from {metadata}")


def read_package(package_xml):
    root = ElementTree.parse(package_xml).getroot()
    depends = {e.text.strip() for tag in ("depend", "build_depend", "exec_depend")
               for e in root.findall(tag) if e.text}
    groups = {e.text.strip() for e in root.findall("member_of_group") if e.text}
    return Package((root.findtext("name") or "").strip(), package_xml.parent, depends,
                   "rosidl_interface_packages" in groups)


def find_packages(root):
    """Discover packages like colcon does: COLCON_IGNORE prunes a subtree, packages do not nest."""
    packages, seen = [], set()
    for dirpath, dirnames, filenames in os.walk(root, followlinks=True):
        real = os.path.realpath(dirpath)
        if real in seen or "COLCON_IGNORE" in filenames:
            dirnames[:] = []
            continue
        seen.add(real)
        if "package.xml" in filenames:
            dirnames[:] = []
            try:
                packages.append(read_package(Path(dirpath) / "package.xml"))
            except (OSError, ElementTree.ParseError) as error:
                warn(f"ignoring {dirpath}/package.xml: {error}")
            continue
        dirnames[:] = sorted(d for d in dirnames if not d.startswith("."))
    return packages


def interface_hash(package_dir):
    """Hash of everything that defines the generated code of an interface package."""
    files = [package_dir / "package.xml", package_dir / "CMakeLists.txt"]
    for sub in HASHED_DIRS:
        if (package_dir / sub).is_dir():
            files += (package_dir / sub).rglob("*")
    digest = hashlib.sha256()
    for path in sorted((p for p in files if p.is_file()), key=lambda p: p.relative_to(package_dir).as_posix()):
        digest.update(path.relative_to(package_dir).as_posix().encode() + b"\0")
        digest.update(path.read_bytes() + b"\0")
    return digest.hexdigest()


def load_lock():
    try:
        return json.loads(LOCK_FILE.read_text()).get("packages", {})
    except FileNotFoundError:
        return {}


def survey(config):
    """Classify the interface packages of the workspace and of the mirror against the lock file."""
    distro = ros_distro()
    lock = load_lock()

    found = {}
    for package in find_packages(config.ws_src):
        if not package.is_interface:
            continue
        if package.name in found:
            warn(f"{package.name} exists twice, using {found[package.name].path}")
            continue
        found[package.name] = package
    candidates = {name: p for name, p in found.items() if name not in config.exclude}
    mirrored = {p.name: p for p in find_packages(config.mirror)} if config.mirror.is_dir() else {}
    bundled_root = config.ros2_for_unity / "src" / "ros2cs" / "src"
    bundled = {p.name for p in find_packages(bundled_root) if p.is_interface} if bundled_root.is_dir() else set()

    # A package whose message dependencies ros2-for-unity cannot generate C# for would break the
    # whole build, so it is skipped, and so is everything that depends on it.
    available = bundled | set(candidates) | set(mirrored)
    system_cache = {}

    def missing_dependency(package):
        for dep in sorted(package.depends):
            if dep in available or dep.startswith(TOOL_PREFIXES):
                continue
            if dep not in system_cache:
                path = Path("/opt/ros") / distro / "share" / dep / "package.xml"
                system_cache[dep] = read_package(path) if path.is_file() else None
            system = system_cache[dep]
            if system is not None and not system.is_interface:
                continue
            return dep
        return None

    skipped, grew = {}, True
    while grew:
        grew = False
        for name, package in candidates.items():
            dep = None if name in skipped else missing_dependency(package)
            if dep:
                skipped[name] = dep
                if name not in mirrored:
                    available.discard(name)
                grew = True

    packages = []
    for name, package in sorted(candidates.items()):
        package.source = package.path.relative_to(config.ws_src).as_posix()
        if name in skipped:
            package.status = "skipped"
            package.detail = f"dependency '{skipped[name]}' is not available in ros2-for-unity"
        else:
            package.hash = interface_hash(package.path)
            entry = lock.get(name)
            package.status = "new" if entry is None else "up_to_date" if entry["hash"] == package.hash else "changed"
            package.detail = package.source
        packages.append(package)
    for name, package in sorted(mirrored.items()):
        if name in candidates:
            continue
        package.status = "orphaned"
        reason = "excluded in Tools/ros2_interfaces.json" if name in config.exclude else "not in the ROS2 workspace"
        package.detail = f"{reason}; kept in the mirror, remove with 'sync --prune'"
        packages.append(package)
    return packages


def check(config, as_json):
    packages = survey(config)
    outdated = [p for p in packages if p.status in OUTDATED]
    if as_json:
        print(json.dumps({
            "available": True,
            "message": "",
            "packages": [{"name": p.name, "status": p.status, "detail": p.detail} for p in packages],
        }))
    else:
        print(f"ROS2 interfaces in {config.ws_src} vs. {ASSET_DIR.relative_to(PROJECT)}:")
        for package in packages:
            print(f"  {package.status:<11} {package.name:<32} {package.detail}")
        if outdated:
            print(f"{len(outdated)} package(s) out of date. Run 'python3 {Path(__file__).resolve().relative_to(PROJECT)} sync' "
                  "or Unity: ROS2/Sync Interfaces.")
        else:
            print("Unity is up to date.")
    return 1 if outdated else 0


# --- sync -------------------------------------------------------------------------------------

current_child = None


def abort(signum, _frame):
    if current_child is not None and current_child.poll() is None:
        os.killpg(current_child.pid, signal.SIGTERM)
    raise SyncError(f"aborted by signal {signum}")


def run_streamed(command, cwd, env, say):
    global current_child
    # Own session, so an abort can stop colcon together with every compiler it started.
    with subprocess.Popen(command, cwd=cwd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                          text=True, errors="replace", start_new_session=True) as child:
        current_child = child
        for line in child.stdout:
            say(line.rstrip("\n"))
        returncode = child.wait()
    current_child = None
    return returncode


def rsync(args, log):
    result = subprocess.run(["rsync", "-a", "--itemize-changes", *args], capture_output=True, text=True)
    log.write(result.stdout + result.stderr)
    if result.returncode != 0:
        raise SyncError(f"rsync {' '.join(args)} failed: {result.stderr.strip()}")
    return [("removed" if line.startswith("*deleting") else "updated", line.split(" ", 1)[1].strip())
            for line in result.stdout.splitlines() if line.startswith((">f", "*deleting")) and " " in line]


def with_dependents(names, packages):
    result, grew = set(names), True
    while grew:
        grew = False
        for package in packages:
            if package.name not in result and package.depends & result:
                result.add(package.name)
                grew = True
    return result


def clean_build(workspace, name):
    """Forget what colcon built and installed for a package, so removed messages disappear and no
    CMake cache keeps pointing at a dependency from another workspace (e.g. ~/ros2_ws)."""
    build_dir = workspace / "build" / name
    install_dir = (workspace / "install").resolve()
    manifest = build_dir / "install_manifest.txt"
    if manifest.is_file():
        # Includes copies of dependency assemblies; colcon reinstalls every package on each build.
        for line in manifest.read_text().splitlines():
            path = Path(line.strip())
            if line.strip() and path.is_relative_to(install_dir) and (path.is_file() or path.is_symlink()):
                path.unlink()
    shutil.rmtree(build_dir, ignore_errors=True)


def build(workspace, distro, say):
    # Only the ROS distro is sourced: an underlay like ~/ros2_ws would silently satisfy
    # dependencies that ros2-for-unity then never deploys.
    env = {key: os.environ[key] for key in ("HOME", "USER", "LOGNAME", "LANG", "TERM") if key in os.environ}
    env["PATH"] = CLEAN_PATH
    command = ["bash", "-c", f"source /opt/ros/{distro}/setup.bash && ./src/ros2cs/build.sh"]
    for attempt in (1, 2):
        say(f"--- build attempt {attempt}: {workspace}/src/ros2cs/build.sh "
            f"(only /opt/ros/{distro} sourced)")
        if run_streamed(command, workspace, env, say) == 0:
            break
    else:
        raise SyncError("ros2-for-unity build failed twice (see log); Assets/Ros2ForUnity was not touched")

    # The upstream top-level build.sh packages a complete Ros2ForUnity asset
    # from workspace/src/Ros2ForUnity. This project already owns that asset,
    # and some installations deliberately omit the duplicate source tree.
    # Stage only the freshly built plugins consumed by deploy().
    asset = workspace / "install" / "asset" / "Ros2ForUnity"
    plugins = asset / "Plugins"
    say(f"--- staging Unity plugins in {plugins}")
    dotnet_output = workspace / "install" / "lib" / "dotnet"
    native_output = workspace / "install" / "lib"
    missing = [path for path in (dotnet_output, native_output) if not path.is_dir()]
    if missing:
        raise SyncError(
            "ros2cs build finished without the expected plugin output: " +
            ", ".join(str(path) for path in missing) +
            "; Assets/Ros2ForUnity was not touched")

    plugins.mkdir(parents=True, exist_ok=True)
    native_plugins = plugins / "Linux" / "x86_64"
    native_plugins.mkdir(parents=True, exist_ok=True)
    managed_count = 0
    native_count = 0
    for source in dotnet_output.iterdir():
        if source.is_file() and source.suffix != ".pdb":
            shutil.copy2(source, plugins / source.name)
            managed_count += 1
    for source in native_output.iterdir():
        if source.is_file() and not source.name.endswith("_python.so"):
            shutil.copy2(source, native_plugins / source.name)
            native_count += 1
    if not managed_count or not native_count:
        raise SyncError(
            f"ros2cs plugin output is incomplete ({managed_count} managed, {native_count} native files); "
            "Assets/Ros2ForUnity was not touched")
    say(f"--- staged {managed_count} managed and {native_count} native plugin file(s)")

    # ros2cs itself has not changed during an interface sync, so retain the
    # checked-in metadata while replacing the generated assemblies/typesupport.
    for name in ("metadata_ros2cs.xml", "metadata_ros2_for_unity.xml"):
        source = ASSET_DIR / name
        if not source.is_file():
            raise SyncError(f"Unity metadata missing: {source}")
        shutil.copy2(source, asset / name)
    ros2cs_metadata = ASSET_DIR / "metadata_ros2cs.xml"
    # The editor resolves plugins from Plugins/<OS>/x86_64, while player
    # builds consume the copy at Plugins. Keep both layouts valid.
    shutil.copy2(ros2cs_metadata, plugins / "metadata_ros2cs.xml")
    shutil.copy2(ros2cs_metadata, native_plugins / "metadata_ros2cs.xml")


def deploy(workspace, log):
    asset = workspace / "install" / "asset" / "Ros2ForUnity"
    plugins = ASSET_DIR / "Plugins"
    # rsync replaces files by renaming a temporary copy, so a running editor that has the old
    # .so files mapped keeps working until it is restarted.
    changes = rsync(["--delete", "--exclude", "*.meta", f"{asset}/Plugins/", f"{plugins}/"], log)
    for name in ("metadata_ros2cs.xml", "metadata_ros2_for_unity.xml"):
        shutil.copy2(asset / name, ASSET_DIR / name)
    for meta in plugins.rglob("*.meta"):
        if not meta.with_suffix("").exists():
            meta.unlink()
    return changes


def git_state(path):
    try:
        def git(*args):
            return subprocess.run(["git", "-C", str(path), *args], capture_output=True, text=True,
                                  check=True).stdout.strip()
        return {"commit": git("rev-parse", "--short", "HEAD"), "dirty": bool(git("status", "--porcelain", "--", "."))}
    except (OSError, subprocess.CalledProcessError):
        return {}


def write_lock(packages):
    lock = {"packages": {p.name: {"hash": p.hash, "source": p.source, **git_state(p.path)} for p in packages}}
    LOCK_FILE.write_text(json.dumps(lock, indent=2, sort_keys=True) + "\n")


def csharp_users(names):
    pattern = re.compile(r"\b(" + "|".join(map(re.escape, names)) + r")\.(msg|srv|action)\b")
    users = {}
    for path in (PROJECT / "Assets").rglob("*.cs"):
        for match in pattern.finditer(path.read_text(errors="ignore")):
            users.setdefault(match.group(1), set()).add(path.relative_to(PROJECT).as_posix())
    return users


def sync(config, prune):
    workspace = config.ros2_for_unity

    distro = ros_distro()
    packages = survey(config)
    synced = [p for p in packages if p.status in SYNCED]
    orphaned = [p for p in packages if p.status == "orphaned"]
    pruned = []
    if prune and orphaned:
        users = csharp_users([p.name for p in orphaned])
        if users:
            raise SyncError("cannot prune, Unity code still uses: " + "; ".join(
                f"{name} ({', '.join(sorted(files))})" for name, files in sorted(users.items())))
        pruned = orphaned

    for sig in (signal.SIGINT, signal.SIGTERM):
        signal.signal(sig, abort)
    LOG_FILE.parent.mkdir(exist_ok=True)
    with open(LOG_FILE, "w") as log:
        def say(text):
            print(text, flush=True)
            log.write(text + "\n")
            log.flush()

        say(f"Syncing {len(synced)} interface package(s) from {config.ws_src} into {config.mirror}")
        dirty = set()
        for package in synced:
            target = config.mirror / package.name
            if package.status in OUTDATED or not target.is_dir() or interface_hash(target) != package.hash:
                dirty.add(package.name)
            for kind, path in rsync(["--delete", "--exclude", ".git", f"{package.path}/", f"{target}/"], log):
                say(f"  {kind}: {package.name}/{path}")
        for package in pruned:
            say(f"  removing {package.name} from the mirror")
            shutil.rmtree(package.path)
            dirty.add(package.name)

        dirty = with_dependents(dirty, find_packages(config.mirror))
        say(f"Rebuilding from scratch: {', '.join(sorted(dirty)) or '-'}")
        for name in sorted(dirty):
            clean_build(workspace, name)
        shutil.rmtree(workspace / "install" / "asset", ignore_errors=True)

        build(workspace, distro, say)
        changes = deploy(workspace, log)
        write_lock(synced)

        updated = sum(1 for kind, _ in changes if kind == "updated")
        say("")
        say(f"Done: {updated} plugin file(s) updated, {len(changes) - updated} removed in "
            f"{(ASSET_DIR / 'Plugins').relative_to(PROJECT)}.")
        for package in packages:
            if package.status in ("skipped", "orphaned") and package not in pruned:
                say(f"  {package.status}: {package.name} ({package.detail})")
        say(f"Commit {config.mirror} and this project. Restart the Unity editor before entering play mode.")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    commands = parser.add_subparsers(dest="command", required=True)
    check_parser = commands.add_parser("check", help="report interface packages that differ from the last sync")
    check_parser.add_argument("--json", action="store_true", help="machine readable output for the Unity editor")
    sync_parser = commands.add_parser("sync", help="mirror, build and deploy the interface packages")
    sync_parser.add_argument("--prune", action="store_true",
                             help="remove mirrored packages that are no longer in the ROS2 workspace")
    args = parser.parse_args()

    try:
        config = load_config()
        validate_config(config)
        if args.command == "check":
            return check(config, args.json)
        return sync(config, args.prune)
    except SyncError as error:
        if args.command == "check" and args.json:
            print(json.dumps({"available": False, "message": str(error), "packages": []}))
        print(f"ERROR: {error}", file=sys.stderr)
        return 2 if args.command == "check" else 1


if __name__ == "__main__":
    sys.exit(main())
