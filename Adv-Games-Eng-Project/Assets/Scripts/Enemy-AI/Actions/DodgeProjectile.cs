using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


/// <summary>
/// Try to dodge an incoming projectile.
/// </summary>
public class DodgeProjectile: GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool done;

    // Init preconditions and effects.
    public DodgeProjectile()
    {
        addPrecondition("incomingProjectile", true);   // Is there a projectile incoming?

        addEffect("incomingProjectile", false);        // Projectile has been dodged.

        actionEnabled = false;
    }


    // Reset global variables.
    public override void reset()
    {
        done = false;
        target = null;
        cost = 1f;
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
        return true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        // If projectile has already hit something, then the action is already done since the projectile can't be dodged anymore.
        // Also will catch an error if the list is empty and this is (for some reason) called.
        if (WorldData.PlayerProjectiles.Count == 0 || !WorldData.PlayerProjectiles[0].activeInHierarchy)
        {
            done = true;
            WorldData.PlayerProjectiles.Clear();
            UpdateWorldState();
            return true;
        }
        target = WorldData.PlayerProjectiles[0];

        // do raycast to left and right sides, choose side that is furthest away
        // from the projectile's path, then move to that side
        Vector3 left;
        Vector3 right;

        RaycastHit hit;

        Physics.Raycast(agent.transform.position, agent.transform.right, out hit, 15f, LayerMask.GetMask("Wall"));
        right = hit.point;

        Physics.Raycast(agent.transform.position, agent.transform.right * -1, out hit, 15f, LayerMask.GetMask("Wall"));
        left = hit.point;

        GetComponent<NavMeshAgent>().speed = 0f;

        if (Vector3.Distance(agent.transform.position, right) > Vector3.Distance(agent.transform.position, left))
        {
            // Move to the right
            agent.GetComponent<Rigidbody>().AddForce(agent.transform.right * 10f, ForceMode.Impulse);
            Debug.DrawRay(agent.transform.position + Vector3.up, agent.transform.right * 100f, Color.red, 5f);
        }
        else
        {
            // Move to the left
            agent.GetComponent<Rigidbody>().AddForce(agent.transform.right * -10f, ForceMode.Impulse);
            Debug.DrawRay(agent.transform.position + Vector3.up, agent.transform.right * -100f, Color.red, 5f);
        }

        done = true;
        WorldData.PlayerProjectiles.Clear();
        UpdateWorldState();

        GetComponent<NavMeshAgent>().speed = 9f;

        return true;
    }
}