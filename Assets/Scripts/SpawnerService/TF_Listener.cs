using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;
using tf2_msgs.msg;
using System.Threading;

[RequireComponent(typeof(ROS2UnityComponent))]
public class TF_Listener : MonoBehaviour
{
    private string tfTopic = "/tf_static";
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    QualityOfServiceProfile tfQosProfile = new QualityOfServiceProfile();
    private ISubscription<TFMessage> tfStaticSub;

    // A struct to hold (childFrame, parentFrame) data
    private struct FramePair
    {
        public string childFrame;
        public string parentFrame;
        public Vector3 childTransform;
        public Quaternion childRotation;
    }

    // Thread-safe buffer of frame pairs awaiting processing
    private List<FramePair> framePairsToProcess = new List<FramePair>();
    private object framesLock = new object();

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        
        // Set up QoS profile for the TF subscription to make sure we get all messages
        tfQosProfile.SetReliability(ReliabilityPolicy.QOS_POLICY_RELIABILITY_RELIABLE);
        tfQosProfile.SetDurability(DurabilityPolicy.QOS_POLICY_DURABILITY_TRANSIENT_LOCAL);

        if (ros2Unity == null)
        {
            Debug.LogError("ROS2UnityComponent not found on this GameObject. Please add it in the Inspector.");
            return;
        }
    }

    void Update()
    {
        // Create the node & subscription once, after ros2Unity is OK
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("Unity_TF_Listener");
            tfStaticSub = ros2Node.CreateSubscription<TFMessage>(
                tfTopic,
                TFCallback,
                tfQosProfile
            );
            Debug.Log($"Subscribed to {tfTopic} and ready to receive transforms.");
        }

        // On the main thread, handle any new (child, parent) pairs.
        lock (framesLock)
        {
            foreach (var fp in framePairsToProcess)
            {
                // Try to find the child GameObject
                GameObject childObj = GameObject.Find(fp.childFrame);
                // Try to find the parent GameObject
                GameObject parentObj = GameObject.Find(fp.parentFrame);


                // check if child name contains "Glue". If so, spawn a empty game object and parent it to the child
                if (fp.childFrame.Contains("Glue") && childObj == null && parentObj != null)
                {
                    GameObject adhesivePoint = new GameObject();
                    adhesivePoint.name = fp.childFrame.ToString();
                    adhesivePoint.tag = "spawned";
                    adhesivePoint.transform.position = fp.childTransform;
                    adhesivePoint.transform.rotation = fp.childRotation;
                    adhesivePoint.transform.SetParent(GameObject.Find(fp.parentFrame).transform, false);
                    Debug.Log($"Spawned {adhesivePoint.name} under {fp.parentFrame}");
                    continue;
                }

                // If the child object is null or not tagged as "spawned", skip it
                if (childObj == null || childObj.tag != "spawned")
                {
                    continue;
                }


                if (parentObj == null)
                {
                    Debug.LogWarning($"Parent GameObject not found: {fp.parentFrame}. Setting no parent.");
                    childObj.transform.SetParent(null, false);
                }
                else if(parentObj.transform == childObj.transform.parent)
                {
                    // Already parented
                    continue;
                }
                else
                {
                    // Re-parent
                    childObj.transform.SetParent(parentObj.transform, true);
                    childObj.transform.localPosition = fp.childTransform;
                    childObj.transform.localRotation = fp.childRotation;
                    Debug.Log($"Re-parented {childObj.name} under {parentObj.name}");
                }
            }
            framePairsToProcess.Clear();
        }
    }

    // This callback fires whenever a new TFMessage arrives on /tf_static
    // (on a background thread!)
    private void TFCallback(TFMessage msg)
    {
        lock (framesLock)
        {
            foreach (var transformStamped in msg.Transforms)
            {
                // The "parent frame" is from the header
                string parentFrame = transformStamped.Header.Frame_id;

                // The "child frame" is from the transform
                string childFrame = transformStamped.Child_frame_id;

                Vector3 position = new(
                    (float)(transformStamped.Transform.Translation.Y * -1),  // ROS Y -> Unity X
                    (float)transformStamped.Transform.Translation.Z,  // ROS Z -> Unity Y
                    (float)transformStamped.Transform.Translation.X   // ROS X -> Unity Z
                );
                Quaternion rotation = new(
                    (float)transformStamped.Transform.Rotation.Y,  // ROS Y -> Unity X
                    (float)transformStamped.Transform.Rotation.Z,  // ROS Z -> Unity Y
                    (float)(transformStamped.Transform.Rotation.X*-1),  // ROS X -> Unity Z
                    (float)transformStamped.Transform.Rotation.W
                );

                // Add to our processing list
                framePairsToProcess.Add(
                    new FramePair { childFrame = childFrame, parentFrame = parentFrame, childRotation = rotation, childTransform = position }
                );
            }
        }
    }
}