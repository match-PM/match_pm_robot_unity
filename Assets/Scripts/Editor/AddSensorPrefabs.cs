using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Add sensor prefabs to the model
/// </summary>
/// 
public class AddSensorPrefabs
{
    public void AddSensors(string modelName)
    {
        GameObject root = GameObject.Find(modelName);
        if (root == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }

        

    }

    public void SetArticulationBodiesActive(bool activate, Transform parent = null)
    {
        ArticulationBody[] articulationBodies;

        if (parent != null)
        {
            // Get all ArticulationBody components in the specified parent
            articulationBodies = parent.GetComponentsInChildren<ArticulationBody>(true);
        }
        else
        {
            // Get all ArticulationBody components in the entire scene
            articulationBodies = Object.FindObjectsOfType<ArticulationBody>();
        }

        // Activate or deactivate each ArticulationBody
        foreach (var articulationBody in articulationBodies)
        {
            articulationBody.enabled = activate;
        }

        Debug.Log($"{articulationBodies.Length} ArticulationBody components {(activate ? "activated" : "deactivated")}");
    }

}