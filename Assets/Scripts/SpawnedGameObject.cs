using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter;

public class SpawnedGameObject : MonoBehaviour
{

    public string objectName;
    public float[] targetPosition;
    public float[] targetRotation;
    public string cadDataPath;
    Mesh[] Meshes;
    

    // Start is called before the first frame update
    void Start()
    {
        transform.localPosition = transform.localPosition += new Vector3(targetPosition[0], targetPosition[1], targetPosition[2]);
        transform.rotation = new Quaternion(targetRotation[0],targetRotation[1],targetRotation[2],targetRotation[3]);
        name = objectName + $"{DateTime.Now.ToString()}";
        
        
        // create Meshes from STL-File
        Meshes = GetMeshFromSTL(cadDataPath);
        
        for (int i = 0;i<Meshes.Length; i++)
        {
            createGameObject(Meshes[i], objectName + $"_{i}");
        }    
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Mesh[] GetMeshFromSTL(string stlPath)
    {
        //Debug.Log("Doing Meshes");
        Mesh[] ImportedMeshes = StlImporter.ImportMesh(stlPath);
        return ImportedMeshes;
    }

    private void createGameObject(Mesh mesh, string partName)
    { 
        GameObject spawnedSmallerObject = Instantiate(new GameObject(), transform, false);

        var sso = spawnedSmallerObject.AddComponent<SpawnSmallerObject>();
        sso.objectName = partName;
        sso.meshPart = mesh;

        spawnedSmallerObject.AddComponent<MeshFilter>();
        spawnedSmallerObject.AddComponent<MeshRenderer>();        
    }
}
