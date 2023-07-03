using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Robotics.UrdfImporter;


/// <summary>
/// This script subscribes to the /joint_states topic and publishes the values to the corresponding Axis.
/// Each axis has an Articulation Body which is used to control the axis. The joint_state valus are published to the
/// target variable of the Articulation Body.
/// </summary>

namespace ROS2
{
public class Joint_States_To_Axis : MonoBehaviour
{
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<sensor_msgs.msg.JointState> joint_sub;

    private ArticulationBody[] articulationChain;

    String urdfJointName;

    sensor_msgs.msg.JointState joint_state_msg;

    private Dictionary<String, ArticulationBody> UnityJoints = new Dictionary<String, ArticulationBody>();


    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        // Get the reference to the parent GameObject
        GameObject parentObject = gameObject; 

        articulationChain = this.GetComponentsInChildren<ArticulationBody>();

        // Save all ArticulationBodys with the corresponding Joint Name
        foreach (ArticulationBody joint in articulationChain)
        {
            if (joint.gameObject.GetComponent<UrdfJointPrismatic>() != null)
            {
                urdfJointName = joint.gameObject.GetComponent<UrdfJointPrismatic>().jointName;
                UnityJoints.Add(urdfJointName, joint);
                
            }
            else if (joint.gameObject.GetComponent<UrdfJointRevolute>() != null)
            {
                urdfJointName = joint.gameObject.GetComponent<UrdfJointRevolute>().jointName;
                UnityJoints.Add(urdfJointName, joint);
            }
            
        }

    }

    void Update()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("ROS2UnityListenerNode");
            joint_sub = ros2Node.CreateSubscription<sensor_msgs.msg.JointState>(
              "/joint_states", msg => publishPositionToAxis(msg));


        }

        // Update the joint positions
        if (joint_state_msg != null)
        {
            // Find the corresponding name of the Joint in Unity and the Joint Name in the topic /joint_states
            foreach (string jointName in joint_state_msg.Name)
            {
                int jointIndex = Array.IndexOf(joint_state_msg.Name, jointName);
                
                foreach (KeyValuePair<String, ArticulationBody> kvp in UnityJoints)
                {
                    if (kvp.Key == jointName)
                    {
                        ArticulationDrive drive = kvp.Value.xDrive;
                        if (kvp.Value.jointType.ToString() == "RevoluteJoint")
                        {
                            
                            drive.target = Convert.ToSingle(joint_state_msg.Position[jointIndex]*180/Math.PI);
                        }
                        else
                        {
                            drive.target = Convert.ToSingle(joint_state_msg.Position[jointIndex]);
                        }

                        // Update the target of the ArticulationBody
                        kvp.Value.xDrive = drive;
                    } 

                }

            }

        }

       
    }

    void publishPositionToAxis(sensor_msgs.msg.JointState msg_)
    {
        joint_state_msg = msg_;
    }


}

}
