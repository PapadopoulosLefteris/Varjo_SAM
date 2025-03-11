using UnityEngine;
using UnityEngine.UI;  // Add the UI namespace for RawImage
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Varjo.XR;


// Screen dimensions are Width:696 Height:376
// Screen Pixel Width: 2840 Height:2816

public class ServerController : MonoBehaviour
{
    public GameObject gazeTarget;

    public Camera playerCamera;                   // Assign the main VR camera
    public Camera passThroughCaptureCamera;       // Assign the secondary camera
    public RawImage rawImageDisplay;              // Assign the RawImage component from your Canvas
    public RenderTexture passThroughRenderTexture; // Assign the Render Texture here
    public Texture2D processedTexture;
    
    private HttpClient httpClient;
    public bool isProcessingFrame = false;       // Tracks if the current frame is being processed
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


        
        httpClient = new HttpClient();
        
        imagesize = passThroughRenderTexture.width;
        gazeTarget.SetActive(true);
        // Set RawImage aspect ratio to match the camera's render texture aspect ratio
        AdjustRawImageSize();


     
    }

    //void Update()
    //{

    //    rawImageDisplay.texture = processedTexture;
    //    //Capture frames at a set interval, but skip if a frame is being processed
    //    if (!isProcessingFrame && Time.time >= nextCaptureTime)
    //        if (!isProcessingFrame)
    //        {
    //            StartCoroutine(CaptureAndSendImage(100, 100));
    //        }
    //}



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

    public IEnumerator CaptureAndSendImage(int X,int Y)
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

    public async Task SendImageToPythonServer(byte[] imageBytes, int X, int Y)
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
            //Debug.Log($"[SendImageToPythonServer] HTTP request took {Time.time - requestStartTime} seconds");

            if (response.IsSuccessStatusCode)
            {
                // Time taken to read and process the response
                
                byte[] processedImageBytes = await response.Content.ReadAsByteArrayAsync();
                //Debug.Log($"[SendImageToPythonServer] Total time for processing image: {Time.time - startTime} seconds");
                // Load the processed image back into a Texture2D
                processedTexture = new Texture2D(passThroughRenderTexture.width, passThroughRenderTexture.height);
                processedTexture.LoadImage(processedImageBytes);

                // Set the processed texture to the RawImage's texture
                //rawImageDisplay.texture = processedTexture;
                
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
