using UnityEngine;

public class BoxObject : MonoBehaviour, IArrowHit, IArrowKnockbackReceiver
{
    [Header("Knockback")]
    [Tooltip("화살에 맞았을 때 화살이 날아간 방향으로 가해지는 힘의 크기")]
    public float knockbackForce = 10f;

    [Header("Player Collision")]
    [Tooltip("Player와 충돌을 무시할 콜라이더")]
    public BoxCollider2D boxCollider2D;

    [Header("Audio")]
    [Tooltip("아래로 떨어지는 동안(Rigidbody2D.velocity.y < 0) 반복 재생할 낙하 소리. AudioAssist의 Loop를 켜두어야 합니다.")]
    public AudioAssist fall_Box;

    [Tooltip("바닥/발판 등 다른 콜라이더와 부딪혔을 때 한 번 재생할 충돌 소리.")]
    public AudioAssist hit_Box;

    [Tooltip("이 박스가 소멸될 때(예: RopeRegenerator가 이전에 떨어진 박스를 제거할 때) 한 번 재생할 효과음. 재생만으로는 자동 트리거되지 않고, 외부(RopeRegenerator 등)에서 PlayDisappearSoundAndDestroy()를 호출해야 합니다.")]
    public AudioAssist disappear_Box;

    [Tooltip("disappear_Box 재생 후 실제로 오브젝트를 파괴하기까지 대기하는 시간(초). Destroy가 AudioSource도 같이 없애버리므로, 소리가 끝까지 들리려면 클립 길이 이상으로 설정하세요.")]
    public float disappearDestroyDelay = 1f;

    private Rigidbody2D rb;
    private bool isFallAudioPlaying;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        IgnorePlayerCollision();
    }

    private void FixedUpdate()
    {
        UpdateFallAudio();
    }

    private void UpdateFallAudio()
    {
        if (fall_Box == null || rb == null)
            return;

        // 밧줄(Rope)에 매달린 동안은 진자처럼 흔들리며 velocity.y가 순간적으로 음수가 될 수
        // 있는데, 이건 "떨어지는" 게 아니라 "매달려 흔들리는" 거라 낙하 소리가 나면 안 된다.
        // Rope.AttachHangingObjects()는 매달 대상(이 박스)의 GameObject에 직접
        // HingeJoint2D를 추가하므로, 그 컴포넌트가 있는지로 현재 매달린 상태인지 판단한다.
        bool isHangingFromRope = TryGetComponent<HingeJoint2D>(out _);
        bool isFalling = !isHangingFromRope && rb.velocity.y < 0f;

        if (isFalling == isFallAudioPlaying)
            return;

        isFallAudioPlaying = isFalling;

        if (isFalling)
            fall_Box.Play();
        else
            fall_Box.Stop();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hit_Box != null)
            hit_Box.Play();
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

    private void IgnorePlayerCollision()
    {
        if (boxCollider2D == null)
            return;

        var player = FindObjectOfType<PlayerController>();
        if (player != null && player.TryGetComponent<Collider2D>(out var playerCollider))
            Physics2D.IgnoreCollision(boxCollider2D, playerCollider, true);
    }

    public void OnHit()
    {
    }

    public void OnArrowKnockback(Vector2 hitPoint, Vector2 hitDirection)
    {
        if (rb == null)
            return;

        float xDir = Mathf.Sign(hitDirection.x);
        rb.AddForce(new Vector2(xDir * knockbackForce, 0f), ForceMode2D.Impulse);
    }
}
