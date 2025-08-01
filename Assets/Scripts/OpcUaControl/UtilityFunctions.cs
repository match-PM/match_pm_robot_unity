using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;

using UnityEngine;

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;



namespace UtilityFunctions
{
    public static class ComponentClasses
    {
        // The Component class represents a basic component.
        public class Component
        {
            // The name of the component.
            public string name;

            // The parent GameObject to which the component is attached.
            public GameObject parentObject;

            public Component() { }

            public Component(string componentName, GameObject parentGameObject)
            {
                name = componentName;
                parentObject = parentGameObject;
            }
        }

        // The LightComponent class specializes in handling light components and inherits from the Component class.
        public class LightComponent : Component
        {
            // A list of Light components.
            public List<Light> lights;

            // A reference to a single Light component.
            public Light light;

            // A list to store the state of the lights.
            public List<bool> state;

            // The intensity of the light.
            public double intensity;

            // The color of the light.
            public Color color;

            public LightComponent(List<Light> lightsList)
            {
                lights = lightsList;
                state = new List<bool>();
                intensity = 1.0;
                color = Color.white;
            }

            // Method to update the state and color of the light component based on input parameters.
            public void UpdateValues(List<bool> currentState, float[] currentColor = null)
            {
                if (!currentState.SequenceEqual(state))
                {
                    for (int i = 0; i < currentState.Count; i++)
                    {
                        light = lights[i];
                        turnLightsOnOff(currentState[i]);
                    }
                    state = currentState;
                }
                ;

                if (currentColor != null && !color.Equals(new Color((float)currentColor[0], (float)currentColor[1], (float)currentColor[2], 1.0f)))
                {
                    foreach (Light light in lights)
                    {
                        Debug.Log("Changing Color!");
                        light.color = new Color((float)currentColor[0], (float)currentColor[1], (float)currentColor[2], 1.0f);
                    }
                    color = light.color;
                }
                ;
            }

            // Method to turn lights on or off based on the current activity.
            void turnLightsOnOff(bool currentActivity)
            {
                Dictionary<bool, string> message = new Dictionary<bool, string>();
                message.Add(true, "on!");
                message.Add(false, "off!");

                light.enabled = currentActivity;

                Debug.Log("The " + light.name + " is " + message[currentActivity]);
            }

            // Method to retrieve the light components from the parent object.
            void getLightComponentFormParent()
            {

                if (parentObject.GetComponent<Light>() == null)
                {
                    Transform transforms = parentObject.transform;
                    foreach (Transform t in transforms)
                    {
                        if (t != null && t.gameObject != null && t.gameObject != parentObject)
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


        public class DriveComponent : Component
        {
            public ArticulationBody articulationBody;
            public ArticulationDrive newDrive;
            private Vector3 startPosition;
            private Vector3 currentPosition;
            public float newTarget;

            public DriveComponent(GameObject currentGameObject)
            {
                this.name = currentGameObject.name;
                this.parentObject = currentGameObject;
                articulationBody = currentGameObject.GetComponent<ArticulationBody>();
            }

            public void move(int readTarget, double? unitsPerIncrement)
            {
                newTarget = doTargetConversion(readTarget, unitsPerIncrement);
                newDrive = articulationBody.xDrive;
                newDrive.target = newTarget;
                articulationBody.xDrive = newDrive;

            }


            private float doTargetConversion(int readTarget, double? unitsPerIncrement)
            {
                float newTarget = 0.0f;

                if (unitsPerIncrement == null)
                {
                    unitsPerIncrement = articulationBody.xDrive.upperLimit * (float)Math.Pow(10, 6);
                }
                if (articulationBody.jointType == ArticulationJointType.PrismaticJoint)
                {
                    newTarget = (float)readTarget * (float)unitsPerIncrement * (float)Math.Pow(10, -6);
                }
                else if (articulationBody.jointType == ArticulationJointType.RevoluteJoint)
                {
                    if (articulationBody.transform.name == "RobotAxisR" || articulationBody.transform.name == "RobotAxisQ")
                    {
                        newTarget = (float)readTarget * (float)unitsPerIncrement *-1;
                    }
                    else
                    {
                        newTarget = (float)readTarget * (float)unitsPerIncrement;
                    }
                    
                }

                return newTarget;
            }

        }
    }



    public class GenericFunctions
    {
        public GenericFunctions() { }

        // Static method to get the children GameObjects of a given GameObject.
        public static List<GameObject> getChildrenGameObjects(GameObject currentGameObject)
        {
            Transform transforms = currentGameObject.transform;
            List<GameObject> childrenGameObjects = new List<GameObject>();

            // Iterate through the child transforms and add their GameObjects to the list.
            foreach (Transform t in transforms)
            {
                if (t != null && t.gameObject != null && t.gameObject != currentGameObject)
                {
                    childrenGameObjects.Add(t.gameObject);
                }
            }
            return childrenGameObjects;
        }

        // Static method to convert an array of integer color values to an array of floats.
        public static float[] convertColor(int[] colorReading)
        {
            float[] currentColor = new float[colorReading.Length];

            // Iterate through the integer color values and convert them to floats.
            for (int i = 0; i < colorReading.Length; i++)
            {
                currentColor[i] = (float)colorReading[i] / 100.0f;
            }

            return currentColor;
        }



        public static void hideGameObjects(List<GameObject> objectsToHide)
        {
            foreach (GameObject objectToHide in objectsToHide)
            {
                if (objectToHide.name.Equals("t_axis_tool") ||
                    objectToHide.name.Equals("Visuals") ||
                    objectToHide.name.Equals("Collisions"))
                {
                    continue;
                }
                objectToHide.SetActive(false);
            }
        }

        public static class YamlLoader
        {
            public static Dictionary<string, object> LoadYaml(string filepath)
            {
                Dictionary<string, object> dict = null;
                try
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .WithObjectFactory(new CustomObjectFactory()) // Use the custom object factory
                        .Build();

                    var yaml = File.ReadAllText(filepath);
                    var rawObject = deserializer.Deserialize<object>(yaml);

                    // Safely convert to Dictionary<string, object>
                    dict = ConvertToDictionary(rawObject) as Dictionary<string, object>;

                    if (dict == null)
                    {
                        throw new InvalidOperationException("YAML root object is not a dictionary");
                    }

                    // Recursively process the dictionary and convert booleans
                    ProcessBooleansRecursively(dict);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while loading the YAML file: " + ex);
                }
                return dict;
            }


            private static object ConvertToDictionary(object obj)
            {
                if (obj is Dictionary<object, object> originalDict)
                {
                    return originalDict.ToDictionary(
                        entry => entry.Key.ToString(),
                        entry => ConvertToDictionary(entry.Value) // Missing closing parentheses can cause issues
                    );
                }
                else if (obj is List<object> list)
                {
                    return list.Select(ConvertToDictionary).ToList();
                }
                return obj; // Missing return can also cause issues
            }


            private static void ProcessBooleansRecursively(Dictionary<string, object> dict)
            {
                foreach (var key in dict.Keys.ToList())
                {
                    var value = dict[key];

                    if (value is string strValue)
                    {
                        if (bool.TryParse(strValue, out bool boolValue))
                        {
                            dict[key] = boolValue;
                        }
                    }
                    else if (value is Dictionary<string, object> nestedDict)
                    {
                        ProcessBooleansRecursively(nestedDict);
                    }
                    else if (value is List<object> list)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i] is string itemStr && bool.TryParse(itemStr, out bool itemBool))
                            {
                                list[i] = itemBool;
                            }
                        }
                    }
                }
            }
        }

        public class CustomObjectFactory : IObjectFactory
        {
            public object Create(Type type)
            {
                if (type == typeof(Dictionary<object, object>))
                {
                    // Ensure dictionaries are created as Dictionary<string, object>
                    return new Dictionary<string, object>();
                }
                if (type == typeof(List<object>))
                {
                    // Ensure lists are created properly
                    return new List<object>();
                }
                return Activator.CreateInstance(type);
            }
        }

    }



    namespace OPCUA
    {

        // NodeData class is used to store information about an OPC UA node.
        public class NodeData
        {
            public NodeId nodeId;    // The NodeId associated with the node.
            public DataValue dataValue = new DataValue(); // The data value associated with the node.

            public NodeData() { }
            public NodeData(NodeId id)
            {
                nodeId = id;
            }
        }

        public class OPCUAWriteContainer
        {
            // Dictionary to store WriteValues with a string key.
            public Dictionary<string, WriteValue> container = new Dictionary<string, WriteValue>();
            // Collection to hold WriteValues.
            public WriteValueCollection nodesToWrite;
            // WriteValue object to hold a single node value.
            private WriteValue nodeValue;

            public OPCUAWriteContainer() { }

            // Method to convert the container dictionary to a WriteValueCollection.
            void convertToValueCollection()
            {
                // Converting the dictionary values to a list and initializing the WriteValueCollection.
                nodesToWrite = new WriteValueCollection(container.Values.ToList());
            }

            // Method to add a node to the container.
            public void addToCollection(NodeId nodeId, string parentName, string childName, Variant initialValue)
            {
                if (!container.ContainsKey(parentName + "/" + childName))
                {
                    WriteValue nodeValue = new WriteValue()
                    {
                        // Creating a new WriteValue object for the node.
                        NodeId = nodeId,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(initialValue)
                    };

                    container.Add(parentName + "/" + childName, nodeValue);

                    convertToValueCollection();
                }
            }

            // Method to remove a node from the container.
            public void removeFromColection(string parentName, string childName)
            {
                container.Remove(parentName + "/" + childName);
                convertToValueCollection();
            }
        }
    }
}


