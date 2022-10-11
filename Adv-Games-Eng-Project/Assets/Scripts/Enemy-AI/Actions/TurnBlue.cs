using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class TurnBlue : GoapAction
{
    private bool isBlue;
    
    // Init preconditions and effects.
    public TurnBlue()
    {
        addPrecondition("isRed", true);
        addPrecondition("isBlue", false);
        
        addEffect("isBlue", true);
        addEffect("isRed", false);
    }


    // Reset global variables.
    public override void reset()
    {
        isBlue = false;
        cost = 1f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return isBlue;
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
        gameObject.GetComponent<Renderer>().material.color = Color.blue;
        isBlue = true;
        
        return true;
    }
}