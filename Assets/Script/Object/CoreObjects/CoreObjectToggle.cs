using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreObjectToggle : MonoBehaviour
{
    [Header("Core Objects")]
    [Tooltip("어떤 코어를 맞춰도 동일하게 동작합니다. 여러 번 맞출 수 있는지 여부는 각 CoreActivation의 activateOnlyOnce 값을 따릅니다.")]
    public List<CoreActivation> coreObjects;

    [Header("Toggle Targets")]
    [Tooltip("코어가 활성화될 때마다 각 오브젝트의 활성 상태를 개별적으로 반전시킨다 (켜져있으면 끄고, 꺼져있으면 켠다)")]
    public List<GameObject> targetObjects;

    [Header("Wind Fade")]
    [Tooltip("대상에 Object_Wind/Object_Wind_Particle이 자식으로 있으면 즉시 켜고 끄는 대신 WindPower를 이 시간(초) 동안 서서히 올리고 내린다.")]
    public float windFadeDuration = 1f;

    private readonly Dictionary<GameObject, Coroutine> windFadeCoroutines = new Dictionary<GameObject, Coroutine>();

    void Start()
    {
        foreach (var core in coreObjects)
        {
            if (core != null)
                core.onActivated += HandleCoreActivated;
        }
    }

    void OnDestroy()
    {
        foreach (var core in coreObjects)
        {
            if (core != null)
                core.onActivated -= HandleCoreActivated;
        }
    }

    private void HandleCoreActivated()
    {
        foreach (var obj in targetObjects)
        {
            if (obj == null)
                continue;

            if (obj.TryGetComponent<RiseObject>(out var riseObject))
            {
                riseObject.Rise();
                continue;
            }

            if (obj.TryGetComponent<EnableObject>(out var enableObject))
            {
                enableObject.Toggle();
                continue;
            }

            Object_Wind[] winds = obj.GetComponentsInChildren<Object_Wind>(true);
            Object_Wind_Particle[] windParticles = obj.GetComponentsInChildren<Object_Wind_Particle>(true);

            if (winds.Length == 0 && windParticles.Length == 0)
            {
                obj.SetActive(!obj.activeSelf);
                continue;
            }

            bool turningOn = !IsWindCurrentlyOn(obj, winds, windParticles);

            if (windFadeCoroutines.TryGetValue(obj, out var running) && running != null)
                StopCoroutine(running);

            windFadeCoroutines[obj] = StartCoroutine(FadeWindAndToggle(obj, winds, windParticles, turningOn));
        }
    }

    // 같은 바람 오브젝트를 서로 다른 CoreObjectToggle 인스턴스(예: 끄는 스위치와 켜는 스위치가
    // 분리된 경우)가 함께 타겟팅할 수 있어, on/off 여부를 이 컴포넌트가 별도로 기억하면 다른
    // 인스턴스가 먼저 바꾼 상태를 놓칠 수 있다. 그래서 항상 컴포넌트의 실제 현재 상태를 조회한다.
    // obj.activeSelf를 1차로 먼저 보는 이유는, 레벨에 처음부터 GameObject 자체가 비활성화된
    // 채로 배치된("시작부터 꺼진") 바람의 경우 콜라이더/이미션 값 자체는 기본값(활성)으로
    // 저장돼 있을 수 있어 그것만으로는 꺼진 상태로 판단할 수 없기 때문이다.
    private static bool IsWindCurrentlyOn(GameObject obj, Object_Wind[] winds, Object_Wind_Particle[] windParticles)
    {
        if (!obj.activeSelf)
            return false;

        if (winds.Length > 0)
            return winds[0].IsColliderEnabled;

        if (windParticles.Length > 0)
            return windParticles[0].IsEmissionEnabled;

        return false;
    }

    private IEnumerator FadeWindAndToggle(GameObject obj, Object_Wind[] winds, Object_Wind_Particle[] windParticles, bool turningOn)
    {
        float[] windStart = new float[winds.Length];
        float[] windTarget = new float[winds.Length];

        for (int i = 0; i < winds.Length; i++)
        {
            Object_Wind wind = winds[i];

            windStart[i] = turningOn ? 0f : wind.windPower;
            windTarget[i] = turningOn ? wind.BaseWindPower : 0f;
        }

        float[] particleStart = null;
        float[] particleTarget = null;

        if (turningOn)
        {
            particleStart = new float[windParticles.Length];
            particleTarget = new float[windParticles.Length];

            for (int i = 0; i < windParticles.Length; i++)
            {
                Object_Wind_Particle windParticle = windParticles[i];

                particleStart[i] = 0f;
                particleTarget[i] = windParticle.BasePowerScale;

                windParticle.powerScale = 0f;
                windParticle.Init();
                windParticle.SetEmissionEnabled(true);
                windParticle.SetParticlesAlpha(1f);
            }

            for (int i = 0; i < winds.Length; i++)
            {
                winds[i].windPower = 0f;
                winds[i].Init();
                // Collider2D.enabled는 GameObject.SetActive와 별개라, 이전에 꺼둔 채로
                // 남아있을 수 있으므로 명시적으로 다시 켠다.
                winds[i].SetColliderEnabled(true);
            }

            obj.SetActive(true);
        }
        else
        {
            // 끄는 순간 즉시 콜라이더부터 비활성화한다 - BlockPlayer()는 windPower를 참조하지
            // 않아서 콜라이더가 켜져 있는 한 페이드와 무관하게 계속 플레이어를 막기 때문이다.
            // 오브젝트 전체를 SetActive(false)하지 않는 이유는 파티클이 함께 있으면 그 순간
            // 렌더링/시뮬레이션이 끊겨 파티클 페이드가 중간에 뚝 끊기기 때문이다.
            for (int i = 0; i < winds.Length; i++)
                winds[i].SetColliderEnabled(false);

            // 끄는 순간 즉시 생성을 멈추고 release한다. release된 파티클은 그 시점 속도 그대로
            // 직진하며 (더 이상 밀리지 않음) 개별적으로 거리 기반 페이드아웃되므로, 이 코루틴이
            // 더 이상 파티클의 powerScale/알파를 프레임마다 건드릴 필요가 없다.
            for (int i = 0; i < windParticles.Length; i++)
            {
                windParticles[i].SetEmissionEnabled(false);
                windParticles[i].Release();
            }
        }

        float timer = 0f;

        while (timer < windFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / windFadeDuration);

            for (int i = 0; i < winds.Length; i++)
            {
                winds[i].windPower = Mathf.Lerp(windStart[i], windTarget[i], t);
                winds[i].Init();
            }

            if (turningOn)
            {
                for (int i = 0; i < windParticles.Length; i++)
                {
                    windParticles[i].powerScale = Mathf.Lerp(particleStart[i], particleTarget[i], t);
                    windParticles[i].Init();
                }
            }

            yield return null;
        }

        for (int i = 0; i < winds.Length; i++)
        {
            winds[i].windPower = windTarget[i];
            winds[i].Init();
        }

        if (turningOn)
        {
            for (int i = 0; i < windParticles.Length; i++)
            {
                windParticles[i].powerScale = particleTarget[i];
                windParticles[i].Init();
            }
        }
        else if (windParticles.Length == 0)
        {
            // 파티클이 없는(리지드바디 바람만 있는) 대상은 기존처럼 페이드 후 비활성화한다.
            // 파티클이 있는 대상은 release된 파티클이 각자 다른 시점에 다 사라질 때까지
            // 렌더링/시뮬레이션이 끊기지 않도록 계속 활성 상태로 둔다.
            obj.SetActive(false);
        }

        windFadeCoroutines.Remove(obj);
    }
}
