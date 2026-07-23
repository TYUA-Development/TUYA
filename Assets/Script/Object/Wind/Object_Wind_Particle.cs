using UnityEngine;

public class Object_Wind_Particle : MonoBehaviour
{
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

    [Header("Kill On Floor")]
    [Tooltip("이 레이어에 닿으면 파티클을 즉시 제거합니다 (예: Floor)")]
    public LayerMask killOnCollisionLayer;

    [Header("Object Blocking")]
    [Tooltip("이 레이어의 콜라이더가 바람 오브젝트와 파티클 사이를 가로막으면 그 파티클을 즉시 제거합니다. 예: Wall")]
    public LayerMask blockingLayer;

    private Vector2 power;
    private Collider2D windCollider;
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
        Vector3 velocityDelta = power * 0.1f * Time.deltaTime;

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
                particleBuffer[i].remainingLifetime = 0f;
                continue;
            }

            if (windCollider != null && windCollider.OverlapPoint(worldPos))
            {
                float falloff = GetFalloffMultiplier(worldPos);
                particleBuffer[i].velocity += velocityDelta * falloff;
            }
        }

        ps.SetParticles(particleBuffer, count);
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
