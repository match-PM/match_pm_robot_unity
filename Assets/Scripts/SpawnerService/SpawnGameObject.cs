using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter;
using ROS2;
using std_msgs.msg;

public class SpawnGameObject : MonoBehaviour
{
    public float[] targetPosition;
    public float[] targetRotation;
    public string parentFrame;
    public string cadDataPath;
    public ColorRGBA color;
    public string uuid;

    // Random placement error to reflect real-world variability. Each axis is drawn
    // uniformly from [-max, +max] when the part first enters the scene. Set by
    // UnityComponentSpawner before ApplyPose runs.
    // Position: metres, along the part's own ROS axes.
    public Vector3 maxPositionOffset;
    // Rotation: degrees, roll/pitch/yaw about the part's own ROS axes.
    public Vector3 maxRotationOffsetDeg;

    private Mesh[] meshes;

    // True pose of a part in Unity. All poses in Unity coordinates.
    [Serializable]
    private struct TruePose
    {
        public string uuid;
        // Frame the local pose refers to.
        public string parentFrame;
        public Vector3 localPosition;
        public Quaternion localRotation;
        // Fallback if the parent changed while Unity was not running.
        public Vector3 worldPosition;
        public Quaternion worldRotation;
        // Pose the assembly manager believed when the part was last placed (diagnostics only).
        public Vector3 scenePosition;
        public Quaternion sceneRotation;
    }

    [Serializable]
    private class TruePoseFile
    {
        public List<TruePose> parts = new List<TruePose>();
    }

    // Simulates that the true placement of a part is unknown until it is measured: when a
    // part first enters the scene it is put at its scene pose plus a random error. After
    // that, the scene pose is ignored. Measurement corrections only change what the
    // assembly manager believes, never where the part really is. Only a change of parent
    // (gripping, placing) is taken from the scene: the part keeps its world pose and from
    // then on moves with the new parent.
    //
    // UnityComponentSpawner destroys and rebuilds every spawned object whenever the scene
    // changes, so the true pose is kept per UUID outside this component, and stored on
    // disk so stopping and restarting play mode keeps it. It is deleted when the UUID
    // leaves the scene (ForgetPose), so a respawn draws a new error.
    private static readonly Dictionary<string, TruePose> posesByUuid = new Dictionary<string, TruePose>();
    private static bool posesLoaded = false;

    private static string PoseFilePath =>
        System.IO.Path.Combine(Application.persistentDataPath, "spawned_part_poses.json");

    // Static fields survive entering play mode when domain reload is disabled, so force a
    // reload from disk on every play mode start.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPoses()
    {
        posesByUuid.Clear();
        posesLoaded = false;
    }

    // Called when the object leaves the scene, so a later respawn with the same UUID
    // gets a fresh placement error.
    public static void ForgetPose(string uuid)
    {
        LoadPoses();
        if (!string.IsNullOrEmpty(uuid) && posesByUuid.Remove(uuid))
            SavePoses();
    }

    // Called with the first scene message after play mode starts: anything stored from the
    // last session that is no longer in the scene was removed while Unity was not running.
    public static void ForgetPosesExcept(HashSet<string> uuidsInScene)
    {
        LoadPoses();
        var stale = new List<string>();
        foreach (string uuid in posesByUuid.Keys)
        {
            if (!uuidsInScene.Contains(uuid))
                stale.Add(uuid);
        }
        foreach (string uuid in stale)
            posesByUuid.Remove(uuid);
        if (stale.Count > 0)
            SavePoses();
    }

    // Stores the current world pose of all spawned parts, so the fallback for a parent that
    // changed while Unity was stopped starts from where the part was last seen.
    public static void StoreAllWorldPoses()
    {
        LoadPoses();
        bool changed = false;
        foreach (var sgo in FindObjectsOfType<SpawnGameObject>())
        {
            if (string.IsNullOrEmpty(sgo.uuid) || !posesByUuid.TryGetValue(sgo.uuid, out TruePose pose))
                continue;
            pose.worldPosition = sgo.transform.position;
            pose.worldRotation = sgo.transform.rotation;
            posesByUuid[sgo.uuid] = pose;
            changed = true;
        }
        if (changed)
            SavePoses();
    }

    private static void LoadPoses()
    {
        if (posesLoaded)
            return;
        posesLoaded = true;
        posesByUuid.Clear();

        try
        {
            if (!System.IO.File.Exists(PoseFilePath))
                return;
            var file = JsonUtility.FromJson<TruePoseFile>(System.IO.File.ReadAllText(PoseFilePath));
            foreach (var pose in file?.parts ?? new List<TruePose>())
            {
                // An entry without valid rotations is dropped and the part is spawned anew.
                if (!string.IsNullOrEmpty(pose.uuid) && IsValid(pose.localRotation) && IsValid(pose.worldRotation))
                    posesByUuid[pose.uuid] = pose;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SpawnGameObject] Could not read {PoseFilePath}, spawning parts anew: {e.Message}");
        }
    }

    private static void SavePoses()
    {
        try
        {
            var file = new TruePoseFile { parts = new List<TruePose>(posesByUuid.Values) };
            System.IO.File.WriteAllText(PoseFilePath, JsonUtility.ToJson(file, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SpawnGameObject] Could not write {PoseFilePath}: {e.Message}");
        }
    }

    private static bool IsValid(Quaternion q)
    {
        return Mathf.Abs(Quaternion.Dot(q, q) - 1f) < 1e-3f;
    }

    // Called by UnityComponentSpawner once the object is parented, parents before children
    // so the parent's world pose is already final. previousWorldPoses holds the world pose
    // of every part that existed before this rebuild, by UUID.
    public void ApplyPose(Dictionary<string, Pose> previousWorldPoses)
    {
        LoadPoses();

        // Pose the assembly manager believes, relative to the parent frame.
        Vector3 scenePosition = new Vector3(targetPosition[0], targetPosition[1], targetPosition[2]).Ros2Unity();
        Quaternion sceneRotation = new Quaternion(
            targetRotation[0],
            targetRotation[1],
            targetRotation[2],
            targetRotation[3]
        ).Ros2Unity();

        bool hasUuid = !string.IsNullOrEmpty(uuid);
        TruePose stored = default;
        bool known = hasUuid && posesByUuid.TryGetValue(uuid, out stored);

        if (hasUuid && previousWorldPoses.TryGetValue(uuid, out Pose worldPose))
        {
            // Part existed before this rebuild: it stays where it is, whatever the scene says.
            transform.SetPositionAndRotation(worldPose.position, worldPose.rotation);
        }
        else if (known && stored.parentFrame == parentFrame)
        {
            // Play mode restart: back to the stored pose relative to the same parent.
            transform.localPosition = stored.localPosition;
            transform.localRotation = stored.localRotation;
        }
        else if (known)
        {
            // The parent changed while Unity was not running, so the pose relative to the new
            // parent is unknown. The last world pose is the best guess.
            Debug.LogWarning($"[SpawnGameObject] Parent of {name} (uuid {uuid}) changed from " +
                             $"'{stored.parentFrame}' to '{parentFrame}' while Unity was not running; " +
                             "restoring its last world pose.");
            transform.SetPositionAndRotation(stored.worldPosition, stored.worldRotation);
        }
        else
        {
            PlaceWithRandomError(scenePosition, sceneRotation);
        }

        if (hasUuid)
            StorePose(stored, known, scenePosition, sceneRotation);
    }

    // First placement: scene pose plus a random error in the part's own frame.
    private void PlaceWithRandomError(Vector3 scenePosition, Quaternion sceneRotation)
    {
        // Drawn in ROS coordinates so the limits refer to the part's ROS axes.
        Vector3 rosPosition = RandomSymmetric(maxPositionOffset);
        Vector3 rosRpyDeg = RandomSymmetric(maxRotationOffsetDeg);
        transform.localPosition = scenePosition + sceneRotation * rosPosition.Ros2Unity();
        transform.localRotation = sceneRotation * RpyToQuaternion(rosRpyDeg).Ros2Unity();

        Debug.Log($"[SpawnGameObject] Spawned {name} (uuid {uuid}) with placement error: " +
                  $"pos [µm] = ({rosPosition.x * 1e6f:F1}, {rosPosition.y * 1e6f:F1}, {rosPosition.z * 1e6f:F1}), " +
                  $"rpy [deg] = ({rosRpyDeg.x:F3}, {rosRpyDeg.y:F3}, {rosRpyDeg.z:F3})");
    }

    private void StorePose(TruePose previous, bool known, Vector3 scenePosition, Quaternion sceneRotation)
    {
        var pose = new TruePose
        {
            uuid = uuid,
            parentFrame = parentFrame,
            localPosition = transform.localPosition,
            localRotation = transform.localRotation,
            worldPosition = transform.position,
            worldRotation = transform.rotation,
            scenePosition = scenePosition,
            sceneRotation = sceneRotation
        };

        bool sceneChanged = !known ||
                            previous.parentFrame != pose.parentFrame ||
                            (previous.scenePosition - scenePosition).sqrMagnitude > 1e-18f ||
                            Quaternion.Angle(previous.sceneRotation, sceneRotation) > 1e-6f;
        bool trueChanged = !known ||
                           previous.parentFrame != pose.parentFrame ||
                           (previous.localPosition - pose.localPosition).sqrMagnitude > 1e-18f ||
                           Quaternion.Angle(previous.localRotation, pose.localRotation) > 1e-6f;

        posesByUuid[uuid] = pose;
        if (sceneChanged || trueChanged)
            SavePoses();

        if (known && sceneChanged)
        {
            // Difference between where the assembly manager believes the part is and where it
            // really is, in the part's ROS axes - should be close to zero after a correction.
            Quaternion inverseScene = Quaternion.Inverse(sceneRotation);
            Vector3 rosError = (inverseScene * (pose.localPosition - scenePosition)).Unity2Ros();
            float angleError = Quaternion.Angle(sceneRotation, pose.localRotation);
            Debug.Log($"[SpawnGameObject] Scene pose of {name} (uuid {uuid}) changed, part kept in place. " +
                      $"Remaining error: pos [µm] = ({rosError.x * 1e6f:F1}, {rosError.y * 1e6f:F1}, {rosError.z * 1e6f:F1}), " +
                      $"angle [deg] = {angleError:F4}");
        }
    }

    void Start()
    {
        // --- Import your mesh data
        meshes = GetMeshFromSTL(cadDataPath);

        // Create sub-GameObjects (one mesh per GO)
        for (int i = 0; i < meshes.Length; i++)
        {
            CreateMeshPartGameObject(meshes[i], name + $"_{i}");
        }
    }

    private static Vector3 RandomSymmetric(Vector3 max)
    {
        return new Vector3(
            UnityEngine.Random.Range(-max.x, max.x),
            UnityEngine.Random.Range(-max.y, max.y),
            UnityEngine.Random.Range(-max.z, max.z)
        );
    }

    // ROS fixed-axis roll/pitch/yaw (R = Rz * Ry * Rx), built in ROS coordinates so it
    // can be converted with Ros2Unity like any other ROS orientation.
    private static Quaternion RpyToQuaternion(Vector3 rpyDeg)
    {
        return Quaternion.AngleAxis(rpyDeg.z, new Vector3(0, 0, 1)) *
               Quaternion.AngleAxis(rpyDeg.y, new Vector3(0, 1, 0)) *
               Quaternion.AngleAxis(rpyDeg.x, new Vector3(1, 0, 0));
    }

    private Mesh[] GetMeshFromSTL(string stlPath)
    {
        Mesh[] importedMeshes = StlImporter.ImportMesh(stlPath);

        // If you need to flip from ROS to Unity at the geometry level as well,
        // you can uncomment the transformation steps below.
        // Be sure your final coordinate frames are consistent.

        /*
        foreach (Mesh m in importedMeshes)
        {
            Vector3[] v = m.vertices;
            Vector3[] n = m.normals;

            // Transform all vertices/normals from ROS to Unity
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = v[i].Ros2Unity();
                n[i] = n[i].Ros2Unity();
            }
            m.vertices = v;
            m.normals = n;

            // Reverse triangles if necessary
            int[] t = m.triangles;
            System.Array.Reverse(t);
            m.triangles = t;
        }
        */
        return importedMeshes;
    }

    private void CreateMeshPartGameObject(Mesh mesh, string partName)
    {
        // This child object holds one chunk of the mesh
        GameObject spawnedPart = new GameObject(partName);
        spawnedPart.transform.SetParent(transform, false);

        // We match the parent's local transform by default 
        // (because we used 'false' in SetParent).

        // Add renderer and filter to hold the mesh
        var mf = spawnedPart.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mechRenderer = spawnedPart.AddComponent<MeshRenderer>();

        Material mat = new Material(Shader.Find("Standard"));

        if (partName.Contains("Holder") || partName.Contains("Modulcarrier"))
        {
            // find the "aluminium" material in the Resources folder
            Material aluminiumMat = Resources.Load<Material>("Materials/Aluminium");
            mechRenderer.material = aluminiumMat;
        }
        else if (partName.Contains("Sensor"))
        {
            // find the "CdTe" material in the Resources folder
            Material cdteMat = Resources.Load<Material>("Materials/CdTe");
            mechRenderer.material = cdteMat;
        }
        else
        {
            // Debug.LogWarning($"Part name {partName} does not contain 'Modulcarrier' or 'Sensor'. {color.R}, {color.G}, {color.B}, {color.A} will be applied to the default material.");
            // Material aluminiumMat = Resources.Load<Material>("Materials/Aluminium");
            // mechRenderer.material = aluminiumMat;
            // mat.color = new Color(color.R, color.G, color.B, color.A);
            mechRenderer.material = mat;
            mat.color = new Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        // Add a mesh collider to the part and set convex to true
        var mc = spawnedPart.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;

    }
}
