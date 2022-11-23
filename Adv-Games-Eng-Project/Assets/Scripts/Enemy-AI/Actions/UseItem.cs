using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


/// <summary>
/// Attempt to use an item to debuff the player. Requires being aimed so that projectile attacks are accurate.
/// </summary>
public class UseItem : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool usedItem;

    // Init preconditions and effects.
    public UseItem()
    {
        addPrecondition("hasItem", true);         // Does the enemy have an item?
        addPrecondition("aimingAtPlayer", true);  // Is the enemy already aiming at the player?

        addEffect("hasItem", false);        // Item has been used.
        addEffect("aimingAtPlayer", false); // No longer aiming at player.
        addEffect("hasUsedItem", true);     // Item used in this action sequence.

        actionEnabled = true; // Action enabling may still need worked on
    }
 

    // Reset global variables.
    public override void reset()
    {
        usedItem = false;
        cost =-1f;
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