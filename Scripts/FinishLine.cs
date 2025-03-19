using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{

    public Rigidbody rb;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object we collided with is tagged "Finish"
        if (other.CompareTag("Finish"))
        {

            Debug.Log("Finish Line Triggered!");
            GameController.instance.RestartTrial();

        
        }
    }

}
