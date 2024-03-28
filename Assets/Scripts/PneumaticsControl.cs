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
    ComponentClasses.DriveComponent pneumaticComponent;
    private OPCUA_Client OPCUA_Client;
    private GameObject robotGameObject;
    private chooseMode.Mode mode;
    private int readTarget;
    private int lastMove = 0;
    private int move;
    private int currentState = -1;
    private int lastState = 0;

    private List<OPCUAWriteContainer> containerList;

    void updateDispenserPosition(){
        move = (int) OPCUA_Client.allNodes[gameObject.name + "/" + "MoveCommand"].dataValue.Value;
        if(lastMove != move)
        {
            if(move == 1){
                readTarget = 1;
            }else if(move == -1){
                readTarget = 0;
            }
            pneumaticComponent.move(readTarget, null);
            lastMove = move;
        }
    }

    async void writeState(){
        if(pneumaticComponent.articulationBody.xDrive.target == pneumaticComponent.articulationBody.xDrive.lowerLimit)
        {
            currentState = -1;
        }else{
            currentState = 1;
        }

        if(lastState != currentState)
        { 
            containerList[0].writeValue = new DataValue(currentState);
            await OPCUA_Client.WriteValues(containerList);
            lastState = currentState;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        OPCUA_Client = robotGameObject.GetComponent<OPCUA_Client>();
        mode = robotGameObject.GetComponent<chooseMode>().mode;
        pneumaticComponent = new ComponentClasses.DriveComponent(gameObject);
        if(mode == 0)
        {
            containerList = new List<OPCUAWriteContainer> {new OPCUAWriteContainer(gameObject.name, "Position", new Variant())};
        } 
    }

    // Update is called once per frame
    void Update()
    {   
        if (OPCUA_Client.startUpdate)
        {
            updateDispenserPosition();
            
            if(mode == 0)
            {
                writeState();
            }
        }
            
    }   
}
