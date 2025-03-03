using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter;
using ROS2;

public class SpawnGameObject : MonoBehaviour
{
    public float[] targetPosition;
    public float[] targetRotation;
    public string cadDataPath;

    private Mesh[] meshes;

    void Start()
    {
        // --- 1) If your position from ROS is in a coordinate system 
        //     that differs from Unity, apply your Ros2Unity() call.
        //     Otherwise, just assign directly. 
        //
        // Instead of "transform.localPosition = transform.localPosition += ...",
        // simply set the localPosition:
        Vector3 unityPosition = new Vector3(targetPosition[0], targetPosition[1], targetPosition[2]);
        // If needed, do: unityPosition = unityPosition.Ros2Unity();
        transform.localPosition = unityPosition.Ros2Unity();

        // --- 2) Same for rotation:
        Quaternion unityRotation = new Quaternion(
            targetRotation[0],
            targetRotation[1],
            targetRotation[2],
            targetRotation[3]
        );
        // If needed, do: unityRotation = unityRotation.Ros2Unity();
        transform.localRotation = unityRotation;

        // --- 3) Import your mesh data
        meshes = GetMeshFromSTL(cadDataPath);

        // Create sub-GameObjects (one mesh per GO)
        for (int i = 0; i < meshes.Length; i++)
        {
            CreateMeshPartGameObject(meshes[i], name + $"_{i}");
        }
    }

    private Mesh[] GetMeshFromSTL(string stlPath)
    {
        Mesh[] importedMeshes = StlImporter.ImportMesh(stlPath);

        // If you need to flip from ROS to Unity at the geometry level as well,
        // you can uncomment the transformation steps below.
        // Be sure your final coordinate frames are consistent.

        /*
        foreach (Mesh m in importedMeshes)
        {
            Vector3[] v = m.vertices;
            Vector3[] n = m.normals;

            // Transform all vertices/normals from ROS to Unity
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = v[i].Ros2Unity();
                n[i] = n[i].Ros2Unity();
            }
            m.vertices = v;
            m.normals = n;

            // Reverse triangles if necessary
            int[] t = m.triangles;
            System.Array.Reverse(t);
            m.triangles = t;
        }
        */
        return importedMeshes;
    }

    private void CreateMeshPartGameObject(Mesh mesh, string partName)
    {
        // This child object holds one chunk of the mesh
        GameObject spawnedPart = new GameObject(partName);
        spawnedPart.transform.SetParent(transform, false);

        // We match the parent's local transform by default 
        // (because we used 'false' in SetParent).

        // Add renderer and filter to hold the mesh
        var mf = spawnedPart.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mechRenderer = spawnedPart.AddComponent<MeshRenderer>();

        Material mat = new Material(Shader.Find("Standard"));

        // 2) Set the material color; for example, make it red:
        // mat.color = Color.red;
        // or choose any color e.g. new Color(r, g, b, a)
        mat.color = new Color(0.1f, 0.5f, 0.8f, 1.0f); 
    
        mechRenderer.material = mat;
    }
}
