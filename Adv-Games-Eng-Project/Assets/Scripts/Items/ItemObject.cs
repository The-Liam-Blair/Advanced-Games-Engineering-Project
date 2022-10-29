using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class ItemObject : MonoBehaviour
{
    // Stores it's stats so it can access the debuff type and it's duration.
    private Item itemStats;
    
    // Prevents multiple collision runs from one instance of a collision.
    private bool hasCollided = true;

    // Called when an item is picked up by either the player or the enemy.
    public void OnSpawn(Item stats)
    {
        itemStats = stats;
        hasCollided = false;
    }

    protected void OnCollisionEnter(Collision other)
    {
        // Weird bug where this collision function is called multiple times from 1 collision (Despite the object being disabled after the first run of this function).
        // Null item stats + 2nd col call = error. Bool collision check prevents this.
        if (hasCollided) { return; }

        hasCollided = true;
        
        // Item hit/touched an object that isn't the owner, so apply the effect to it!
        if (!itemStats.owner.tag.Equals(other.gameObject.tag))
        {
            switch (itemStats.GetEffect())
            {
                // Victim cannot move at all for \duration/ seconds.
                case "STUN":
                    if (other.gameObject.name == "Player") { other.gameObject.GetComponent<PlayerControl>().Stun(itemStats.duration); }
                    else if(other.gameObject.name == "Enemy"){ other.gameObject.GetComponent<GoapAgent>().Stun(itemStats.duration); }
                    
                    break;

                // Victim's movement speed is reduced by 66% for \duration/ seconds.
                case "SLOW":
                     if (other.gameObject.name == "Player") { other.gameObject.GetComponent<PlayerControl>().Slow(itemStats.duration); }
                     else if (other.gameObject.name == "Enemy") { other.gameObject.GetComponent<GoapAgent>().Slow(itemStats.duration); }
                    
                    break;
 
                // (AI) enemy's vision is removed for \duration/ seconds.
                // (Player) Player's vision is heavily darkened for \duration/ seconds.
                case "BLIND": // todo
                    break;
            }

            // Deactivate item as it's now been used up.
            itemStats.owner = null;
        }
        gameObject.SetActive(false);
    }
}
