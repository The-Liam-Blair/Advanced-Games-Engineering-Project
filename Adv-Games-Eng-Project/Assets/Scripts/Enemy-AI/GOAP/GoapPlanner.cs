using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used to create action plans.
/// </summary>
public class GoapPlanner
{
    
	/// <summary>
	/// Tries to create a plan using the given agent, action list, world state and desired goal state.
	/// </summary>
	/// <param name="agent">Agent who the plan is for.</param>
	/// <param name="availableActions">THe list of actions the agent can do.</param>
	/// <param name="worldState">The current world state for this agent.</param>
	/// <param name="goal">The desired goal state which the agent is trying to fulfill.</param>
	/// <returns>An action plan if a plan is found, otherwise null.</returns>
	public Queue<GoapAction> plan(GameObject agent,
								  HashSet<GoapAction> availableActions, 
	                              HashSet<KeyValuePair<string,bool>> worldState, 
	                              HashSet<KeyValuePair<string,bool>> goal) 
	{
		// reset the actions so we can start fresh with them
		foreach (GoapAction a in availableActions) {
			a.doReset ();
		}

		// check what actions can run using their checkProceduralPrecondition
		HashSet<GoapAction> usableActions = new HashSet<GoapAction> ();
		foreach (GoapAction a in availableActions) {
			if ( a.checkProceduralPrecondition(agent) )
				usableActions.Add(a);
		}
		
		// we now have all actions that can run, stored in usableActions

		// build up the tree and record the leaf nodes that provide a solution to the goal.
		List<Node> leaves = new List<Node>();

		// build graph
		Node start = new Node (null, 0, worldState, null);
		bool success = buildGraph(start, leaves, usableActions, goal);

		if (!success) {
			// oh no, we didn't get a plan
			Debug.Log("NO PLAN");
			return null;
		}

		// get the cheapest leaf
		Node cheapest = null;
		foreach (Node leaf in leaves) {
			if (cheapest == null)
				cheapest = leaf;
			else {
				if (leaf.runningCost < cheapest.runningCost)
					cheapest = leaf;
			}
		}

		// get its node and work back through the parents
		List<GoapAction> result = new List<GoapAction> ();
		Node n = cheapest;
		while (n != null) {
			if (n.action != null) {
				result.Insert(0, n.action); // insert the action in the front
			}
			n = n.parent;
		}
		// we now have this action list in correct order

		Queue<GoapAction> queue = new Queue<GoapAction> ();
		foreach (GoapAction a in result) {
			queue.Enqueue(a);
		}

		// hooray we have a plan!
		return queue;
	}

	 /// <summary>
	 /// Bulds the node graph, using A* to find the best node path. Each node represents a new world state, and each edge represents an action
	 /// being taken to reach that new node. The cheapest path is then found by calculating and adding each action's cost, and finding the cheapest action set.
	 /// Recursive function.
	 /// </summary>
	 /// <param name="parent">Parent node of the current node being considered.</param>
	 /// <param name="leaves">Child node(s) of the current node.</param>
	 /// <param name="usableActions">Actions that can be completed in this currently-simulated world state (Each action's preconditions are satisfied).</param>
	 /// <param name="goal">Desired goal state.</param>
	 /// <returns>True: an action set is found that satisfies the goal state, otherwise false.</returns>
    private bool buildGraph (Node parent, List<Node> leaves, HashSet<GoapAction> usableActions, HashSet<KeyValuePair<string,bool>> goal)
	{
		bool foundOne = false;

		// go through each action available at this node and see if we can use it here
		foreach (GoapAction action in usableActions) {

			// if the parent state has the conditions for this action's preconditions, we can use it here
			if ( inState(action.Preconditions, parent.state) ) {

				// apply the action's effects to the parent state
				HashSet<KeyValuePair<string,bool>> currentState = populateState (parent.state, action.Effects);
				//Debug.Log(GoapAgent.prettyPrint(currentState));
				Node node = new Node(parent, parent.runningCost+action.cost, currentState, action);

				if (inState(goal, currentState)) {
					// we found a solution!
					leaves.Add(node);
					foundOne = true;
				} else {
					// not at a solution yet, so test all the remaining actions and branch out the tree
					HashSet<GoapAction> subset = actionSubset(usableActions, action);
					bool found = buildGraph(node, leaves, subset, goal);
					if (found)
						foundOne = true;
				}
			}
		}

		return foundOne;
	}


     /// <summary>
	 /// Creates a subset of actions except the removeMe action. Used to branch out the node tree when building the graph.
	 /// </summary>
	 /// <param name="actions">List of (usable) actions.</param>
	 /// <param name="removeMe">Action, that is to be explicitly removed from this subset.</param>
	 /// <returns>The action subset.</returns>
	private HashSet<GoapAction> actionSubset(HashSet<GoapAction> actions, GoapAction removeMe) {
		HashSet<GoapAction> subset = new HashSet<GoapAction> ();
		foreach (GoapAction a in actions) {
			if (!a.Equals(removeMe))
				subset.Add(a);
		}
		return subset;
	}
     
     /// <summary>
	 /// Checks the state of a currently-simulated world state and compares it with the desired goal state. If these states
	 /// match, then a path to a goal state has been found.
	 /// </summary>
	 /// <param name="test">The goal state.</param>
	 /// <param name="state">The simulated state being tested.</param>
	 /// <returns>True if the simulated world state 100% satisfies the goal state, otherwise false.</returns>
	private bool inState(HashSet<KeyValuePair<string,bool>> test, HashSet<KeyValuePair<string,bool>> state) {
		bool allMatch = true;
		foreach (KeyValuePair<string,bool> t in test) {
			bool match = false;
			foreach (KeyValuePair<string,bool> s in state) {
				if (s.Equals(t)) {
					match = true;
					break;
				}
			}
			if (!match)
				allMatch = false;
		}
		return allMatch;
	}
	

	 /// <summary>
	 /// Apply a change to a simulated world state. Essentially changes a node's world state based on an action edge's effects to propagate the node graph.
	 /// </summary>
	 /// <param name="currentState">Unmodified node state.</param>
	 /// <param name="stateChange">Action effect(s) which is used to change the node's state.</param>
	 /// <returns></returns>
	private HashSet<KeyValuePair<string,bool>> populateState(HashSet<KeyValuePair<string,bool>> currentState, HashSet<KeyValuePair<string,bool>> stateChange) {
		HashSet<KeyValuePair<string,bool>> state = new HashSet<KeyValuePair<string,bool>> ();
		// copy the KVPs over as new objects
		foreach (KeyValuePair<string,bool> s in currentState) {
			state.Add(new KeyValuePair<string,bool>(s.Key,s.Value));
		}

		foreach (KeyValuePair<string,bool> change in stateChange) {
			// if the key exists in the current state, update the Value
			bool exists = false;

			foreach (KeyValuePair<string,bool> s in state) {
				if (s.Equals(change)) {
					exists = true;
					break;
				}
			}

			if (exists) {
				state.RemoveWhere( (KeyValuePair<string,bool> kvp) => { return kvp.Key.Equals (change.Key); } );
				KeyValuePair<string,bool> updated = new KeyValuePair<string,bool>(change.Key,change.Value);
				state.Add(updated);
			}
			// if it does not exist in the current state, add it
			else {
				state.Add(new KeyValuePair<string,bool>(change.Key,change.Value));
			}
		}
		return state;
	}


	 /// <summary>
	 /// Node class used to instantiate the node graph and store at each node the simulated world state, action to get to the node, and accumulated cost.
	 /// </summary>
	private class Node {
		public Node parent;
		public float runningCost;
		public HashSet<KeyValuePair<string,bool>> state;
		public GoapAction action;
        public Node(Node parent, float runningCost, HashSet<KeyValuePair<string,bool>> state, GoapAction action) {
			this.parent = parent;
			this.runningCost = runningCost;
			this.state = state;
			this.action = action;
		}
	}

}


