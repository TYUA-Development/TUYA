using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public float flyTime = 2.0f;
    public float gravityValue = 3.0f;

    private Rigidbody2D rb;
    private Transform shooter;

    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        gravityValue = rb.gravityScale;
        rb.gravityScale = 0.0f;
    }

    // 외부(플레이어)에서 방향을 넘겨서 쏘는 함수
    public void Launch(Vector2 dir, Transform shooter)
    {
        this.shooter = shooter;

        dir = dir.normalized;

        rb.velocity = dir * speed;        // 초기 속도
        transform.right = dir;            // 화살 앞부분이 방향 보게 회전

        Destroy(gameObject, lifeTime);    // 일정 시간 뒤 자동 삭제
    }

    private void Update()
    {
        if (hasHit)
            return;

        if (flyTime > 0)
            flyTime -= Time.deltaTime;
        else
            rb.gravityScale = gravityValue;
    }

    void FixedUpdate()
    {
        if (hasHit)
            return;

        // 속도 방향으로 계속 회전(포물선 꺾일 때 화살도 같이 숙여짐)
        if (rb.velocity.sqrMagnitude > 0.01f)
            transform.right = rb.velocity;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        if (shooter == other.transform)
            return;

        if (other.TryGetComponent<IArrowHit>(out IArrowHit target))
        {
            hasHit = true;

            // 맞을 때 사운드 재생
            BowSFXRandomizer bowSFX = FindObjectOfType<BowSFXRandomizer>();

            if (bowSFX != null)
                bowSFX.PlayHit();

            target.OnHit();

            Vector2 hitPoint = other.ClosestPoint(transform.position);

            Stick(other.transform, hitPoint);
        }

        // IArrowHit이 없는 오브젝트에 닿았을 때는 반응하지 않음
    }

    void Stick(Transform target, Vector2 hitPoint)
    {
        transform.position = hitPoint;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        transform.SetParent(target, true);

        Vector3 p = target.lossyScale;
    }
}