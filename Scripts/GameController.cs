using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    
    public bool gamePlaying { get; private set; }
    public int countdownTime;


    private float startTime, elapsedTime;
    TimeSpan timePlaying;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        gamePlaying = false;
        RestartTrial();

        StartCoroutine(CountdownToStart());
    }

    private void BeginGame()
    {
        gamePlaying = true;
        startTime = Time.time;
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Position Changed");
            RestartTrial();

        }

        if (gamePlaying)
        {
            elapsedTime = Time.time - startTime;
            timePlaying = TimeSpan.FromSeconds(elapsedTime);

            //string timePlayingStr = "Time: " + timePlaying.ToString("mm':'ss'.'ff");
        }
    }

    IEnumerator CountdownToStart()
    {

        SC_FPSController playerController = FindObjectOfType<SC_FPSController>();
        if (playerController != null)
        {
            playerController.canMove = false;
        }

     
 

        BeginGame();
        if (playerController != null)
        {
            playerController.canMove = true;
        }

        yield return new WaitForSeconds(0.5f);

    }

    public void FinishGame()
    {
        if (!gamePlaying) return;

        gamePlaying = false;

        elapsedTime = Time.time - startTime;
        timePlaying = TimeSpan.FromSeconds(elapsedTime);
        float finalTime = (float)timePlaying.TotalSeconds;


        SC_FPSController playerController = FindObjectOfType<SC_FPSController>();
        //int finalIncidentCount = playerController.GetOutOfCrosswalkIncidents();


        //PositionLogger logger = FindObjectOfType<PositionLogger>();
        //if (logger != null)
        //{
        //    logger.LogTrialResults(finalTime, finalIncidentCount);
        //}

     


    }

    public void RestartTrial()
    {

        Debug.Log("RestartTrial() called!");

       

        SC_FPSController playerController = FindObjectOfType<SC_FPSController>();
        if (playerController != null)
        {

            playerController.canMove = false;
            Debug.Log("PlayerController is not null");
            RandomStartPosition randomStart = playerController.GetComponent<RandomStartPosition>();
            if (randomStart != null)
            {
                Debug.Log("randomStart is not null");
                randomStart.SetRandomPosition();

            }
            PositionLogger logger = playerController.GetComponent<PositionLogger>();
            if (logger != null)
            {
                Debug.Log("Logger is not null");
                logger.LogTrialResults(elapsedTime);
                logger.StartNewTrial();

            }
            playerController.canMove = true;

            // Reset the out-of-crosswalk incidents and status
            //playerController.outOfCrosswalkIncidents = 0;
            //playerController.wasOnCrosswalk = false;

        }

        //PositionLogger logger = FindObjectOfType<PositionLogger>();

        //if (logger != null)
        //{
        //    logger.StartNewTrial();
        //}



        StartCoroutine(CountdownToStart());
    }



}