using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


/// <summary>
/// Attempt to retrieve a found item in the level.
/// </summary>
public class GetItem : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool gotItem;

    private NavMeshPath path;

    // Init preconditions and effects.
    public GetItem()
    {
        addPrecondition("hasItem", false); // Do we currently not have an item?

        addEffect("hasItem", true);        // Item is picked up by the enemy.

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        gotItem = false;
        cost = 1f;
        target = null;
        path = null;
        hasPrePerformRun = false;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return gotItem;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return true;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        path = new NavMeshPath();
        // If the enemy has seen at least 1 item...
        if (GetComponent<GoapAgent>().getWorldData().ItemLocations.Count > 0)
        {
            float closestItemDist = Int32.MaxValue;
            // Loop through the list of seen items, find the item that is closest (todo: Use nav mesh instead of vector distance).
            foreach (GameObject item in GetComponent<GoapAgent>().getWorldData().ItemLocations)
            {
                if (Vector3.Distance(agent.transform.position, item.transform.position) < closestItemDist)
                {
                    target = item;
                    closestItemDist = Vector3.Distance(agent.transform.position, item.transform.position);
                    cost = closestItemDist;
                }
            }
            
            // If a nav mesh is being rebuilt, spin until it's built.
            while (NavMeshBaker.ISNAVMESHBUILDING) {}
            NavMesh.CalculatePath(agent.transform.position, target.transform.position, NavMesh.AllAreas, path);

            return true;
        }
        return false;
    }

    public override void prePerform()
    {
        GetComponent<NavMeshAgent>().SetPath(path);

        hasPrePerformRun = true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        gotItem = true;
        UpdateWorldState();
        
        return true;
    }
}