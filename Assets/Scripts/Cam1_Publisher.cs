using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ROS2
{

/// <summary>
/// This script publishes the camera image to a ros2 topic
/// </summary>
public class Cam1_Publisher : MonoBehaviour
{
    // Start is called before the first frame update
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IPublisher<sensor_msgs.msg.Image> cam1_pub;
 
    private Camera cam_1;

    private RenderTexture renderTexture;
    private RenderTexture lastTexture;
 
    private Texture2D mainCameraTexture;
    private Rect frame;
 
 
    private int frame_width;
    private int frame_height;
    private uint image_step = 4;

    private sensor_msgs.msg.Image msg = new sensor_msgs.msg.Image();

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        cam_1 = GetComponent<Camera>();


        renderTexture = new RenderTexture(cam_1.pixelWidth, cam_1.pixelHeight, 24);
        renderTexture.Create();

        frame_width = renderTexture.width;
        frame_height = renderTexture.height;

        frame = new Rect(0, 0, frame_width, frame_height);

        mainCameraTexture = new Texture2D(frame_width, frame_height, TextureFormat.RGBA32, false);

        msg.Width = (uint) frame_width;
        msg.Height = (uint) frame_height;
        msg.Step = image_step * (uint) frame_width;
        msg.Encoding = "rgba8";
    }

    void Update()
    {
        if (ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("ROS2UnityCam1Publisher");
                cam1_pub = ros2Node.CreatePublisher<sensor_msgs.msg.Image>("Image_Cam1_raw");
            }

        }

        StartCoroutine(CaptureAndPublish());
    }
    IEnumerator CaptureAndPublish()
    {
        // Wait for end of frame to capture
        yield return new WaitForEndOfFrame();

        cam_1.targetTexture = renderTexture;
        lastTexture = RenderTexture.active;
 
        RenderTexture.active = renderTexture;
        cam_1.Render();
 
        mainCameraTexture.ReadPixels(frame, 0, 0);
        mainCameraTexture.Apply();
 
        cam_1.targetTexture = lastTexture;
 
        cam_1.targetTexture = null;


        // string path = Path.Combine(Application.dataPath, "SavedImage.png");
        // File.WriteAllBytes(path, mainCameraTexture.EncodeToPNG());

        msg.Data = mainCameraTexture.GetRawTextureData();
        cam1_pub.Publish(msg);

    }


}

}  // namespace ROS2
