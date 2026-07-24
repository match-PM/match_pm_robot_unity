using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;
using tf2_msgs.msg;
using System.Threading;
using addAdhesivePointsMsg = pm_msgs.msg.VizAdhesivePoints;

[RequireComponent(typeof(ROS2UnityComponent))]
public class Add_Adhesive_Points : MonoBehaviour
{
    private string addAdhesivePointsTopic = "/pm_adhesive_displayer/adhesive_points";
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<addAdhesivePointsMsg> addAdhesivePointsSub;

    // Prefab to be instantiated
    public GameObject adhesivePointPrefab;

    // A struct to store the data needed to spawn an adhesive point
    private struct AddAdhesivePoint
    {
        public string pointName;
        public string parentFrame;
        public Vector3 localPosition;
    }

    // Every point we've been told to display, keyed by name. This is the source of
    // truth: as long as a point is in here we (re)create it whenever it isn't currently
    // alive in the scene.
    private Dictionary<string, AddAdhesivePoint> desiredPoints
        = new Dictionary<string, AddAdhesivePoint>();

    // The live GameObject for each point currently instantiated in the scene, keyed by
    // name. UnityComponentSpawner destroys and rebuilds the spawned hierarchy (e.g. when
    // a component is gripped), which also destroys any adhesive point parented under it.
    // Unity reports a destroyed object as == null, so we detect that below and respawn
    // the point instead of losing it for the rest of the session.
    private Dictionary<string, GameObject> spawnedPoints
        = new Dictionary<string, GameObject>();

    // A lock to synchronize access from the subscription callback
    private readonly object framesLock = new object();

    private bool shouldClearAll = false;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity == null)
        {
            Debug.LogError("ROS2UnityComponent not found on this GameObject. Please add it in the Inspector.");
            return;
        }
    }

    void Update()
    {
        // Create the node & subscription once, after ros2Unity is ready
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("Unity_AdhesivePoints_Listener");
            addAdhesivePointsSub = ros2Node.CreateSubscription<addAdhesivePointsMsg>(
                addAdhesivePointsTopic,
                MsgCallback
            );
            Debug.Log($"Subscribed to {addAdhesivePointsTopic} and ready to receive points.");
        }

        // If the callback signaled that we should remove all points:
        if (shouldClearAll)
        {
            ClearAllAdhesivePoints();
        }

        lock (framesLock)
        {
            foreach (var kvp in desiredPoints)
            {
                string pointName = kvp.Key;
                var ap = kvp.Value;

                // Already alive in the scene? (Unity's overloaded == null reports a
                // destroyed object as null, so a rebuild falls through to respawn here.)
                if (spawnedPoints.TryGetValue(pointName, out GameObject existing) && existing != null)
                {
                    continue;
                }

                GameObject parentObj = GameObject.Find(ap.parentFrame);
                if (parentObj == null)
                {
                    // Parent not in the scene yet (or mid-rebuild); try again next frame.
                    continue;
                }

                // Create the prefab. Use worldPositionStays: false so the prefab keeps its
                // own local rotation/scale instead of Unity back-computing them from the
                // parent frame's world transform. TF frames can carry rotated / non-uniform
                // scales, and the default (worldPositionStays: true) can shear or collapse the
                // point's scale to near-zero, making it invisible until a scene reload.
                GameObject pointObject = Instantiate(adhesivePointPrefab, parentObj.transform, false);
                pointObject.tag = "AdhesivePoint";
                pointObject.name = ap.pointName;
                pointObject.transform.localPosition = ap.localPosition;

                // Remember the live instance so we don't recreate it every frame, and so we
                // can tell when it has been destroyed by a hierarchy rebuild.
                spawnedPoints[pointName] = pointObject;

                Debug.Log($"Created adhesive point '{ap.pointName}' under '{ap.parentFrame}'.");
            }
        }
    }

    // This callback fires on a background thread when a new message arrives
    private void MsgCallback(addAdhesivePointsMsg msg)
    {
        lock (framesLock)
        {
            // 1) If the incoming message has no points: remove all existing adhesive points
            if (msg.Points.Length == 0 && desiredPoints.Count != 0)
            {
                Debug.Log("Received empty points array. Removing all adhesive points in the scene...");

                shouldClearAll = true;
                return;
            }

            foreach (var p in msg.Points)
            {
                string trimmedName = p.Name.Trim();
                if (desiredPoints.ContainsKey(trimmedName))
                {
                    // Already requested; Update() keeps it alive (and respawns it after a
                    // hierarchy rebuild), so there is nothing to do here.
                    continue;
                }

                // Record it as desired; Update() will (re)create it whenever it isn't alive.
                desiredPoints[trimmedName] = new AddAdhesivePoint {
                    pointName   = trimmedName,
                    parentFrame = p.Parent_frame.Trim(),
                    localPosition = new Vector3(
                        (float)(p.Point_pose.Position.Y*-1),
                        (float)(p.Point_pose.Position.Z*-1),
                        (float)p.Point_pose.Position.X
                    )
                };
            }
        }
    }

    private void ClearAllAdhesivePoints()
    {
        // Must be done on main thread
        Debug.Log("Clearing all adhesive points...");

        // 1) Remove all objects with tag "AdhesivePoint"
        GameObject[] allAdhesiveObjects = GameObject.FindGameObjectsWithTag("AdhesivePoint");
        foreach (var obj in allAdhesiveObjects)
        {
            Destroy(obj);
        }

        // 2) Clear data structures
        lock (framesLock)
        {
            desiredPoints.Clear();
            spawnedPoints.Clear();
        }

        // 3) Reset the flag
        shouldClearAll = false;
    }
}