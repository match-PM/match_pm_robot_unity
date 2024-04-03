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
    private Dictionary<int, string> readNodeName = new Dictionary<int, string>() { { 0, "TargetPosition" }, { 1, "ActualPosition" } };
#nullable enable
    private Dictionary<int, string?> writeNodeName = new Dictionary<int, string?>() { { 0, "ActualPosition" }, { 1, null } };

    void updateAxis()
    {
        // Read the target position from the OPC UA server.
        readTarget = (int)OPCUA_Client.allNodes[gameObject.name + "/" + readNodeName[(int)mode]].dataValue.Value;

        if (lastRead != readTarget)
        {
            // If the targets are different, move the axis to the new target position.
            axis.move(readTarget, unitsPerIncrement);
            lastRead = readTarget;
        }

        // Check if a 'writeNodeName' is defined for the selected mode.
        if (writeNodeName[(int)mode] != null)
        {
            // Write Positon to server.
            writePosition();
        }
    }

    async void writePosition()
    {
        // Get the current position of the axis.
        float position = axis.articulationBody.jointPosition[0];

        // Calculate a new position value.
        containerList[0].writeValue = new DataValue((int)(position / (float)unitsPerIncrement * (float)Math.Pow(10, 6)));

        // Write Positon to server.
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
            containerList = new List<OPCUAWriteContainer> { new OPCUAWriteContainer(gameObject.name, writeNodeName[(int)mode], new Variant()) };
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (OPCUA_Client.updateReady)
        {
            unitsPerIncrement = (double)OPCUA_Client.allNodes[gameObject.name + "/" + "UnitsPerIncrement"].dataValue.Value;
            updateAxis();
        }
    }
}
