using System.Collections;
using UnityEngine;

public class RunwayObject : MonoBehaviour
{
    public Collider2D RunwayCollider;
    public bool stairs = false;

    private Coroutine dropCoroutine;
    private readonly Collider2D[] contactBuffer = new Collider2D[4];
    private bool playerInsideDetection;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerController>(out _))
        {
            playerInsideDetection = true;
            RunwayCollider.enabled = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.gameObject.TryGetComponent<PlayerController>(out _))
            return;

        playerInsideDetection = false;

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
        dropCoroutine = null;

        // 대기 중에도 플레이어가 감지 트리거 안에 그대로 있다면 켜지 않는다.
        // 트리거를 벗어날 때 OnTriggerExit2D가 다시 활성화해준다.
        if (!playerInsideDetection)
            RunwayCollider.enabled = true;
    }

    public void OnRunWayCollider()
    {
        // 감지 트리거에 아직 접촉 중이면(=Fall 상태의 착지 감지 등 다른 경로에서 호출되어도)
        // 발판을 켜지 않는다. OnTriggerStay2D가 매 프레임 꺼진 상태를 유지시켜야 한다.
        if (playerInsideDetection)
            return;

        if (dropCoroutine != null)
        {
            StopCoroutine(dropCoroutine);
            dropCoroutine = null;
        }

        RunwayCollider.enabled = true;
    }
}
