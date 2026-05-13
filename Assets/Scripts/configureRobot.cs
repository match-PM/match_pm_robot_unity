using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ROS2;
using UtilityFunctions; // GetConfigFilePath, GenericFunctions.YamlLoader

using ConfigureRobotReq = pm_msgs.srv.EmptyWithSuccess_Request;
using ConfigureRobotResp = pm_msgs.srv.EmptyWithSuccess_Response;

public class configureRobot : MonoBehaviour
{
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
    // 1K Dispenser Tips
    // -------------------------------------------------------------------------
    [Header("1K Dispenser Tips  (pm_robot_1K_dispenser_tip)")]
    [Tooltip("YAML: availabe_dispenser_tips — Dis_Tip_Yellow_I_100_A240_L12700")]
    public GameObject dispenser_Tip_Yellow_I_100_A240_L12700;

    [Tooltip("YAML: availabe_dispenser_tips — Dis_Tip_Lavender_I_150_A310_L12700")]
    public GameObject dispenser_Tip_Lavender_I_150_A310_L12700;

    [Tooltip("YAML: availabe_dispenser_tips — Dis_Tip_Red_I_250_A520_L12700")]
    public GameObject dispenser_Tip_Red_I_250_A520_L12700;

    // -------------------------------------------------------------------------
    // Gonio Left
    // -------------------------------------------------------------------------
    [Header("Gonio Left  (pm_robot_gonio_left)")]
    [Tooltip("YAML: with_Gonio_Left — entire Gonio Left assembly")]
    public GameObject assembly_GonioLeft;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_left_Siemens_Carrier")]
    public GameObject gonioLeft_Chuck_Siemens_Carrier;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_left_Siemens_Carrier_SIN")]
    public GameObject gonioLeft_Chuck_Siemens_Carrier_SIN;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_left_Siemens_Sensor")]
    public GameObject gonioLeft_Chuck_Siemens_Sensor;

    [Tooltip("YAML: availabe_chucks — Chuck_Siemens_Carrier_P39")]
    public GameObject gonioLeft_Chuck_Siemens_Carrier_P39;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_left_IPEG_Demonstrator")]
    public GameObject gonioLeft_Chuck_IPEG_Demonstrator;

    [Tooltip("YAML: availabe_chucks — Chuck_Siemens_Carrier_P39_V2")]
    public GameObject gonioLeft_Chuck_Siemens_Carrier_P39_V2;

    [Tooltip("YAML: availabe_chucks — Chuck_Carrier_Demo")]
    public GameObject gonioLeft_Chuck_Carrier_Demo;

    // -------------------------------------------------------------------------
    // Gonio Right
    // -------------------------------------------------------------------------
    [Header("Gonio Right  (pm_robot_gonio_right)")]
    [Tooltip("YAML: with_Gonio_Right — entire Gonio Right assembly")]
    public GameObject assembly_GonioRight;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_right_Siemens_UFC")]
    public GameObject gonioRight_Chuck_Siemens_UFC;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_right_double_lens_holder_12_7")]
    public GameObject gonioRight_Chuck_DoubleLensHolder_12_7;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_right_ipeg_active_alignment_station")]
    public GameObject gonioRight_Chuck_IPEG_ActiveAlignment;

    [Tooltip("YAML: availabe_chucks — chuck_gonio_right_siemens_SIN")]
    public GameObject gonioRight_Chuck_Siemens_SIN;

    // -------------------------------------------------------------------------
    // Smarpod Station
    // -------------------------------------------------------------------------
    [Header("Smarpod Station  (pm_smparpod_station)")]
    [Tooltip("YAML: with_smarpod_station — entire Smarpod Station assembly")]
    public GameObject assembly_SmarpodStation;

    [Tooltip("YAML: availabe_chucks — chuck_diodenplacement  (use_chuck: 'empty' deactivates all chucks)")]
    public GameObject smarpod_Chuck_DiodePlacement;

    // -------------------------------------------------------------------------
    // Parallel Gripper 1 Jaw — PG1
    // -------------------------------------------------------------------------
    [Header("PG1 Tool Bodies  (pm_robot_tool_parallel_gripper_1_jaw)")]
    [Tooltip("YAML: available_tools[0].tool_name — SmarAct_Piezo_Gripper")]
    public GameObject pg1_Tool_SmarAct_Piezo_Gripper;

    [Tooltip("YAML: available_tools[1].tool_name — SmarAct_Piezo_Gripper_flat")]
    public GameObject pg1_Tool_SmarAct_Piezo_Gripper_flat;

    [Header("PG1 Jaws — SmarAct_Piezo_Gripper")]
    [Tooltip("YAML: available_tools[0].availabe_jaws[0] — Default_Jaw")]
    public GameObject pg1_Jaw_SmarActGripper_Default;

    [Header("PG1 Jaws — SmarAct_Piezo_Gripper_flat")]
    [Tooltip("YAML: available_tools[1].availabe_jaws[0] — Default_Jaw")]
    public GameObject pg1_Jaw_SmarActGripperFlat_Default;

    [Tooltip("YAML: available_tools[1].availabe_jaws[1] — Flat_Jaw")]
    public GameObject pg1_Jaw_SmarActGripperFlat_Flat;

    [Tooltip("YAML: available_tools[1].availabe_jaws[1] — Sensing_Jaw")]
    public GameObject pg1_Jaw_SmarActGripperFlat_Sensing;

    // -------------------------------------------------------------------------
    // Parallel Gripper 2 Jaws — PG2
    // -------------------------------------------------------------------------
    [Header("PG2 Tool Body  (pm_robot_tool_parallel_gripper_2_jaws)")]
    [Tooltip("YAML: available_tools[0].tool_name — Schunk_MPG_10_plus")]
    public GameObject pg2_Tool_Schunk_MPG_10_plus;

    [Header("PG2 Jaws — Schunk_MPG_10_plus")]
    [Tooltip("YAML: available_tools[0].availabe_jaws[0] — Jaw_3mm_Lens")]
    public GameObject pg2_Jaw_SchunkMPG_Jaw3mmLens;

    // -------------------------------------------------------------------------
    // Vacuum Tools
    // -------------------------------------------------------------------------
    [Header("Vacuum Tool Bodies  (pm_robot_vacuum_tools)")]
    [Tooltip("YAML: availabe_tools[0].tool_name — Tool_SPT_Toolholder")]
    public GameObject vac_Tool_SPT_Toolholder;

    [Tooltip("YAML: availabe_tools[1].tool_name — Tool_Schunk_SPT_Tool_old")]
    public GameObject vac_Tool_Schunk_SPT_Tool_old;

    [Tooltip("YAML: availabe_tools[2].tool_name — Siemens_Vacuum_Array")]
    public GameObject vac_Tool_Siemens_Vacuum_Array;

    [Tooltip("YAML: availabe_tools[3].tool_name — Siemens_Vacuum_Array_short")]
    public GameObject vac_Tool_Siemens_Vacuum_Array_short;

    [Tooltip("YAML: availabe_tools[4].tool_name — Siemens_Vacuum_Array_angled")]
    public GameObject vac_Tool_Siemens_Vacuum_Array_angled;

    [Tooltip("YAML: availabe_tools[5].tool_name — Schunk_Lens_Gripper")]
    public GameObject vac_Tool_Schunk_Lens_Gripper;

    [Tooltip("YAML: availabe_tools[6].tool_name — Schunk_Lens_Gripper_long")]
    public GameObject vac_Tool_Schunk_Lens_Gripper_long;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private Dictionary<string, object> _yamlRoot;

    // Per-section selection maps (YAML name → pre-wired GO)
    private Dictionary<string, GameObject> _dispenserTips;
    private Dictionary<string, GameObject> _gonioLeftChucks;
    private Dictionary<string, GameObject> _gonioRightChucks;
    private Dictionary<string, GameObject> _pg1Tools;
    private Dictionary<string, Dictionary<string, GameObject>> _pg1JawsPerTool;
    private Dictionary<string, GameObject> _pg2Tools;
    private Dictionary<string, GameObject> _pg2Jaws;
    private Dictionary<string, GameObject> _vacTools;
    private Dictionary<string, GameObject> _smarpodChucks;

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

        BuildMaps();

        string filepath = GetConfigFilePath("pm_robot_bringup", "pm_robot_bringup_config.yaml");
        _yamlRoot = GenericFunctions.YamlLoader.LoadYaml(filepath);

        if (_yamlRoot != null)
            ApplyConfiguration(_yamlRoot);
        else
            Debug.LogError("configureRobot: Error loading yaml file: " + filepath);
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
            string filepath = GetConfigFilePath("pm_robot_bringup", "pm_robot_bringup_config.yaml");
            _yamlRoot = GenericFunctions.YamlLoader.LoadYaml(filepath);
            if (_yamlRoot != null)
            {
                ApplyConfiguration(_yamlRoot);
                Debug.Log("configureRobot: Reconfigured via ROS2 service.");
            }
            else
            {
                Debug.LogError("configureRobot service: Error loading yaml file: " + filepath);
            }
        });

        response.Success = true;
        return response;
    }

    // =========================================================================
    // Map builder — wires public fields into internal dictionaries
    // =========================================================================

    private void BuildMaps()
    {
        _dispenserTips = new Dictionary<string, GameObject>
        {
            { "Dis_Tip_Yellow_I_100_A240_L12700",   dispenser_Tip_Yellow_I_100_A240_L12700 },
            { "Dis_Tip_Lavender_I_150_A310_L12700", dispenser_Tip_Lavender_I_150_A310_L12700 },
            { "Dis_Tip_Red_I_250_A520_L12700",      dispenser_Tip_Red_I_250_A520_L12700 },
        };

        _gonioLeftChucks = new Dictionary<string, GameObject>
        {
            { "chuck_gonio_left_Siemens_Carrier",     gonioLeft_Chuck_Siemens_Carrier },
            { "chuck_gonio_left_Siemens_Carrier_SIN", gonioLeft_Chuck_Siemens_Carrier_SIN },
            { "chuck_gonio_left_Siemens_Sensor",      gonioLeft_Chuck_Siemens_Sensor },
            { "Chuck_Siemens_Carrier_P39",            gonioLeft_Chuck_Siemens_Carrier_P39 },
            { "chuck_gonio_left_IPEG_Demonstrator",   gonioLeft_Chuck_IPEG_Demonstrator },
            { "Chuck_Siemens_Carrier_P39_V2",         gonioLeft_Chuck_Siemens_Carrier_P39_V2 },
            { "Chuck_Carrier_Demo",                   gonioLeft_Chuck_Carrier_Demo },
        };

        _gonioRightChucks = new Dictionary<string, GameObject>
        {
            { "chuck_gonio_right_Siemens_UFC",                  gonioRight_Chuck_Siemens_UFC },
            { "chuck_gonio_right_double_lens_holder_12_7",      gonioRight_Chuck_DoubleLensHolder_12_7 },
            { "chuck_gonio_right_ipeg_active_alignment_station", gonioRight_Chuck_IPEG_ActiveAlignment },
            { "chuck_gonio_right_siemens_SIN",                  gonioRight_Chuck_Siemens_SIN },
        };

        _pg1Tools = new Dictionary<string, GameObject>
        {
            { "SmarAct_Piezo_Gripper",      pg1_Tool_SmarAct_Piezo_Gripper },
            { "SmarAct_Piezo_Gripper_flat", pg1_Tool_SmarAct_Piezo_Gripper_flat },
        };

        _pg1JawsPerTool = new Dictionary<string, Dictionary<string, GameObject>>
        {
            {
                "SmarAct_Piezo_Gripper", new Dictionary<string, GameObject>
                {
                    { "Default_Jaw", pg1_Jaw_SmarActGripper_Default },
                }
            },
            {
                "SmarAct_Piezo_Gripper_flat", new Dictionary<string, GameObject>
                {
                    { "Default_Jaw", pg1_Jaw_SmarActGripperFlat_Default },
                    { "Flat_Jaw",    pg1_Jaw_SmarActGripperFlat_Flat },
                    { "Sensing_Jaw", pg1_Jaw_SmarActGripperFlat_Sensing}, 
                }
            },
        };

        _pg2Tools = new Dictionary<string, GameObject>
        {
            { "Schunk_MPG_10_plus", pg2_Tool_Schunk_MPG_10_plus },
        };

        _pg2Jaws = new Dictionary<string, GameObject>
        {
            { "Jaw_3mm_Lens", pg2_Jaw_SchunkMPG_Jaw3mmLens },
        };

        _vacTools = new Dictionary<string, GameObject>
        {
            { "Tool_SPT_Toolholder",         vac_Tool_SPT_Toolholder },
            { "Tool_Schunk_SPT_Tool_old",     vac_Tool_Schunk_SPT_Tool_old },
            { "Siemens_Vacuum_Array",         vac_Tool_Siemens_Vacuum_Array },
            { "Siemens_Vacuum_Array_short",   vac_Tool_Siemens_Vacuum_Array_short },
            { "Siemens_Vacuum_Array_angled",  vac_Tool_Siemens_Vacuum_Array_angled },
            { "Schunk_Lens_Gripper",          vac_Tool_Schunk_Lens_Gripper },
            { "Schunk_Lens_Gripper_long",     vac_Tool_Schunk_Lens_Gripper_long },
        };

        // "empty" is intentionally absent — it is handled as a sentinel in ConfigureSmarpodStation
        _smarpodChucks = new Dictionary<string, GameObject>
        {
            { "chuck_diodenplacement", smarpod_Chuck_DiodePlacement },
        };
    }

    // =========================================================================
    // Top-level configuration dispatcher
    // =========================================================================

    private void ApplyConfiguration(Dictionary<string, object> root)
    {
        ConfigureMeasuringSystems(root);
        ConfigureDispenserTips(root);
        ConfigureGonioLeft(root);
        ConfigureGonioRight(root);
        ConfigureSmarpodStation(root);
        ConfigureTools(root);
    }

    // =========================================================================
    // Section configurators
    // =========================================================================

    private void ConfigureMeasuringSystems(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "measuring_systems", out var d)) return;
        measuringSystem_ConfocalBottom?.SetActive(GetBool(d, "use_confocal_bottom"));
        measuringSystem_ConfocalHead?.SetActive(GetBool(d, "use_confocal_head"));
        measuringSystem_KeyenceBottom?.SetActive(GetBool(d, "use_keyence_bottom"));
    }

    private void ConfigureDispenserTips(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "pm_robot_1K_dispenser_tip", out var d)) return;
        ActivateSelected(_dispenserTips, GetString(d, "use_dispenser_tip"));
    }

    private void ConfigureGonioLeft(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "pm_robot_gonio_left", out var d)) return;
        bool active = GetBool(d, "with_Gonio_Left");
        assembly_GonioLeft?.SetActive(active);
        if (active)
            ActivateSelected(_gonioLeftChucks, GetString(d, "use_chuck"));
        else
            DeactivateAll(_gonioLeftChucks);
    }

    private void ConfigureGonioRight(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "pm_robot_gonio_right", out var d)) return;
        bool active = GetBool(d, "with_Gonio_Right");
        assembly_GonioRight?.SetActive(active);
        if (active)
            ActivateSelected(_gonioRightChucks, GetString(d, "use_chuck"));
        else
            DeactivateAll(_gonioRightChucks);
    }

    private void ConfigureSmarpodStation(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "pm_smparpod_station", out var d)) return;
        bool active = GetBool(d, "with_smarpod_station");
        assembly_SmarpodStation?.SetActive(active);
        if (!active)
        {
            DeactivateAll(_smarpodChucks);
            return;
        }
        string useChuck = GetString(d, "use_chuck");
        if (string.IsNullOrEmpty(useChuck) || useChuck == "empty")
            DeactivateAll(_smarpodChucks);
        else
            ActivateSelected(_smarpodChucks, useChuck);
    }

    private void ConfigureTools(Dictionary<string, object> root)
    {
        if (!TryGetSubDict(root, "pm_robot_tools", out var toolsDict)) return;
        ConfigurePG1(toolsDict);
        ConfigurePG2(toolsDict);
        ConfigureVacuumTools(toolsDict);
    }

    private void ConfigurePG1(Dictionary<string, object> toolsDict)
    {
        if (!TryGetSubDict(toolsDict, "pm_robot_tool_parallel_gripper_1_jaw", out var d)) return;
        bool active = GetBool(d, "use_paralell_gripper");

        if (!active)
        {
            DeactivateAll(_pg1Tools);
            foreach (var jawMap in _pg1JawsPerTool.Values) DeactivateAll(jawMap);
            return;
        }

        string useTool = GetString(d, "use_tool");
        ActivateSelected(_pg1Tools, useTool);

        string useJaw = GetString(d, "use_jaw_type");
        foreach (var kv in _pg1JawsPerTool)
        {
            if (kv.Key == useTool)
                ActivateSelected(kv.Value, useJaw);
            else
                DeactivateAll(kv.Value);
        }
    }

    private void ConfigurePG2(Dictionary<string, object> toolsDict)
    {
        if (!TryGetSubDict(toolsDict, "pm_robot_tool_parallel_gripper_2_jaws", out var d)) return;
        bool active = GetBool(d, "use_paralell_gripper");

        if (!active)
        {
            DeactivateAll(_pg2Tools);
            DeactivateAll(_pg2Jaws);
            return;
        }

        ActivateSelected(_pg2Tools, GetString(d, "use_tool"));
        ActivateSelected(_pg2Jaws, GetString(d, "use_jaw_type"));
    }

    private void ConfigureVacuumTools(Dictionary<string, object> toolsDict)
    {
        if (!TryGetSubDict(toolsDict, "pm_robot_vacuum_tools", out var d)) return;
        bool active = GetBool(d, "use_vacuum_tool");

        if (!active)
        {
            DeactivateAll(_vacTools);
            return;
        }

        string useTool = GetString(d, "use_tool");
        ActivateSelected(_vacTools, useTool);
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

    private void ActivateSelected(Dictionary<string, GameObject> group, string selectedKey)
    {
        foreach (var kv in group)
        {
            if (kv.Value == null) continue;
            kv.Value.SetActive(kv.Key == selectedKey);
        }
    }

    private void DeactivateAll(Dictionary<string, GameObject> group)
    {
        foreach (var kv in group)
            if (kv.Value != null)
            {
                kv.Value?.SetActive(false);
            }
            
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
