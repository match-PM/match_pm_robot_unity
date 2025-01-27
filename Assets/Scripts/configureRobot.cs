using System.Collections.Generic;
using UnityEngine;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;
using UtilityFunctions;
using UnityEditor.Media;
using UnityEngine.Animations;
using System.Diagnostics.Tracing;


public class configureRobot : MonoBehaviour
{
    string filepath = "/home/pmlab/pm_ros2_ws/install/pm_robot_bringup/share/pm_robot_bringup/config/pm_robot_bringup_config.yaml";
    Dictionary<string, object> dictionary;
    private string componentName = null;


    // Start is called before the first frame update
    void Start()
    {
        dictionary = GenericFunctions.YamlLoader.LoadYaml(filepath);

        if (dictionary != null)
        {
            chooseComponentsFormConfig(dictionary);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void chooseComponentsFormConfig(Dictionary<string, object> dict, string? parent = null, bool isTool = false)
    {
        bool hasUseKey = false;
        string useKeyValue = null;
        string useKey = null;
        bool hasUnityParentKey = false;
        string unityParentValue = null;

        bool booleanToolUsed = false;
        string currentUnityParent = null;  // Store unity_parent separately

        foreach (var kvp in dict)
        {
            Debug.Log("Key: " + kvp.Key + " is tool: " + isTool);

            if (kvp.Key.StartsWith("use_"))
            {
                Debug.Log("Checking key: " + kvp.Key);
                if (isTool)
                {
                    if (kvp.Value is bool && (bool)kvp.Value)
                    {
                        Debug.Log("Boolean tool in use: " + kvp.Key);
                        booleanToolUsed = true;
                        // Store unity_parent temporarily
                        useKey = kvp.Key;
                        if (dict.ContainsKey("use_tip"))
                        {
                            useKeyValue = "PM_Robot_Tool_TCP_" + (string)dict["use_tip"];
                            currentUnityParent = dict["unity_parent"].ToString();  // Store unity_parent
                        }
                        else if (dict.ContainsKey("use_jaw_type"))
                        {
                            useKeyValue = "PM_Robot_Tool_TCP_" + (string)dict["use_jaw_type"];
                            currentUnityParent = dict["unity_parent"].ToString();  // Store unity_parent
                        }
                        Debug.Log("Using Tool: " + useKeyValue);
                    }
                }
                else
                {
                    hasUseKey = true;
                    useKeyValue = kvp.Value.ToString();
                    useKey = kvp.Key;
                }
            }

            if (kvp.Key == "unity_parent" && !isTool)
            {
                hasUnityParentKey = true;
                unityParentValue = kvp.Value.ToString();
            }
        }

        // Only handle unity_parent after tool selection is confirmed
        if (booleanToolUsed && !string.IsNullOrEmpty(currentUnityParent))
        {
            Debug.Log("Unity Parent: " + currentUnityParent + " " + useKey + ": " + dict[useKey]);
            GameObject gameObject = GameObject.Find(currentUnityParent);
            List<GameObject> childrenGameObjects = GenericFunctions.getChildrenGameObjects(gameObject);
            List<string> childrenNames = childrenGameObjects.Select(child => child.gameObject.name).ToList();
            componentName = gameObject.name;
            string bestMatch = compareComponentNames(childrenNames, useKeyValue);
            List<GameObject> objectsToHide = childrenGameObjects.Where(child => child.gameObject.name != bestMatch).ToList();
            for (int i = 0; i < objectsToHide.Count; i++)
            {
                name = objectsToHide[i].name;
                if (name.Equals("t_axis_tool"))
                {
                    objectsToHide.RemoveAt(i);
                }
            }
            GenericFunctions.hideGameObjects(objectsToHide);
        }
        else if (hasUseKey && hasUnityParentKey)
        {
            Debug.Log("Unity Parent: " + unityParentValue + " " + useKey + ": " + dict[useKey]);
            GameObject gameObject = GameObject.Find(unityParentValue);
            List<GameObject> childrenGameObjects = GenericFunctions.getChildrenGameObjects(gameObject);
            List<string> childrenNames = childrenGameObjects.Select(child => child.gameObject.name).ToList();
            componentName = gameObject.name;
            string bestMatch = compareComponentNames(childrenNames, useKeyValue);
            List<GameObject> objectsToHide = childrenGameObjects.Where(child => child.gameObject.name != bestMatch).ToList();
            GenericFunctions.hideGameObjects(objectsToHide);
        }

        foreach (var kvp in dict)
        {
            if (kvp.Value is Dictionary<string, object> subDict)
            {
                bool subIsTool = (kvp.Key == "pm_robot_tools" || isTool);
                chooseComponentsFormConfig(ConvertDict(subDict), null, subIsTool);
            }
        }
    }



    Dictionary<string, object> ConvertDict(Dictionary<string, object> oldDict)
    {
        var newDict = new Dictionary<string, object>();

        foreach (var kvp in oldDict)
        {
            if (kvp.Key is string keyStr)
            {
                newDict[keyStr] = kvp.Value;
            }
        }

        return newDict;
    }

    string compareComponentNames(List<string> stringList, string stringToCompare)
    {
        int maxMatches = 0;
        string bestMatch = null;
        List<string> candidates = new List<string>();
        var inputString = new HashSet<string>(stringToCompare.Split("_"));

        foreach (var candidate in stringList)
        {
            var candidateStrings = new HashSet<string>(candidate.Split("_"));
            int matches = inputString.Intersect(candidateStrings).Count();

            if (matches > maxMatches)
            {
                maxMatches = matches;
                bestMatch = candidate;
                candidates.Clear();
                candidates.Add(candidate);
            }
            else if (matches == maxMatches)
            {
                candidates.Add(candidate);
            }
        }
        if (maxMatches == 0)
        {
            Debug.LogError("No similar compontents found for: " + stringToCompare + " under component: " + componentName);
        }

        if (candidates.Count > 1)
        {
            Debug.LogError("Multiple components found for: " + stringToCompare + ": " + candidates + ". Currently selected: " + bestMatch);
        }

        return bestMatch;
    }


}
