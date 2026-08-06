using System.Collections.Generic;
using UnityEngine;

// 등록된 ParticleSystem들 중, 이 오브젝트의 Collider2D 범위 안에 들어온 파티클만
// 알파를 0으로 낮춰 카메라에 안 보이게 가린다. 범위를 벗어나면 가려지기 직전의
// 알파로 복원된다. 파티클을 죽이거나 지우지 않으므로 무게/충돌 등 다른 로직에는
// 영향을 주지 않는다.
[RequireComponent(typeof(Collider2D))]
public class ParticleMask : MonoBehaviour
{
    [Tooltip("이 영역 안에 들어오면 가려질 파티클 시스템 목록")]
    [SerializeField] private ParticleSystem[] targetParticleSystems;

    private Collider2D maskArea;
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];

    // 파티클별로 가려지기 직전의 알파를 기억해뒀다가, 범위를 벗어나면 그 값으로 복원한다.
    // Object_Wind_Particle의 blockedFadeStates와 같은 이유로 매 프레임 새로 구성한다 -
    // 더 이상 가려지지 않거나 죽은 파티클의 기록은 자동으로 버려져 누수되지 않는다.
    private readonly Dictionary<ParticleSystem, Dictionary<uint, byte>> maskedAlphaStates =
        new Dictionary<ParticleSystem, Dictionary<uint, byte>>();

    private void Awake()
    {
        maskArea = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        if (maskArea == null || targetParticleSystems == null)
            return;

        foreach (ParticleSystem ps in targetParticleSystems)
        {
            if (ps != null)
                MaskParticles(ps);
        }
    }

    private void MaskParticles(ParticleSystem ps)
    {
        int count = ps.particleCount;
        if (count == 0)
        {
            maskedAlphaStates.Remove(ps);
            return;
        }

        if (particleBuffer.Length < count)
            particleBuffer = new ParticleSystem.Particle[count];

        count = ps.GetParticles(particleBuffer);
        bool isWorldSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;

        maskedAlphaStates.TryGetValue(ps, out Dictionary<uint, byte> previousMasked);
        Dictionary<uint, byte> currentMasked = null;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = isWorldSpace
                ? particleBuffer[i].position
                : ps.transform.TransformPoint(particleBuffer[i].position);

            uint seed = particleBuffer[i].randomSeed;
            Color32 color = particleBuffer[i].startColor;

            if (maskArea.OverlapPoint(worldPos))
            {
                // 이미 가려진 상태였다면 그때의 원래 알파를 계속 이어받고, 이번에 처음
                // 가려졌다면 지금 알파를 원래 값으로 기억해둔다.
                byte baselineAlpha = (previousMasked != null && previousMasked.TryGetValue(seed, out byte prevAlpha))
                    ? prevAlpha
                    : color.a;

                if (currentMasked == null)
                    currentMasked = new Dictionary<uint, byte>();
                currentMasked[seed] = baselineAlpha;

                color.a = 0;
                particleBuffer[i].startColor = color;
            }
            else if (previousMasked != null && previousMasked.TryGetValue(seed, out byte restoreAlpha))
            {
                // 방금 영역을 벗어났다면 가려지기 전 알파로 되돌린다.
                color.a = restoreAlpha;
                particleBuffer[i].startColor = color;
            }
            // 가려진 적 없는 파티클은 건드리지 않는다 - 다른 알파 파이프라인(수명 페이드 등)과
            // 충돌하지 않기 위함.
        }

        ps.SetParticles(particleBuffer, count);

        if (currentMasked != null)
            maskedAlphaStates[ps] = currentMasked;
        else
            maskedAlphaStates.Remove(ps);
    }
}
