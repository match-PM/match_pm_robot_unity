using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;


using spawnObjReq = assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Request;
using spawnObjResp = assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Response;
using rcl_interfaces.msg;

/// <summary>
/// This script subscribes to the /SpawnObjectUnity service and creates an object at the given position and rotation.
/// The object has the following Attributes:
///     string obj_name
///     string parent_frame
///     Vector translation
///     Quaternion rotation
///     string cad_data
///     ---
///     bool success
///
/// 
/// ros2 service call /object_manager/so assembly_manager_interfaces_unity/srv/SpawnObjectUnity "{obj_name: my_object_1, parent_frame: RobotAxisY, translation: {x: 0.0, y: 0.0, z: 0.0}, rotation: {x: 0.0, y: 0.0, z: 0.0, w: 0.0}, cad_data: //home/match-mover/Downloads/UFC.STL}"}
/// Step 1:
///         spwaw an object:
///             ros2 service call /object_manager/so assembly_manager_interfaces_unity/srv/SpawnObjectUnity "{obj_name: my_object_1, parent_frame: Y_Axis , translation:[0.0,0.0,0.0], rotation:[0.0,0.0,0.0,0.0], cad_data: //home/pmlab/Downloads/Tool_MPG_10_Base.STL}"
/// Step 2:
///         change the parent_frame:
///             ros2 service call /object_manager/cp assembly_manager_interfaces_unity/srv/ChangeParentFrame "{obj_name: my_object_1, parent_frame: X_Axis}"
/// Step 3:
///         delete the object:
///             ros2 service call /object_manager/do assembly_manager_interfaces_unity/srv/DestroyObject "{obj_name: my_object_1}"
/// Step 4:
///         create Ref frame:
///             ros2 service call object_manager/crf assembly_manager_interfaces_unity/srv/CreateRefFrame "{frame_name: Ref_Frame_1, parent_frame: X_Axis , pose:{position: {x: -0.035, y: -0.02166, z: 0.00235}, orientation: {x: 0.0, y: 0.0, z: 0.0, w: 1.0}}}"
/// Step 5:
///         delete Ref frame:
///             ros2 service call object_manager/drf assembly_manager_interfaces_unity/srv/DeleteRefFrame "{frame_name: Ref_Frame_1}"
/// Step 6:
///         disable object collision:
///             ros2 service call object_manager/doc assembly_manager_interfaces_unity/srv/DisableObjCollision "{obj_name: my_object_1, link: Y_Axis}"
/// Step 7:
///         get informations:
///             ros2 service call object_manager/gi assembly_manager_interfaces_unity/srv/GetInfo
/// Step 8:
///         modify pose:
///             ros2 service call object_manager/mp assembly_manager_interfaces_unity/srv/ModifyPose "{frame_name: Ref_Frame_1, rel_pose:{position: {x: -0.035, y: -0.02166, z: 0.00235}, orientation: {x: 0.0, y: 0.0, z: 0.0, w: 1.0}}}"
///
/// </summary>

namespace ROS2
{
    public class Spawn_Object : MonoBehaviour
    {
        // Initialize everythin for service communication
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private IService<spawnObjReq, spawnObjResp> SpawnObjectUnityService;

        private bool NewData = false;
        private assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Request recievedRequest;

        private Hold_Info Info;

        public bool withRefFrame = false;

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
                        SpawnObjectUnityService = ros2Node.CreateService<spawnObjReq, spawnObjResp>("/assembly_manager_unity/spawn_object", SpawnObjectUnity);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.InnerException);
                    }
                }
            }
        }

        // Callback function which is executed on incomming data
        public assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Response SpawnObjectUnity(assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Request msg)
        {
            Debug.Log(msg.Cad_data);
            Debug.Log("About to spawn Object with name: " + msg.Obj_name + " and parent frame: " + msg.Parent_frame +
                      ", at position: " + string.Join(", ", msg.Translation) + " and rotation: " + string.Join(", ", msg.Rotation) + ". CAD-Data: " + msg.Cad_data);

            assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Response response = new assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Response();

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
            foreach (ArticulationBody possibleParent in articulationBodies)
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

            if (withRefFrame)
            {
                Instantiate(Resources.Load<GameObject>("Prefabs/RefFrame"), spawnedGameObject.transform);
            }
            
            // Append "SpawndeGameObject" script to GameObject
            var sgo = spawnedGameObject.AddComponent<SpawnGameObject>();
            float[] translation = new float[3];
            // Unpack Vector3 into translation List
            translation[0] = (float)recievedRequest.Translation.X;
            translation[1] = (float)recievedRequest.Translation.Y;
            translation[2] = (float)recievedRequest.Translation.Z;
            sgo.targetPosition = translation;

            // Unpack Quaterion into rotation Vector
            float[] rotation = new float[4];
            rotation[0] = (float)recievedRequest.Rotation.X;
            rotation[1] = (float)recievedRequest.Rotation.Y;
            rotation[2] = (float)recievedRequest.Rotation.Z;
            rotation[3] = (float)recievedRequest.Rotation.W;
            sgo.targetRotation = rotation;

            sgo.cadDataPath = recievedRequest.Cad_data;
            sgo.tag = "spawned";
            //add tag for later recognition
            // Info.addToSpawnNamesList(recievedRequest.Obj_name); // Add to namelist
        }
    }
}
