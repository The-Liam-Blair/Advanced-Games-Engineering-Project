
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TouchCube : GoapAction
{
    private bool movedThere;
    private GameObject targetCube;

    public TouchCube()
    {
        addPrecondition("touchingCube", false);
        addEffect("pog", true);
    }


    public override void reset()
    {
        movedThere = false;
        targetCube = null;
    }

    public override bool isDone()
    {
        return movedThere;
    }

    public override bool requiresInRange()
    {
        return true; // yes we need to be near a supply pile so we can drop off the ore
    }

    public override bool checkProceduralPrecondition(GameObject agent)
    {
        // find the nearest supply pile that has spare ore
        target = GameObject.Find("Cube");

        return true;
    }

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
