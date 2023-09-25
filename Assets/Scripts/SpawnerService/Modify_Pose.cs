using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using modifyPoseReq = spawn_object_interfaces.srv.ModifyPose_Request;
using modifyPoseResp = spawn_object_interfaces.srv.ModifyPose_Response;

/// <summary>
/// This script subscribes to the /ChangeParentFrame service and changes the parent frame of an object with the given name.
/// The service has the following attributes:
///     string frame_name
///     geometry_msgs/Pose rel_pose
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
public class Modify_Pose : MonoBehaviour
{
    // Initialize everythin for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<modifyPoseReq, modifyPoseResp> ModifyPoseService;

    private bool NewData = false;
    private spawn_object_interfaces.srv.ModifyPose_Request recievedRequest;
    
    // Start is called before the first frame update
    void Start()
    {
        // Open a node for communication         
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {                        
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityModifyPoseService");

                try
                {
                    ModifyPoseService = ros2Node.CreateService<modifyPoseReq, modifyPoseResp>("/object_manager/mp", ModifyPose);
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
        
    }

     // Callback function which is executed on incomming data
    public spawn_object_interfaces.srv.ModifyPose_Response ModifyPose(spawn_object_interfaces.srv.ModifyPose_Request msg)
    {
        Debug.Log("About to modify the pose of frame name: " + msg.Frame_name + 
        " with relative pose position: x:" + msg.Rel_pose.Position.X + ", y: " + msg.Rel_pose.Position.Y + ", z: " + msg.Rel_pose.Position.Z + 
        " and relative pose orientation: x:" + msg.Rel_pose.Orientation.X + ", y: " + msg.Rel_pose.Orientation.Y + ", z: " + msg.Rel_pose.Orientation.Z + ", w: " + msg.Rel_pose.Orientation.W);

        spawn_object_interfaces.srv.ModifyPose_Response response = new spawn_object_interfaces.srv.ModifyPose_Response();
       
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
        // // Get all possible parent objects/axes
        // ArticulationBody[] articulationBodies = GetComponentsInChildren<ArticulationBody>();

        // var foundParent = new GameObject().transform;

        // // Find the object with the given parent name
        // foreach ( ArticulationBody possibleParent in articulationBodies )
        // {
        //     //Debug.Log(possibleParent.name);
        //     if (possibleParent.name == recievedRequest.Parent_frame)
        //     {                
        //         foundParent = possibleParent.transform;
        //     }
        // }
        
        // //Debug.Log("FoundParent: " + foundParent );
        // // Instantiate new GameObject with the given parent frame
        // GameObject spawnedGameObject = Instantiate(new GameObject(),foundParent, false);

        // // Append "SpawndeGameObject" script to GameObject
        // var sgo = spawnedGameObject.AddComponent<SpawnGameObject>();
        // sgo.objectName = recievedRequest.Obj_name;
        // sgo.targetPosition = recievedRequest.Translation;
        // sgo.targetRotation = recievedRequest.Rotation;
        // sgo.cadDataPath = recievedRequest.Cad_data;
        // sgo.tag = "spawned"; //add tag for later recognition
    }
}
}