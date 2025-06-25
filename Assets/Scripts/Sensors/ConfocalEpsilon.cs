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
        private LineRenderer lineRenderer;
        private double distance;
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
        private assembly_manager_interfaces_unity.srv.SpawnObjectUnity_Request recievedRequest;
        public string NodeName = "ROS2UnityConfocalEpsilonNode";
        public string ServiceName = "/pm_uepsilon_confocal/get_value";
        public string PublisherName = "/pm_uepsilon_confocal/value";
        private const float measureToleranceFactor = 0.0003f; // This factor is used to add some tolerance to raycast calculations, because after moving with laser too close to the part the measurement reaches high values.
        public const float WorkingDistance = 0.041f; // The distance at which the confocal sensor is calibrated to work, in meters (41mm).
        public const float MaxDistanceFromWorkingDistance = 0.006f; 
        public const float MinDistance = -0.003f;
        public const float MaxDistance = 0.003f;


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

            if (distance != distance_prev)
            {
                // If the distance has changed, update the previous distance
                distance_prev = distance;

                measurement.Data = distance * 1000000; // Convert to micrometers
            }

            return measurement;
        }

        void renderLine()
        {
            lineRenderer.SetPosition(0, transform.position);

            // Set the start position of the line renderer to the transform position
            if (Physics.Raycast(transform.position + new Vector3(0.0f, measureToleranceFactor, 0.0f), -transform.up, out hit))
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
                lineRenderer.SetPosition(1, -transform.up * 5000);
                if (laserPointInstance != null)
                {
                    laserPointInstance.SetActive(false);
                }
            }
        }

        private void CalculateDistance()
        {
            distance = transform.position.y - hit.point.y;
            distance = Mathf.Clamp((float)distance, MinDistance, MaxDistance);
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
