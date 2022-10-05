using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;


/**
 * A general labourer class.
 * You should subclass this for specific Labourer classes and implement
 * the createGoalState() method that will populate the goal for the GOAP
 * planner.
 */
public class GoalCreation : MonoBehaviour, IGoap
{
	public float moveSpeed = 5;


	void Start ()
	{
        
    }


	void Update ()
	{

	}

	/**
	 * Key-Value data that will feed the GOAP actions and system while planning.
	 */
	public HashSet<KeyValuePair<string,object>> getWorldState () {
		HashSet<KeyValuePair<string,object>> worldData = new HashSet<KeyValuePair<string,object>> ();
        worldData.Add(new KeyValuePair<string, object>("touchingCube", false));

        /*
		worldData.Add(new KeyValuePair<string, object>("hasOre", (backpack.numOre > 0) ));
		worldData.Add(new KeyValuePair<string, object>("hasLogs", (backpack.numLogs > 0) ));
		worldData.Add(new KeyValuePair<string, object>("hasFirewood", (backpack.numFirewood > 0) ));
		worldData.Add(new KeyValuePair<string, object>("hasTool", (backpack.tool != null) ));
        */

        return worldData;
	}

    /**
	 * Our only goal will ever be to make tools.
	 * The ForgeTooldAction will be able to fulfill this goal.
	 */
    public HashSet<KeyValuePair<string, object>> createGoalState()
    {
        HashSet<KeyValuePair<string, object>> goal = new HashSet<KeyValuePair<string, object>>();

        goal.Add(new KeyValuePair<string, object>("pog", true));
        return goal;
    }


    public void planFailed (HashSet<KeyValuePair<string, object>> failedGoal)
	{
		// Not handling this here since we are making sure our goals will always succeed.
		// But normally you want to make sure the world state has changed before running
		// the same goal again, or else it will just fail.
	}

	public void planFound (HashSet<KeyValuePair<string, object>> goal, Queue<GoapAction> actions)
	{
		// Yay we found a plan for our goal
		Debug.Log ("<color=green>Plan found</color> "+GoapAgent.prettyPrint(actions));
	}

	public void actionsFinished ()
	{
		// Everything is done, we completed our actions for this gool. Hooray!
		Debug.Log ("<color=blue>Actions completed</color>");
	}

	public void planAborted (GoapAction aborter)
	{
		// An action bailed out of the plan. State has been reset to plan again.
		// Take note of what happened and make sure if you run the same goal again
		// that it can succeed.
		Debug.Log ("<color=red>Plan Aborted</color> "+GoapAgent.prettyPrint(aborter));
	}

	public bool moveAgent(GoapAction nextAction) {
		// move towards the NextAction's target
		float step = moveSpeed * Time.deltaTime;
		gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, nextAction.target.transform.position, step);
		
		if ((gameObject.transform.position - nextAction.target.transform.position).magnitude < 1 ) {
			// we are at the target location, we are done
			nextAction.setInRange(true);
			return true;
		} else
			return false;
	}
}

