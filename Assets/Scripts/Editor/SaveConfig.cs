using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Class to save a configuration of a model to a JSON file
/// Scripts and settings of ArticulationBody settings are saved for each component
/// </summary>
public class SaveConfiguration
{

    public void SaveConfig(string modelName, string configName)
    {
        //find the root object by modelName
        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }
        var components = new List<ComponentConfig>();
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        // components with the name 'Collisions' or 'Visuals' and their children are ignored
        foreach (var child in children)
        {
            // Skip GameObjects named "Collisions" or "Visuals" and their children
            if (IsIgnored(child))
                continue;
            var attachedScripts = new List<string>();

            // Collect script names
            foreach (var script in child.GetComponents<MonoBehaviour>())
            {
                if (script != null)
                {
                    attachedScripts.Add(script.GetType().Name);
                }
            }

            // Collect ArticulationBody configuration if available
            ArticulationBodyConfig articulationConfig = null;
            var articulationBody = child.GetComponent<ArticulationBody>();

            // Only save ArticulationBody settings if it's not a FixedJoint
            if (articulationBody != null)
            {
                articulationConfig = new ArticulationBodyConfig
                {
                    collisionDetection = articulationBody.collisionDetectionMode.ToString(),
                    linearDamping = articulationBody.linearDamping,
                    angularDamping = articulationBody.angularDamping,
                    jointFriction = articulationBody.jointFriction,
                    mass = articulationBody.mass,
                    useGravity = articulationBody.useGravity,
                    xDrive = ConvertDrive(articulationBody.xDrive)
                };
            }

            components.Add(new ComponentConfig
            {
                componentName = child.name,
                attachedScripts = attachedScripts.ToArray(),
                articulationBodyConfig = articulationConfig
            });
        }

        // Save to JSON
        ConfigData data = new ConfigData { components = components };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Application.dataPath + "/" + configName +".json", json);
        Debug.Log("Configuration (including ArticulationBody settings) saved to " + configName + ".json");
    }

    private ArticulationDriveConfig ConvertDrive(ArticulationDrive drive)
    {
        return new ArticulationDriveConfig
        {
            driveType = drive.driveType.ToString(), 
            lowerLimit = drive.lowerLimit,
            upperLimit = drive.upperLimit,
            stiffness = drive.stiffness,
            damping = drive.damping,
            forceLimit = drive.forceLimit,
            targetVelocity = drive.targetVelocity
        };
    }

    private bool IsIgnored(Transform transform)
    {
        if (transform.name == "Collisions" || transform.name == "Visuals")
            return true;

        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.name == "Collisions" || parent.name == "Visuals")
                return true;
            parent = parent.parent;
        }
        return false;
    }
}
