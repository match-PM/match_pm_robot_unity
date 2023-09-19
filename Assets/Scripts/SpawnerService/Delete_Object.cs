using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using deleteObjReq = custom_service_interface.srv.DO_Request;
using deleteObjResp = custom_service_interface.srv.DO_Response;

/// <summary>
/// This script subscribes to the /DeleteObject service and deletes an object with the given name.
/// The service has the following attributes:
///     string obj_name
///     ---
///     bool success
///
/// Step 1:
///         spwaw an object:
///             ros2 service call /object_manager/so custom_service_interface/srv/SO "{obj_name: my_object_1, parent_frame: Y_Axis , translation:[0.0,0.0,0.0], rotation:[0.0,0.0,0.0,0.0], cad_data: //home/pmlab/Downloads/Tool_MPG_10_Base.STL}"
/// Step 2:
///         change the parent_frame:
///             ros2 service call /object_manager/cp custom_service_interface/srv/CP "{obj_name: my_object_1, new_parent_frame: X_Axis}"
/// Step 3:
///         delete the object:
///             ros2 service call /object_manager/do custom_service_interface/srv/DO "{obj_name: my_object_1}"
///
/// </summary>

namespace ROS2
{
public class Delete_Object : MonoBehaviour
{
    // Initialize everython for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<deleteObjReq, deleteObjResp> DeleteObjectService;

    private bool NewData = false;
    private custom_service_interface.srv.DO_Request recievedRequest;

    // Start is called before the first frame update
    void Start()
    {
        // Open a node for communication
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {                        
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityDeletingService");

                try
                {
                    DeleteObjectService = ros2Node.CreateService<deleteObjReq, deleteObjResp>("/object_manager/do", deleteObject);
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
        
    }

    // Callback function which is executed on incomming data
    public custom_service_interface.srv.DO_Response deleteObject(custom_service_interface.srv.DO_Request msg)
    {
        Debug.Log("About to delete Object with name:" + msg.Obj_name);

        custom_service_interface.srv.DO_Response response = new custom_service_interface.srv.DO_Response();
       
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
        GameObject[] GameObjects = GameObject.FindGameObjectsWithTag("spawned");

        //delete object with same name as given
        foreach(GameObject Object in GameObjects)
        {
            if (recievedRequest.Obj_name == Object.name)
            {
                Destroy(Object);
            }
        }
    }
}
}
