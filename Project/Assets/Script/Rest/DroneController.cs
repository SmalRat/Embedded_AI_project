using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float acceleration = 500f;
    public float turnSpeed = 50f;
    public float maxSpeed = 20f;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // Apply force for movement
        Vector3 force = transform.forward * moveInput * acceleration * Time.deltaTime;
        if (rb.velocity.magnitude < maxSpeed)
        {
            rb.AddForce(force, ForceMode.Acceleration);
        }

        // Apply torque for turning
        Vector3 torque = Vector3.up * turnInput * turnSpeed * Time.deltaTime;
        rb.AddTorque(torque, ForceMode.VelocityChange);
    }
}
