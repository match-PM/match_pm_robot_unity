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
    private bool[] stateReading;
    private bool[] writeValues;
    private bool currentState = false;
    private double[] time;
    private double elapsedTime = 0.0;
    int[] power;
    // Start is called before the first frame update
    void updateUVLight(){
        
        stateReading = (bool[]) OPCUA_Client.allNodes["HoenleUV/OnOff"].dataValue.Value;
        time = (double[]) OPCUA_Client.allNodes["HoenleUV/Time"].dataValue.Value;
        power = (int[]) OPCUA_Client.allNodes["HoenleUV/Power"].dataValue.Value;

        if(currentState == true && elapsedTime >= time[ArrayIndex])
        {
            currentState = !currentState;
            UVLight.enabled = currentState;
            writeUVValues();
        }else if(currentState == false && stateReading[ArrayIndex] == true)
        {
            currentState = stateReading[ArrayIndex];
            UVLight.enabled = currentState;
            UVLight.intensity = power[ArrayIndex] / 100.0f;
            elapsedTime = 0.0;
        }
    }

    async void writeUVValues(){
        stateReading[ArrayIndex] = currentState;
        containerList[0].writeValue = new DataValue(stateReading);
        await OPCUA_Client.WriteValues(containerList);
    }

    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        UVLight = GetComponent<Light>();
        UVLight.enabled = false;
        containerList = new List<OPCUAWriteContainer> {new OPCUAWriteContainer("HoenleUV", "OnOff", new Variant())};
    }

    // Update is called once per frame
    void Update()
    {
        if(OPCUA_Client.startUpdate){
            elapsedTime += Time.deltaTime;
            updateUVLight();
        }
    }
}
