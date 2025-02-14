using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;


public class CollisionDetection : MonoBehaviour
{

    private ArticulationBody articulationBody;
    private ArticulationBody sensor;
    private ArticulationBody part;


    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with: " + collision.gameObject);

        Vector3 relativeVelocity = collision.relativeVelocity;
        part = collision.gameObject.GetComponent<ArticulationBody>();

        if (sensor != null && part != null)
        {
            float sensorMass = sensor.mass;

            float velocityChange = relativeVelocity.magnitude;


            float deltaTime = Time.fixedDeltaTime;

            // Calculate the force (approximation)
            float force = (sensorMass * velocityChange) / deltaTime;

            Debug.Log("Collision Force: " + force);
        }
    }

    private void Start()
    {
        sensor = GetComponent<ArticulationBody>();
    }

    // private void FixedUpdate()
    // {
    //     if (articulationBody != null)
    //     {
    //         Vector3 accumulatedForce = articulationBody.GetAccumulatedForce();
    //         Debug.Log("Accumulated Force: " + accumulatedForce.magnitude * 1000000);
    //     }
    // }

}
