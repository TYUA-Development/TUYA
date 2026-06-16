using System.Collections;
using UnityEngine;

public class CoreActivationController : MonoBehaviour, IArrowHit, ICoreEvent
{
    [Header("Hit Detection")]
    [Tooltip("ȭ�� �±� �̸�. ȭ�� ������Ʈ�� Arrow �±׸� �޸� ���� �������Դϴ�.")]
    public string arrowTag = "Arrow";

    [Tooltip("�� �� Ȱ��ȭ�Ǹ� �ٽ� �۵����� �ʰ� �ϱ�")]
    public bool activateOnlyOnce = true;

    [Tooltip("���� ȭ���� ��������")]
    public bool destroyArrowOnHit = false;

    [Header("Player Lock")]
    [Tooltip("���� �� �÷��̾ ������ �����ϴ� ��ũ��Ʈ")]
    public PlayerCutsceneLocker2D playerCutsceneLocker;

    [Tooltip("���� PlayerController �Է� ��ݿ�. CutsceneLocker�� ���� ���� ���˴ϴ�.")]
    public PlayerController playerController;

    [Tooltip("���� �� �÷��̾� �̵� ����")]
    public bool lockPlayerDuringEvent = true;

    [Tooltip("CutsceneLocker�� ���� �� ����� ���� �Է� ��� �ð�")]
    public float playerLockTime = 10f;

    [Header("Letterbox")]
    [Tooltip("�� �ھ� ���⿡�� ���Ʒ� �����ٸ� �������")]
    public bool useLetterbox = false;

    [Tooltip("������ UI ��ũ��Ʈ")]
    public CutsceneLetterboxUI letterboxUI;

    [Tooltip("�����ٰ� ������ �ð�")]
    public float letterboxInTime = 0.45f;

    [Tooltip("�����ٰ� ������� �ð�")]
    public float letterboxOutTime = 0.45f;

    [Tooltip("���� ���̳� ī�޶� ��Ŀ���� ���� ��, �����ٸ� ������ �ð�")]
    public float letterboxHoldTimeWithoutCamera = 1.5f;

    [Header("Visual - Core Circle")]
    [Tooltip("��Ʈ ���� ª�� ��½�̴� ��")]
    public SpriteRenderer hitFlashRenderer;

    [Tooltip("Ȱ��ȭ�� �� ���� �������� ��")]
    public SpriteRenderer activateGlowRenderer;

    [Tooltip("Ȱ��ȭ �� ��� �����ִ� ������ ��")]
    public SpriteRenderer stableGlowRenderer;

    [Tooltip("���� ���� ���� ������Ʈ")]
    public Transform rotatingLightRing;

    [Header("Visual Values")]
    [Tooltip("��Ʈ ���� �ִ� ����")]
    [Range(0f, 1f)]
    public float hitFlashAlpha = 1f;

    [Tooltip("Ȱ��ȭ �� �ִ� ����")]
    [Range(0f, 1f)]
    public float activateGlowAlpha = 0.85f;

    [Tooltip("�Ϸ� �� �����Ǵ� �� ����")]
    [Range(0f, 1f)]
    public float stableGlowAlpha = 0.45f;

    [Tooltip("Ȱ��ȭ �� ���� �� ȸ�� �ӵ�")]
    public float ringRotateSpeed = 60f;

    [Header("Particles - Core")]
    [Tooltip("ȭ���� �´� ���� ª�� ���� / ����")]
    public ParticleSystem hitParticle;

    [Tooltip("�ھ ���� �� ������ ����")]
    public ParticleSystem activateParticle;

    [Tooltip("�ھ� �Ϸ� ����")]
    public ParticleSystem completeParticle;

    [Header("Audio - Core")]
    [Tooltip("ȭ���� �ھ �´� ����")]
    public AudioSource hitAudio;

    [Tooltip("�ھ ������ �Ҹ�")]
    public AudioSource activateAudio;

    [Tooltip("�Ϸ� ������")]
    public AudioSource completeAudio;

    [Header("Core Self Rise")]
    [Tooltip("�ھ� ��ü�� ���� ����Ѵٸ� �ֱ�. �ƴϸ� ����α�")]
    public RisingObjectController coreRiseObject;

    [Tooltip("�ھ� ��ü ����� �������")]
    public bool useCoreSelfRise = false;

    [Header("Connected Object")]
    [Tooltip("������ �ö�� �� / �� / ���� �ٴ�. ������ �ϴ� �ھ��� ����μ���.")]
    public RisingObjectController connectedRisingObject;

    [Tooltip("�ö�� ������Ʈ�� ���� ī�޶�� ��������")]
    public bool useCameraFocus = true;

    [Tooltip("ī�޶� ��Ŀ�� ��ũ��Ʈ")]
    public CoreCameraFocus2D cameraFocus;

    [Header("Camera Focus Timing")]
    [Tooltip("ī�޶� �ö�� ������Ʈ �ʿ� �ӹ��� �ð�")]
    public float cameraHoldTime = 6f;

    [Tooltip("üũ�ϸ� ī�޶� �÷��̾�� ���ƿ� ������ ��ٸ� �� �÷��̾ Ǯ���ݴϴ�.")]
    public bool waitUntilCameraFocusEnds = true;

    [Header("Timing")]
    [Tooltip("��Ʈ ���� �ð�")]
    public float hitFlashTime = 0.25f;

    [Tooltip("��Ʈ �� Ȱ��ȭ���� ��ٸ��� �ð�")]
    public float delayBeforeActivate = 0.25f;

    [Tooltip("Ȱ��ȭ ���� �������� �ð�")]
    public float activateGlowTime = 1f;

    [Tooltip("Ȱ��ȭ �� ī�޶� �����̱� �� ���")]
    public float delayBeforeCameraFocus = 0.35f;

    [Tooltip("ī�޶� ���� �ö�� ������Ʈ�� ����ִ� �ð�")]
    public float delayBeforeRise = 2.1f;

    [Tooltip("��� �Ϸ� �� �Ϸ� ������� ���")]
    public float delayBeforeComplete = 0.6f;

    [Header("State")]
    public bool isActivated;

    public event System.Action onActivated;

    private bool isRunning;
    private Coroutine activationCoroutine;

    private void Awake()
    {
        SetRendererAlpha(hitFlashRenderer, 0f);
        SetRendererAlpha(activateGlowRenderer, 0f);
        SetRendererAlpha(stableGlowRenderer, 0f);

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
        bool startedCameraFocus = false;

        LockPlayer();

        if (useLetterbox && letterboxUI != null)
            letterboxUI.ShowBars(letterboxInTime);

        // A. ��Ʈ Ȯ��
        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivate > 0f)
            yield return new WaitForSeconds(delayBeforeActivate);

        // B. Ȱ��ȭ �ν�
        PlayAudio(activateAudio);
        PlayParticle(activateParticle);

        yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, 0f, activateGlowAlpha, activateGlowTime));
        yield return StartCoroutine(FadeRendererAlpha(stableGlowRenderer, 0f, stableGlowAlpha, 0.25f));

        if (useCoreSelfRise && coreRiseObject != null)
            coreRiseObject.StartRise();

        if (delayBeforeCameraFocus > 0f)
            yield return new WaitForSeconds(delayBeforeCameraFocus);

        // ī�޶� �ö�� ������Ʈ�� �ڿ������� ����
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

        // D. ����� ������Ʈ ���
        if (connectedRisingObject != null)
            connectedRisingObject.StartRise();

        if (connectedRisingObject != null)
            yield return new WaitForSeconds(connectedRisingObject.riseDuration + delayBeforeComplete);
        else
            yield return new WaitForSeconds(delayBeforeComplete);

        // E. �Ϸ� ǥ��
        PlayAudio(completeAudio);
        PlayParticle(completeParticle);

        if (startedCameraFocus && waitUntilCameraFocusEnds && cameraFocus != null)
        {
            while (cameraFocus.isFocusing)
            {
                yield return null;
            }
        }

        // ���� ���� ��� ī�޶� ��Ŀ���� ���۵��� ���� ���,
        // �����ٰ� �ٷ� ������� �ʵ��� ���� ���� �ð� ����
        if (!startedCameraFocus && useLetterbox && letterboxHoldTimeWithoutCamera > 0f)
        {
            yield return new WaitForSeconds(letterboxHoldTimeWithoutCamera);
        }

        if (useLetterbox && letterboxUI != null)
            letterboxUI.HideBars(letterboxOutTime);

        UnlockPlayer();

        isRunning = false;
        activationCoroutine = null;
    }

    private void LockPlayer()
    {
        if (!lockPlayerDuringEvent)
            return;

        if (playerCutsceneLocker != null)
        {
            playerCutsceneLocker.LockNow();
        }
        else if (playerController != null)
        {
            playerController.LockPlayerInput(playerLockTime);
        }
    }

    private void UnlockPlayer()
    {
        if (!lockPlayerDuringEvent)
            return;

        if (playerCutsceneLocker != null)
        {
            playerCutsceneLocker.UnlockNow();
        }
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