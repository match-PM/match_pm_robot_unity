using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamController : MonoBehaviour
{

    public float speed = 0.5f;
    public float mouseSensitivity = 5.0f;

    public float zoomSensitivity = 1.0f;
    public float minZoomDistance = 5.0f;
    public float maxZoomDistance = 20.0f;

    private float xRotation = 0.0f;
    private float currentZoomDistance = 10.0f;

    void Update()
    {
        // Keyboard movement
        float moveHorizontal = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float moveVertical = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        transform.Translate(new Vector3(moveHorizontal, 0.0f, moveVertical));

        // Mouse rotation
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRotation, transform.localEulerAngles.y + mouseX, 0f);
        }

        // Zoom with mouse wheel
        float scroll = (Input.GetAxis("Mouse ScrollWheel"))*(-1);
        if (scroll != 0f)
        {
            currentZoomDistance -= scroll * zoomSensitivity;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
            transform.position = transform.position - transform.forward * scroll * zoomSensitivity;
        }
    }
}
