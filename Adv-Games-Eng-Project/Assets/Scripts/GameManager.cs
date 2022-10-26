using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* public class GameManager : MonoBehaviour
{
    // Item pool
    public static GameObject[] items = new GameObject[10];

    // Pool pointer.
    public int itemIndex = 0;

    // Prefabs for generating the item objects.
    [SerializeField] private GameObject itemPrefab;
    
    private void Start()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = Instantiate(itemPrefab, transform);
            items[i].SetActive(false);
            items[i].AddComponent<Item>();
        }
    }

    public void ItemPickedUp(Item item, GameObject owner)
    {
        items[itemIndex].GetComponent<Item>().owner = owner;
    }
}

*/