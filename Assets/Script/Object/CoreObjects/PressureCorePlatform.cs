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

    [Tooltip("Top에서 순간적으로 떨어졌다가 이 시간 안에 다시 닿으면 무게 해제를 취소한다. 플랫폼이 이동하는 동안 위에 얹힌 오브젝트가 잠깐 튕겨서 떨어지는 경우를 걸러내기 위함.")]
    public float releaseGraceDuration = 0.1f;

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

    [Header("Audio")]
    [Tooltip("Top이 눌려서 내려가는 동안 재생할 소리. AudioAssist의 Loop를 켜두면 계속 반복 재생됩니다.")]
    public AudioAssist loop_Down;

    [Tooltip("Top이 올라오는 동안 재생할 소리. AudioAssist의 Loop를 켜두면 계속 반복 재생됩니다.")]
    public AudioAssist loop_Up;

    [Tooltip("Top이 내려가다가 bottomStopper에 닿아서 멈추는 순간 한 번 재생할 효과음.")]
    public AudioAssist stop_Bottom;

    private const float StopEpsilon = 0.001f;

    private PlatformState currentState;
    private Coroutine moveCoroutine;

    private readonly Dictionary<Collider2D, float> pressingWeights = new Dictionary<Collider2D, float>();
    private readonly Dictionary<Collider2D, Coroutine> pendingReleases = new Dictionary<Collider2D, Coroutine>();
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
        BeginRelease(collision.collider);
    }

    private void EvaluateCollision(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & weighableLayer.value) == 0)
        {
            ReleaseCollider(collision.collider);
            return;
        }

        // 이미 무게로 등록된 콜라이더는 Stay 중 접촉면 법선을 다시 검사하지 않는다.
        // Top 자신이 이동하는 동안에는 위에 얹힌 오브젝트의 접촉 법선이 순간적으로
        // 흔들릴 수 있는데, 그때마다 여기서 등록을 뺐다 다시 넣었다 하면 반대편과의
        // 무게 비교(EvaluatePair)가 매 프레임 뒤집혀서 Top이 목표까지 못 가고 제자리에서
        // 떨리게 된다. 실제 이탈 판정은 OnCollisionExit2D(HandleTopCollisionExit)가 전담한다.
        if (pressingWeights.ContainsKey(collision.collider))
        {
            // 해제 유예 중이었다면(잠깐 떨어졌다가 다시 닿음) 해제를 취소한다.
            // 무게를 뺐다 다시 더하는 게 아니라 애초에 안 뺀 것으로 처리되므로
            // EvaluatePair()가 다시 불리지 않고, 방향도 뒤집히지 않는다.
            CancelPendingRelease(collision.collider);
            return;
        }

        collision.GetContacts(contactBuffer);

        // Box2D가 드물게 'contact != NULL' 어서션과 함께 접촉점을 하나도 못 돌려줄 때가 있다.
        // 최초 등록 판정을 이번 프레임만으로 포기하지 않고, 다음 Stay에서 다시 시도한다.
        if (contactBuffer.Count == 0)
            return;

        if (!IsPressedFromAbove())
            return;

        TY_Weight weightComponent = collision.collider.GetComponentInParent<TY_Weight>();
        if (weightComponent == null)
            return;

        pressingWeights[collision.collider] = weightComponent.weight;
        RecalculateWeight();
    }

    private void ReleaseCollider(Collider2D collider)
    {
        if (pressingWeights.Remove(collider))
            RecalculateWeight();
    }

    // 즉시 해제하지 않고 releaseGraceDuration만큼 기다린 뒤에도 여전히 안 닿아 있으면
    // 그때 진짜로 해제한다. 그 사이에 EvaluateCollision에서 같은 콜라이더가 다시 들어오면
    // CancelPendingRelease가 이 코루틴을 멈춰서 무게가 계속 유지된다.
    private void BeginRelease(Collider2D collider)
    {
        if (!pressingWeights.ContainsKey(collider))
            return;

        CancelPendingRelease(collider);
        pendingReleases[collider] = StartCoroutine(ReleaseAfterGrace(collider));
    }

    private IEnumerator ReleaseAfterGrace(Collider2D collider)
    {
        yield return new WaitForSeconds(releaseGraceDuration);
        pendingReleases.Remove(collider);
        ReleaseCollider(collider);
    }

    private void CancelPendingRelease(Collider2D collider)
    {
        if (pendingReleases.TryGetValue(collider, out Coroutine routine))
        {
            StopCoroutine(routine);
            pendingReleases.Remove(collider);
        }
    }

    // 호출 전에 contactBuffer가 이미 collision.GetContacts()로 채워져 있어야 한다.
    private bool IsPressedFromAbove()
    {
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

        // 아래 두 코루틴은 정상적으로 목표에 도달했을 때 스스로 이동 루프 소리를 멈추지만,
        // 짝의 무게가 다시 바뀌어 방향이 뒤집히는 등 목표 도달 전에 코루틴이 강제로 끊기는
        // 경우엔 그 정지 코드가 실행되지 않으므로 상태 전환 시점에 한 번 더 확실히 멈춘다.
        if (loop_Down != null)
            loop_Down.Stop();

        if (loop_Up != null)
            loop_Up.Stop();

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
        ColliderDistance2D distance = topCollider.Distance(bottomStopper);

        // 이미 bottomStopper에 닿아 있어서 실제로 이동할 게 없다면 루프/정지 효과음을 재생하지
        // 않는다 - 진짜로 눌려 내려가는 움직임이 있었을 때만 "내려감"/"바닥에 닿음"으로 취급한다.
        bool wasMoving = distance.distance > StopEpsilon;

        if (wasMoving && loop_Down != null)
            loop_Down.Play();

        while (distance.distance > StopEpsilon)
        {
            // FixedUpdate 주기(WaitForFixedUpdate)에 맞춰 한 스텝씩 옮긴다. Update(yield return null)
            // 주기로 옮기면 렌더 프레임이 물리 스텝보다 잦은 만큼 Top의 Kinematic 콜라이더가
            // 물리 스텝 사이사이 여러 번 앞서 이동해버려서, 다음 물리 스텝에서 위에 얹힌 박스를
            // 한 번에 크게 밀어넣는 셈이 되어 박스가 튕기고(붙었다 떨어졌다) 무게 판정이
            // 매 프레임 뒤집혀 떨리는 원인이 된다.
            float step = Mathf.Min(speed * Time.fixedDeltaTime, distance.distance);
            topTransform.position += Vector3.down * step;
            yield return new WaitForFixedUpdate();

            distance = topCollider.Distance(bottomStopper);
        }

        if (wasMoving)
        {
            if (loop_Down != null)
                loop_Down.Stop();

            if (stop_Bottom != null)
                stop_Bottom.Play();
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

        bool wasMoving = Vector3.Distance(topTransform.position, targetWorldPosition) > StopEpsilon;

        if (wasMoving && loop_Up != null)
            loop_Up.Play();

        while (Vector3.Distance(topTransform.position, targetWorldPosition) > StopEpsilon)
        {
            // MoveDownRoutine과 같은 이유로 물리 스텝에 맞춰 이동한다.
            topTransform.position = Vector3.MoveTowards(topTransform.position, targetWorldPosition, speed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        topTransform.position = targetWorldPosition;

        if (wasMoving && loop_Up != null)
            loop_Up.Stop();

        moveCoroutine = null;
    }
}
