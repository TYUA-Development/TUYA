using UnityEngine;

public class RunwayObject : MonoBehaviour
{
    public Collider2D RunwayCollider;

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
        RunwayCollider.enabled = true;
    }

    public void OnRunWayCollider()
    {
        // 감지 트리거에 아직 접촉 중이면(=Fall 상태의 착지 감지 등 다른 경로에서 호출되어도)
        // 발판을 켜지 않는다. OnTriggerStay2D가 매 프레임 꺼진 상태를 유지시켜야 한다.
        if (playerInsideDetection)
            return;

        RunwayCollider.enabled = true;
    }
}
