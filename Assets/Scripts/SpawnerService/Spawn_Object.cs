using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;


using spawnObjReq = custom_service_interface.srv.SO_Request;
using spawnObjResp = custom_service_interface.srv.SO_Response;

/// <summary>
/// This script subscribes to the /SpawnObject service and creates an object at the given position and rotation.
/// The object has the following Attributes:
///     string obj_name
///     string parent_frame
///     float32[3] translation
///     float32[4] rotation
///     string cad_data
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
public class Spawn_Object : MonoBehaviour
{
    // Initialize everython for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<spawnObjReq, spawnObjResp> SpawnObjectService;

    private bool NewData = false;
    private custom_service_interface.srv.SO_Request recievedRequest;
    
    // Start is called before the first frame update
    void Start()
    {       
        // Open a node for communication         
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {                        
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnitySpawningService");

                try
                {
                    SpawnObjectService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/object_manager/so", spawnObject);///object_manager/spawn_object
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
    }

    // Callback function which is executed on incomming data
    public custom_service_interface.srv.SO_Response spawnObject(custom_service_interface.srv.SO_Request msg)
    {
        Debug.Log("About to spawn Object with name:" + msg.Obj_name + " and parent frame: " + msg.Parent_frame +
                  ", at position: " + string.Join(", ", msg.Translation) + " and rotation: " + string.Join(", ", msg.Rotation) + ". CAD-Data: " + msg.Cad_data);

        custom_service_interface.srv.SO_Response response = new custom_service_interface.srv.SO_Response();
       
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

        var foundParent = new GameObject().transform;

        // Find the object with the given parent name
        foreach ( ArticulationBody possibleParent in articulationBodies )
        {
            //Debug.Log(possibleParent.name);
            if (possibleParent.name == recievedRequest.Parent_frame)
            {                
                foundParent = possibleParent.transform;
            }
        }
        
        //Debug.Log("FoundParent: " + foundParent );
        // Instantiate new GameObject with the given parent frame
        GameObject spawnedGameObject = Instantiate(new GameObject(),foundParent, false);

        // Append "SpawndeGameObject" script to GameObject
        var sgo = spawnedGameObject.AddComponent<SpawnGameObject>();
        sgo.objectName = recievedRequest.Obj_name;
        sgo.targetPosition = recievedRequest.Translation;
        sgo.targetRotation = recievedRequest.Rotation;
        sgo.cadDataPath = recievedRequest.Cad_data;
        sgo.tag = "spawned"; //add tag for later recognition
    }
}
}