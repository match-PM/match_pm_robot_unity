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
    config = json.loads(CONFIG_FILE.read_text())
    ws_src = os.environ.get("PM_ROS2_WS_SRC", config["ros2_ws_src"])
    ros2_for_unity = Path(os.path.expanduser(os.environ.get("PM_ROS2_FOR_UNITY", config["ros2_for_unity"])))
    return Config(
        ws_src=Path(os.path.expanduser(ws_src)),
        ros2_for_unity=ros2_for_unity,
        mirror=ros2_for_unity / config["interface_repo"],
        exclude=set(config.get("exclude", [])),
    )


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
    if not config.ws_src.is_dir():
        message = f"ROS2 workspace not found at {config.ws_src}, nothing to check"
        print(json.dumps({"available": False, "message": message, "packages": []}) if as_json else message)
        return 0

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
    command = ["bash", "-c", f"source /opt/ros/{distro}/setup.bash && ./build.sh"]
    for attempt in (1, 2):
        say(f"--- build attempt {attempt}: {workspace}/build.sh (only /opt/ros/{distro} sourced)")
        if run_streamed(command, workspace, env, say) == 0:
            return
    raise SyncError("ros2-for-unity build failed twice (see log); Assets/Ros2ForUnity was not touched")


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
    if not (workspace / "build.sh").is_file():
        raise SyncError(f"ros2-for-unity not found at {workspace} (set PM_ROS2_FOR_UNITY or Tools/ros2_interfaces.json)")
    if not config.mirror.is_dir():
        raise SyncError(f"interface repo {config.mirror} missing, run ./pull_repositories.sh in {workspace} first")
    if not config.ws_src.is_dir():
        raise SyncError(f"ROS2 workspace not found at {config.ws_src} (set PM_ROS2_WS_SRC or Tools/ros2_interfaces.json)")

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
