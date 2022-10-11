using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        
        worldData.Add(new KeyValuePair<string, object>("touchingPlayer", (gameObject.transform.position - GameObject.Find("Player").transform.position).magnitude < 2f));
        worldData.Add(new KeyValuePair<string, object>("touchingGrass", (gameObject.transform.position - GameObject.Find("Grass").transform.position).magnitude < 2f));

        worldData.Add(new KeyValuePair<string, object>("isBlue", gameObject.GetComponent<Renderer>().material.color == Color.blue));
        worldData.Add(new KeyValuePair<string, object>("isRed", gameObject.GetComponent<Renderer>().material.color == Color.red));

        return worldData;
	}

    /**
     * Generate goals
     * Will later be expanded to have goal insistence values so multiple goals can exist.
	 */
    public HashSet<KeyValuePair<string, object>> createGoalState()
    {
        // Holds a singular goal (Can hold multiple but node expansion will try to satisfy every goal condition at once, cannot natively handle multiple goals).
        HashSet<KeyValuePair<string, object>> goal = new HashSet<KeyValuePair<string, object>>();
        
        // Pre-define every goal the enemy may select from, populate into list.
        HashSet<KeyValuePair<string, object>> goalList = new HashSet<KeyValuePair<string, object>>();
        goalList.Add(new KeyValuePair<string, object>("touchingPlayer", true));
        goalList.Add(new KeyValuePair<string, object>("touchingGrass", true));

        // Do some maf to calculate which goal should be chosen at this current moment.
        (string, object) cheapestGoalData = DetermineGoal(getWorldState(), goalList);
        
        goal.Add(new KeyValuePair<string, object>(cheapestGoalData.Item1, cheapestGoalData.Item2));
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

        if ((gameObject.transform.position - nextAction.target.transform.position).magnitude < 2f ) {
			// we are at the target location, we are done
			nextAction.setInRange(true);
            nextAction.gameObject.GetComponent<NavMeshAgent>().ResetPath();
            return true;
		} else
			return false;
	}

	// Black magic double return values!
	// Determines what goal to choose. Currently very inefficient, kept however for readability and will be properly updated later on.
	// todo: do some fancy actual calculataions (lookup utility ai) to properly determine goal insistence values.
    public (string, object) DetermineGoal(HashSet<KeyValuePair<string, object>> worldState, HashSet<KeyValuePair<string, object>> goalList)
    {
		// return values init so the compiler doesn't go mental.
        string goal = "";
        bool goalFlag = false;

        // Init some world data for use later. Wasteful but kept for readability currently.
        bool touchingPlayer = worldState.Contains(new KeyValuePair<string, object>("touchingPlayer", true));
        bool touchingGrass = worldState.Contains(new KeyValuePair<string, object>("touchingGrass", true));

        bool isRed = worldState.Contains(new KeyValuePair<string, object>("isRed", true));
        bool isBlue = worldState.Contains(new KeyValuePair<string, object>("isBlue", true));

        // List of goals and their associated insistence values.
        HashSet<KeyValuePair<string, int>> goalIValues = new HashSet<KeyValuePair<string, int>>();

        // For each goal...
        for (int i = 0; i < goalList.Count; i++)
        {

            // Find goal by it's name.
            string currentGoal = goalList.ElementAt(i).Key;
            // Init current goal's insistence value.
            int Insistence = 0;

            // For each specific goal, determine insistence value based on world state, then add results to the goalIValues list.
            switch (currentGoal)
            {
                case "touchingPlayer":
                    if (touchingGrass) { Insistence -= 1; }
                    if (isRed) { Insistence -= 1; }
                    goalIValues.Add(new KeyValuePair<string, int>(currentGoal, Insistence));
                    break;

                case "touchingGrass":
                    if (touchingPlayer) { Insistence -= 1; }
                    if (isBlue) { Insistence -= 1; }
                    goalIValues.Add(new KeyValuePair<string, int>(currentGoal, Insistence));
                    break;
            }
        }

        // Find goal with least insistence value, using the previously-populated goalIValues list.
        int min = Int16.MaxValue;
        for (int i = 0; i < goalIValues.Count; i++)
        {
            // If new min is found, set output values to the related goal and the goal condition.
            if (goalIValues.ElementAt(i).Value < min)
            {
                min = goalIValues.ElementAt(i).Value;
                
                goal = goalIValues.ElementAt(i).Key;
                goalFlag = goalList.Contains(new KeyValuePair<string, object>(goal, true));
            }
        }
        
        // Always returns lowest insistence goal.
        return (goal, goalFlag);
    }
}

