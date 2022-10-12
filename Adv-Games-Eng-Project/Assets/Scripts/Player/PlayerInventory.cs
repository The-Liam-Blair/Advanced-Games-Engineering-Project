using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{

    public int keyPieceCount
    {
        get;
        set;
    }

    void Awake()
    {
        keyPieceCount = 0;
    }
}
