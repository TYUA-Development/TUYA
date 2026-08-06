using System.Collections.Generic;
using UnityEngine;

public enum WindDirection
{
    Right,
    UpRight,
    Up,
    UpLeft,
    Left,
    DownLeft,
    Down,
    DownRight
}

public class Object_Wind : MonoBehaviour, ICoreEvent
{
    public float windPower;

    [Tooltip("바람이 향하는 방향. windPower가 음수이면 이 방향의 반대로 힘이 작용합니다.")]
    public WindDirection windDirection = WindDirection.Right;

    public bool blockPlayer;

    [Header("Distance Falloff")]
    [Range(0f, 10f)]
    [Tooltip("이 오브젝트(transform.position)에서 멀어질수록 바람 세기가 감소하는 정도. 0 = 감소 없음(거리와 무관하게 동일한 힘), 10 = 아주 빠르게 감소")]
    public float distanceFalloff = 0f;

    [Header("Object Blocking")]
    [Tooltip("이 레이어의 콜라이더가 바람 오브젝트와 대상 사이를 가로막으면(Box 형태로 경로 체크) 그 대상은 바람 영향을 받지 않습니다. 예: Wall, Floor")]
    public LayerMask blockingLayer;

    [Tooltip("blockingLayer에 속해 있어도 차단 판정에서 제외할 오브젝트(들). 예: 레벨 기본 바닥처럼 항상 존재해서 바람과 대상 사이를 가로막지만 실제로는 바람을 막으면 안 되는 오브젝트. 해당 GameObject의 콜라이더만 예외 처리되며, 자식 오브젝트의 콜라이더는 포함되지 않습니다.")]
    public List<GameObject> blockingExceptions = new List<GameObject>();

    [Tooltip("이 레이어의 오브젝트는 바람 영역에 들어와도 영향을 받지 않습니다.")]
    public LayerMask ignoredLayer;

    [Header("Block Feel")]
    [Tooltip("blockPlayer로 막힌 플레이어를 트리거 밖으로 밀어내는 최대 속도(유닛/초). 겹친 깊이를 한 프레임에 순간 보정하지 않고 이 속도로 서서히 밀어내 순간이동처럼 보이지 않게 한다.")]
    public float blockPushSpeed = 15f;

    [Range(0f, 1f)]
    [Tooltip("막힐 때 진입 속도를 얼마나 반동으로 되돌릴지. 0 = 그 자리에서 멈춤, 1 = 들어온 속도 그대로 튕겨나감.")]
    public float blockBounciness = 0.3f;

    [Tooltip("차단 판정 최소 검사 깊이(유닛). 실제 검사 범위는 대상 위치부터 이 바람 콜라이더 자신의 경계까지 자동으로 늘어나므로(점프 등으로 차단벽에서 떨어져도 같은 바람 구역 안이면 계속 보호됨), 이 값은 그 여유분(그리고 콜라이더가 없을 때의 기본값) 역할만 합니다.")]
    public float blockingCheckDepth = 0.3f;

    [Header("Audio")]
    [Tooltip("이 바람이 켜져 있는 동안(콜라이더가 활성 상태인 동안) 재생할 바람 소리. AudioAssist의 Loop를 켜두면 계속 반복 재생됩니다.")]
    public AudioAssist loop_Wind;

    [Tooltip("바람이 꺼져 있다가 켜지는 순간(활성화 직후) 한 번 재생할 효과음.")]
    public AudioAssist start_Wind;

    [Tooltip("바람이 켜져 있다가 꺼지는 순간 한 번 재생할 효과음.")]
    public AudioAssist stop_Wind;

    [Tooltip("바람이 꺼질 때 loop_Wind를 즉시 정지하지 않고 이 시간(초) 동안 볼륨을 서서히 줄이며 정지시킵니다.")]
    public float loopWindFadeOutDuration = 0.5f;

    private Vector2 direction;
    private Vector2 power;

    private Dictionary<Collider2D, Rigidbody2D> colliderList = new Dictionary<Collider2D, Rigidbody2D> ();
    private readonly HashSet<Collider2D> fallThroughExempt = new HashSet<Collider2D>();

    private Collider2D selfCollider;
    private float? baseWindPower;

    private bool wantsWindAudio;
    private bool isWindAudioPlaying;

    // CoreObjectToggle이 여러 인스턴스로 나뉘어(예: 끄는 스위치/켜는 스위치) 같은 바람을 함께
    // 다룰 수 있어, "원래 windPower"를 외부(CoreObjectToggle)가 개별적으로 기억하면 서로 다른
    // 인스턴스가 이미 0으로 꺼진 값을 원본으로 잘못 캐싱할 수 있다. 그래서 이 컴포넌트 자신이
    // 원본값을 한 번만 캡처해 진실 소스로 둔다.
    //
    // Awake만으로는 부족하다 - GameObject가 처음부터 비활성화된 채로 배치된 바람은 Awake가
    // 아예 실행되지 않으므로, BaseWindPower가 처음 조회되는 시점(그때까지 아무도 windPower를
    // 건드리지 않았다면 여전히 원본값)에 지연 캡처하는 걸 폴백으로 둔다. 두 캡처 경로 모두
    // "아직 값이 없을 때만" 쓰도록 가드해야 한다 - 처음 꺼진 바람을 켤 때는 지연 캡처로 먼저
    // 원본값을 확보한 뒤 windPower를 0으로 만들고 나서 SetActive(true)를 호출하는데, 이
    // SetActive(true) 호출 안에서 Awake가 그제서야 동기적으로 처음 실행되기 때문에, Awake가
    // 무조건 덮어쓰면 방금 0으로 만든 값을 다시 캡처해 지연 캡처 결과를 망가뜨리게 된다.
    void Awake()
    {
        if (!baseWindPower.HasValue)
            baseWindPower = windPower;
    }

    public float BaseWindPower
    {
        get
        {
            if (!baseWindPower.HasValue)
                baseWindPower = windPower;

            return baseWindPower.Value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        selfCollider = GetComponent<Collider2D>();
        Init();

        UpdateWindAudio(selfCollider == null || selfCollider.enabled);
    }

    // GameObject가 다시 활성화될 때(예: CoreObjectToggle이 SetColliderEnabled(true)를
    // SetActive(true)보다 먼저 호출하는 순서라, 그 시점엔 아직 비활성 상태라 Play()를 걸 수
    // 없었던 경우) 마지막으로 원했던 재생 상태(wantsWindAudio)를 다시 반영한다.
    private void OnEnable()
    {
        if (wantsWindAudio)
            UpdateWindAudio(true);
    }

    // GameObject가 비활성화되면 AudioSource도 같이 멈추므로(Unity가 자동으로 처리),
    // 재생 중 플래그만 초기화해서 다음 OnEnable/SetColliderEnabled(true) 때 다시 재생되게 한다.
    private void OnDisable()
    {
        isWindAudioPlaying = false;
    }

    // shouldPlay=false면 즉시 정지, true면 재생을 "원한다"는 의도(wantsWindAudio)를 기록하고
    // GameObject가 활성 상태일 때만 실제로 Play()를 건다 - 비활성 오브젝트에서 AudioAssist.Play()를
    // 호출하면 Unity가 "Coroutines can't be started on inactive GameObjects" 에러를 낸다.
    // isWindAudioPlaying이 실제로 바뀌는 시점(꺼짐↔켜짐 전환)에만 start_Wind/stop_Wind
    // 원샷 효과음을 재생한다 - 매번 호출될 때마다 재생되는 게 아니라 딱 그 전환 순간에만.
    private void UpdateWindAudio(bool shouldPlay)
    {
        wantsWindAudio = shouldPlay;

        if (!shouldPlay)
        {
            if (isWindAudioPlaying)
            {
                if (loop_Wind != null)
                    loop_Wind.FadeOut(loopWindFadeOutDuration);

                isWindAudioPlaying = false;

                if (gameObject.activeInHierarchy && stop_Wind != null)
                    stop_Wind.Play();
            }

            return;
        }

        if (!gameObject.activeInHierarchy || isWindAudioPlaying)
            return;

        isWindAudioPlaying = true;

        if (loop_Wind != null)
            loop_Wind.Play();

        if (start_Wind != null)
            start_Wind.Play();
    }

    public void Init()
    {
        direction = GetDirectionVector(windDirection);
        power = direction * windPower;
    }

    // CoreObjectToggle이 바람을 끌 때, 오브젝트 전체를 SetActive(false)하지 않고도(파티클이
    // 함께 있으면 그러면 파티클 페이드가 끊기므로) 즉시 충돌/차단(BlockPlayer)만 멈추고 싶을 때
    // 쓴다. GameObject.SetActive와 달리 Collider2D.enabled는 독립적인 값이라 명시적으로
    // 켜고 꺼야 한다.
    public void SetColliderEnabled(bool value)
    {
        if (selfCollider == null)
            selfCollider = GetComponent<Collider2D>();

        if (selfCollider != null)
            selfCollider.enabled = value;

        UpdateWindAudio(value);
    }

    // CoreObjectToggle이 이 바람이 "지금 켜져 있는지"를 판단할 때 쓴다. 같은 바람 오브젝트를
    // 서로 다른 CoreObjectToggle 인스턴스(예: 끄는 스위치 하나, 켜는 스위치 하나)가 함께
    // 타겟팅할 수 있으므로, 별도 상태 변수를 각자 들고 있기보다 콜라이더의 실제 활성 여부를
    // 그때그때 조회하는 쪽이 항상 정확하다.
    public bool IsColliderEnabled
    {
        get
        {
            if (selfCollider == null)
                selfCollider = GetComponent<Collider2D>();

            return selfCollider != null && selfCollider.enabled;
        }
    }

    public static Vector2 GetDirectionVector(WindDirection dir)
    {
        switch (dir)
        {
            case WindDirection.Right: return Vector2.right;
            case WindDirection.UpRight: return new Vector2(1f, 1f).normalized;
            case WindDirection.Up: return Vector2.up;
            case WindDirection.UpLeft: return new Vector2(-1f, 1f).normalized;
            case WindDirection.Left: return Vector2.left;
            case WindDirection.DownLeft: return new Vector2(-1f, -1f).normalized;
            case WindDirection.Down: return Vector2.down;
            case WindDirection.DownRight: return new Vector2(1f, -1f).normalized;
            default: return Vector2.right;
        }
    }

    public void FixedUpdate()
    {
        foreach (var kvp in colliderList)
        {
            Rigidbody2D rb = kvp.Value;

            if (IsBlocked(kvp.Key))
                continue;

            float falloff = GetFalloffMultiplier(rb.position);
            rb.velocity += power * falloff * Time.deltaTime;
        }
    }

    private float GetFalloffMultiplier(Vector2 targetPosition)
    {
        if (distanceFalloff <= 0f)
            return 1f;

        float distance = Vector2.Distance(transform.position, targetPosition);
        return 1f / (1f + distanceFalloff * distance);
    }

    // 대상은 이미 바람 트리거 안에 들어와 있을 때만 이 메서드가 호출되므로(OnTrigger already
    // overlapping), 바람 오브젝트에서 대상까지의 장거리 라인오브사이트 대신 "대상 기준 바람이
    // 오는 반대 방향(-direction)에 차단벽/바닥이 있는가"만 확인한다. 대상의 절반 크기만큼
    // 거리를 트리밍하던 이전 방식은 대상이 정확히 그 위에 서 있는(틈이 0인) 차단벽까지 검사
    // 범위 밖으로 밀어내 버려서, 정작 "차단벽 위에 서 있어서 안전한" 가장 흔한 경우를
    // 놓치는 문제가 있었다.
    private bool IsBlocked(Collider2D targetCollider)
    {
        if (blockingLayer.value == 0 || targetCollider == null)
            return false;

        Vector2 checkDirection = -direction;
        if (checkDirection == Vector2.zero)
            return false;

        Bounds bounds = targetCollider.bounds;
        bool vertical = Mathf.Abs(checkDirection.y) >= Mathf.Abs(checkDirection.x);

        // 점프 등으로 대상이 차단벽에서 살짝 떨어져도(맞닿아 있지 않아도) 같은 바람 구역
        // 안에서 그 차단벽 뒤에 있다면 계속 보호되도록, 검사 깊이를 대상 위치부터 "이 바람
        // 콜라이더 자신의 경계"까지로 늘린다. 그 경계를 벗어나면 애초에 이 바람의 트리거
        // 밖이라 이 메서드 자체가 호출되지 않으므로, 더 늘릴 필요는 없다.
        float probeDepth = blockingCheckDepth;
        if (selfCollider != null)
        {
            Bounds windBounds = selfCollider.bounds;
            float playerNearEdge;
            float windFarEdge;

            if (vertical)
            {
                if (checkDirection.y < 0f) { playerNearEdge = bounds.min.y; windFarEdge = windBounds.min.y; }
                else { playerNearEdge = bounds.max.y; windFarEdge = windBounds.max.y; }
            }
            else
            {
                if (checkDirection.x < 0f) { playerNearEdge = bounds.min.x; windFarEdge = windBounds.min.x; }
                else { playerNearEdge = bounds.max.x; windFarEdge = windBounds.max.x; }
            }

            probeDepth = Mathf.Max(probeDepth, Mathf.Abs(windFarEdge - playerNearEdge) + blockingCheckDepth);
        }

        Vector2 probeSize = vertical
            ? new Vector2(bounds.size.x, probeDepth)
            : new Vector2(probeDepth, bounds.size.y);

        float edgeOffset = vertical ? bounds.extents.y : bounds.extents.x;
        Vector2 probeCenter = (Vector2)bounds.center + checkDirection.normalized * (edgeOffset + probeDepth * 0.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(probeCenter, probeSize, 0f, blockingLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (blockingExceptions != null && blockingExceptions.Contains(hit.gameObject))
                continue;

            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & ignoredLayer.value) != 0)
            return;

        if (collision.CompareTag("Player"))
        {
            // 위에서 낙하해 들어온 경우 힘도 적용하지 않고 막지도 않는다. 완전히 빠져나갈
            // 때까지(OnTriggerExit2D) 이 상태를 유지해서, 낙하 도중 다시 힘/차단이 끼어들지 않게 한다.
            // 단, Up 방향 바람은 위에서 떨어지는 플레이어를 받아 올리는 용도이므로 이 예외를 적용하지 않는다.
            if (windDirection != WindDirection.Up && IsFallingFromAbove(collision))
            {
                fallThroughExempt.Add(collision);
                return;
            }

            if (blockPlayer)
            {
                BlockPlayer(collision);
                return;
            }
        }

        if (collision.TryGetComponent(out Rigidbody2D rb))
        {
            colliderList[collision] = rb;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || fallThroughExempt.Contains(collision))
            return;

        if (blockPlayer)
            BlockPlayer(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        colliderList.Remove(collision);
        fallThroughExempt.Remove(collision);
    }

    private bool IsFallingFromAbove(Collider2D playerCollider)
    {
        if (playerCollider.attachedRigidbody == null)
            return false;

        if (selfCollider == null)
            return false;

        Vector2 previousPosition = playerCollider.attachedRigidbody.position
            - playerCollider.attachedRigidbody.velocity * Time.fixedDeltaTime;

        // 직전 프레임에 바람 콜라이더 윗면보다 위에 있었다면(=위에서 낙하해 들어온 것)
        return previousPosition.y >= selfCollider.bounds.max.y;
    }

    // 미는 방향은 속도가 아니라 두 콜라이더의 AABB 겹침 깊이로 정한다. 더 얕게 겹친 축이
    // 실제로 플레이어가 파고든 축이라고 보고 그 축의 반대 방향(플레이어가 있던 쪽)으로 민다.
    // 속도 기반 방식은 점프/낙하로 인한 수직 속도가 섞이면 엉뚱한 축으로 미는 문제가 있었다.
    private static Vector2 ComputeAxisPushDirection(Bounds playerBounds, Bounds windBounds)
    {
        float overlapX = Mathf.Min(playerBounds.max.x, windBounds.max.x) - Mathf.Max(playerBounds.min.x, windBounds.min.x);
        float overlapY = Mathf.Min(playerBounds.max.y, windBounds.max.y) - Mathf.Max(playerBounds.min.y, windBounds.min.y);

        if (overlapX <= 0f && overlapY <= 0f)
            return Vector2.zero;

        if (overlapX < overlapY)
            return playerBounds.center.x < windBounds.center.x ? Vector2.left : Vector2.right;

        return playerBounds.center.y < windBounds.center.y ? Vector2.down : Vector2.up;
    }

    // blockPlayer가 켜져 있고 blockingLayer로 가로막는 실제 벽이 없을 때,
    // 바람 콜라이더 자체를 벽처럼 취급해 플레이어가 겹친 만큼 트리거 경계 밖으로 밀어낸다.
    private void BlockPlayer(Collider2D playerCollider)
    {
        Rigidbody2D rb = playerCollider.attachedRigidbody;
        if (rb == null)
            return;

        if (IsBlocked(playerCollider))
            return;

        if (selfCollider == null)
            return;

        ColliderDistance2D dist = playerCollider.Distance(selfCollider);
        if (!dist.isOverlapped)
            return;

        Vector2 pushDirection = ComputeAxisPushDirection(playerCollider.bounds, selfCollider.bounds);
        if (pushDirection == Vector2.zero)
            return;

        // 겹친 깊이를 한 번에 전부 보정하면 순간이동처럼 보이므로, blockPushSpeed로
        // 제한된 거리만큼만 서서히 밀어낸다.
        float penetration = -dist.distance;
        float step = Mathf.Min(blockPushSpeed * Time.deltaTime, penetration);
        rb.position += pushDirection * step;

        // 진입 방향 속도를 그대로 0으로 죽이지 않고, blockBounciness만큼 반동으로 되돌려
        // 벽에 튕기는 듯한 자연스러운 느낌을 준다.
        float velocityAlongPush = Vector2.Dot(rb.velocity, pushDirection);
        if (velocityAlongPush < 0f)
        {
            float bouncedVelocity = -velocityAlongPush * blockBounciness;
            rb.velocity += pushDirection * (bouncedVelocity - velocityAlongPush);
        }
    }

    public void OnCoreEvent(bool isPressed = true)
    {

    }
}
