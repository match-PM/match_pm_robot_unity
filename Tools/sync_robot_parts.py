#!/usr/bin/env python3
"""Sync the robot parts of the ROS2 workspace to Unity's inputs and back into its source tree.

Builds the packages the Unity URDF is generated from, runs generate_unity_urdf.py (which writes
the combined URDF and manifest to the install space) and copies changed results next to the
generator, so that the next colcon build or clean rebuild installs them again.

  python3 Tools/sync_robot_parts.py

The workspace comes from ros2_ws_src in Tools/ros2_interfaces.json (or PM_ROS2_WS_SRC). Unity
runs this from Config Tools/Delta Import Robot Parts and imports the new parts afterwards.
"""

import fcntl
import filecmp
import json
import os
import re
import shutil
import signal
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True

from sync_ros2_interfaces import (CLEAN_PATH, CONFIG_FILE, PROJECT, SyncError, abort, find_packages,
                                  ros_distro, run_streamed)

LOG_FILE = PROJECT / "Logs" / "robot_parts_sync.log"
LOCK_FILE = PROJECT / "Logs" / "robot_parts_sync.lock"
GENERATOR_PACKAGE = "pm_robot_description"
GENERATOR = Path("urdf") / "generate_unity_urdf.py"
URDF_NAME = "pm_robot_unity.urdf"
MANIFEST_NAME = "pm_robot_unity_manifest.json"
XACRO_FIND = re.compile(r"\$\(find ([A-Za-z0-9_]+)\)")


def workspace_source():
    try:
        configured = json.loads(CONFIG_FILE.read_text())["ros2_ws_src"]
    except (OSError, ValueError, KeyError) as error:
        raise SyncError(f"cannot read ros2_ws_src from {CONFIG_FILE}: {error}")
    return Path(os.path.expanduser(os.environ.get("PM_ROS2_WS_SRC", configured)))


def required_packages(source):
    """The generator's package plus every workspace package its xacro files reach with $(find ...)."""
    by_name = {package.name: package for package in find_packages(source)}
    if GENERATOR_PACKAGE not in by_name:
        raise SyncError(f"package {GENERATOR_PACKAGE} not found below {source}")
    needed, queue = {}, [GENERATOR_PACKAGE]
    while queue:
        name = queue.pop()
        if name in needed or name not in by_name:
            continue
        needed[name] = by_name[name]
        for xacro in needed[name].path.rglob("*.xacro"):
            queue.extend(XACRO_FIND.findall(xacro.read_text(errors="replace")))
    return list(needed.values())


def share_dir(workspace, name):
    return workspace / "install" / name / "share" / name


def stash_runtime_files(packages, workspace, stash, say):
    """Back up installed files that are newer than their source: ROS saves calibration and
    configuration into the install space at runtime, and a build would put the old values back."""
    kept = []
    for package in packages:
        share = share_dir(workspace, package.name)
        for source in package.path.rglob("*"):
            relative = source.relative_to(package.path)
            if not source.is_file() or "__pycache__" in relative.parts:
                continue
            if source.name in (URDF_NAME, MANIFEST_NAME):
                continue
            installed = share / relative
            if (installed.is_file() and installed.stat().st_mtime > source.stat().st_mtime
                    and not filecmp.cmp(source, installed, shallow=False)):
                backup = Path(stash) / str(len(kept))
                shutil.copy2(installed, backup)
                kept.append((installed, backup))
                say(f"--- keeping {installed}: newer than {source}")
    return kept


def ros_command(distro, workspace, command):
    overlay = workspace / "install" / "setup.bash"
    return ["bash", "-c", f"source /opt/ros/{distro}/setup.bash; "
                          f"if [ -f '{overlay}' ]; then source '{overlay}'; fi; {command}"]


def build(packages, workspace, distro, env, say):
    names = " ".join(sorted(package.name for package in packages))
    say(f"--- building {names}")
    command = ros_command(distro, workspace, f"colcon build --packages-select {names}")
    if run_streamed(command, workspace, env, say) != 0:
        raise SyncError("colcon build failed (see log)")


def generate(generator_package, workspace, distro, env, say):
    share = share_dir(workspace, GENERATOR_PACKAGE)
    bringup = share_dir(workspace, "pm_robot_bringup") / "config" / "pm_robot_bringup_config.yaml"
    command = (f"python3 '{generator_package.path / GENERATOR}' --yaml '{bringup}' "
               f"--xacro '{share / 'urdf' / 'pm_robot_main.xacro'}' --output-dir '{share / 'urdf'}'")
    say("--- generating the Unity URDF and manifest")

    # The generator rewrites the installed bringup config for every permutation and restores it at
    # the end. Killing it halfway would leave a wrong configuration, so a cancel waits for it.
    cancelled = []
    previous = {sig: signal.signal(sig, lambda signum, _frame: cancelled.append(signum))
                for sig in (signal.SIGINT, signal.SIGTERM)}
    returncode = run_streamed(ros_command(distro, workspace, command), workspace, env, say)
    for sig, handler in previous.items():
        signal.signal(sig, handler)
    if cancelled:
        raise SyncError(f"aborted by signal {cancelled[0]}")
    if returncode != 0:
        raise SyncError("generate_unity_urdf.py failed (see log)")


def manifest_content(path):
    if not path.is_file():
        return None
    content = json.loads(path.read_text())
    content.pop("generatedAt", None)
    return content


def publish(generator_package, workspace, say):
    """Copy the results into the source tree, ignoring a manifest that only has a new timestamp."""
    output = share_dir(workspace, GENERATOR_PACKAGE) / "urdf"
    target = generator_package.path / "urdf"
    changed = []
    if not (target / URDF_NAME).is_file() or not filecmp.cmp(output / URDF_NAME, target / URDF_NAME, shallow=False):
        changed.append(URDF_NAME)
    if manifest_content(output / MANIFEST_NAME) != manifest_content(target / MANIFEST_NAME):
        changed.append(MANIFEST_NAME)
    for name in changed:
        shutil.copyfile(output / name, target / name)
        say(f"--- updated {target / name}")
    return changed


def sync():
    source = workspace_source()
    workspace = source.parent
    distro = ros_distro()
    packages = required_packages(source)
    generator_package = next(package for package in packages if package.name == GENERATOR_PACKAGE)
    env = {key: os.environ[key] for key in ("HOME", "USER", "LOGNAME", "LANG", "TERM") if key in os.environ}
    env["PATH"] = CLEAN_PATH

    for sig in (signal.SIGINT, signal.SIGTERM):
        signal.signal(sig, abort)
    LOG_FILE.parent.mkdir(exist_ok=True)
    with open(LOCK_FILE, "w") as lock, open(LOG_FILE, "w") as log:
        def say(text):
            print(text, flush=True)
            log.write(text + "\n")
            log.flush()

        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError:
            raise SyncError("another robot parts sync is still running")

        with tempfile.TemporaryDirectory() as stash:
            kept = stash_runtime_files(packages, workspace, stash, say)
            try:
                build(packages, workspace, distro, env, say)
            finally:
                for installed, backup in kept:
                    shutil.copy2(backup, installed)

        generate(generator_package, workspace, distro, env, say)
        changed = publish(generator_package, workspace, say)

        say("")
        say(f"Done: {', '.join(changed) + ' changed, commit in the ROS2 repo' if changed else 'source tree unchanged'}.")
        for installed, _ in kept:
            say(f"  kept runtime file: {installed}")
    return 0


def main():
    try:
        return sync()
    except SyncError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
