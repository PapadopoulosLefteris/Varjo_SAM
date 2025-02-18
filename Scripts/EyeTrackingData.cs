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
    private float floatingGazeTargetDistance = 3f;
    public int x;
    public int y;   
    private int imagesize;
    private int screenWidth;
    private int screenHeight;   




    void Start()
    {
        imagesize = passThroughRenderTexture.width;
        //screenWidth = Screen.width;
        //screenHeight = Screen.height;
        screenHeight = 662;
        screenWidth = 668;
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

        if (Input.GetKeyDown(KeyCode.D))
        {
            // Set calibration to full mode
            VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.OneDot;
            // Call the gaze calibration function
            VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);
        }


        //GET GAZE COORDINATES
        // Get the most recent gaze data
        VarjoEyeTracking.GazeData gazeData = VarjoEyeTracking.GetGaze();

        // Get the gaze direction
        Vector3 gazeDirection = gazeData.gaze.forward;  // Gaze direction

        rayOrigin = playerCamera.transform.TransformPoint(gazeData.gaze.origin);

        //Set gaze direction as raycast direction
       direction = playerCamera.transform.TransformDirection(new Vector3(
       gazeData.gaze.forward.x,
       gazeData.gaze.forward.y,
       0.2f
       )); //Optimal z?


        //SHOW GAZE TARGET (Debugging)
        // Update gaze target's position in world space
        gazeTarget.transform.position = playerCamera.transform.position + playerCamera.transform.forward +
                                            direction * gazeData.focusDistance;


        gazeTarget.transform.localScale = Vector3.one * floatingGazeTargetDistance / 100;

        Vector3 screenPoint = playerCamera.WorldToScreenPoint(gazeTarget.transform.position);
        x = Mathf.RoundToInt((screenPoint.x - screenWidth)/(screenWidth) * imagesize/2);
        y = Mathf.RoundToInt((screenPoint.y - screenHeight) / ( screenHeight) * imagesize/2);
        //x = Mathf.RoundToInt((screenPoint.x / 2840) * imagesize);
        //y= imagesize - Mathf.RoundToInt((screenPoint.y / 2816) * imagesize);


        Debug.Log($"X = {x}, Y = {y}, Screenpoint_x = {screenPoint.x}, Screenpoint_y = {screenPoint.y}");
        Debug.Log("Screen Resolution: " + Screen.width + "x" + Screen.height);


    }

     
}
