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


public class RobotAxisControl : MonoBehaviour
{
    ComponentClasses.AxisComponent axis;
    float currentTarget;
    int readTarget;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    double unitsPerIncrement;

    void updateAxis()
    {
        readTarget = (int) OPCUA_Client.allNodes[gameObject.name].childrenNodes["TargetPosition"].result.Value;
        axis.move(readTarget, unitsPerIncrement);
        float position = axis.articulationBody.jointPosition[0];

        // IMPORTANT CHANGE TOLERANCE TO ACTUAL POSITION!!!
        // OPCUAWriteContainer container = new OPCUAWriteContainer(gameObject.name, "Tolerance", new Variant((int) (position * (float) Math.Pow(10, 6))));
        // await OPCUA_Client.WriteValues(new List<OPCUAWriteContainer> {container});
    }
    
    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        axis = new ComponentClasses.AxisComponent(gameObject); 
        unitsPerIncrement = (double) OPCUA_Client.allNodes[gameObject.name].childrenNodes["UnitsPerIncrement"].result.Value;
        
    }

    // Update is called once per frame
    void Update()
    {

        if(OPCUA_Client.allNodes[gameObject.name].childrenNodes["UnitsPerIncrement"].result.Value != null && OPCUA_Client.allNodes[gameObject.name].childrenNodes["TargetPosition"].result.Value != null)
        {
            unitsPerIncrement = (double) OPCUA_Client.allNodes[gameObject.name].childrenNodes["UnitsPerIncrement"].result.Value;
            updateAxis();
        }

    }
}
