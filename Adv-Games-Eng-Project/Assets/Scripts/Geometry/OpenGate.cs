using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenGate : MonoBehaviour
{
    private bool open;

    private PlayerInventory p_inventory;

    void Start()
    {
        p_inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>();
    }

    // If the player touches the gate and has all the key pieces, open the gate, winning the game.
    void Update()
    {
        if ( !open && p_inventory.keyPieceCount == 6)
        {
            gameObject.transform.Rotate(0, 60, 0);
            gameObject.transform.position = new Vector3(-9f, 1f, -4.5f);
            open = true;
        }
    }
}