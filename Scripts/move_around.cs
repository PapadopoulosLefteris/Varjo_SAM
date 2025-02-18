using UnityEngine;

public class CircularMotion : MonoBehaviour
{
    public Transform centerPoint;  // The center around which the object will move
    public float radius = 5f;      // Radius of the circular path
    public float speed = 0.0001f;       // Speed of rotation

    private float angle = 0f;

    void Update()
    {
        // Calculate new position using Sin and Cos for circular motion
        
        float y =  Mathf.Sin( angle) ;

        // Set the position to create the circular motion
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Increment the angle based on speed and time
        angle += speed * Time.deltaTime;
    }
}
