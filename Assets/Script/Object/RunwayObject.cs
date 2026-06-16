using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunwayObject : MonoBehaviour
{
    public Collider2D RunwayCollider;
    public bool stairs = false;

    private Coroutine dropCoroutine;
    private readonly Collider2D[] contactBuffer = new Collider2D[4];

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.gameObject.TryGetComponent<PlayerController>(out var character))
        {
            RunwayCollider.enabled = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.TryGetComponent<PlayerController>(out _))
            return;

        if (dropCoroutine != null)
        {
            StopCoroutine(dropCoroutine);
            dropCoroutine = null;
        }

        RunwayCollider.enabled = true;
    }

    private void FixedUpdate()
    {
        if (!stairs || RunwayCollider == null || !RunwayCollider.enabled || dropCoroutine != null)
            return;

        int count = RunwayCollider.GetContacts(contactBuffer);
        for (int i = 0; i < count; i++)
        {
            if (!contactBuffer[i].TryGetComponent<PlayerController>(out var player))
                continue;

            if (player.InputReader.InputData.moveAxis.y < 0)
            {
                dropCoroutine = StartCoroutine(DropThrough());
                break;
            }
        }
    }

    private IEnumerator DropThrough()
    {
        RunwayCollider.enabled = false;
        yield return new WaitForSeconds(0.5f);
        RunwayCollider.enabled = true;
        dropCoroutine = null;
    }

    public void OnRunWayCollider()
    {
        if (dropCoroutine != null)
        {
            StopCoroutine(dropCoroutine);
            dropCoroutine = null;
        }
        RunwayCollider.enabled = true;
    }
}
