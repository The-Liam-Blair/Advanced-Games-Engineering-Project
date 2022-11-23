using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


public class CALL_PlayerFound : GoapAction
{
    // Action-specific global variables needed for proper action execution.
    private bool done;

    private float startTime;

    // Init preconditions and effects.
    public CALL_PlayerFound()
    {
        addPrecondition("foundPlayer", true);

        actionEnabled = true;
    }


    // Reset global variables.
    public override void reset()
    {
        done = false;
        cost = -1f;
        target = null;
        startTime = 0f;
    }

    // Check if the action has been completed.
    public override bool isDone()
    {
        return done;
    }

    // Determines if the action can be performed now from being in the correct location.
    public override bool requiresInRange()
    {
        return false;
    }

    // Checks if the action can be run
    public override bool checkProceduralPrecondition(GameObject agent)
    {
        target = GameObject.FindGameObjectWithTag("Player");
        return true;
    }

    // Implementation of the action itself, does not include movement: Only the action AFTER arriving to the correct location.
    public override bool perform(GameObject agent)
    {
        agent.GetComponent<NavMeshAgent>().speed = 0f;
        if (startTime == 0) { startTime = Time.time; }
        if (Time.time - startTime > 2)
        {
            done = true;

            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                if (Vector3.Distance(agent.transform.position, enemy.transform.position) <= 40 && agent != enemy)
                {
                    Debug.DrawLine(agent.transform.position + Vector3.up, enemy.transform.position + Vector3.up,
                        Color.white, 3);
                    enemy.GetComponent<GoapAgent>().ReceiveCall(agent, target, "CHASEPLAYER");
                }
            }
        }
        agent.GetComponent<NavMeshAgent>().speed = 11f;
        return true;
    }
}