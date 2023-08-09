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

public class Camera1LightControl : MonoBehaviour
{
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private List <LightComponent> LightComponents = new List<LightComponent>();
    
    public class LightComponent
    {
        public string name;
        public GameObject parentGameObject;
        public List<Light> lights;
        public Light light;
        public List<bool> state;
        public double intensity;
        public Color color;

        public LightComponent(string lightName, GameObject parent)
        {
            name = lightName;
            parentGameObject = parent;
            lights =  new List<Light>();
            state = new List<bool>();
            intensity = 1.0;
            color = Color.white;
            getLightComponentFormParent();
        }

        public void UpdateValues(List<bool> currentState, float[] currentColor = null)
        {
            if(!currentState.SequenceEqual(state))
            {   
                for(int i = 0; i<currentState.Count; i++)
                {
                    light = lights[i];
                    turnLightsOnOff(currentState[i]);
                }
                state = currentState;
            };

            if(currentColor != null && !color.Equals(new Color((float) currentColor[0], (float) currentColor[1], (float) currentColor[2], 1.0f)))
            {
                foreach (Light light in lights)
                {
                    Debug.Log("Changing Color!");
                    light.color = new Color((float) currentColor[0], (float) currentColor[1], (float) currentColor[2], 1.0f);
                }
                color = light.color;
            };
        }


        void turnLightsOnOff(bool currentActivity)
        {
        Dictionary <bool, string> message = new Dictionary<bool, string>(); 
        message.Add(true, "on!"); 
        message.Add(false, "off!"); 

        light.enabled = currentActivity;

        Debug.Log("The " + light.name + " is " + message[currentActivity]);
        }

        void getLightComponentFormParent()
        {
            
            if(parentGameObject.GetComponent<Light>() == null)
            {
                Transform transforms = parentGameObject.transform;
                foreach(Transform t in transforms)
                {
                    if(t != null && t.gameObject != null && t.gameObject != parentGameObject)
                    {
                        lights.Add(t.gameObject.GetComponent<Light>());
                    }
                }
            }
            else
            {
                lights.Add(parentGameObject.GetComponent<Light>());
            }
        }
    };

    bool checkForStart()
    {
        bool start = true;
        if(LightComponents.Any(item => OPCUA_Client.allNodes["Camera1"].childrenNodes[item.name].result.Value == null))
        {
            start = false;
        }
        return start;
    }

    void updateLights()
    {
        foreach(LightComponent lightComponent in LightComponents)
            {
                List <bool> currentState = new List<bool>();
                float[] currentColor = null;
                if(lightComponent.name != "CoaxLight")
                {
                    var stateReading = (bool[]) OPCUA_Client.allNodes["Camera1"].childrenNodes[lightComponent.name].result.Value;
                    var colorReading = (int[]) OPCUA_Client.allNodes["Camera1"].childrenNodes[lightComponent.name+"RGB"].result.Value;
                    currentColor = convertColor(colorReading);
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

    float[] convertColor(int[] colorReading)
    {
        float[] currentColor = new float[colorReading.Length];

        for(int i = 0; i<colorReading.Length; i++)
        {
            currentColor[i] = (float)colorReading[i] / 100.0f;
        }

        return currentColor;
    }

    void getChildrenLightComponents()
    {
        Transform transforms = transform;

        foreach(Transform t in transforms)
        {
            if(t != null && t.gameObject != null && t.gameObject != gameObject)
            {
                LightComponents.Add(new LightComponent(t.name, t.gameObject));
            }
        }
    }


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
        if(checkForStart())
        {
            updateLights();
        }
    }
}
