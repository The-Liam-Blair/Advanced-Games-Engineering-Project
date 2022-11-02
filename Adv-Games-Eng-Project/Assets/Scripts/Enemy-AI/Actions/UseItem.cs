﻿using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class UseItem : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool usedItem;

    // Init preconditions and effects.
    public UseItem()
    {
        addPrecondition("foundPlayer", true);
        addPrecondition("hasItem", true);
        addPrecondition("aimingAtPlayer", true);

        addEffect("hasItem", false);
        addEffect("aimingAtPlayer", false);

        actionEnabled = false;
    }
 

    // Reset global variables.
    public override void reset()
    {
        usedItem = false;
        cost = 0f;
        target = GameObject.Find("Player");
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return usedItem;
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
        // Action is forcefully run when it's running cost becomes too high. This checks if that condition has been triggered.
        if (!base.perform(agent)) { return false; };

        GetComponent<Inventory>().UseItem();
        usedItem = true;

        UpdateWorldState();

        return true;
    }
}