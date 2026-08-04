using System.Collections;
using UnityEngine;

public class CoreActivation : MonoBehaviour, IArrowHit, ICoreEvent
{
    [Header("Hit Detection")]
    public string arrowTag = "Arrow";
    public bool activateOnlyOnce = true;
    public bool destroyArrowOnHit = false;

    [Header("Player Lock")]
    public PlayerCutsceneLocker2D playerCutsceneLocker;
    public PlayerController playerController;
    public bool lockPlayerDuringEvent = true;
    public float playerLockTime = 10f;

    [Header("Visual - Core Circle")]
    public SpriteRenderer hitFlashRenderer;
    public SpriteRenderer activateGlowRenderer;
    public SpriteRenderer stableGlowRenderer;

    [Header("Visual Values")]
    [Range(0f, 1f)] public float hitFlashAlpha = 1f;
    [Range(0f, 1f)] public float activateGlowAlpha = 0.85f;
    [Range(0f, 1f)] public float stableGlowAlpha = 0.45f;

    [Header("Particles - Core")]
    public ParticleSystem hitParticle;
    public ParticleSystem activateParticle;

    [Header("Audio - Core")]
    public AudioSource hitAudio;
    public AudioSource activateAudio;

    [Header("Timing")]
    public float hitFlashTime = 0.25f;
    public float delayBeforeActivate = 0.25f;
    public float activateGlowTime = 1f;
    public float activateGlowFadeOutTime = 1f;

    [Header("State")]
    public bool isActivated;
    public bool activationLocked = false;

    public event System.Action onActivated;

    private bool isRunning;
    private Coroutine activationCoroutine;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if (playerCutsceneLocker == null && playerController != null)
            playerCutsceneLocker = playerController.gameObject.GetComponent<PlayerCutsceneLocker2D>();

        SetRendererAlpha(hitFlashRenderer, 0f);
        SetRendererAlpha(activateGlowRenderer, 0f);
        SetRendererAlpha(stableGlowRenderer, 0f);

        StopParticle(hitParticle);
        StopParticle(activateParticle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryActivateByObject(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryActivateByObject(collision.gameObject);
    }

    private void TryActivateByObject(GameObject hitObject)
    {
        if (hitObject == null)
            return;

        if (activationLocked)
            return;

        if (activateOnlyOnce && isActivated)
            return;

        bool isArrow = false;

        if (!string.IsNullOrEmpty(arrowTag) && hitObject.CompareTag(arrowTag))
            isArrow = true;

        if (hitObject.GetComponent<Arrow>() != null)
            isArrow = true;

        if (hitObject.GetComponentInParent<Arrow>() != null)
            isArrow = true;

        if (!isArrow)
            return;

        if (destroyArrowOnHit)
            Destroy(hitObject);

        StartActivation();
    }

    public void StartActivation()
    {
        if (activationLocked)
            return;

        if (isRunning)
            return;

        if (activateOnlyOnce && isActivated)
            return;

        isActivated = true;
        isRunning = true;

        onActivated?.Invoke();

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        activationCoroutine = StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        LockPlayer();

        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivate > 0f)
            yield return new WaitForSeconds(delayBeforeActivate);

        PlayAudio(activateAudio);
        PlayParticle(activateParticle);

        yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, 0f, activateGlowAlpha, activateGlowTime));
        yield return StartCoroutine(FadeRendererAlpha(stableGlowRenderer, 0f, stableGlowAlpha, 0.25f));
        yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, activateGlowAlpha, 0f, activateGlowFadeOutTime));

        UnlockPlayer();

        isRunning = false;
        activationCoroutine = null;
    }

    private void LockPlayer()
    {
        if (!lockPlayerDuringEvent)
            return;

        if (playerCutsceneLocker != null)
            playerCutsceneLocker.LockNow();
        else if (playerController != null)
            playerController.LockPlayerInput(playerLockTime);
    }

    private void UnlockPlayer()
    {
        if (!lockPlayerDuringEvent)
            return;

        if (playerCutsceneLocker != null)
            playerCutsceneLocker.UnlockNow();
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

    public void OnHit()
    {
        OnCoreEvent();
    }

    public void OnCoreEvent(bool isPressed = true)
    {
        StartActivation();
    }
}
