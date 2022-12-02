using System;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


/// <summary>
/// After seeing the enemy and using an item on the player, call to nearby enemies that the player is nearby so that they may assist this enemy.
/// </summary>
public class CALL_PlayerFound : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool done;

    private float startTime;
    
    // Init preconditions and effects.
    public CALL_PlayerFound()
    {
        addPrecondition("foundPlayer", true); // Has the enemy found the player?
        addPrecondition("hasUsedItem", true); // Has the enemy used an item in this action sequence?

        addEffect("hasUsedItem", false); // No actual world state change from calling other enemies but resets the used item state for reuse.

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        done = false;
        cost = -1f;
        target = null;
        startTime = 0f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return done;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return false;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        target = GameObject.FindGameObjectWithTag("Player");
        return true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        agent.GetComponent<NavMeshAgent>().speed = 0f; // Enemy stops moving while preparing to call other enemies.
        
        
        if (startTime == 0) { startTime = Time.time; } // Record time taken: 2 seconds wait then do the call.

        //find spot light child object
        GameObject spotLight = agent.transform.Find("Spot Light").gameObject;
        spotLight.GetComponent<Light>().color = Color.white; // White light = about to call for backup: run!

        // When the 2 seconds have passed, call other enemies.
        if (Time.time - startTime > 2)
        {
            spotLight.GetComponent<Light>().color = Color.clear;
            spotLight.GetComponent<Light>().color = new Color32(155, 4, 1, 255); // Back to red light again.

            agent.GetComponent<NavMeshAgent>().speed = 10f;
            
            done = true;

            // Find all nearby enemies. If any are nearby, call the relevant function that handles receiving calls. Pass in the player position and the goal of chasing the player.
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                if (Vector3.Distance(agent.transform.position, enemy.transform.position) <= 200 && agent != enemy)
                {
                    enemy.GetComponent<GoapAgent>().ReceiveCall(agent, target, "CHASEPLAYER");
                }
            }
            UpdateWorldState();
        }
        return true;
    }
}