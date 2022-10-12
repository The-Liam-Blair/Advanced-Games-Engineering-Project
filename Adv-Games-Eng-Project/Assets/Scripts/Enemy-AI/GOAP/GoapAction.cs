
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

// Generic action class definition
public abstract class GoapAction : MonoBehaviour {

	// Preconditions need to be satisfied before an action is able to be run.
	private HashSet<KeyValuePair<string,object>> preconditions;

	// Effects are the outcomes of running an action to completion.
	private HashSet<KeyValuePair<string,object>> effects;
    
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

	public GoapAction() {
		preconditions = new HashSet<KeyValuePair<string, object>> ();
		effects = new HashSet<KeyValuePair<string, object>> ();
	}

	public void doReset() {
		inRange = false;
		target = null;
		reset ();
        currentMovementCost = 0f;
        currentCostTooHigh = false;
        resetCount = 0;
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
	public void addPrecondition(string key, object value) 
    {
		preconditions.Add (new KeyValuePair<string, object>(key, value) );
	}
    public void removePrecondition(string key) 
    {
		KeyValuePair<string, object> remove = default(KeyValuePair<string,object>);
		foreach (KeyValuePair<string, object> kvp in preconditions) 
        {
            if (kvp.Key.Equals(key))
            {
                remove = kvp;
            }
        }
        if (!default(KeyValuePair<string, object>).Equals(remove))
        {
            preconditions.Remove(remove);
        }
    }

    public void addEffect(string key, object value) 
    {
		effects.Add (new KeyValuePair<string, object>(key, value) );
	}
    public void removeEffect(string key) 
    {
		KeyValuePair<string, object> remove = default(KeyValuePair<string,object>);
		foreach (KeyValuePair<string, object> kvp in effects) 
        {
            if (kvp.Key.Equals(key))
            {
                remove = kvp;
            }
        }
        if (!default(KeyValuePair<string, object>).Equals(remove))
        {
            effects.Remove(remove);
        }
    }

    // Action preconditions and effects getters.
    public HashSet<KeyValuePair<string, object>> Preconditions 
    {
		get 
        {
			return preconditions;
		}
	}
    public HashSet<KeyValuePair<string, object>> Effects 
    {
		get 
        {
			return effects;
		}
	}

    // Credit to 'mcapousek' for the path length calculation:
    // https://forum.unity.com/threads/getting-the-distance-in-nav-mesh.315846/
    //
    // Returns true walking distance length of a given nav mesh path.
    public float GetPathLength(NavMeshPath path)
    {
		float pathLength = 0f;

        // Handle cases when only one corner is recorded in the path, which does not work in the below algorithm.
        if(path.corners.Length < 2) { return Vector3.Distance(gameObject.transform.position, path.corners[0]); }


        for (int i = 1; i < path.corners.Length; i++)
        {
            pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return pathLength;
    }
}