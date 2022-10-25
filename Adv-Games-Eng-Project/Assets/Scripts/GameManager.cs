using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using static UnityEditor.Progress;
using Quaternion = UnityEngine.Quaternion;
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
        // 10 item pickups can exist in the map at one time.
        deactivatedCount = 0;
        itemPickups.Capacity = 8;
        for (int i = 0; i < itemPickups.Capacity; i++)
        {
            // why is there no resize methods for lists c#?!
            itemPickups.Add(Instantiate(itemPrefabs[0], new Vector3(Random.Range(0, 10), 1, Random.Range(0, 10)), Quaternion.identity));

            // Init the items, adding the relevant components.
            itemPickups[i].transform.parent = GameObject.Find("ItemPickups").transform;
            itemPickups[i].SetActive(true);
            itemPickups[i].AddComponent<ItemPickup>();
            itemPickups[i].GetComponent<ItemPickup>().InitItem("PROJECTILE", "STUN", 2); // <-- this'll be randomised later on.
            itemPickups[i].name = i.ToString();
        }

        // Init item objects; projectiles and traps. Only set active and used when an item is used.
        itemObjects.Capacity = 8;
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
        item.transform.position = new Vector3(Random.Range(0, 10), 1, Random.Range(0, 10));
        item.SetActive(true);
        deactivatedCount--;
    }

    /// <summary>
    /// Called when an item is used, the item object is spawned, either a projectile or trap.
    /// </summary>
    /// <param name="attacker">The game object that used the item.</param>
    /// <param name="stats">Information relating to how the item should perform when used</param>
    public void SpawnItem(GameObject attacker, Item stats)
    {
        // Teleport the object to the correct position (in-front of the user), activate it and init it the object using the item stats.
        itemObjects[itemObjectsPoolPointer].transform.position = attacker.transform.position + attacker.transform.forward;
        itemObjects[itemObjectsPoolPointer].SetActive(true);
        itemObjects[itemObjectsPoolPointer].GetComponent<ItemObject>().OnSpawn(stats);
        
        // Get the type of the item to determine how it should function.
        switch (stats.GetType())
        {
            // Throwable: item object is a projectile that will be fired in-front of the user at great speed, intended to hit a target.
            case "THROWABLE":
                itemObjects[itemObjectsPoolPointer].GetComponent<Rigidbody>().AddForce(attacker.transform.forward * 20, ForceMode.Impulse);
                break;

            // Placeable: item object is a non-moving object that persists on the ground. Once stepped on by a target, it will apply it's debuff to it.
            case "PLACEABLE":
                // todo
                break;
        }
        
        // When the pool pointer reaches the length of the pool, loop it back to 0.
        itemObjectsPoolPointer++;
        if (itemObjectsPoolPointer > itemObjects.Count - 1) { itemObjectsPoolPointer = 0; }

    }
}