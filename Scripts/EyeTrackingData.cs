using System.Collections.Generic;
using UnityEngine;
using Varjo.XR;
using UnityEngine.UI; // For handling UI elements

public class EyeTrackingCapture : MonoBehaviour
{

    public GameObject gazeTarget;
    public Camera playerCamera;
    public RenderTexture passThroughRenderTexture;
    private Vector3 fixationPoint;
    private Vector3 direction;
    private Vector3 rayOrigin;
    public Transform fixationPointTransform;
    public Vector3 testpointposition;
    public Vector3 gazeDirection;
    private float floatingGazeTargetDistance = 3f;
    public int offsetx;
    public int offsety;

    public int x;
    public int y;
    private int imagesize;
    private int screenWidth;
    private int screenHeight;   




    void Start()
    {
        imagesize = passThroughRenderTexture.width;
        Debug.Log($"Screen width {Screen.width}");
        Debug.Log($"Screen height {Screen.height}");
        //screenWidth = Screen.width;
        //screenHeight = Screen.height;
        //screenHeight = 662;
        //screenWidth = 668;

        screenWidth = 710;
        screenHeight = 704;
        VarjoEyeTracking.SetGazeOutputFilterType(VarjoEyeTracking.GazeOutputFilterType.None);
        Debug.Log(VarjoEyeTracking.GetGazeOutputFilterType());
    }

    void Update()
    {
        GetEyeTracking();
    }

    public void GetEyeTracking()
    {

        if (Input.GetKeyDown(KeyCode.C))
        {
            // Set calibration to full mode
            VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;
            // Call the gaze calibration function
            VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);


        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            // Set calibration to full mode
            VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.OneDot;
            // Call the gaze calibration function
            VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);
        }

        //GazeOutputFilterType VarjoEyeTracking.GetGazeOutputFilterType();
        

        //GET GAZE COORDINATES
        // Get the most recent gaze data
        VarjoEyeTracking.GazeData gazeData = VarjoEyeTracking.GetGaze();

        // Get the gaze direction
        gazeDirection = gazeData.gaze.forward;  // Gaze direction

        rayOrigin = playerCamera.transform.TransformPoint(gazeData.gaze.origin);

        //Set gaze direction as raycast direction
       direction = playerCamera.transform.TransformDirection(new Vector3(
       gazeData.gaze.forward.x,
       gazeData.gaze.forward.y,
       0.6f
       )); //Optimal z?


        //SHOW GAZE TARGET (Debugging)
        // Update gaze target's position in world space
        gazeTarget.transform.position = playerCamera.transform.position + playerCamera.transform.forward +
                                            direction * gazeData.focusDistance;


        gazeTarget.transform.localScale = Vector3.one * floatingGazeTargetDistance / 100;

        Vector3 screenPoint = playerCamera.WorldToScreenPoint(gazeTarget.transform.position);

        x = Mathf.RoundToInt((screenPoint.x) / (screenWidth) * imagesize / 2);
        y = imagesize - Mathf.RoundToInt((screenPoint.y / screenHeight) * imagesize / 2);

        offsetx = Mathf.RoundToInt((screenPoint.x - screenWidth)/(screenWidth) * imagesize/4f);
        offsety = Mathf.RoundToInt((screenPoint.y - screenHeight) / ( screenHeight) * imagesize/4f);
        //x = Mathf.RoundToInt((screenPoint.x / 2840) * imagesize);
        //y= imagesize - Mathf.RoundToInt((screenPoint.y / 2816) * imagesize);

        //Debug.Log(gazeDirection);
        Debug.Log($"Offsetx = {offsetx}, Offsety = {offsety}, imagesize = {imagesize}");
        Debug.Log($"X = {x}, Y = {y}, Screenpoint_x = {screenPoint.x}, Screenpoint_y = {screenPoint.y}");
        //Debug.Log("Screen Resolution: " + Screen.width + "x" + Screen.height);


    }
    void CalculateCalibrationQuality()
    {
        Debug.Log($"Calibration Quality Left: {VarjoEyeTracking.GetGazeCalibrationQuality().left}, Calibration Quality Right {VarjoEyeTracking.GetGazeCalibrationQuality().right}s");
    }


}
