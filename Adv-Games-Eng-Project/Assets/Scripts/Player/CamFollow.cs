using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    private GameObject player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Attaching camera as a child to the player will make the camera rotate with it (and the control scheme assumes a constant camera rotation position), so
    // this script handles camera movement to follow the player but not rotate.
    void LateUpdate()
    {
        transform.position = new Vector4(player.transform.position.x, transform.position.y, player.transform.position.z);
    }
}
