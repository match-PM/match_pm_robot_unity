using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ROS2;
using UtilityFunctions; // GenericFunctions.YamlLoader

using Float64MultiArray = std_msgs.msg.Float64MultiArray;

// =============================================================================
// Drives the 2-jaw parallel gripper from ROS.
//
// pm_parallel_gripper_2_jaws_controller is a JointGroupPositionController on
// mock hardware (PMSystemGripper in ros2_control_real_HW.xacro), so its command
// topic is the integration point for Unity - exactly as for the UV cart, see
// UvCartController. Publisher is pm_robot_primitive_skills, which sends
// [jaw_1, jaw_2] in meters.
//
// The jaw GameObjects carry the active tool/jaw suffix
// (Tool_Parallel_Gripper_Jaw_1_<use_tool>_<use_jaw_type>), so they are not
// hardcoded here: the suffix comes from the selected item of the
// pm_robot_tools/pm_robot_tool_parallel_gripper_2_jaws section of
// pm_robot_unity_manifest.json, matched against pm_robot_bringup_config.yaml.
// A new gripper tool therefore needs no Unity code change.
//
// Both jaw joints are limited 0 -> 0.002 with opposite axes in the URDF, so the
// same command value on both drives closes the gripper symmetrically and no
// sign flip is needed here.
// =============================================================================

public class Gripper2JawsController : MonoBehaviour
{
    private const string ManifestSectionPath = "pm_robot_tools/pm_robot_tool_parallel_gripper_2_jaws";
    private const string Jaw1LinkPrefix = "Tool_Parallel_Gripper_Jaw_1_";
    private const string Jaw2LinkPrefix = "Tool_Parallel_Gripper_Jaw_2_";

    [Header("Robot")]
    [Tooltip("Root of the robot hierarchy; the jaw links are resolved by name below this object. Defaults to the GameObject named 'pm_robot'.")]
    public GameObject robotRoot;

    [Header("ROS")]
    public string commandTopic = "/pm_parallel_gripper_2_jaws_controller/commands";
    public string nodeName = "ROS2UnityGripper2JawsNode";

    [Header("Jaw Drive")]
    [Tooltip("Applied to both jaw drives when they are resolved. The URDF import leaves " +
             "stiffness/damping at 0, which cannot hold a target position, so the drives " +
             "are configured here rather than by hand in the Inspector (a re-import would " +
             "discard that). Mirrors SmarActGripperServiceController.")]
    public float driveStiffness = 20000f;
    public float driveDamping = 200f;
    public float driveForceLimit = 10000f;

    [Header("Resolved jaws (read-only)")]
    [Tooltip("Filled from the manifest at startup; shown for diagnostics.")]
    public ArticulationBody jaw1;
    public ArticulationBody jaw2;

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<Float64MultiArray> commandSubscription;

    private readonly object targetLock = new object();
    private double[] pendingTarget;

    // Resolving reads the YAML and manifest through 'ros2 pkg prefix', which
    // spawns a shell - throttle the retries when the gripper is not (yet) there.
    private const float ResolveRetryInterval = 2f;
    private float nextResolveTime;
    private bool resolved;
    private bool loggedUnresolved;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity == null)
            ros2Unity = FindObjectOfType<ROS2UnityComponent>();

        if (robotRoot == null)
            robotRoot = GameObject.Find("pm_robot");

        TryResolveJaws();
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity != null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode(nodeName);
            commandSubscription = ros2Node.CreateSubscription<Float64MultiArray>(
                commandTopic, CommandCallback);
            Debug.Log($"Gripper2JawsController: subscribed to '{commandTopic}'.");
        }

        double[] target;
        lock (targetLock)
        {
            target = pendingTarget;
            pendingTarget = null;
        }

        if (target == null)
            return;

        // A command means the gripper is expected to exist; if it was not
        // resolved at startup (e.g. reconfigured since), try again.
        if (!resolved && Time.time >= nextResolveTime)
            TryResolveJaws();

        if (!resolved)
        {
            if (!loggedUnresolved)
            {
                Debug.LogWarning($"Gripper2JawsController: received a command on '{commandTopic}' " +
                                 "but the gripper jaws are not resolved; the gripper will not move.");
                loggedUnresolved = true;
            }
            return;
        }

        loggedUnresolved = false;
        SetDriveTarget(jaw1, (float)target[0]);
        SetDriveTarget(jaw2, (float)target[1]);
    }

    private static void SetDriveTarget(ArticulationBody jaw, float target)
    {
        if (jaw == null || jaw.dofCount == 0)
            return;

        var drive = jaw.xDrive;
        drive.target = target;
        jaw.xDrive = drive;
    }

    private void CommandCallback(Float64MultiArray msg)
    {
        if (msg.Data == null || msg.Data.Length < 2)
        {
            Debug.LogWarning($"Gripper2JawsController: ignoring command on '{commandTopic}' with " +
                             $"{msg.Data?.Length ?? 0} value(s); expected 2 ([jaw_1, jaw_2]).");
            return;
        }

        lock (targetLock)
            pendingTarget = new double[2] { msg.Data[0], msg.Data[1] };
    }

    // =========================================================================
    // Manifest-driven jaw resolution
    // =========================================================================

    /// <summary>Resolves the jaw ArticulationBodies of the gripper currently
    /// selected in pm_robot_bringup_config.yaml. Safe to call again after a
    /// reconfiguration.</summary>
    public void TryResolveJaws()
    {
        nextResolveTime = Time.time + ResolveRetryInterval;
        resolved = false;
        jaw1 = null;
        jaw2 = null;

        if (robotRoot == null)
        {
            Debug.LogError("Gripper2JawsController: robot root not set and no GameObject named 'pm_robot' found.");
            return;
        }

        if (!TryGetSelectedSuffix(out string suffix))
            return;

        jaw1 = FindJaw(Jaw1LinkPrefix + suffix);
        jaw2 = FindJaw(Jaw2LinkPrefix + suffix);
        resolved = jaw1 != null && jaw2 != null;

        if (!resolved)
            return;

        ConfigureDrive(jaw1);
        ConfigureDrive(jaw2);
    }

    /// <summary>Puts a jaw drive into position control. xDrive returns a copy of
    /// the drive struct, so it has to be read, modified and written back.</summary>
    private void ConfigureDrive(ArticulationBody jaw)
    {
        var drive = jaw.xDrive;
        drive.driveType = ArticulationDriveType.Target;
        drive.stiffness = driveStiffness;
        drive.damping = driveDamping;
        drive.forceLimit = driveForceLimit;
        jaw.xDrive = drive;
    }

    /// <summary>Suffix of the selected 2-jaw gripper, e.g.
    /// "Schunk_MPG_20_plus_Jaw_ASG_Gripper"; false if no gripper is selected or
    /// the config/manifest could not be read.</summary>
    private bool TryGetSelectedSuffix(out string suffix)
    {
        suffix = null;

        string configPath = configureRobot.GetConfigFilePath("pm_robot_bringup", "pm_robot_bringup_config.yaml");
        var yamlRoot = string.IsNullOrEmpty(configPath) ? null : GenericFunctions.YamlLoader.LoadYaml(configPath);
        if (yamlRoot == null)
        {
            Debug.LogError($"Gripper2JawsController: could not load '{configPath}'.");
            return false;
        }

        var manifest = RobotUnityManifest.Load(configureRobot.GetManifestPath());
        if (manifest == null)
            return false;

        var section = manifest.sections.FirstOrDefault(s => s.yamlPath == ManifestSectionPath);
        if (section == null)
        {
            Debug.LogError($"Gripper2JawsController: manifest has no section '{ManifestSectionPath}'.");
            return false;
        }

        if (!TryGetSectionDict(yamlRoot, section.yamlPath, out var sectionDict))
            return false;

        if (!string.IsNullOrEmpty(section.enableKey) && !GetBool(sectionDict, section.enableKey))
        {
            Debug.Log("Gripper2JawsController: no 2-jaw gripper selected " +
                      $"({section.yamlPath}.{section.enableKey} is false); staying idle.");
            return false;
        }

        string[] selected = section.selectKeys.Select(k => GetString(sectionDict, k)).ToArray();
        var item = section.items.FirstOrDefault(i => IsSelected(i.yamlNames, selected));
        if (item == null)
        {
            Debug.LogError("Gripper2JawsController: no manifest item in " +
                           $"'{section.yamlPath}' matches the selection [{string.Join(", ", selected)}].");
            return false;
        }

        string candidate = string.Join("_", item.yamlNames);

        // The jaw links are what this script drives; if the generator ever
        // renames them, fail here rather than silently not moving the gripper.
        foreach (string prefix in new[] { Jaw1LinkPrefix, Jaw2LinkPrefix })
        {
            if (!item.links.Contains(prefix + candidate))
            {
                Debug.LogError($"Gripper2JawsController: manifest item '{candidate}' does not own the " +
                               $"expected link '{prefix + candidate}'. Regenerate the manifest " +
                               "(generate_unity_urdf.py) or update this script's link prefixes.");
                return false;
            }
        }

        suffix = candidate;
        return true;
    }

    private ArticulationBody FindJaw(string linkName)
    {
        foreach (var body in robotRoot.GetComponentsInChildren<ArticulationBody>(true))
        {
            if (body.name != linkName)
                continue;

            if (body.dofCount == 0)
            {
                Debug.LogError($"Gripper2JawsController: '{linkName}' has no articulation DOF; " +
                               "it cannot be driven.");
                return null;
            }
            return body;
        }

        Debug.LogWarning($"Gripper2JawsController: jaw '{linkName}' not found under " +
                         $"'{robotRoot.name}'. Import it via 'Config Tools/Delta Import Robot Parts'.");
        return null;
    }

    // =========================================================================
    // YAML helpers (kept local; configureRobot's equivalents are private)
    // =========================================================================

    private static bool IsSelected(string[] yamlNames, string[] selected)
    {
        if (yamlNames.Length != selected.Length)
            return false;
        for (int i = 0; i < yamlNames.Length; i++)
        {
            if (string.IsNullOrEmpty(selected[i]) || selected[i] == "empty" || selected[i] != yamlNames[i])
                return false;
        }
        return true;
    }

    private static bool TryGetSectionDict(Dictionary<string, object> root, string yamlPath,
        out Dictionary<string, object> result)
    {
        result = root;
        foreach (var key in yamlPath.Split('/'))
        {
            if (result == null || !result.TryGetValue(key, out var val) ||
                !(val is Dictionary<string, object> typed))
            {
                Debug.LogError($"Gripper2JawsController: YAML path '{yamlPath}' not found (at key '{key}').");
                result = null;
                return false;
            }
            result = typed;
        }
        return true;
    }

    private static bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue = false)
    {
        if (dict.TryGetValue(key, out var val))
        {
            if (val is bool b) return b;
            if (val is string s && bool.TryParse(s, out bool parsed)) return parsed;
        }
        Debug.LogWarning($"Gripper2JawsController: bool key '{key}' not found; defaulting to {defaultValue}.");
        return defaultValue;
    }

    private static string GetString(Dictionary<string, object> dict, string key, string defaultValue = null)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
            return val.ToString();
        Debug.LogWarning($"Gripper2JawsController: string key '{key}' not found; defaulting to '{defaultValue}'.");
        return defaultValue;
    }
}
