﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using JetBrains.Annotations;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;


public sealed class GoapAgent : MonoBehaviour {

	private FSM stateMachine;

	private FSM.FSMState idleState; // finds something to do
	private FSM.FSMState moveToState; // moves to a target
	private FSM.FSMState performActionState; // performs an action
	
	private HashSet<GoapAction> availableActions;
	private Queue<GoapAction> currentActions;

	private IGoap dataProvider; // this is the implementing class that provides our world data and listens to feedback on planning

	private GoapPlanner planner;

	// Reference to the world knowledge class
    public CurrentWorldKnowledge WorldData;

	// Records how long the enemy has chased the player in 1 action/goal. Forces the chase to stop after a set limit.
    public static float playerChaseTime;

	// Cooldown prevents enemy from attacking the player while it is above 0f. Forces enemy to stop repeatedly chasing the player.
    public static float playerChaseCooldown;

	// Value represents how aggressive the enemy should be. Higher aggressiveness = Enemy is given approximate player position with increasing accuracy as
	// the value increases. Reduced significantly after executing a chase plan. Used to make the enemy forcefully encounter the player more often.
    public static float aggressiveness;

    void Start () {
		stateMachine = new FSM ();
		availableActions = new HashSet<GoapAction> ();
		currentActions = new Queue<GoapAction> ();
		planner = new GoapPlanner ();
		findDataProvider ();
		createIdleState ();
		createMoveToState ();
		createPerformActionState ();
		stateMachine.pushState (idleState);
		loadActions ();

        // Movement speed of the enemy.
        GetComponent<NavMeshAgent>().speed = 11f;

		// 'Pointer' to the world data class.
        WorldData = new CurrentWorldKnowledge();

        playerChaseTime = 0f;
        playerChaseCooldown = 0f;

        aggressiveness = 0;

    }
	

	void Update () {
        stateMachine.Update (this.gameObject);
    }


	public void addAction(GoapAction a) {
		availableActions.Add (a);
	}

	public GoapAction getAction(Type action) {
		foreach (GoapAction g in availableActions) {
			if (g.GetType().Equals(action) )
			    return g;
		}
		return null;
	}

	public void removeAction(GoapAction action) {
		availableActions.Remove (action);
	}

	private bool hasActionPlan() {
		return currentActions.Count > 0;
	}

	private void createIdleState() {
		idleState = (fsm, gameObj) => {
			// GOAP planning

			// get the world state and the goal we want to plan for
			HashSet<KeyValuePair<string,bool>> worldState = dataProvider.getWorldState();
			HashSet<KeyValuePair<string,bool>> goal = dataProvider.createGoalState();

            // Plan
            Queue<GoapAction> plan = planner.plan(gameObject, availableActions, worldState, goal);
            
            if (plan != null) {
				// we have a plan, hooray!
				currentActions = plan;
				dataProvider.planFound(goal, plan);

				fsm.popState(); // move to PerformAction state
				fsm.pushState(performActionState);

			} else {
				// ugh, we couldn't get a plan
				Debug.Log("<color=orange>Failed Plan:</color>"+prettyPrint(goal));
				dataProvider.planFailed(goal);
				fsm.popState (); // move back to IdleAction state
				fsm.pushState (idleState);
			}

		};
	}
	
	private void createMoveToState() {
		moveToState = (fsm, gameObj) => {
			// move the game object

			GoapAction action = currentActions.Peek();
            if (action.requiresInRange() && action.target == null) {
				Debug.Log("<color=red>Fatal error:</color> Action requires a target but has none. Planning failed. You did not assign the target in your Action.checkProceduralPrecondition()");
				fsm.popState(); // move
				fsm.popState(); // perform
				fsm.pushState(idleState);
				return;
			}

			// get the agent to move itself
			if ( dataProvider.moveAgent(action) ) {
				fsm.popState();
			}
        };
	}
	
	private void createPerformActionState() {

		performActionState = (fsm, gameObj) => {
			// perform the action

			if (!hasActionPlan()) {
				// no actions to perform
				Debug.Log("<color=red>Done actions</color>");
				fsm.popState();
				fsm.pushState(idleState);
				dataProvider.actionsFinished();
				return;
			}

			GoapAction action = currentActions.Peek();
			if ( action.isDone() ) {
                
                // the action is done. Remove it so we can perform the next one
                currentActions.Dequeue();
			}

			if (hasActionPlan()) {
				// perform the next action
				action = currentActions.Peek();
				bool inRange = action.requiresInRange() ? action.isInRange() : true;

				if ( inRange ) {
					// we are in range, so perform the action
					bool success = action.perform(gameObj);

                    if (!success) {
						// action failed, we need to plan again

                        fsm.popState();
						fsm.pushState(idleState);
						dataProvider.planAborted(action);
					}
				} else {
					// we need to move there first
					// push moveTo state
					fsm.pushState(moveToState);
				}

			} else {
				// no actions left, move to Plan state
				fsm.popState();
				fsm.pushState(idleState);
				dataProvider.actionsFinished();
			}

		};
	}

	private void findDataProvider() {
		foreach (Component comp in gameObject.GetComponents(typeof(Component)) ) {
			if ( typeof(IGoap).IsAssignableFrom(comp.GetType()) ) {
				dataProvider = (IGoap)comp;
				return;
			}
		}
	}

	private void loadActions ()
	{
		GoapAction[] actions = gameObject.GetComponents<GoapAction>();
		foreach (GoapAction a in actions) {
            if (a.isActionEnabled()) { availableActions.Add(a); }
        }
		Debug.Log("Found actions: "+prettyPrint(actions));
	}

    /// <summary>
	/// Starts the stun co-routine disabling enemy movement.
	/// </summary>
	/// <param name="duration">Length of the stun effect.</param>
    public void Stun(int duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

	/// <summary>
	/// Starts the slow co-routine, slowing movement.
	/// </summary>
	/// <param name="duration">Slowness duration.</param>
    public void Slow(int duration)
    {
		StartCoroutine(SlowCoroutine(duration));
    }

    public static string prettyPrint(HashSet<KeyValuePair<string,bool>> state) {
		String s = "";
		foreach (KeyValuePair<string,bool> kvp in state) {
			s += kvp.Key + ":" + kvp.Value.ToString();
			s += ", ";
		}
		return s;
	}

	public static string prettyPrint(Queue<GoapAction> actions) {
		String s = "";
		foreach (GoapAction a in actions) {
			s += a.GetType().Name;
			s += "-> ";
		}
		s += "GOAL";
		return s;
	}

	public static string prettyPrint(GoapAction[] actions) {
		String s = "";
		foreach (GoapAction a in actions) {
			s += a.GetType().Name;
			s += ", ";
		}
		return s;
	}

	public static string prettyPrint(GoapAction action) {
		String s = ""+action.GetType().Name;
		return s;
	}

    /// <summary>
	/// Stun the enemy for a given duration. Stun in this instance for the enemy sets movement speed to 0, and so disables movement and rotation.
	/// <br>(Basic implementation, the enemy <strong>--CAN--</strong> perform actions if it's current action's target reaches the enemy, but this should be rare).</br>
	/// </summary>
	/// <param name="duration">Length of the stun effect in seconds.</param>
    IEnumerator StunCoroutine(int duration)
    {
        GetComponent<NavMeshAgent>().acceleration = 0; // Acceleration both stops enemy movement and does not interfere with a slow de-buff, which effects speed.
        GetComponent<NavMeshAgent>().velocity = Vector3.zero; // Forcefully stops enemy movement.
        yield return new WaitForSeconds(duration);
        GetComponent<NavMeshAgent>().acceleration = 50;

        yield return null;
    }

    IEnumerator SlowCoroutine(int duration)
    {
        GetComponent<NavMeshAgent>().speed = 3.33f; // Apply speed debuff.
        yield return new WaitForSeconds(duration);
        GetComponent<NavMeshAgent>().speed = 10f; // Inverse the debuff to get the normal speed again.

        yield return null;
    }

}


/// <summary>
/// Stores current world state and methods to freely modify it.
/// </summary>
public class CurrentWorldKnowledge
{
    // Goals as enums for readability.
    private enum GOALS
    {
        CHASEPLAYER,
        PATROL,
        FINDITEM
    };

    // Stores world changes as a key value pair of a fact about the world and if it's true or false.
    public HashSet<KeyValuePair<string, bool>> WorldData;

	// List of goals the enemy has.
	// String - Goal name.
	// Bool - Desired state for this goal.
	// Int - Goal's current insistence value.
    public List<Tuple<string, bool, int>> Goals;

	// Current goal being pursued.
    public Tuple<string, bool, int> currentGoal;

    // List of sighted items, mostly used for getting their location.
    public List<GameObject> ItemLocations;

    // Stores if a player used item (Projectile or trap) is sighted, used to avoid it, if it knows how to.
    public List<GameObject> PlayerProjectiles;

    public CurrentWorldKnowledge()
    {
		// Set initial world state to false for all facts.
        WorldData = new HashSet<KeyValuePair<string, bool>>();
        WorldData.Add(new KeyValuePair<string, bool>("aimingAtPlayer", false));
        WorldData.Add(new KeyValuePair<string, bool>("foundPlayer", false));
        WorldData.Add(new KeyValuePair<string, bool>("attackPlayer", false));

        WorldData.Add(new KeyValuePair<string, bool>("isPatrolling", false));

        WorldData.Add(new KeyValuePair<string, bool>("hasItem", false));

		// Set list of all possible goals to choose from, and a placeholder insistence value of -1.
        Goals = new List<Tuple<string, bool, int>>();
        Goals.Add(new Tuple<string, bool, int>("attackPlayer", true, -1));
        Goals.Add(new Tuple<string, bool, int>("isPatrolling", true, -1));
        Goals.Add(new Tuple<string, bool, int>("hasItem", true, -1));

        ItemLocations = new List<GameObject>();
        PlayerProjectiles = new List<GameObject>();
    }

    public void AddItemLocation(GameObject item)
    {
        foreach (GameObject items in ItemLocations)
        {
            if (items.Equals(item))
            {
                return;
            }
        }
        ItemLocations.Add(item);
    }

    public void RemoveItemLocation(GameObject item)
    {
        for (int i = 0; i < ItemLocations.Count; i++)
        {
            if (ItemLocations[i].Equals(item))
            {
                ItemLocations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Add a fact to the knowledge list.
    /// </summary>
    /// <param name="data">New data fact to add.</param>
    public void AddData(KeyValuePair<string, bool> data)
    {
        WorldData.Add(data);
    }

    /// <summary>
    /// Remove a fact from the list
    /// </summary>
    /// <param name="removalData">The fact to remove from the knowledge list</param>
    /// <returns>True: Successful erasure.<br></br> False: Erasure failure.</returns>
    public bool RemoveData(KeyValuePair<string, bool> removalData)
    {
        return WorldData.Remove(removalData);
    }

    /// <summary>
    /// World State getter
    /// </summary>
    /// <returns>Current world state.</returns>
    public HashSet<KeyValuePair<string, bool>> GetWorldState()
    {

        return WorldData;
    }

    /// <summary>
    /// Modifies a fact's state. If it does not exist, add it as a new fact (Returns false in this case to indicate a new fact is made, in case of spelling mistakes, etc).
    /// </summary>
    /// <param name="newData">The fact that will be modified.</param>
    /// <returns>True: Successful fact modification <br></br> False: Fact was not found in the knowledge base and so a new fact is made.</returns>
    public bool EditDataValue(KeyValuePair<string, bool> newData)
    {
        foreach (var data in WorldData)
        {
            if (newData.Key == data.Key)
            {
                if (RemoveData(data))
                {
                    WorldData.Add((newData));
                    return true;
                }
            }
        }

        // Called if the above code block doesn't find any existing data, so adds it as a new fact here.
        WorldData.Add(newData);
        // Returns false to indicate that the fact is new, not as an error/indication of a failed operation.
        return false;
    }

    /// <summary>
    /// Locates the state of a given fact in the knowledge base. If the fact does not exist, it is created as a new, false fact.
    /// </summary>
    /// <param name="fact">Name of the fact.</param>
    /// <param name="state">State of the fact.</param>
    /// <returns>Fact state if the fact was found in the knowledge base, or false if the fact was not found and so was created in the 'false' state.</returns>
    public bool GetFactState(string fact, bool state)
    {
        foreach (var factName in WorldData)
        {
            // Fact name found in knowledge base: return it's state.
            if (factName.Key == fact) { return WorldData.Contains(new KeyValuePair<string, bool>(fact, state)); }
        }

        // Fact string not found in knowledge base: Create a new fact with it's state set to false.
        WorldData.Add(new KeyValuePair<string, bool>(fact, false));
        return false;
    }

    /// <summary>
    /// Removes all facts from the knowledge base.
    /// </summary>
    public void ClearData()
    {
        WorldData.Clear();
    }

    /// <summary>
	/// List of facts string output.
	/// </summary>
	/// <returns>Facts in an output-friendly format.</returns>
    public override string ToString()
    {
        string output = "";

        foreach (var data in WorldData)
        {
            output += data.ToString() + "\n";
        }
        return output;
    }

    /// <summary>
    /// Function that handles insistence value updating.
    /// </summary>
    /// <param name="goal">Goal whose insistence value is being modified.</param>
    /// <param name="newInsistence">New insistence value of this goal.</param>
    /// <param name="index">Index of the goal in the goal list.</param>
    private void UpdateGoalInsistence(Tuple<string, bool, int> goal, int newInsistence, int index)
    {
        // A temporary tuple needs to be created, identical to the goal but with the new insistence value, as there is no support
        // for modifying items within a tuple (They are read-only).
        var updatedGoal = Tuple.Create<string, bool, int>(goal.Item1, goal.Item2, newInsistence);
        Goals[index] = updatedGoal; 
    }

    /// <summary>
    /// Re-calculate each goal's insistence value.
    /// <br></br>
    /// Exists on a seperate function so it can be called repeatedly to constantly update each goal's insistence.
    /// </summary>
    public void DetermineGoalsInsistence()
    {
        // Goals represented using an enum
        GOALS aGoal;
        
        // Retrieve relevant world data and their status. Done individually for readability.
        bool playerFound = GetFactState("foundPlayer", true);
        bool hasItem = GetFactState("hasItem", true);

        // For each goal...
        for (int i = 0; i < Goals.Count; i++)
        {

            // Find goal by it's name.
            string currentGoal = Goals.ElementAt(i).Item1;

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
                    if (playerFound) { Insistence += 100; }
                    UpdateGoalInsistence(Goals[i], Insistence, i);
                    break;

                // Find an item if an item has been seen. Provides medium insistence, meaning some other goals may override this goal.
                // If the enemy hasn't seen any items yet or already has one, this goal's insistence is set to -1, meaning it will not be picked.
                case GOALS.FINDITEM:
                    Insistence = -1;
                    if(ItemLocations.Count > 0 && GetFactState("hasItem", false)) { Insistence += 56; }
                    UpdateGoalInsistence(Goals[i], Insistence, i);
                    break;

                // Patrol goal can only be chosen if no other goal has an insistence value of 1 or higher. Essentially, this
                // means that it's only chosen if no other better goal can be found at this time.
                case GOALS.PATROL:
                    int minInsistence = Int32.MaxValue;
                    foreach (var goals in Goals)
                    {
                        if(goals.Item1 == "isPatrolling") { continue; }
                        if (goals.Item3 < minInsistence) { minInsistence = goals.Item3; } // Item 3 - Insistence value of goal.
                    }
                    if (minInsistence <= 1) { Insistence += 10; }
                    UpdateGoalInsistence(Goals[i], Insistence, i);
                    break;
            }
        }
    }

    /// <summary>
    /// Get the current goal.
    /// </summary>
    /// <returns></returns>
    public Tuple<string, bool, int> GetCurrentGoal()
    {
        return currentGoal;
    }

    /// <summary>
    /// Determine which goal should be pursued next by getting their insistence values.
    /// </summary>
    /// <returns></returns>
    public HashSet<KeyValuePair<string, bool>> DetermineNewGoal()
    {
        // Update goal insistence values.
        DetermineGoalsInsistence();

        // Find goal with the highest insistence value, using the previously-populated goalIValues list.
        int max = -1;
        for (int i = 0; i < Goals.Count; i++)
        {
            // If new max is found, set current goal to the related goal and the goal condition.
            if (Goals.ElementAt(i).Item3 > max)
            {
                max = Goals.ElementAt(i).Item3;
                currentGoal = Goals.ElementAt(i);
            }
        }

        // Prepare output, converting from tuple to a key value pair which is used by the planner.
        HashSet<KeyValuePair<string, bool>> output = new HashSet<KeyValuePair<string, bool>>();
        output.Add(new KeyValuePair<string, bool>(currentGoal.Item1, currentGoal.Item2));

        // Always returns highest insistence goal and the desired goal state.
        return output;
    }
}