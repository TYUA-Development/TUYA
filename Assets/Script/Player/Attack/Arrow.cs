using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public float flyTime = 2.0f;
    public float gravityValue = 3.0f;

    [Header("Hit FX")]
    public GameObject arrowHitFX;

    private Rigidbody2D rb;
    private Transform shooter;

    private bool hasHit = false;

    private ParticleSystem[] flightParticles;
    private TrailRenderer[] flightTrails;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        flightParticles = GetComponentsInChildren<ParticleSystem>(true);
        flightTrails = GetComponentsInChildren<TrailRenderer>(true);
    }

    private void Start()
    {
        gravityValue = rb.gravityScale;
        rb.gravityScale = 0.0f;
    }

    // �ܺ�(�÷��̾�)���� ������ �Ѱܼ� ��� �Լ�
    public void Launch(Vector2 dir, Transform shooter)
    {
        this.shooter = shooter;

        dir = dir.normalized;

        rb.velocity = dir * speed;        // �ʱ� �ӵ�
        transform.right = dir;            // ȭ�� �պκ��� ���� ���� ȸ��

        StartFlightFX();

        Destroy(gameObject, lifeTime);    // ���� �ð� �� �ڵ� ����
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

        // �ӵ� �������� ��� ȸ��(������ ���� �� ȭ�쵵 ���� ������)
        if (rb.velocity.sqrMagnitude > 0.01f)
            transform.right = rb.velocity;
    }

public void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        if (shooter == other.transform)
            return;

        if (other.TryGetComponent<IArrowPassThrough>(out IArrowPassThrough passThrough))
        {
            Vector2 passPoint = other.ClosestPoint(transform.position);
            passThrough.OnArrowPass(passPoint, rb.velocity.normalized);
            return;
        }

        if (other.TryGetComponent<IArrowHit>(out IArrowHit target))
        {
            hasHit = true;

            // 충돌 지점 계산
            Vector2 hitPoint = other.ClosestPoint(transform.position);

            // 날아가는 중 재생되던 파티클/궤적 정지
            StopFlightFX();

            // 피격 시 사운드 재생
            BowSFXRandomizer bowSFX = FindObjectOfType<BowSFXRandomizer>();

            if (bowSFX != null)
                bowSFX.PlayHit(transform.position);

            // 피격 시 이펙트 생성
            SpawnHitFX(hitPoint);

            // 넉백을 받는 오브젝트라면 화살이 박히기(속도 0) 전에 진행 방향을 넘겨준다
            if (other.TryGetComponent<IArrowKnockbackReceiver>(out IArrowKnockbackReceiver knockbackReceiver))
                knockbackReceiver.OnArrowKnockback(hitPoint, rb.velocity.normalized);

            // 타겟 로직/피격 상태 갱신
            target.OnHit();

            // 화살 박히기
            Stick(other.transform, hitPoint);
        }

        // IArrowHit도 IArrowPassThrough도 없는 콜라이더는 무시하고 지나감
    }

    void StartFlightFX()
    {
        if (flightParticles != null)
        {
            for (int i = 0; i < flightParticles.Length; i++)
            {
                if (flightParticles[i] == null)
                    continue;

                flightParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                flightParticles[i].Play();
            }
        }

        if (flightTrails != null)
        {
            for (int i = 0; i < flightTrails.Length; i++)
            {
                if (flightTrails[i] == null)
                    continue;

                flightTrails[i].Clear();
                flightTrails[i].emitting = true;
            }
        }
    }

    void StopFlightFX()
    {
        if (flightParticles != null)
        {
            for (int i = 0; i < flightParticles.Length; i++)
            {
                if (flightParticles[i] == null)
                    continue;

                flightParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (flightTrails != null)
        {
            for (int i = 0; i < flightTrails.Length; i++)
            {
                if (flightTrails[i] == null)
                    continue;

                flightTrails[i].emitting = false;
                flightTrails[i].Clear();
            }
        }
    }

    void SpawnHitFX(Vector2 hitPoint)
    {
        if (arrowHitFX == null)
            return;

        Instantiate(arrowHitFX, hitPoint, transform.rotation);
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