using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using valueReq = pm_uepsilon_confocal_msgs.srv.GetValue_Request;
using valueResp = pm_uepsilon_confocal_msgs.srv.GetValue_Response;
using std_msgs.msg;


namespace ROS2
{
    public class ConfocalEpsilon : MonoBehaviour
    {
        public enum LaserDirection { Down, Up, Left, Right, Forward, Back }
        public LaserDirection Direction = LaserDirection.Down;

        private LineRenderer lineRenderer;
        private double distance;
        private double calc_distance;
        private double distance_prev;
        private GameObject robotGameObject;
        private chooseMode.Mode mode;
        private RaycastHit hit;

        public GameObject laserPointPrefab;

        private GameObject laserPointInstance;
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private IService<valueReq, valueResp> GetValueService;
        private IPublisher<std_msgs.msg.Float64> valuePublisher;
        private bool ros2Initialized = false;
        public string NodeName = "ROS2UnityConfocalEpsilonNode";
        public string ServiceName = "/pm_uepsilon_confocal/get_value";
        public string PublisherName = "/pm_uepsilon_confocal/value";
        private const float startPositionOffset = 0.003f;

        private Vector3 GetRayDirection()
        {
            switch (Direction)
            {
                case LaserDirection.Up:      return transform.up;
                case LaserDirection.Down:    return -transform.up;
                case LaserDirection.Left:    return -transform.right;
                case LaserDirection.Right:   return transform.right;
                case LaserDirection.Forward: return transform.forward;
                case LaserDirection.Back:    return -transform.forward;
                default:                     return -transform.up;
            }
        }
        private const float MinDistance = -0.0006f;
        private const float MaxDistance = 0.0006f;


        // Start is called before the first frame update
        void Start()
        {
            // Initialize the line renderer
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = 0.001f;
            robotGameObject = GameObject.Find("pm_robot");

            if (laserPointPrefab != null)
            {
                // Create an instance of the laser point prefab
                laserPointInstance = Instantiate(laserPointPrefab);
                laserPointInstance.SetActive(false);
            }
            else
            {
                Debug.LogError("Laser point prefab is not set.");
            }

            // Open a node for communication         
            ros2Unity = GetComponent<ROS2UnityComponent>();
            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode(NodeName);

                    try
                    {
                        GetValueService = ros2Node.CreateService<valueReq, valueResp>(ServiceName, GetValue);
                        valuePublisher = ros2Node.CreatePublisher<std_msgs.msg.Float64>(PublisherName);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.InnerException);
                    }
                }
            }

            // Move the laser object in the opposite laser direction by startPositionOffset
            transform.position -= GetRayDirection() * startPositionOffset;

            ros2Initialized = true;
        }

        // Callback function which is executed on incomming data
        public valueResp GetValue(valueReq req)
        {
            valueResp response = new valueResp();

            response.Data = GetMeasurement().Data;
            response.Success = true;
            return response;
        }

        Float64 GetMeasurement()
        {
            Float64 measurement = new Float64();
            measurement.Data = calc_distance * 1000000; // Convert to micrometers
            return measurement;
        }

        void renderLine()
        {
            lineRenderer.SetPosition(0, transform.position);

            // Set the start position of the line renderer to the transform position
            Vector3 rayDir = GetRayDirection();
            if (Physics.Raycast(transform.position, rayDir, out hit))
            {
                // If a collider is hit, set the end position of the line renderer to the hit point
                if (hit.collider)
                {
                    lineRenderer.SetPosition(1, hit.point);

                    if (laserPointInstance != null)
                    {
                        laserPointInstance.transform.position = hit.point;
                        laserPointInstance.transform.rotation = Quaternion.LookRotation(hit.normal);
                        laserPointInstance.SetActive(true);
                    }
                }
            }
            else
            {
                lineRenderer.SetPosition(1, rayDir * 5000);
                if (laserPointInstance != null)
                {
                    laserPointInstance.SetActive(false);
                }
            }
        }

        private void CalculateDistance()
        {
            // Project hit distance onto the ray direction axis
            distance = Vector3.Dot(hit.point - transform.position, GetRayDirection());
            // Deviation from zero (TCP position):
            // Debug.Log($"name of the hit object: {hit.collider.gameObject.name}");
            double centered = distance - startPositionOffset;
            calc_distance = Mathf.Clamp((float)centered, MinDistance, MaxDistance);
            // Debug.Log($"distance: {distance*1e6} µm, centered: {centered*1e6} µm, calc_distance: {calc_distance*1e6} µm");
        }

        // Update is called once per frame
        void Update()
        {

            if (!ros2Initialized)
            {
                return;
            }
            else
            {
                renderLine();

                CalculateDistance();

                valuePublisher.Publish(GetMeasurement());
            }
        }

    }
}
