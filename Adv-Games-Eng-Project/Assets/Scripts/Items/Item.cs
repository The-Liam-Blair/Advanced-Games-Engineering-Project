using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Implementation of items, primarily item statistics such as effects.
/// </summary>
public class Item
{

    // Item usage.
    protected enum ItemType
    {
        THROWABLE,
        WALL
    }

    protected ItemType type;

    // Type and effect getter/setter.
    public new string GetType()
    {
        return type.ToString();
    }

    public string GetEffect()
    {
        return effect.ToString();
    }

    // What does the item do to it's victim?
    protected enum ItemEffect
    {
        STUN,
        SLOW,
        BLIND,
        NONE
    }
    protected ItemEffect effect;

    // How long does the item de-buff the victim?
    public int duration
    {
        protected set;
        get;
    }

    // Who is holding this item (Prevents the user from hurting themselves with their own items).
    public GameObject owner
    {
        get;
        set;
    }

    /// <summary>
    /// Called when an item is picked up, essentially a constructor for the item and it's stats.
    /// </summary>
    /// <param name="type">Item type.</param>
    /// <param name="effect">Item effect.</param>
    /// <param name="duration">Item duration.</param>
    /// <param name="owner">Item owner.</param>
    public void SetItem(string type, string effect, int duration, GameObject owner)
    {

        // Set the duration of the item's effect.
        this.duration = duration;


        // Set the item's effect.
        switch (effect)
        {
            case "STUN":
                this.effect = ItemEffect.STUN;
                break;
            case "SLOW":
                this.effect = ItemEffect.SLOW;
                break;
            case "BLIND":
                this.effect = ItemEffect.BLIND;
                break;
        }

        // Set the item type.
        switch (type)
        {
            case "THROWABLE":
                this.type = ItemType.THROWABLE;
                break;
            case "WALL":
                this.type = ItemType.WALL;
                duration *= 2;
                this.effect = ItemEffect.NONE;
                break;
        }
    }
}
