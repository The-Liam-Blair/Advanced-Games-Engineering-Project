using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

/// <summary>
/// The collectible item, which handles assigning items to users.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    private string itemType;
    private string itemEffect;
    private int itemDuration;

    /// <summary>
    /// Item stat constructor.
    /// </summary>
    public void InitItem(string _itemType, string _itemEffect, int _duration)
    {
        itemType = _itemType;
        itemEffect = _itemEffect;
        itemDuration = _duration;
    }

    /// <summary>
    /// When a user touches the collectible item, give it to the user by assigning them as the owner. Also alerts the game manager that the item was picked up, so it can be queued
    /// for respawning later.
    /// </summary>
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
            GameObject.FindGameObjectWithTag("Enemy").GetComponent<GoapAgent>().getWorldData().RemoveItemLocation(gameObject);
        }
    }
}
