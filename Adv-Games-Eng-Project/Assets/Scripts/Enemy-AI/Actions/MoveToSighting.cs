using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


/// <summary>
/// Investigate a player sighting from another enemy, heard from their call.
/// </summary>
public class MoveToSighting : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool done;
    
    // Init preconditions and effects.
    public MoveToSighting()
    {
        addPrecondition("RECEIVECALL_playerSighting", true); // Has a player sighting call been heard?
        
        addEffect("moveToPlayerSighting", true);             // Moved to the point where the call was heard.

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        done = false;
        cost = 1f;
        target = null;
        hasPrePerformRun = false;
        path = null;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return done;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return true;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        if (target == null)
        {
            target = GameObject.Find("_WAYPOINT" + Int32.Parse(GetComponent<GoapAgent>().name));
        }
        if (NavMeshBaker == null)
        {
            NavMeshBaker = GameObject.Find("NAV_MESHES").GetComponent<ReBake>();
        }

        return true;
    }

    public override void prePerform()
    {
        target.transform.position = GameObject.FindGameObjectWithTag("Player").transform.position;

        // Get cost from time to target (distance / speed) Speed is constant so acceleration isn't calculated.
        // Since path to target isn't created yet, one must be sampled (but not exactly instantiated) to test for distance.

        NavMeshPath _path = new NavMeshPath();
        NavMeshAgent nmAgent = GetComponent<NavMeshAgent>();

        // If a nav mesh is being rebuilt, spin until it's built.
        while (NavMeshBaker.ISNAVMESHBUILDING)
        {}

        NavMesh.CalculatePath(transform.position, target.transform.position, NavMesh.AllAreas, _path);
        if (_path.status == NavMeshPathStatus.PathComplete)
        {
            // Get the path distance.
            float pathDist = GetPathLength(_path);

            // Calculate movement cost from distance / speed.
            cost = pathDist / nmAgent.speed;
            path = _path;
            nmAgent.SetPath(path);
        }

        UpdateNavAreas(path);

        hasPrePerformRun = true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        agent.GetComponent<GoapAgent>().getWorldData()
            .EditDataValue(new KeyValuePair<string, bool>("RECEIVECALL_playerSighting", false));
        done = true;
        return true;
    }
}