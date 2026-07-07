using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object_Wind : MonoBehaviour, ICoreEvent
{
    public float windPower;

    public bool blockPlayer;

    private Vector2 direction;
    private Vector2 power;

    private Dictionary<Collider2D, Rigidbody2D> colliderList = new Dictionary<Collider2D, Rigidbody2D> ();
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];
    private Collider2D windCollider;

    [Header("Particle Wind")]
    public ParticleSystem[] affectedParticleSystems;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    public void Init()
    {
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        power = direction * windPower;

        windCollider = GetComponent<Collider2D>();

        return;
    }

    public void FixedUpdate()
    {
        foreach (var rb in colliderList.Values)
        {
            rb.velocity += power * Time.deltaTime;
        }

        if (affectedParticleSystems != null)
        {
            foreach (var ps in affectedParticleSystems)
            {
                if (ps != null)
                    PushParticlesInRange(ps);
            }
        }
    }

    private void PushParticlesInRange(ParticleSystem ps)
    {
        int count = ps.particleCount;
        if (count == 0 || windCollider == null)
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

            if (windCollider.OverlapPoint(worldPos))
                particleBuffer[i].velocity += velocityDelta;
        }

        ps.SetParticles(particleBuffer, count);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Rigidbody2D rb))
        {
            colliderList[collision] = rb;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        colliderList.Remove(collision);
    }

    public void OnCoreEvent()
    {
        
    }
}
