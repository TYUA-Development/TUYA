using System.Collections;
using UnityEngine;

public class CoreActivationController : MonoBehaviour
{
    [Header("Hit Detection")]
    [Tooltip("화살 태그 이름. 화살 오브젝트에 Arrow 태그를 달면 가장 안정적입니다.")]
    public string arrowTag = "Arrow";

    [Tooltip("한 번 활성화되면 다시 작동하지 않게 하기")]
    public bool activateOnlyOnce = true;

    [Tooltip("맞은 화살을 제거할지")]
    public bool destroyArrowOnHit = false;

    [Header("Player Lock")]
    [Tooltip("연출 중 플레이어를 완전히 고정하는 스크립트")]
    public PlayerCutsceneLocker2D playerCutsceneLocker;

    [Tooltip("기존 PlayerController 입력 잠금용. CutsceneLocker가 없을 때만 사용됩니다.")]
    public PlayerController playerController;

    [Tooltip("연출 중 플레이어 이동 제한")]
    public bool lockPlayerDuringEvent = true;

    [Tooltip("CutsceneLocker가 없을 때 사용할 예비 입력 잠금 시간")]
    public float playerLockTime = 10f;

    [Header("Letterbox")]
    [Tooltip("이 코어 연출에서 위아래 검정바를 사용할지")]
    public bool useLetterbox = false;

    [Tooltip("검정바 UI 스크립트")]
    public CutsceneLetterboxUI letterboxUI;

    [Tooltip("검정바가 나오는 시간")]
    public float letterboxInTime = 0.45f;

    [Tooltip("검정바가 사라지는 시간")]
    public float letterboxOutTime = 0.45f;

    [Tooltip("비출 돌이나 카메라 포커스가 없을 때, 검정바를 유지할 시간")]
    public float letterboxHoldTimeWithoutCamera = 1.5f;

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
    public float hitFlashAlpha = 1f;

    [Tooltip("활성화 빛 최대 알파")]
    [Range(0f, 1f)]
    public float activateGlowAlpha = 0.85f;

    [Tooltip("완료 후 유지되는 빛 알파")]
    [Range(0f, 1f)]
    public float stableGlowAlpha = 0.45f;

    [Tooltip("활성화 후 원형 빛 회전 속도")]
    public float ringRotateSpeed = 60f;

    [Header("Particles - Core")]
    [Tooltip("화살이 맞는 순간 짧은 파편 / 가루")]
    public ParticleSystem hitParticle;

    [Tooltip("코어가 켜질 때 나오는 입자")]
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

    [Header("Core Self Rise")]
    [Tooltip("코어 자체도 위로 상승한다면 넣기. 아니면 비워두기")]
    public RisingObjectController coreRiseObject;

    [Tooltip("코어 자체 상승을 사용할지")]
    public bool useCoreSelfRise = false;

    [Header("Connected Object")]
    [Tooltip("실제로 올라올 돌 / 길 / 신전 바닥. 반응만 하는 코어라면 비워두세요.")]
    public RisingObjectController connectedRisingObject;

    [Tooltip("올라올 오브젝트를 먼저 카메라로 보여줄지")]
    public bool useCameraFocus = true;

    [Tooltip("카메라 포커스 스크립트")]
    public CoreCameraFocus2D cameraFocus;

    [Header("Camera Focus Timing")]
    [Tooltip("카메라가 올라올 오브젝트 쪽에 머무는 시간")]
    public float cameraHoldTime = 6f;

    [Tooltip("체크하면 카메라가 플레이어에게 돌아올 때까지 기다린 뒤 플레이어를 풀어줍니다.")]
    public bool waitUntilCameraFocusEnds = true;

    [Header("Timing")]
    [Tooltip("히트 섬광 시간")]
    public float hitFlashTime = 0.25f;

    [Tooltip("히트 후 활성화까지 기다리는 시간")]
    public float delayBeforeActivate = 0.25f;

    [Tooltip("활성화 빛이 차오르는 시간")]
    public float activateGlowTime = 1f;

    [Tooltip("활성화 후 카메라가 움직이기 전 대기")]
    public float delayBeforeCameraFocus = 0.35f;

    [Tooltip("카메라가 먼저 올라올 오브젝트를 잡아주는 시간")]
    public float delayBeforeRise = 2.1f;

    [Tooltip("상승 완료 후 완료 사운드까지 대기")]
    public float delayBeforeComplete = 0.6f;

    [Header("State")]
    public bool isActivated;

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

        // A. 히트 확인
        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivate > 0f)
            yield return new WaitForSeconds(delayBeforeActivate);

        // B. 활성화 인식
        PlayAudio(activateAudio);
        PlayParticle(activateParticle);

        yield return StartCoroutine(FadeRendererAlpha(activateGlowRenderer, 0f, activateGlowAlpha, activateGlowTime));
        yield return StartCoroutine(FadeRendererAlpha(stableGlowRenderer, 0f, stableGlowAlpha, 0.25f));

        if (useCoreSelfRise && coreRiseObject != null)
            coreRiseObject.StartRise();

        if (delayBeforeCameraFocus > 0f)
            yield return new WaitForSeconds(delayBeforeCameraFocus);

        // 카메라가 올라올 오브젝트를 자연스럽게 비춤
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

        // D. 연결된 오브젝트 상승
        if (connectedRisingObject != null)
            connectedRisingObject.StartRise();

        if (connectedRisingObject != null)
            yield return new WaitForSeconds(connectedRisingObject.riseDuration + delayBeforeComplete);
        else
            yield return new WaitForSeconds(delayBeforeComplete);

        // E. 완료 표시
        PlayAudio(completeAudio);
        PlayParticle(completeParticle);

        if (startedCameraFocus && waitUntilCameraFocusEnds && cameraFocus != null)
        {
            while (cameraFocus.isFocusing)
            {
                yield return null;
            }
        }

        // 비출 돌이 없어서 카메라 포커스가 시작되지 않은 경우,
        // 검정바가 바로 사라지지 않도록 따로 유지 시간 적용
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
}