using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Apply general settings to a new model
/// </summary>

public class ApplyGeneralSettings
{
    public string path_config = Application.dataPath + "/PM_Robot/Configs/";

    // Apply general settings to a new model
    public void ApplySettings(string modelName, string configName)
    {
        //find the root object by modelName
        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }
        // ArticulationBody of base_link_empthy -> immovable
        ArticulationBody base_link_empty = root.transform.FindChildByPattern("base_link_empthy").GetComponent<ArticulationBody>();
        base_link_empty.immovable = true;
        
    }

    // Rename links in a model to match the names in the opcua server
    public void RenameLinks(string modelName)
    {
        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }

        string path = path_config + "/OpcUaAxisNames.json";
        if (!File.Exists(path))
        {
            Debug.LogError("Configuration file not found: " + path);
            return;
        }

        string jsonContent = File.ReadAllText(path);
        LinkMappings linkMappings = JsonUtility.FromJson<LinkMappings>(jsonContent);

        if (linkMappings == null || linkMappings.mappings == null || linkMappings.mappings.Length == 0)
        {
            Debug.LogError("Failed to load link mappings or no mappings defined.");
            return;
        }

        Dictionary<string, string> mappingDict = new Dictionary<string, string>();
        foreach (var mapping in linkMappings.mappings)
        {
            if (!string.IsNullOrEmpty(mapping.original) && !string.IsNullOrEmpty(mapping.newName))
            {
                mappingDict[mapping.original.ToLower()] = mapping.newName;
            }
        }

        RenameLinkRecursively(root.transform, mappingDict);

        Debug.Log("Link renaming finished.");
    }

    // Recursively rename links in a model
    private void RenameLinkRecursively(Transform current, Dictionary<string, string> mappingDict)
    {
        if (mappingDict.TryGetValue(current.name.ToLower(), out string newName) && current.parent.name != "unnamed")
        {
            Debug.Log($"Renaming link: {current.name} -> {newName}");
            current.name = newName;
        }

        foreach (Transform child in current)
        {
            RenameLinkRecursively(child, mappingDict);
        }
    }
    
}

[System.Serializable]
public class LinkMapping
{
    public string original;
    public string newName;
}

[System.Serializable]
public class LinkMappings
{
    public LinkMapping[] mappings;
}

public static class TransformExtensions
{
    public static Transform FindChildByPattern(this Transform parent, string pattern)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(pattern))
                return child;

            Transform found = child.FindChildByPattern(pattern);
            if (found != null)
                return found;
        }
        return null;
    }
}
