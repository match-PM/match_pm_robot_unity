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


    void renderLine()
    {
        lineRenderer.SetPosition(0, transform.position);
        RaycastHit hit;

        // Set the start position of the line renderer to the transform position
        if (Physics.Raycast(transform.position, -transform.up, out hit))
        {
            // If a collider is hit, set the end position of the line renderer to the hit point
            if (hit.collider)
            {
                lineRenderer.SetPosition(1, hit.point);
            }
        }
        else
        {
            lineRenderer.SetPosition(1, -transform.up * 5000);
        }
    }

    void writeLaserDistance()
    {
        // Calculate the distance between the hit point and the transform position
        distance = hit.point[0] - transform.position[0];
        // Check if the distance has changed since the previous measurement
        if (distance != distance_prev)
        {
            // Create a variant to hold the distance value
            Variant value = new Variant(distance);

            // Update the previous distance with the current distance
            distance_prev = distance;

            // Write the distance measurement to the OPC UA server
            OPCUA_Client.writeToServer(gameObject.name, "Measurement", value);
        }
    }

    IEnumerator Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;
        
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
