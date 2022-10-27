using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using Debug = UnityEngine.Debug;
using Object = System.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using static System.Collections.Specialized.BitVector32;


public enum GOALS
{
    CHASEPLAYER,
    PATROL
};

/**
 * Handles the creation and selection of goals before planning. Also handles agent movement.
 */
public class GoalCreation : MonoBehaviour, IGoap
{

    public CurrentWorldKnowledge WorldData;

    void Start()
    {
        WorldData = GetComponent<GoapAgent>().WorldData;
    }

    /**
	 * Returns all the (known) world state that the enemy has perceived.
     * World state in this instance is boolean values that represent different attributes of the world and their state.
	 */
	public HashSet<KeyValuePair<string,bool>> getWorldState () 
    {
        Debug.Log(WorldData.ToString());
        return WorldData.GetWorldState();
    }

    /**
     * Called before planning to decide what goal (end effect of an action) the agent should pursue.
     * Firstly acquires the current world state and list of all selectable goals and decides the goal to pursue using their insistence values.
     * Returns the goal with the highest insistence (priority) value.
	 */
    public HashSet<KeyValuePair<string, bool>> createGoalState()
    {
        // Holds a singular goal (Can hold multiple but node expansion will try to satisfy every goal condition at once, cannot natively handle multiple goals).
        HashSet<KeyValuePair<string, bool>> goal = new HashSet<KeyValuePair<string, bool>>();
        
        // Pre-define every goal the enemy may select from, populate into list.
        HashSet<KeyValuePair<string, bool>> goalList = new HashSet<KeyValuePair<string, bool>>();

        goalList.Add(new KeyValuePair<string, bool>("foundPlayer", true));
        goalList.Add(new KeyValuePair<string, bool>("isPatrolling", true));


        // Do some maf to calculate which goal should be chosen at this current moment.
        (string, bool) cheapestGoalData = DetermineGoal(goalList);
        
        // Return the chosen goal to the planner.
        goal.Add(new KeyValuePair<string, bool>(cheapestGoalData.Item1, cheapestGoalData.Item2));
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
    public void planFailed (HashSet<KeyValuePair<string, bool>> failedGoal)
	{}

	/**
	 * Called every time a plan has been completed fully, including a relevant action set generated that results in the completion of a goal.
	 */
	public void planFound (HashSet<KeyValuePair<string, bool>> goal, Queue<GoapAction> actions)
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
     * Called once per frame while the enemy agent is in the 'MoveTo' state.
     * Contains most dynamic game logic, such as dynamic movement cost checking and detecting objects.
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
    public bool moveAgent(GoapAction nextAction) 
    {
        
        /////////////////////////////////
        //// -- UPDATE AGENT PATH -- ////
        /////////////////////////////////

        // Update + set enemy agent's nav mesh destination. to be the action's associated location.
        gameObject.GetComponent<NavMeshAgent>().destination = nextAction.target.transform.position;
        nextAction.GetPathLength(gameObject.GetComponent<NavMeshAgent>().path);
        
        
        ///////////////////////////////////
        //// -- CHECK MOVEMENT COST -- ////
        ///////////////////////////////////

        // Perform a distance check between enemy agent and action's associated location. If this evaluates to true, destroy the nav mesh path
        // and return true (agent is at action location). Otherwise, return false (agent needs to keep moving).
        if ((gameObject.transform.position - nextAction.target.transform.position).magnitude < 2f)
        {
            nextAction.setInRange(true);
            nextAction.gameObject.GetComponent<NavMeshAgent>().ResetPath();
            return true;
        }

        // Update current movement cost of this action. Value is halved to scale it approximately to the pre-calculated cost of the action.
        nextAction.currentMovementCost += Time.deltaTime * 0.5f;
        
        // Check current movement cost to the expected action movement cost.
        if (nextAction.currentMovementCost > nextAction.cost && GoapAgent.playerChaseTime == 0f)
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

            // Algorithm will only reduce the current action time 3 times to stop the enemy from endlessly pursuing a target.
            else if (nextAction.resetCount < 3)
            {
                float dist = Vector3.Distance(transform.position, nextAction.target.transform.position);
                float calcCost = dist / nextAction.currentMovementCost;

                // Approximately covered 2/3's or more of the distance already
                if (calcCost < nextAction.cost / 3f)
                {
                    nextAction.resetCount++;
                    nextAction.currentMovementCost -= calcCost;
                }
            }
        }


        ///////////////////////////////////////////////
        //// -- HANDLE RAY CAST/SIGHT DETECTION -- ////
        ///////////////////////////////////////////////

        // Draw 10 rays in a fan-like spread in front of the enemy. This construct represents it's sight.
        // Numbering goes from -5 to 5 as the fan needs to be symmetrical to represent a realistic line of sight. The indexing of the array is compensated
        // as seen below as 'i + 5';
        RaycastHit[] hits = new RaycastHit[10];

        for (int i = -5; i < 5; i++)
        {
            Debug.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(i * 10, transform.up) * transform.forward * 8f, Color.red);

            // Draw 10 raycasts from the enemy in a fan - like shape. Each ray will travel for 5 units in their respective directions and then
            // record the first collision encountered in the "hit" output. It will not report collisions beyond the first (does not travel through entities, walls).
            if (Physics.Raycast(transform.position,
                    Quaternion.AngleAxis(i * 10, transform.up) * transform.forward *
                    8f, // Partial thanks to github co-pilot for coming up with a
                    out hits[i + 5], // solution that allowed the ray to rotate properly w/o deforming!
                    8f))
            {
                // If a ray cast hits the player and the enemy isn't in an active chase, begin the chase:
                // - Chase variable is set to 0.1 (Prevents the condition from re-evaluating to true if ray hits every frame on chase start).
                // - Set world fact "found player" to true to make the chase player goal to be chosen in the next planning session, as it has a high insistence value.
                // - Forcefully exit the current action (henceforth plan) by making its cost too high to run and force it to be evaluated so it is aborted,
                //   allowing a new plan (chasing the player plan) to be made.
                if (hits[i + 5].collider.gameObject.tag == "Player" && GoapAgent.playerChaseTime == 0f &&
                    GoapAgent.playerChaseCooldown < 0f)
                {
                    WorldData.EditDataValue(new KeyValuePair<string, bool>("foundPlayer", true));
                    GoapAgent.playerChaseTime = 0.01f;
                    nextAction.currentCostTooHigh = true;
                    nextAction.setInRange(true);
                    return true;
                }

                else if (hits[i + 5].collider.gameObject.tag == "Item" &&
                         GetComponent<Inventory>().IteminInventory == null)
                {
                    WorldData.EditDataValue(new KeyValuePair<string, bool>("hasItem", true));
                    gameObject.GetComponent<NavMeshAgent>().destination = hits[i + 5].collider.gameObject.transform.position;
                }
            }
        }


        /////////////////////////////
        //// -- HANDLE TIMERS -- ////
        /////////////////////////////

        // If the enemy is in a chase and the chase lasts for over 5 seconds...
        // - Set world fact "found player" to false to prevent another chase player goal to be returned in the next planning session.
        // - Reset chase timer to 0f.
        // - Forcefully exit the current plan of chasing player (like above), to stop the chase.
        // - New plan will be generated without the chase goal, making the enemy do something else.
        // - Even though the chase was a failure, the chase cooldown is still applied to prevent constant attacks from the enemy.
        if (GoapAgent.playerChaseTime > 5f)
        {
            WorldData.EditDataValue(new KeyValuePair<string, bool>("foundPlayer", false));
            GoapAgent.playerChaseTime = 0f;
            GoapAgent.playerChaseCooldown = 5f;
            nextAction.currentCostTooHigh = true;
            nextAction.setInRange(true);
            return true;
        }

        // Increment the player chase timer by dt if the player is a target of an action (Only of which is the chase player action).
        if (nextAction.target.tag == "Player")
        {
            GoapAgent.playerChaseTime += Time.deltaTime;
        }

        GoapAgent.playerChaseCooldown -= Time.deltaTime;

        // Returns false if above conditions don't set it to true, indicating that the agent needs to travel more.
        return false;
	}
    
    // Black magic 2 returned variables!
	// Determines what goal to choose. Currently very inefficient, kept however for readability and will be properly updated later on.
	// todo: do some fancy actual calculations (lookup utility ai) to properly determine goal insistence values.
    public (string, bool) DetermineGoal(HashSet<KeyValuePair<string, bool>> goalList)
    {
        // Goals represented using an enum
        GOALS aGoal;

        // return values declared so the compiler doesn't go mental.
        string goal = "";
        bool goalFlag = false;

        // Retrieve all world data and their status. Done individually for readability.
        bool playerFound = WorldData.GetFactState("foundPlayer", true);

        bool isPatrolling = WorldData.GetFactState("isPatrolling", true);

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
                // Find and touch the player. Has negative insistence value by default, so will only be the chosen goal if the player found state is
                // at true. Otherwise, other goals will always be chosen due to a higher starting insistence value of 0.
                case GOALS.CHASEPLAYER:
                    Insistence = -1;
                    if(playerFound) { Insistence += 100; }
                    goalIValues.Add(new KeyValuePair<string, int>(currentGoal, Insistence));
                    break;

                case GOALS.PATROL:
                    int minInsistence  = Int32.MaxValue;
                    foreach (var insistence in goalIValues)
                    {
                        if(insistence.Value < minInsistence) { minInsistence = insistence.Value; }
                    }
                    if (minInsistence <= 1) { Insistence = 100; }
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
                goalFlag = goalList.Contains(new KeyValuePair<string, bool>(goal, true));
            }
        }

        // Always returns highest insistence goal and the desired goal state.
        return (goal, goalFlag);
    }
}

