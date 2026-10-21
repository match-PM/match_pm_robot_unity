using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// =============================================================================
// Data model for pm_robot_unity_manifest.json, generated next to the combined
// Unity URDF by generate_unity_urdf.py (pm_robot_description package).
//
// The manifest maps every selectable YAML configuration item (dispenser tip,
// gonio/smarpod chuck, vacuum tool+tip, gripper tool+jaw) to the URDF link
// names it owns and to its empty spawn frames (gonio part origins, smarpod
// part spawns and tool TCPs).
// configureRobot uses it at runtime to toggle the matching GameObjects, and
// the DeltaUrdfImporter editor tool uses it to import missing links, so a new
// tool added in ROS requires no Unity code changes.
// =============================================================================

[Serializable]
public class RobotUnityManifest
{
    public string generatedAt;
    public string sourceConfig;
    public string urdf;
    public ManifestSection[] sections;

    /// <summary>Loads and validates a manifest; returns null (with an error
    /// logged) if the file is missing or malformed.</summary>
    public static RobotUnityManifest Load(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogError($"RobotUnityManifest: manifest not found at '{path}'. " +
                           "Run generate_unity_urdf.py (pm_robot_description) to create it.");
            return null;
        }

        RobotUnityManifest manifest;
        try
        {
            manifest = JsonUtility.FromJson<RobotUnityManifest>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            Debug.LogError($"RobotUnityManifest: failed to parse '{path}': {ex.Message}");
            return null;
        }

        if (manifest?.sections == null || manifest.sections.Length == 0)
        {
            Debug.LogError($"RobotUnityManifest: '{path}' contains no sections.");
            return null;
        }

        manifest.Validate(path);
        return manifest;
    }

    private void Validate(string path)
    {
        // A link owned by two items of the same section is legitimate (an
        // identical shared visual, e.g. two dispenser tips differing only in
        // TCP offset); across sections it would break activation.
        var owner = new Dictionary<string, ManifestSection>();
        foreach (var section in sections)
        {
            if (section.items == null || section.items.Length == 0)
            {
                Debug.LogWarning($"RobotUnityManifest: section '{section.yamlPath}' has no items ({path}).");
                continue;
            }
            foreach (var item in section.items)
            {
                // Spawn-frame-only items (smarpod 'empty' chuck) legitimately
                // have no rootLinks.
                if ((item.rootLinks == null || item.rootLinks.Length == 0) &&
                    (item.spawnPoints == null || item.spawnPoints.Length == 0))
                    Debug.LogWarning($"RobotUnityManifest: item '{string.Join("/", item.yamlNames)}' " +
                                     $"in '{section.yamlPath}' has neither rootLinks nor spawnPoints ({path}).");
                foreach (var link in item.links ?? Array.Empty<string>())
                {
                    if (owner.TryGetValue(link, out var other) && other != section)
                        Debug.LogWarning($"RobotUnityManifest: link '{link}' appears in sections " +
                                         $"'{other.yamlPath}' and '{section.yamlPath}' ({path}).");
                    owner[link] = section;
                }
                foreach (var spawnPoint in item.spawnPoints ?? Array.Empty<ManifestSpawnPoint>())
                {
                    if (string.IsNullOrEmpty(spawnPoint.name) ||
                        string.IsNullOrEmpty(spawnPoint.sourceLink) ||
                        string.IsNullOrEmpty(spawnPoint.parentLink))
                    {
                        Debug.LogWarning($"RobotUnityManifest: item " +
                                         $"'{string.Join("/", item.yamlNames)}' in " +
                                         $"'{section.yamlPath}' has an incomplete spawn point ({path}).");
                    }
                }
            }
        }
    }
}

[Serializable]
public class ManifestSection
{
    /// <summary>Path of the section in pm_robot_bringup_config.yaml, nested
    /// keys separated by '/', e.g. "pm_robot_tools/pm_robot_vacuum_tools".</summary>
    public string yamlPath;

    /// <summary>Bool key enabling the section ("" = always enabled). Key names
    /// mirror the YAML verbatim, including its typos (e.g. "use_paralell_gripper").</summary>
    public string enableKey;

    /// <summary>Selection keys, e.g. ["use_tool", "use_tip"]. An item is
    /// selected iff the YAML value of selectKeys[i] equals yamlNames[i] for all i.</summary>
    public string[] selectKeys;

    /// <summary>Name of the Unity GameObject the section's items live under
    /// (informational; activation is done by link name lookup).</summary>
    public string unityParent;

    public ManifestItem[] items;
}

[Serializable]
public class ManifestItem
{
    /// <summary>Selection tuple, aligned with the section's selectKeys.</summary>
    public string[] yamlNames;

    /// <summary>All URDF link names owned by this item (equal to the Unity
    /// GameObject names).</summary>
    public string[] links;

    /// <summary>The owned links whose parent is outside the item; toggling
    /// these toggles the whole item.</summary>
    public string[] rootLinks;

    /// <summary>Mesh paths referenced by the owned links, relative to the
    /// pm_robot_description share directory (e.g. "meshes/Tools/x.STL").</summary>
    public string[] meshes;

    /// <summary>Empty Unity GameObjects used as part/tool spawn frames. Their
    /// pose comes from sourceLink in the generated URDF, then the importer
    /// reparents them below parentLink without changing their world pose.</summary>
    public ManifestSpawnPoint[] spawnPoints;
}

[Serializable]
public class ManifestSpawnPoint
{
    /// <summary>Final Unity GameObject name, e.g. PM_Robot_Tool_TCP.</summary>
    public string name;

    /// <summary>Scenario-unique link in the combined URDF. It carries the
    /// exact xyz/rpy generated by the ROS xacro and YAML configuration.</summary>
    public string sourceLink;

    /// <summary>Manifest-owned chuck/tool link that becomes the empty
    /// GameObject's parent in the Unity scene; a base robot link for items
    /// without a visible part (smarpod 'empty' chuck).</summary>
    public string parentLink;
}
