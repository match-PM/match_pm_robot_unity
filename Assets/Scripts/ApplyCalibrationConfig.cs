using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UtilityFunctions; // GetConfigFilePath, GenericFunctions.YamlLoader

using Unity.Robotics.UrdfImporter;
using ROS2;

[Serializable]
public class JointMapping
{
    [Tooltip("Exact YAML key (e.g., 'Cam1_Toolhead_TCP_Joint')")]
    public string yamlJointName;

    [Tooltip("Target Transform in Unity")]
    public Transform target;

    [Tooltip("If true, add offsets to current local pose; otherwise replace local pose.")]
    public bool addToCurrentPose = false;

}

public class ApplyCalibrationConfig : MonoBehaviour
{
    [Header("YAML Config (loaded with your utility)")]
    public string packageName = "pm_robot_description";
    public string fileName = "pm_robot_path_real_HW.yaml";

    [Header("Axis")]
    public ArticulationBody xAxis;
    public ArticulationBody yAxis;
    public ArticulationBody zAxis;

    [Header("Manual Mapping (YAML key → Unity Transform)")]
    public List<JointMapping> mappings = new List<JointMapping>();

    Dictionary<string, object> _root;               
    Dictionary<string, object> _calibrationRoot;    

    void Awake()
    {
        // 1. Load main config
        string filepath = GetConfigFilePath(packageName, fileName);
        _calibrationRoot = GenericFunctions.YamlLoader.LoadYaml(filepath);

        if (_calibrationRoot == null)
        {
            Debug.LogError("Failed to load main YAML config.");
            return;
        }

        // 2. Extract calibration file path
        if (!_calibrationRoot.TryGetValue("pm_robot_calibration_file_path", out object calibPathObj))
        {
            Debug.LogError("Key 'pm_robot_calibration_file_path' not found in YAML.");
            return;
        }

        string calibrationFilePath = calibPathObj.ToString();

        if (!File.Exists(calibrationFilePath))
        {
            Debug.LogError($"Calibration YAML not found at: {calibrationFilePath}");
            return;
        }

        // 3. Load calibration YAML
        _root = GenericFunctions.YamlLoader.LoadYaml(calibrationFilePath);

        if (_root == null)
        {
            Debug.LogError("Failed to load calibration YAML.");
            return;
        }

        // if (_root == null)
        // {
        //     Debug.LogError($"[configureRobot] Error loading YAML file: {filepath}");
        //     return;
        // }
        // Apply XYZ calibration offsets
        ApplyCalibrationXYZ();

        ApplyOffsets();
    }

    void ApplyOffsets()
    {
        if (_root == null)
        {
            Debug.LogWarning("[configureRobot] No YAML loaded, skip.");
            return;
        }

        foreach (var map in mappings)
        {
            if (map == null || map.target == null || string.IsNullOrWhiteSpace(map.yamlJointName))
            {
                Debug.LogWarning("[configureRobot] Skipping an incomplete mapping.");
                continue;
            }

            if (!_root.TryGetValue(map.yamlJointName, out var sectionObj) || !(sectionObj is Dictionary<string, object> section))
            {
                Debug.LogWarning($"[configureRobot] YAML key not found or not a map: '{map.yamlJointName}'");
                continue;
            }

            // Read offsets (µm and degrees)
            float x_um = ReadFloat(section, "x_offset");
            float y_um = ReadFloat(section, "y_offset");
            float z_um = ReadFloat(section, "z_offset");
            float rx_deg = ReadFloat(section, "rx_offset");
            float ry_deg = ReadFloat(section, "ry_offset");
            float rz_deg = ReadFloat(section, "rz_offset");

            // µm → m (ROS frame)
            Vector3 posROS_m = new Vector3(x_um, y_um, z_um) * 1e-6f;

            // deg → Quaternion (ROS frame)
            Quaternion rotROS = Quaternion.Euler(rx_deg, ry_deg, rz_deg);

            // ROS → Unity
            Vector3 posUnity = posROS_m.Ros2Unity();
            Quaternion rotUnity = rotROS.Ros2Unity();

            ArticulationBody map_body = map.target.GetComponent<ArticulationBody>();

            // Apply (local)
            if (map.addToCurrentPose)
            {
                // check, if ArticulationBody is active
                if (map_body != null && map_body.isActiveAndEnabled)
                {
                    map_body.matchAnchors = false;
                    map_body.parentAnchorPosition += posUnity;
                    map_body.parentAnchorRotation = map_body.parentAnchorRotation * rotUnity;
                }
                else
                {
                    map.target.localPosition += posUnity;
                    map.target.localRotation *= rotUnity;
                }

            }
            else
            {
                map.target.localPosition = posUnity;
                map.target.localRotation = rotUnity;
            }

            Debug.Log($"[configureRobot] Applied '{map.yamlJointName}' → '{map.target.name}' " +
                      $"pos(µm):[{x_um},{y_um},{z_um}] rot(deg):[{rx_deg},{ry_deg},{rz_deg}]");
        }
    }

    static float ReadFloat(Dictionary<string, object> map, string key)
    {
        if (!map.TryGetValue(key, out var obj) || obj == null) return 0f;

        switch (obj)
        {
            case float f: return f;
            case double d: return (float)d;
            case int i: return i;
            case long l: return l;
            case string s:
                if (float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var val))
                    return val;
                break;
        }
        return 0f;
    }

    // [ContextMenu("Reload & Apply")]
    // public void ReloadAndApply()
    // {
    //     string filepath = GetConfigFilePath(packageName, fileName);
    //     _root = GenericFunctions.YamlLoader.LoadYaml(filepath);
    //     if (_root == null)
    //     {
    //         Debug.LogError($"[configureRobot] Error reloading YAML file: {filepath}");
    //         return;
    //     }
    //     ApplyOffsets();
    // }

    private void ApplyCalibrationXYZ()
    {
        // Get the PM_Robot_XYZ_Calibration from the YAML
        if (!_root.TryGetValue("PM_Robot_XYZ_Calibration", out var sectionObj) || !(sectionObj is Dictionary<string, object> section))
        {
            Debug.LogWarning($"[configureRobot] YAML key not found or not a map: 'PM_Robot_XYZ_Calibration'");
            return;
        }

        // Read offsets (µm and degrees)
        float x_um = ReadFloat(section, "x_offset");
        float y_um = ReadFloat(section, "y_offset");
        float z_um = ReadFloat(section, "z_offset");

        // Apply the offsets to the respective ArticulationBodies
        if (xAxis != null)
        {
            xAxis.matchAnchors = false;
            var xPos = xAxis.parentAnchorPosition;
            xPos.z += x_um * 1e-6f; // Convert µm to m
            xAxis.parentAnchorPosition = xPos;
        }
        if (yAxis != null)
        {
            yAxis.matchAnchors = false;
            var yPos = yAxis.parentAnchorPosition;
            yPos.x += y_um * 1e-6f; // Convert µm to m
            yAxis.parentAnchorPosition = yPos;
        }
        if (zAxis != null)
        {
            zAxis.matchAnchors = false;
            var zPos = zAxis.parentAnchorPosition;
            Debug.LogWarning($"Applying Z offset: {z_um} µm → {z_um * 1e-6f} m");
            zPos.y += z_um * 1e-6f; // Convert µm to m
            zAxis.parentAnchorPosition = zPos;
        }

    }

    public static string GetROS2PackagePath(string packageName)
    {
        try
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"source /opt/ros/humble/setup.bash && ros2 pkg prefix " + packageName + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = psi })
            {
                process.Start();
                string packagePath = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(packagePath))
                {
                    return packagePath;
                }
                else
                {
                    Console.WriteLine($"Error finding ROS2 package '{packageName}': {process.StandardError.ReadToEnd()}");
                    return null;
                }
            }
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
        {
            return Path.Combine(packagePath, "share", packageName, "calibration_config", fileName);
        }
        return null;
    }
}
