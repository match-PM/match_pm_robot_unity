using System.Collections.Generic;
using UnityEngine;
using ROS2;
using tf2_msgs.msg;

[RequireComponent(typeof(ROS2UnityComponent))]
public class GlueTFSpawner : MonoBehaviour
{
    private string tfTopic = "/tf_static";
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private QualityOfServiceProfile tfQosProfile = new QualityOfServiceProfile();
    private ISubscription<TFMessage> tfStaticSub;

    // For thread-safe processing
    private struct FramePair
    {
        public string childFrame;
        public string parentFrame;
        public Vector3 childTransform;
        public Quaternion childRotation;
    }
    private List<FramePair> framePairsToProcess = new List<FramePair>();
    private object framesLock = new object();

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        tfQosProfile.SetReliability(ReliabilityPolicy.QOS_POLICY_RELIABILITY_RELIABLE);
        tfQosProfile.SetDurability(DurabilityPolicy.QOS_POLICY_DURABILITY_TRANSIENT_LOCAL);
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("Unity_Glue_TF_Spawner");
            tfStaticSub = ros2Node.CreateSubscription<TFMessage>(
                tfTopic, TFCallback, tfQosProfile
            );
        }

        lock (framesLock)
        {
            foreach (var fp in framePairsToProcess)
            {
                // Only handle Glue frames
                if (!fp.childFrame.Contains("Glue")) continue;

                GameObject parentObj = GameObject.Find(fp.parentFrame);
                if (parentObj != null && GameObject.Find(fp.childFrame) == null)
                {
                    GameObject adhesivePoint = new GameObject(fp.childFrame);
                    adhesivePoint.tag = "spawned";
                    adhesivePoint.transform.position = fp.childTransform;
                    adhesivePoint.transform.rotation = fp.childRotation;
                    adhesivePoint.transform.SetParent(parentObj.transform, false);

                    Debug.Log($"[GlueTFSpawner] Spawned {adhesivePoint.name} under {fp.parentFrame}");
                }
            }
            framePairsToProcess.Clear();
        }
    }

    private void TFCallback(TFMessage msg)
    {
        lock (framesLock)
        {
            foreach (var t in msg.Transforms)
            {
                // Only add frames that contain "Glue"
                if (!t.Child_frame_id.Contains("Glue")) continue;

                Vector3 pos = new(
                    (float)(t.Transform.Translation.Y * -1), // ROS Y -> Unity X
                    (float)t.Transform.Translation.Z,         // ROS Z -> Unity Y
                    (float)t.Transform.Translation.X          // ROS X -> Unity Z
                );
                Quaternion rot = new(
                    (float)t.Transform.Rotation.Y,            // ROS Y -> Unity X
                    (float)t.Transform.Rotation.Z,            // ROS Z -> Unity Y
                    (float)(t.Transform.Rotation.X * -1),     // ROS X -> Unity Z
                    (float)t.Transform.Rotation.W
                );

                framePairsToProcess.Add(new FramePair
                {
                    childFrame = t.Child_frame_id,
                    parentFrame = t.Header.Frame_id,
                    childTransform = pos,
                    childRotation = rot
                });
            }
        }
    }
}
