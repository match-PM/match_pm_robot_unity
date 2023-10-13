using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class chooseMode : MonoBehaviour
{ 
    public enum Mode
    {
        Simulation = 0, 
        Visualization = 1
    };

    public Mode mode;

    void Start(){
        Debug.Log("Mode: " + mode);
    }
}
