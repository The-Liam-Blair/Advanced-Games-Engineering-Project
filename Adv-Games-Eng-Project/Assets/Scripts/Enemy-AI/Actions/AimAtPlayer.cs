﻿using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


/// <summary>
/// Attempt to aim an item at the player, so that the item can be used afterwards for an accurate attack.
/// </summary>
public class AimAtPlayer : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool isAiming;

    // Records duration of aiming.
    private float startTime;

    // Init preconditions and effects.
    public AimAtPlayer()
    {
        addPrecondition("hasItem", true);         // Does the enemy have an item to use?
        addPrecondition("foundPlayer", true);     // Has the player been seen?
        addPrecondition("aimingAtPlayer", false); // Is the enemy not aiming at the player?

        addEffect("aimingAtPlayer", true); // Enemy is now aiming at the player.

        actionEnabled = true;   
    }


    // Reset global variables.
    public override void reset()
    {
        isAiming = false;
        cost = 1f;
        target = GameObject.Find("Player");
        startTime = 0f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return isAiming;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return false;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        // Get angle difference between current rotation and rotation needed to aim and look at the player.
        // If the angle difference is < 35 degrees, then it's short enough that using a projectile item will be quick and useful.
        // Otherwise, don't use projectile. In either case, aiming (and throwing items) requires player line of sight, so the enemy
        // will chase the player regardless of an item is used or not. Aiming + throwing is only done if it is cheap enough.
        float angleDiff = Vector3.Angle(agent.transform.forward, target.transform.position - agent.transform.position);
        cost = (angleDiff < 35f) ? -angleDiff : 999f;
        return true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        // Action is forcefully run when it's running cost becomes too high. This checks if that condition has been triggered.
        if (!base.perform(agent)) { return false; };

        // Record time taken to aim.
        if (startTime == 0) { startTime = Time.time; }

        // Rotate to face the player. While rotating, enemy cannot move.
        agent.GetComponent<NavMeshAgent>().speed = 0f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.transform.position - agent.transform.position), 5 * Time.deltaTime);

        // Aiming angle difference is <5 degrees, so able to fire the projectile at the player.
        if (Vector3.Angle(target.transform.position - agent.transform.position, agent.transform.forward) < 5f)
        {
            agent.GetComponent<NavMeshAgent>().speed = 9f;
            isAiming = true;
            UpdateWorldState();
        }

        // After 2 seconds of aiming, abort. (Enemy will keep rotating until it's aimed at the player's CURRENT position, not a past recorded position.
        if (Time.time - startTime > 2f)
        {
            Debug.Log("Aiming failed");

            // Stops the agent from trying to fire a second time.
            actionEnabled = false;
            return false;
        }

        // Unlike most actions, this action does not complete in one frame. It returns true to ensure that the action did not return an error (so plan isn't aborted),
        // but isAiming is still false, so the action is not dequeued from the plan yet.
        return true;
    }
}