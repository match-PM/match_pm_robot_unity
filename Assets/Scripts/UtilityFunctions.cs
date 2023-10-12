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


namespace UtilityFunctions
{
    public static class ComponentClasses
    {
        public class Component
        {
            public string name;
            public GameObject parentObject;

            public Component() {}

            public Component(string componentName, GameObject parentGameObject)
            {
                name = componentName;
                parentObject = parentGameObject;
            }
        }

        public class LightComponent : Component
        {
            public List<Light> lights;
            public Light light;
            public List<bool> state;
            public double intensity;
            public Color color;

            public LightComponent(GameObject currentGameObject)
            {   
                this.name = currentGameObject.name;
                this.parentObject = currentGameObject;
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
                
                if(parentObject.GetComponent<Light>() == null)
                {
                    Transform transforms = parentObject.transform;
                    foreach(Transform t in transforms)
                    {
                        if(t != null && t.gameObject != null && t.gameObject != parentObject)
                        {
                            lights.Add(t.gameObject.GetComponent<Light>());
                        }
                    }
                }
                else
                {
                    lights.Add(parentObject.GetComponent<Light>());
                }
            }
        };


        public class DriveComponent:Component
        {
            public ArticulationBody articulationBody;
            public ArticulationDrive newDrive;
            private Vector3 startPosition;
            private Vector3 currentPosition;

            public DriveComponent(GameObject currentGameObject)
            {
                this.name = currentGameObject.name;
                this.parentObject = currentGameObject;
                articulationBody = currentGameObject.GetComponent<ArticulationBody>();
                startPosition = currentGameObject.transform.localPosition;
                currentPosition = startPosition;
                Debug.Log("Start Position: " + startPosition);
            }

            public void move(int readTarget,double unitsPerIncrement)
            {
                float newTarget = doTargetConversion(readTarget, unitsPerIncrement);
                if(newTarget != articulationBody.xDrive.target)
                {
                    newDrive = articulationBody.xDrive;
                    newDrive.target = newTarget;
                    articulationBody.xDrive = newDrive;
                }

                // Quaternion anchorRotation = articulationBody.anchorRotation;
                // Matrix4x4 rotationMatrix = Matrix4x4.Rotate(anchorRotation);
                // Vector3 newPosition = startPosition + rotationMatrix.MultiplyVector(new Vector3(newTarget, 0, 0));
                // if(newPosition != currentPosition){
                //     // parentObject.transform.localPosition = newPosition;
                //     parentObject.transform.Translate(new Vector3(newTarget, 0, 0) * Time.deltaTime);
                //     Debug.Log("Name: " + name + " Vector: " + rotationMatrix.MultiplyVector(newPosition) + " New position: " + newPosition);
                //     currentPosition = parentObject.transform.localPosition;
                // }
                
            }

            float doTargetConversion(int readTarget, double unitsPerIncrement)
            {
                float newTarget = 0.0f;

                if(articulationBody.jointType == ArticulationJointType.PrismaticJoint)
                {
                    newTarget = (float) readTarget * (float) unitsPerIncrement * (float) Math.Pow(10, -6);
                }

                if(articulationBody.jointType == ArticulationJointType.RevoluteJoint)
                {
                    // newTarget = (float) readTarget * ((float) Math.PI/180.0f);
                    newTarget = (float) readTarget;
                }
                
                return newTarget;
            }

        }
    }
    


    public class GenericFunctions
    {
        public GenericFunctions () {}

        public static List<GameObject> getChildrenGameObjects(GameObject currentGameObject)
        {
            Transform transforms = currentGameObject.transform;
            List<GameObject> childrenGameObjects = new List <GameObject>();
            foreach(Transform t in transforms)
            {
                if(t != null && t.gameObject != null && t.gameObject != currentGameObject)
                {
                    childrenGameObjects.Add(t.gameObject);
                }
            }
            return childrenGameObjects;
        }

        public static float[] convertColor(int[] colorReading)
        {
            float[] currentColor = new float[colorReading.Length];

            for(int i = 0; i<colorReading.Length; i++)
            {
                currentColor[i] = (float)colorReading[i] / 100.0f;
            }

            return currentColor;
        } 

        public static bool checkForStart(string parentName, OPCUA_Client OPCUA_Client)
        {
            return !OPCUA_Client.allNodes[parentName].childrenNodes.Values.Any(item => item.result.Value ==  null);
        }
    }


    namespace OPCUA
    {
        public class NodeData
        {
            public NodeId nodeId {get; set;}
            public Dictionary<string, ChildNode> childrenNodes {get; set;} 
        }

        public class ChildNode
        {
            public NodeId nodeId {get; set;}
            public DataValue result = new DataValue();
        }  

        public class OPCUAWriteContainer
        {
            public string parent; 
            public string child;
            public DataValue writeValue = new DataValue();

            public OPCUAWriteContainer(string parentName, string childName, Variant value)
            {
                parent = parentName; 
                child = childName;
                writeValue = new DataValue(value);
            }  
        }
    }
}
