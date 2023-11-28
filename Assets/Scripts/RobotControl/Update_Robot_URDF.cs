using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script subscribes to the /robot_description and deactivate all components, that are not
/// mentioned in the /robot_description. The robot model in Unity has all components of the match_pm_robot active by default.
/// </summary>

namespace ROS2
{
public class Update_Robot_URDF : MonoBehaviour
{
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;

    private ISubscription<std_msgs.msg.String> robot_description_sub;

    XmlDocument xmlDoc = new XmlDocument();

    private List<GameObject> childrenList = new List<GameObject>();
    private List<string> linkNames = new List<string>();

    private String robot_description;

    private Boolean robot_description_updated;

    // Start is called before the first frame update
    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        
        // Get the reference to the parent GameObject
        GameObject parentObject = gameObject; 
 
        // Save all children into the list
        SaveChildren(parentObject.transform);



    }

    // Update is called once per frame
    void Update()
    {
 
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("ROS2UnityListenerNode_RD");

            robot_description_sub = ros2Node.CreateSubscription<std_msgs.msg.String>(
                "/robot_description", msg => publishRobotDescription(msg.Data));

        }

        if(robot_description != null && robot_description_updated == true)
        {
            // Disable the objects in the childrenList that do not match the linkName
            foreach (GameObject child in childrenList)
            {
                string childName = child.name;
                child.SetActive(true);
                if (!linkNames.Contains(childName) && childName != "world" && childName != "base_link_empthy")
                {
                    child.SetActive(false);
                    Debug.Log("Disabled child object: " + childName);
                }
            
            }
            robot_description_updated = false;
            // enabled = false;
        }
    }

    void publishRobotDescription(String robot_description_)
    {
        robot_description = robot_description_;
        xmlDoc.LoadXml(robot_description_);

        XmlNodeList linkNodes = xmlDoc.SelectNodes("//link");

        foreach (XmlNode linkNode in linkNodes)
        {
            XmlAttribute nameAttribute = linkNode.Attributes["name"];
            if (nameAttribute != null)
            {
                string linkName = nameAttribute.Value;
                linkNames.Add(linkName);
                //Debug.Log("Link Name: " + linkName);
            }
        }
        robot_description_updated = true;
        Debug.Log("updated robot description");
    }

    void SaveChildren(Transform parentTransform)
    {
        foreach (Transform childTransform in parentTransform)
        {
            GameObject childObject = childTransform.gameObject;

            // Exclude objects named "Collision, Visuals etc." and their children
            if (childObject.name != "Collisions" && childObject.name != "Visuals" && childObject.name != "unnamed" && childObject.name != "Plugins")
            {
                childrenList.Add(childObject);

                // Recursively save the children of the current child object
                SaveChildren(childTransform);
            }
        }
    }

}
}
