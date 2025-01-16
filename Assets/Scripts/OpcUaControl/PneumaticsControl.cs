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


public class PneumaticsControl : MonoBehaviour
{
    ComponentClasses.DriveComponent pneumaticComponent;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;
    private int readTarget;
    private int lastMove = 0;
    private int move;
    private int currentState = -1;
    private int lastState = 0;

    private List<OPCUAWriteContainer> containerList;

    void updateDispenserPosition()
    {
        // Retrieve the move command value from the OPC UA client
        move = (int)OPCUA_Client.allNodes[gameObject.name + "/" + "MoveCommand"].dataValue.Value;
        if (lastMove != move)
        {
            if (move == 1)
            {
                readTarget = 1; // Move forward
            }
            else if (move == -1)
            {
                readTarget = 0; // Move backward
            }
            pneumaticComponent.move(readTarget, null);
            lastMove = move;
        }
    }

    // Function to write the current state (position) of the dispenser to the OPC UA server
    void writeState()
    {
        // Check if the dispenser is at the upper limit position
        if (Mathf.Abs(pneumaticComponent.articulationBody.jointPosition[0] - pneumaticComponent.articulationBody.xDrive.upperLimit) < 0.001)
        {
            currentState = 1; // Set the current state to 1 (upper limit reached)
        }
        // Check if the dispenser is at the lower limit position
        else if (Mathf.Abs(pneumaticComponent.articulationBody.jointPosition[0] - pneumaticComponent.articulationBody.xDrive.lowerLimit) < 0.001)
        {
            currentState = -1; // Set the current state to -1 (lower limit reached)
        }

        // Check if there is a change in the current state
        if (lastState != currentState)
        {
            Variant value = new Variant(currentState);
            // Write the current state to the OPC UA server
            OPCUA_Client.writeToServer(gameObject.name, "Position", value);
            // Update the last state
            lastState = currentState;
        }
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;
        pneumaticComponent = new ComponentClasses.DriveComponent(gameObject);

        yield return new WaitUntil(() => OPCUA_Client.IsConnected);

        // If the mode is 0, add the dispenser's position to the write container of the OPC UA client
        if (mode == 0)
        {
            OPCUA_Client.addToWriteContainer(gameObject.name, "Position");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the OPC UA client has new data ready
        if (OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            // Update the dispenser's position
            updateDispenserPosition();
            
            // If the mode is 0, write the current state of the dispenser
            if (mode == 0)
            {
                writeState();
            }
        }

    }
}
