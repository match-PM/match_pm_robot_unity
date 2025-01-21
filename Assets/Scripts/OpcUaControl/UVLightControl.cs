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


public class UVLightControl : MonoBehaviour
{
    public int ArrayIndex;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private List<OPCUAWriteContainer> containerList;
    private Light UVLight;
    private bool isInitialized = false;
    private bool[] stateReading;
    private bool[] writeValues;
    private bool currentState = false;
    private double[] time;
    private double elapsedTime = 0.0;
    int[] power;
  
    // Function to update the state of the UV light based on OPC UA data
    void updateUVLight()
    {
        // Retrieve the state, time, and power values of the UV light from OPC UA nodes
        stateReading = (bool[])OPCUA_Client.allNodes["HoenleUV/OnOff"].dataValue.Value;
        time = (double[])OPCUA_Client.allNodes["HoenleUV/Time"].dataValue.Value;
        power = (int[])OPCUA_Client.allNodes["HoenleUV/Power"].dataValue.Value;

        // Check if the UV light is currently on and if it has exceeded the specified time limit
        if (currentState == true && elapsedTime >= time[ArrayIndex])
        {
            // Turn off the UV light
            currentState = !currentState;
            UVLight.enabled = currentState;
            // Write the updated state to the OPC UA server
            writeUVValues();
        }
        else if (currentState == false && stateReading[ArrayIndex] == true)
        {
            // Turn on the UV light
            currentState = stateReading[ArrayIndex];
            UVLight.enabled = currentState;
            // Adjust the intensity of the UV light based on the power value
            UVLight.intensity = power[ArrayIndex] / 100.0f;
            // Reset the elapsed time
            elapsedTime = 0.0;
        }
    }


    // Function to write the updated state of the UV light to the OPC UA server
    void writeUVValues()
    {
        // Add the UV light state to the write container of the OPC UA client
        OPCUA_Client.addToWriteContainer("HoenleUV", "OnOff", () => isInitialized = true);
        // Update the state reading array with the current state
        stateReading[ArrayIndex] = currentState;
        // Create a variant to hold the updated state reading
        Variant value = new Variant(stateReading);
        // Write the updated state to the OPC UA server
        OPCUA_Client.writeToServer("HoenleUV", "OnOff", value);
        // Remove the UV light state from the write container of the OPC UA client
        OPCUA_Client.removeFromWriteContainer("HoenleUV", "OnOff");
        // Log the shutdown of the UV light and the elapsed time
        Debug.Log("Shutting "+ (ArrayIndex+1)+ "." + " UV light down. Elapsed UV light time: " + elapsedTime.ToString("0.00") + " seconds.");
    }

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();

        yield return new WaitUntil(() => OPCUA_Client.IsConnected);
        
        UVLight = GetComponent<Light>();
        UVLight.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            elapsedTime += Time.deltaTime;
            updateUVLight();
        }
    }
}
