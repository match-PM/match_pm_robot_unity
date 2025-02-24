using System.Collections;
using UnityEngine;
using System;
using Unity.VisualScripting;
public class ConfigureCamera : MonoBehaviour
{
    [Header("Lens Specifications")]
    public float lensMagnification;

    [Header("Sensor Specifications")]
    [Tooltip("Width of camera sensor in Px.")]
    public float sensorWidth;
    [Tooltip("Height of camera sensor in Px.")]
    public float sensorHeight;
    [Tooltip("Width of single pixel in µm.")]
    public float pixelSizeX;
    [Tooltip("Height of single pixel in µm.")]
    public float pixelSizeY;

    [Tooltip("Conversion factor from millimeters to Unity units. Eg.If 1 Unity unit = 1 mm, then scale=1.")]
    public float scale;

    private Camera cam;
    IEnumerator AddCamera()
    {
        yield return null;

        // Add the Camera component
        cam = gameObject.AddComponent<Camera>();
        cam.orthographic = true;

        // Align camera's z-axis to parent's y-axis
        Quaternion parentRotation = transform.parent.rotation;
        Quaternion desiredRotation = Quaternion.LookRotation(parentRotation * Vector3.up, parentRotation * Vector3.forward);
        transform.rotation = desiredRotation;

        calculateAndSetOrthographicSize();
        cam.nearClipPlane = 0.0f;
        cam.farClipPlane = 10f;
        cam.aspect = (float)sensorWidth / (float)sensorHeight;
    }


    void Start()
    {
        Camera existingCam = gameObject.GetComponent<Camera>();
        if (existingCam != null)
        {
            Destroy(existingCam);
        }

        StartCoroutine(AddCamera());

    }


    // Update is called once per frame
    void Update()
    {
    }

    IEnumerator AddCoaxialLight()
    {
        yield return null;
    }


    float calculateMetricSensorSize(float sensorSize, float pixelSize)
    {
        float metricSize = sensorSize * pixelSize * (float)Math.Pow(10, -3);
        return metricSize;
    }


    float calculateAFOV(float sensorsize, float magnification)
    {
        float FOV = sensorsize / magnification;
        return FOV;
    }

    void calculateAndSetOrthographicSize()
    {
        float orthographicSize = 0.5f * (sensorHeight * pixelSizeX * (float)Math.Pow(10, -3) / lensMagnification * scale);
        cam.orthographicSize = orthographicSize;
    }
}
