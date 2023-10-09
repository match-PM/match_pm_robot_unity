using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;
using Unity.Robotics.UrdfImporter;

using createRefReq = spawn_object_interfaces.srv.CreateRefFrame_Request;
using createRefResp = spawn_object_interfaces.srv.CreateRefFrame_Response;

/// <summary>
/// This script subscribes to the /ChangeParentFrame service and changes the parent frame of an object with the given name.
/// The service has the following attributes:
///     string frame_name
///     string parent_frame
///     geometry_msgs/Pose pose
///     ---
///     bool success
///
/// Step 1:
///         spwaw an object:
///             ros2 service call /object_manager/so spawn_object_interfaces/srv/SpawnObject "{obj_name: my_object_1, parent_frame: Y_Axis , translation:[0.0,0.0,0.0], rotation:[0.0,0.0,0.0,0.0], cad_data: //home/pmlab/Downloads/Tool_MPG_10_Base.STL}"
/// Step 2:
///         change the parent_frame:
///             ros2 service call /object_manager/cp spawn_object_interfaces/srv/ChangeParentFrame "{obj_name: my_object_1, parent_frame: X_Axis}"
/// Step 3:
///         delete the object:
///             ros2 service call /object_manager/do spawn_object_interfaces/srv/DestroyObject "{obj_name: my_object_1}"
/// Step 4:
///         create Ref frame:
///             ros2 service call object_manager/crf spawn_object_interfaces/srv/CreateRefFrame "{frame_name: Ref_Frame_1, parent_frame: X_Axis , pose:{position: {x: -0.035, y: -0.02166, z: 0.00235}, orientation: {x: 0.0, y: 0.0, z: 0.0, w: 1.0}}}"
/// Step 5:
///         delete Ref frame:
///             ros2 service call object_manager/drf spawn_object_interfaces/srv/DeleteRefFrame "{frame_name: Ref_Frame_1}"
/// Step 6:
///         disable object collision:
///             ros2 service call object_manager/doc spawn_object_interfaces/srv/DisableObjCollision "{obj_name: my_object_1, link: Y_Axis}"
/// Step 7:
///         get informations:
///             ros2 service call object_manager/gi spawn_object_interfaces/srv/GetInfo
/// Step 8:
///         modify pose:
///             ros2 service call object_manager/mp spawn_object_interfaces/srv/ModifyPose "{frame_name: Ref_Frame_1, rel_pose:{position: {x: -0.035, y: -0.02166, z: 0.00235}, orientation: {x: 0.0, y: 0.0, z: 0.0, w: 1.0}}}"
///
/// </summary>

namespace ROS2
{
public class Create_Ref_Frame : MonoBehaviour
{
    // Initialize everythin for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<createRefReq, createRefResp> CreateRefService;

    private bool NewData = false;
    private spawn_object_interfaces.srv.CreateRefFrame_Request recievedRequest;
    
    private Hold_Info Info;

    // Start is called before the first frame update
    void Start()
    {
        //Script holding Lists with the wanted Information
        Info = GetComponentInChildren<Hold_Info>();

        // Open a node for communication         
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {                        
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityCreateRefFrameService");

                try
                {
                    CreateRefService = ros2Node.CreateService<createRefReq, createRefResp>("/object_manager/crf", CreateRefFrame);
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
        
    }

     // Callback function which is executed on incomming data
    public spawn_object_interfaces.srv.CreateRefFrame_Response CreateRefFrame(spawn_object_interfaces.srv.CreateRefFrame_Request msg)
    {
        Debug.Log("About to create Ref Frame with frame name: " + msg.Frame_name + " and parent frame: " + msg.Parent_frame +
        " with pose position: x:" + msg.Pose.Position.X + ", y: " + msg.Pose.Position.Y + ", z: " + msg.Pose.Position.Z + 
        " and pose orientation: x:" + msg.Pose.Orientation.X + ", y: " + msg.Pose.Orientation.Y + ", z: " + msg.Pose.Orientation.Z + ", w: " + msg.Pose.Orientation.W);  

        spawn_object_interfaces.srv.CreateRefFrame_Response response = new spawn_object_interfaces.srv.CreateRefFrame_Response();
       
        NewData = true;
        recievedRequest = msg;

        response.Success = true;
        return response;
    }

    // Update is called once per frame
    void Update()
    {
        if (NewData)
        {
            createGameObject();
            NewData = false;
        }   
    }

        // Main function the gets the job done
    private void createGameObject()
    {
        // Get all possible parent objects/axes
        ArticulationBody[] articulationBodies = GetComponentsInChildren<ArticulationBody>();

        Vector3 relPos = new Vector3((float)recievedRequest.Pose.Position.X, (float)recievedRequest.Pose.Position.Y, (float)recievedRequest.Pose.Position.Z);
        Quaternion relQuat = new Quaternion((float)recievedRequest.Pose.Orientation.X, (float)recievedRequest.Pose.Orientation.Y, (float)recievedRequest.Pose.Orientation.Z, (float)recievedRequest.Pose.Orientation.W);


        GameObject newRefFrame = new GameObject(recievedRequest.Frame_name);
        Instantiate(Resources.Load<GameObject>("Prefabs/tf_RefFrame"), newRefFrame.transform);
        // Find the object with the given parent name
        foreach ( ArticulationBody possibleParent in articulationBodies )
        {
            //Debug.Log(possibleParent.name);
            if (possibleParent.name == recievedRequest.Parent_frame)
            {                
                newRefFrame.transform.parent = possibleParent.transform;
                newRefFrame.transform.position = possibleParent.transform.position;
                newRefFrame.transform.rotation = possibleParent.transform.rotation;
            }
        }

        // Apply relative position and rotation
        newRefFrame.transform.localPosition += relPos;
        newRefFrame.transform.rotation *= relQuat.Ros2Unity(); // Rotate from ROS to Unity
        newRefFrame.tag = "RefFrame"; //add tag for later recognition
        // newRefFrame.AddComponent<UrdfLink>();
        // newRefFrame.AddComponent<ArticulationBody>();
        // newRefFrame.AddComponent<UrdfInertial>();
        // newRefFrame.AddComponent<UrdfJointPrismatic>();
   
        Info.addToFrameNamesList(recievedRequest.Frame_name);   // Add to namelist
    }
}
}