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

}