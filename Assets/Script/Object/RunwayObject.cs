using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunwayObject : MonoBehaviour
{
    public Collider2D RunwayCollider;

    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    if (collision.gameObject.TryGetComponent<PlayerController>(out var character))
    //    {
    //        RunwayCollider.enabled = true;
    //        Debug.Log("Runway Out off");
    //        //RunwayWallCollider.enabled = false;
    //    }
    //    Debug.Log("Runway Out");
    //}

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.gameObject.TryGetComponent<PlayerController>(out var character))
        {
            RunwayCollider.enabled = false;
        }
    }

    public void OnRunWayCollider()
    {
        RunwayCollider.enabled = true;
    }
}
