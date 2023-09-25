using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using changeObjReq = spawn_object_interfaces.srv.ChangeParentFrame_Request;
using changeObjResp = spawn_object_interfaces.srv.ChangeParentFrame_Response;

/// <summary>
/// This script subscribes to the /ChangeParentFrame service and changes the parent frame of an object with the given name.
/// The service has the following attributes:
///     string obj_name
///     string parent_frame
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
public class Change_Parent_Frame : MonoBehaviour
{
    // Initialize everython for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<changeObjReq, changeObjResp> ChangeParentFrameService;

    private bool NewData = false;
    private spawn_object_interfaces.srv.ChangeParentFrame_Request recievedRequest;

    // Start is called before the first frame update
    void Start()
    {
        // Open a node for communication
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {                        
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityChangeParentFrameService");

                try
                {
                    ChangeParentFrameService = ros2Node.CreateService<changeObjReq, changeObjResp>("/object_manager/cp", changeParentFrame);
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
        
    }

    // Callback function which is executed on incomming data
    public spawn_object_interfaces.srv.ChangeParentFrame_Response changeParentFrame(spawn_object_interfaces.srv.ChangeParentFrame_Request msg)
    {
        Debug.Log("about to change parent frame of Object with name:" + msg.Obj_name + " to new parent frame: " + msg.Parent_frame);

        spawn_object_interfaces.srv.ChangeParentFrame_Response response = new spawn_object_interfaces.srv.ChangeParentFrame_Response();
       
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
            changeParentFrameGameObject();
            NewData = false;
        }   
        
    }

    // Main function the gets the job done
    private void changeParentFrameGameObject()
    {
        // Find all spawned object by tag
        GameObject[] GameObjects = GameObject.FindGameObjectsWithTag("spawned");
        
        // Get all possible parent objects/axes
        ArticulationBody[] articulationBodies = GetComponentsInChildren<ArticulationBody>();
        
        var foundParent = new GameObject().transform;

        // Find the object with the given parent name
        foreach ( ArticulationBody possibleNewParent in articulationBodies )
        {
            //Debug.Log(possibleNewParent.name);
            if (possibleNewParent.name == recievedRequest.Parent_frame)
            {                
                foundParent = possibleNewParent.transform;
            }
        }

        // Set the new parent to the object with the given name
        foreach(GameObject gameObject in GameObjects)
        {
            if (gameObject.name == recievedRequest.Obj_name)
            {
                gameObject.transform.SetParent(foundParent);
            }
        }
    }
}
}