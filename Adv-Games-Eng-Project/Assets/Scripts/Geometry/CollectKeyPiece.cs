using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectKeyPiece : MonoBehaviour
{
    // When the player touches the key piece, add that key piece to their inventory.
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<PlayerInventory>().keyPieceCount++;
            gameObject.SetActive(false);
        }
    }
}
