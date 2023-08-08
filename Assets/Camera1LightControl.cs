using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

public class Camera1LightControl : MonoBehaviour
{
    private Light coaxLightCam1;
    private OPCUA_Client OPCUA_client;
    private GameObject robot;

    public class LightComponent
    {
        public Light light {get; set;}
        public bool lightState = false;
        public double intensity = 1.0;
        public Color lightColor = Color.white;
    };
    // Start is called before the first frame update
    void Start()
    {
        robot = GameObject.Find("pm_robot");
        OPCUA_client = robot.GetComponent<OPCUA_Client>();
        coaxLightCam1 = GetComponent<Light>();
        coaxLightCam1.intensity = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void turnLightsOnOff(Light light, bool currentState)
    {
        light.intensity = Convert.ToInt32(!currentState);
    }

}
