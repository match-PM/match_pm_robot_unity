using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enableArticualtionBodies : MonoBehaviour
{
    public bool ArticualtionBodies = false;

    void Start()
    {
        foreach (ArticulationBody ar in GetComponentsInChildren<ArticulationBody>())
            ar.enabled = ArticualtionBodies;
    }
}
