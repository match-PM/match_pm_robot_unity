using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ROS2;
using assembly_manager_interfaces.msg;

public class UnityComponentSpawner : MonoBehaviour
{

    private string ObjectScene = "assembly_manager/scene";
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<ObjectScene> objectSceneSub;
    private Dictionary<string, assembly_manager_interfaces.msg.Object> objectsByUuid = new Dictionary<string, assembly_manager_interfaces.msg.Object>();
    private Dictionary<string, GameObject> spawnedByUuid = new Dictionary<string, GameObject>();
    private List<assembly_manager_interfaces.msg.Object> pendingObjectList = null;
    private bool scene_has_changed = false;
    public bool withRefFrame = false;
    public GameObject adhesivePointPrefab;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        if (ros2Unity == null)
        {
            Debug.LogError("ROS2UnityComponent not found on this GameObject. Please add it in the Inspector.");
            return;
        }
    }

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

        if (scene_has_changed && pendingObjectList != null)
        {
            ApplySceneIncrementally(pendingObjectList);
            pendingObjectList = null;
            scene_has_changed = false;
        }
    }

    private void ObjectSceneCallback(ObjectScene msg)
    {
        var newList = new List<assembly_manager_interfaces.msg.Object>(msg.Objects_in_scene);
        if (CheckSceneHasChanged(newList))
        {
            pendingObjectList = newList;
            scene_has_changed = true;
        }
    }

    private void ApplySceneIncrementally(List<assembly_manager_interfaces.msg.Object> newObjectList)
    {
        var newByUuid = new Dictionary<string, assembly_manager_interfaces.msg.Object>();
        foreach (var obj in newObjectList)
            newByUuid[obj.Uuid] = obj;

        // Remove objects that no longer exist
        var removedUuids = objectsByUuid.Keys.Where(uuid => !newByUuid.ContainsKey(uuid)).ToList();
        foreach (var uuid in removedUuids)
        {
            if (spawnedByUuid.TryGetValue(uuid, out var go))
            {
                Debug.Log($"[UnityComponentSpawner] Removing object: {objectsByUuid[uuid].Obj_name}");
                Destroy(go);
                spawnedByUuid.Remove(uuid);
            }
            objectsByUuid.Remove(uuid);
        }

        // Add new objects or update existing ones
        // We need a name->GO dict for parenting (includes both new and existing)
        Dictionary<string, GameObject> spawnedByName = new Dictionary<string, GameObject>();
        foreach (var kvp in spawnedByUuid)
        {
            if (kvp.Value != null)
                spawnedByName[kvp.Value.name] = kvp.Value;
        }

        foreach (var obj in newObjectList)
        {
            if (spawnedByUuid.ContainsKey(obj.Uuid) && spawnedByUuid[obj.Uuid] != null)
            {
                // Object exists — check if it needs a full rebuild or just a property update
                var oldObj = objectsByUuid[obj.Uuid];
                if (NeedsRebuild(oldObj, obj))
                {
                    // Full rebuild for this object
                    Debug.Log($"[UnityComponentSpawner] Rebuilding object: {obj.Obj_name}");
                    Destroy(spawnedByUuid[obj.Uuid]);
                    var newGo = SpawnObject(obj);
                    spawnedByUuid[obj.Uuid] = newGo;
                    spawnedByName[obj.Obj_name] = newGo;
                }
                else
                {
                    // Only update ref_frame properties (adhesive points)
                    UpdateRefFrameProperties(spawnedByUuid[obj.Uuid], oldObj, obj);
                }
            }
            else
            {
                // New object — spawn it
                Debug.Log($"[UnityComponentSpawner] Spawning new object: {obj.Obj_name}");
                var newGo = SpawnObject(obj);
                spawnedByUuid[obj.Uuid] = newGo;
                spawnedByName[obj.Obj_name] = newGo;
            }

            objectsByUuid[obj.Uuid] = obj;
        }

        // Set parents
        foreach (var obj in newObjectList)
        {
            if (string.IsNullOrEmpty(obj.Parent_frame))
                continue;

            if (!spawnedByUuid.TryGetValue(obj.Uuid, out var childGo) || childGo == null)
                continue;

            GameObject parentObj = null;
            if (!spawnedByName.TryGetValue(obj.Parent_frame, out parentObj))
            {
                parentObj = GameObject.Find(obj.Parent_frame);
            }

            if (parentObj != null && parentObj.activeInHierarchy)
            {
                if (childGo.transform.parent != parentObj.transform)
                {
                    childGo.transform.SetParent(parentObj.transform, false);
                }
            }
            else
            {
                Debug.LogWarning($"Parent frame {obj.Parent_frame} for {obj.Obj_name} does not exist!");
            }
        }
    }

    private GameObject SpawnObject(assembly_manager_interfaces.msg.Object obj)
    {
        var spawnedGameObject = new GameObject(obj.Obj_name);
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

        foreach (var rf in obj.Ref_frames)
        {
            if (rf.Properties.Glue_pt_frame_properties.Is_glue_point)
            {
                GameObject gluePoint = CreateGluePoint(spawnedGameObject, rf, obj.Obj_name);

                if (rf.Properties.Glue_pt_frame_properties.Has_been_placed)
                {
                    // CreateAdhesivePoint(gluePoint, rf.Frame_name);
                    continue;
                }
            }
        }

        if (withRefFrame)
        {
            Instantiate(Resources.Load<GameObject>("Prefabs/RefFrame"), spawnedGameObject.transform);
        }

        return spawnedGameObject;
    }

    private void UpdateRefFrameProperties(GameObject go, assembly_manager_interfaces.msg.Object oldObj, assembly_manager_interfaces.msg.Object newObj)
    {
        foreach (var newRf in newObj.Ref_frames)
        {
            if (!newRf.Properties.Glue_pt_frame_properties.Is_glue_point)
                continue;

            // Find matching old ref_frame
            bool wasPlaced = false;
            foreach (var oldRf in oldObj.Ref_frames)
            {
                if (oldRf.Frame_name == newRf.Frame_name)
                {
                    wasPlaced = oldRf.Properties.Glue_pt_frame_properties.Has_been_placed;
                    break;
                }
            }

            bool isNowPlaced = newRf.Properties.Glue_pt_frame_properties.Has_been_placed;

            // Ensure glue point exists
            Transform gluePointTransform = go.transform.Find(newRf.Frame_name);
            GameObject gluePoint;
            if (gluePointTransform == null)
            {
                gluePoint = CreateGluePoint(go, newRf, newObj.Obj_name);
            }
            else
            {
                gluePoint = gluePointTransform.gameObject;
            }

            if (!wasPlaced && isNowPlaced)
            {
                // Adhesive was just placed
                // CreateAdhesivePoint(gluePoint, newRf.Frame_name);
                continue;
            }
            else if (wasPlaced && !isNowPlaced)
            {
                // Adhesive was removed
                Transform adhesiveTransform = gluePoint.transform.Find(newRf.Frame_name + "_adhesive");
                if (adhesiveTransform != null)
                {
                    Debug.Log($"[UnityComponentSpawner] Removing adhesive point at: {newRf.Frame_name}");
                    Destroy(adhesiveTransform.gameObject);
                }
            }
        }
    }

    private GameObject CreateGluePoint(GameObject parent, assembly_manager_interfaces.msg.RefFrame rf, string objName)
    {
        GameObject gluePoint = new GameObject(rf.Frame_name);
        gluePoint.tag = "spawned";
        gluePoint.transform.SetParent(parent.transform, false);
        gluePoint.transform.localPosition = new Vector3(
            (float)(rf.Pose.Position.Y * -1),
            (float)rf.Pose.Position.Z,
            (float)rf.Pose.Position.X
        );
        Debug.Log($"[UnityComponentSpawner] Created glue point: {rf.Frame_name} under {objName}");
        return gluePoint;
    }

    private void CreateAdhesivePoint(GameObject gluePoint, string frameName)
    {
        if (gluePoint.transform.Find(frameName + "_adhesive") != null)
            return;

        if (adhesivePointPrefab != null)
        {
            GameObject adhesivePoint = Instantiate(adhesivePointPrefab, gluePoint.transform);
            adhesivePoint.tag = "AdhesivePoint";
            adhesivePoint.name = frameName + "_adhesive";
            adhesivePoint.transform.localPosition = Vector3.zero;
            Debug.Log($"[UnityComponentSpawner] Created adhesive point at: {frameName}");
        }
        else
        {
            Debug.LogWarning("[UnityComponentSpawner] adhesivePointPrefab is not assigned!");
        }
    }

    private bool NeedsRebuild(assembly_manager_interfaces.msg.Object oldObj, assembly_manager_interfaces.msg.Object newObj)
    {
        if (oldObj.Obj_name != newObj.Obj_name) return true;
        if (oldObj.Parent_frame != newObj.Parent_frame) return true;
        if (oldObj.Cad_data != newObj.Cad_data) return true;
        if (oldObj.Obj_pose.Position.X != newObj.Obj_pose.Position.X) return true;
        if (oldObj.Obj_pose.Position.Y != newObj.Obj_pose.Position.Y) return true;
        if (oldObj.Obj_pose.Position.Z != newObj.Obj_pose.Position.Z) return true;
        if (oldObj.Obj_pose.Orientation.X != newObj.Obj_pose.Orientation.X) return true;
        if (oldObj.Obj_pose.Orientation.Y != newObj.Obj_pose.Orientation.Y) return true;
        if (oldObj.Obj_pose.Orientation.Z != newObj.Obj_pose.Orientation.Z) return true;
        if (oldObj.Obj_pose.Orientation.W != newObj.Obj_pose.Orientation.W) return true;
        if (oldObj.Ref_frames.Length != newObj.Ref_frames.Length) return true;
        return false;
    }

    private bool CheckSceneHasChanged(List<assembly_manager_interfaces.msg.Object> msgObjectList)
    {
        if (objectsByUuid.Count != msgObjectList.Count)
            return true;

        foreach (var newObj in msgObjectList)
        {
            if (!objectsByUuid.TryGetValue(newObj.Uuid, out var oldObj))
                return true;

            if (NeedsRebuild(oldObj, newObj))
                return true;

            // Check ref_frame properties
            for (int j = 0; j < oldObj.Ref_frames.Length; j++)
            {
                var rfA = oldObj.Ref_frames[j];
                var rfB = newObj.Ref_frames[j];

                if (rfA.Frame_name != rfB.Frame_name)
                    return true;

                if (rfA.Properties.Glue_pt_frame_properties.Has_been_placed !=
                    rfB.Properties.Glue_pt_frame_properties.Has_been_placed)
                    return true;
            }
        }
        return false;
    }
}
