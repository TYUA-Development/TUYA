using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public float flyTime = 2.0f;
    public float gravityValue = 3.0f;

    [Header("Hit FX")]
    public GameObject arrowHitFX;

    [Header("Wind Light")]
    [Tooltip("켜면 arrowLight가 바람(Object_Wind)에 실제로 힘을 받고 있는 동안에만(차단되지 않은 경우에만) 켜집니다. 트리거에 들어와 있어도 장애물에 막혀 있으면 켜지지 않습니다. 꺼두면(기본값) arrowLight는 항상 켜진 채로 둡니다.")]
    public bool windLight = false;

    [Tooltip("windLight가 켜져 있을 때 밝기를 페이드시킬 대상 Light 2D. 비워두면 자식에서 자동으로 찾습니다.")]
    public Light2D arrowLight;

    [Tooltip("windLight의 밝기가 0<->기본 밝기로 페이드 인/아웃되는 데 걸리는 시간(초).")]
    public float windLightFadeDuration = 0.25f;

    private Rigidbody2D rb;
    private Collider2D selfCollider;
    private Transform shooter;

    private bool hasHit = false;

    private ParticleSystem[] flightParticles;
    private TrailRenderer[] flightTrails;

    // 트리거에 들어와 있는 Object_Wind들. 실제로 빛을 켤지는 매 프레임 이 중 하나라도
    // IsBlocked()가 false인(=차단되지 않고 힘을 받는) 게 있는지로 다시 판단한다 - 단순히
    // 트리거 안에 있다는 것만으로는 장애물에 막혀 실제로 힘을 안 받는 경우까지 켜져 버린다.
    private readonly HashSet<Object_Wind> activeWinds = new HashSet<Object_Wind>();

    private float arrowLightBaseIntensity = 1f;
    private float arrowLightTargetIntensity = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();
        flightParticles = GetComponentsInChildren<ParticleSystem>(true);
        flightTrails = GetComponentsInChildren<TrailRenderer>(true);

        if (windLight)
        {
            if (arrowLight == null)
                arrowLight = GetComponentInChildren<Light2D>(true);

            if (arrowLight != null)
            {
                arrowLightBaseIntensity = arrowLight.intensity;
                arrowLight.intensity = 0f;
                arrowLight.enabled = true;
            }
        }
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
        UpdateWindLightTarget();
        UpdateWindLightFade();

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
        HandleWindTriggerEnter(other);

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

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<Object_Wind>(out Object_Wind wind))
            activeWinds.Remove(wind);
    }

    private void HandleWindTriggerEnter(Collider2D other)
    {
        if (!windLight)
            return;

        if (!other.TryGetComponent<Object_Wind>(out Object_Wind wind))
            return;

        if (((1 << gameObject.layer) & wind.ignoredLayer.value) != 0)
            return;

        activeWinds.Add(wind);
    }

    // 트리거 안에 있는 바람들 중 실제로(차단되지 않고) 힘을 주고 있는 게 하나라도 있는지
    // 매 프레임 다시 확인한다 - 장애물에 막혀 있으면 트리거 안에 있어도 켜지지 않는다.
    private void UpdateWindLightTarget()
    {
        if (!windLight || arrowLight == null)
            return;

        bool receivingForce = false;

        foreach (Object_Wind wind in activeWinds)
        {
            if (wind != null && !wind.IsBlocked(selfCollider))
            {
                receivingForce = true;
                break;
            }
        }

        arrowLightTargetIntensity = receivingForce ? arrowLightBaseIntensity : 0f;
    }

    private void UpdateWindLightFade()
    {
        if (!windLight || arrowLight == null)
            return;

        float rate = arrowLightBaseIntensity / Mathf.Max(windLightFadeDuration, 0.0001f);
        arrowLight.intensity = Mathf.MoveTowards(arrowLight.intensity, arrowLightTargetIntensity, rate * Time.deltaTime);
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