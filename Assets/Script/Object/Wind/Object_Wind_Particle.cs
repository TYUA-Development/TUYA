using UnityEngine;

public class Object_Wind_Particle : MonoBehaviour
{
    public float windPower;

    [Header("Particle Wind")]
    public ParticleSystem[] affectedParticleSystems;

    [Header("Kill On Floor")]
    [Tooltip("이 레이어에 닿으면 파티클을 즉시 제거합니다 (예: Floor)")]
    public LayerMask killOnCollisionLayer;

    private Vector2 power;
    private Collider2D windCollider;
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        power = direction * windPower;

        windCollider = GetComponent<Collider2D>();
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

            if (windCollider != null && windCollider.OverlapPoint(worldPos))
                particleBuffer[i].velocity += velocityDelta;
        }

        ps.SetParticles(particleBuffer, count);
    }
}
