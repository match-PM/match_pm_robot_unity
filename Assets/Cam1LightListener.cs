using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ROS2
{

/// <summary>
/// 
/// </summary>
public class Cam1LightListener : MonoBehaviour
{
    // Start is called before the first frame update
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<std_msgs.msg.Bool> cam1RingLight_sub; // check if msg type correspondes to publisher data type
    private ISubscription<std_msgs.msg.Int16MultiArray> cam1RingLightColor_sub; // check if msg type correspondes to publisher data type
    
    private Light cam1RingLight;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        cam1RingLight = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            
            ros2Node = ros2Unity.CreateNode("Cam1LightListener");
            cam1RingLight_sub = ros2Node.CreateSubscription<std_msgs.msg.Bool>("", msg=>updateLightState(msg)); // add publihser name 
            cam1RingLightColor_sub = ros2Node.CreateSubscription<std_msgs.msg.Int16MultiArray>("", msg=>updateLightColor(msg)); // add publihser name 
        }
    }

    public void updateLightColor(std_msgs.msg.Int16MultiArray msg)
    {
        cam1RingLight.color = new Color(msg.Data[0]/100, msg.Data[1]/100, msg.Data[2]/100);
    }

    public void  updateLightState(std_msgs.msg.Bool msg)
    {
        if (msg.Data == true){
            cam1RingLight.intensity = 1;
        }
        else
        {
            cam1RingLight.intensity = 0;
        }
    }
}

}
