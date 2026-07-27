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

    // Prefab used for points dispensed with the 2K dispenser. Unlike the fixed size
    // adhesive point above, this one carries an AdhesiveDot component and is scaled to the
    // diameter reported in the message, which pm_robot_primitive_skills derives from the
    // dispense time — so a longer dispense leaves a visibly bigger dot. Resolved from
    // Resources in Start() when it is not wired up in the inspector.
    public GameObject adhesiveDotPrefab;

    private const string AdhesiveDotPrefabResource = "Prefabs/Adhesive_Dot_2K";

    // Marks the points that came from the 2K dispenser (PrimitiveSkillsUtils sets
    // endeffector_frame to '2K_Dispenser_TCP' for those, '1K_Dispenser_TCP' otherwise).
    private const string Dispenser2kMarker = "2K";

    // A struct to store the data needed to spawn an adhesive point
    private struct AddAdhesivePoint
    {
        public string pointName;
        public string parentFrame;
        public Vector3 localPosition;
        // Dot geometry in mm, straight from the message. Only used for 2K points; the 1K
        // prefab keeps its own fixed size.
        public float diameterMm;
        public float heightMm;
        public bool is2K;
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

        if (adhesiveDotPrefab == null)
        {
            adhesiveDotPrefab = Resources.Load<GameObject>(AdhesiveDotPrefabResource);
            if (adhesiveDotPrefab == null)
                Debug.LogWarning($"Adhesive dot prefab '{AdhesiveDotPrefabResource}' not found — 2K points fall back to the fixed size adhesive point prefab.");
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
                    // Dispensing a second time at the same glue point adds adhesive, so the
                    // message reports a bigger diameter for the same name. Re-apply it to the
                    // live dot instead of leaving it at the size of the first dispense.
                    AdhesiveDot liveDot = existing.GetComponent<AdhesiveDot>();
                    if (liveDot != null &&
                        (!Mathf.Approximately(liveDot.diameterMm, ap.diameterMm) ||
                         !Mathf.Approximately(liveDot.heightMm, ap.heightMm)))
                    {
                        Debug.Log($"Resized adhesive dot '{ap.pointName}' to ⌀{ap.diameterMm} mm x {ap.heightMm} mm.");
                        liveDot.SetSize(ap.diameterMm, ap.heightMm);
                    }
                    continue;
                }

                GameObject parentObj = GameObject.Find(ap.parentFrame);
                if (parentObj == null)
                {
                    // Parent not in the scene yet (or mid-rebuild); try again next frame.
                    continue;
                }

                // 2K points get the scalable dot prefab, everything else keeps the original
                // fixed size adhesive point prefab.
                GameObject prefab = (ap.is2K && adhesiveDotPrefab != null) ? adhesiveDotPrefab : adhesivePointPrefab;

                // Create the prefab. Use worldPositionStays: false so the prefab keeps its
                // own local rotation/scale instead of Unity back-computing them from the
                // parent frame's world transform. TF frames can carry rotated / non-uniform
                // scales, and the default (worldPositionStays: true) can shear or collapse the
                // point's scale to near-zero, making it invisible until a scene reload.
                GameObject pointObject = Instantiate(prefab, parentObj.transform, false);
                pointObject.tag = "AdhesivePoint";
                pointObject.name = ap.pointName;
                pointObject.transform.localPosition = ap.localPosition;

                // Size the dot from the message (mm). Only the dot prefab carries this
                // component, so the 1K prefab is unaffected.
                AdhesiveDot dot = pointObject.GetComponent<AdhesiveDot>();
                if (dot != null)
                    dot.SetSize(ap.diameterMm, ap.heightMm);

                // Remember the live instance so we don't recreate it every frame, and so we
                // can tell when it has been destroyed by a hierarchy rebuild.
                spawnedPoints[pointName] = pointObject;

                Debug.Log($"Created adhesive point '{ap.pointName}' under '{ap.parentFrame}' (2K={ap.is2K}, ⌀{ap.diameterMm} mm x {ap.heightMm} mm).");
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

                // Record it as desired; Update() will (re)create it whenever it isn't alive.
                // An already known point is overwritten rather than skipped: the size may have
                // changed since the last message (dispensing again at the same glue point), and
                // Update() picks that up and resizes the live dot.
                desiredPoints[trimmedName] = new AddAdhesivePoint {
                    pointName   = trimmedName,
                    parentFrame = p.Parent_frame.Trim(),
                    localPosition = new Vector3(
                        (float)(p.Point_pose.Position.Y*-1),
                        (float)(p.Point_pose.Position.Z*-1),
                        (float)p.Point_pose.Position.X
                    ),
                    diameterMm = (float)p.Diameter,
                    heightMm   = (float)p.Hight,
                    is2K       = p.Endeffector_frame != null && p.Endeffector_frame.Contains(Dispenser2kMarker)
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