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
/// </summary>

//ros2 service call /object_manager/spawn_object spawn_object_interfaces/srv/SpawnObject "{obj_name: Siemens_UFC, parent_frame: Gonio_Right_Part_Origin , translation:[0.0,0.0,0.0], rotation:[0.0,0.0,0.0,1.0], cad_data: //home/pmlab/Downloads/Tool_MPG_10_Base.STL}"
//ros2 launch pm_robot_bringup pm_robot_sim_HW.launch.py 


namespace ROS2
{
public class Spawn_Object : MonoBehaviour
{
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<spawnObjReq, spawnObjResp> SpawnObjectService;
    //public GameObject spawnGameObject;

    private bool NewData = false;
    private custom_service_interface.srv.SO_Request recievedRequest;
    

    // Start is called before the first frame update
    void Start()
    {             
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {
                        
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityService");

                try
                {
                    SpawnObjectService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/object_manager/so", spawnObject);///object_manager/spawn_object
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
                //Debug.Log(SpawnObjectService.ToString());
            }
        }
    }

    public custom_service_interface.srv.SO_Response spawnObject(custom_service_interface.srv.SO_Request msg)
    {
        Debug.Log("Spawning Object with name:" + msg.Obj_name + " and parent frame: " + msg.Parent_frame +
                  ", at position: " + string.Join(", ", msg.Translation) + " and rotation: " + string.Join(", ", msg.Rotation) + ". CAD-Data: " + msg.Cad_data);
        custom_service_interface.srv.SO_Response response = new custom_service_interface.srv.SO_Response();
       
        NewData = true;
        recievedRequest = msg;

        response.Success = true;
        return response;
    }
    
    void Update()
    {
        if (NewData)
        {
            createGameObject();
            NewData = false;
        }   
    }

    private void createGameObject()
    { 
        ArticulationBody[] articulationBodies = GetComponentsInChildren<ArticulationBody>();

        var foundParent = new GameObject().transform;

        foreach ( ArticulationBody possibleParent in articulationBodies )
        {
            //Debug.Log(possibleParent.name);
            if (possibleParent.name == recievedRequest.Parent_frame)
            {                
                foundParent = possibleParent.transform;
            }
        }
        
        Debug.Log("FoundParent: " + foundParent );
        GameObject spawnedGameObject = Instantiate(new GameObject(),foundParent, false);

        var sgo = spawnedGameObject.AddComponent<SpawnedGameObject>();
        sgo.objectName = recievedRequest.Obj_name;
        sgo.targetPosition = recievedRequest.Translation;
        sgo.targetRotation = recievedRequest.Rotation;
        sgo.cadDataPath = recievedRequest.Cad_data;
    }
}
}