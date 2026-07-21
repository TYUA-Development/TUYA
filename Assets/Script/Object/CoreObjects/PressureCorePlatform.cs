using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressureCorePlatform : MonoBehaviour
{
    private enum PlatformState { Up, Down }

    [Header("Connection")]
    [Tooltip("무게를 서로 비교할 반대편 플랫폼")]
    public PressureCorePlatform connectedPlatform;

    [Header("Structure")]
    [Tooltip("실제로 무게를 받는 Top 자식 오브젝트의 콜라이더. 이 스크립트는 부모(Rigidbody2D가 있는)에 붙이고, 이 필드에 Top의 Collider2D를 연결합니다.")]
    public Collider2D topCollider;

    [Header("Detection")]
    [Tooltip("이 레이어의 오브젝트만 무게로 인정합니다")]
    public LayerMask weighableLayer = ~0;

    [Tooltip("위에서 눌렀다고 인정할 접촉면 법선의 최소 Y값")]
    [Range(0f, 1f)]
    public float minUpwardNormal = 0.5f;

    [Header("Movement")]
    [Tooltip("Up 상태일 때 Top 오브젝트가 도달할 로컬 좌표 (Top의 부모 기준)")]
    public Vector3 upLocalPosition;

    [Tooltip("Down 상태일 때 Top 오브젝트가 닿으면 하강을 멈출 대상 콜라이더 (예: PlatForm_Bottom, 움직이지 않는 고정 오브젝트여야 함). 하강은 월드 -Y 방향으로 진행되며, 움직이는 것은 부모가 아니라 topCollider 자신입니다.")]
    public Collider2D bottomStopper;

    [Tooltip("이 플랫폼이 내려가는 속도. 무게 비교로 짝이 이동할 때는 항상 무거운(내려가는) 쪽의 moveSpeed가 양쪽에 동일하게 적용되어, 올라가는 속도와 내려가는 속도가 항상 일치합니다.")]
    public float moveSpeed = 1f;

    [Header("Initial State")]
    [Tooltip("씬 배치 상 이 플랫폼이 시작부터 Down 위치(바닥에 닿아 있음)인지 여부. 무게가 같아 판정이 없을 때(히스테리시스) 기준이 됩니다.")]
    public bool startsDown = true;

    private const float StopEpsilon = 0.001f;

    private PlatformState currentState;
    private Coroutine moveCoroutine;

    private readonly Dictionary<Collider2D, float> pressingWeights = new Dictionary<Collider2D, float>();
    private readonly List<ContactPoint2D> contactBuffer = new List<ContactPoint2D>();

    private float currentWeight;
    public float CurrentWeight => currentWeight;

    private void Awake()
    {
        currentState = startsDown ? PlatformState.Down : PlatformState.Up;
    }

    // Top 콜라이더는 자식 오브젝트에 있어 이 부모는 OnCollision2D 메시지를 직접 받지 못한다.
    // PressureTopRelay가 Top 오브젝트에서 이 메서드들을 대신 호출해준다.
    public void HandleTopCollisionEnter(Collision2D collision) => EvaluateCollision(collision);
    public void HandleTopCollisionStay(Collision2D collision) => EvaluateCollision(collision);

    public void HandleTopCollisionExit(Collision2D collision)
    {
        ReleaseCollider(collision.collider);
    }

    private void EvaluateCollision(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & weighableLayer.value) == 0 ||
            !IsPressedFromAbove(collision))
        {
            ReleaseCollider(collision.collider);
            return;
        }

        TY_Weight weightComponent = collision.collider.GetComponentInParent<TY_Weight>();
        if (weightComponent == null)
            return;

        if (pressingWeights.TryGetValue(collision.collider, out float existing) && Mathf.Approximately(existing, weightComponent.weight))
            return;

        pressingWeights[collision.collider] = weightComponent.weight;
        RecalculateWeight();
    }

    private void ReleaseCollider(Collider2D collider)
    {
        if (pressingWeights.Remove(collider))
            RecalculateWeight();
    }

    private bool IsPressedFromAbove(Collision2D collision)
    {
        collision.GetContacts(contactBuffer);

        // Top 콜라이더가 부모 Rigidbody2D의 compound collider인 구조에서는
        // 접촉면 법선의 부호가 단일 오브젝트 충돌(PressurePlate 등)과 반대로 나온다.
        // 부호에 의존하지 않고 수직 방향인지만 절댓값으로 판정한다.
        for (int i = 0; i < contactBuffer.Count; i++)
        {
            if (Mathf.Abs(contactBuffer[i].normal.y) >= minUpwardNormal)
                return true;
        }

        return false;
    }

    private void RecalculateWeight()
    {
        float total = 0f;
        foreach (float value in pressingWeights.Values)
            total += value;

        currentWeight = total;
        EvaluatePair();
    }

    private void EvaluatePair()
    {
        if (connectedPlatform == null)
            return;

        // 무게가 같으면 직전 상태를 그대로 유지한다 (히스테리시스).
        if (Mathf.Approximately(currentWeight, connectedPlatform.currentWeight))
            return;

        PressureCorePlatform heavier = currentWeight > connectedPlatform.currentWeight ? this : connectedPlatform;
        PressureCorePlatform lighter = heavier == this ? connectedPlatform : this;

        // 내려가는(무거운) 쪽의 속도를 양쪽에 동일하게 적용해 상승/하강 속도를 일치시킨다.
        float speed = heavier.moveSpeed;

        heavier.SetState(PlatformState.Down, speed);
        lighter.SetState(PlatformState.Up, speed);
    }

    private void SetState(PlatformState state, float speed)
    {
        if (currentState == state)
            return;

        currentState = state;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(state == PlatformState.Down ? MoveDownRoutine(speed) : MoveUpRoutine(speed));
    }

    private IEnumerator MoveDownRoutine(float speed)
    {
        if (bottomStopper == null || topCollider == null)
        {
            moveCoroutine = null;
            yield break;
        }

        Transform topTransform = topCollider.transform;

        while (true)
        {
            ColliderDistance2D distance = topCollider.Distance(bottomStopper);

            if (distance.distance <= StopEpsilon)
                break;

            float step = Mathf.Min(speed * Time.deltaTime, distance.distance);
            topTransform.position += Vector3.down * step;
            yield return null;
        }

        moveCoroutine = null;
    }

    private IEnumerator MoveUpRoutine(float speed)
    {
        if (topCollider == null)
        {
            moveCoroutine = null;
            yield break;
        }

        Transform topTransform = topCollider.transform;

        // upLocalPosition은 Inspector 입력 편의를 위한 로컬 좌표이지만, 이동 자체는
        // Down과 동일하게 월드 공간에서 진행한다. 부모(PressurePlatform)의 localScale이
        // 1이 아니면(예: 3배) localPosition 기준 이동이 같은 speed 값으로도 실제 월드
        // 이동 거리가 배로 커져 Down/Up 속도가 어긋나기 때문이다.
        Vector3 targetWorldPosition = topTransform.parent != null
            ? topTransform.parent.TransformPoint(upLocalPosition)
            : upLocalPosition;

        while (Vector3.Distance(topTransform.position, targetWorldPosition) > StopEpsilon)
        {
            topTransform.position = Vector3.MoveTowards(topTransform.position, targetWorldPosition, speed * Time.deltaTime);
            yield return null;
        }

        topTransform.position = targetWorldPosition;
        moveCoroutine = null;
    }
}
