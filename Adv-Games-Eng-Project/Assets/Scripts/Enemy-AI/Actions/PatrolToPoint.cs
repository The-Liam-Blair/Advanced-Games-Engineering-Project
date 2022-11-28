using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using Random = UnityEngine.Random;


/// <summary>
/// Patrol to a random location. As the aggressiveness value increases, the patrol location increasingly becomes more accurate
/// to the player's position.
/// </summary>
public class PatrolToPoint : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool reachedWaypoint;

    private NavMeshPath path;

    // Init preconditions and effects.
    public PatrolToPoint()
    {

        addPrecondition("isPatrolling", false); // Is the enemy not patrolling?
        
        addEffect("isPatrolling", true);        // Enemy has patrolled.

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        reachedWaypoint = false;
        cost = 1f;
        path = null;
                hasPrePerformRun = false;

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
        // Each enemy's waypoint is basically the enemy's int value, as seen below, so each follow their own way points independently.
        if (target == null)
        {
            target = GameObject.Find("_WAYPOINT" + Int32.Parse(GetComponent<GoapAgent>().name));
        }

        if (NavMeshBaker == null)
        {
            NavMeshBaker = GameObject.Find("NAV_MESHES").GetComponent<ReBake>();
        }
        
        // Get cost from time to target (distance / speed) Speed is constant so acceleration isn't calculated.
        // Since path to target isn't created yet, one must be sampled (but not exactly instantiated) to test for distance.
        NavMeshAgent nmAgent = agent.GetComponent<NavMeshAgent>();

        // Calculate path sample, store inside path variable.
        NavMeshPath _path = new NavMeshPath();

        Vector3 randWalkPoint = Vector3.zero;

        // Retrieve aggressiveness value.
        float aggressiveness = GetComponent<GoapAgent>().aggressiveness;

        // No. of tries to get a suitable path. Serves as an exit to the while loop if something goes wrong.
        int tries = 0;

        // Runs until a suitable path is found or run out of tries (Which should be in the first iteration due to how SamplePosition works, but good to be safe).
        while (tries < 100)
        {
            tries++;

            // Init player's approximate position by getting the player's actual position.
            Vector3 playerPos = GameObject.Find("Player").transform.position;

            // Low aggressiveness: Player's position is not given in any way. Random movement.
            if (aggressiveness <= 29)
            {
                randWalkPoint = new Vector3(Random.Range(-30f, 30f), 1, Random.Range(-30f, 30f));
            }    
            
            // Medium aggressiveness: Player's position is supplied with 20 units of noise on the x and z axes.
            else if (aggressiveness > 30 && aggressiveness <= 59)
            {
                randWalkPoint = playerPos + new Vector3(Random.Range(-10f, 10f), 1, Random.Range(-10f, 10f));
            }
            
            // High aggressiveness: Player's position is supplied with 5 units of noise on the x and z axes (More accurate than medium).
            else if (aggressiveness > 60 && aggressiveness <= 89)
            {
                randWalkPoint = playerPos + new Vector3(Random.Range(-2.5f, 2.5f), 1, Random.Range(-2.5f, 2.5f));
            }
            
            // Extreme aggressiveness: Player's position is supplied with no noise (Most accurate).
            else
            {
                randWalkPoint = playerPos;
            }

            // If a nav mesh is being rebuilt, spin until it's built.
            while (NavMeshBaker.ISNAVMESHBUILDING) {}

            // Attempt to find a suitable point on the nav mesh that's closest to or at the randomPoint position.
            if (NavMesh.SamplePosition(randWalkPoint, out NavMeshHit hit, Vector3.Distance(agent.transform.position, randWalkPoint), NavMesh.AllAreas))
            {
                // Test if the enemy is able to reach the way point using nav mesh travel from it's current location.
                // If the path is valid...
                NavMesh.CalculatePath(agent.transform.position, hit.position, NavMesh.AllAreas, _path);
                if (_path.status == NavMeshPathStatus.PathComplete)
                {
                    // Update target position to the way point position.
                    target.transform.position = hit.position;

                    // Get the path distance.
                    float pathDist = GetPathLength(_path);

                    // Calculate movement cost from distance / speed.
                    cost = pathDist / nmAgent.speed;

                    path = _path;

                    // Path found, so this action is valid for the plan in it's current stage.
                    return true;
                }
            }
        }
        
        

        // Only runs if a path isn't found, which shouldn't happen.
        return false;
    }

    public override void prePerform()
    {
        GetComponent<NavMeshAgent>().path = path;

        hasPrePerformRun = true;

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