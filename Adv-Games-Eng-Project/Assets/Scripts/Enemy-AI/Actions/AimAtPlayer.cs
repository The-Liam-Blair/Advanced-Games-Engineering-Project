using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class AimAtPlayer : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool isAiming;

    // Records duration of aiming.
    private float startTime;

    // Init preconditions and effects.
    public AimAtPlayer()
    {
        addPrecondition("hasItem", true);
        addPrecondition("aimingAtPlayer", false);

        addEffect("aimingAtPlayer", true);

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        isAiming = false;
        cost = -1f;
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
        // Cost is the angle difference for rotating to aim at the player. Cost starts at 20, final cost needs to be 20 or lower to make it cheaper
        // than just charging for the player (Can only use items standing still). So, the angle needed to turn to face the player needs to be 20 degrees or less.
        //cost = Vector3.Angle(target.transform.position - agent.transform.position, agent.transform.forward);
        cost = 0f;
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
            agent.GetComponent<NavMeshAgent>().speed = 11f;
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