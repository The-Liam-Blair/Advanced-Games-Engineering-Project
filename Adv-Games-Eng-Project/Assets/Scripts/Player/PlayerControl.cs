using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [SerializeField] private int speed;
    
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    // Fixed update used for consistent physics movement.
    // Rigidbody movement also used as it simplifies collision handling with terrain.
    private void FixedUpdate()
    {
        // If x axis key is pressed: calculate movement vector. Otherwise, key not being pressed, so cancel out movement from rigidbody
        // velocity (Stops skidding/forced movement).
        float horizontal = (Input.GetAxisRaw("Horizontal") != 0)
            ? Input.GetAxisRaw("Horizontal") * speed
            : -rb.velocity.x;

        float vertical = (Input.GetAxisRaw("Vertical") != 0)
            ? Input.GetAxisRaw("Vertical") * speed
            : -rb.velocity.z;

        // Apply movements floats to vector, add to rigid body as an impulse.
        rb.AddForce(new Vector3(horizontal, 0, vertical), ForceMode.Impulse);

        // If player is moving diagonally...
        if (Mathf.Abs(rb.velocity.x) > 0 && Mathf.Abs(rb.velocity.z) > 0)
        {
            // Apply a 95% velocity/top speed clamp (As diagonal speed is faster than horizontal/vertical).
            // 95% diagonal clamped speed =~ 50% ordinal clamped speed approximately, so no speed is lost or gained by moving diagonally.
            rb.velocity = new Vector3(
                Mathf.Clamp(rb.velocity.x, -speed * 0.05f, speed * 0.05f),
                0,
                Mathf.Clamp(rb.velocity.z, -speed * 0.05f, speed * 0.05f));
        }
        
        // Player is not moving diagonally, so is moving in an ordinal direction.
        else
        {
            // Apply standard 50% velocity clamp.
            rb.velocity = new Vector3(
                Mathf.Clamp(rb.velocity.x, -speed * 0.5f, speed * 0.5f),
                0,
                Mathf.Clamp(rb.velocity.z, -speed * 0.5f, speed * 0.5f));
        }
    }
}
