using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    // Movement speed scalar.
    private int speed;
    
    // Rigidbody
    private Rigidbody rb;

    // Reference to the player's inventory.
    private PlayerInventory inventory;

    // Reads movement inputs.
    float horizontal;
    float vertical;

    // Stores player displacements per frame, calculated from movement inputs.
    Vector3 movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        inventory = GetComponent<PlayerInventory>();
    }
    
    private void FixedUpdate()
    {
        // If the sprint key is held: Set speed to 14 (40% increase), otherwise set speed to 10.
        speed = (Input.GetAxisRaw("Sprint") > 0)? 14 : 10;
        
        // Collect inputs received during this frame
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        // Using inputs, calculate player displacement vector, and apply to the player as movement.
        movement = Vector3.zero;
        if (horizontal != 0)
        {
            movement.x += (horizontal > 0) ? speed : -speed;
        }
        if (vertical != 0)
        {
            movement.z += (vertical > 0) ? speed : -speed;
        }
        
        // If player is moving diagonally (Both horizontal and vertical inputs being pressed) : Multiply vector by 30% to normalize speed (diagonal movement is faster
        // than horizontal/vertical movement only). 30% value retrieved from pythagoras theorem.
        if (movement.z != 0 && movement.x != 0)
        {
            movement *= 0.7f;
        }

        // Rotate the player towards the movement vector if the player has moved.
        if (movement != Vector3.zero)
        {
            // Quartenion.Slerp in this instance applies the rotation (from current to new rotation) over time for a natural-looking rotation.
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 0.33f);
        }

        rb.MovePosition(transform.position + movement * Time.deltaTime);
        
        
        // If the use item key is pressed, attempt to use the held item (if it exists)
        if (Input.GetAxisRaw("Use") > 0)
        {
            if (inventory.IteminInventory != null)
            {
                inventory.UseItem();
            }
        }
    }
}
