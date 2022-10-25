using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private string itemType;
    private string itemEffect;
    private int itemDuration;

    public void InitItem(string _itemType, string _itemEffect, int _duration)
    {
        itemType = _itemType;
        itemEffect = _itemEffect;
        itemDuration = _duration;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Item item = new Item();
            item.SetItem(itemType, itemEffect, itemDuration, other.gameObject);
            item.owner = other.gameObject;
            other.gameObject.GetComponent<PlayerInventory>().IteminInventory = item;
            GameObject.Find("_GAMEMANAGER").GetComponent<GameManager>().ItemPickedUp(int.Parse((name)));
        }
    }
}
