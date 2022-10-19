using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
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

    public CurrentWorldKnowledge WorldData;

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

        GetComponent<NavMeshAgent>().speed = 8f;

        WorldData = new CurrentWorldKnowledge();
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

			/*MovableComponent movable = (MovableComponent) gameObj.GetComponent(typeof(MovableComponent));
			if (movable == null) {
				Debug.Log("<color=red>Fatal error:</color> Trying to move an Agent that doesn't have a MovableComponent. Please give it one.");
				fsm.popState(); // move
				fsm.popState(); // perform
				fsm.pushState(idleState);
				return;
			}

			float step = movable.moveSpeed * Time.deltaTime;
			gameObj.transform.position = Vector3.MoveTowards(gameObj.transform.position, action.target.transform.position, step);

			if (gameObj.transform.position.Equals(action.target.transform.position) ) {
				// we are at the target location, we are done
				action.setInRange(true);
				fsm.popState();
			}*/
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
			if(a.isActionEnabled()) { availableActions.Add(a); }
        }
		Debug.Log("Found actions: "+prettyPrint(actions));
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

	// Stores current world state and methods to freely modify it.
    public class CurrentWorldKnowledge
    {
		// Stores world changes as a key value pair of a fact about the world and if it's true or false.
        public HashSet<KeyValuePair<string, bool>> WorldData;

        public CurrentWorldKnowledge()
        {
            WorldData = new HashSet<KeyValuePair<string, bool>>();
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
		/// Modifies a fact's state. Fact needs to already exist in the current world state.
		/// </summary>
		/// <param name="newData">The fact that will be modified.</param>
		/// <returns>True: Successful fact modification <br></br> False: Fact was not found within the knowledge base, operation failed.</returns>
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
            return false;
        }

		/// <summary>
		/// Removes all facts from the knowledge base.
		/// </summary>
        public void ClearData()
        {
			WorldData.Clear();
        }

		// List of facts string output.
        public override string ToString()
        {
            string output = "";

            foreach (var data in WorldData)
            {
                output += data.ToString() + "\n";
            }
            return output;
        }

    }

}
