using UnityEngine;

public class CanvasScalerVR : MonoBehaviour
{
    public Camera vrCamera;               // The VR camera reference
    public Canvas worldCanvas;            // The world-space canvas to scale
    public float planeDistance = 0.1f;    // Distance from the camera to the canvas plane
    public float scaleFactor = 1.0f;      // Additional scale factor for fine-tuning

    void Start()
    {
        // Ensure the canvas render mode is set to World Space
        if (worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("Canvas render mode must be set to World Space.");
            return;
        }

        ScaleCanvasToFrustum();
    }

    void ScaleCanvasToFrustum()
    {
        float camHeight;

        
       
            
        camHeight = 2.0f * planeDistance * Mathf.Tan(vrCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        

        // Calculate the scale for the canvas to match the camera’s frustum height
        float screenSizeY = Screen.height;
        float calculatedScale = (camHeight / screenSizeY) * scaleFactor;

        // Apply the calculated scale to the canvas's RectTransform
        RectTransform canvasRectTransform = worldCanvas.GetComponent<RectTransform>();
        canvasRectTransform.localScale = new Vector3(calculatedScale, calculatedScale, calculatedScale);
    }
}
