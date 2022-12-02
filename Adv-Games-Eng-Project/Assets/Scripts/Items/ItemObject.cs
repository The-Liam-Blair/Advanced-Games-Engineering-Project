using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

/// <summary>
/// The item in action. This implementation is the used item, so handles collision detection and inflicting effects on victims.
/// </summary>
public class ItemObject : MonoBehaviour
{
    // Stores it's stats so it can access the debuff type and it's duration.
    private Item itemStats;
    
    // Prevents multiple collision runs from one instance of a collision.
    private bool hasCollided = true;

    /// <summary>
    /// Called when an item is just used, the item's stats are set. Unique implementation for walls as they don't have effects.
    /// </summary>
    /// <param name="stats">The stats to assign the item object.</param>
    public void OnSpawn(Item stats)
    {
        itemStats = stats;
        hasCollided = false;

        if (itemStats.GetType() == "WALL")
        {
            itemStats.owner = null;
            GetComponent<NavMeshObstacle>().enabled = true;

            StartCoroutine(WallLifeTimeCoroutine(itemStats.duration));
        }
    }

    protected void OnCollisionEnter(Collision other)
    {
        // Glitch where this collision function is called multiple times from 1 collision (Despite the object being disabled after the first run of this function).
        // Null item stats + 2nd col call = error. Bool collision check prevents this.
        // Also don't run if the item is a wall, since it's purpose is only to block pathways.
        if (hasCollided || itemStats.GetType() == "WALL") { return; }

        hasCollided = true;
        
        // Item hit/touched an object that isn't the owner, so apply the effect to it!
        if (!itemStats.owner.tag.Equals(other.gameObject.tag))
        {
            switch (itemStats.GetEffect())
            {
                // Victim cannot move at all for \duration/ seconds.
                case "STUN":
                    if (other.gameObject.tag == "Player") { other.gameObject.GetComponent<PlayerControl>().Stun(itemStats.duration); }
                    else if(other.gameObject.tag == "Enemy"){ other.gameObject.GetComponent<GoapAgent>().Stun(itemStats.duration); }
                    
                    break;

                // Victim's movement speed is reduced by 66% for \duration/ seconds.
                case "SLOW":
                     if (other.gameObject.tag == "Player") { other.gameObject.GetComponent<PlayerControl>().Slow(itemStats.duration); }
                     else if (other.gameObject.tag == "Enemy") { other.gameObject.GetComponent<GoapAgent>().Slow(itemStats.duration); }

                     break;
                
                // (AI) enemy's vision is removed for \duration/ seconds.
                // (Player) Player's vision is heavily darkened for \duration/ seconds.
                case "BLIND":
                    if (other.gameObject.tag == "Player") { other.gameObject.GetComponent<PlayerControl>().Blind(itemStats.duration); }
                    else if (other.gameObject.tag == "Enemy") { other.gameObject.GetComponent<GoapAgent>().Blind(itemStats.duration); }
                    
                    break;
            }

            if (other.gameObject.tag == "Enemy")
            {
                other.gameObject.GetComponent<UseItem>().IncreaseKnowledge(100);
                other.gameObject.GetComponent<DodgeProjectile>().IncreaseKnowledge(100);
            }

            // Deactivate item as it's now been used up.
            itemStats.owner = null;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Lifetime of a placed wall before it expires.
    /// </summary>
    /// <param name="duration">Duration before expiring.</param>
    IEnumerator WallLifeTimeCoroutine(int duration)
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
        GetComponent<NavMeshObstacle>().enabled = false;
        yield return null;
    }
}
