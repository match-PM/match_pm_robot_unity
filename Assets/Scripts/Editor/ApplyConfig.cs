using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System;
using UnityEditor;

/// <summary>
/// Class to apply a configuration to a new model.
/// Scripts and ArticulationBody settings are attached to the components in the new model.
/// </summary>
public class ApplyConfiguration
{
    public string path_config = Application.dataPath + "/PM_Robot/Configs/";

    public void ApplyConfig(string modelName, string configName)
    {
        string path = path_config + configName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogError("Configuration file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        ConfigData data = JsonUtility.FromJson<ConfigData>(json);

        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }

        foreach (var config in data.components)
        {
            Transform target = FindClosestMatchingChildRecursive(root.transform, config.componentName);
            if (target == null)
            {
                Debug.LogWarning($"Component {config.componentName} not found or no close match in the new model.");
                continue;
            }

            // Attach scripts
            foreach (var scriptName in config.attachedScripts)
            {
                // Check if the script is already attached
                if (target.GetComponent(scriptName) != null)
                {
                    Debug.LogWarning(target.name + $" Script {scriptName} already attached.");
                    continue;
                }
                
                Type scriptType = FindScriptType(scriptName);
                if (scriptType != null && target.GetComponent(scriptType) == null)
                {
                    target.gameObject.AddComponent(scriptType);
                }
                else if (scriptType == null)
                {
                    Debug.LogError(target.name + $" Script {scriptName} not found in Assets folder.");
                }
            }

            // Update ArticulationBody settings
            if (config.articulationBodyConfig != null)
            {
                var articulationBody = target.GetComponent<ArticulationBody>();
                if (articulationBody == null)
                {
                    //Debug.LogWarning($"ArticulationBody not found on {config.componentName}. Skipping update.");
                    continue;
                }

                ApplyArticulationBodyConfig(articulationBody, config.articulationBodyConfig);
            }
        }

        Debug.Log("Configuration applied successfully.");
    }

    private Type FindScriptType(string scriptName)
    {
        string[] guids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript", new[] { "Assets" });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script != null && script.name == scriptName)
            {
                return script.GetClass(); // Returns the Type of the script
            }
        }

        return null; // Script not found
    }

    private Transform FindClosestMatchingChildRecursive(Transform parent, string targetName, float similarityThreshold = 0.8f)
    {
        Transform bestMatch = null;
        float highestSimilarity = 0f;
        
        if(parent.name == targetName)
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            // Skip if the parent name is 'unnamed'
            if (child.parent.name == "unnamed" || child.name == "unnamed")
            {
                continue;
            }
            float similarity = CalculateNameSimilarity(child.name, targetName);
            if (similarity > highestSimilarity && similarity >= similarityThreshold)
            {
                highestSimilarity = similarity;
                bestMatch = child;
            }

            Transform result = FindClosestMatchingChildRecursive(child, targetName, similarityThreshold);
            if (result != null)
            {
                float childSimilarity = CalculateNameSimilarity(result.name, targetName);
                if (childSimilarity > highestSimilarity)
                {
                    highestSimilarity = childSimilarity;
                    bestMatch = result;
                }
            }
        }

        return bestMatch;
    }

    // Calculate similarity between two strings
    private float CalculateNameSimilarity(string name1, string name2)
    {
        // Example similarity calculation using Levenshtein distance
        int distance = LevenshteinDistance(name1.ToLower(), name2.ToLower());
        int maxLength = Mathf.Max(name1.Length, name2.Length);
        return 1.0f - (float)distance / maxLength;
    }

    // Levenshtein distance calculation
    // calculates the minimum number of single-character edits required to change one string into another
    private int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                if (a[i - 1] == b[j - 1])
                    dp[i, j] = dp[i - 1, j - 1];
                else
                    dp[i, j] = Mathf.Min(dp[i - 1, j] + 1, Mathf.Min(dp[i, j - 1] + 1, dp[i - 1, j - 1] + 1));
            }
        }

        return dp[a.Length, b.Length];
    }

    private void ApplyArticulationBodyConfig(ArticulationBody articulationBody, ArticulationBodyConfig config)
    {
        try
        {
            articulationBody.collisionDetectionMode = Enum.TryParse<CollisionDetectionMode>(
                config.collisionDetection, true, out var mode)
                ? mode
                : CollisionDetectionMode.Discrete; // Default fallback value
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse collision detection mode: {config.collisionDetection}. Error: {ex.Message}");
            articulationBody.collisionDetectionMode = CollisionDetectionMode.Discrete; // Default fallback value
        }

        articulationBody.linearDamping = config.linearDamping;
        articulationBody.angularDamping = config.angularDamping;
        articulationBody.jointFriction = config.jointFriction;
        articulationBody.mass = config.mass;
        articulationBody.useGravity = config.useGravity;

        ApplyDrive(articulationBody, config.xDrive);
    }
    private void ApplyDrive(ArticulationBody articulationBody, ArticulationDriveConfig driveConfig)
    {
        if (articulationBody.jointType == ArticulationJointType.FixedJoint)
        {
            return;
        }

        var drive = new ArticulationDrive();

        if (Enum.TryParse(driveConfig.driveType, true, out ArticulationDriveType driveType))
        {
            drive.driveType = driveType;
        }
        else
        {
            Debug.LogWarning(articulationBody.transform.name + $" Invalid drive type: {driveConfig.driveType} for. Defaulting to Target drive type.");
            drive.driveType = ArticulationDriveType.Target; // Default fallback value
        }

        drive.lowerLimit = driveConfig.lowerLimit;
        drive.upperLimit = driveConfig.upperLimit;
        drive.stiffness = driveConfig.stiffness;
        drive.damping = driveConfig.damping;
        drive.forceLimit = driveConfig.forceLimit;
        drive.targetVelocity = driveConfig.targetVelocity;

        articulationBody.xDrive = drive;
    }
}
