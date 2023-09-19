using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using changeObjReq = custom_service_interface.srv.CP_Request;
using changeObjResp = custom_service_interface.srv.CP_Response;

/// <summary>
/// This script subscribes to the /ChangeParentFrame service and changes the parent frame of an object with the given name.
/// The service has the following attributes:
///     string obj_name
///     string new_parent_frame
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
public class Change_Parent_Frame : MonoBehaviour
{
    // Initialize everython for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<changeObjReq, changeObjResp> ChangeParentFrameService;

    private bool NewData = false;
    private custom_service_interface.srv.CP_Request recievedRequest;

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
    public custom_service_interface.srv.CP_Response changeParentFrame(custom_service_interface.srv.CP_Request msg)
    {
        Debug.Log("abaout to change parent frame of Object with name:" + msg.Obj_name + " to new parent frame: " + msg.New_parent_frame);

        custom_service_interface.srv.CP_Response response = new custom_service_interface.srv.CP_Response();
       
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
            if (possibleNewParent.name == recievedRequest.New_parent_frame)
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