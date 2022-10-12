using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using Vector3 = UnityEngine.Vector3;


public enum GOALS
{
    TOUCHPLAYER,
    TOUCHGRASS
};

/**
 * Handles the creation and selection of goals before planning. Also handles agent movement.
 */
public class GoalCreation : MonoBehaviour, IGoap
{
    /**
	 * Returns all the (known) world state that the enemy has perceived.
     * World state in this instance is boolean values that represent different attributes of the world and their state.
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
     * Called before planning to decide what goal (end effect of an action) the agent should pursue.
     * Firstly acquires the current world state and list of all selectable goals and decides the goal to pursue using their insistence values.
     * Returns the goal with the highest insistence (priority) value.
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
	 *  Plan creation process failed. Only really called during plan creation process, not during the execution of a plan.
	 * Essentially, this should not be called ever, as if the game enters a state that results in a failed plan, the planner will
	 * infinitely create failed plans. Avoid creating any failed plans at all to prevent this!
	 *
	 * Alternatively, world state can be updated in this function to (potentially) cancel out a failed plan feedback loop. Not necessary however so left out
	 * for now.
	 */
    public void planFailed (HashSet<KeyValuePair<string, object>> failedGoal)
	{}

	/**
	 * Called every time a plan has been completed fully, including a relevant action set generated that results in the completion of a goal.
	 */
	public void planFound (HashSet<KeyValuePair<string, object>> goal, Queue<GoapAction> actions)
	{
		Debug.Log ("<color=green>Plan found</color> "+GoapAgent.prettyPrint(actions));
	}
    
	/**
	 * Debugger message that is called every time the enemy agent completes an action from it's planned action set.
	 */
	public void actionsFinished ()
	{
		// Everything is done, we completed our actions for this gool. Hooray!
		Debug.Log ("<color=blue>Actions completed</color>");
	}
    
	/**
	 * Called when the agent decides to cancel a plan. Currently unimplemented, but would likely destroy the current plan, update world state and
	 * go through the planning process again.
	 *
	 * Likely will be called when an action seems to be unachievable within a reasonable time-frame, such as chasing a player.
	 */
	public void planAborted (GoapAction aborter)
	{
		// An action bailed out of the plan. State has been reset to plan again.
		// Take note of what happened and make sure if you run the same goal again
		// that it can succeed.
		Debug.Log ("<color=red>Plan Aborted</color> "+GoapAgent.prettyPrint(aborter));
	}

    /**
     * Instructs the enemy agent to move to a specific location, currently only the action's associated location (if it exists). Modified to
     * call the nav mesh component for proper path finding. If an action does not have an associated location, this function is not called.
     * For predictive pathfinding: Modify this to be able to access region confidence values.
     *
     * Called once per frame while the enemy agent is in the 'MoveTo' state.
     *
     *
     * Minor problem with nav mesh path-finding: when the agent approaches a narrow mesh strip, it will intrinsically slow down as it thinks it's about to
     * walk off the walkable nav mesh area. Extremely noticeable on tight walkways and sharp corners.
     * Solution: (Derived from https://forum.unity.com/threads/obstacle-avoidance-causing-agents-to-slow-down-on-corners.1074550/):
     *  - Set agent path obstacle quality to none (Will not look ahead for obstacles, so does not slow down in the above instances.
     *  - However: Unable to detect dynamic obstacles (E.g., incoming player projectiles).
     *  - But: Utilize ray-casting to detect any specific obstacles, like player projectiles. If one is detected, TEMPORARILY set the obstacle quality to high
     *    to dodge it, then set it back to none afterwards.
     */
    public bool moveAgent(GoapAction nextAction) {
        
        // Update + set enemy agent's nav mesh destination. to be the action's associated location.
        gameObject.GetComponent<NavMeshAgent>().destination = nextAction.target.transform.position;

        // Perform a distance check between enemy agent and action's associated location. If this evaluates to true, destroy the nav mesh path
        // and return true (agent is at action location). Otherwise, return false (agent needs to keep moving).
        if ((gameObject.transform.position - nextAction.target.transform.position).magnitude < 2f ) 
        {
			nextAction.setInRange(true);
            nextAction.gameObject.GetComponent<NavMeshAgent>().ResetPath();
            return true;
		}

        // Update current movement cost of this action. Value is halved to scale it approximately to the pre-calculated cost of the action.
        nextAction.currentMovementCost += Time.deltaTime * 0.5f;
        
        // Check current movement cost to the expected action movement cost.
        if (nextAction.currentMovementCost > nextAction.cost)
        {
            // Movement cost is just too high, abort the plan (Force the action function to run null and return false, causing the plan to collapse and be restarted).
            if (nextAction.currentMovementCost > nextAction.cost * 2)
            {
                nextAction.currentCostTooHigh = true;
                nextAction.setInRange(true);
                return true;
            }

            // However, if movement cost is only a bit above the expected value ( 1x < m < 2x), calculate the remaining distance to travel to the target.
            // If this distance is short, reduce the current movement cost slightly to make the enemy pursue this action for longer in the hopes that
            // it reaches it within a still-reasonable time frame.

            // Algorithm will only reduce the current action time 3 times to stop the enemy from endlessly //ursuing a target.
            else if (nextAction.resetCount < 3)
            {
                // todo: fix pathfind distance algorithm, isnae working (For this case only).

                float dist = Vector3.Distance(transform.position, nextAction.target.transform.position);
                float calcCost = dist / nextAction.currentMovementCost;

                // approximately covered 2/3's or more of the distance already
                if (calcCost < nextAction.cost / 3f)
                {
                    Debug.DrawLine(transform.position, nextAction.target.transform.position, Color.red, 4);
                    Debug.Log("MAJOR POGGIN");
                    Debug.Log("OLD TIME: " + nextAction.currentMovementCost);
                    nextAction.resetCount++;
                    nextAction.currentMovementCost -= calcCost;
                    Debug.Log("NEW TIME: " + nextAction.currentMovementCost);

                }
            }
        }
        return false;
	}

	// Black magic 2 returned variables!
	// Determines what goal to choose. Currently very inefficient, kept however for readability and will be properly updated later on.
	// todo: do some fancy actual calculations (lookup utility ai) to properly determine goal insistence values.
    public (string, object) DetermineGoal(HashSet<KeyValuePair<string, object>> worldState, HashSet<KeyValuePair<string, object>> goalList)
    {

        GOALS aGoal;

        // return values declared so the compiler doesn't go mental.
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

            // Set enum value to goal.
            aGoal = (GOALS)i;
            // Init current goal's insistence value.
            int Insistence = 0;

            // For each specific goal, determine insistence value based on world state, then add results to the goalIValues list.
            switch (aGoal)
            {
                case GOALS.TOUCHPLAYER:
                    if (touchingGrass) { Insistence += 1; }
                    if (isRed) { Insistence += 1; }
                    goalIValues.Add(new KeyValuePair<string, int>(currentGoal, Insistence));
                    break;

                case GOALS.TOUCHGRASS:
                    if (touchingPlayer) { Insistence += 1; }
                    if (isBlue) { Insistence += 1; }
                    goalIValues.Add(new KeyValuePair<string, int>(currentGoal, Insistence));
                    break;
            }
        }

        // Find goal with the highest insistence value, using the previously-populated goalIValues list.
        int max = -1;
        for (int i = 0; i < goalIValues.Count; i++)
        {
            // If new max is found, set output values to the related goal and the goal condition.
            if (goalIValues.ElementAt(i).Value > max)
            {
                max = goalIValues.ElementAt(i).Value;
                
                goal = goalIValues.ElementAt(i).Key;
                goalFlag = goalList.Contains(new KeyValuePair<string, object>(goal, true));
            }
        }
        
        // Always returns highest insistence goal and the desired goal state.
        return (goal, goalFlag);
    }
}

