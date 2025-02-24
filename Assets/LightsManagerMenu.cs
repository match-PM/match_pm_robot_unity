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
using UnityEditor.Animations;


// TODO: 
// - Make these input Fields dynamic. 
// - Add possibility to use custom lights that don't have a node on OPCUA server. 


public class LightsManagerMenu : MonoBehaviour
{
    private OPCUA_Client OPCUA_Client;
    public string OPCUANodeName_Parent;
    public string OPCUANodeName_Child;

    public LightSelection selectedLightType;

    public enum LightSelection
    {
        Coaxial_Light,
        Ring_Light
    }

    public int numberOfLights = 4;

    public float radius;

    private Light coaxLight;
    List<Light> lights = new List<Light>();

    ComponentClasses.LightComponent lightComponent;
    private GameObject robotGameObject;
    private List<ComponentClasses.LightComponent> lightComponents = new List<ComponentClasses.LightComponent>();

    GameObject createChildGameObject(string name)
    {
        GameObject childGameObject = new GameObject(name);
        childGameObject.transform.SetParent(gameObject.transform);
        childGameObject.transform.localPosition = Vector3.zero;
        childGameObject.transform.localRotation = Quaternion.identity;
        childGameObject.transform.localScale = Vector3.one;
        return childGameObject;
    }

    void createCoaxialLight()
    {
        GameObject coaxialLightParent = createChildGameObject("Coaxial Light");
        coaxLight = coaxialLightParent.AddComponent<Light>();
        coaxLight.type = LightType.Spot;
        lights.Add(coaxLight);
        lightComponent = new ComponentClasses.LightComponent(lights);
    }

    void CreateRingLights()
    {
        GameObject ringLightParent = createChildGameObject("Ring Light");

        for (int i = 0; i < numberOfLights; i++)
        {
            float angle = i * (360.0f / numberOfLights);
            float xPos = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float zPos = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            GameObject lightObject = new GameObject("Ring Light " + (i + 1));
            lightObject.transform.SetParent(ringLightParent.transform);
            lightObject.transform.localPosition = new Vector3(xPos, 0, zPos);
            lightObject.transform.localRotation = Quaternion.identity;
            lightObject.transform.localScale = Vector3.one;

            Light ringLight = lightObject.AddComponent<Light>();
            ringLight.type = LightType.Point;
            lights.Add(ringLight);
        }

        lightComponent = new ComponentClasses.LightComponent(lights);
    }


    void updateLights()
    {
        List<bool> currentState = new List<bool>();
        float[] currentColor = null;


        if (selectedLightType != LightSelection.Coaxial_Light)
        {
            // Read state and color values from the OPC UA server for this lightComponent.
            var stateReading = (bool[])OPCUA_Client.allNodes[OPCUANodeName_Parent + "/" + OPCUANodeName_Child].dataValue.Value;
            var colorReading = (int[])OPCUA_Client.allNodes[OPCUANodeName_Parent + "/" + OPCUANodeName_Child + "RGB"].dataValue.Value;

            // Convert the color reading into a float array.
            currentColor = GenericFunctions.convertColor(colorReading);

            // Convert the state reading to a List of bool.
            currentState = stateReading.ToList();
        }
        else
        {
            bool stateReading = (bool)OPCUA_Client.allNodes[OPCUANodeName_Parent + "/" + OPCUANodeName_Child].dataValue.Value;
            currentState.Add(stateReading);
        }
        // Update the lightComponent with the current state and color.
        lightComponent.UpdateValues(currentState, currentColor);
    }

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        yield return new WaitUntil(() => OPCUA_Client.IsConnected);

        if (selectedLightType == LightSelection.Coaxial_Light)
        {
            createCoaxialLight();
        }
        else if (selectedLightType == LightSelection.Ring_Light)
        {
            CreateRingLights();
        }
    }

    void Update()
    {
        if (OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            Debug.Log("Here.");
            updateLights();
        }
    }


}
