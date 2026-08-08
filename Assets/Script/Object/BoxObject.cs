using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxObject : MonoBehaviour, IArrowHit, IArrowKnockbackReceiver
{
    [Header("Knockback")]
    [Tooltip("화살에 맞았을 때 화살이 날아간 방향으로 가해지는 힘의 크기")]
    public float knockbackForce = 10f;

    [Header("Audio")]
    [Tooltip("아래로 떨어지는 동안(Rigidbody2D.velocity.y < 0) 반복 재생할 낙하 소리. AudioAssist의 Loop를 켜두어야 합니다.")]
    public AudioAssist fall_Box;

    [Tooltip("바닥/발판 등 다른 콜라이더와 부딪혔을 때 한 번 재생할 충돌 소리.")]
    public AudioAssist hit_Box;

    [Tooltip("정지했다고 판단할 Rigidbody2D.velocity 크기 임계값. 착지하며 짧게 여러 번 튕기면 OnCollisionEnter2D가 연달아 발동해 AudioAssist.Play()가 매번 직전 재생을 끊고 다시 시작하는데, 그러면 정작 처음 부딪힌 소리는 묻히고 거의 멈출 때의 마지막 재접촉 소리만 온전히 들리게 된다. 첫 충돌 이후 이 속도 이하로(=완전히 정지할 때까지) 떨어지기 전까지는 재트리거를 무시해서 그 문제를 막는다.")]
    public float hitSoundStopVelocity = 0.05f;

    [Tooltip("이 박스가 소멸될 때(예: RopeRegenerator가 이전에 떨어진 박스를 제거할 때) 한 번 재생할 효과음. 재생만으로는 자동 트리거되지 않고, 외부(RopeRegenerator 등)에서 PlayDisappearSoundAndDestroy()를 호출해야 합니다.")]
    public AudioAssist disappear_Box;

    [Tooltip("disappear_Box 재생 후 실제로 오브젝트를 파괴하기까지 대기하는 시간(초). Destroy가 AudioSource도 같이 없애버리므로, 소리가 끝까지 들리려면 클립 길이 이상으로 설정하세요.")]
    public float disappearDestroyDelay = 1f;

    [Header("Landing")]
    [Tooltip("착지 시 위에서 떨어져 닿았다고 인정할 접촉면 법선의 최소 Y값. 물리 반발력(bounciness)이 0이어도, 높은 곳에서 빠르게 떨어진 무거운 박스는 Box2D가 한 스텝 만에 관통을 완전히 보정하지 못해 한 번 튀어 보일 수 있다 - 착지 순간 남은 수직 속도를 직접 0으로 정리해서 이 튐을 없앤다.")]
    [Range(0f, 1f)]
    public float landingNormalThreshold = 0.5f;

    [Tooltip("접촉이 순간적으로 끊겼다가 이 시간 안에 같은 콜라이더와 다시 닿으면 접촉이 계속 이어진 것으로 처리한다(진짜로 끊긴 게 아니라고 봄). 예를 들어 PressureCorePlatform의 Top이 Bottom에 부딪혀 멈추는 순간처럼 물리적으로 미세하게 떨어졌다 다시 붙는 경우, 이 값이 없으면 그걸 새 충돌로 오인해 착지 사운드가 처음엔 묻히고 멈추는 순간에야 뒤늦게 재생된다.")]
    public float contactReleaseGraceDuration = 0.15f;

    private Rigidbody2D rb;
    private bool isFallAudioPlaying;
    private readonly HashSet<Collider2D> knockbackFreeContacts = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> currentContacts = new HashSet<Collider2D>();
    private readonly Dictionary<Collider2D, Coroutine> pendingContactReleases = new Dictionary<Collider2D, Coroutine>();

    // 콜라이더별로 "소리가 이미 재생됐고, 아직 정지 대기 중"인지 추적한다. 예전엔 박스
    // 전체에 대해 플래그 하나였는데, 그러면 PressureCorePlatform의 Top처럼 계속 움직이며
    // 박스를 실어 나르는 콜라이더에 닿아있는 동안(rb.velocity가 hitSoundStopVelocity 밑으로
    // 절대 안 떨어짐) 그 사이에 닿는 완전히 다른 새 콜라이더의 소리까지 같이 막혀버렸다 -
    // Top에 처음 닿을 때는 조용하다가, Top이 bottomStopper에 부딪혀 멈추면서 박스 속도가
    // 비로소 정착 임계값 아래로 떨어지고 그 충격으로 살짝 재접촉이 일어날 때만 소리가 나는
    // 원인이었다. 콜라이더별로 나누면 Top처럼 새로 닿는 콜라이더는 박스가 다른 이유로
    // 계속 움직이고 있어도 즉시 소리가 나고, 같은 콜라이더에서 짧게 여러 번 튕기는 것만
    // 계속 억눌린다(원래 의도).
    private readonly HashSet<Collider2D> settleWaitingContacts = new HashSet<Collider2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        DisableRunwayReactionForceFromPlayer();
    }

    private void FixedUpdate()
    {
        UpdateFallAudio();
        UpdateHitSoundSettle();
    }

    // hit_Box가 재생을 시작하면(첫 충돌), Rigidbody2D 속도가 hitSoundStopVelocity 이하로
    // 떨어져 완전히 정지할 때까지는 다시 재생하지 않도록 대기 상태를 유지한다. 착지하며
    // 여러 번 튕기는 동안 계속 이 속도 이상을 유지하므로 재트리거가 막히다가, 진짜로
    // 멈추고 나서 다음 충돌(예: 다시 화살로 밀려서 굴러가다 부딪히는 경우)부터 정상적으로
    // 재생된다.
    private void UpdateHitSoundSettle()
    {
        if (settleWaitingContacts.Count == 0 || rb == null)
            return;

        if (rb.velocity.sqrMagnitude <= hitSoundStopVelocity * hitSoundStopVelocity)
            settleWaitingContacts.Clear();
    }

    private void UpdateFallAudio()
    {
        if (fall_Box == null || rb == null)
            return;

        bool isFalling = !IsHangingFromRope() && rb.velocity.y < 0f;

        if (isFalling == isFallAudioPlaying)
            return;

        isFallAudioPlaying = isFalling;

        if (isFalling)
            fall_Box.Play();
        else
            fall_Box.Stop();
    }

    // 밧줄(Rope)에 매달린 동안은 진자처럼 흔들리며 velocity.y가 순간적으로 음수가 될 수 있는데,
    // 이건 "떨어지는" 게 아니라 "매달려 흔들리는" 거라 낙하 소리가 나면 안 된다.
    // Rope.AttachHangingObjects()는 매달 대상(이 박스)의 GameObject에 직접 HingeJoint2D를
    // 추가하므로 우선 그 컴포넌트가 있는지로 판단하되, 단순히 있는지만 보면 안 된다 -
    // RopeSegment.Cut()은 세그먼트 자신의(위쪽 체인과 연결된) Joint만 파괴할 뿐, 이 박스 쪽
    // HingeJoint2D는 건드리지 않는다. 그래서 로프가 끊어져 박스+끊어진 세그먼트가 같이
    // 자유낙하하는 동안에도 이 Joint는 계속 남아있어, Joint 존재 여부만으로는 "끊어진 뒤에도
    // 영원히 매달린 것"으로 오판해 낙하 소리가 재생되지 않는 문제가 있었다.
    //
    // 처음엔 "박스가 직접 매달린 세그먼트 자신이 끊어졌는지"(segment.IsCut)로 판단했었는데,
    // 화살은 보통 로프 중간/위쪽을 자르지 박스가 매달린 (대개 마지막) 세그먼트 자체를 자르는
    // 게 아니라서 그 세그먼트 자신의 IsCut은 계속 false로 남는다 - 위쪽 어딘가가 끊어지면
    // 그 아래 세그먼트들(박스가 매달린 세그먼트 포함)은 서로 간의 Joint는 그대로 유지한 채
    // 하나의 사슬로 통째로 떨어진다. 그래서 세그먼트 하나가 아니라 로프 전체가 끊어졌는지
    // (Rope.IsCut - 세그먼트 중 하나라도 끊기면 즉시 true)를 확인해야 한다.
    private bool IsHangingFromRope()
    {
        if (!TryGetComponent(out HingeJoint2D hinge))
            return false;

        if (hinge.connectedBody == null)
            return false;

        if (hinge.connectedBody.TryGetComponent(out RopeSegment segment) &&
            segment.Owner != null && segment.Owner.IsCut)
            return false;

        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        StopFallBounce(collision);
        RegisterContact(collision.collider);
    }

    // Unity는 물리 시뮬레이션이 시작되기 전부터 이미 겹쳐있던 콜라이더 쌍에 대해서는
    // OnCollisionEnter2D를 아예 발생시키지 않는다(Stay만 계속 호출됨) - 예를 들어 박스가
    // 씬에 배치될 때부터 이미 바닥/PressureCorePlatform의 Top에 걸쳐 있었거나, 플랫폼에
    // 실려 내려가는 동안 한 번도 완전히 떨어진 적이 없는 경우가 그렇다. 그래서 Stay에서도
    // 아직 등록되지 않은 콜라이더를 잡아 Enter를 놓친 접촉을 뒤늦게라도 캐치한다.
    // RegisterContact 자체가 currentContacts로 중복 실행을 막으므로, 정상적으로 Enter가
    // 먼저 발동된 경우엔 Stay가 다시 hit_Box를 재생시키지 않는다.
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 착지 첫 프레임(Enter) 이후에도, 깊이 파고든 상태를 Box2D가 여러 스텝에 걸쳐
        // 보정하는 동안 중력이 다시 velocity.y를 음수로 만들 수 있다. Stay에서도 계속
        // 정리해줘야 그 사이 프레임들에서 다시 튀어 보이는 걸 막을 수 있다.
        StopFallBounce(collision);
        RegisterContact(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        BeginContactRelease(collision.collider);
    }

    // 접촉이 끊기자마자 바로 currentContacts 등에서 빼면, PressureCorePlatform의 Top이
    // Bottom에 부딪혀 멈추는 순간처럼 물리적으로 미세하게 떨어졌다 다시 붙는 걸 "새 충돌"로
    // 오인한다 - RegisterContact()가 다시 currentContacts.Add()에 성공해버려서, 착지 사운드가
    // 처음 닿았을 땐 안 나고(방금 재생한 뒤라 아직 settleWaitingContacts에 있어 막힘) 이 재접촉
    // 시점에야 새로 재생되는 것처럼 보인다. 즉시 빼지 않고 contactReleaseGraceDuration만큼
    // 기다린 뒤에도 여전히 재접촉이 없을 때만 진짜로 뺀다 (PressureCorePlatform의
    // releaseGraceDuration과 같은 패턴).
    private void BeginContactRelease(Collider2D collider)
    {
        if (!currentContacts.Contains(collider))
            return;

        CancelPendingContactRelease(collider);
        pendingContactReleases[collider] = StartCoroutine(ReleaseContactAfterGrace(collider));
    }

    private IEnumerator ReleaseContactAfterGrace(Collider2D collider)
    {
        yield return new WaitForSeconds(contactReleaseGraceDuration);

        pendingContactReleases.Remove(collider);
        currentContacts.Remove(collider);
        settleWaitingContacts.Remove(collider);
        knockbackFreeContacts.Remove(collider);
    }

    private void CancelPendingContactRelease(Collider2D collider)
    {
        if (pendingContactReleases.TryGetValue(collider, out Coroutine routine))
        {
            StopCoroutine(routine);
            pendingContactReleases.Remove(collider);
        }
    }

    // 반발력(bounciness)은 0이지만, 높은 곳에서 떨어진 무거운 박스는 착지 순간 속도가 커서
    // Box2D가 한 스텝 만에 관통을 완전히 보정하지 못하고 몇 프레임에 걸쳐 밀어내는 과정에서
    // 튀어 보일 수 있다. 위에서 떨어져 닿은(접촉면 법선이 충분히 위를 향한) 접촉이고 아직
    // 아래로 움직이는 중이면, 그 잔여 하강 속도를 직접 0으로 정리해서 튐을 막는다.
    private void StopFallBounce(Collision2D collision)
    {
        if (rb == null || rb.velocity.y >= 0f)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= landingNormalThreshold)
            {
                Vector2 velocity = rb.velocity;
                velocity.y = 0f;
                rb.velocity = velocity;
                return;
            }
        }
    }

    private void RegisterContact(Collider2D collider)
    {
        // Runway 콜라이더는 플레이어와 충돌이 그대로 발생하도록 의도적으로 남겨뒀으므로
        // (DisableRunwayReactionForceFromPlayer() 참고), 여기서 플레이어를 걸러내
        // 충돌 사운드만 나지 않게 한다.
        if (collider.GetComponentInParent<PlayerController>() != null)
            return;

        // 유예 대기 중(BeginContactRelease)이었다면 실제로는 끊긴 적이 없는 것으로 처리한다.
        CancelPendingContactRelease(collider);

        if (!currentContacts.Add(collider))
            return;

        if (hit_Box != null && !settleWaitingContacts.Contains(collider))
        {
            settleWaitingContacts.Add(collider);
            hit_Box.Play();
        }

        // 접촉한 콜라이더 자신이 아니라 부모 쪽에 있을 수도 있다 (예: PressureCorePlatform은
        // 실제로 부딪히는 "Top" 자식이 아니라 그 부모에 붙어있음).
        if (collider.GetComponentInParent<IBoxKnockbackFree>() != null)
            knockbackFreeContacts.Add(collider);
    }

    // RopeRegenerator 등 외부에서 이전에 떨어진 박스를 치울 때, 바로 Destroy하는 대신
    // 이걸 호출한다. disappear_Box를 먼저 재생하고, Destroy(gameObject, delay)로 소리가
    // 끝까지 들릴 시간을 준 뒤에 실제로 파괴한다 (즉시 Destroy하면 AudioSource도 같이
    // 사라져서 소리가 시작하자마자 끊긴다).
    public void PlayDisappearSoundAndDestroy()
    {
        if (disappear_Box != null)
        {
            disappear_Box.Play();
            Destroy(gameObject, disappearDestroyDelay);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 박스 "몸체" 콜라이더와 플레이어 사이의 충돌은 ColliderIgnore 컴포넌트(excludeLayers)가
    // 완전히 배제하는 걸 전제로 하므로 여기서는 다루지 않는다. 여기서 다루는 건 오직
    // "Runway" 태그가 붙은 발판 콜라이더뿐이다 - 플레이어가 그 위에 정상적으로 설 수 있어야
    // 하므로 충돌 감지와 "박스 → 플레이어" 방향 힘(플레이어를 떠받치는 힘)은 그대로 두고,
    // "플레이어 → 박스" 방향 힘만 꺼서 플레이어가 위에서 떨어져도 박스(가벼운 Dynamic
    // Rigidbody2D)가 흔들리거나 밀리지 않게 한다. forceSendLayers는 기본값(-1, 전 레이어
    // 포함)을 그대로 둬서 플레이어 쪽은 정상적으로 힘을 받는다.
    private void DisableRunwayReactionForceFromPlayer()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
            return;

        int excludeMask = ~(1 << playerLayer);

        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider2D col = ownColliders[i];

            if (col != null && col.CompareTag("Runway"))
                col.forceReceiveLayers &= excludeMask;
        }
    }

    public void OnHit()
    {
    }

    public void OnArrowKnockback(Vector2 hitPoint, Vector2 hitDirection)
    {
        if (rb == null)
            return;

        // IBoxKnockbackFree를 구현한 오브젝트(예: PressureCorePlatform)에 닿아있는 동안은
        // 넉백력을 0으로 만들어(=아예 힘을 가하지 않아) 화살에 맞아도 밀리지 않게 한다.
        if (knockbackFreeContacts.Count > 0)
            return;

        float xDir = Mathf.Sign(hitDirection.x);
        rb.AddForce(new Vector2(xDir * knockbackForce, 0f), ForceMode2D.Impulse);
    }
}
