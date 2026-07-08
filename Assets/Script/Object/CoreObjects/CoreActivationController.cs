using System.Collections;
using UnityEngine;

public class CoreActivationController : MonoBehaviour, IArrowHit, ICoreEvent
{
    [Header("Hit Detection")]
    public string arrowTag = "Arrow";
    public bool activateOnlyOnce = true;
    public bool destroyArrowOnHit = false;

    [Header("Tutorial Hint")]
    [Tooltip("코어를 맞추라고 알려주는 링 오브젝트")]
    public GameObject coreHintRing;

    [Tooltip("시작할 때 힌트링을 꺼둘지")]
    public bool hideHintRingOnAwake = true;

    [Header("After Letterbox Tutorial")]
    public bool showTutorialAfterLetterbox = false;
    public TutorialAreaPrompt afterLetterboxTutorialPrompt;

    [TextArea(2, 5)]
    public string afterLetterboxTutorialMessage =
        "투야는 바람을 지나갈 수 없습니다.\n화살은 바람의 영향을 받아 궤적이 바뀝니다.";

    [Header("Player Lock")]
    public PlayerCutsceneLocker2D playerCutsceneLocker;
    public PlayerController playerController;
    public bool lockPlayerDuringEvent = true;
    public float playerLockTime = 10f;

    [Header("Letterbox")]
    public bool useLetterbox = false;
    public CutsceneLetterboxUI letterboxUI;
    public float letterboxInTime = 0.45f;
    public float letterboxOutTime = 0.45f;
    public float letterboxHoldTimeWithoutCamera = 1.5f;

    [Header("Visual - Core Circle")]
    public SpriteRenderer hitFlashRenderer;
    public SpriteRenderer activateGlowRenderer;
    public SpriteRenderer stableGlowRenderer;
    public Transform rotatingLightRing;

    [Header("Visual Values")]
    [Range(0f, 1f)] public float hitFlashAlpha = 1f;
    [Range(0f, 1f)] public float activateGlowAlpha = 0.85f;
    [Range(0f, 1f)] public float stableGlowAlpha = 0.45f;
    public float ringRotateSpeed = 60f;

    [Header("Particles - Core")]
    public ParticleSystem hitParticle;
    public ParticleSystem activateParticle;
    public ParticleSystem completeParticle;

    [Header("Audio - Core")]
    public AudioSource hitAudio;
    public AudioSource activateAudio;
    public AudioSource completeAudio;

    [Header("Core Self Rise")]
    public RisingObjectController coreRiseObject;
    public bool useCoreSelfRise = false;

    [Header("Connected Object")]
    public RisingObjectController connectedRisingObject;
    public bool useCameraFocus = true;
    public CoreCameraFocus2D cameraFocus;

    [Header("Camera Focus Timing")]
    public float cameraHoldTime = 6f;
    public bool waitUntilCameraFocusEnds = true;

    [Header("Timing")]
    public float hitFlashTime = 0.25f;
    public float delayBeforeActivate = 0.25f;
    public float activateGlowTime = 1f;
    public float delayBeforeCameraFocus = 0.35f;
    public float delayBeforeRise = 2.1f;
    public float delayBeforeComplete = 0.6f;

    [Header("State")]
    public bool isActivated;
    public bool activationLocked = false;

    [Header("Reset After Activation")]
    [Tooltip("활성화 시퀀스가 끝난 뒤 Awake()가 설정한 초기 비주얼(글로우/플래시 알파, 파티클, 힌트 링)로 되돌릴지")]
    public bool resetVisualsAfterActivation = false;
    public float resetGlowFadeTime = 1f;

    public event System.Action onActivated;

    private bool isRunning;
    private Coroutine activationCoroutine;

    private void Awake()
    {
        if(playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if(playerCutsceneLocker == null)
            playerCutsceneLocker = playerController.gameObject.GetComponent<PlayerCutsceneLocker2D>();

        SetRendererAlpha(hitFlashRenderer, 0f);
        SetRendererAlpha(activateGlowRenderer, 0f);
        SetRendererAlpha(stableGlowRenderer, 0f);

        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);

        if (hideHintRingOnAwake && coreHintRing != null)
            coreHintRing.SetActive(false);
    }

    private void Update()
    {
        if (isActivated && rotatingLightRing != null)
            rotatingLightRing.Rotate(0f, 0f, ringRotateSpeed * Time.deltaTime);
    }

    public void ShowCoreHintRing()
    {
        if (coreHintRing != null && !isActivated)
            coreHintRing.SetActive(true);
    }

    public void HideCoreHintRing()
    {
        if (coreHintRing != null)
            coreHintRing.SetActive(false);
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

        HideCoreHintRing();

        onActivated?.Invoke();

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        activationCoroutine = StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        bool startedCameraFocus = false;

        LockPlayer();

        if (useLetterbox && letterboxUI != null)
            letterboxUI.ShowBars(letterboxInTime);

        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivate > 0f)
            yield return new WaitForSeconds(delayBeforeActivate);

        PlayAudio(activateAudio);
        PlayParticle(activateParticle);

        yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, 0f, activateGlowAlpha, activateGlowTime));
        yield return StartCoroutine(FadeRendererAlpha(stableGlowRenderer, 0f, stableGlowAlpha, 0.25f));

        if (useCoreSelfRise && coreRiseObject != null)
            coreRiseObject.StartRise();

        if (delayBeforeCameraFocus > 0f)
            yield return new WaitForSeconds(delayBeforeCameraFocus);

        if (useCameraFocus && cameraFocus != null && connectedRisingObject != null)
        {
            Transform focusTarget = connectedRisingObject.objectToRise;

            if (focusTarget == null)
                focusTarget = connectedRisingObject.transform;

            cameraFocus.FocusOnTarget(focusTarget, cameraHoldTime);
            startedCameraFocus = true;
        }

        if (delayBeforeRise > 0f)
            yield return new WaitForSeconds(delayBeforeRise);

        if (connectedRisingObject != null)
            connectedRisingObject.StartRise();

        if (connectedRisingObject != null)
            yield return new WaitForSeconds(connectedRisingObject.riseDuration + delayBeforeComplete);
        else
            yield return new WaitForSeconds(delayBeforeComplete);

        PlayAudio(completeAudio);
        PlayParticle(completeParticle);

        if (startedCameraFocus && waitUntilCameraFocusEnds && cameraFocus != null)
        {
            while (cameraFocus.isFocusing)
                yield return null;
        }

        if (!startedCameraFocus && useLetterbox && letterboxHoldTimeWithoutCamera > 0f)
            yield return new WaitForSeconds(letterboxHoldTimeWithoutCamera);

        if (useLetterbox && letterboxUI != null)
        {
            letterboxUI.HideBars(letterboxOutTime);

            if (letterboxOutTime > 0f)
                yield return new WaitForSeconds(letterboxOutTime);
        }

        UnlockPlayer();

        ShowAfterLetterboxTutorial();

        if (resetVisualsAfterActivation)
            yield return StartCoroutine(ResetVisualsRoutine());

        isRunning = false;
        activationCoroutine = null;
    }

    private IEnumerator ResetVisualsRoutine()
    {
        if (activateGlowRenderer != null)
            yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, activateGlowRenderer.color.a, 0f, resetGlowFadeTime));

        SetRendererAlpha(hitFlashRenderer, 0f);
        SetRendererAlpha(stableGlowRenderer, 0f);

        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);

        if (hideHintRingOnAwake && coreHintRing != null)
            coreHintRing.SetActive(false);
    }

    private void ShowAfterLetterboxTutorial()
    {
        if (!showTutorialAfterLetterbox)
            return;

        if (afterLetterboxTutorialPrompt == null)
            return;

        afterLetterboxTutorialPrompt.hasShown = false;
        afterLetterboxTutorialPrompt.isShowing = false;
        afterLetterboxTutorialPrompt.tutorialMessage = afterLetterboxTutorialMessage;
        afterLetterboxTutorialPrompt.ShowPrompt();
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

    public void OnCoreEvent()
    {
        StartActivation();
    }
}