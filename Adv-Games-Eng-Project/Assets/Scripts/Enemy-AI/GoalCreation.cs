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
        return WorldData.DetermineNewGoal();
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
	 * Called every time a plan has been planned fully, including a relevant action set generated that results in the completion of a goal.
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
	 * Called when an agent cancels a plan. Plan cancellation is already handled in the movement state, so no implementation is required
	 * For this function, other than for debugging purposes.
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
     * False return = Agent has not arrived at the action's location yet.
     * True  return = Agent no longer needs to move to the action location. Either indicates that it has reached the action location or the
     *                action has been aborted. Essentially ends the loop in this movement state.
     */
    public bool moveAgent(GoapAction nextAction) 
    {
        /////////////////////////////////
        // -- CHECK GOAL INSISTENCE -- //
        /////////////////////////////////

        var currentGoal = WorldData.GetCurrentGoal(); // Fetch current goal.
        // If the current goal has changed (Which means a new goal has been selected)...
        if (currentGoal.Item1 != WorldData.DetermineNewGoal().ElementAt(0).Key)
        {
            // Abort current action + goal, go back to planning so a new plan can be created using the new goal.
            nextAction.currentCostTooHigh = true;
            nextAction.setInRange(true);
            return true;
        }

        
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
        if ((gameObject.transform.position - nextAction.target.transform.position).magnitude < 1f)
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
            if (i >= -2 && i <= 2)
            {
                // Inner sight: Can fire projectiles at the player at this viewing angle.
                Debug.DrawLine(transform.position,
                    transform.position + Quaternion.AngleAxis(i * 10, transform.up) * transform.forward * 12f, Color.black);
            }
            else
            {
                // Outer sight: Needs to rotate first before it can accurately fire a projectile.
                Debug.DrawLine(transform.position,
                    transform.position + Quaternion.AngleAxis(i * 10, transform.up) * transform.forward * 12f,
                    Color.red);
            }

            // Draw 10 raycasts from the enemy in a fan - like shape. Each ray will travel for 5 units in their respective directions and then
            // record the first collision encountered in the "hit" output. It will not report collisions beyond the first (does not travel through entities, walls).
            if (Physics.Raycast(transform.position,
                    Quaternion.AngleAxis(i * 10, transform.up) * transform.forward *
                    12f,
                    out hits[i + 5],
                    8f))
            {
                // If a ray cast hits the player and the enemy isn't in an active chase, begin the chase:
                if (hits[i + 5].collider.gameObject.tag == "Player" && GoapAgent.playerChaseTime == 0f &&
                    GoapAgent.playerChaseCooldown < 0f)
                {
                    // Update world knowledge to reflect that the player has been found.
                    WorldData.EditDataValue(new KeyValuePair<string, bool>("foundPlayer", true));
                    GoapAgent.playerChaseTime = 0.01f;

                    // Massively reduce aggressiveness value as a result of attacking the player.
                    GoapAgent.aggressiveness -= 75f;      
                    if (GoapAgent.aggressiveness < 0) { GoapAgent.aggressiveness = 0; } // And don't let it reach a negative value.
                }

                // Enemy sees an item and doesn't own an item and has learned about using items...
                else if (hits[i + 5].collider.gameObject.tag == "Item" &&
                         GetComponent<Inventory>().IteminInventory == null && GetComponent<UseItem>().isActionEnabled())
                {
                    WorldData.EditDataValue(new KeyValuePair<string, bool>("hasItem", true)); // Item obtained! Update world knowledge to reflect this.
                    gameObject.GetComponent<NavMeshAgent>().destination = hits[i + 5].collider.gameObject.transform.position; // Go to the item position to grab it.
                }
            }
        }


        /////////////////////////////
        //// -- UPDATE TIMERS -- ////
        /////////////////////////////

        // If the enemy is in a chase and the chase lasts for over 5 seconds...
        if (GoapAgent.playerChaseTime > 5f)
        {
            WorldData.EditDataValue(new KeyValuePair<string, bool>("foundPlayer", false)); // Set found player to false, stopping another chase player goal.
            WorldData.EditDataValue(new KeyValuePair<string, bool>("attackPlayer", false));

            GoapAgent.playerChaseTime = 0f; // Reset chase timer.
            GoapAgent.playerChaseCooldown = 5f; // Add a 5 second cooldown before the enemy can initiate another chase.
        }

        // If the player has been found by the enemy...
        if (WorldData.GetFactState("foundPlayer", true ))
        {
            GoapAgent.playerChaseTime += Time.deltaTime; // The enemy is chasing the player! Increase chase timer by dt.
        }
        GoapAgent.playerChaseCooldown -= Time.deltaTime;

        // Increase aggressiveness if not chasing the player, decrease it at a more rapid scale while chasing the player.
        // Range of aggressive is 0 <= aggressiveness <= 100
        GoapAgent.aggressiveness += 4 * Time.deltaTime;
        if (GoapAgent.aggressiveness >= 100f) { GoapAgent.aggressiveness = 100f; }

            // Returns false if above conditions don't set it to true, indicating that the agent needs to travel more.
        return false;
	}
}