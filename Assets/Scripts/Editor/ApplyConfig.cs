using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class ApplyConfiguration
{
    public void ApplyConfig(string modelName)
    {
        string path = Application.dataPath + "/RobotConfig.json";
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
            Transform target = FindChildRecursive(root.transform, config.componentName);
            if (target == null)
            {
                Debug.LogWarning($"Component {config.componentName} not found in the new model.");
                continue;
            }

            // Attach scripts
            foreach (var scriptName in config.attachedScripts)
            {
                Type scriptType = Type.GetType(scriptName);
                if (scriptType != null && target.GetComponent(scriptType) == null)
                    target.gameObject.AddComponent(scriptType);
            }

            // Update ArticulationBody settings
            if (config.articulationBodyConfig != null)
            {
                var articulationBody = target.GetComponent<ArticulationBody>();
                if (articulationBody == null)
                {
                    Debug.LogWarning($"ArticulationBody not found on {config.componentName}. Skipping update.");
                    continue;
                }

                ApplyArticulationBodyConfig(articulationBody, config.articulationBodyConfig);
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }

    private void ApplyArticulationBodyConfig(ArticulationBody articulationBody, ArticulationBodyConfig config)
    {
        articulationBody.collisionDetectionMode = (CollisionDetectionMode)Enum.Parse(typeof(CollisionDetectionMode), config.collisionDetection);
        articulationBody.linearDamping = config.linearDamping;
        articulationBody.angularDamping = config.angularDamping;
        articulationBody.jointFriction = config.jointFriction;
        articulationBody.mass = config.mass;
        articulationBody.useGravity = config.useGravity;

        ApplyDrive(articulationBody, config.xDrive);
    }

    private void ApplyDrive(ArticulationBody articulationBody, ArticulationDriveConfig driveConfig)
    {
        var drive = new ArticulationDrive
        {
            driveType = (ArticulationDriveType)Enum.Parse(typeof(ArticulationDriveType), driveConfig.driveType),
            lowerLimit = driveConfig.lowerLimit,
            upperLimit = driveConfig.upperLimit,
            stiffness = driveConfig.stiffness,
            damping = driveConfig.damping,
            forceLimit = driveConfig.forceLimit,
            targetVelocity = driveConfig.targetVelocity
        };

        articulationBody.xDrive = drive;
    }

}
