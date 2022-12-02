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

    // Get's item stats and duration to be used in the UI output.
    public string ItemOutput()
    {
        string output = "";
        if (IteminInventory == null)
        {
            output = "NONE";
            return output; // Prevents null reference exception errors.
        }
        else if (IteminInventory.GetEffect() == "NONE")
        {
            output += "WALL";
        }
        else
        {
            output += IteminInventory.GetEffect();
        }

        output += ", " + IteminInventory.duration + " SECONDS";

        return output;
    }
}
