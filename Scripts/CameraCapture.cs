using UnityEngine;
using UnityEngine.UI;  // Add the UI namespace for RawImage
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Varjo.XR;

// Screen dimensions are Width:696 Height:376
// Screen Pixel Width: 2840 Height:2816

public class CameraCapture : MonoBehaviour
{
    public GameObject gazeTarget;

    public Camera playerCamera;                   // Assign the main VR camera
    public Camera passThroughCaptureCamera;       // Assign the secondary camera
    public RawImage rawImageDisplay;              // Assign the RawImage component from your Canvas
    public RenderTexture passThroughRenderTexture; // Assign the Render Texture here
    private HttpClient httpClient = new HttpClient();
    private bool isProcessingFrame = false;       // Tracks if the current frame is being processed
    private float captureInterval = 0.07f;         // Capture every 0.1 seconds
    private float nextCaptureTime = 0;
    public float floatingGazeTargetDistance = 3f;

    private float distanceToCanvas;
    public float fps;                  // Variable to store FPS
    private float lastFrameTime;       // Time of the last processed frame
    private Vector3 fixationPoint;
    private Vector3 direction;
    private Vector3 rayOrigin;
    public Transform fixationPointTransform;
    public Vector3 testpointposition;
    int imagesize;

    void Start()
    {
        imagesize = passThroughRenderTexture.width;
        gazeTarget.SetActive(true);
        // Set RawImage aspect ratio to match the camera's render texture aspect ratio
        AdjustRawImageSize();


     
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.F))
        {
            // Set calibration to full mode
            VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;
            // Call the gaze calibration function
            VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);


        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            // Set calibration to full mode
            VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.OneDot;
            // Call the gaze calibration function
            VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);


        }
        //// Calculate FPS
        //if (lastFrameTime > 0)
        //{
        //    float timeTaken = Time.time - lastFrameTime;
        //    fps = 1f / timeTaken;
        //}
        //lastFrameTime = Time.time;

        //Debug.Log("FPS: " + fps);


        //GET GAZE COORDINATES
        // Get the most recent gaze data
        VarjoEyeTracking.GazeData gazeData = VarjoEyeTracking.GetGaze();

        // Get the gaze direction
        Vector3 gazeDirection = gazeData.gaze.forward;  // Gaze direction

        rayOrigin = playerCamera.transform.TransformPoint(gazeData.gaze.origin);

        // Set gaze direction as raycast direction
        //direction = playerCamera.transform.TransformDirection(gazeDirection);
        direction = playerCamera.transform.TransformDirection(new Vector3(
        gazeData.gaze.forward.x ,
        gazeData.gaze.forward.y ,
        0.2f 
        )); //Why do we need z to be close to 0??


        
        //SHOW GAZE TARGET
        // Update gaze target's position in world space
        gazeTarget.transform.position = playerCamera.transform.position + playerCamera.transform.forward +
                                         direction * gazeData.focusDistance;

     
        gazeTarget.transform.localScale = Vector3.one * floatingGazeTargetDistance / 100;
        //Vector3 screenPoint = playerCamera.WorldToScreenPoint(gazeTarget.transform.position);
        Vector3 screenPoint = playerCamera.WorldToScreenPoint(gazeTarget.transform.position);
        int newX = Mathf.RoundToInt((screenPoint.x / 2840) * imagesize);
        int newY = imagesize - Mathf.RoundToInt((screenPoint.y / 2816) * imagesize);






        // Capture frames at a set interval, but skip if a frame is being processed
        //if (!isProcessingFrame && Time.time >= nextCaptureTime)
        if (!isProcessingFrame)
        {
            nextCaptureTime = Time.time + captureInterval;
            StartCoroutine(CaptureAndSendImage(newX, newY));
        }
    }


    void CalculateCalibrationQuality()
    {
        Debug.Log($"Calibration Quality Left: {VarjoEyeTracking.GetGazeCalibrationQuality().left}, Calibration Quality Right {VarjoEyeTracking.GetGazeCalibrationQuality().right}s");
    }

    void AdjustRawImageSize()
    {
        // Get the RectTransform of the RawImage
        RectTransform rawImageRect = rawImageDisplay.GetComponent<RectTransform>();

        // Match the size of the RawImage to the screen dimensions
        rawImageRect.sizeDelta = new Vector2(Screen.width, Screen.height);
        rawImageRect.anchorMin = Vector2.zero;  // Bottom-left corner
        rawImageRect.anchorMax = Vector2.one;   // Top-right corner
        rawImageRect.offsetMin = Vector2.zero;  // Reset offsets
        rawImageRect.offsetMax = Vector2.zero;
    }

    IEnumerator CaptureAndSendImage(int X,int Y)
    {
        isProcessingFrame = true;

        // Ensure the RenderTexture is active for capturing
        RenderTexture.active = passThroughRenderTexture;
        Texture2D screenShot = new Texture2D(passThroughRenderTexture.width, passThroughRenderTexture.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, passThroughRenderTexture.width, passThroughRenderTexture.height), 0, 0);
        screenShot.Apply();
        RenderTexture.active = null;

        // Encode and send to the Python server
        byte[] imageBytes = screenShot.EncodeToJPG();
        Destroy(screenShot);

        yield return SendImageToPythonServer(imageBytes, X, Y);

        // Set isProcessingFrame to false after the image has been processed and response received
        //isProcessingFrame = false;
    }

    private async Task SendImageToPythonServer(byte[] imageBytes, int X, int Y)
    {
        float startTime = Time.time;  // Start time for the method


        //var content = new ByteArrayContent(imageBytes);
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(imageContent, "file", "image.jpg");



        content.Add(new StringContent(X.ToString()), "x");
        content.Add(new StringContent(Y.ToString()), "y");

        //content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        try
        {

            // Time taken to make the HTTP request
            float requestStartTime = Time.time;
            HttpResponseMessage response = await httpClient.PostAsync("http://127.0.0.1:5005/process-image", content);
            Debug.Log($"[SendImageToPythonServer] HTTP request took {Time.time - requestStartTime} seconds");

            if (response.IsSuccessStatusCode)
            {
                // Time taken to read and process the response
                
                byte[] processedImageBytes = await response.Content.ReadAsByteArrayAsync();

                // Load the processed image back into a Texture2D
                Texture2D processedTexture = new Texture2D(passThroughRenderTexture.width, passThroughRenderTexture.height);
                processedTexture.LoadImage(processedImageBytes);

                // Set the processed texture to the RawImage's texture
                rawImageDisplay.texture = processedTexture;
                Debug.Log($"[SendImageToPythonServer] Total time for processing image: {Time.time - startTime} seconds");
                isProcessingFrame = false;
            }
            else
            {
                Debug.LogError("Failed to receive processed image from Python server.");
            }
        }
        catch (HttpRequestException e)
        {
            Debug.LogError("Error in HTTP request: " + e.Message);
        }
    }
}
