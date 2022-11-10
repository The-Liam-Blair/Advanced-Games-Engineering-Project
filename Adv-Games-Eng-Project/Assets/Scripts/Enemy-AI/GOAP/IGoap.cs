using UnityEngine;
using System.Collections;

/**
 * Collect the world data for this Agent that will be
 * used for GOAP planning.
 */
using System.Collections.Generic;


/// <summary>
/// Any agent that wants to use GOAP must implement this interface. It provides information to the GOAP planner so it can plan what actions to use.
/// it also provides an interface for the planner to give
/// feedback to the Agent and report success / failure.
/// </summary>
public interface IGoap
{
	/// <summary>
	/// Retrieves an agent's current world state.
	/// </summary>
	/// <returns>The agent's world state.</returns>
    HashSet<KeyValuePair<string,bool>> getWorldState ();


	/// <summary>
	/// Calculates which goal should be selected. Called before planning.
	/// </summary>
	/// <returns>The desired goal state which satisfies a goal.</returns>
	HashSet<KeyValuePair<string,bool>> createGoalState ();


    /// <summary>
    /// Called when the planning process resulted in no plan being found, so the goal state was impossible to be reached.
    /// Normally impossible to reach during standard gameplay, this is kept only for debugging purposes.
    /// </summary>
    /// <param name="failedGoal">The impossible-to-reach goal state.</param>
    void planFailed (HashSet<KeyValuePair<string,bool>> failedGoal);
    

	/// <summary>
	/// Called when a plan was found. Kept for debugging purposes currently.
	/// </summary>
	/// <param name="goal">Goal state that was reached.</param>
	/// <param name="actions">List of actions in order that will achieve the goal state.</param>
	void planFound (HashSet<KeyValuePair<string, bool>> goal, Queue<GoapAction> actions);


	/// <summary>
	/// Called when all actions in a plan have been successfully completed, kept for debugging purposes.
	/// </summary>
	void actionsFinished ();


	/// <summary>
	/// Called when an action was aborted, and so the entire plan was scrapped. Kept for debugging purposes.
	/// </summary>
	/// <param name="aborter">The action that resulted in the abortion.</param>
	void planAborted (GoapAction aborter);


    /// <summary>
    /// Movement state of the agent. As such, this state is the most common state the agent will be in, and includes all real-time calculations such as
    /// checking goal insistence and handling ray casts for sight.
    /// </summary>
    /// <param name="nextAction">The current action, which the agent is currently moving towards.</param>
    /// <returns>True if the agent arrived at the action location or the action was bailed. False if the agent hasn't arrived at the action location yet.</returns>
	bool moveAgent(GoapAction nextAction);
}

