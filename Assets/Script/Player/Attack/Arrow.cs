using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public float flyTime = 2.0f;
    public float gravityValue = 3.0f;

    [Header("Hit FX")]
    public GameObject arrowHitFX;

    [Header("Special Stick Surfaces")]
    public bool stickToForestTemple9ChildGameObject = true;
    public string templeStickParentName = "temple_9";
    public string templeStickObjectName = "GameObject";

    private Rigidbody2D rb;
    private Transform shooter;

    private bool hasHit = false;

    private ParticleSystem[] flightParticles;
    private TrailRenderer[] flightTrails;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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
        HandleHit(other, other.ClosestPoint(transform.position));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
            return;

        Vector2 hitPoint = transform.position;

        if (collision.contactCount > 0)
            hitPoint = collision.GetContact(0).point;
        else
            hitPoint = collision.collider.ClosestPoint(transform.position);

        HandleHit(collision.collider, hitPoint);
    }

    private void HandleHit(Collider2D other, Vector2 hitPoint)
    {
        if (hasHit)
            return;

        if (other == null)
            return;

        if (shooter == other.transform)
            return;

        IArrowHit target = null;
        bool shouldStick = other.TryGetComponent<IArrowHit>(out target);

        if (!shouldStick)
            shouldStick = IsSpecialStickSurface(other.transform);

        if (!shouldStick)
            return;

        hasHit = true;

        StopFlightFX();

        BowSFXRandomizer bowSFX = FindObjectOfType<BowSFXRandomizer>();

        if (bowSFX != null)
            bowSFX.PlayHit(transform.position);

        SpawnHitFX(hitPoint);

        if (target != null)
            target.OnHit();

        Stick(other.transform, hitPoint);
    }

    private bool IsSpecialStickSurface(Transform target)
    {
        if (!stickToForestTemple9ChildGameObject)
            return false;

        if (target == null)
            return false;

        if (target.name != templeStickObjectName)
            return false;

        Transform parent = target.parent;

        return parent != null && parent.name == templeStickParentName;
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
