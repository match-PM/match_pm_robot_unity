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
    ComponentClasses.DriveComponent axis;
    float currentTarget;
    int readTarget;
    private int lastRead = 0;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    double unitsPerIncrement;
    private List<OPCUAWriteContainer> containerList;
    private chooseMode.Mode mode;
    private Dictionary<int, string> readNodeName = new Dictionary<int, string>() {{0, "TargetPosition"} , {1, "ActualPosition"}};
    #nullable enable
    private Dictionary<int, string?> writeNodeName = new Dictionary<int, string?>() {{0, "ActualPosition"} , {1, null}};

    void updateAxis()
    {
        readTarget = (int) OPCUA_Client.allNodes[gameObject.name].childrenNodes[readNodeName[(int) mode]].result.Value;
        
        if(lastRead != readTarget)
        {
            axis.move(readTarget, unitsPerIncrement);
            lastRead = readTarget;
        }
        
        if(writeNodeName[(int) mode] != null)
        {
            writePosition();
        }
    }

    async void writePosition()
    {
        float position = axis.articulationBody.jointPosition[0];
        containerList[0].writeValue = new DataValue((int) (position / (float) unitsPerIncrement *  (float) Math.Pow(10, 6)));
        await OPCUA_Client.WriteValues(containerList);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;
        axis = new ComponentClasses.DriveComponent(gameObject);  
        if (mode == 0)
        {
            containerList = new List<OPCUAWriteContainer> {new OPCUAWriteContainer(gameObject.name, writeNodeName[(int) mode], new Variant())};
        }
    }

    // Update is called once per frame
    void Update()
    {

        // if(OPCUA_Client.allNodes[gameObject.name].childrenNodes["UnitsPerIncrement"].result.Value != null && OPCUA_Client.allNodes[gameObject.name].childrenNodes["TargetPosition"].result.Value != null && OPCUA_Client.allNodes[gameObject.name].childrenNodes["ActualPosition"].result.Value != null)
        // {
        //     unitsPerIncrement = (double) OPCUA_Client.allNodes[gameObject.name].childrenNodes["UnitsPerIncrement"].result.Value;
        //     updateAxis();
        // }

        if(OPCUA_Client.startUpdate)
        {
            unitsPerIncrement = (double) OPCUA_Client.allNodes[gameObject.name].childrenNodes["UnitsPerIncrement"].result.Value;
            updateAxis();
        }
    }
}
