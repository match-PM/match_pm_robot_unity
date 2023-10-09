using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ROS2
{
public class UpdateRefFrames : MonoBehaviour
{

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<tf2_msgs.msg.TFMessage> chatter_sub;
    private tf2_msgs.msg.TFMessage recievedRequest;

    bool done = false;

    List<string> alreadyAdded = new List<string>(); //to evade multiples RefFrames an TFs
    


    // Start is called before the first frame update
    void Start()
    {
        // open a Subscriber at /tf topic
        ros2Unity = GetComponent<ROS2UnityComponent>();    
        
        if (ros2Node == null && ros2Unity.Ok())
        {
            Debug.Log("subscribe");
            ros2Node = ros2Unity.CreateNode("ROS2UnityListenerRefFrames");
            chatter_sub = ros2Node.CreateSubscription<tf2_msgs.msg.TFMessage>("/tf", msg => newData(msg)/*, msg => drawNewRefFrames(msg)*/);
        }    
    }

    // Update is called once per frame
    void Update()
    {
        if (!done)
        {
            drawNewRefFrames(recievedRequest);
        }
    }

    // callback
    // is called, when /tf-data has been recieved
    void newData(tf2_msgs.msg.TFMessage incData)
    {
        // save, becaause Find in drawNewRefFrames can only be run on mainThread
        recievedRequest = incData;
        try
        {
            // remove subscription, since we only need one message
            ros2Node.RemoveSubscription<tf2_msgs.msg.TFMessage>(chatter_sub);
            ros2Node = null;
        }
        catch(Exception ex)
        {
            Debug.Log("Exception in newData: " + ex.Message);
        }
    }
    
    // Instatiates the refFrame Prefab at the given tf-Frame
    void drawNewRefFrames(tf2_msgs.msg.TFMessage incomming)
    {
        try{
            done = true; // in case the first Message was broken
            
            geometry_msgs.msg.TransformStamped[] transforms = incomming.Transforms;
            
            foreach (geometry_msgs.msg.TransformStamped ts in transforms)
            {            
                if (!alreadyAdded.Contains(ts.Header.Frame_id))
                {
                    GameObject go = GameObject.Find(ts.Header.Frame_id); // Find TF
                    
                    Instantiate(Resources.Load<GameObject>("Prefabs/RefFrame"), go.transform);  //Append refFrame
                    alreadyAdded.Add(ts.Header.Frame_id);                                
                }
            }
        }
        catch(Exception ex)
        {
            Debug.Log("Exception in drawNewRefFrames: " + ex.Message);
            // get next message and try again
            done = false;
            chatter_sub = ros2Node.CreateSubscription<tf2_msgs.msg.TFMessage>("/tf", msg => newData(msg));
        }
    }

}
}
