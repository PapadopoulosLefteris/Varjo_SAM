using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PositionLogger : MonoBehaviour
{
    private List<string> positionData = new List<string>();
    private string filePath;
    private int trialNumber = 1;
    private bool isLogging = true;

    // Start is called before the first frame update
    void Start()
    {
        int fileNumber = 1;
        string folderPath = Application.persistentDataPath;

        do
        {
            filePath = Path.Combine(Application.persistentDataPath, $"C:\\Users\\Administrator\\Desktop\\Data\\position_data_{fileNumber}.csv");
            fileNumber++;
        } while (File.Exists(filePath));


        positionData.Add("Trial, Time, X, Z, Final Time, Out of Crosswalk Incidents");

    }

    // Update is called once per frame
    void Update()
    {
        if (isLogging)
        {
            Vector3 pos = transform.position;
            string logEntry = $"{trialNumber}, {Time.time}, {pos.x}, {pos.z}";
            positionData.Add(logEntry);

        }
    }

    public void OnApplicationQuit()
    {
        File.WriteAllLines(filePath, positionData);
        Debug.Log("Data saved to: " + filePath);
    }

    public void LogTrialResults(float finalTime)
    {
        positionData.Add($"Trial {trialNumber} Results,, ,{finalTime} , , ");
        isLogging = false;
    }

    public void StartNewTrial()
    {
        trialNumber++;
        positionData.Add($"Trial {trialNumber} Started at: {Time.time}");
        isLogging = true;
    }
}
