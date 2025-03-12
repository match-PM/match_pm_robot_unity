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
    private double distance;
    private double distance_prev;
    private bool isInitialized = false;
    private List<OPCUAWriteContainer> containerList;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;
    private RaycastHit hit;

    public GameObject laserPointPrefab;

    private GameObject laserPointInstance;

    // This factor is used to add some tolerance to raycast calculations, because after moving with laser too close to the part the measurement reaches high values.
    private float measureToleranceFactor = 10;
    private const float MinDistance = -0.0003f;
    private const float MaxDistance = 0.0003f;

    private void CalculateDistance()
    {
        distance = transform.position.y - hit.point.y;
        distance = Mathf.Clamp((float)distance, MinDistance, MaxDistance);
        // Debug.Log("Distance: " + distance);
        // Debug.Log("Hit point: " + hit.point);
    }

    void renderLine()
    {
        lineRenderer.SetPosition(0, transform.position);

        // Set the start position of the line renderer to the transform position
        if (Physics.Raycast(transform.position + new Vector3(0.0f, measureToleranceFactor * 0.0003f, 0.0f), -transform.up, out hit))
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
        if (distance != distance_prev)
        {
            // Debug.Log("Distance: " + distance);
            // Create a variant to hold the distance value
            Variant value = new Variant(distance*1000000);

            // Update the previous distance with the current distance
            distance_prev = distance;

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
