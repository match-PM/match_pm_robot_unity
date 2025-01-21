using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Unity.Robotics.UrdfImporter;

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

        // Get the script UrdfLinks of gameObject world and set base_link to true
        root.transform.FindChildByPattern("world").GetComponent<UrdfLink>().IsBaseLink = true;
        
    }

    // Deactivate all Collision Modells
    public void DeactivateCollisionModels(string modelName)
    {
        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }

        // Deactivate all MeshColliders
        MeshCollider[] meshColliders;

        meshColliders = Object.FindObjectsOfType<MeshCollider>();

        foreach (var meshCollider in meshColliders)
        {
            meshCollider.enabled = false;
        }

        Debug.Log("Collision Models deactivation finished.");
    }

    // Activate only the ArticulationBodys of the axis defined in the configuration file
    public void ActivateAxisArticulationBodies(string modelName)
    {
        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }

        // Deactivate all ArticulationBodies
        ArticulationBody[] articulationBodies;

        articulationBodies = Object.FindObjectsOfType<ArticulationBody>();

        foreach (var articulationBody in articulationBodies)
        {
            if (articulationBody.name == "base_link_empthy")
            {
                continue;
            }
            articulationBody.enabled = false;
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

        HashSet<string> axisNames = new HashSet<string>();
        foreach (var mapping in linkMappings.mappings)
        {
            if (!string.IsNullOrEmpty(mapping.newName))
            {
                axisNames.Add(mapping.newName.ToLower());
            }
        }

        ActivateAxisArticulationBodiesRecursively(root.transform, axisNames);

        Debug.Log("Axis ArticulationBodies activation finished.");
    }

    // Recursively activate only the ArticulationBodys of the axis defined in the configuration file
    private void ActivateAxisArticulationBodiesRecursively(Transform current, HashSet<string> axisNames)
    {
        ArticulationBody articulationBody = current.GetComponent<ArticulationBody>();
        if (articulationBody != null && axisNames.Contains(current.name.ToLower()))
        {
            Debug.Log($"Activating ArticulationBody: {current.name}");
            articulationBody.enabled = true;
            Debug.Log($"Activating ArticulationBody: {articulationBody.transform.parent} parent");
            articulationBody.transform.parent.GetComponent<ArticulationBody>().enabled = true;
        }

        foreach (Transform child in current)
        {
            ActivateAxisArticulationBodiesRecursively(child, axisNames);
        }
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
