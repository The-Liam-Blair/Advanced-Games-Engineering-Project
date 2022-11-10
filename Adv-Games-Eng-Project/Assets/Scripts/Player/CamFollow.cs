using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds the player camera and instructs it to follow the player's position. While assigning it as a child to the player would also do this, it would also rotate
/// the camera with the player, which is not desired (And the control scheme assumes a constant camera angle).
/// </summary>
public class CamFollow : MonoBehaviour
{
    private GameObject player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void LateUpdate()
    {
        transform.position = new Vector4(player.transform.position.x, transform.position.y, player.transform.position.z);
    }
}
