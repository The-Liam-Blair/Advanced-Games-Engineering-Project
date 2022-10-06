
using System;
using UnityEngine;
using Random = UnityEngine.Random;


public class TouchCube : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool movedThere;
    private GameObject targetCube;

    // Init preconditions and effects.
    public TouchCube()
    {
        addPrecondition("touchingPlayer", false);
        addEffect("pog", true);
    }


    // Reset global variables.
    public override void reset()
    {
        movedThere = false;
        targetCube = null;
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
        // find the nearest supply pile that has spare ore
        target = GameObject.Find("Player");

        return true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        Debug.DrawLine(
                agent.transform.position, 
                agent.transform.position + new Vector3(0, 10, 0),
                (Color) new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1), 
                5);
        target.transform.position += Vector3.Normalize(target.transform.position - agent.transform.position) * 0.5f;
        
        movedThere = true;
        return true;
    }
}
