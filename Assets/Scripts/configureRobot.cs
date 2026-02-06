using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UtilityFunctions; // GetConfigFilePath, GenericFunctions.YamlLoader

public class configureRobot : MonoBehaviour
{
    private Dictionary<string, object> dictionary;
    private string componentName = null;

    void Start()
    {
        string filepath = GetConfigFilePath("pm_robot_bringup", "pm_robot_bringup_config.yaml");
        dictionary = GenericFunctions.YamlLoader.LoadYaml(filepath);

        if (dictionary != null)
        {
            chooseComponentsFormConfig(dictionary);
        }
        else
        {
            Debug.LogError("Error loading yaml file: " + filepath);
        }
    }

    void Update() { }

    void chooseComponentsFormConfig(Dictionary<string, object> dict, string parent = null, bool isTool = false)
    {
        bool hasUseKey = false;
        string useKeyValue = null;
        string useKey = null;

        bool hasUnityParentKey = false;
        string unityParentValue = null;

        // Tool-mode selection
        bool booleanToolUsed = false;
        string currentUnityParent = null;
        string selectedToolSuffix = null;

        // Scan keys in this dictionary
        foreach (var kvp in dict)
        {
            if (isTool)
            {
                // Enabled flag for a tool: any use_* boolean == true
                if (kvp.Key.StartsWith("use_") && kvp.Value is bool b && b)
                {
                    booleanToolUsed = true;
                }

                // Cache unity_parent if present
                if (kvp.Key == "unity_parent" && kvp.Value != null)
                {
                    currentUnityParent = kvp.Value.ToString();
                }
            }
            else
            {
                if (kvp.Key.StartsWith("use_"))
                {
                    hasUseKey = true;
                    useKeyValue = kvp.Value?.ToString();
                    useKey = kvp.Key;
                }

                if (kvp.Key == "unity_parent")
                {
                    hasUnityParentKey = true;
                    unityParentValue = kvp.Value?.ToString();
                }
            }
        }

        // In tool mode: pick the Unity child by use_tool first (fixes use_paralell_gripper),
        // then fall back to use_tip/use_jaw_type if use_tool is missing.
        if (isTool && booleanToolUsed)
        {
            if (dict.TryGetValue("use_tool", out var toolObj) && toolObj != null)
                selectedToolSuffix = toolObj.ToString();
            else if (dict.TryGetValue("use_tip", out var tipObj) && tipObj != null)
                selectedToolSuffix = tipObj.ToString();
            else if (dict.TryGetValue("use_jaw_type", out var jawObj) && jawObj != null)
                selectedToolSuffix = jawObj.ToString();

            if (!string.IsNullOrEmpty(selectedToolSuffix) && !string.IsNullOrEmpty(currentUnityParent))
            {
                // Keep your existing naming convention
                useKeyValue = "PM_Robot_Tool_TCP_" + selectedToolSuffix;
            }
        }

        // Only handle unity_parent after tool selection is confirmed
        if (isTool && booleanToolUsed && !string.IsNullOrEmpty(currentUnityParent) && !string.IsNullOrEmpty(useKeyValue))
        {
            GameObject gameObject = GameObject.Find(currentUnityParent);
            if (gameObject == null)
            {
                Debug.LogError($"Unity parent '{currentUnityParent}' not found for tool selection '{useKeyValue}'.");
            }
            else
            {
                List<GameObject> childrenGameObjects = GenericFunctions.getChildrenGameObjects(gameObject);
                List<string> childrenNames = childrenGameObjects.Select(child => child.gameObject.name).ToList();

                componentName = gameObject.name;
                string bestMatch = compareComponentNames(childrenNames, useKeyValue);

                if (!string.IsNullOrEmpty(bestMatch))
                {
                    List<GameObject> objectsToHide = childrenGameObjects.Where(child => child.gameObject.name != bestMatch).ToList();

                    // Do not hide structural nodes
                    for (int i = objectsToHide.Count - 1; i >= 0; i--)
                    {
                        string childName = objectsToHide[i].name;
                        if (childName == "t_axis_tool" || childName == "Visuals" || childName == "Collisions")
                        {
                            objectsToHide.RemoveAt(i);
                        }
                    }

                    GenericFunctions.hideGameObjects(objectsToHide);
                }
            }
        }
        else if (!isTool && hasUseKey && hasUnityParentKey && !string.IsNullOrEmpty(unityParentValue) && !string.IsNullOrEmpty(useKeyValue))
        {
            GameObject gameObject = GameObject.Find(unityParentValue);
            if (gameObject == null)
            {
                Debug.LogError($"Unity parent '{unityParentValue}' not found for selection '{useKeyValue}'.");
            }
            else
            {
                List<GameObject> childrenGameObjects = GenericFunctions.getChildrenGameObjects(gameObject);
                List<string> childrenNames = childrenGameObjects.Select(child => child.gameObject.name).ToList();

                componentName = gameObject.name;
                string bestMatch = compareComponentNames(childrenNames, useKeyValue);

                if (!string.IsNullOrEmpty(bestMatch))
                {
                    List<GameObject> objectsToHide = childrenGameObjects.Where(child => child.gameObject.name != bestMatch).ToList();
                    GenericFunctions.hideGameObjects(objectsToHide);
                }
            }
        }

        // Recurse into sub-dictionaries
        foreach (var kvp in dict)
        {
            if (kvp.Value is Dictionary<string, object> subDict)
            {
                bool subIsTool = (kvp.Key == "pm_robot_tools" || isTool);
                chooseComponentsFormConfig(ConvertDict(subDict), null, subIsTool);
            }
            // If your YAML loader returns Dictionary<object, object> for nested dicts,
            // you can also handle it here if needed.
            else if (kvp.Value is Dictionary<object, object> objSubDict)
            {
                bool subIsTool = (kvp.Key == "pm_robot_tools" || isTool);
                chooseComponentsFormConfig(ConvertDictObject(objSubDict), null, subIsTool);
            }
        }
    }

    Dictionary<string, object> ConvertDict(Dictionary<string, object> oldDict)
    {
        var newDict = new Dictionary<string, object>();
        foreach (var kvp in oldDict)
        {
            newDict[kvp.Key] = kvp.Value;
        }
        return newDict;
    }

    Dictionary<string, object> ConvertDictObject(Dictionary<object, object> oldDict)
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
        if (stringList == null || stringList.Count == 0 || string.IsNullOrEmpty(stringToCompare))
            return null;

        // Prefer exact match first (most reliable)
        var exact = stringList.FirstOrDefault(s => s == stringToCompare);
        if (!string.IsNullOrEmpty(exact))
            return exact;

        int maxMatches = 0;
        string bestMatch = null;
        List<string> candidates = new List<string>();

        var inputString = new HashSet<string>(stringToCompare.Split('_'));

        foreach (var candidate in stringList)
        {
            var candidateStrings = new HashSet<string>(candidate.Split('_'));
            int matches = inputString.Intersect(candidateStrings).Count();

            if (matches > maxMatches)
            {
                maxMatches = matches;
                bestMatch = candidate;
                candidates.Clear();
                candidates.Add(candidate);
            }
            else if (matches == maxMatches && matches > 0)
            {
                candidates.Add(candidate);
            }
        }

        if (maxMatches == 0)
        {
            Debug.LogError("No similar components found for: " + stringToCompare + " under component: " + componentName);
            return null;
        }

        // Tie-breakers
        if (candidates.Count > 1)
        {
            int targetLength = inputString.Count;
            var sameLengthCandidates = candidates.Where(c => c.Split('_').Length == targetLength).ToList();

            if (sameLengthCandidates.Count == 1)
            {
                bestMatch = sameLengthCandidates[0];
                candidates = sameLengthCandidates;
            }
            else if (sameLengthCandidates.Count > 1)
            {
                // Check for exact match among same-length candidates (case-insensitive as last resort)
                var exactIgnoreCase = sameLengthCandidates.FirstOrDefault(c =>
                    string.Equals(c, stringToCompare, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(exactIgnoreCase))
                {
                    bestMatch = exactIgnoreCase;
                    candidates = new List<string> { exactIgnoreCase };
                }
            }
        }

        if (candidates.Count > 1)
        {
            Debug.LogError("Multiple components found for: " + stringToCompare +
                           ": " + string.Join(", ", candidates) +
                           ". Currently selected: " + bestMatch);
        }

        return bestMatch;
    }

    public static string GetROS2PackagePath(string packageName)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"source /opt/ros/humble/setup.bash && ros2 pkg prefix " + packageName + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new System.Diagnostics.Process { StartInfo = psi })
            {
                process.Start();
                string packagePath = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(packagePath))
                {
                    return packagePath;
                }
                else
                {
                    Console.WriteLine($"Error finding ROS2 package '{packageName}': {process.StandardError.ReadToEnd()}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception while retrieving ROS2 package path: " + ex.Message);
            return null;
        }
    }

    public static string GetConfigFilePath(string packageName, string fileName)
    {
        string packagePath = GetROS2PackagePath(packageName);
        if (!string.IsNullOrEmpty(packagePath))
        {
            return Path.Combine(packagePath, "share", packageName, "config", fileName);
        }
        return null;
    }
}
