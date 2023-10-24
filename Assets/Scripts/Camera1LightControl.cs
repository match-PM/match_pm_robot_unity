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
    
    void updateLights()
    {
        foreach(ComponentClasses.LightComponent lightComponent in lightComponents)
            {
                List <bool> currentState = new List<bool>();
                float[] currentColor = null;

                // Check if the lightComponent's name is not "CoaxLight."
                if(lightComponent.name != "CoaxLight")
                {
                    // Read state and color values from the OPC UA server for this lightComponent.
                    var stateReading = (bool[]) OPCUA_Client.allNodes["Camera1" + "/" + lightComponent.name].dataValue.Value;
                    var colorReading = (int[]) OPCUA_Client.allNodes["Camera1" + "/" + lightComponent.name + "RGB"].dataValue.Value;

                    // Convert the color reading into a float array.
                    currentColor = GenericFunctions.convertColor(colorReading);

                    // Convert the state reading to a List of bool.
                    currentState = stateReading.ToList();   
                }
                else
                {
                    bool stateReading = (bool) OPCUA_Client.allNodes["Camera1" + "/" + lightComponent.name].dataValue.Value;
                    currentState.Add(stateReading);
                }
                // Update the lightComponent with the current state and color.
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
        if(OPCUA_Client.startUpdate)
        {
            updateLights();
        }
    }
}
