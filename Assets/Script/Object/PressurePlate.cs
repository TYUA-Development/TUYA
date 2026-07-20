using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("이 레이어의 오브젝트만 발판을 누를 수 있습니다 (예: Player, Object)")]
    public LayerMask pressLayer;

    [Tooltip("위에서 눌렀다고 인정할 접촉면 법선의 최소 Y값. 1에 가까울수록 거의 수직으로 위에서 눌러야 인정됩니다.")]
    [Range(0f, 1f)]
    public float minUpwardNormal = 0.5f;

    [Header("Target")]
    [Tooltip("발판이 눌렸을 때 OnCoreEvent()를 호출할 대상들 (ICoreEvent를 구현한 컴포넌트가 있는 오브젝트)")]
    public List<GameObject> targetObjects = new List<GameObject>();

    [Header("Trigger Settings")]
    public bool activateOnlyOnce = false;

    [Tooltip("true면 눌렀던 오브젝트가 모두 떨어지는 순간 대상들에게 OnCoreEvent(false)를 보냅니다.")]
    public bool isCancel = false;

    private readonly HashSet<Collider2D> pressingColliders = new HashSet<Collider2D>();
    private readonly List<ContactPoint2D> contactBuffer = new List<ContactPoint2D>();
    private bool hasActivatedOnce;
    private bool isActivated;

    private void OnCollisionEnter2D(Collision2D collision) => EvaluateCollision(collision);
    private void OnCollisionStay2D(Collision2D collision) => EvaluateCollision(collision);

    private void OnCollisionExit2D(Collision2D collision)
    {
        ReleaseCollider(collision.collider);
    }

    private void EvaluateCollision(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & pressLayer.value) == 0 ||
            !IsPressedFromAbove(collision))
        {
            ReleaseCollider(collision.collider);
            return;
        }

        bool wasEmpty = pressingColliders.Count == 0;
        pressingColliders.Add(collision.collider);

        if (wasEmpty)
            Activate();
    }

    private void ReleaseCollider(Collider2D collider)
    {
        if (!pressingColliders.Remove(collider))
            return;

        if (isCancel && pressingColliders.Count == 0)
            Deactivate();
    }

    private bool IsPressedFromAbove(Collision2D collision)
    {
        collision.GetContacts(contactBuffer);

        for (int i = 0; i < contactBuffer.Count; i++)
        {
            if (contactBuffer[i].normal.y >= minUpwardNormal)
                return true;
        }

        return false;
    }

    private void Activate()
    {
        if (activateOnlyOnce && hasActivatedOnce)
            return;

        hasActivatedOnce = true;
        isActivated = true;

        SendCoreEvent(true);
    }

    private void Deactivate()
    {
        if (!isActivated)
            return;

        isActivated = false;

        SendCoreEvent(false);
    }

    private void SendCoreEvent(bool isPressed)
    {
        for (int i = 0; i < targetObjects.Count; i++)
        {
            if (targetObjects[i] != null && targetObjects[i].TryGetComponent<ICoreEvent>(out var coreEvent))
                coreEvent.OnCoreEvent(isPressed);
        }
    }
}
