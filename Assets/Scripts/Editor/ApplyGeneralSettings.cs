using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Apply general settings to a new model
/// </summary>
/// 
public class ApplyGeneralSettings
{
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
