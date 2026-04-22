using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunwayObject : MonoBehaviour
{
    public Collider2D RunwayCollider;
    public Collider2D RunwayWallCollider;
    // Start is called before the first frame update

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.TryGetComponent<PlayerController>(out var character))
        {
            if(character.currentState is PlayerJumpState)
            {
                RunwayCollider.enabled = true;
                RunwayWallCollider.enabled = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerController>(out var character))
        {
            RunwayCollider.enabled = false;
            RunwayWallCollider.enabled = false;
        }
    }
}
