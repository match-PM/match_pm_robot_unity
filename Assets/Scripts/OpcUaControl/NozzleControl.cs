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

public class NozzleControl : MonoBehaviour
{
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;

    // Current nozzle state (-1 = vacuum, 0 = off, 1 = pressure)

    // Flag to check when OPC UA is initialized
    private bool isInitialized = false;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Find the shared references
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;

        // Wait until the OPC UA client is actually connected
        yield return new WaitUntil(() => OPCUA_Client.IsConnected);

        isInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Only process if initialized, OPC UA is ready, and the client is connected
        if (isInitialized && OPCUA_Client.IsConnected)
        {
            UpdateNozzleState();
        }
    }

    /// <summary>
    /// Reads the commanded nozzle state (-1, 0, or 1) from the OPC UA client
    /// </summary>
    private void UpdateNozzleState()
    {
        // Read the nozzle command from OPC UA
        int commandedState = (int)OPCUA_Client.allNodes[gameObject.name + "/" + "State"].dataValue.Value;
        Debug.Log("Commanded Nozzle State: " + commandedState);

    }

}
