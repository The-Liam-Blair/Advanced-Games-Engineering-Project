using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;


public class PatrolToPoint : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool reachedWaypoint;

    // Init preconditions and effects.
    public PatrolToPoint()
    {

        addPrecondition("isPatrolling", false);
        
        addEffect("isPatrolling", true);

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        reachedWaypoint = false;
        cost = 1f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return reachedWaypoint;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return true;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        // Target is not reset per frame (Target in this instance is an empty object for storing way point location).
        // Instead, the target is teleported to the next calculated way point and used for distance checking if the way point is reached.
        if (target == null)
        {
            target = GameObject.Find("_WAYPOINT");
        }


        // Get cost from time to target (distance / speed) Speed is constant so acceleration isn't calculated.
        // Since path to target isn't created yet, one must be sampled (but not exactly instantiated) to test for distance.
        NavMeshAgent nmAgent = agent.GetComponent<NavMeshAgent>();

        // Calculate path sample, store inside path variable.
        NavMeshPath path = new NavMeshPath();

        float randWalkDistance = 0f;
        // For between 2 to 5 iterations...
        for (int i = 0; i < Random.Range(2f, 5f); i++)
        {
            // Increase the random walk distance by a random value between 4 and 8 (Real values, not just integer).
            // Minimum walk distance: 2 iterations * 4 range = 8 units.
            // Maximum walk distance: 5 iterations * 8 range = 40 units.
            randWalkDistance += Random.Range(4f, 8f);
        }

        // No. of tries to get a suitable path. Serves as an exit to the while loop if something goes wrong.
        int tries = 0;
        
        // Runs until a suitable path is found or run out of tries (Which should be in the first iteration due to how SamplePosition works, but good to be safe).
        while (tries < 20)
        {
            tries++;
            
            // Determine orientation by getting a random angle (from the enemy's view/perspective) in a 90 degree cone.
            Vector3 orientation = Quaternion.AngleAxis(Random.Range(-45f, 45f), Vector3.forward) * agent.transform.forward;
            
            // Multiply the orientation by the distance multiplier to get the final way point location.
            Vector3 randomPoint = orientation * randWalkDistance;
            
            // Attempt to find a suitable point on the nav mesh that's closest to or at the randomPoint position.
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, randWalkDistance, 1))
            {
                // Test if the enemy is able to reach the way point using nav mesh travel from it's current location.
                // If the path is valid...
                NavMesh.CalculatePath(agent.transform.position, hit.position, 1, path);
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    // Update target position to the way point position.
                    target.transform.position = hit.position;

                    // Get the path distance.
                    float pathDist = GetPathLength(path);

                    // Calculate movement cost from distance / speed.
                    cost = pathDist / nmAgent.speed;

                    // Path found, so this action is valid for the plan in it's current stage.
                    return true;
                }
            }
        }

        // Only runs if a path isn't found, which shouldn't happen.
        return false;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        // Action is forcefully run when it's running cost becomes too high. This checks if that condition has been triggered.
        if( !base.perform(agent)) { return false; };
        
        reachedWaypoint = true;

        return true;
    }
}