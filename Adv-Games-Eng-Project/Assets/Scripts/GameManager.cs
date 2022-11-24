using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using Vector4 = System.Numerics.Vector4;

/// <summary>
/// Game manager handles multiple game systems, from spawning and using of items to generating enemies to generating the enemy stats output on the player camera.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Item pool
    private List<GameObject> itemPickups = new List<GameObject>();

    // Number of deactivated items, used to see if more should be spawned.
    private int deactivatedCount;

    // Prefabs for generating the item objects.
    [SerializeField] private GameObject[] itemPrefabs;


    // Used Item Objects (Projectiles, placed traps, etc).
    private List<GameObject> itemObjects = new List<GameObject>();

    // Pointer to handle the item objects pool.
    private int itemObjectsPoolPointer;

    // Prefabs for item objects.
    [SerializeField] private GameObject[] itemObjectPrefabs;

    // Enemy + waypoint object lists
    private List<GameObject> enemyObjects = new List<GameObject>();
    private List<GameObject> waypointObjects = new List<GameObject>();

    // Enemy + waypoint prefab
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject wayPointPrefab;

    // UI Text Fields, used to show GOAP-related information.
    // Current goal insistence values.
    private Text IOutput;

    // Current aggressiveness value.
    private Text AOutput;

    // Current action list (Actions in a plan that have been completed will be removed and updated in the UI).
    private Text AcOutput;

    // Camera
   [SerializeField] private GameObject cam;

   // 'Pointer' like value thats used to determine which enemy is currently being viewed on the enemy camera.
    private int viewEnemy;

    // Set to 0.3f every time the camera is switched or moved to another enemy to prevent multiple inputs per button press.
    private float inputSleep;


    private void Awake()
    {
        // Get references
        viewEnemy = 0; // 1 enemy initially spawned, so starts at 0

        IOutput = GameObject.Find("IOutput").GetComponent<Text>();
        AOutput = GameObject.Find("AOutput").GetComponent<Text>();
        AcOutput = GameObject.Find("AcOutput").GetComponent<Text>();


        // Init item pickups
        // 4 item pickups can exist in the map at one time.
        deactivatedCount = 0;
        itemPickups.Capacity = 4;
        for (int i = 0; i < itemPickups.Capacity; i++)
        {
            // why is there no resize methods for lists c#?!
            itemPickups.Add(Instantiate(itemPrefabs[0], GetValidPosition(), Quaternion.identity));

            // Init the items, adding the relevant components.
            itemPickups[i].transform.parent = GameObject.Find("ITEMPICKUPS").transform;
            itemPickups[i].SetActive(true);
            itemPickups[i].AddComponent<ItemPickup>();

            itemPickups[i].name = i.ToString();

            // Get stats for the item, which is currently random.
            (string, string, int) itemStats = GetRandomItemStats();

            // Update visual of the item pickup based on it's type and effect, to be able to determine item usage easily.
            UpdatePickupVisual(itemPickups[i], itemStats.Item1, itemStats.Item2);

            itemPickups[i].GetComponent<ItemPickup>().InitItem(itemStats.Item1, itemStats.Item2, itemStats.Item3);
        }

        // Init item objects; projectiles and walls. Only set active and used when an item is used.
        itemObjects.Capacity = 4;
        itemObjectsPoolPointer = 0;
        for (int i = 0; i < itemObjects.Capacity; i++)
        {
            itemObjects.Add(Instantiate(itemObjectPrefabs[0], Vector3.zero, Quaternion.identity));

            itemObjects[i].AddComponent<NavMeshObstacle>();
            itemObjects[i].GetComponent<NavMeshObstacle>().carving = true;
            itemObjects[i].GetComponent<NavMeshObstacle>().enabled = false;

            itemObjects[i].transform.parent = GameObject.Find("ITEMOBJECTS").transform;
            itemObjects[i].SetActive(false);
            itemObjects[i].AddComponent<ItemObject>();
        }

        // Init enemy objects list with 1 enemy with respective way point.
        waypointObjects.Add(Instantiate(wayPointPrefab, Vector3.zero, Quaternion.identity));
        enemyObjects.Add(Instantiate(enemyPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity));

        waypointObjects[0].name = "_WAYPOINT0";
        waypointObjects[0].transform.parent = GameObject.Find("WAYPOINTS").transform;
        
        enemyObjects[0].transform.parent = GameObject.Find("ENEMIES").transform;
    }

    private void LateUpdate()
    {
        // If an item was picked up...
        if (deactivatedCount > 0)
        {
            // Find the now-inactive item pickup.
            for (int i = 0; i < itemPickups.Count; i++)
            {
                if (!itemPickups[i].activeSelf)
                {
                    // Respawn it.
                    GenerateNewItem(itemPickups[i]);
                }
            }
        }

        // Handle inputs to swap between enemies on the enemy camera.
        // Checks if the sleep cooldown has ended, and is set to 0.33 after a successful camera switch.
        // So unable to swap camera for 0.1 seconds after previous swap to prevent multiple inputs from 1 button press.
        if (inputSleep < 0)
        {
            if (Input.GetAxisRaw("NextCam") > 0)
            {
                viewEnemy++;
            }
            else if (Input.GetAxisRaw("PreviousCam") > 0)
            {
                viewEnemy--;
            }
            inputSleep = 0.1f;
        }

        // Make sure the enemy index doesn't over/underflow.
        if(viewEnemy > enemyObjects.Count - 1) { viewEnemy = 0; }
        else if(viewEnemy < 0) { viewEnemy = enemyObjects.Count - 1; }


        // Reset text outputs.
        IOutput.text = string.Empty;
        AcOutput.text = string.Empty;

        // Update output with aggressiveness value.
        AOutput.text = enemyObjects[viewEnemy].GetComponent<GoapAgent>().aggressiveness.ToString();


        // For each goal the enemy has...
        foreach (Tuple<string, bool, int> goal in enemyObjects[viewEnemy].GetComponent<GoapAgent>().getWorldData()
                     .GetGoals())
        {
            // Get the goal's insistence value and output it.
            IOutput.text += goal.Item3 + "\n";
        }

        // For each action remaining in the action plan...
        foreach (GoapAction a in enemyObjects[viewEnemy].GetComponent<GoapAgent>().getCurrentActions())
        {
            // Output it in order of execution.
            if (AcOutput.text == string.Empty)
            {
                AcOutput.text += a._name;
            }
            else
            {
                AcOutput.text += "--> " + a._name;
            }
        }

        cam.transform.position = new Vector3(enemyObjects[viewEnemy].transform.position.x,
            cam.transform.position.y,
            enemyObjects[viewEnemy].transform.position.z);

        // Decrease the sleep timer by dt.
        inputSleep -= Time.deltaTime;
    }

    /// <summary>
    /// Add a new enemy instance to the game, as well as another waypoint object for it to use for patrolling. Called only
    /// when the Add Enemy UI Button is pressed.
    /// </summary>
    public void AddEnemy()
    {
        // Add new enemy and waypoint at valid positions.
        enemyObjects.Add(Instantiate(enemyPrefab, GetValidPosition(), Quaternion.identity));
        
        waypointObjects.Add(Instantiate(wayPointPrefab, Vector3.zero, Quaternion.identity));

        // Update their names which is used for way point allocation to a unique ID number.
        waypointObjects[waypointObjects.Count - 1].name = "_WAYPOINT" + (waypointObjects.Count - 1).ToString();
        waypointObjects[waypointObjects.Count - 1].transform.parent = GameObject.Find("WAYPOINTS").transform;

        enemyObjects[enemyObjects.Count - 1].transform.parent = GameObject.Find("ENEMIES").transform;
    }

    /// <summary>
    /// Essentially only used to rename enemies in order of spawning (See GoapAgent.Awake).
    /// </summary>
    /// <returns>Number of enemies that currently exist.</returns>
    public int GetEnemyCount()
    {
        return enemyObjects.Count;
    }

    /// <summary>
    /// When an item is picked up, deactivate it so it can be re-spawned later on.
    /// </summary>
    /// <param name="name">Item name, which is used as it's unique ID so the right item can be despawned.</param>
    public void ItemPickedUp(int name)
    {
        itemPickups[name].SetActive(false);
        deactivatedCount++;
    }

    /// <summary>
    /// Re-spawn an item pickup by making it active again and teleporting it to a new position.
    /// </summary>
    /// <param name="item">Item to be respawned.</param>
    private void GenerateNewItem(GameObject item)
    {
        // Item stats and usage are re-rolled when the item is re-spawned.
        (string, string, int) itemStats = GetRandomItemStats();
        item.GetComponent<ItemPickup>().InitItem(itemStats.Item1, itemStats.Item2, itemStats.Item3);
        
        UpdatePickupVisual(item, itemStats.Item1, itemStats.Item2);

        item.transform.position = GetValidPosition();
        item.SetActive(true);
        deactivatedCount--;
    }

    /// <summary>
    /// Called when an item is used, the item object is spawned, either a projectile or wall.
    /// </summary>
    /// <param name="attacker">The game object that used the item.</param>
    /// <param name="stats">Information relating to how the item should perform when used</param>
    public void EntityUseItem(GameObject attacker, Item stats)
    {
        Debug.Log("TYPE: " + stats.GetType() + " EFFECT: " + stats.GetEffect() + " DURATION: " + stats.duration);
        // Teleport the object to the correct position (in-front of the user), activate it and init it the object using the item stats.
        itemObjects[itemObjectsPoolPointer].transform.position = attacker.transform.position + (attacker.transform.forward * 2f);
        itemObjects[itemObjectsPoolPointer].SetActive(true);
        itemObjects[itemObjectsPoolPointer].GetComponent<ItemObject>().OnSpawn(stats);

        itemObjects[itemObjectsPoolPointer].GetComponent<Renderer>().material.color = (stats.owner == GameObject.Find("Player")) ? Color.cyan : Color.red;

        // Get the type of the item to determine how it should function.
        // For the item (depending on if it's thrown or placed):
        // - Edit mesh to suit the item type.
        // - Enable kinematic for placed objects so they don't move from physics or collisions.
        // - Set the scale of the object (Walls are large, projectiles are smaller).
        switch (stats.GetType())
        {
            // Throwable: item object is a projectile that will be fired in-front of the user at great speed, intended to hit a target.
            case "THROWABLE":
                itemObjects[itemObjectsPoolPointer].GetComponent<MeshFilter>().sharedMesh = itemObjectPrefabs[0].GetComponent<MeshFilter>().sharedMesh;
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().isKinematic = false;
                itemObjects[itemObjectsPoolPointer].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                // Push projectile forward.
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().AddForce(attacker.transform.forward.normalized * 20, ForceMode.Impulse);
                break;

            // Placeable: item object is a non-moving object that persists on the ground and blocks movement.
            case "WALL":
                itemObjects[itemObjectsPoolPointer].GetComponent<MeshFilter>().sharedMesh = itemObjectPrefabs[1].GetComponent<MeshFilter>().sharedMesh;
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().isKinematic = true;
                itemObjects[itemObjectsPoolPointer].transform.localScale = new Vector3(6, 1.5f, 2);
                itemObjects[itemObjectsPoolPointer].transform.rotation = Quaternion.LookRotation(attacker.transform.forward, Vector3.up);

                // Wall is placed slightly further infront of the user than normal since it's much larger: Stops collisions with the user.
                itemObjects[itemObjectsPoolPointer].transform.position = attacker.transform.position + (attacker.transform.forward * 2f);
                break;
        }
        
        // When the pool pointer reaches the length of the pool, loop it back to 0.
        itemObjectsPoolPointer++;
        if (itemObjectsPoolPointer > itemObjects.Count - 1) { itemObjectsPoolPointer = 0; }

    }
    /// <summary>
    /// Tries to find a valid position to place an object. Valid in this instance is a location on the nav mesh quad.
    /// Currently may return positions that intersect with walls but does not (majorly) impact gameplay. todo.
    /// </summary>
    /// <returns>The valid position found.</returns>
    Vector3 GetValidPosition()
    {
        int tries = 0;
        Vector3 pos = Vector3.zero;

        // Get the nav mesh bounds, which is the entire walkable area.
        Bounds NavMeshBounds = GameObject.Find("Walkable_Bounds").GetComponent<MeshRenderer>().bounds;

        NavMeshPath path = new NavMeshPath();


        // 100 attempts before algorithm bails.
        while (tries < 100)
        {
            tries++;
            // Generate a new position on the walkable area
            pos = new Vector3(Random.Range(NavMeshBounds.min.x, NavMeshBounds.max.x), 1.1f, Random.Range(NavMeshBounds.min.z, NavMeshBounds.max.z));

            // If this position is within 1 unit of a nearby nav mesh point...
            if (NavMesh.SamplePosition(pos, out NavMeshHit navMeshPos, 3f, NavMesh.AllAreas))
            {
                // Fetch the closest nav mesh point to the original position and test if a path can be made to it from origin (Origin lies on the nav mesh path).
                // If a valid path is found...
                if (NavMesh.CalculatePath(new Vector3(0, 1, 0), navMeshPos.position, NavMesh.AllAreas, path))
                {
                    // Return the valid position.
                    return navMeshPos.position;
                }
            }

        }
        // Algorithm should not bail and reach this return statement.
        Debug.Log("Unable to find valid position");
        return pos;
    }

    /// <summary>
    /// (Re)generates a newly-generated item's stats, randomly.
    /// </summary>
    /// <returns>The new stats of the item:
    /// <br></br>Item type,
    /// <br></br>Item effect,
    /// <br></br>Item effect duration.</returns>
    private (string, string, int) GetRandomItemStats()
    {
        // Random item effect and type assignment.
        // i = Item type (Thrown or a temporary wall placeable).
        // j = Item effect (Stun, slow or blind).
        // k = Item effect duration (Reduced by 50% for stun effects since stun is powerful, while doubled for wall items so that
        //     walls can be utilized better by enemy/player.
        int i, j, k;
        i = Random.Range(0, 10);
        j = Random.Range(0, 3);
        k = Random.Range(8, 12);

        string type = "", effect = "";

        // Item type
        switch (i)
        {
            // 10% chance for a wall item to be generated, otherwise a projectile.
            case 0:
                type = "WALL";
                k *= 2; // Walls' duration is doubled to give it more impact as it's only use is for blocking.
                break;

            default:
                type = "THROWABLE";
                break;
        }

        // Item effect
        switch (j)
        {
            case 0:
                effect = "STUN";
                k = Mathf.RoundToInt(k * 0.5f); // Stun is a really strong effect, so it's duration is halved.
                break;

            case 1:
                effect = "SLOW";
                break;

            case 2:
                effect = "BLIND";
                break;
        }

        // Walls have no effects, it's just a wall.
        if (type == "WALL") { effect = "NONE"; }
        
        // return (type, effect, duration)
        return (type, effect, k);

    }

    /// <summary>
    /// Update the visual of an item pickup by it's type and effect. Used to quickly determine what a specific item can do just from a glance.
    /// <br></br>Does not modify the look of the projectiles or physical placed trap, only the item pickup that enables usage of said item.
    /// </summary>
    /// <param name="itemPickup">Game Object pickup being modified.</param>
    /// <param name="type">Modifies mesh.<br></br>Square = placed.<br></br>capsule = thrown.</param>
    /// <param name="effect">Modifies colour.<br></br>White = slow.<br></br>Black = stun.<br></br>Blue = blind.</param>
    void UpdatePickupVisual(GameObject itemPickup, string type, string effect)
    {
        // Change mesh based on item type.
        switch (type)
        {
            case "WALL":
                itemPickup.GetComponent<MeshFilter>().sharedMesh = itemPrefabs[1].GetComponent<MeshFilter>().sharedMesh;
                break;

            case "THROWABLE":
                itemPickup.GetComponent<MeshFilter>().sharedMesh = itemPrefabs[0].GetComponent<MeshFilter>().sharedMesh;
                break;
        }

        // Change colour  based on item effect.
        switch (effect)
        {
            case "STUN":
                itemPickup.GetComponent<MeshRenderer>().material.color = Color.black;
                break;

            case "SLOW":
                itemPickup.GetComponent<MeshRenderer>().material.color = Color.white;
                break;

            case "BLIND":
                itemPickup.GetComponent<MeshRenderer>().material.color = Color.blue;
                break;

            case "NONE":
                itemPickup.GetComponent<MeshRenderer>().material.color = Color.green;
                break;
        }
    }
}