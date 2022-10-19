using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class TurnRed : GoapAction
{
    private bool isRed;

    // Init preconditions and effects.
    public TurnRed()
    {
        addPrecondition("isRed", false);
        
        addEffect("isRed", true);

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        isRed = false;
        cost = 1f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return isRed;
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
        gameObject.GetComponent<Renderer>().material.color = Color.red;
        isRed = true;
        UpdateWorldState();

        return true;
    }
}