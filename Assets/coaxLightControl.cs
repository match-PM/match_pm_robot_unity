using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;


public class coaxLightControl : MonoBehaviour
{
    private Light coaxLightCam1;
    private bool coaxLightbool = false;
    private OPCUA_test OPCUA_script;
    private GameObject robot;

    
    // Start is called before the first frame update
    void Start()
    {
        robot = GameObject.Find("pm_robot");
        OPCUA_script = robot.GetComponent<OPCUA_test>();
        coaxLightCam1 = GetComponent<Light>();
        coaxLightCam1.intensity = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (OPCUA_script.coaxLightMessage != null && (bool) OPCUA_script.coaxLightMessage.Value != coaxLightbool)
        {
            if ((bool) OPCUA_script.coaxLightMessage.Value == true )
            {
                coaxLightCam1.intensity = 1;
                coaxLightbool = true;
                Debug.Log("Coaxial Light Cam1 switched on");
            }
            else
            {
                coaxLightCam1.intensity = 0;
                coaxLightbool = false;
                Debug.Log("Coaxial Light Cam1 switched off");
                
            }
        }
    }
}



