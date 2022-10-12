using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenGate : MonoBehaviour
{
    private bool open;
    void OnCollisionEnter(Collision other)
    {
        if ( !open && other.gameObject.tag == "Player" && other.gameObject.GetComponent<PlayerInventory>().keyPieceCount == 3)
        {
            gameObject.transform.Rotate(0, 60, 0);
            gameObject.transform.position = new Vector3(-9f, 1f, -4.5f);
            open = true;
        }
    }
}