using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 10f;           // Speed of movement
    public float mouseSensitivity = 100f;   // Speed of rotation with the mouse

    public Camera captureCamera;            // The camera that will follow the target camera's rotation
    public Camera VRCamera;


    private float xRotation = 0f;           // To store up-down rotation

    void Start()
    {
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Move the camera independently
        MoveCamera();

        // Rotate the camera with mouse input
        RotateCamera();

        //SyncCameraWithXR();
    }

    void MoveCamera()
    {
        // Get input from WASD or arrow keys
        float horizontal = Input.GetAxis("Horizontal");  // A/D or Left/Right arrows
        float vertical = Input.GetAxis("Vertical");      // W/S or Up/Down arrows

        // Calculate the movement direction relative to the camera's forward and right vectors
        Vector3 moveDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        // Apply the movement
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    void RotateCamera()
    {
        // Get mouse movement input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate the camera up and down by adjusting xRotation, clamping it to avoid full rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);  // Limit to 90 degrees up/down

        // Apply rotation: up-down on x-axis and left-right on y-axis
        transform.localRotation = Quaternion.Euler(xRotation, transform.localEulerAngles.y + mouseX, 0f);
    }

    void SyncCameraWithXR()
    {
        // Sync position
        transform.position = VRCamera.transform.position;

        // Sync rotation (only left-right, up-down rotation should be handled in the RotateCamera method)
        transform.rotation = Quaternion.Euler(VRCamera.transform.rotation.eulerAngles.x, VRCamera.transform.rotation.eulerAngles.y, 0f);
    }
}
