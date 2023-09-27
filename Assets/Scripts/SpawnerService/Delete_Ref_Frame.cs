using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using deleteRefReq = spawn_object_interfaces.srv.DeleteRefFrame_Request;
using deleteRefResp = spawn_object_interfaces.srv.DeleteRefFrame_Response;

/// <summary>
/// This script subscribes to the /ChangeParentFrame service and changes the parent frame of an object with the given name.
/// The service has the following attributes:
///     string frame_name
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
public class Delete_Ref_Frame : MonoBehaviour
{
    // Initialize everythin for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<deleteRefReq, deleteRefResp> DeleteRefService;

    private bool NewData = false;
    private spawn_object_interfaces.srv.DeleteRefFrame_Request recievedRequest;
    
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
                ros2Node = ros2Unity.CreateNode("ROS2UnityDeleteRefFrameService");

                try
                {
                    DeleteRefService = ros2Node.CreateService<deleteRefReq, deleteRefResp>("/object_manager/drf", DeleteRefFrame);
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
        
    }

     // Callback function which is executed on incomming data
    public spawn_object_interfaces.srv.DeleteRefFrame_Response DeleteRefFrame(spawn_object_interfaces.srv.DeleteRefFrame_Request msg)
    {
        Debug.Log("About to delete Ref Frame with frame name: " + msg.Frame_name);  

        spawn_object_interfaces.srv.DeleteRefFrame_Response response = new spawn_object_interfaces.srv.DeleteRefFrame_Response();
       
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
            deleteGameObject();
            NewData = false;
        }   
    }

        // Main function the gets the job done
    private void deleteGameObject()
    {
        // Find all spawned object by tag
        GameObject[] GameObjects = GameObject.FindGameObjectsWithTag("RefFrame");

        //delete object with same name as given
        foreach(GameObject Object in GameObjects)
        {
            if (recievedRequest.Frame_name == Object.name)
            {
                Destroy(Object);
                Info.removeFromFrameNamesList(recievedRequest.Frame_name); // Remove from namelist
            }
        }
    }
}
}