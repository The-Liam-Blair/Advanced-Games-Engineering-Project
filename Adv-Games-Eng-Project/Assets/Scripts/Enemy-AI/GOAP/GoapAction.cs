
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

// Generic action class definition
public abstract class GoapAction : MonoBehaviour {

	// Preconditions need to be satisfied before an action is able to be run.
	private HashSet<KeyValuePair<string, bool>> preconditions;

	// Effects are the outcomes of running an action to completion.
	private HashSet<KeyValuePair<string,bool>> effects;
    
	private bool inRange = false;
    
    // Arbitrary action cost used to denote the time cost for performing an action. Factored into planning.
    public float cost;

    // Actual cost of running this action (including moving) in real time. Used to determine if an action should be cancelled and the plan revised.
    public float currentMovementCost;
    public bool currentCostTooHigh;

    // Used exclusively when near the target to reduce the movement cost to stop the action from aborting, to record the number of times this has occurred so
    // that this can only occur a limited amount of times to prevent an infinite loop of an action being endlessly pursued.
    public int resetCount;
    
    // Potential target of an action. Can be null if no target is required.
	public GameObject target;

    protected bool actionEnabled;

    // Representation of an enemy's understanding of a given item or game mechanic.
    // Knowledge is gained from being hurt by the item. todo: maybe from seeing the item in use as well?
    // If knowledge >= 100 : Enemy will be able to utilise that item or game mechanic against the player.
    // If knowledge >= 200 : Enemy additionally will try to counter player-made items or mechanics used against the enemy, primarily dodging.
    protected int actionKnowledge;
    protected bool canCounterAction;
    
    public string _name;

    // Reference to the agent's world knowledge.
    public CurrentWorldKnowledge WorldData;

    void Start()
    {
        WorldData = GetComponent<GoapAgent>().getWorldData();
        _name = GetType().FullName;

    }

    public GoapAction() {
		preconditions = new HashSet<KeyValuePair<string,bool>> ();
		effects = new HashSet<KeyValuePair<string,bool>> ();

        actionKnowledge = 0;
        canCounterAction = false;
    }

    /// <summary>
    /// Resets the action. All action properties are reset every time a new plan starts to be planned.
    /// </summary>
    public void doReset() {
		inRange = false;
		target = null;
		reset ();
        currentMovementCost = 0f;
        currentCostTooHigh = false;
        resetCount = 0;
        WorldData = GetComponent<GoapAgent>().getWorldData();
    }

    /// <summary>
    /// Reset action after it's been used.
    /// </summary>
    public abstract void reset();
    
	/// <summary>
    /// Check if the action has been completed yet.
    /// </summary>
    /// <returns>True if the action has been completed, false if it still needs more time for completion (In the perform state).</returns>
	public abstract bool isDone();
    
    /// <summary>
    /// Check if the action is capable of being run later on in the current simulated world state, used during planning.
    /// </summary>
    /// <param name="agent">The agent who would be carrying out the action.</param>
    /// <returns>True if the action is completable, false if it is not.</returns>
	public abstract bool checkProceduralPrecondition(GameObject agent);


    /// <summary>
    /// Action is run, in the perform state. Also contains implementation of checking if an action's cost is too high and so should be bailed.
    /// </summary>
    /// <param name="agent">Agent performing the action.</param>
    /// <returns>False if the action fails, true otherwise.
    /// <br></br> True return does NOT mean the action is over necessarily, it means that the perform loop was successful- more
    /// time may be needed to complete the action.</returns>
    public virtual bool perform(GameObject agent)
    {
        return !currentCostTooHigh;
    }


    /// <summary>
    /// Check if the action requires the agent to be in range of the target.
    /// </summary>
    /// <returns>True if a range/positional requirement exists for this action, otherwise false.</returns>
    public abstract bool requiresInRange ();
	
    
    // Range requirement getter and setter.
	public bool isInRange () 
    {
		return inRange;
	}
    public void setInRange(bool inRange) 
    {
		this.inRange = inRange;
	}

	// Effect and precondition adders/removers.
	public void addPrecondition(string key, bool value) 
    {
		preconditions.Add (new KeyValuePair<string,bool>(key, value) );
	}
    public void removePrecondition(string key) 
    {
		KeyValuePair<string,bool> remove = default(KeyValuePair<string,bool>);
		foreach (KeyValuePair<string,bool> kvp in preconditions) 
        {
            if (kvp.Key.Equals(key))
            {
                remove = kvp;
            }
        }
        if (!default(KeyValuePair<string,bool>).Equals(remove))
        {
            preconditions.Remove(remove);
        }
    }

    public void addEffect(string key, bool value) 
    {
		effects.Add (new KeyValuePair<string,bool>(key, value) );
	}
    public void removeEffect(string key) 
    {
		KeyValuePair<string,bool> remove = default(KeyValuePair<string,bool>);
		foreach (KeyValuePair<string,bool> kvp in effects) 
        {
            if (kvp.Key.Equals(key))
            {
                remove = kvp;
            }
        }
        if (!default(KeyValuePair<string,bool>).Equals(remove))
        {
            effects.Remove(remove);
        }
    }

    // Action preconditions and effects getters.
    public HashSet<KeyValuePair<string,bool>> Preconditions 
    {
		get 
        {
			return preconditions;
		}
	}
    public HashSet<KeyValuePair<string,bool>> Effects 
    {
		get 
        {
			return effects;
		}
	}


    /// <summary>
    /// Checks if the action is enabled. A disabled action is not factored into the planning process.
    /// </summary>
    /// <returns>True if enabled, otherwise false.</returns>
    public bool isActionEnabled()
    {
        return actionEnabled;
    }


    /// <summary>
    /// Calculates the float distance using a nav mesh path between the start and end point. Distance represents actual travel distance the agent needs to travel.
    /// </summary>
    /// <param name="path">The nav mesh path being tested for distance.</param>
    /// <returns>The actual movement distance as a float value.</returns>
    public float GetPathLength(NavMeshPath path)
    {
		float pathLength = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return pathLength;
    }

    /// <summary>
    /// Called after an action is successfully performed, the action's effects are used to modify the agent's world knowledge.
    /// </summary>
    public void UpdateWorldState()
    {
        foreach (var effect in Effects)
        {
           WorldData.EditDataValue(effect);
        }
    }



    /// <summary>
    /// Increase an agent's knowledge of this action. Called when the agent is a victim of a similar action.
    /// When an agent's action knowledge exceeds certain thresholds, they may be able to learn the related action and then counter it.
    /// </summary>
    /// <param name="knowledge">Amount of knowledge to award to the agent.</param>
    public void IncreaseKnowledge(int knowledge)
    {
        actionKnowledge += knowledge;

        if (!actionEnabled && actionKnowledge >= 100)    { actionEnabled = true; GetComponent<GoapAgent>().addAction(this); }
        if (!canCounterAction && actionKnowledge >= 200) { canCounterAction = true; }
    }
}