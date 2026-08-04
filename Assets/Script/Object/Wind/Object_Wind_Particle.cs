using System.Collections.Generic;
using UnityEngine;

public class Object_Wind_Particle : MonoBehaviour
{
    [Tooltip("바람 세기 전체 배율(0~1). 0이면 파티클이 밀리지 않고, 1이면 Wind Speed Range에서 배정된 속도가 그대로 적용됩니다. CoreObjectToggle의 바람 페이드 인/아웃이 이 값을 서서히 올리고 내립니다.")]
    [Range(0f, 1f)]
    public float powerScale = 1f;

    [Tooltip("바람이 향하는 방향.")]
    public WindDirection windDirection = WindDirection.Right;

    [Header("Particle Wind")]
    public ParticleSystem[] affectedParticleSystems;

    [Header("Wind Speed Range")]
    [Tooltip("각 파티클에게 배정되는 목표 속도의 최소값")]
    public float windSpeedMin = 5f;
    [Tooltip("각 파티클에게 배정되는 목표 속도의 최대값")]
    public float windSpeedMax = 8f;
    [Tooltip("windSpeedMin~windSpeedMax 사이에서 파티클이 가질 수 있는 속도 간격. 예) Min=5, Max=8, Step=1이면 각 파티클은 5, 6, 7, 8 중 하나의 속도를 무작위로 배정받아 태어날 때부터 죽을 때까지 그 속도를 유지합니다.")]
    public float speedStep = 1f;

    [Header("Stretch By Speed")]
    [Tooltip("체크하면 동그란 파티클 하나를 Stretched Billboard로 렌더링해서, 파티클 속도(=밀려나는 세기)에 비례해 길쭉하게 늘어나 보이게 합니다. 별도의 길쭉한 파티클을 따로 둘 필요가 없습니다.")]
    public bool stretchBySpeed = true;
    [Tooltip("ParticleSystemRenderer.lengthScale - 파티클 기본 길이 배율")]
    public float stretchLengthScale = 2f;
    [Tooltip("ParticleSystemRenderer.velocityScale - 속도가 빠를수록 길이가 늘어나는 정도")]
    public float stretchVelocityScale = 0.05f;

    [Header("Lifetime Fade")]
    [Tooltip("체크하면 파티클이 자신의 생명주기(Lifetime)가 끝나갈수록 점점 투명해지다가 사라집니다. 끄면 기존처럼 파티클 시스템의 Lifetime이 다 되는 순간 바로 사라집니다.")]
    public bool fadeOutOverLifetime = true;

    [Range(0f, 1f)]
    [Tooltip("파티클 수명(0~1, 0=생성 직후, 1=수명 끝) 중 이 지점부터 투명해지기 시작합니다. 0 = 태어나자마자 서서히 옅어짐, 1에 가까울수록 수명 막바지에만 급격히 사라짐.")]
    public float fadeStartLifetimePercent = 0.5f;

    [Header("Kill On Floor")]
    [Tooltip("이 레이어에 닿으면 파티클을 즉시 제거합니다 (예: Floor)")]
    public LayerMask killOnCollisionLayer;

    [Header("Object Blocking")]
    [Tooltip("이 레이어의 콜라이더가 바람 오브젝트와 파티클 사이를 가로막으면 그 파티클에 영향을 줍니다. 예: Wall")]
    public LayerMask blockingLayer;

    [Tooltip("차단됐을 때 즉시 사라지는 대신, 그 시점의 알파에서 0이 될 때까지 걸리는 시간(초). 0이면 기존처럼 즉시 사라집니다.")]
    public float blockedFadeOutDuration = 0.15f;

    [Header("Wind Link (다른 Wind_Particle로 연결)")]
    [Tooltip("이 지점 반경 안에 들어온 파티클은 속도(크기)는 유지한 채 방향만 Connection Target Point 쪽으로 꺾입니다. 비워두면 연결 기능을 사용하지 않습니다.")]
    public Transform connectionPoint;

    [Tooltip("연결 시 파티클이 향할 지점. 보통 연결하려는 다른 Wind_Particle의 시작 지점에 배치합니다. 이 지점에 도달한 뒤로는 그 Wind_Particle이 자기 콜라이더 안에서 알아서 밀어줍니다.")]
    public Transform connectionTargetPoint;

    [Tooltip("Connection Point로부터 이 반경(유닛) 안에 들어오면 방향 전환이 시작됩니다.")]
    public float connectionRadius = 0.5f;

    [Header("Release (파티클 생성을 멈출 때)")]
    [Tooltip("Release()가 호출되면(예: CoreObjectToggle에서 바람을 끌 때), 그 순간 살아있는 파티클은 더 이상 바람에 밀리지 않고 그 시점의 속도 그대로 직진합니다. Release된 지점부터 이 거리(유닛)만큼 날아가는 동안 알파가 1에서 0으로 선형 감소하다가 사라집니다.")]
    public float releaseFadeDistance = 5f;

    private Vector2 windDir;
    private Collider2D windCollider;
    private bool released;

    private struct BlockedFadeState
    {
        public float elapsed;
        public byte baselineAlpha;
    }

    private readonly Dictionary<ParticleSystem, Dictionary<uint, BlockedFadeState>> blockedFadeStates =
        new Dictionary<ParticleSystem, Dictionary<uint, BlockedFadeState>>();
    private readonly Dictionary<ParticleSystem, Dictionary<uint, Vector2>> releaseOrigins =
        new Dictionary<ParticleSystem, Dictionary<uint, Vector2>>();
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];

    private float? basePowerScale;

    // Object_Wind.baseWindPower와 같은 이유 및 같은 지연 캡처 폴백이 필요하다 - 처음부터
    // GameObject가 비활성화된 파티클 바람은 Awake가 실행되지 않으므로, BasePowerScale이 처음
    // 조회되는 시점에 캡처하는 경로도 함께 둔다. 두 경로 모두 "아직 값이 없을 때만" 캡처하도록
    // 가드해야 SetActive(true) 안에서 뒤늦게 실행되는 Awake가 지연 캡처 결과를 덮어쓰지 않는다.
    private void Awake()
    {
        if (!basePowerScale.HasValue)
            basePowerScale = powerScale;
    }

    public float BasePowerScale
    {
        get
        {
            if (!basePowerScale.HasValue)
                basePowerScale = powerScale;

            return basePowerScale.Value;
        }
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        windDir = Object_Wind.GetDirectionVector(windDirection);
        windCollider = GetComponent<Collider2D>();

        released = false;
        releaseOrigins.Clear();

        ApplyStretchSettings();
        ApplyLifetimeFadeSettings();
    }

    // 파티클 생성을 막 멈췄을 때 호출한다. 그 순간 살아있는 파티클은 더 이상 바람에 밀리지 않고
    // (PushParticlesInRange가 속도를 건드리지 않으므로 지금 속도 그대로 직진), release 지점부터의
    // 이동 거리에 비례해 개별적으로 페이드아웃된다. 수명 기반 페이드(Color Over Lifetime)와
    // 거리 기반 페이드가 곱해져 이중으로 어두워지지 않도록 그 모듈은 꺼둔다.
    public void Release()
    {
        released = true;

        if (affectedParticleSystems == null)
            return;

        foreach (var ps in affectedParticleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = false;
        }
    }

    // 파티클의 randomSeed로부터 결정론적으로 목표 속도를 골라낸다. seed는 파티클이 태어날 때
    // 정해져 평생 바뀌지 않으므로, 별도의 상태 추적(Dictionary) 없이도 같은 파티클은 항상 같은
    // 속도를 유지한다. windSpeedMin~windSpeedMax를 speedStep 간격으로 나눈 값들 중 하나로 고정된다.
    private float GetAssignedSpeed(uint seed)
    {
        int steps = speedStep > 0f
            ? Mathf.FloorToInt((windSpeedMax - windSpeedMin) / speedStep + 0.0001f) + 1
            : 1;
        steps = Mathf.Max(steps, 1);

        int index = (int)(seed % (uint)steps);
        return windSpeedMin + index * speedStep;
    }

    public void SetEmissionEnabled(bool value)
    {
        if (affectedParticleSystems == null)
            return;

        foreach (var ps in affectedParticleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = value;
        }
    }

    // CoreObjectToggle이 이 파티클 바람이 "지금 켜져 있는지"를 판단할 때 쓴다. Object_Wind의
    // IsColliderEnabled와 같은 이유로, 별도 상태 변수 대신 첫 번째 파티클 시스템의 emission
    // 활성 여부를 그때그때 조회한다.
    public bool IsEmissionEnabled
    {
        get
        {
            if (affectedParticleSystems == null)
                return false;

            foreach (var ps in affectedParticleSystems)
            {
                if (ps != null)
                    return ps.emission.enabled;
            }

            return false;
        }
    }

    public void SetParticlesAlpha(float alpha)
    {
        if (affectedParticleSystems == null)
            return;

        byte alphaByte = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);

        foreach (var ps in affectedParticleSystems)
        {
            if (ps == null)
                continue;

            int count = ps.particleCount;
            if (count == 0)
                continue;

            if (particleBuffer.Length < count)
                particleBuffer = new ParticleSystem.Particle[count];

            count = ps.GetParticles(particleBuffer);

            for (int i = 0; i < count; i++)
            {
                Color32 color = particleBuffer[i].startColor;
                color.a = alphaByte;
                particleBuffer[i].startColor = color;
            }

            ps.SetParticles(particleBuffer, count);
        }
    }

    public void StopAndClearParticles()
    {
        if (affectedParticleSystems == null)
            return;

        foreach (var ps in affectedParticleSystems)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void ApplyStretchSettings()
    {
        if (!stretchBySpeed || affectedParticleSystems == null)
            return;

        foreach (var ps in affectedParticleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer == null)
                continue;

            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            psRenderer.lengthScale = stretchLengthScale;
            psRenderer.velocityScale = stretchVelocityScale;
        }
    }

    // Color Over Lifetime 모듈에 알파 그라디언트를 대입한다. startColor.a(SetParticlesAlpha가
    // 쓰는 값)와는 별도의 파이프라인이라 최종 알파는 두 값의 곱으로 적용되므로, 코어 토글 시의
    // 전체 페이드(SetParticlesAlpha)와 이 수명별 페이드가 서로 덮어쓰며 충돌하지 않는다.
    private void ApplyLifetimeFadeSettings()
    {
        if (affectedParticleSystems == null)
            return;

        foreach (var ps in affectedParticleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = fadeOutOverLifetime;

            if (!fadeOutOverLifetime)
                continue;

            float fadeStart = Mathf.Clamp(fadeStartLifetimePercent, 0f, 0.999f);

            var alphaKeys = new List<GradientAlphaKey> { new GradientAlphaKey(1f, 0f) };
            if (fadeStart > 0f)
                alphaKeys.Add(new GradientAlphaKey(1f, fadeStart));
            alphaKeys.Add(new GradientAlphaKey(0f, 1f));

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys.ToArray());

            colorOverLifetime.color = gradient;
        }
    }

    private void FixedUpdate()
    {
        if (affectedParticleSystems == null)
            return;

        foreach (var ps in affectedParticleSystems)
        {
            if (ps != null)
                PushParticlesInRange(ps);
        }
    }

    private void PushParticlesInRange(ParticleSystem ps)
    {
        int count = ps.particleCount;
        if (count == 0)
            return;

        if (particleBuffer.Length < count)
            particleBuffer = new ParticleSystem.Particle[count];

        count = ps.GetParticles(particleBuffer);
        bool isWorldSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;

        blockedFadeStates.TryGetValue(ps, out Dictionary<uint, BlockedFadeState> previousBlocked);
        Dictionary<uint, BlockedFadeState> currentBlocked = null;

        Dictionary<uint, Vector2> origins = null;
        if (released && !releaseOrigins.TryGetValue(ps, out origins))
        {
            origins = new Dictionary<uint, Vector2>();
            releaseOrigins[ps] = origins;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = isWorldSpace
                ? particleBuffer[i].position
                : ps.transform.TransformPoint(particleBuffer[i].position);

            if (killOnCollisionLayer.value != 0 && Physics2D.OverlapPoint(worldPos, killOnCollisionLayer))
            {
                particleBuffer[i].remainingLifetime = 0f;
                continue;
            }

            if (IsBlocked(worldPos))
            {
                // 즉시 제거하는 대신, 차단되기 시작한 시점의 알파를 기준으로 blockedFadeOutDuration
                // 동안 0까지 서서히 투명해지다가 사라지게 한다. randomSeed로 파티클 개체를 프레임 간
                // 추적하며, 이번 프레임에 차단된 파티클만 currentBlocked에 다시 담아 매 프레임 새로
                // 구성한다 (죽거나 더 이상 차단되지 않은 파티클의 기록은 자동으로 버려져 누수되지 않는다).
                if (blockedFadeOutDuration <= 0f)
                {
                    particleBuffer[i].remainingLifetime = 0f;
                    continue;
                }

                uint seed = particleBuffer[i].randomSeed;
                BlockedFadeState state;
                if (previousBlocked != null && previousBlocked.TryGetValue(seed, out state))
                {
                    state.elapsed += Time.deltaTime;
                }
                else
                {
                    state.elapsed = 0f;
                    state.baselineAlpha = particleBuffer[i].startColor.a;
                }

                float fadeT = Mathf.Clamp01(state.elapsed / blockedFadeOutDuration);

                Color32 color = particleBuffer[i].startColor;
                color.a = (byte)Mathf.RoundToInt(Mathf.Lerp(state.baselineAlpha, 0f, fadeT));
                particleBuffer[i].startColor = color;

                if (fadeT >= 1f)
                {
                    particleBuffer[i].remainingLifetime = 0f;
                }
                else
                {
                    if (currentBlocked == null)
                        currentBlocked = new Dictionary<uint, BlockedFadeState>();

                    currentBlocked[seed] = state;
                }

                continue;
            }

            if (released)
            {
                // 속도는 건드리지 않는다 - 그대로 두면 release되기 직전 속도로 계속 직진한다("고정").
                uint seed = particleBuffer[i].randomSeed;
                Vector2 pos2D = worldPos;

                if (!origins.TryGetValue(seed, out Vector2 origin))
                {
                    origin = pos2D;
                    origins[seed] = origin;
                }

                float distance = Vector2.Distance(pos2D, origin);
                float alpha = releaseFadeDistance > 0f
                    ? Mathf.Clamp01(1f - distance / releaseFadeDistance)
                    : 0f;

                Color32 releaseColor = particleBuffer[i].startColor;
                releaseColor.a = (byte)Mathf.RoundToInt(alpha * 255f);
                particleBuffer[i].startColor = releaseColor;

                if (alpha <= 0f)
                    particleBuffer[i].remainingLifetime = 0f;

                continue;
            }

            if (connectionPoint != null && connectionTargetPoint != null)
            {
                Vector2 particlePos2D = worldPos;
                float connectionDistSqr = ((Vector2)connectionPoint.position - particlePos2D).sqrMagnitude;

                if (connectionDistSqr <= connectionRadius * connectionRadius)
                {
                    // 방향만 새 목표(연결된 Wind의 시작 지점)로 바꾸고 속도 크기는 그대로 유지한다.
                    Vector2 toTarget = (Vector2)connectionTargetPoint.position - particlePos2D;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        float speed = particleBuffer[i].velocity.magnitude;
                        particleBuffer[i].velocity = toTarget.normalized * speed;
                    }

                    continue;
                }
            }

            if (windDir != Vector2.zero && windCollider != null && windCollider.OverlapPoint(worldPos))
            {
                // 매 프레임 힘을 누적(+=)하면 시간이 지날수록 속도가 계속 쌓이는 문제가 있었다.
                // 그 대신 바람 방향 속도 성분을 파티클마다 고정된 목표 속도로 매 프레임 직접
                // 대입한다 (누적 없음). 바람과 무관한 축의 속도(중력 낙하 등)는 그대로 보존한다.
                float targetSpeed = GetAssignedSpeed(particleBuffer[i].randomSeed) * powerScale;
                Vector2 currentVelocity = particleBuffer[i].velocity;
                Vector2 alongWind = Vector2.Dot(currentVelocity, windDir) * windDir;
                Vector2 perpendicular = currentVelocity - alongWind;
                particleBuffer[i].velocity = perpendicular + windDir * targetSpeed;
            }
        }

        ps.SetParticles(particleBuffer, count);

        if (currentBlocked != null)
            blockedFadeStates[ps] = currentBlocked;
        else
            blockedFadeStates.Remove(ps);
    }

    private bool IsBlocked(Vector2 targetPosition)
    {
        if (blockingLayer.value == 0)
            return false;

        Vector2 origin = transform.position;
        Vector2 toTarget = targetPosition - origin;
        float distance = toTarget.magnitude;

        if (distance <= 0.0001f)
            return false;

        Vector2 direction = toTarget / distance;
        return Physics2D.Raycast(origin, direction, distance, blockingLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (connectionPoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(connectionPoint.position, connectionRadius);

        if (connectionTargetPoint != null)
            Gizmos.DrawLine(connectionPoint.position, connectionTargetPoint.position);
    }
}
