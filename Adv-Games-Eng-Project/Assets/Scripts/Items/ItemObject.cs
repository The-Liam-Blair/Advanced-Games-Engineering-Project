using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class ItemObject : MonoBehaviour
{
    private Item itemStats;

    private void Awake()
    {
        itemStats = GameObject.Find("Player").GetComponent<PlayerInventory>().IteminInventory;
    }

    protected void OnCollisionEnter(Collision other)
    {
        // Item hit/touched an object that isn't the owner, so apply the effect to it!
        if (!itemStats.owner.tag.Equals(other.gameObject.tag))
        {
            switch (itemStats.GetEffect())
            {
                // Victim cannot move at all for /duration\ seconds.
                case "STUN":
                    break;

                // Victim's movement speed is reduced by 66% for \duration/ seconds.
                case "SLOW":
                    break;

                // (If AI) enemy's vision is removed for \duration/ seconds.
                // (If Player) Player's vision is heavily darkened for \duration/ seconds.
                case "BLIND":
                    break;
            }

            // Deactivate item as it's now been used up.
            itemStats.owner = null;
        }
        Destroy(this.gameObject);
    }
}
