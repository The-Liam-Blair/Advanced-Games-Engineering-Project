using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// todo: Make inventory abstract for enemy + player inventories
public class PlayerInventory : MonoBehaviour
{

    [SerializeField] private GameObject itemObject;

    public int keyPieceCount
    {
        get;
        set;
    }

    public Item IteminInventory
    {
        get;
        set;
    }

    private void Awake()
    {
        keyPieceCount = 0;
        IteminInventory = null;
    }


    public void UseItem()
    {
        if (IteminInventory != null)
        {
            GameObject item = Instantiate(itemObject,
                transform.position + transform.forward,
               Quaternion.identity);

            switch (IteminInventory.GetType())
            {
                //todo: implement stunnable throwables (Does not stun currently).
                case "THROWABLE":
                    item.GetComponent<Rigidbody>().AddForce(transform.forward * 15, ForceMode.Impulse);
                    break;

                case "PLACEABLE":
                    break;
            }
            IteminInventory = null;
        }
    }
}
