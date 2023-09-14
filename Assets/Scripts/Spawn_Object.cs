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
                    SpawnObjectService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/object_manager/so", spawnObject);///object_manager/spawn_object

                    Debug.Log("ich bin hier: 4.5");
                }
                catch(Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
                Debug.Log("ich bin hier: 5");
                Debug.Log(SpawnObjectService.ToString());
                Debug.Log("ich bin hier: 6");
                
            }
        }
    }

    public custom_service_interface.srv.SO_Response spawnObject(custom_service_interface.srv.SO_Request msg)
    {
        Debug.Log("ich bin hier: " + msg);
        Debug.Log("Spawning Object with name:" + msg.Obj_name + " and parent frame: " + msg.Parent_frame +
                    ", at position: " + msg.Translation + " and rotation: " + msg.Rotation + ". CAD-Data: " + msg.Cad_data);
        custom_service_interface.srv.SO_Response response = new custom_service_interface.srv.SO_Response();
        response.Success = true;
        return response;
    }
}
}