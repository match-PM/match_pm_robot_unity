using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Linq;
using UtilityFunctions;
using UtilityFunctions.OPCUA;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

public class LaserRay : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float distance;
    private float calc_distance;
    private float distance_prev;
    private bool isInitialized = false;
    private List<OPCUAWriteContainer> containerList;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;
    private RaycastHit hit;

    public GameObject laserPointPrefab;

    private GameObject laserPointInstance;

    // This factor is used to add some tolerance to raycast calculations, because after moving with laser too close to the part the measurement reaches high values.
    private float measureToleranceFactor = 10e-6f; 
    private const float MinDistance = -0.0003f;
    private const float MaxDistance = 0.0003f;
    private const float startPositionOffset = 0.001f;
    private float zeroOffset;

    private void CalculateDistance()
    {

        distance = hit.point.y - transform.position.y;

        // Debug.Log("Raw Distance: " + distance*1e6);
        // deviation from our “zero”:
        float centered = distance + startPositionOffset;
        // Debug.Log("Centered Distance: " + centered);
        calc_distance = Mathf.Clamp((float)centered, MinDistance, MaxDistance);
        // Debug.Log("Clamped Distance: " + calc_distance*1e6);
        // Debug.Log("Distance: " + distance);
        // Debug.Log("Hit point: " + hit.point);
    }

    void renderLine()
    {
        lineRenderer.SetPosition(0, transform.position);

        // Set the start position of the line renderer to the transform position
        if (Physics.Raycast(transform.position + new Vector3(0.0f, 0.0f, 0.0f), -transform.up, out hit))
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

    void writeLaserDistance()
    {
        CalculateDistance();
        // Check if the distance has changed since the previous measurement
        if (System.Math.Abs(calc_distance - distance_prev) > float.Epsilon) // Use a small epsilon for float/double comparisons
        {
            Variant value = new Variant(Convert.ToDouble(calc_distance) * 1000000.0); 

            // Update the previous distance with the current distance
            distance_prev = calc_distance;
            Debug.Log("Writing Distance: " + value);

            // Write the distance measurement to the OPC UA server
            OPCUA_Client.writeToServer(gameObject.name, "Measurement", value);
        }
    }


    IEnumerator Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.001f;
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;


        if (laserPointPrefab != null)
        {
            laserPointInstance = Instantiate(laserPointPrefab);
            laserPointInstance.SetActive(false);
        }
        else
        {
            Debug.LogError("Laser point prefab is not set!");
        }

        yield return new WaitUntil(() => OPCUA_Client.IsConnected);

        // move the laser 1mm up
        // transform.position = new Vector3(transform.position.x, transform.position.y + startPositionOffset, transform.position.z);

        if (mode == 0)
        {
            OPCUA_Client.addToWriteContainer(gameObject.name, "Measurement");
        }

        isInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        renderLine();

        if (isInitialized && mode == 0 && OPCUA_Client.updateReady && OPCUA_Client.IsConnected)
        {
            writeLaserDistance();
        }
    }
}
