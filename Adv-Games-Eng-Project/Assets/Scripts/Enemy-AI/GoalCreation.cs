using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.UIElements;


/**
 * A general labourer class.
 * You should subclass this for specific Labourer classes and implement
 * the createGoalState() method that will populate the goal for the GOAP
 * planner.
 */
public class GoalCreation : MonoBehaviour, IGoap
{
    /**
	 * Key-Value data that will feed the GOAP actions and system while planning.
	 */
	public HashSet<KeyValuePair<string,object>> getWorldState () {
		HashSet<KeyValuePair<string,object>> worldData = new HashSet<KeyValuePair<string,object>> ();
        
        // worldData.Add(new KeyValuePair<string, object>("precondition", dynamic boolean check for precondition status) ));
        
        worldData.Add(new KeyValuePair<string, object>("touchingPlayer", false));

        return worldData;
	}

    /**
     * Generate goals
     * Will later be expanded to have goal insistence values so multiple goals can exist.
	 */
    public HashSet<KeyValuePair<string, object>> createGoalState()
    {
        HashSet<KeyValuePair<string, object>> goal = new HashSet<KeyValuePair<string, object>>();

        goal.Add(new KeyValuePair<string, object>("pog", true));
        return goal;
    }

	/**
	 * Not sure why the framework defined this empty, but keeping it regardless.
	 * Will likely need to be properly implemented later as many plans (Chasing player, etc) will inevitably fail multiple times.
	 */
    public void planFailed (HashSet<KeyValuePair<string, object>> failedGoal)
	{
		// Not handling this here since we are making sure our goals will always succeed.
		// But normally you want to make sure the world state has changed before running
		// the same goal again, or else it will just fail.
	}

	/**
	 * Debugger plan assembled confirmation message.
	 */
	public void planFound (HashSet<KeyValuePair<string, object>> goal, Queue<GoapAction> actions)
	{
		// Yay we found a plan for our goal
		Debug.Log ("<color=green>Plan found</color> "+GoapAgent.prettyPrint(actions));
	}
    
	/**
	 * Debugger action completed confirmation message.
	 */
	public void actionsFinished ()
	{
		// Everything is done, we completed our actions for this gool. Hooray!
		Debug.Log ("<color=blue>Actions completed</color>");
	}
    
	/**
	 * Debugger plan cancelled confirmation message
	 */
	public void planAborted (GoapAction aborter)
	{
		// An action bailed out of the plan. State has been reset to plan again.
		// Take note of what happened and make sure if you run the same goal again
		// that it can succeed.
		Debug.Log ("<color=red>Plan Aborted</color> "+GoapAgent.prettyPrint(aborter));
	}

    /**
     * Instructs the enemy agent to move (Currently to the action objective). Modified to call the nav mesh component for proper path finding.
     * For predictive pathfinding: Either modify this to be able to access region confidence values, or modify actions to access region confidence values.
     */
    public bool moveAgent(GoapAction nextAction) {
        // move towards the NextAction's target
        //float step = moveSpeed * Time.deltaTime;
        //gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, nextAction.target.transform.position, step);

        gameObject.GetComponent<NavMeshAgent>().destination = nextAction.target.transform.position;

        if ((gameObject.transform.position - nextAction.target.transform.position).magnitude < 1 ) {
			// we are at the target location, we are done
			nextAction.setInRange(true);
            gameObject.GetComponent<NavMeshAgent>().ResetPath();
            return true;
		} else
			return false;
	}
}

