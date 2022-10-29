using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// todo: Make inventory abstract for enemy + player inventories
public class Inventory : MonoBehaviour
{

    [SerializeField] private GameObject itemObject;
    
    public Item IteminInventory
    {
        get;
        set;
    }

    private void Awake()
    {
        IteminInventory = null;
    }


    public void UseItem()
    {
        if (IteminInventory != null)
        {
            GameObject.Find("_GAMEMANAGER").GetComponent<GameManager>().EntityUseItem(gameObject, IteminInventory);
            IteminInventory = null;
        }
    }
}