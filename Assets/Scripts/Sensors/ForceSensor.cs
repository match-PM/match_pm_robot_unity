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
    private ArticulationBody forceSensor;
    private double[] forceData = { 0, 0, 0, 0, 0, 0 };
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private List<OPCUAWriteContainer> containerList;

    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        forceSensor = GetComponent<ArticulationBody>();
        forceSensor.useGravity = false;
        OPCUA_Client.addToWriteContainer("ForceSensor", "Measurements");
    }

    void writeForceValues()
    {
        forceData[2] = (double)forceSensor.driveForce[0];
        Debug.Log(forceData[2]);
        Variant forceValues = new Variant(forceData);
        OPCUA_Client.writeToServer("ForceSensor", "Measurements", forceValues);
    }

    // Update is called once per frame
    void Update()
    {
        if (OPCUA_Client.updateReady)
        {
            writeForceValues();
        }
    }
}
