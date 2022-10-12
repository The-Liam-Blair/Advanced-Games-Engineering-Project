using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectKeyPiece : MonoBehaviour
{
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<PlayerInventory>().keyPieceCount++;
            gameObject.SetActive(false);
        }
    }
}
