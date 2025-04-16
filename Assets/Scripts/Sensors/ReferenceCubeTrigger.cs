using UnityEngine;
using UtilityFunctions.OPCUA;
using Opc.Ua;
using System.Collections;

public class ProbeTrigger : MonoBehaviour
{
    private ArticulationBody probeBody;
    private OPCUA_Client opcuaClient;
    private GameObject robotGameObject;
    public float triggerDistance = 0.0001f; // 0.1 mm in meters
    private float initialY;
    private bool triggered = false;

    IEnumerator Start()
    {
        robotGameObject = GameObject.Find("pm_robot");
        opcuaClient = robotGameObject.GetComponent<OPCUA_Client>();
        probeBody = GetComponent<ArticulationBody>();
        initialY = probeBody.transform.localPosition.y;

        yield return new WaitUntil(() => opcuaClient.IsConnected);

        opcuaClient.addToWriteContainer(gameObject.name, "Pushed");
    }

    void Update()
    {
        if (opcuaClient.updateReady && opcuaClient.IsConnected)
        {
            CheckAndWriteTrigger();
        }
    }

    void CheckAndWriteTrigger()
    {
        float currentY = probeBody.transform.localPosition.y;
        float displacement = Mathf.Abs(currentY - initialY);

        bool shouldBeTriggered = displacement >= triggerDistance;

        // Only write to OPC UA if state changes
        if (shouldBeTriggered != triggered)
        {
            triggered = shouldBeTriggered;
            Debug.Log("Trigger state changed: " + triggered);
            Variant triggerState = new Variant(triggered);
            opcuaClient.writeToServer(gameObject.name, "Pushed", triggerState);
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name);
    }

}
