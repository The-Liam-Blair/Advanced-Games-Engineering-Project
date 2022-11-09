using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class GetItem : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool gotItem;

    // Init preconditions and effects.
    public GetItem()
    {
        addPrecondition("hasItem", false);

        addEffect("hasItem", true);

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        gotItem = false;
        cost = 1f;
        target = null;
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
        // If the enemy has seen at least 1 item...
        if (GetComponent<GoapAgent>().WorldData.ItemLocations.Count > 0)
        {
            float closestItemDist = Int32.MaxValue;
            // Loop through the list of seen items, find the item that is closest (todo: Use nav mesh instead of vector distance).
            foreach (GameObject item in GetComponent<GoapAgent>().WorldData.ItemLocations)
            {
                if (Vector3.Distance(agent.transform.position, item.transform.position) < closestItemDist)
                {
                    target = item;
                    closestItemDist = Vector3.Distance(agent.transform.position, item.transform.position);
                    cost = closestItemDist + 10; // todo fix cost cancelling this action early
                }
            }
            return true;
        }
        return false;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        // Action is forcefully run when it's running cost becomes too high. This checks if that condition has been triggered.
        //if (!base.perform(agent))
        //{
        //    return false; 

        //};

        gotItem = true;

        UpdateWorldState();

        return true;
    }
}