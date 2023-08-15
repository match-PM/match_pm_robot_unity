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

public class Camera2LightControl : MonoBehaviour
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

                int stateReading = (int) OPCUA_Client.allNodes["Camera2"].childrenNodes[lightComponent.name].result.Value;
                currentState.Add(stateReading != 0);
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
        if(GenericFunctions.checkForStart(gameObject.name, OPCUA_Client))
        {
            updateLights();
        }
    }
}
