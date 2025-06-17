using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;            // ← for Thread.Sleep
using UnityEngine;
using ROS2;

// ROS2 message/service types
using trajectory_msgs.msg;
using builtin_interfaces.msg;

// Alias your service Request/Response types:
using forceGripping       = pm_opcua_skills_msgs.srv.ForceSensingMove_Request;
using forceGrippingResp   = pm_opcua_skills_msgs.srv.ForceSensingMove_Response;

public class OpcUaSkills : MonoBehaviour
{
    private ROS2UnityComponent           ros2Unity;
    private ROS2Node                     ros2Node;

    // Publisher for JointTrajectory
    private IPublisher<JointTrajectory>  jointTrajectoryPub;

    // Service server handle
    private IService<forceGripping, forceGrippingResp> ForceSensingMoveService;

    void Start()
    {
        // 1) Grab the ROS2 bridge component
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity == null || !ros2Unity.Ok())
        {
            Debug.LogError("ROS2UnityComponent not found or not OK");
            return;
        }

        // 2) Create a ROS2 node
        ros2Node = ros2Unity.CreateNode("ROS2UnityForceSensingMoveService");

        // 3) Create a JointTrajectory publisher
        jointTrajectoryPub = ros2Node.CreatePublisher<trajectory_msgs.msg.JointTrajectory>(
            "/pm_robot_xyz_axis_controller/joint_trajectory"
        );

        // 4) Advertise the ForceSensingMove service
        try
        {
            ForceSensingMoveService = ros2Node.CreateService<forceGripping, forceGrippingResp>(
                "/pm_opcua_skills_controller/ForceSensingMove",
                ForceSensingMove
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating ForceSensingMove service: {ex.Message}");
        }
    }

    /// <summary>
    /// Service callback: executes a force‐sensing move from (start_x, start_y, start_z) toward
    /// (target_x, target_y, target_z), stepping by step_size. At each step it reads force
    /// and aborts if any component exceeds (max_fx, max_fy, max_fz).
    /// </summary>
    private forceGrippingResp ForceSensingMove(forceGripping request)
    {
        var response = new forceGrippingResp();
        try
        {
            // 1) Move to the “start” position once (blocking)
            PublishPoint(request.Start_x * 1e-6f, request.Start_y * 1e-6f, request.Start_z * 1e-6f);
            // assume it takes ~1 second to get to the start pos
            Thread.Sleep(1000);

            // 2) Build Vector3s for start/target
            Vector3 start   = new Vector3(request.Start_x * 1e-6f,  request.Start_y * 1e-6f,  request.Start_z * 1e-6f);
            Vector3 target  = new Vector3(request.Target_x * 1e-6f, request.Target_y * 1e-6f, request.Target_z * 1e-6f);
            float   step    = (float)request.Step_size* 1e-6f;

            // Direction (unit) from start → target
            Vector3 direction = (target - start).normalized;
            Vector3 current   = start;

            // 3) Loop until we reach (or zero‐distance) to target
            while (Vector3.Distance(current, target) > 1e-3f)
            {
                // Compute the next step
                Vector3 next = current + direction * step;

                // If “next” overshoots in any axis, clamp to target on that axis
                if ((direction.x > 0f && next.x > target.x) || (direction.x < 0f && next.x < target.x))
                    next.x = target.x;
                if ((direction.y > 0f && next.y > target.y) || (direction.y < 0f && next.y < target.y))
                    next.y = target.y;
                if ((direction.z > 0f && next.z > target.z) || (direction.z < 0f && next.z < target.z))
                    next.z = target.z;

                // 4) Publish one‐step trajectory to “next”
                PublishPoint(
                    Mathf.RoundToInt(next.x),
                    Mathf.RoundToInt(next.y),
                    Mathf.RoundToInt(next.z)
                );

                // Wait for the robot to reach “next”. Adjust duration as needed.
                Thread.Sleep(500);

                // 5) Read force (placeholder stub)
                Vector3 measuredForce = GetCurrentForce();

                // 6) Check thresholds
                if (Mathf.Abs(measuredForce.x) >= request.Max_fx ||
                    Mathf.Abs(measuredForce.y) >= request.Max_fy ||
                    Mathf.Abs(measuredForce.z) >= request.Max_fz)
                {
                    // Abort: force threshold exceeded
                    response.Success            = false;
                    response.Threshold_exceeded = true;
                    response.Error              = ""; // no error string, but could fill if desired
                    return response;
                }

                // 7) Update current; if we reached target on all axes, exit loop
                current = next;
                if (Mathf.Approximately(current.x, target.x) &&
                    Mathf.Approximately(current.y, target.y) &&
                    Mathf.Approximately(current.z, target.z))
                {
                    break;
                }
            }

            // If we exit loop normally, we’ve reached the target without exceeding force
            response.Success            = true;
            response.Threshold_exceeded = false;
            response.Error              = "";
        }
        catch (Exception e)
        {
            // Catch any unexpected exception and report as failure
            response.Success            = false;
            response.Error              = e.Message;
            response.Threshold_exceeded = false;
        }

        return response;
    }

    /// <summary>
    /// Publishes a single-point JointTrajectory to move X/Y/Z joints to (x, y, z).
    /// Each call uses a duration for simplicity.
    /// </summary>
    private void PublishPoint(float x, float y, float z)
    {
        var traj = new JointTrajectory();
        traj.Joint_names = new List<string>()
        {
            "X_Axis_Joint",
            "Y_Axis_Joint",
            "Z_Axis_Joint"
        }.ToArray();

        var point = new JointTrajectoryPoint();
        point.Positions = new List<double> { (double)x, (double)y, (double)z }.ToArray();

        // second from now
        point.Time_from_start = new Duration { Sec = 0, Nanosec = 500000000 };

        traj.Points = new List<JointTrajectoryPoint>() { point }.ToArray();
        jointTrajectoryPub.Publish(traj);

        Debug.Log($"[ForceSensingMove] Published step→ [{x}, {y}, {z}] ");
    }

    /// <summary>
    /// Placeholder stub for reading the current force. In a real implementation,
    /// you’d subscribe to your force‐sensor topic (e.g. geometry_msgs/WrenchStamped)
    /// or query OPC UA for the latest reading.
    /// Here we just return Vector3.zero.
    /// </summary>
    private Vector3 GetCurrentForce()
    {
        // TODO: Replace with actual force-sensor read (e.g. last received ROS2 Wrench message)
        return Vector3.zero;
    }
}
