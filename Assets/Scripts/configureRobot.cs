using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ROS2;
using UtilityFunctions; // GetConfigFilePath, GenericFunctions.YamlLoader

using ConfigureRobotReq = pm_msgs.srv.EmptyWithSuccess_Request;
using ConfigureRobotResp = pm_msgs.srv.EmptyWithSuccess_Response;

// =============================================================================
// Applies the pm_robot_bringup_config.yaml configuration to the Unity robot:
// activates the selected tools/tips/chucks and deactivates the alternatives.
//
// The mapping from YAML selections to GameObjects is NOT hardcoded here; it is
// read from pm_robot_unity_manifest.json, which generate_unity_urdf.py emits
// next to the combined URDF in the pm_robot_description install. GameObjects
// are resolved by name (Unity object name == URDF link name), so a new tool
// only needs a ROS-side entry, regeneration, and a run of the
// "Config Tools/Delta Import Robot Parts" editor tool - no code changes.
//
// Only hardware that is outside the URDF generation (measuring systems, the
// gonio/smarpod assemblies) is still wired in the Inspector.
// =============================================================================

public class configureRobot : MonoBehaviour
{
    [Header("Robot")]
    [Tooltip("Root of the robot hierarchy; all manifest links are resolved by name below this object. Defaults to the GameObject named 'pm_robot'.")]
    public GameObject robotRoot;

    // -------------------------------------------------------------------------
    // Measuring Systems
    // -------------------------------------------------------------------------
    [Header("Measuring Systems")]
    [Tooltip("YAML: measuring_systems.use_confocal_bottom")]
    public GameObject measuringSystem_ConfocalBottom;

    [Tooltip("YAML: measuring_systems.use_confocal_head")]
    public GameObject measuringSystem_ConfocalHead;

    [Tooltip("YAML: measuring_systems.use_keyence_bottom")]
    public GameObject measuringSystem_KeyenceBottom;

    // -------------------------------------------------------------------------
    // Assemblies (enable flags; their chucks/tools come from the manifest)
    // -------------------------------------------------------------------------
    [Header("Assemblies")]
    [Tooltip("YAML: pm_robot_gonio_left.with_Gonio_Left — entire Gonio Left assembly")]
    public GameObject assembly_GonioLeft;

    [Tooltip("YAML: pm_robot_gonio_right.with_Gonio_Right — entire Gonio Right assembly")]
    public GameObject assembly_GonioRight;

    [Tooltip("YAML: pm_smparpod_station.with_smarpod_station — entire Smarpod Station assembly")]
    public GameObject assembly_SmarpodStation;

    [Tooltip("ROS offset (m, base_link frame) of Gonio Right while the Smarpod Station is active. " +
             "Mirrors gonio_offset_smarpod_x/y in pm_robot_gonio_right.xacro.")]
    public Vector3 gonioRightSmarpodOffsetRos = new Vector3(0f, 0.6f, 0f);

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private Dictionary<string, object> _yamlRoot;
    private RobotUnityManifest _manifest;
    private Vector3 _gonioRightAppliedOffset; // Unity frame, currently added to Gonio Right

    // ROS2
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<ConfigureRobotReq, ConfigureRobotResp> srvConfigureRobot;

    // Thread-safe queue for Unity main-thread execution
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        if (robotRoot == null)
        {
            // A scene can contain more than one robot (e.g. a leftover
            // 'pm_robot (1)' from an earlier full import). Picking one at
            // random would configure the wrong robot, so say so out loud.
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                .GetRootGameObjects();
            var candidates = new List<GameObject>();
            foreach (var go in roots)
                if (go.name == "pm_robot" || go.name.StartsWith("pm_robot ("))
                    candidates.Add(go);

            if (candidates.Count > 1)
                Debug.LogWarning("configureRobot: the scene contains several robot roots (" +
                                 string.Join(", ", candidates.ConvertAll(g => g.name)) +
                                 "). Assign the intended one to 'Robot Root' in the Inspector; " +
                                 "falling back to the one named 'pm_robot'.");

            robotRoot = candidates.Find(g => g.name == "pm_robot")
                        ?? (candidates.Count > 0 ? candidates[0] : null);
        }
        if (robotRoot == null)
            Debug.LogError("configureRobot: robot root not set and no GameObject named 'pm_robot' found.");

        LoadAndApplyConfiguration();
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action?.Invoke();

        if (ros2Unity != null && ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("configure_robot_node");
                srvConfigureRobot = ros2Node.CreateService<ConfigureRobotReq, ConfigureRobotResp>(
                    "~/configure_robot", ConfigureRobotServiceCallback);
                Debug.Log("configureRobot: ROS2 service '~/configure_robot' registered.");
            }
        }
    }

    // =========================================================================
    // ROS2 service callback
    // =========================================================================

    private ConfigureRobotResp ConfigureRobotServiceCallback(ConfigureRobotReq request)
    {
        var response = new ConfigureRobotResp();

        mainThreadActions.Enqueue(() =>
        {
            LoadAndApplyConfiguration();
            Debug.Log("configureRobot: Reconfigured via ROS2 service.");
        });

        response.Success = true;
        return response;
    }

    // =========================================================================
    // Configuration loading
    // =========================================================================

    private void LoadAndApplyConfiguration()
    {
        string filepath = GetConfigFilePath("pm_robot_bringup", "pm_robot_bringup_config.yaml");
        _yamlRoot = GenericFunctions.YamlLoader.LoadYaml(filepath);
        if (_yamlRoot == null)
        {
            Debug.LogError("configureRobot: Error loading yaml file: " + filepath);
            return;
        }

        // Reload the manifest on every apply so a regeneration in ROS is
        // picked up by the next reconfigure service call.
        _manifest = RobotUnityManifest.Load(GetManifestPath());

        ApplyConfiguration(_yamlRoot);
    }

    public static string GetManifestPath()
    {
        string packagePath = GetROS2PackagePath("pm_robot_description");
        if (string.IsNullOrEmpty(packagePath))
            return null;
        return Path.Combine(packagePath, "share", "pm_robot_description", "urdf",
                            "pm_robot_unity_manifest.json");
    }

    // =========================================================================
    // Top-level configuration dispatcher
    // =========================================================================

    private void ApplyConfiguration(Dictionary<string, object> root)
    {
        ConfigureMeasuringSystems(root);
        ConfigureAssemblies(root);
        ConfigureSmarpodStation(root);
        ApplyManifestSections(root);
    }

    // =========================================================================
    // Inspector-wired hardware (outside the URDF generation)
    // =========================================================================

    private void ConfigureMeasuringSystems(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "measuring_systems", out var d)) return;
        measuringSystem_ConfocalBottom?.SetActive(GetBool(d, "use_confocal_bottom"));
        // New confocal-head variants are generated into the manifest and are
        // activated below from confocal_sensor_head.use_config. Keep the old
        // Inspector-wired single mesh disabled when that section is present;
        // this also makes existing scenes migrate cleanly after a delta import.
        bool manifestControlsConfocalHead = _manifest?.sections?.Any(
            s => s.yamlPath == "confocal_sensor_head") == true;
        measuringSystem_ConfocalHead?.SetActive(
            !manifestControlsConfocalHead && GetBool(d, "use_confocal_head"));
        measuringSystem_KeyenceBottom?.SetActive(GetBool(d, "use_keyence_bottom"));
    }

    private void ConfigureAssemblies(Dictionary<string, object> root)
    {
        if (TryGetSubDict(root, "pm_robot_gonio_left", out var left))
            assembly_GonioLeft?.SetActive(GetBool(left, "with_Gonio_Left"));
        if (TryGetSubDict(root, "pm_robot_gonio_right", out var right))
            assembly_GonioRight?.SetActive(GetBool(right, "with_Gonio_Right"));
    }

    // The station's chucks and part spawn frames come from the manifest.
    private void ConfigureSmarpodStation(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "pm_smparpod_station", out var d)) return;
        bool active = GetBool(d, "with_smarpod_station");
        assembly_SmarpodStation?.SetActive(active);
        PlaceGonioRight(active);
    }

    /// <summary>The Smarpod Station takes Gonio Right's place, so ROS moves
    /// Gonio Right aside while the station is configured. Applied as a delta
    /// to the current pose, so calibration offsets applied by
    /// ApplyCalibrationConfig are kept regardless of script order.</summary>
    private void PlaceGonioRight(bool smarpodStation)
    {
        if (assembly_GonioRight == null)
            return;

        Vector3 target = smarpodStation ? gonioRightSmarpodOffsetRos.Ros2Unity() : Vector3.zero;
        Vector3 delta = target - _gonioRightAppliedOffset;
        if (delta == Vector3.zero)
            return;

        // An active ArticulationBody ignores transform changes; move its
        // parent anchor instead (same approach as ApplyCalibrationConfig).
        var body = assembly_GonioRight.GetComponent<ArticulationBody>();
        if (body != null && body.isActiveAndEnabled)
        {
            body.matchAnchors = false;
            body.parentAnchorPosition += delta;
        }
        else
        {
            assembly_GonioRight.transform.localPosition += delta;
            if (body != null && !body.matchAnchors)
                body.parentAnchorPosition += delta;
        }
        _gonioRightAppliedOffset = target;
    }

    // =========================================================================
    // Manifest-driven tools / tips / chucks
    // =========================================================================

    private void ApplyManifestSections(Dictionary<string, object> yamlRoot)
    {
        if (_manifest == null || robotRoot == null)
            return;

        Dictionary<string, GameObject> linkIndex = BuildLinkIndex();

        // A GameObject may be shared by several items (identical shared
        // visual, e.g. two dispenser tips differing only in TCP offset), so
        // desired states are OR-combined before applying.
        var desired = new Dictionary<GameObject, bool>();
        var missingActive = new List<string>();
        var missingInactive = new List<string>();

        foreach (var section in _manifest.sections)
        {
            if (!TryGetSectionDict(yamlRoot, section.yamlPath, out var d))
                continue;

            Dictionary<string, object> enableDict = d;
            if (!string.IsNullOrEmpty(section.enableYamlPath) &&
                !TryGetSectionDict(yamlRoot, section.enableYamlPath, out enableDict))
                continue;

            bool enabled = string.IsNullOrEmpty(section.enableKey) ||
                           GetBool(enableDict, section.enableKey);
            string[] selected = section.selectKeys.Select(k => GetString(d, k)).ToArray();

            foreach (var item in section.items)
            {
                bool active = enabled && IsSelected(item.yamlNames, selected);
                foreach (var linkName in item.rootLinks)
                {
                    if (linkIndex.TryGetValue(linkName, out var go))
                    {
                        desired.TryGetValue(go, out bool current);
                        desired[go] = current || active;
                    }
                    else
                    {
                        (active ? missingActive : missingInactive).Add(linkName);
                    }
                }

                // An item without a visible part (smarpod 'empty' chuck) owns
                // only a spawn frame on a base robot link. Toggling it like a
                // chuck keeps exactly one active frame of that name, which
                // name-based lookups (GameObject.Find) rely on.
                if (item.rootLinks.Length > 0)
                    continue;
                foreach (var spawnPoint in item.spawnPoints ?? Array.Empty<ManifestSpawnPoint>())
                {
                    GameObject go = null;
                    if (linkIndex.TryGetValue(spawnPoint.parentLink, out var parent))
                        go = parent.transform.Cast<Transform>()
                            .FirstOrDefault(c => c.name == spawnPoint.name)?.gameObject;
                    if (go != null)
                    {
                        desired.TryGetValue(go, out bool current);
                        desired[go] = current || active;
                    }
                    else
                    {
                        (active ? missingActive : missingInactive).Add($"{spawnPoint.parentLink}/{spawnPoint.name}");
                    }
                }
            }
        }

        foreach (var kv in desired)
            kv.Key.SetActive(kv.Value);

        if (missingActive.Count > 0)
            Debug.LogWarning("configureRobot: the SELECTED configuration needs GameObjects that are " +
                             "missing from the scene: " + string.Join(", ", missingActive) +
                             ". Import them via 'Config Tools/Delta Import Robot Parts' in the Unity editor.");
        if (missingInactive.Count > 0)
            Debug.Log($"configureRobot: {missingInactive.Count} inactive manifest link(s) not present in " +
                      "the scene (harmless unless they get selected): " + string.Join(", ", missingInactive));
    }

    // 'empty' needs no special case: the generator only emits an 'empty' item
    // when that selection owns something (the smarpod 'empty' spawn frame).
    private static bool IsSelected(string[] yamlNames, string[] selected)
    {
        if (yamlNames.Length != selected.Length)
            return false;
        for (int i = 0; i < yamlNames.Length; i++)
        {
            if (string.IsNullOrEmpty(selected[i]) || selected[i] != yamlNames[i])
                return false;
        }
        return true;
    }

    private Dictionary<string, GameObject> BuildLinkIndex()
    {
        // Rebuilt on every apply: cheap, and robust against objects added by
        // the delta importer between two reconfigure calls.
        var index = new Dictionary<string, GameObject>();
        foreach (var t in robotRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!index.ContainsKey(t.name))
                index[t.name] = t.gameObject;
            // Duplicate names among non-manifest objects (e.g. 'Visuals') are
            // normal; manifest link names are unique by construction.
        }
        return index;
    }

    private bool TryGetSectionDict(Dictionary<string, object> root, string yamlPath,
        out Dictionary<string, object> result)
    {
        result = root;
        foreach (var key in yamlPath.Split('/'))
        {
            if (!TryGetSubDict(result, key, out result))
                return false;
        }
        return true;
    }

    // =========================================================================
    // Helper methods
    // =========================================================================

    private bool TryGetSubDict(Dictionary<string, object> parent, string key,
        out Dictionary<string, object> result)
    {
        result = null;
        if (!parent.TryGetValue(key, out var val))
        {
            Debug.LogWarning($"configureRobot: YAML key '{key}' not found.");
            return false;
        }
        if (val is Dictionary<string, object> typed)
        {
            result = typed;
            return true;
        }
        Debug.LogWarning($"configureRobot: YAML key '{key}' is not a dictionary (got {val?.GetType()}).");
        return false;
    }

    private bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue = false)
    {
        if (dict.TryGetValue(key, out var val))
        {
            if (val is bool b) return b;
            if (val is string s && bool.TryParse(s, out bool parsed)) return parsed;
        }
        Debug.LogWarning($"configureRobot: Bool key '{key}' not found or not a bool; defaulting to {defaultValue}.");
        return defaultValue;
    }

    private string GetString(Dictionary<string, object> dict, string key, string defaultValue = null)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
            return val.ToString();
        Debug.LogWarning($"configureRobot: String key '{key}' not found or null; defaulting to '{defaultValue}'.");
        return defaultValue;
    }

    // =========================================================================
    // ROS2 package / config file path utilities
    // =========================================================================

    public static string GetROS2PackagePath(string packageName)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"source /opt/ros/humble/setup.bash && ros2 pkg prefix " + packageName + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = psi };
            process.Start();
            string packagePath = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(packagePath))
                return packagePath;

            Console.WriteLine($"Error finding ROS2 package '{packageName}': {process.StandardError.ReadToEnd()}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception while retrieving ROS2 package path: " + ex.Message);
            return null;
        }
    }

    public static string GetConfigFilePath(string packageName, string fileName)
    {
        string packagePath = GetROS2PackagePath(packageName);
        if (!string.IsNullOrEmpty(packagePath))
            return Path.Combine(packagePath, "share", packageName, "config", fileName);
        return null;
    }
}
