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
    private List<ComponentClasses.LightComponent> lightComponents = new List<ComponentClasses.LightComponent>();

    void updateLights()
    {
        foreach (ComponentClasses.LightComponent lightComponent in lightComponents)
        {
            // Create a list to store the current state of the light component.
            List<bool> currentState = new List<bool>();

            // Initialize a variable for the current color (but it remains null).
            float[] currentColor = null;

            // Determine the state based on the state reading.
            // If the state reading is not 0, it's considered as 'true' (on); otherwise, it's 'false' (off).
            int stateReading = (int)OPCUA_Client.allNodes["Camera2" + "/" + lightComponent.name].dataValue.Value;
            currentState.Add(stateReading != 0);

            // Update the light component with the current state and color (which is null).
            lightComponent.UpdateValues(currentState, currentColor);
        }
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();

        yield return new WaitUntil(() => OPCUA_Client.IsConnected);

        // List to store child GameObjects of the current GameObject.
        List<GameObject> childrenGameObjects = GenericFunctions.getChildrenGameObjects(gameObject);

        // Iterate through the child GameObjects and create LightComponent objects for each.
        foreach (GameObject childGameObject in childrenGameObjects)
        {
            lightComponents.Add(new ComponentClasses.LightComponent(childGameObject));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            updateLights();
        }
    }
}
