using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

public class SpawnSmallerObject : MonoBehaviour
{
    public string objectName;    
    public Mesh meshPart;
  
    public MeshFilter mf;
    public MeshRenderer mr;

    // Start is called before the first frame update
    void Start()
    {
        // Set internal parameter
        name = objectName;

        // Add Material from Resources folder to MeshRenderer
        mr = GetComponent<MeshRenderer>();
        var newMat = Resources.Load<Material>("Materials/green");
        mr.material = newMat;
        
        // Add mesh to MeshFilter
        mf = GetComponent<MeshFilter>();
        mf.mesh = meshPart;
    }
}