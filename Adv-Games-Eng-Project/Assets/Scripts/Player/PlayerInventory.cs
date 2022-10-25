using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// todo: Make inventory abstract for enemy + player inventories
public class PlayerInventory : MonoBehaviour
{

    [SerializeField] private GameObject itemObject;

    public int keyPieceCount
    {
        get;
        set;
    }

    public Item IteminInventory
    {
        get;
        set;
    }

    private void Awake()
    {
        keyPieceCount = 0;
        IteminInventory = null;
    }


    public void UseItem()
    {
        if (IteminInventory != null)
        {
            GameObject.Find("_GAMEMANAGER").GetComponent<GameManager>().SpawnItem(gameObject, IteminInventory);
            IteminInventory = null;
        }
    }
}
