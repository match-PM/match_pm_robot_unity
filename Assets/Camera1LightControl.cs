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
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    
    public class LightComponent
    {
        public string name {get; set;}
        public Light light {get; set;}
        public bool active = false;
        public double intensity = 1.0;
        public Color color = Color.white;
    };
    
    // private Dictionary<string, LightComponent> childrenLightComponents = new Dictionary<string, LightComponent>();
    private List <LightComponent> currentStates = new List<LightComponent>();
    private List<LightComponent> oldStates = new List<LightComponent>();
    
    
    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        getChildrenLightComponents();
    }

    // Update is called once per frame
    void Update()
    {
        var currentState = (bool) OPCUA_Client.allNodes["Camera1"].childrenNodes[currentStates[0].name].result.Value;
        Debug.Log(currentState.GetType());
        currentStates[0].active = currentState;
        turnLightsOnOff(currentStates[0]);
    }


    void turnLightsOnOff(LightComponent lightComponent)
    {
        Dictionary <bool, string> message = new Dictionary<bool, string>(); 
        message.Add(true, "on!"); 
        message.Add(false, "off!"); 

        lightComponent.light.intensity = Convert.ToInt32(lightComponent.active);

        Debug.Log("The " + lightComponent.name + " is " + message[lightComponent.active]);
    }

    void getChildrenLightComponents()
    {
        Transform transforms = transform;

        foreach(Transform t in transforms)
        {
            if(t != null && t.gameObject != null && t.gameObject != gameObject)
            {
                currentStates.Add(new LightComponent() {name = t.name, light = t.gameObject.GetComponent<Light>()});
            }
        }
    }

}
