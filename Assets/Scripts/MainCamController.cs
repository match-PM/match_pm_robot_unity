using UnityEngine;

public class MainCamController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Normal movement speed in world units per second.")]
    public float speed = 0.5f;
    [Tooltip("Movement multiplier while either Shift key is held.")]
    public float fastSpeedMultiplier = 4.0f;

    [Header("Mouse")]
    [Tooltip("Look sensitivity while the left mouse button is held.")]
    public float mouseSensitivity = 5.0f;
    public float zoomSensitivity = 1.0f;
    [Tooltip("Pan sensitivity while the middle mouse button is held.")]
    public float panSensitivity = 0.02f;

    private float pitch;
    private float yaw;
    private int ignoreLookUntilFrame = -1;

    private void Start()
    {
        SynchronizeLookAngles();
    }

    private void Update()
    {
        HandleCursor();
        HandleLook();
        HandlePan();
        HandleMovement();
        HandleZoom();
    }

    private void HandleCursor()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Locking moves the pointer to the center of the Game view. Ignore
            // that synthetic delta so clicking near an edge cannot rotate the camera.
            SynchronizeLookAngles();
            ignoreLookUntilFrame = Time.frameCount + 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ReleaseCursor();
        }
    }

    private void HandleLook()
    {
        if (!Input.GetMouseButton(0) || Time.frameCount <= ignoreLookUntilFrame)
        {
            return;
        }

        // Mouse axes already contain the per-frame delta, so Time.deltaTime must
        // not be applied here; doing so makes looking around frame-rate dependent.
        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -89.0f, 89.0f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
    }

    private void HandleMovement()
    {
        Vector3 movement = transform.right * Input.GetAxisRaw("Horizontal")
                         + transform.forward * Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.E))
        {
            movement += Vector3.up;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            movement -= Vector3.up;
        }

        movement = Vector3.ClampMagnitude(movement, 1.0f);
        bool fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = speed * (fast ? fastSpeedMultiplier : 1.0f);
        transform.position += movement * currentSpeed * Time.unscaledDeltaTime;
    }

    private void HandlePan()
    {
        if (!Input.GetMouseButton(2))
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        transform.position += (-transform.right * mouseX - transform.up * mouseY) * panSensitivity;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * scroll * zoomSensitivity;
    }

    private void OnDisable()
    {
        ReleaseCursor();
    }

    private static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SynchronizeLookAngles()
    {
        Vector3 angles = transform.eulerAngles;
        pitch = angles.x > 180.0f ? angles.x - 360.0f : angles.x;
        yaw = angles.y;
    }
}
