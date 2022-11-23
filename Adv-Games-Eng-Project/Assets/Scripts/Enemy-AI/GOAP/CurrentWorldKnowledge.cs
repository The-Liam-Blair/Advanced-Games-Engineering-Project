using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
/// <summary>
/// Stores current world state and methods to freely modify it.
/// </summary>
public class CurrentWorldKnowledge
{
    // Goals as enums for readability.
    private enum GOALS
    {
        CHASEPLAYER,
        FINDITEM,
        CALL_PLAYERSIGHTED,
        PATROL
    };

    // Stores world changes as a key value pair of a fact about the world and if it's true or false.
    private HashSet<KeyValuePair<string, bool>> WorldData;

    // List of goals the enemy has.
    // String - Goal name.
    // Bool - Desired state for this goal.
    // Int - Goal's current insistence value.
    private List<Tuple<string, bool, int>> Goals;

    // Current goal being pursued.
    private Tuple<string, bool, int> currentGoal;

    // List of sighted items, mostly used for getting their location.
    public List<GameObject> ItemLocations;

    // Stores if a player used item (Projectile or trap) is sighted, used to avoid it, if it knows how to.
    private List<GameObject> PlayerProjectiles;

    public CurrentWorldKnowledge()
    {
        // Set initial world state to false for all facts.
        WorldData = new HashSet<KeyValuePair<string, bool>>();


        WorldData.Add(new KeyValuePair<string, bool>("foundPlayer", false));  // Has the player been sighted?
        WorldData.Add(new KeyValuePair<string, bool>("attackPlayer", false)); // Is the CHASEPLAYER goal being pursued?

        WorldData.Add(new KeyValuePair<string, bool>("aimingAtPlayer", false)); // Aiming (item) at player?
        WorldData.Add(new KeyValuePair<string, bool>("hasItem", false));        // Does the enemy have an item?
        WorldData.Add(new KeyValuePair<string, bool>("hasUsedItem", false));    // Did the enemy use an item in this current action sequence?

        WorldData.Add(new KeyValuePair<string, bool>("RECEIVECALL_playerSighting", false)); // Did the enemy hear a player sighted call from another enemy?
        WorldData.Add(new KeyValuePair<string, bool>("moveToPlayerSighting", false));       // Is the enemy investigating a heard player sighting?

        WorldData.Add(new KeyValuePair<string, bool>("isPatrolling", false)); // Is the enemy currently patrolling?



        // Set list of all possible goals to choose from, and a placeholder insistence value of -1.
        Goals = new List<Tuple<string, bool, int>>();

        Goals.Add(new Tuple<string, bool, int>("attackPlayer", true, -1));         // Move to the player and attack them.
        Goals.Add(new Tuple<string, bool, int>("hasItem", true, -1));              // Acquire a seen item.
        Goals.Add(new Tuple<string, bool, int>("moveToPlayerSighting", true, -1)); // Investigate a player sighting called from another enemy.
        Goals.Add(new Tuple<string, bool, int>("isPatrolling", true, -1));         // Default goal if no other is picked: Patrol w.r.t aggressiveness.


        ItemLocations = new List<GameObject>();
    }

    /// <summary>
    /// Add an item pickup's location to the knowledge base.
    /// </summary>
    /// <param name="item">The item game object that has been sighted.</param>
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

    /// <summary>
    /// Remove an item pickup's location from the knowledge base.
    /// </summary>
    /// <param name="item">Seen item to be removed.</param>
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
    /// Exists on a separate function so it can be called repeatedly to constantly update each goal's insistence.
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
                    if (ItemLocations.Count > 0 && GetFactState("hasItem", false)) { Insistence = 66; }
                    UpdateGoalInsistence(Goals[i], Insistence, i);
                    break;

                case GOALS.CALL_PLAYERSIGHTED:
                    Insistence = -1;
                    if(GetFactState("RECEIVECALL_playerSighting", true)) { Insistence = 90; }
                    UpdateGoalInsistence(Goals[i], Insistence, i);
                    break;

                // Patrol goal can only be chosen if no other goal has an insistence value of 1 or higher. Essentially, this
                // means that it's only chosen if no other better goal can be found at this time.
                case GOALS.PATROL:
                    int minInsistence = Int32.MaxValue;
                    foreach (var goals in Goals)
                    {
                        if (goals.Item1 == "isPatrolling") { continue; }
                        if (goals.Item3 < minInsistence) { minInsistence = goals.Item3; } // Item 3 - Insistence value of goal.
                    }
                    if (minInsistence <= 1) { Insistence += 13; }
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
    /// Get all goals.
    /// </summary>
    /// <returns>List of all goals.</returns>
    public List<Tuple<string, bool, int>> GetGoals()
    {
        return Goals;
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