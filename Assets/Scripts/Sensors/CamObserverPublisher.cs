using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ROS2
{

    /// <summary>
    /// This script publishes the camera image to a ros2 topic
    /// </summary>
    public class CamObserverPublisher : MonoBehaviour
    {
        // Start is called before the first frame update
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private IPublisher<sensor_msgs.msg.Image> cam_pub;

        private Camera cam_1;

        public string nodeName = "ROS2UnityCam1Publisher";
        public string topicName = "Image_Cam1_raw";
        
        [Tooltip("Sensor image width in pixels")]
        public int sensorWidth = 1280;
        [Tooltip("Sensor image height in pixels")]
        public int sensorHeight = 720;

        private AsyncImagePublisher capture;

        // Throttle
        [Tooltip("Seconds between published frames (default 0.1 = 10 fps)")]
        public float publishInterval = 0.1f;
        private float timeSinceLastCapture;

        void Start()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
            cam_1 = GetComponent<Camera>();
            // Spread the cameras over different frames instead of capturing all at once.
            timeSinceLastCapture = Random.Range(0f, publishInterval);
        }

        void Update()
        {
            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode(nodeName);
                    // Use sensor-data QoS (BEST_EFFORT) to match the vision node's
                    // subscription (qos_profile_sensor_data) and avoid the reliable
                    // writer/reader matching handshake.
                    cam_pub = ros2Node.CreateSensorPublisher<sensor_msgs.msg.Image>(topicName);
                }
            }

        }

        // The capture renders the camera explicitly, so it does not need to wait for the
        // engine's own render pass. It used to yield on WaitForEndOfFrame, which in the Editor
        // only resumes while the Game view tab is actually being repainted: with the tab hidden
        // the coroutine never resumed, the in-flight guard stayed latched and the publisher went
        // silent for the rest of the session without logging anything. The capture is started
        // directly in LateUpdate (after all transforms are final) instead; AsyncImagePublisher
        // completes it off the main thread, and its in-flight guard times out rather than latching.
        void LateUpdate()
        {
            timeSinceLastCapture += Time.deltaTime;
            if (timeSinceLastCapture < publishInterval)
            {
                return;
            }

            if (cam_pub == null || cam_1 == null)
            {
                return;
            }

            // Skip publishing once the ROS context is gone (e.g. while leaving Play mode),
            // otherwise ros2cs logs "Cannot publish as the class is already disposed".
            if (!ros2Unity.Ok())
            {
                return;
            }

            // The rendered image is bottom-up; the observer image is published top-down.
            capture ??= new AsyncImagePublisher(cam_1, sensorWidth, sensorHeight, true, cam_pub, ros2Unity);
            if (capture.TryCapture())
            {
                timeSinceLastCapture = 0f;
            }
        }

        void OnDestroy()
        {
            capture?.Dispose();
        }

    }

}  // namespace ROS2
