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


    void writeForceValues()
    {
        // Vector3 force = forceSensor.GetAccumulatedForce();
        // Vector3 torque = forceSensor.GetAccumulatedTorque();
        // forceData[0] = force.x;
        // forceData[1] = force.y;
        // forceData[3] = force.z;
        // forceData[4] = torque.x;
        // forceData[5] = torque.y;    
        // forceData[6] = torque.z;
        // Debug.Log($"ForceSensor: Force: {force.x}, {force.y}, {force.z} | Torque: {torque.x}, {torque.y}, {torque.z}");
        forceData[0] = (double)forceSensor.driveForce[0]* 100000;
        forceData[1] = (double)forceSensor.driveForce[1]* 100000;
        forceData[2] = (double)forceSensor.driveForce[2]* 100000;
        // Debug.Log(forceData[2]);
        Variant forceValues = new Variant(forceData);
        OPCUA_Client.writeToServer("ForceSensor", "Measurements", forceValues);
    }

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        forceSensor = GetComponent<ArticulationBody>();
        forceSensor.useGravity = false;
        mode = robotGameObject.GetComponent<chooseMode>().mode;

        yield return new WaitUntil(() => OPCUA_Client.IsConnected);
        if (mode == 0)
        {
            OPCUA_Client.addToWriteContainer("ForceSensor", "Measurements");
        }
        isInitialized = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isInitialized && mode == 0 && OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            writeForceValues();
        }
    }
}
