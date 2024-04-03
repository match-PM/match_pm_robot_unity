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
    private List<OPCUAWriteContainer> containerList;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;
    private RaycastHit hit;


    void renderLine()
    {
        lineRenderer.SetPosition(0, transform.position);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, -transform.up, out hit))
        {
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

    async void writeLaserDistance()
    {
        distance = hit.point[0] - transform.position[0];
        if (distance != distance_prev)
        {
            containerList[0].writeValue = new DataValue(distance);
            distance_prev = distance;
            await OPCUA_Client.WriteValues(containerList);
        }
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;
        if (mode == 0)
        {
            containerList = new List<OPCUAWriteContainer> { new OPCUAWriteContainer(gameObject.name, "Measurement", new Variant()) };
        }
    }

    // Update is called once per frame
    void Update()
    {
        renderLine();

        if (mode == 0 && OPCUA_Client.updateReady)
        {
            writeLaserDistance();
        }
    }
}
