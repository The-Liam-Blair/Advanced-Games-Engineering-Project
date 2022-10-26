using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private string itemType = "THROWABLE";
    private string itemEffect = "STUN";
    private int itemDuration = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Item item = new Item();
            item.SetItem(itemType, itemEffect, itemDuration, other.gameObject);
            item.owner = other.gameObject;
            other.gameObject.GetComponent<PlayerInventory>().IteminInventory = item;
            Destroy(this.gameObject);
        }
    }
}
