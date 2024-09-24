using UnityEngine;

public class CircularMovement : MonoBehaviour
{
    public float radius = 100.0f;
    public float speed = 20.0f;
    public float initial_height;
    public float fixedRoll = 60.0f; // Fixed roll in degrees

    private Vector3 center = Vector3.zero;
    private float angle = 0.0f;

    void Start()
    {
        // transform.position = center + new Vector3(radius, initial_height, 0);
        initial_height = transform.position.y;
    }

    void Update()
    {
        // Calculate the angle increment based on speed and time
        angle += speed * Time.deltaTime;

        // Convert the angle to radians
        float radians = angle * Mathf.Deg2Rad;

        // Calculate the new position based on the angle and radius
        float x = Mathf.Cos(radians) * radius;
        float z = Mathf.Sin(radians) * radius;

        // Set the new position of the plane
        transform.position = new Vector3(x, initial_height, z);

        // Calculate the direction vector for the plane
        Vector3 direction = new Vector3(-Mathf.Sin(radians), 0, Mathf.Cos(radians));

        // Set the rotation of the plane to look in the direction of movement with fixed roll
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, fixedRoll);
    }
}
