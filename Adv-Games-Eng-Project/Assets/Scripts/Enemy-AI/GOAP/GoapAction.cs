
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
    // Knowledge is gained from witnessing the player use an action (Small increase), or being a victim of the item (Large increase).
    // If knowledge >= 100 : Enemy will be able to utilise that item or game mechanic against the player.
    // If knowledge >= 200 : Enemy additionally will try to counter player-made items or mechanics used against the enemy (Dodging, dismantling or avoiding).
    protected int actionKnowledge;
    protected bool canCounterAction;

    public string _name;

    public CurrentWorldKnowledge WorldData;

    void Start()
    {
        WorldData = GetComponent<GoapAgent>().WorldData;
        _name = GetType().FullName;

    }

    public GoapAction() {
		preconditions = new HashSet<KeyValuePair<string,bool>> ();
		effects = new HashSet<KeyValuePair<string,bool>> ();

        actionKnowledge = 0;
        canCounterAction = false;
    }

    public void doReset() {
		inRange = false;
		target = null;
		reset ();
        currentMovementCost = 0f;
        currentCostTooHigh = false;
        resetCount = 0;
        WorldData = GetComponent<GoapAgent>().WorldData;
    }

    // Reset action after it's been used.
    public abstract void reset();
    
	// Check if the action has been completed yet.
	public abstract bool isDone();
    
    // Check if the action is capable of being run later on in the current world state, used during planning.
	public abstract bool checkProceduralPrecondition(GameObject agent);


    // Run the action, returns true if the action was successful.
    public virtual bool perform(GameObject agent)
    {
        return !currentCostTooHigh;
    }


    // Check if the action requires the agent to be in range of the target.
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

	// Add or remove effects or preconditions to an action.
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


    // Action enabled getter
    public bool isActionEnabled()
    {
        return actionEnabled;
    }


    // Returns true walking distance length of a given nav mesh path.
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
    /// Called after an action is successfully performed; updates the world state fact base using the effect(s) of the action.
    /// </summary>
    public void UpdateWorldState()
    {
        foreach (var effect in Effects)
        {
           WorldData.EditDataValue(effect);
        }
    }



    // Enemy gains knowledge from being the victim of actions similar to actions done by the player.
    public void IncreaseKnowledge(int knowledge)
    {
        actionKnowledge += knowledge;

        if (knowledge >= 100) { actionEnabled = true; GetComponent<GoapAgent>().addAction(this); }
        if (knowledge >= 200) { canCounterAction = true; }
    }
}