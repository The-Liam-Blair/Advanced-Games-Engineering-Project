using UnityEngine;
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
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Defines the agent that follows the GOAP system. Includes agent states and local variables such as actions and world data.
/// </summary>
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
    private CurrentWorldKnowledge WorldData;

	// Records how long the enemy has chased the player in 1 action/goal. Forces the chase to stop after a set limit.
    public float playerChaseTime;

	// Cooldown prevents enemy from attacking the player while it is above 0f. Forces enemy to stop repeatedly chasing the player.
    public float playerChaseCooldown;

    public float sightingChaseCooldown;

    // Value represents how aggressive the enemy should be. Higher aggressiveness = Enemy is given approximate player position with increasing accuracy as
	// the value increases. Reduced significantly after executing a chase plan. Used to make the enemy forcefully encounter the player more often.
    public float aggressiveness;

	/// <summary>
	/// On awake, initialise the state machine with 3 states: idle, move and perform.
	/// Then load the actions and start the state machine in the idle state.
	/// </summary>
    void Awake() {
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

        // Set enemy name to number of enemies in the scene currently, starting at 0. E.g., first enemy is known as "0".
        // Must be done in awake function as enemy name is used as an indexer for assigning movement waypoints and awake functions are called before start functions.
        name = GameObject.Find("_GAMEMANAGER").GetComponent<GameManager>().GetEnemyCount().ToString();

    }
	

	void Update () {
        stateMachine.Update (this.gameObject);
    }
	
	/// <summary>
	/// Local world knowledge getter.
	/// </summary>
	/// <returns>World knowledge</returns>
    public CurrentWorldKnowledge getWorldData()
    {
        return WorldData;
    }

	/// <summary>
	/// Add action to the action list.
	/// </summary>
	/// <param name="a">Action to be added.</param>
    public void addAction(GoapAction a) {
		availableActions.Add (a);
	}

	/// <summary>
	/// Specfic action getter
	/// </summary>
	/// <param name="action">Action trying to retrieve</param>
	/// <returns>The action, or null if it doesn't exist in the list.</returns>
	public GoapAction getAction(Type action) {
		foreach (GoapAction g in availableActions) {
			if (g.GetType().Equals(action) )
			    return g;
		}
		return null;
	}
	
	/// <summary>
	/// Action remover
	/// </summary>
	/// <param name="action">Action to remove.</param>
	public void removeAction(GoapAction action) {
		availableActions.Remove (action);
	}

	/// <summary>
	/// Action list size getter. Used to determine if all actions in a plan have been completed.
	/// </summary>
	/// <returns>True: actions still exist, otherwise false.</returns>
	private bool hasActionPlan() {
		return currentActions.Count > 0;
	}

	/// <summary>
	/// Action plan getter
	/// </summary>
	/// <returns>Action list.</returns>
    public Queue<GoapAction> getCurrentActions()
    {
        return currentActions;
    }

	/// <summary>
	/// Idle agent state. Agent is only in this state if a plan has been completed or aborted, and so a new plan is created.
	/// </summary>
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
	
	/// <summary>
	/// Movement state, where the agent is trying to move to an action's target location. The most common state to be in, it's only accessed if the current
	/// action has a range/positional requirement. Once the agent has reached the relevant location, they exit this state.
	/// </summary>
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
	
	/// <summary>
	/// Perform state, where the agent is able to perform the current action in the plan.
	/// </summary>
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


   public void ReceiveCall(GameObject caller, GameObject target, string callerGoal)
   {

       if (callerGoal == "CHASEPLAYER" && sightingChaseCooldown < 0f)
       {
           sightingChaseCooldown = 5f;
           WorldData.EditDataValue(new KeyValuePair<string, bool>("RECEIVECALL_playerSighting", true));
           StartCoroutine(PlayerSightingExpirationCoroutine(3));
       }
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
	/// <br>(Basic implementation, the enemy <strong>CAN</strong> perform actions if it's current action's target reaches the enemy, but this should be rare).</br>
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

	/// <summary>
	/// Slows the enemy for a given duration. Slow is a direct -66% movement modifier.
	/// </summary>
	/// <param name="duration">Duration of the slow effect.</param>
    IEnumerator SlowCoroutine(int duration)
    {
        GetComponent<NavMeshAgent>().speed = 3.33f; // Apply speed debuff.
        yield return new WaitForSeconds(duration);
        GetComponent<NavMeshAgent>().speed = 10f; // Inverse the debuff to get the normal speed again.

        yield return null;
    }

    IEnumerator PlayerSightingExpirationCoroutine(int expireTime)
    {
        yield return new WaitForSeconds(expireTime);
        WorldData.EditDataValue(new KeyValuePair<string, bool>("RECEIVECALL_playerSighting", false));
    }

}