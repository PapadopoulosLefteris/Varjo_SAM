using UnityEngine;

public class MatchCanvasWithFOV : MonoBehaviour
{
    public Camera playerCamera; // Reference to the player's camera
    public RectTransform canvasRectTransform; // Reference to the canvas RectTransform
    public float distanceFromCamera = 886f; // Distance between camera and canvas

    void Start()
    {
        // Ensure the canvas is in world space
        canvasRectTransform.transform.SetParent(null); // Detach from any parent if needed
        canvasRectTransform.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    void Update()
    {
        // Step 1: Get the camera's FOV
        float fov = playerCamera.fieldOfView;

        // Step 2: Calculate the height of the visible area at the given distance
        //float height = 2f * distanceFromCamera * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        float height = 2f * distanceFromCamera * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        // Step 3: Calculate the width based on the aspect ratio of the camera
        float width = height * playerCamera.aspect;

        // Step 4: Set the canvas size to match the camera's FOV
        canvasRectTransform.sizeDelta = new Vector2(width, height);

        // Step 5: Position the canvas in front of the camera
        Vector3 forwardDirection = playerCamera.transform.forward;
        Vector3 canvasPosition = playerCamera.transform.position + forwardDirection * distanceFromCamera;
        canvasRectTransform.position = canvasPosition;
        canvasRectTransform.rotation = playerCamera.transform.rotation; // Optional, keeps the canvas aligned to the camera's rotation
    }
}
