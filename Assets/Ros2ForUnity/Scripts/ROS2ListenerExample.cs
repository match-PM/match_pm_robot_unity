// Copyright 2019-2021 Robotec.ai.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;
//using RosMessageTypes.Moveit;

namespace ROS2
{

/// <summary>
/// An example class provided for testing of basic ROS2 communication
/// </summary>
public class ROS2ListenerExample : MonoBehaviour
{
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<sensor_msgs.msg.JointState> chatter_sub;

    //private Double[] position;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("ROS2UnityListenerNode");
            chatter_sub = ros2Node.CreateSubscription<sensor_msgs.msg.JointState>(
              "/joint_states", msg => current_Position(msg.Name, msg.Position));//Debug.Log("Unity listener heard: [" + msg.Position[1] + "]"));

            //Debug.Log(position[1].ToString());
        }
    }

    private void current_Position(String[] name, Double[] position)
    {
        Debug.Log("Joint: " + name[1] + "Position: " + position[1]);
    }
}

}  // namespace ROS2
