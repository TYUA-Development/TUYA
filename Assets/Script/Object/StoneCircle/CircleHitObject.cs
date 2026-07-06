using System.Collections;
using UnityEngine;

public class CircleHitObject : MonoBehaviour, IArrowHit
{
    [Header("Existing Stone Circle System")]
    public StoneCircleManager manager;
    private int triggerId;

    [Header("Special Machine Activation")]
    public WindMachineActivationController activationController;

    [Header("Hit Option")]
    public bool activateOnlyOnce = true;

    [Header("Visual - Core Style")]
    public SpriteRenderer hitFlashRenderer;
    public SpriteRenderer activateGlowRenderer;
    public SpriteRenderer stableGlowRenderer;

    [Header("Visual Values")]
    [Range(0f, 1f)] public float hitFlashAlpha = 1f;
    [Range(0f, 1f)] public float activateGlowAlpha = 0.85f;
    [Range(0f, 1f)] public float stableGlowAlpha = 0.45f;
    public float hitFlashTime = 0.25f;
    public float delayBeforeActivateEffect = 0.25f;
    public float activateGlowTime = 1f;
    public float delayBeforeCompleteEffect = 0.6f;

    [Header("Particles - Core Style")]
    public ParticleSystem hitParticle;
    public ParticleSystem activateParticle;
    public ParticleSystem completeParticle;

    [Header("Audio - Core Style")]
    public AudioSource hitAudio;
    public AudioSource activateAudio;
    public AudioSource completeAudio;

    private bool activated = false;
    private Coroutine effectCoroutine;

    private void Awake()
    {
        SetRendererAlpha(hitFlashRenderer, 0f);
        SetRendererAlpha(activateGlowRenderer, 0f);
        SetRendererAlpha(stableGlowRenderer, 0f);

        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);
    }

    public void Init(StoneCircleManager manager, int triggerId)
    {
        this.manager = manager;
        this.triggerId = triggerId;
    }

    public void Reset()
    {
        activated = false;
    }

    public void OnHit()
    {
        if (activateOnlyOnce && activated)
            return;

        activated = true;
        PlayCoreStyleEffect();

        // �� ��� ���� �Ŵ����� ����Ǿ� ������ �װ� �켱 ����
        if (activationController != null)
        {
            activationController.Activate();
            return;
        }

        // ���� �� �Ǿ� ������ ���� ��� ����
        if (manager != null)
        {
            manager.RotateCircles(triggerId);
        }
    }

    private void PlayCoreStyleEffect()
    {
        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);

        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(CoreStyleEffectRoutine());
    }

    private IEnumerator CoreStyleEffectRoutine()
    {
        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivateEffect > 0f)
            yield return new WaitForSeconds(delayBeforeActivateEffect);

        PlayAudio(activateAudio);
        PlayParticle(activateParticle);

        yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, 0f, activateGlowAlpha, activateGlowTime));
        yield return StartCoroutine(FadeRendererAlpha(stableGlowRenderer, 0f, stableGlowAlpha, 0.25f));

        if (delayBeforeCompleteEffect > 0f)
            yield return new WaitForSeconds(delayBeforeCompleteEffect);

        PlayAudio(completeAudio);
        PlayParticle(completeParticle);

        effectCoroutine = null;
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
            SetRendererAlpha(renderer, Mathf.Lerp(fromAlpha, toAlpha, t));
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
