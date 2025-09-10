using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using Float64MultiArray = std_msgs.msg.Float64MultiArray;
using System.Data.Common;

public class UvCartController : MonoBehaviour
{

    private string UvCartTopic = "/pm_uv_cart_manual_controller/commands";
    public ArticulationBody uvCartFront;
    public ArticulationBody uvCartBack;

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<Float64MultiArray> uvCartClient;

    // Start is called before the first frame update
    private double[] targetData = null;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("ROS2UnityUvCartNode");
            uvCartClient = ros2Node.CreateSubscription<Float64MultiArray>(
                UvCartTopic,
                UvCartCallback
            );
        }

        // Move the UV cart only when new target data is available
        if (targetData != null)
        {
            var frontDrive = uvCartFront.xDrive;
            frontDrive.target = (float)targetData[0];
            uvCartFront.xDrive = frontDrive;

            var backDrive = uvCartBack.xDrive;
            backDrive.target = (float)targetData[1];
            uvCartBack.xDrive = backDrive;

            targetData = null; // Reset after applying
        }
    }

    private void UvCartCallback(Float64MultiArray msg)
    {
        // Store the received message data for processing in Update
        if (msg.Data != null)
        {
            targetData = new double[2] { msg.Data[0], msg.Data[1] };
        }
    }
}
