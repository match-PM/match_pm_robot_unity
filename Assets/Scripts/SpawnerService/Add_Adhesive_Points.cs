using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ROS2;
using tf2_msgs.msg;
using System.Threading;
using addAdhesivePointsMsg = pm_msgs.msg.VizAdhesivePoints;

[RequireComponent(typeof(ROS2UnityComponent))]
public class Add_Adhesive_Points : MonoBehaviour
{
    private string addAdhesivePointsTopic = "/pm_adhesive_displayer/adhesive_points";
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    QualityOfServiceProfile tfQosProfile = new QualityOfServiceProfile();
    private ISubscription<addAdhesivePointsMsg> addAdhesivePointsSub;
    
    public GameObject adhesivePointPrefab;

    // A struct to hold (childFrame, parentFrame) data
    private struct AddAdhesivePoint
    {
        public string pointName;
        public string parentFrame;
        public Vector3 Point_pose;
    }

    // Thread-safe buffer of frame pairs awaiting processing
    private List<AddAdhesivePoint> adhesivePointsToProcess = new List<AddAdhesivePoint>();
    private object framesLock = new object();

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        
        // Set up QoS profile for the TF subscription to make sure we get all messages
        // tfQosProfile.SetReliability(ReliabilityPolicy.QOS_POLICY_RELIABILITY_RELIABLE);
        // tfQosProfile.SetDurability(DurabilityPolicy.QOS_POLICY_DURABILITY_TRANSIENT_LOCAL);

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
            ros2Node = ros2Unity.CreateNode("Unity_AdhesivePoints_Listener");
            addAdhesivePointsSub = ros2Node.CreateSubscription<addAdhesivePointsMsg>(
                addAdhesivePointsTopic,
                MsgCallback
            );
            Debug.Log($"Subscribed to {addAdhesivePointsTopic} and ready to receive transforms.");
        }

        // On the main thread, handle any new (child, parent) pairs.
        lock (framesLock)
        {
            foreach (var ap in adhesivePointsToProcess)
            {
                // If pointName is already in the scene, skip it
                if (GameObject.Find(ap.pointName) != null)
                {   
                    continue;
                }
                // Try to find the parent GameObject
                GameObject parentObj = GameObject.Find(ap.parentFrame);

                // If the child object is null or not tagged as "spawned", skip it
                if (parentObj == null)
                {
                    Debug.LogWarning($"Parent object {ap.parentFrame} not found.");
                    continue;
                }
                else
                {
                    // Add the adhesivePointPrefab to the parentObj
                    GameObject adhesivePoint = Instantiate(adhesivePointPrefab, parentObj.transform);
                    adhesivePoint.transform.localPosition = ap.Point_pose;
                }
            }
            adhesivePointsToProcess.Clear();
        }
    }

    // This callback fires whenever a new TFMessage arrives on /tf_static
    // (on a background thread!)
    private void MsgCallback(addAdhesivePointsMsg msg)
    {
        lock (framesLock)
        {
            foreach (var point in msg.Points)
            {
                // The "parent frame" is from the point
                string parentFrame = point.Parent_frame;
                string pointName = point.Name;

                // get point_pose from the point (point_pose is a geometry_msgs/Pose) transfrom it to Unity Pose
                Vector3 position = new((float)point.Point_pose.Position.X, (float)point.Point_pose.Position.Y, (float)point.Point_pose.Position.Z);

                // Add to our processing list
                adhesivePointsToProcess.Add(
                    new AddAdhesivePoint { pointName = pointName, parentFrame = parentFrame , Point_pose = position }
                );
            }
        }
    }
}
