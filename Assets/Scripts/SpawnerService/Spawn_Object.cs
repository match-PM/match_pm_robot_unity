using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;


using spawnObjReq = spawn_object_interfaces.srv.SpawnObject_Request;
using spawnObjResp = spawn_object_interfaces.srv.SpawnObject_Response;

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
public class Spawn_Object : MonoBehaviour
{
    // Initialize everythin for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<spawnObjReq, spawnObjResp> SpawnObjectService;

    private bool NewData = false;
    private spawn_object_interfaces.srv.SpawnObject_Request recievedRequest;

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
                ros2Node = ros2Unity.CreateNode("ROS2UnitySpawningService");

                try
                {
                    SpawnObjectService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/object_manager/so", spawnObject);
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }
    }

    // Callback function which is executed on incomming data
    public spawn_object_interfaces.srv.SpawnObject_Response spawnObject(spawn_object_interfaces.srv.SpawnObject_Request msg)
    {
        Debug.Log("About to spawn Object with name: " + msg.Obj_name + " and parent frame: " + msg.Parent_frame +
                  ", at position: " + string.Join(", ", msg.Translation) + " and rotation: " + string.Join(", ", msg.Rotation) + ". CAD-Data: " + msg.Cad_data);

        spawn_object_interfaces.srv.SpawnObject_Response response = new spawn_object_interfaces.srv.SpawnObject_Response();
       
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

        // New GameObject
        GameObject spawnedGameObject = new GameObject(recievedRequest.Obj_name);
        
        // Find the object with the given parent name
        foreach ( ArticulationBody possibleParent in articulationBodies )
        {
            //Debug.Log(possibleParent.name);
            if (possibleParent.name == recievedRequest.Parent_frame)
            {                
                spawnedGameObject.transform.parent = possibleParent.transform;
                spawnedGameObject.transform.position = possibleParent.transform.position;
                spawnedGameObject.transform.rotation = possibleParent.transform.rotation;
            }
        }
        
        // Instantiate new GameObject with the given parent frame

        // Append "SpawndeGameObject" script to GameObject
        var sgo = spawnedGameObject.AddComponent<SpawnGameObject>();
        sgo.targetPosition = recievedRequest.Translation;
        sgo.targetRotation = recievedRequest.Rotation;
        sgo.cadDataPath = recievedRequest.Cad_data;
        sgo.tag = "spawned"; //add tag for later recognition

        Info.addToSpawnNamesList(recievedRequest.Obj_name); // Add to namelist
    }
}
}