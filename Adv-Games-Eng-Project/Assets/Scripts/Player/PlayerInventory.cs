using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// todo: Make inventory abstract for enemy + player inventories
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
}
