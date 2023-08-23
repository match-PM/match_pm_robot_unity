using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

//using spawnObjReq = spawn_object_interfaces.msg.SpawnObject;
//using spawnObjResp = spawn_object_interfaces.msg.SpawnObject_Response;
//using spawnObjectBoth = spawn_object_interface.srv.Spawn_Object;

using spawnObjReq = customservices.msg.SpawnObjectRequest;
using spawnObjResp = customservices.msg.SpawnObjectResponse;


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

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("ich bin hier: 1 " + typeof(spawnObjReq));
       
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {
            Debug.Log("ich bin hier: 2");
            
            if (ros2Node == null)
            {
                Debug.Log("ich bin hier: 3");
                
                ros2Node = ros2Unity.CreateNode("ROS2UnityService");
                
                Debug.Log("ich bin hier: 4");
                
                try
                {
                    SpawnObjectService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/object_manager/spawn_object", spawnObject);
                    Debug.Log("ich bin hier: 4.5");
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
                Debug.Log("ich bin hier: 5");
                Debug.Log(SpawnObjectService.ToString());
                
            }
        }
    }

    public customservices.msg.SpawnObjectResponse spawnObject(customservices.msg.SpawnObjectRequest msg)
    {
        Debug.Log("ich bin hier: " + msg);
        // Debug.Log("Spawning Object with name:" + msg.obj_name + " and parent frame: " + msg.parent_frame +
         //            ", at position: " + msg.translation + " and rotation: " + msg.rotation + ". CAD-Data: " + msg.cad_data);
        customservices.msg.SpawnObjectResponse response = new customservices.msg.SpawnObjectResponse();
        //response.success = true;
        return response;
    }
}
}