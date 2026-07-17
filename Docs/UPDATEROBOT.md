# Update pm_robot

When new tools, chucks, tips or jaws are added to the robot in ROS, the Unity
simulation is updated with a **delta import** — the existing scene robot (with
all its tuned ArticulationBodies, attached scripts and sensors) is never
deleted or re-imported. 
The runtime configuration (`configureRobot.cs`) reads which GameObjects belong to which
YAML selection from a generated manifest.

## How it works

`generate_unity_urdf.py` (in `pm_robot_description/urdf/` of the
match_pm_robot repo) iterates every tool/chuck/tip/jaw permutation of
`pm_robot_bringup_config.yaml`, runs xacro for each, and combines the results
into one URDF containing every possible part. Alongside it writes
`pm_robot_unity_manifest.json`, which maps every YAML selection to the URDF
link names it owns (Unity GameObject name == URDF link name).

- Tool links (vacuum tools and grippers) are suffixed with
  `_{tool}_{tip}` / `_{tool}_{jaw}` so every combination is a disjoint,
  uniquely named subtree (e.g.
  `Tool_Parallel_Gripper_Body_2_Jaws_Schunk_MPG_20_plus_Jaw_ASG_Gripper`).
- `configureRobot.cs` loads the manifest from the pm_robot_description
  install at Play (and on every `configure_robot` service call), resolves the
  links by name below the `pm_robot` root, and activates exactly the selected
  items. Only measuring systems, the gonio/smarpod assemblies and smarpod
  chucks are still wired in the Inspector.
- The editor tool **Config Tools → Delta Import Robot Parts** compares the
  manifest against the scene and imports only the missing links (meshes are
  copied automatically from the ROS install). New parts follow the scene
  conventions: MeshColliders disabled, ArticulationBodies disabled, item roots
  deactivated.

## Adding a new tool / chuck / tip / jaw

1. **ROS**: add the part to `pm_robot_bringup_config.yaml` (its
   `availabe_...` list), the xacro, and the STL under
   `pm_robot_description/meshes/...`. Then `colcon build`.
2. **Generate**: run the generator (it reads/writes the install space):

   ```bash
   python3 ~/ros2_ws/src/match_pm_robot/pm_robot_description/urdf/generate_unity_urdf.py
   ```

   Optional flags: `--yaml`, `--xacro`, `--output-dir`, `--output-name`, and
   `--unity-project <path>` to also copy the URDF/manifest/missing meshes into
   `Assets/PM_Robot/` (for reference only — runtime and editor tool read the
   ROS install directly).
3. **Unity**: menu `Config Tools → Delta Import Robot Parts`. Check the
   **Robot root** field points at the robot you actually use (see below) →
   **Scan (dry run)** → review the listed items, each showing the scene path
   its parts will be attached to → **Import missing parts**. The console then
   reports the full path of every created object.
4. **Add scripts** If the added components should have scipts, the must be attached manually.
   For example, every tip of the gripper has to have a force_sensor script attached so the force sensor can be used.
5. **Run**: select the part in `pm_robot_bringup_config.yaml`, then press Play
   or call the reconfigure service while playing:

   ```bash
   ros2 service call /configure_robot_node/configure_robot pm_msgs/srv/EmptyWithSuccess
   ```

If a driven part was added (e.g. a new gripper), attach its controller script
(e.g. `SmarActGripperServiceController`) to the imported tool body, set
`movingJawObjectName` to the canonical jaw link
(`Tool_Parallel_Gripper_Jaw_{tool}_{jaw}`, see the manifest), and enable that
jaw's ArticulationBody.

## Only one robot in the scene

Both the delta importer and `configureRobot` resolve every part by object name
below a single robot root. If the scene contains more than one robot (e.g. a
leftover `pm_robot (1)` from an earlier full URDF import), parts land in one of
them and the other silently keeps the old state. The importer warns and makes
you assign the root explicitly; `configureRobot` warns at Play. **Delete the
leftover robot** (or assign the intended root in both places) — do not keep two.



