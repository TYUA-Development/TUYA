using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreObjectToggle : MonoBehaviour
{
    [Header("Core Objects")]
    [Tooltip("어떤 코어를 맞춰도 동일하게 동작합니다. 여러 번 맞출 수 있는지 여부는 각 CoreActivationController의 activateOnlyOnce 값을 따릅니다.")]
    public List<CoreActivationController> coreObjects;

    [Header("Toggle Targets")]
    [Tooltip("코어가 활성화될 때마다 각 오브젝트의 활성 상태를 개별적으로 반전시킨다 (켜져있으면 끄고, 꺼져있으면 켠다)")]
    public List<GameObject> targetObjects;

    [Header("Wind Fade")]
    [Tooltip("대상에 Object_Wind/Object_Wind_Particle이 자식으로 있으면 즉시 켜고 끄는 대신 WindPower를 이 시간(초) 동안 서서히 올리고 내린다.")]
    public float windFadeDuration = 1f;

    [Tooltip("끌 때, WindPower가 0이 된 직후 바로 비활성화하지 않고 이 시간(초) 동안 파티클을 서서히 투명하게 만든 뒤 비활성화한다.")]
    public float particleFadeOutDuration = 1f;

    private readonly Dictionary<Object_Wind, float> originalWindPower = new Dictionary<Object_Wind, float>();
    private readonly Dictionary<Object_Wind_Particle, float> originalWindParticlePower = new Dictionary<Object_Wind_Particle, float>();
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

            Object_Wind[] winds = obj.GetComponentsInChildren<Object_Wind>(true);
            Object_Wind_Particle[] windParticles = obj.GetComponentsInChildren<Object_Wind_Particle>(true);

            if (winds.Length == 0 && windParticles.Length == 0)
            {
                obj.SetActive(!obj.activeSelf);
                continue;
            }

            if (windFadeCoroutines.TryGetValue(obj, out var running) && running != null)
                StopCoroutine(running);

            windFadeCoroutines[obj] = StartCoroutine(FadeWindAndToggle(obj, winds, windParticles, !obj.activeSelf));
        }
    }

    private IEnumerator FadeWindAndToggle(GameObject obj, Object_Wind[] winds, Object_Wind_Particle[] windParticles, bool turningOn)
    {
        float[] windStart = new float[winds.Length];
        float[] windTarget = new float[winds.Length];

        for (int i = 0; i < winds.Length; i++)
        {
            Object_Wind wind = winds[i];

            if (!originalWindPower.ContainsKey(wind))
                originalWindPower[wind] = wind.windPower;

            windStart[i] = turningOn ? 0f : wind.windPower;
            windTarget[i] = turningOn ? originalWindPower[wind] : 0f;
        }

        float[] particleStart = new float[windParticles.Length];
        float[] particleTarget = new float[windParticles.Length];

        for (int i = 0; i < windParticles.Length; i++)
        {
            Object_Wind_Particle windParticle = windParticles[i];

            if (!originalWindParticlePower.ContainsKey(windParticle))
                originalWindParticlePower[windParticle] = windParticle.windPower;

            particleStart[i] = turningOn ? 0f : windParticle.windPower;
            particleTarget[i] = turningOn ? originalWindParticlePower[windParticle] : 0f;
        }

        if (turningOn)
        {
            for (int i = 0; i < winds.Length; i++)
            {
                winds[i].windPower = 0f;
                winds[i].Init();
            }

            for (int i = 0; i < windParticles.Length; i++)
            {
                windParticles[i].windPower = 0f;
                windParticles[i].Init();
                windParticles[i].SetEmissionEnabled(true);
                windParticles[i].SetParticlesAlpha(1f);
            }

            obj.SetActive(true);
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

            for (int i = 0; i < windParticles.Length; i++)
            {
                windParticles[i].windPower = Mathf.Lerp(particleStart[i], particleTarget[i], t);
                windParticles[i].Init();
            }

            yield return null;
        }

        for (int i = 0; i < winds.Length; i++)
        {
            winds[i].windPower = windTarget[i];
            winds[i].Init();
        }

        for (int i = 0; i < windParticles.Length; i++)
        {
            windParticles[i].windPower = particleTarget[i];
            windParticles[i].Init();
        }

        if (!turningOn)
        {
            for (int i = 0; i < windParticles.Length; i++)
                windParticles[i].SetEmissionEnabled(false);

            float fadeTimer = 0f;

            while (fadeTimer < particleFadeOutDuration)
            {
                fadeTimer += Time.deltaTime;
                float fadeT = Mathf.Clamp01(fadeTimer / particleFadeOutDuration);
                float fadeAlpha = Mathf.Lerp(1f, 0f, fadeT);

                for (int i = 0; i < windParticles.Length; i++)
                    windParticles[i].SetParticlesAlpha(fadeAlpha);

                yield return null;
            }

            for (int i = 0; i < windParticles.Length; i++)
                windParticles[i].StopAndClearParticles();

            obj.SetActive(false);
        }

        windFadeCoroutines.Remove(obj);
    }
}
