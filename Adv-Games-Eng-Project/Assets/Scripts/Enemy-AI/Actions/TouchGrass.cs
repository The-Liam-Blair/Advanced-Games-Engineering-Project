using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class TouchGrass : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool movedThere;

    // Init preconditions and effects.
    public TouchGrass()
    {
        addPrecondition("touchingGrass", false);
        addEffect("touchingGrass", true);
        addEffect("touchingPlayer", false);
    }


    // Reset global variables.
    public override void reset()
    {
        movedThere = false;
        target = null;
        cost = 1f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return movedThere;
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
        target = GameObject.Find("Grass");
        Debug.Log(target.name);

        // Get cost from time to target (distance / speed) Speed is constant so acceleration isn't calculated.
        // Since path to target isn't created yet, one must be sampled (but not exactly instantiated) to test for distance.
        NavMeshAgent nmAgent = agent.GetComponent<NavMeshAgent>();

        // Calculate path sample, store inside path variable.
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(agent.transform.position, target.transform.position, NavMesh.AllAreas, path);

        // If the path is valid...
        if (path.status == NavMeshPathStatus.PathComplete)
        {
            // Get the path distance.
            float pathDist = GetPathLength(path);

            // Calculate movement cost from distance / speed.
            cost = pathDist / nmAgent.speed;

            Debug.Log("DIST:" + pathDist + "units \n SPEED:" + nmAgent.speed + "units/s");
            Debug.Log("COST:" + cost);

            // Path found, so this action is valid for the plan in it's current stage.
            return true;
        }

        // Only runs if a path isn't found, which shouldn't happen.
        return false;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        for (int i = 0; i < 10000; i++)
        {
            
        Debug.DrawLine(
                agent.transform.position,
                agent.transform.position + new Vector3(Random.Range(-3,3), Random.Range(5,10), Random.Range(-3, 3)),
                (Color)new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1),
                5);
        }

        movedThere = true;
        return true;
    }
}