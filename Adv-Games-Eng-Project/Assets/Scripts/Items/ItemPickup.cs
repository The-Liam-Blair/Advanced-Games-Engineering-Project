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
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Enemy")
        {
            Item item = new Item();
            item.SetItem(itemType, itemEffect, itemDuration, other.gameObject);
            item.owner = other.gameObject;
            
            // Player inventory is a different class as it can uniquely hold key pieces, unlike the standard inventory which the enemy uses.
            if(other.gameObject.tag == "Player") {other.gameObject.GetComponent<PlayerInventory>().IteminInventory = item; }
            else { other.gameObject.GetComponent<Inventory>().IteminInventory = item; }

            GameObject.Find("_GAMEMANAGER").GetComponent<GameManager>().ItemPickedUp(int.Parse((name)));
        }
    }
}
