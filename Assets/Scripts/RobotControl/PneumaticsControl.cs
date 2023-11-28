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

public class PneumaticsControl : MonoBehaviour
{
    ComponentClasses.DriveComponent dispenser;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;
    private bool moveForward;
    private bool lastMoveForward;
    private bool moveBackward;
    private bool readTarget;
    private bool lastMoveBackward;
    private bool isForward = false;
    private bool isBackward = false;
    private List<OPCUAWriteContainer> containerList;

    void updateDispenserPosition(){
        moveForward = (bool) OPCUA_Client.allNodes[gameObject.name + "/" + "MoveForwardCmd"].dataValue.Value;

        moveBackward = (bool) OPCUA_Client.allNodes[gameObject.name + "/" + "MoveBackwardCmd"].dataValue.Value;

        if((moveForward == true && lastMoveForward != moveForward) || (moveBackward == true && lastMoveBackward != moveBackward))
        {
            readTarget = moveForward == true ? true : false;
            dispenser.move(Convert.ToInt32(readTarget), null);
        }

        lastMoveForward = moveForward;
        lastMoveBackward = moveBackward;
    }

    async void writeState(){
        if(dispenser.articulationBody.jointPosition[0] != dispenser.articulationBody.xDrive.lowerLimit)
        {
            isForward = true;
            isBackward = false;
        }
        else if(dispenser.articulationBody.jointPosition[0] == dispenser.articulationBody.xDrive.lowerLimit)
        {
            isForward = false;
            isBackward = true;
        }

        containerList[0].writeValue = new DataValue(isForward);
        containerList[1].writeValue = new DataValue (isBackward);
        await OPCUA_Client.WriteValues(containerList);
    }


    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;
        dispenser = new ComponentClasses.DriveComponent(gameObject);
        if(mode == 0)
        {
            containerList = new List<OPCUAWriteContainer> {new OPCUAWriteContainer(gameObject.name, "IsForward", new Variant(isForward)), new OPCUAWriteContainer(gameObject.name, "IsBackward", new Variant(isBackward))};
        } 
    }

    // Update is called once per frame
    void Update()
    {   
        if (OPCUA_Client.startUpdate)
        {
            if(mode == 0)
            {
                writeState();
            }
            updateDispenserPosition();
        }
            
    }   
}
