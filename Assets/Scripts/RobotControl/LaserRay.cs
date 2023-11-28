using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserRay : MonoBehaviour
{
    private LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0, transform.position);
        RaycastHit hit;

        if(Physics.Raycast(transform.position, -transform.up, out hit))
        {
            if(hit.collider)
            {
                lineRenderer.SetPosition(1, hit.point);
            }
        }
        else
        {
            lineRenderer.SetPosition(1, -transform.up*5000);
        }
    }
}
