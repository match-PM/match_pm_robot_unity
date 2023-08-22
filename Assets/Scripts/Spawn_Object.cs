using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using spawnObjReq = spawn_object_interface.srv.SpawnObject_Request;
using spawnObjResp = spawn_object_interface.srv.SpawnObject_Response;
//using spawnObjectBoth = spawn_object_interface.srv.Spawn_Object;
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
        Debug.Log("ich bin hier: 1");
       
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {
            Debug.Log("ich bin hier: 2");
            
            if (ros2Node == null)
            {
                Debug.Log("ich bin hier: 3");
                
                ros2Node = ros2Unity.CreateNode("ROS2UnityTestTestTestService");
                
                Debug.Log("ich bin hier: 4");
                
                SpawnObjectService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/object_manager/spawn_object", spawnObject);
            }
        }
    }

    public spawn_object_interface.srv.SpawnObject_Response spawnObject(spawn_object_interface.srv.SpawnObject_Request msg)
    {
        Debug.Log("ich bin hier: " + msg);
        // Debug.Log("Spawning Object with name:" + msg.obj_name + " and parent frame: " + msg.parent_frame +
         //            ", at position: " + msg.translation + " and rotation: " + msg.rotation + ". CAD-Data: " + msg.cad_data);
        spawn_object_interface.srv.SpawnObject_Response response = new spawn_object_interface.srv.SpawnObject_Response();
        //response.success = true;
        return response;
    }
}
}