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

public class Camera1LightControl : MonoBehaviour
{
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private List <ComponentClasses.LightComponent> lightComponents = new List<ComponentClasses.LightComponent>();
    
    bool checkForStart()
    {
        bool start = true;
        if(lightComponents.Any(item => OPCUA_Client.allNodes["Camera1"].childrenNodes[item.name].result.Value == null))
        {
            start = false;
        }
        return start;
    }

    void updateLights()
    {
        foreach(ComponentClasses.LightComponent lightComponent in lightComponents)
            {
                List <bool> currentState = new List<bool>();
                float[] currentColor = null;
                if(lightComponent.name != "CoaxLight")
                {
                    var stateReading = (bool[]) OPCUA_Client.allNodes["Camera1"].childrenNodes[lightComponent.name].result.Value;
                    var colorReading = (int[]) OPCUA_Client.allNodes["Camera1"].childrenNodes[lightComponent.name+"RGB"].result.Value;
                    currentColor = GenericFunctions.convertColor(colorReading);
                    currentState = stateReading.ToList();   
                }
                else
                {
                    bool stateReading = (bool) OPCUA_Client.allNodes["Camera1"].childrenNodes[lightComponent.name].result.Value;
                    currentState.Add(stateReading);
                }
                lightComponent.UpdateValues(currentState, currentColor);
            }
    }

    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        List<GameObject> childrenGameObjects = GenericFunctions.getChildrenGameObjects(gameObject); 
        foreach(GameObject childGameObject in childrenGameObjects)
        {
            lightComponents.Add(new ComponentClasses.LightComponent(childGameObject));
        }  
    }

    // Update is called once per frame
    void Update()
    {   
        if(checkForStart())
        {
            updateLights();
        }
    }
}
