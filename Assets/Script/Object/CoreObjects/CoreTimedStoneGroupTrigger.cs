using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreTimedStoneGroupTrigger : MonoBehaviour, IArrowHit
{
    [Header("Target Rising Objects")]
    public List<TimedRisingObjectController> targetObjects = new List<TimedRisingObjectController>();

    [Header("Trigger Settings")]
    [Tooltip("코어 맞은 뒤 돌 그룹이 작동하기 전 대기. 바로 올라오게 하려면 0")]
    public float groupStartDelay = 0f;

    [Tooltip("한 번만 작동시키려면 체크")]
    public bool activateOnlyOnce = false;

    [Tooltip("돌이 움직이는 중에는 다시 맞아도 무시")]
    public bool ignoreHitsWhileRunning = true;

    [Header("Visual - Core Circle")]
    [Tooltip("히트 순간 짧게 번쩍이는 원")]
    public SpriteRenderer hitFlashRenderer;

    [Tooltip("활성화될 때 빛이 차오르는 원")]
    public SpriteRenderer activateGlowRenderer;

    [Tooltip("활성화 후 계속 남아있는 은은한 빛")]
    public SpriteRenderer stableGlowRenderer;

    [Tooltip("빛이 도는 원형 오브젝트")]
    public Transform rotatingLightRing;

    [Header("Visual Values")]
    [Tooltip("히트 섬광 최대 알파")]
    [Range(0f, 1f)]
    public float hitFlashAlpha = 0.25f;

    [Tooltip("활성화 빛 최대 알파")]
    [Range(0f, 1f)]
    public float activateGlowAlpha = 0.75f;

    [Tooltip("완료 후 유지되는 빛 알파")]
    [Range(0f, 1f)]
    public float stableGlowAlpha = 0.45f;

    [Tooltip("활성화 후 원형 빛 회전 속도")]
    public float ringRotateSpeed = 60f;

    [Header("Particles - Core")]
    [Tooltip("화살이 맞는 순간 짧은 파편 / 반짝이")]
    public ParticleSystem hitParticle;

    [Tooltip("코어가 켜질 때 터지는 빛 파티클")]
    public ParticleSystem activateParticle;

    [Tooltip("코어 완료 입자")]
    public ParticleSystem completeParticle;

    [Header("Audio - Core")]
    [Tooltip("화살이 코어에 맞는 순간")]
    public AudioSource hitAudio;

    [Tooltip("코어가 켜지는 소리")]
    public AudioSource activateAudio;

    [Tooltip("완료 공명음")]
    public AudioSource completeAudio;

    [Header("Timing")]
    [Tooltip("히트 섬광 시간")]
    public float hitFlashTime = 0.3f;

    [Tooltip("히트 후 활성화 이펙트까지 기다리는 시간")]
    public float delayBeforeActivate = 0.02f;

    [Tooltip("활성화 빛이 차오르는 시간")]
    public float activateGlowTime = 2f;

    [Tooltip("완료 이펙트까지 기다리는 시간")]
    public float delayBeforeComplete = 0.2f;

    [Header("Repeat Settings")]
    [Tooltip("반복 작동하는 코어라면, 맞을 때마다 활성화 빛을 다시 0에서 시작합니다.")]
    public bool resetGlowOnEveryHit = true;

    [Tooltip("반복 작동하는 코어라면, 돌 작동이 끝난 뒤 활성화 빛을 다시 꺼줍니다.")]
    public bool fadeOutGlowAfterSequence = false;

    [Tooltip("활성화 빛이 꺼지는 시간")]
    public float glowFadeOutTime = 0.4f;

    [Header("State")]
    public bool hasActivatedOnce;
    public bool isRunning;
    public bool isActivated;

    private Coroutine triggerCoroutine;
    private Coroutine coreEffectCoroutine;

    private void Awake()
    {
        SetRendererAlpha(hitFlashRenderer, 0f);

        if (resetGlowOnEveryHit)
        {
            SetRendererAlpha(activateGlowRenderer, 0f);
            SetRendererAlpha(stableGlowRenderer, 0f);
        }

        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);
    }

    private void Update()
    {
        if (isActivated && rotatingLightRing != null)
        {
            rotatingLightRing.Rotate(0f, 0f, ringRotateSpeed * Time.deltaTime);
        }
    }

    public void OnHit()
    {
        if (activateOnlyOnce && hasActivatedOnce)
            return;

        if (ignoreHitsWhileRunning && isRunning)
            return;

        if (triggerCoroutine != null)
            StopCoroutine(triggerCoroutine);

        triggerCoroutine = StartCoroutine(TriggerRoutine());
    }

    private IEnumerator TriggerRoutine()
    {
        isRunning = true;
        isActivated = true;
        hasActivatedOnce = true;

        if (resetGlowOnEveryHit)
        {
            SetRendererAlpha(hitFlashRenderer, 0f);
            SetRendererAlpha(activateGlowRenderer, 0f);
            SetRendererAlpha(stableGlowRenderer, 0f);
        }

        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);

        // 코어 이펙트는 따로 재생. 돌 상승을 막지 않음.
        if (coreEffectCoroutine != null)
            StopCoroutine(coreEffectCoroutine);

        coreEffectCoroutine = StartCoroutine(CoreEffectRoutine());

        // 돌은 바로 또는 groupStartDelay 후 작동
        if (groupStartDelay > 0f)
            yield return new WaitForSeconds(groupStartDelay);

        for (int i = 0; i < targetObjects.Count; i++)
        {
            if (targetObjects[i] == null)
                continue;

            targetObjects[i].TriggerRiseAndLower();
        }

        // 모든 돌이 끝날 때까지 대기
        bool anyMoving = true;

        while (anyMoving)
        {
            anyMoving = false;

            for (int i = 0; i < targetObjects.Count; i++)
            {
                if (targetObjects[i] == null)
                    continue;

                if (targetObjects[i].isMoving)
                {
                    anyMoving = true;
                    break;
                }
            }

            yield return null;
        }

        if (delayBeforeComplete > 0f)
            yield return new WaitForSeconds(delayBeforeComplete);

        PlayAudio(completeAudio);
        PlayParticle(completeParticle);

        if (fadeOutGlowAfterSequence)
        {
            yield return StartCoroutine(FadeRendererAlpha(
                activateGlowRenderer,
                activateGlowAlpha,
                0f,
                glowFadeOutTime
            ));

            yield return StartCoroutine(FadeRendererAlpha(
                stableGlowRenderer,
                stableGlowAlpha,
                0f,
                glowFadeOutTime
            ));

            isActivated = false;
        }

        isRunning = false;
        triggerCoroutine = null;
    }

    private IEnumerator CoreEffectRoutine()
    {
        // A. 히트 이펙트 / 사운드
        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivate > 0f)
            yield return new WaitForSeconds(delayBeforeActivate);

        // B. 활성화 이펙트 / 사운드
        PlayAudio(activateAudio);
        PlayParticle(activateParticle);

        yield return StartCoroutine(FadeRendererAlpha(
            activateGlowRenderer,
            0f,
            activateGlowAlpha,
            activateGlowTime
        ));

        yield return StartCoroutine(FadeRendererAlpha(
            stableGlowRenderer,
            0f,
            stableGlowAlpha,
            0.25f
        ));

        coreEffectCoroutine = null;
    }

    private IEnumerator FlashRenderer(SpriteRenderer renderer, float maxAlpha, float duration)
    {
        if (renderer == null)
            yield break;

        float half = duration * 0.5f;

        yield return StartCoroutine(FadeRendererAlpha(renderer, 0f, maxAlpha, half));
        yield return StartCoroutine(FadeRendererAlpha(renderer, maxAlpha, 0f, half));
    }

    private IEnumerator FadeRendererAlpha(SpriteRenderer renderer, float fromAlpha, float toAlpha, float duration)
    {
        if (renderer == null)
            yield break;

        if (duration <= 0f)
        {
            SetRendererAlpha(renderer, toAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            SetRendererAlpha(renderer, alpha);

            yield return null;
        }

        SetRendererAlpha(renderer, toAlpha);
    }

    private void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
            return;

        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }

    private void StopParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void PlayAudio(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.Play();
    }
}