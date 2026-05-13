using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private string autoResolveIgnoredBranchName = "RobotAxisZ";
    [SerializeField] private bool logContactEvents = true;
    [SerializeField] private string[] externalContactTags = { "spawned", "AdhesivePoint" };

    [SerializeField] private string robotRootObjectName = "pm_robot";

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
    private readonly HashSet<int> activeContactIds = new HashSet<int>();
    private Vector3 latestPublishedForce = Vector3.zero;

    void ResolveRos2Unity()
    {
        if (ros2Unity != null)
        {
            loggedMissingRos2Unity = false;
            return;
        }

        if (robotGameObject == null)
            robotGameObject = GameObject.Find(robotRootObjectName);

        if (robotGameObject != null)
            ros2Unity = robotGameObject.GetComponent<ROS2UnityComponent>();

        if (ros2Unity == null && !loggedMissingRos2Unity)
        {
            Debug.LogWarning(
                $"ForceSensorPiezoGripper: No ROS2UnityComponent found on '{robotRootObjectName}'. " +
                $"Add the ROS2UnityComponent to the {robotRootObjectName} object.");
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

        if (!string.IsNullOrWhiteSpace(autoResolveIgnoredBranchName))
        {
            GameObject ignoredBranch = GameObject.Find(autoResolveIgnoredBranchName);
            if (ignoredBranch != null)
            {
                ignoredHierarchyRoot = ignoredBranch.transform;
                return;
            }
        }
    }

    Transform GetCollisionTransform(Collision collision)
    {
        if (collision == null)
            return null;

        if (collision.collider != null)
            return collision.collider.transform;

        return collision.transform;
    }

    int GetContactId(Collision collision)
    {
        Transform collisionTransform = GetCollisionTransform(collision);
        if (collisionTransform == null)
            return 0;

        Rigidbody attachedRigidbody = collisionTransform.GetComponentInParent<Rigidbody>();
        if (attachedRigidbody != null)
            return attachedRigidbody.GetInstanceID();

        ArticulationBody attachedArticulation = collisionTransform.GetComponentInParent<ArticulationBody>();
        if (attachedArticulation != null)
            return attachedArticulation.GetInstanceID();

        return collisionTransform.GetInstanceID();
    }

    bool HasAllowedExternalTag(Transform collisionTransform)
    {
        if (collisionTransform == null || externalContactTags == null)
            return false;

        Transform current = collisionTransform;
        while (current != null)
        {
            foreach (string tagName in externalContactTags)
            {
                if (!string.IsNullOrWhiteSpace(tagName) && current.CompareTag(tagName))
                    return true;
            }

            current = current.parent;
        }

        return false;
    }


    /// <summary>
    /// Count only external physical contacts. Anything inside the robot hierarchy
    /// is treated as self-contact and ignored.
    /// </summary>
    bool IsRobotCollider(Collision collision)
    {
        if (collision == null)
            return false;

        Transform collisionTransform = GetCollisionTransform(collision);
        if (collisionTransform == null)
            return false;

        Transform sensorParent = forceSensor != null
            ? forceSensor.transform.parent
            : transform.parent;

        if (sensorParent != null && collisionTransform.parent == sensorParent)
            return true;

        if (HasAllowedExternalTag(collisionTransform))
            return false;

        if (robotGameObject == null && !string.IsNullOrWhiteSpace(robotRootObjectName))
            robotGameObject = GameObject.Find(robotRootObjectName);

        if (robotGameObject != null)
        {
            Transform robotRoot = robotGameObject.transform;
            if (collisionTransform == robotRoot || collisionTransform.IsChildOf(robotRoot))
                return true;
        }

        ResolveIgnoredHierarchyRoot();

        if (ignoredHierarchyRoot != null && collisionTransform.IsChildOf(ignoredHierarchyRoot))
            return true;

        return false;
    }

    string DescribeCollision(Collision collision)
    {
        if (collision == null)
            return "<null>";

        Transform collisionTransform = GetCollisionTransform(collision);

        if (collisionTransform == null)
            return collision.gameObject != null ? collision.gameObject.name : "<unknown>";

        Transform root = collisionTransform.root != null ? collisionTransform.root : collisionTransform;
        return $"collider='{collisionTransform.name}' root='{root.name}'";
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsRobotCollider(collision)) return;

        activeContactIds.Add(GetContactId(collision));
        activeCollisions = activeContactIds.Count;

        if (logContactEvents)
            Debug.Log($"ForceSensorPiezoGripper: contact enter {DescribeCollision(collision)} activeContacts={activeCollisions}");
    }

    void OnCollisionExit(Collision collision)
    {
        if (IsRobotCollider(collision)) return;

        activeContactIds.Remove(GetContactId(collision));
        activeCollisions = activeContactIds.Count;

        if (logContactEvents)
            Debug.Log($"ForceSensorPiezoGripper: contact exit {DescribeCollision(collision)} activeContacts={activeCollisions}");

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
        activeContactIds.Clear();
        activeCollisions = 0;
        lock (latestForceLock)
            latestPublishedForce = Vector3.zero;
    }

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find(robotRootObjectName);
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

        isInitialized = true;
    }

    void FixedUpdate()
    {
        if (isInitialized && mode == 0)
            writeForceValues();
    }
}
