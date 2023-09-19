using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter;
using ROS2;

public class SpawnGameObject : MonoBehaviour
{
    public string objectName;
    public float[] targetPosition;
    public float[] targetRotation;
    public string cadDataPath;
    Mesh[] Meshes;    

    // Start is called before the first frame update
    void Start()
    {
        // Set internal parameter
        transform.localPosition = transform.localPosition += new Vector3(targetPosition[0], targetPosition[1], targetPosition[2]);
        transform.rotation = new Quaternion(targetRotation[0],targetRotation[1],targetRotation[2],targetRotation[3]).Ros2Unity(); // Rotate from ROS to Unity
        name = objectName;
        
        
        // create Meshes from STL-File
        Meshes = GetMeshFromSTL(cadDataPath);
        
        // create multiple GameObjects for each incomming Object becaus of maximum num of vertices
        for (int i = 0;i < Meshes.Length; i++)
        {
            createGameObject(Meshes[i], objectName + $"_{i}");
        }            
    }

    private Mesh[] GetMeshFromSTL(string stlPath)
    {
        //Debug.Log("Doing Meshes");
        Mesh[] ImportedMeshes = StlImporter.ImportMesh(stlPath);

        // If the coordinates need to be changed, simply uncomment.
        // Notice the transformation in Start()

        /*// transform coordinates from ROS to Unity
        foreach (Mesh m in ImportedMeshes)
        {
            Vector3[] v = m.vertices;
            Vector3[] n = m.normals;
            Vector3[] r = Left2Right(v);
            Vector3[] s = Left2Right(n);
            m.vertices = r;
            m.normals = s;            

            int[] t = m.triangles;
            System.Array.Reverse(t);
            m.triangles = t;
        }*/
        return ImportedMeshes;
    }

    // Creates the smaller GameObjects
    private void createGameObject(Mesh mesh, string partName)
    {
        // Instantiate new GameObject with this as parent frame
        GameObject spawnedSmallerObject = Instantiate(new GameObject(), transform, false);
        
        // Append "SpawnSmallerObject" script to GameObject
        spawnedSmallerObject.name = partName;
        var sso = spawnedSmallerObject.AddComponent<SpawnSmallerObject>();
        sso.objectName = partName;
        sso.meshPart = mesh;

        spawnedSmallerObject.AddComponent<MeshFilter>();
        spawnedSmallerObject.AddComponent<MeshRenderer>();    
    }

    // Helper to change coordinates from Ros to Unity
    // Look at "Transformations.cs in Ros2forUnity
    private static Vector3[] Left2Right(Vector3[] v)
    {
        Vector3[] r = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            r[i] = v[i].Ros2Unity();
        }
        return r;
    }
}
