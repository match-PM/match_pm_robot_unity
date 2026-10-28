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

    [Header("Random spawn offset (placement variability)")]
    [Tooltip("Max position error along the part's own ROS axes, in metres. Drawn uniformly from [-max, +max].")]
    public Vector3 maxPositionOffset = new Vector3(0.0005f, 0.0005f, 0f);
    [Tooltip("Max rotation error (roll, pitch, yaw) about the part's own ROS axes, in degrees. Drawn uniformly from [-max, +max].")]
    public Vector3 maxRotationOffsetDeg = new Vector3(0f, 0f, 0.5f);

    // UUIDs seen in the previous scene message (null until the first one arrives).
    // Only touched on the ROS spin thread.
    private HashSet<string> lastSeenUuids = null;
    // UUIDs of the first scene message after play mode starts, handed to the main thread to
    // drop poses stored on disk for parts that left the scene while Unity was stopped.
    private HashSet<string> firstSceneUuids = null;
    // UUIDs that left the scene, handed from the ROS spin thread to the main thread.
    // Collected per message rather than per rebuild: a clear followed quickly by a
    // respawn can be merged into a single rebuild, and the removal must not be missed.
    private readonly HashSet<string> removedUuids = new HashSet<string>();

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

    // Parts move with their parents (gonio, gripper) without a rebuild, so the stored world
    // pose is refreshed once more when play mode stops.
    void OnApplicationQuit()
    {
        SpawnGameObject.StoreAllWorldPoses();
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

        // Read the flag before draining the removals: the callback queues removals before it
        // sets the flag, so every removal belonging to this rebuild is forgotten first and a
        // respawned part cannot keep its old pose.
        bool rebuild = scene_has_changed;

        var forgottenUuids = new HashSet<string>();
        lock (removedUuids)
        {
            if (firstSceneUuids != null)
            {
                SpawnGameObject.ForgetPosesExcept(firstSceneUuids);
                firstSceneUuids = null;
            }
            foreach (string uuid in removedUuids)
                SpawnGameObject.ForgetPose(uuid);
            forgottenUuids.UnionWith(removedUuids);
            removedUuids.Clear();
        }

        if (rebuild)
        {
            Debug.Log("Scene has changed, applying objects to Unity.");
            // Parts that stay in the scene keep their world pose across the rebuild.
            var previousWorldPoses = CaptureWorldPoses(forgottenUuids);
            ClearScene(); // Clear the scene before applying new objects
            if (ApplyObjectsToUnity(previousWorldPoses))
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
        TrackRemovedUuids(msg.Objects_in_scene);

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

    // Queues every UUID that was in the previous message but is missing from this one,
    // so its stored pose is deleted and a respawn draws a new placement error.
    private void TrackRemovedUuids(IList<assembly_manager_interfaces.msg.Object> msgObjectList)
    {
        var currentUuids = new HashSet<string>();
        foreach (var obj in msgObjectList)
            currentUuids.Add(obj.Uuid);

        lock (removedUuids)
        {
            if (lastSeenUuids == null)
            {
                firstSceneUuids = currentUuids;
            }
            else
            {
                foreach (string uuid in lastSeenUuids)
                {
                    if (!currentUuids.Contains(uuid))
                        removedUuids.Add(uuid);
                }
            }
        }
        lastSeenUuids = currentUuids;
    }

    // World pose of every spawned part by UUID, taken before the scene is torn down. Parts
    // that were removed from the scene (and possibly respawned) are left out, so a respawn
    // is placed with a new placement error.
    private Dictionary<string, UnityEngine.Pose> CaptureWorldPoses(HashSet<string> forgottenUuids)
    {
        var worldPoses = new Dictionary<string, UnityEngine.Pose>();
        foreach (var sgo in FindObjectsOfType<SpawnGameObject>())
        {
            if (string.IsNullOrEmpty(sgo.uuid) || forgottenUuids.Contains(sgo.uuid))
                continue;
            worldPoses[sgo.uuid] = new UnityEngine.Pose(sgo.transform.position, sgo.transform.rotation);
        }
        return worldPoses;
    }

    // Number of spawned parts above this one in the hierarchy.
    private static int SpawnDepth(Transform t)
    {
        int depth = 0;
        for (Transform p = t.parent; p != null; p = p.parent)
        {
            if (p.GetComponent<SpawnGameObject>() != null)
                depth++;
        }
        return depth;
    }

    private bool ApplyObjectsToUnity(Dictionary<string, UnityEngine.Pose> previousWorldPoses)
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
            sgo.parentFrame = obj.Parent_frame;
            sgo.maxPositionOffset = maxPositionOffset;
            sgo.maxRotationOffsetDeg = maxRotationOffsetDeg;

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

        // Third pass: set poses, parents before children, so a part kept at its previous
        // world pose is placed relative to its parent's final pose.
        var spawnedParts = new List<SpawnGameObject>();
        foreach (var go in spawnedDict.Values)
            spawnedParts.Add(go.GetComponent<SpawnGameObject>());
        spawnedParts.Sort((a, b) => SpawnDepth(a.transform).CompareTo(SpawnDepth(b.transform)));
        foreach (var sgo in spawnedParts)
            sgo.ApplyPose(previousWorldPoses);

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

            // The reference frames have to be compared as well. An object usually enters the
            // scene before its frames exist (glue points are added afterwards), and the object
            // itself does not change when that happens. Without this check the rebuild is never
            // triggered, so the frames only ever appear after a play mode restart — and
            // anything that hangs off them (adhesive dots) never shows up at all.
            if (RefFramesHaveChanged(a.Ref_frames, b.Ref_frames))
                return true;
        }
        return false;
    }

    // True if the two reference frame lists differ in count, name or pose.
    private bool RefFramesHaveChanged(IList<assembly_manager_interfaces.msg.RefFrame> oldFrames,
                                      IList<assembly_manager_interfaces.msg.RefFrame> newFrames)
    {
        if (oldFrames.Count != newFrames.Count)
            return true;

        for (int i = 0; i < oldFrames.Count; i++)
        {
            var a = oldFrames[i];
            var b = newFrames[i];

            if (a.Frame_name != b.Frame_name)
                return true;
            if (a.Pose.Position.X != b.Pose.Position.X)
                return true;
            if (a.Pose.Position.Y != b.Pose.Position.Y)
                return true;
            if (a.Pose.Position.Z != b.Pose.Position.Z)
                return true;
            if (a.Pose.Orientation.X != b.Pose.Orientation.X)
                return true;
            if (a.Pose.Orientation.Y != b.Pose.Orientation.Y)
                return true;
            if (a.Pose.Orientation.Z != b.Pose.Orientation.Z)
                return true;
            if (a.Pose.Orientation.W != b.Pose.Orientation.W)
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
