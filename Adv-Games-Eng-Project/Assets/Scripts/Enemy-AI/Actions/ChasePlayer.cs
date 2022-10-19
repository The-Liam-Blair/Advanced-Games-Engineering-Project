using System;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class ChasePlayer : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool attackedPlayer;

    // Init preconditions and effects.
    public ChasePlayer()
    {
        addPrecondition("foundPlayer", true);

        addEffect("foundPlayer", false);

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        attackedPlayer = false;
        target = null;
        cost = 1f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return attackedPlayer;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return true;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        // Right now player position is always known, will be updated later to be predicted.
        target = GameObject.Find("Player");

        // Get cost from time to target (distance / speed) Speed is constant so acceleration isn't calculated.
        // Since path to target isn't created yet, one must be sampled (but not exactly instantiated) to test for distance.
        NavMeshAgent nmAgent = agent.GetComponent<NavMeshAgent>();

        // Calculate path sample, store inside path variable.
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(agent.transform.position, target.transform.position, 1, path);

        // If the path is valid...
        if (path.status == NavMeshPathStatus.PathComplete)
        {
            // Get the path distance.
            float pathDist = GetPathLength(path);

            // Calculate movement cost from distance / speed.
            cost = pathDist / nmAgent.speed;

            // Path found, so this action is valid for the plan in it's current stage.
            return true;
        }

        // Only runs if a path isn't found, which shouldn't happen.
        return false;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        // Action is forcefully run when it's running cost becomes too high. This checks if that condition has been triggered.
        if( !base.perform(agent)) { return false; };

        Debug.Log("OW!");

        attackedPlayer = true;
        UpdateWorldState();

        return true;
    }
}