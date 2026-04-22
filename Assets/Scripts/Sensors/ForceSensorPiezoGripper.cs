using System.Collections;
using UnityEngine;
using ROS2;

using GripperForcesMsg = pm_msgs.msg.GripperForces;

public class ForceSensorPiezoGripper : MonoBehaviour
{
    [Header("ROS2")]
    public string NodeName = "ROS2UnityGripperForceSensor";
    public string PublisherName = "/pm_parallel_gripper_jaw_controller/forces";

    [Header("Gripper Collision Filtering")]
    [SerializeField] private Transform ignoredHierarchyRoot;
    [SerializeField] private bool autoResolveIgnoredHierarchyRoot = true;

    private bool isInitialized = false;
    private ArticulationBody forceSensor;
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IPublisher<GripperForcesMsg> forcePublisher;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;

    private Vector3 contactForce = Vector3.zero;
    private Vector3 lastContactForce = Vector3.zero;
    private int activeCollisions = 0;
    private bool frameHasImpulse = false;
    private bool ignoredHierarchyResolved = false;
    private bool loggedMissingRos2Unity = false;
    private readonly object latestForceLock = new object();
    private Vector3 latestPublishedForce = Vector3.zero;

    void ResolveRos2Unity()
    {
        if (ros2Unity != null)
        {
            loggedMissingRos2Unity = false;
            return;
        }

        if (robotGameObject == null)
            robotGameObject = GameObject.Find("pm_robot");

        if (robotGameObject != null)
            ros2Unity = robotGameObject.GetComponent<ROS2UnityComponent>();

        if (ros2Unity == null && !loggedMissingRos2Unity)
        {
            Debug.LogWarning(
                "ForceSensorPiezoGripper: No ROS2UnityComponent found on 'pm_robot'. " +
                "Add the ROS2UnityComponent to the pm_robot object.");
            loggedMissingRos2Unity = true;
            return;
        }

        if (ros2Unity != null)
            loggedMissingRos2Unity = false;
    }

    void ResolveIgnoredHierarchyRoot()
    {
        if (ignoredHierarchyRoot != null || ignoredHierarchyResolved || !autoResolveIgnoredHierarchyRoot)
            return;

        ignoredHierarchyResolved = true;

        ParallelGripperServiceController gripperController = GetComponentInParent<ParallelGripperServiceController>();
        if (gripperController != null)
        {
            ignoredHierarchyRoot = gripperController.transform;
            return;
        }

        if (transform.root != null)
        {
            ignoredHierarchyRoot = transform.root;
            return;
        }

        if (transform.parent != null)
            ignoredHierarchyRoot = transform.parent;
    }

    /// <summary>
    /// Returns true if the colliding object is part of the gripper's own hierarchy.
    /// These internal contacts should be ignored by the force sensor.
    /// </summary>
    bool IsRobotCollider(Collision collision)
    {
        if (collision == null)
            return false;

        ResolveIgnoredHierarchyRoot();

        Transform collisionTransform = collision.collider != null ? collision.collider.transform : collision.transform;
        if (collisionTransform == null)
            return false;

        if (ignoredHierarchyRoot != null && collisionTransform.IsChildOf(ignoredHierarchyRoot))
            return true;

        Transform parent = forceSensor != null ? forceSensor.transform.parent : transform.parent;
        if (parent != null && collisionTransform.parent == parent)
            return true;

        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsRobotCollider(collision)) return;
        activeCollisions++;
    }

    void OnCollisionExit(Collision collision)
    {
        if (IsRobotCollider(collision)) return;
        activeCollisions = Mathf.Max(0, activeCollisions - 1);
        if (activeCollisions == 0)
            lastContactForce = Vector3.zero;
    }

    void OnCollisionStay(Collision collision)
    {
        if (IsRobotCollider(collision))
            return;

        Vector3 totalImpulse = collision.impulse;
        contactForce += totalImpulse / Time.fixedDeltaTime;

        if (totalImpulse.magnitude > 1e-6f)
        {
            lastContactForce = contactForce;
            frameHasImpulse = true;
        }
    }

    /// <summary>
    /// Converts a vector from Unity (left-handed: X right, Y up, Z forward)
    /// to ROS2 REP-103 (right-handed: X forward, Y left, Z up).
    /// </summary>
    Vector3 UnityToRos(Vector3 v) => new Vector3(v.z, -v.x, v.y);

    public bool TryGetLatestForce(out Vector3 force)
    {
        lock (latestForceLock)
        {
            force = latestPublishedForce;
            return isInitialized;
        }
    }

    void writeForceValues()
    {
        Vector3 effectiveForce = (activeCollisions > 0 && !frameHasImpulse) ? lastContactForce : contactForce;
        Vector3 rosForce = UnityToRos(effectiveForce);
        Vector3 publishedForce = new Vector3(rosForce.x, rosForce.y, -rosForce.z);

        lock (latestForceLock)
            latestPublishedForce = publishedForce;

        if (forcePublisher != null)
        {
            GripperForcesMsg forceValues = new GripperForcesMsg();
            forceValues.Fx = publishedForce.x;
            forceValues.Fy = publishedForce.y;
            forceValues.Fz = publishedForce.z;
            forcePublisher.Publish(forceValues);
        }

        Debug.Log($"ForceSensorPiezoGripper [ROS2]: Force=({publishedForce.x:F4}, {publishedForce.y:F4}, {publishedForce.z:F4}) | Mag={publishedForce.magnitude:F4} | contacts={activeCollisions}");

        contactForce = Vector3.zero;
        frameHasImpulse = false;
    }

    void LockJoint()
    {
        forceSensor.swingYLock = ArticulationDofLock.LockedMotion;
        forceSensor.swingZLock = ArticulationDofLock.LockedMotion;
        forceSensor.twistLock = ArticulationDofLock.LockedMotion;
    }

    void zeroForceSensor()
    {
        lastContactForce = Vector3.zero;
        lock (latestForceLock)
            latestPublishedForce = Vector3.zero;
    }

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        forceSensor = GetComponent<ArticulationBody>();

        if (forceSensor == null)
        {
            Debug.LogError($"ForceSensorPiezoGripper: Missing ArticulationBody on '{gameObject.name}'.");
            yield break;
        }

        forceSensor.useGravity = false;
        LockJoint();
        ResolveIgnoredHierarchyRoot();

        if (robotGameObject != null)
        {
            chooseMode chooseModeComponent = robotGameObject.GetComponent<chooseMode>();
            if (chooseModeComponent != null)
                mode = chooseModeComponent.mode;
        }

        isInitialized = true;

        yield return new WaitUntil(() =>
        {
            ResolveRos2Unity();
            return ros2Unity != null && ros2Unity.Ok();
        });

        if (ros2Node == null)
        {
            ros2Node = ros2Unity.CreateNode(NodeName);
            forcePublisher = ros2Node.CreatePublisher<GripperForcesMsg>(PublisherName);
        }
    }

    void FixedUpdate()
    {
        if (isInitialized && mode == 0)
            writeForceValues();
    }
}
