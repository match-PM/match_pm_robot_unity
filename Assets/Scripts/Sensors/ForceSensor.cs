using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Linq;
using UtilityFunctions;
using UtilityFunctions.OPCUA;

public class ForceSensor : MonoBehaviour
{
    private bool isInitialized = false;
    private ArticulationBody forceSensor;
    private double[] forceData = { 0, 0, 0, 0, 0, 0, 0};
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private List<OPCUAWriteContainer> containerList;
    private chooseMode.Mode mode;

    // Contact force accumulation
    private Vector3 contactForce = Vector3.zero;
    private Vector3 contactTorque = Vector3.zero;
    private Vector3 lastContactForce = Vector3.zero;
    private Vector3 lastContactTorque = Vector3.zero;
    private int activeCollisions = 0;
    private bool frameHasImpulse = false;

    /// <summary>
    /// Returns true if the colliding object is part of the robot's own hierarchy.
    /// These internal contacts should be ignored by the force sensor.
    /// </summary>
    bool IsRobotCollider(Collision collision)
    {
        // Check if the collision is with an object that has the same parent
        Transform parent = collision.gameObject.transform.parent;

        if (parent.gameObject == forceSensor.transform.parent.gameObject)
        {    
            Debug.Log($"Ignoring collision with {collision.gameObject.name} (same parent)");
            return true;            
        }

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
        // Only clear held force when all contacts are gone
        if (activeCollisions == 0)
        {
            lastContactForce = Vector3.zero;
            lastContactTorque = Vector3.zero;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (IsRobotCollider(collision))
        {
            // reset force
            lastContactForce = Vector3.zero;
            lastContactTorque = Vector3.zero;
            return;
        }
            

        // Sum up all contact impulses and convert to force (impulse / dt)
        Vector3 totalImpulse = collision.impulse;
        contactForce += totalImpulse / Time.fixedDeltaTime;

        // Calculate torque from contact points relative to this object's center
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 lever = contact.point - forceSensor.worldCenterOfMass;
            Vector3 forceAtContact = contact.normal * (totalImpulse.magnitude / Mathf.Max(collision.contactCount, 1)) / Time.fixedDeltaTime;
            contactTorque += Vector3.Cross(lever, forceAtContact);
        }

        // Only update last-known values when there is a meaningful impulse
        if (totalImpulse.magnitude > 1e-6f)
        {
            lastContactForce = contactForce;
            lastContactTorque = contactTorque;
            frameHasImpulse = true;
        }
    }

    /// <summary>
    /// Converts a vector from Unity (left-handed: X right, Y up, Z forward)
    /// to ROS2 REP-103 (right-handed: X forward, Y left, Z up).
    /// </summary>
    Vector3 UnityToRos(Vector3 v) => new Vector3(v.z, -v.x, v.y);

    void writeForceValues()
    {
        // When in contact but stationary, impulse is ~0 every frame.
        // Use the last known non-zero force so contact detection stays reliable.
        Vector3 effectiveForce  = (activeCollisions > 0 && !frameHasImpulse) ? lastContactForce  : contactForce;
        Vector3 effectiveTorque = (activeCollisions > 0 && !frameHasImpulse) ? lastContactTorque : contactTorque;

        Vector3 rosForce  = UnityToRos(effectiveForce);
        Vector3 rosTorque = UnityToRos(effectiveTorque);

        forceData[0] = (double)rosForce.x;
        forceData[1] = (double)rosForce.y;
        forceData[2] = (double)rosForce.z*-1; // Invert Z 
        forceData[3] = (double)rosTorque.x;
        forceData[4] = (double)rosTorque.y;
        forceData[5] = (double)rosTorque.z;
        forceData[6] = (double)rosForce.magnitude;

        // Debug.Log($"ForceSensor [ROS2]: Force=({rosForce.x:F4}, {rosForce.y:F4}, {rosForce.z:F4}) | Mag={rosForce.magnitude:F4} | contacts={activeCollisions}");

        Variant forceValues = new Variant(forceData);
        OPCUA_Client.writeToServer("ForceSensor", "Measurements", forceValues);

        // Reset accumulators for next physics step
        contactForce = Vector3.zero;
        contactTorque = Vector3.zero;
        frameHasImpulse = false;
    }

    void LockJoint()
    {
        // Fully lock all axes to prevent any rotation of the transform
        forceSensor.swingYLock = ArticulationDofLock.LockedMotion;
        forceSensor.swingZLock = ArticulationDofLock.LockedMotion;
        forceSensor.twistLock = ArticulationDofLock.LockedMotion;
    }

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        forceSensor = GetComponent<ArticulationBody>();
        forceSensor.useGravity = false;
        LockJoint();
        mode = robotGameObject.GetComponent<chooseMode>().mode;

        yield return new WaitUntil(() => OPCUA_Client.IsConnected);
        if (mode == 0)
        {
            OPCUA_Client.addToWriteContainer("ForceSensor", "Measurements");
        }
        isInitialized = true;
    }

    void FixedUpdate()
    {
        if (isInitialized && mode == 0 && OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            writeForceValues();
        }
    }
}
