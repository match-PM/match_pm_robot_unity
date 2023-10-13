using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pick_Component : MonoBehaviour
{

    public GameObject targetObject;   // The GameObject to attach to.
    public float attachDistance = 2f; // The distance at which attachment occurs.
    public float attachedZPosition = -0.0567f;

    private bool isAttached = false;

    private void Update()
    {
        // Calculate the distance between this GameObject and the target GameObject.
        float distance = Vector3.Distance(transform.position, targetObject.transform.position);

        // If the distance is less than or equal to the attachDistance and not already attached, attach the GameObject.
        if (distance <= attachDistance && !isAttached)
        {
            AttachToTarget();
        }
    }

    private void AttachToTarget()
    {
        // Attach this GameObject to the target GameObject.
        transform.parent = targetObject.transform;

        // Set the attached GameObject's z position to the specified value.
        Vector3 attachedPosition = transform.localPosition;
        attachedPosition.z = attachedZPosition;
        transform.localPosition = attachedPosition;

        // Mark as attached to prevent further attachment.
        isAttached = true;
    }
}
