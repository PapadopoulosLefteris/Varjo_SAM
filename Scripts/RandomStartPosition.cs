using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomStartPosition : MonoBehaviour
{
    public Transform[] spawnPositions;
    public int index = 0;




    public void SetRandomPosition()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;  // Disable to avoid interference

        index = Random.Range(0, spawnPositions.Length);
        transform.position = spawnPositions[index].position;
        Debug.Log("Starting position: " + spawnPositions[index].position);

        if (controller != null)
            controller.enabled = true;  // Re-enable after setting the position
    }
}
