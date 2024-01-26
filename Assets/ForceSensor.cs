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
    private double[] forceData = {0,0,0,0,0,0};
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
        containerList = new List<OPCUAWriteContainer> {new OPCUAWriteContainer("ForceSensor", "Measurements", new Variant())};
    }

    async void writeForceValues(){
        forceData[2] = (double) forceSensor.driveForce[0];
        Debug.Log(forceData[2]);
        containerList[0].writeValue = new DataValue(forceData);
        await OPCUA_Client.WriteValues(containerList);
    }

    // Update is called once per frame
    void Update()
    {
        if(OPCUA_Client.startUpdate){
            writeForceValues();
        }
    }
}
