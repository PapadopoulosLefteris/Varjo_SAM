using System;
using UnityEngine;
using Python.Runtime;
using System.IO;

public class PythonTest : MonoBehaviour
{
    private dynamic pythonScript;  // Declare pythonScript as a dynamic variable

    void Start()
    {
        // 🔹 Set the correct Python DLL path (update this to match your Python version!)
       
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", @"C:\Users\Administrator\AppData\Local\Programs\Python\Python310\python310.dll");
        Environment.SetEnvironmentVariable("PYTHONHOME", @"C:\Users\Administrator\AppData\Local\Programs\Python\Python310");
        Environment.SetEnvironmentVariable("PYTHONPATH", @"C:\Users\Administrator\AppData\Local\Programs\Python\Python310\Lib");

        try
        {
            // 🔹 Initialize Python.NET
            PythonEngine.Initialize();
            

            using (Py.GIL()) // Ensure Python's Global Interpreter Lock is handled
            {
                Debug.Log("Starting initialization");
                dynamic sys = Py.Import("sys");
              
                dynamic torch = Py.Import("torch");
                Debug.Log(torch.cuda.is_available());
                Debug.Log(torch.cuda.device_count());

                string scriptDirectory = @"C:\Users\Administrator\Desktop\unitysam";
                sys.path.append(scriptDirectory); // Append the directory to sys.path

                pythonScript = Py.Import("segment_inference");

                // 🔹 Call the initialize_model function to load the model
                pythonScript.initialize_model();

                Debug.Log("SAM2 model and predictor initialized successfully.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Python initialization error: {e.Message}");
        }
        finally
        {
            // 🔹 Shutdown Python.NET to clean up resources
            PythonEngine.Shutdown();
        }
    }
}
