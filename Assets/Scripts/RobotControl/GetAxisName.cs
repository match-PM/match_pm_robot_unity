using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.UrdfImporter.Editor;

public class GetAxisName : MonoBehaviour
{
    private ConfigurableJoint configurableJoint;

    //private UrdfJoint urdfJoint;
    
    // Start is called before the first frame update
    void Start()
    {
        UrdfJointPrismatic urdfJoint = GetComponent<UrdfJointPrismatic>();

        Debug.Log(urdfJoint.jointName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
