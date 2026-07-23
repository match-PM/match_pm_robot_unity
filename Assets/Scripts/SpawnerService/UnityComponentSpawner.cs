using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;
using Unity.Robotics.UrdfImporter;
using assembly_manager_interfaces.msg;

public class UnityComponentSpawner : MonoBehaviour
{

    private string ObjectScene = "assembly_manager/scene";
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<ObjectScene> objectSceneSub;
    private List<assembly_manager_interfaces.msg.Object> object_list = new List<assembly_manager_interfaces.msg.Object>();
    private bool scene_has_changed = false;
    public bool withRefFrame = false;

    // Start is called before the first frame update
    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        if (ros2Unity == null)
        {
            Debug.LogError("ROS2UnityComponent not found on this GameObject. Please add it in the Inspector.");
            return;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("Unity_Component_Spawner");
            objectSceneSub = ros2Node.CreateSubscription<ObjectScene>(
                ObjectScene,
                ObjectSceneCallback
            );
            Debug.Log($"Subscribed to {ObjectScene} and ready to receive objects.");
        }

        if (scene_has_changed)
        {
            Debug.Log("Scene has changed, applying objects to Unity.");
            ClearScene(); // Clear the scene before applying new objects
            if (ApplyObjectsToUnity())
            {
                Debug.Log("Objects applied successfully.");
            }
            else
            {
                Debug.LogError("Failed to apply objects to Unity.");
            }
            scene_has_changed = false; // Reset the flag after processing
        }
    }

    private void ObjectSceneCallback(ObjectScene msg)
    {
        if (CheckSceneHasChanged(msg.Objects_in_scene))
        {
            object_list = new List<assembly_manager_interfaces.msg.Object>(msg.Objects_in_scene);
            scene_has_changed = true;
        }
        else
        {
            // Scene has not changed, do nothing
            scene_has_changed = false;
        }
    }

    private bool ApplyObjectsToUnity()
    {
        // Keep track of all spawned objects by name for easy parenting
        Dictionary<string, GameObject> spawnedDict = new Dictionary<string, GameObject>();
        // Keep track of all reference frames created in THIS rebuild by their (fully
        // qualified) frame name. Parent resolution below must use these frames, never a
        // global GameObject.Find: during a rebuild the previous (being-destroyed) objects
        // and their identically-named frames are still alive, and Find could bind a new
        // object to a dying frame, causing it to be destroyed together with the old parent.
        Dictionary<string, GameObject> frameDict = new Dictionary<string, GameObject>();

        // First pass: create all GameObjects
        foreach (var obj in object_list)
        {
            var spawnedGameObject = new GameObject(obj.Obj_name);

            // (Optional: Add stuff here like components)
            var sgo = spawnedGameObject.AddComponent<SpawnGameObject>();
            sgo.color = obj.Apperance_color;
            float[] translation = new float[3];
            translation[0] = (float)obj.Obj_pose.Position.X;
            translation[1] = (float)obj.Obj_pose.Position.Y;
            translation[2] = (float)obj.Obj_pose.Position.Z;
            sgo.targetPosition = translation;

            float[] rotation = new float[4];
            rotation[0] = (float)obj.Obj_pose.Orientation.X;
            rotation[1] = (float)obj.Obj_pose.Orientation.Y;
            rotation[2] = (float)obj.Obj_pose.Orientation.Z;
            rotation[3] = (float)obj.Obj_pose.Orientation.W;
            sgo.targetRotation = rotation;

            sgo.cadDataPath = obj.Cad_data;
            sgo.tag = "spawned";
            sgo.uuid = obj.Uuid;

            // Create a child GameObject for EVERY reference frame of this object.
            // Frame names are fully qualified (e.g. "<Obj>_Vision_Hole_Left"), so this
            // lets objects whose Parent_frame is another object's frame find their parent
            // in the hierarchy during the second pass below.
            var refFrames = obj.Ref_frames;
            foreach (var rf in refFrames)
            {
                // Skip if a frame with this name already exists under the object
                if (spawnedGameObject.transform.Find(rf.Frame_name) != null)
                {
                    Debug.Log($"[UnityComponentSpawner] Frame {rf.Frame_name} already exists, skipping creation.");
                    continue;
                }

                // Create the frame GameObject as a child of the object.
                // The frame pose is expressed relative to the object origin.
                GameObject frameObject = new GameObject(rf.Frame_name);
                frameObject.tag = "spawned";
                frameObject.transform.SetParent(spawnedGameObject.transform, false);

                // Apply the frame's local pose using the same ROS->Unity convention
                // as SpawnGameObject, so objects parented to this frame line up correctly.
                Vector3 framePos = new Vector3(
                    (float)rf.Pose.Position.X,
                    (float)rf.Pose.Position.Y,
                    (float)rf.Pose.Position.Z
                );
                frameObject.transform.localPosition = framePos.Ros2Unity();

                Quaternion frameRot = new Quaternion(
                    (float)rf.Pose.Orientation.X,
                    (float)rf.Pose.Orientation.Y,
                    (float)rf.Pose.Orientation.Z,
                    (float)rf.Pose.Orientation.W
                );
                frameObject.transform.localRotation = frameRot.Ros2Unity();

                // Register so objects parented to this frame resolve to THIS rebuild's frame.
                frameDict[rf.Frame_name] = frameObject;

                Debug.Log($"[UnityComponentSpawner] Created frame: {rf.Frame_name} under {obj.Obj_name}");
            }

            if (withRefFrame)
            {
                Instantiate(Resources.Load<GameObject>("Prefabs/RefFrame"), spawnedGameObject.transform);
            }
            // Debug.Log($"Spawning GameObject: {obj.Obj_name}, uuid: {sgo.uuid}");
            // Add to dictionary
            spawnedDict[obj.Obj_name] = spawnedGameObject;
        }

        // Second pass: set parent
        foreach (var obj in object_list)
        {
            if (!string.IsNullOrEmpty(obj.Parent_frame))
            {
                GameObject parentObj = null;

                // Resolve the parent frame against objects/frames created in THIS rebuild
                // first, so we never bind to a leftover object from the previous scene that
                // is about to be destroyed. Only fall back to a global scene search for
                // static frames (e.g. Gonio_*) that are not spawned by this component.
                if (!spawnedDict.TryGetValue(obj.Parent_frame, out parentObj) &&
                    !frameDict.TryGetValue(obj.Parent_frame, out parentObj))
                {
                    // Fallback: a static (non-spawned) frame already present in the scene
                    parentObj = GameObject.Find(obj.Parent_frame);
                }

                if (parentObj != null && parentObj.activeInHierarchy)
                {
                    Debug.Log($"Setting parent for {obj.Obj_name} to {parentObj.name}");
                    spawnedDict[obj.Obj_name].transform.SetParent(parentObj.transform, false);
                }
                else
                {
                    Debug.LogWarning($"Parent frame {obj.Parent_frame} for {obj.Obj_name} does not exist!");
                }
            }
        }

        return true;
    }

    private bool CheckSceneHasChanged(IList<assembly_manager_interfaces.msg.Object> msgObjectList)
    {
        if (object_list.Count != msgObjectList.Count)
        {
            return true;
        }
        for (int i = 0; i < object_list.Count; i++)
        {
            var a = object_list[i];
            var b = msgObjectList[i];

            if (a.Obj_name != b.Obj_name)
                return true;
            if (a.Parent_frame != b.Parent_frame)
                return true;
            if (a.Obj_pose.Position.X != b.Obj_pose.Position.X)
                return true;
            if (a.Obj_pose.Position.Y != b.Obj_pose.Position.Y)
                return true;
            if (a.Obj_pose.Position.Z != b.Obj_pose.Position.Z)
                return true;
            if (a.Obj_pose.Orientation.X != b.Obj_pose.Orientation.X)
                return true;
            if (a.Obj_pose.Orientation.Y != b.Obj_pose.Orientation.Y)
                return true;
            if (a.Obj_pose.Orientation.Z != b.Obj_pose.Orientation.Z)
                return true;
            if (a.Obj_pose.Orientation.W != b.Obj_pose.Orientation.W)
                return true;
        }
        return false;
    }
    
    private bool ClearScene()
    {
        // Find all GameObjects with the tag "spawned"
        GameObject[] spawnedObjects = GameObject.FindGameObjectsWithTag("spawned");

        // Destroy each spawned object
        foreach (GameObject obj in spawnedObjects)
        {
            Destroy(obj);
        }

        // Clear the object list
        // object_list.Clear();

        Debug.Log("Scene cleared of all spawned objects.");
        return true;
    }
}
