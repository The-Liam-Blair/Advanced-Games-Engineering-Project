using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles all player inputs.
/// </summary>
public class PlayerControl : MonoBehaviour
{
    // Movement speed scalar.
    private float speed;

    // Stunned bool, used to limit player's speed and movement.
    private bool isStunned;
    
    // Rigidbody
    private Rigidbody rb;

    // Reference to the player's inventory.
    private PlayerInventory inventory;

    // Reads movement inputs.
    float horizontal;
    float vertical;

    // Stores player displacements per frame, calculated from movement inputs.
    Vector3 movement;
    
    // Scales movement speed, used to slow the player.
    private float speedMultiplier;

    private void Start()
    {
        isStunned = false;
        rb = GetComponent<Rigidbody>();
        inventory = GetComponent<PlayerInventory>();

        speedMultiplier = 1f;
    }
    
    private void FixedUpdate()
    {
        // No movement possible when stunned, and cannot use items either.
        if (isStunned)
        {
            return;
        }

        // If the sprint key is held: Set speed to 14 (55% increase), otherwise set speed to 9.
        speed = (Input.GetAxisRaw("Sprint") > 0) ? 14f : 9f;

        // Scale speed with the scalar.
        speed *= speedMultiplier;
        
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

        if (Input.GetAxisRaw("Exit") > 0)
        {
            Application.Quit();
        }
    }


    /// <summary>
    /// Stun the player for a given duration. Stun in this instance disables all inputs, including movement and using items.
    /// </summary>
    /// <param name="duration">Duration of the stun.</param>
    public void Stun(int duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    /// <summary>
    /// Slow the player for a given duration. Slow in this instance reduces the player's movement speed by 66%.
    /// </summary>
    /// <param name="duration">Duration of the slow effect.</param>
    public void Slow(int duration)
    {
        StartCoroutine(SlowCoroutine(duration));
    }

    /// <summary>
    /// Blind the player for a given duration. Blind massively reduces sight for the player.
    /// </summary>
    /// <param name="duration"></param>
    public void Blind(int duration)
    {
        // TODO: DEBUG CODE: REMOVE FROM RELEASE
        // Only blind works in the lighting test scene.
        if (SceneManager.GetActiveScene().name == "Multi-agent-Test")
        {
            return;
        }
        StartCoroutine(BlindCoroutine(duration));
    }

    /// <summary>
    /// Stun the player for a given duration
    /// </summary>
    /// <param name="duration">Length of the stun in seconds.</param>
    IEnumerator StunCoroutine(int duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;

        yield return null;
    }

    /// <summary>
    /// Slow the player for a given duration
    /// </summary>
    /// <param name="duration">Length of the slow in seconds.</param>
    IEnumerator SlowCoroutine(int duration)
    {
        speedMultiplier = 0.33f; // Reduce multiplier by 2/3's for movement slowness.
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f; // Return multiplier back to 1 for normal speed.

        yield return null;
    }

    IEnumerator BlindCoroutine(int duration)
    {
        GameObject spot = GameObject.Find("Spot Light");
        spot.SetActive(false);
        yield return new WaitForSeconds(duration);
        spot.SetActive(true);

        yield return null;
    }
}
