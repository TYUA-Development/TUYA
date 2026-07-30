using System.Collections.Generic;
using UnityEngine;

public class Object_Wind_Particle : MonoBehaviour
{
    [Tooltip("바람 방향으로 파티클이 움직이는 목표 속도. 시간에 따라 가속/누적되지 않고, 매 프레임 이 값에 거리 감쇠(Distance Falloff)를 곱한 속도로 즉시 고정됩니다 (멀어질수록 이 값보다 느려짐).")]
    public float windPower;

    [Tooltip("바람이 향하는 방향. windPower가 음수이면 이 방향의 반대로 힘이 작용합니다.")]
    public WindDirection windDirection = WindDirection.Right;

    [Header("Particle Wind")]
    public ParticleSystem[] affectedParticleSystems;

    [Header("Distance Falloff")]
    [Range(0f, 10f)]
    [Tooltip("이 오브젝트(transform.position)에서 멀어질수록 파티클을 미는 힘이 감소하는 정도. 0 = 감소 없음(거리와 무관하게 동일한 힘), 10 = 아주 빠르게 감소")]
    public float distanceFalloff = 0f;

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

    private Vector2 power;
    private Collider2D windCollider;

    private struct BlockedFadeState
    {
        public float elapsed;
        public byte baselineAlpha;
    }

    private readonly Dictionary<ParticleSystem, Dictionary<uint, BlockedFadeState>> blockedFadeStates =
        new Dictionary<ParticleSystem, Dictionary<uint, BlockedFadeState>>();
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        Vector2 direction = Object_Wind.GetDirectionVector(windDirection);
        power = direction * windPower;

        windCollider = GetComponent<Collider2D>();

        ApplyStretchSettings();
        ApplyLifetimeFadeSettings();
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
        Vector2 windDir = power.normalized;
        float maxSpeed = power.magnitude * 0.1f;

        blockedFadeStates.TryGetValue(ps, out Dictionary<uint, BlockedFadeState> previousBlocked);
        Dictionary<uint, BlockedFadeState> currentBlocked = null;

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

            if (windDir != Vector2.zero && windCollider != null && windCollider.OverlapPoint(worldPos))
            {
                // 매 프레임 힘을 누적(+=)하면 오래/멀리 밀린 파티클일수록 속도가 계속 쌓여
                // 오히려 먼 파티클이 더 빨라지는 문제가 있었다. 그 대신 바람 방향 속도 성분을
                // "현재 거리에서의 목표 속도"로 매 프레임 직접 대입한다 (누적 없음, 거리만이
                // 속도를 결정). 바람과 무관한 축의 속도(중력 낙하 등)는 그대로 보존한다.
                float falloff = GetFalloffMultiplier(worldPos);
                Vector2 currentVelocity = particleBuffer[i].velocity;
                Vector2 alongWind = Vector2.Dot(currentVelocity, windDir) * windDir;
                Vector2 perpendicular = currentVelocity - alongWind;
                particleBuffer[i].velocity = perpendicular + windDir * (maxSpeed * falloff);
            }
        }

        ps.SetParticles(particleBuffer, count);

        if (currentBlocked != null)
            blockedFadeStates[ps] = currentBlocked;
        else
            blockedFadeStates.Remove(ps);
    }

    private float GetFalloffMultiplier(Vector2 targetPosition)
    {
        if (distanceFalloff <= 0f)
            return 1f;

        float distance = Vector2.Distance(transform.position, targetPosition);
        return 1f / (1f + distanceFalloff * distance);
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
}
