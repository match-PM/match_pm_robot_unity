using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class setName : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        name = "ReferenceFrame_" + transform.parent.name ;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
