using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;

using forceGripping = pm_opcua_skills_msgs.srv.ForceSensingMove_Request;
using forceGrippingResp = pm_opcua_skills_msgs.srv.ForceSensingMove_Response;

public class OpcUaSkills : MonoBehaviour
{

    // Initialize everything for service communication
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<forceGripping, forceGrippingResp> ForceSensingMoveService;
    private bool NewData = false;
    private pm_opcua_skills_msgs.srv.ForceSensingMove_Request recievedRequest;

    // Start is called before the first frame update
    void Start()
    {  
        // Open a node for communication         
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityForceSensingMoveService");

                try
                {
                    ForceSensingMoveService = ros2Node.CreateService<forceGripping, forceGrippingResp>("/pm_opcua_skills_controller/ForceSensingMove", ForceSensingMove);
                }
                catch (System.Exception ex)
                {
                    Debug.Log(ex.InnerException);
                }
            }
        }

    }
    // This function is called when the service is called
    private forceGrippingResp ForceSensingMove(pm_opcua_skills_msgs.srv.ForceSensingMove_Request recievedRequest)
    {
        // Store the request data
        this.recievedRequest = recievedRequest;
        NewData = true;

        // Create a response object
        pm_opcua_skills_msgs.srv.ForceSensingMove_Response response = new pm_opcua_skills_msgs.srv.ForceSensingMove_Response();

        // Call the function to handle the request
        // response = HandleForceSensingMove(recievedRequest, response);

        return response;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
