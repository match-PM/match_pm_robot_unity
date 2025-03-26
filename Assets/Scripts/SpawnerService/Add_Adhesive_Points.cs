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

    // Already created points
    private HashSet<string> createdPoints = new HashSet<string>();

    // Still waiting for parent
    private Dictionary<string, AddAdhesivePoint> pendingPoints
        = new Dictionary<string, AddAdhesivePoint>();

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
            List<string> toRemove = new List<string>();

            foreach (var kvp in pendingPoints)
            {
                string pointName = kvp.Key;
                var ap = kvp.Value;

                GameObject parentObj = GameObject.Find(ap.parentFrame);
                if (parentObj == null)
                {
                    // Parent not found yet; try again next frame
                    continue;
                }

                // Create the prefab
                GameObject pointObject = Instantiate(adhesivePointPrefab, parentObj.transform);
                pointObject.tag = "AdhesivePoint";
                pointObject.name = ap.pointName;
                pointObject.transform.localPosition = ap.localPosition;

                // Mark as created so we never re‐add it
                createdPoints.Add(pointName);

                // We can now remove it from pending
                toRemove.Add(pointName);

                Debug.Log($"Created adhesive point '{ap.pointName}' under '{ap.parentFrame}'.");
            }

            // Clean up
            foreach (string rm in toRemove)
            {
                pendingPoints.Remove(rm);
            }
        }
    }

    // This callback fires on a background thread when a new message arrives
    private void MsgCallback(addAdhesivePointsMsg msg)
    {
        lock (framesLock)
        {
            // 1) If the incoming message has no points: remove all existing adhesive points
            if (msg.Points.Length == 0 && pendingPoints.Count != 0)
            {
                Debug.Log("Received empty points array. Removing all adhesive points in the scene...");

                shouldClearAll = true;
                return;
            }

            foreach (var p in msg.Points)
            {
                string trimmedName = p.Name.Trim();
                if (createdPoints.Contains(trimmedName))
                {   
                    // We already created it
                    continue;
                }
                if (pendingPoints.ContainsKey(trimmedName))
                {
                    // It's still waiting for its parent from a previous message
                    continue;
                }

                // Otherwise add to pending
                pendingPoints[trimmedName] = new AddAdhesivePoint {
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
            pendingPoints.Clear();
            createdPoints.Clear();
        }

        // 3) Reset the flag
        shouldClearAll = false;
    }
}