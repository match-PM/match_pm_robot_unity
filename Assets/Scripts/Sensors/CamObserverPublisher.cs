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

        private RenderTexture renderTexture;

        private Texture2D mainCameraTexture;
        private Rect frame;


        private int frame_width;
        private int frame_height;
        private uint image_step = 4;

        private sensor_msgs.msg.Image msg = new sensor_msgs.msg.Image();

        // Throttle
        [Tooltip("Seconds between published frames (default 0.1 = 10 fps)")]
        public float publishInterval = 0.1f;
        private float timeSinceLastCapture = 0f;

        void Start()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
            cam_1 = GetComponent<Camera>();
            
            // Initialize render texture and camera messaging
            InitializeCamera();
        }

        void InitializeCamera()
        {
            // Create render texture with configured sensor dimensions
            renderTexture = new RenderTexture(sensorWidth, sensorHeight, 24);
            renderTexture.Create();

            frame_width = renderTexture.width;
            frame_height = renderTexture.height;

            frame = new Rect(0, 0, frame_width, frame_height);

            mainCameraTexture = new Texture2D(frame_width, frame_height, TextureFormat.RGBA32, false);

            msg.Width = (uint)frame_width;
            msg.Height = (uint)frame_height;
            msg.Step = image_step * (uint)frame_width;
            msg.Encoding = "rgba8";
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
        // silent for the rest of the session without logging anything. Doing the work directly in
        // LateUpdate (after all transforms are final) removes that dependency entirely, and with
        // no coroutine there is no guard left to get stuck.
        void LateUpdate()
        {
            timeSinceLastCapture += Time.deltaTime;
            if (timeSinceLastCapture < publishInterval)
            {
                return;
            }

            if (cam_pub == null || cam_1 == null || mainCameraTexture == null || renderTexture == null)
            {
                return;
            }

            // Skip publishing once the ROS context is gone (e.g. while leaving Play mode),
            // otherwise ros2cs logs "Cannot publish as the class is already disposed".
            if (!ros2Unity.Ok())
            {
                return;
            }

            timeSinceLastCapture = 0f;
            CaptureAndPublish();
        }

        void CaptureAndPublish()
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = cam_1.targetTexture;

            try
            {
                cam_1.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                cam_1.Render();

                // ReadPixels reads from RenderTexture.active.
                mainCameraTexture.ReadPixels(frame, 0, 0);
                mainCameraTexture.Apply();

                // Flip the image vertically to correct orientation
                FlipTextureVertically(mainCameraTexture);

                msg.Data = mainCameraTexture.GetRawTextureData();
                cam_pub.Publish(msg);
            }
            finally
            {
                RenderTexture.active = previousActive;
                cam_1.targetTexture = previousTarget;
            }
        }

        void FlipTextureVertically(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;

            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int topIndex = y * width + x;
                    int bottomIndex = (height - 1 - y) * width + x;

                    Color32 temp = pixels[topIndex];
                    pixels[topIndex] = pixels[bottomIndex];
                    pixels[bottomIndex] = temp;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (mainCameraTexture != null)
            {
                Destroy(mainCameraTexture);
            }
        }

    }

}  // namespace ROS2
