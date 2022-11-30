using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player inventory, which extends the normal inventory class. Additionally also stores key pieces for the player.
/// </summary>
public class PlayerInventory : Inventory
{
    public int keyPieceCount
    {
        get;
        set;
    }

    private void Awake()
    {
        keyPieceCount = 0;
    }

    public string ItemEffectToString()
    {
        if (IteminInventory == null)
        {
            return "NULL";
        }
        else if (IteminInventory.GetEffect() == "NONE")
        {
            return "WALL";
        }
        else
        {
            return IteminInventory.GetEffect();
        }
    }
}
