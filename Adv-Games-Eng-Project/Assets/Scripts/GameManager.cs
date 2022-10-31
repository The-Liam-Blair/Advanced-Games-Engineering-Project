using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Progress;
using Debug = UnityEngine.Debug;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

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


    private void Start()
    {
        // 4 item pickups can exist in the map at one time.
        deactivatedCount = 0;
        itemPickups.Capacity = 4;
        for (int i = 0; i < itemPickups.Capacity; i++)
        {
            // why is there no resize methods for lists c#?!
            itemPickups.Add(Instantiate(itemPrefabs[0], GetValidPosition(), Quaternion.identity));

            // Init the items, adding the relevant components.
            itemPickups[i].transform.parent = GameObject.Find("ItemPickups").transform;
            itemPickups[i].SetActive(true);
            itemPickups[i].AddComponent<ItemPickup>();
            itemPickups[i].name = i.ToString();

            // Get stats for the item, which is currently random.
            (string, string, int) itemStats = GetRandomItemStats();

            // Update visual of the item pickup based on it's type and effect, to be able to determine item usage easily.
            UpdatePickupVisual(itemPickups[i], itemStats.Item1, itemStats.Item2);

            itemPickups[i].GetComponent<ItemPickup>().InitItem(itemStats.Item1, itemStats.Item2, itemStats.Item3);
        }

        // Init item objects; projectiles and traps. Only set active and used when an item is used.
        itemObjects.Capacity = 4;
        itemObjectsPoolPointer = 0;
        for (int i = 0; i < itemObjects.Capacity; i++)
        {
            itemObjects.Add(Instantiate(itemObjectPrefabs[0], Vector3.zero, Quaternion.identity));
            itemObjects[i].transform.parent = GameObject.Find("ItemObjects").transform;
            itemObjects[i].SetActive(false);
            itemObjects[i].AddComponent<ItemObject>();
        }

    }

    private void Update()
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
    }

    // When an item is picked up, deactivate it so it can be re-spawned later on.
    public void ItemPickedUp(int name)
    {
        itemPickups[name].SetActive(false);
        deactivatedCount++;
    }

    // Re-spawn an item pickup by making it active again and teleporting it to a new position.
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
    /// Called when an item is used, the item object is spawned, either a projectile or trap.
    /// </summary>
    /// <param name="attacker">The game object that used the item.</param>
    /// <param name="stats">Information relating to how the item should perform when used</param>
    public void EntityUseItem(GameObject attacker, Item stats)
    {
        Debug.Log("TYPE: " + stats.GetType() + " EFFECT: " + stats.GetEffect() + " DURATION: " + stats.duration);
        // Teleport the object to the correct position (in-front of the user), activate it and init it the object using the item stats.
        itemObjects[itemObjectsPoolPointer].transform.position = attacker.transform.position + (attacker.transform.forward * 1.8f);
        itemObjects[itemObjectsPoolPointer].SetActive(true);
        itemObjects[itemObjectsPoolPointer].GetComponent<ItemObject>().OnSpawn(stats);

        itemObjects[itemObjectsPoolPointer].GetComponent<Renderer>().material.color = (stats.owner == GameObject.Find("Player")) ? Color.cyan : Color.red;

        // Get the type of the item to determine how it should function.
        // For the item (depending on if it's thrown or placed):
        // - Edit mesh to suit the item type.
        // - Enable kinematic for placed objects so they don't move from physics or collisions.
        // - Set the scale of the object (Traps are large, projectiles are smaller).
        switch (stats.GetType())
        {
            // Throwable: item object is a projectile that will be fired in-front of the user at great speed, intended to hit a target.
            case "THROWABLE":
                itemObjects[itemObjectsPoolPointer].GetComponent<MeshFilter>().sharedMesh = itemObjectPrefabs[0].GetComponent<MeshFilter>().sharedMesh;
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().isKinematic = false;
                itemObjects[itemObjectsPoolPointer].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                // Push projectile forward.
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().AddForce(attacker.transform.forward * 25, ForceMode.Impulse);
                break;

            // Placeable: item object is a non-moving object that persists on the ground. Once stepped on by a target, it will apply it's debuff to it.
            case "PLACEABLE":
                itemObjects[itemObjectsPoolPointer].GetComponent<MeshFilter>().sharedMesh = itemObjectPrefabs[1].GetComponent<MeshFilter>().sharedMesh;
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().isKinematic = true;
                itemObjects[itemObjectsPoolPointer].transform.localScale = new Vector3(2, 0.8f, 2);
                
                // Trap is placed slightly further infront of the user than normal since it's much larger: Stops collisions with the user.
                itemObjects[itemObjectsPoolPointer].transform.position = attacker.transform.position + (attacker.transform.forward * 2.5f);
                break;
        }
        
        // When the pool pointer reaches the length of the pool, loop it back to 0.
        itemObjectsPoolPointer++;
        if (itemObjectsPoolPointer > itemObjects.Count - 1) { itemObjectsPoolPointer = 0; }

    }

    Vector3 GetValidPosition()
    {
        int tries = 0;
        Vector3 pos = Vector3.zero;
        NavMeshPath path = new NavMeshPath();


        // 100 attempts before algorithm bails.
        while (tries < 100)
        {
            tries++;
            // Try a new random position to spawn the item pickup.
            pos = new Vector3(Random.Range(0, 30), 1f, Random.Range(-30, 30));

            // If this position is within 10 units of a nearby nav mesh point...
            if (NavMesh.SamplePosition(pos, out NavMeshHit navMeshPos, 3f, 1))
            {
                // Fetch the closest nav mesh point to the original position and test if a path can be made to it from origin (Origin lies on the nav mesh path).
                // If a valid path is found...
                if (NavMesh.CalculatePath(new Vector3(0, 1, 0), navMeshPos.position, NavMesh.AllAreas, path))
                {
                    // Return the valid position + (0, 1, 0) as nav mesh point's y value is 0, so the item will clip into the ground.
                    return navMeshPos.position + Vector3.up;
                }
            }

        }
        // Algorithm should not bail and reach this return statement.
        return pos;
    }

    private (string, string, int) GetRandomItemStats()
    {
        // Random item effect and type assignment.
        // i = Item type (Thrown or placed).
        // j = Item effect (Stun, slow or blind).
        // k = duration (Duration reduced by 50% for stunning effects since stuns are far stronger than slows or blinds).
        int i, j, k;
        i = Random.Range(0, 2);
        j = Random.Range(0, 3);
        k = Random.Range(6, 8);

        string type = "", effect = "";

        switch (i)
        {
            case 0:
                type = "THROWABLE";
                break;

            case 1:
                type = "PLACEABLE"; // todo: <-- Placed items not implemented yet!
                break;
        }

        switch (j)
        {
            case 0:
                effect = "STUN";
                k = Mathf.RoundToInt(k * 0.5f);
                break;

            case 1:
                effect = "SLOW";
                break;

            case 2:
                effect = "BLIND";
                break;
        }
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
            case "PLACEABLE":
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
        }
    }
}