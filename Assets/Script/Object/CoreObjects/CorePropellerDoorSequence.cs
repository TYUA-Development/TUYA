using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CorePropellerDoorSequence : MonoBehaviour, IArrowHit
{
    [Header("Trigger Settings")]
    [Tooltip("한 번만 작동시키려면 체크")]
    public bool activateOnlyOnce = true;

    [Tooltip("작동 중에는 다시 맞아도 무시")]
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

    [Header("Timing - Core")]
    [Tooltip("히트 섬광 시간")]
    public float hitFlashTime = 0.3f;

    [Tooltip("히트 후 활성화 이펙트까지 기다리는 시간")]
    public float delayBeforeActivate = 0.02f;

    [Tooltip("활성화 빛이 차오르는 시간")]
    public float activateGlowTime = 1.2f;

    [Tooltip("완료 이펙트까지 기다리는 시간")]
    public float delayBeforeComplete = 0.2f;

    [Header("Propeller Reveal")]
    [Tooltip("코어 뒤쪽 큰 프로펠러 회전 중심")]
    public Transform propellerPivot;

    [Tooltip("프로펠러 기본 스프라이트들. 처음엔 안 보이다가 드러납니다.")]
    public SpriteRenderer[] propellerBaseRenderers;

    [Tooltip("프로펠러 위에 겹칠 푸른 Glow 스프라이트들")]
    public SpriteRenderer[] propellerGlowRenderers;

    [Tooltip("프로펠러가 드러나기 전 대기 시간")]
    public float delayBeforePropellerReveal = 0.25f;

    [Tooltip("프로펠러 기본 스프라이트가 드러나는 시간")]
    public float propellerRevealTime = 0.8f;

    [Tooltip("프로펠러 기본 스프라이트 최종 알파")]
    [Range(0f, 1f)]
    public float propellerBaseAlpha = 0.9f;

    [Tooltip("프로펠러 푸른 Glow가 드러나는 시간")]
    public float propellerGlowFadeTime = 0.8f;

    [Tooltip("프로펠러 푸른 Glow 최종 알파")]
    [Range(0f, 1f)]
    public float propellerGlowAlpha = 0.85f;

    [Tooltip("프로펠러가 드러날 때 터지는 파란 입자")]
    public ParticleSystem propellerRevealParticle;

    [Tooltip("방 전체에 남는 푸른 먼지 입자")]
    public ParticleSystem roomBlueDustParticle;

    [Tooltip("프로펠러가 드러나는 소리")]
    public AudioSource propellerRevealAudio;

    [Header("Propeller Rotation")]
    [Tooltip("체크하면 시계 방향으로 회전")]
    public bool spinClockwise = true;

    [Tooltip("프로펠러 시작 회전 속도")]
    public float propellerStartSpeed = 10f;

    [Tooltip("프로펠러가 가속될 때 목표 속도")]
    public float propellerTargetSpeed = 180f;

    [Tooltip("프로펠러가 목표 속도까지 올라가는 시간")]
    public float propellerSpinUpTime = 1.1f;

    [Tooltip("가속 후 유지 회전 속도")]
    public float propellerStableSpeed = 130f;

    [Tooltip("프로펠러 회전 루프 사운드")]
    public AudioSource propellerSpinLoopAudio;

    [Header("Door Open")]
    [Tooltip("앞쪽 문 Transform. 직접 움직일 문 오브젝트를 넣으세요.")]
    public Transform doorTransform;

    [Tooltip("문을 직접 이동시킬지")]
    public bool moveDoorDirectly = true;

    [Tooltip("문이 열릴 때 로컬 위치로 얼마나 이동할지. 위로 열리면 Y 양수, 옆으로 열리면 X 사용")]
    public Vector3 doorOpenLocalOffset = new Vector3(0f, 3f, 0f);

    [Tooltip("문이 열리는 시간")]
    public float doorOpenTime = 1.2f;

    [Tooltip("프로펠러 드러난 뒤 문이 열리기 전 대기 시간")]
    public float delayBeforeDoorOpen = 0.7f;

    [Tooltip("문이 열릴 때 나는 소리")]
    public AudioSource doorOpenAudio;

    [Tooltip("문 열림 시작 때 실행할 이벤트")]
    public UnityEvent onDoorOpenStart;

    [Tooltip("문 열림 완료 때 실행할 이벤트")]
    public UnityEvent onDoorOpenComplete;

    [Header("Optional Message Targets")]
    [Tooltip("기존 문 열기 스크립트를 호출하고 싶으면 여기에 오브젝트를 넣으세요.")]
    public MonoBehaviour[] messageTargets;

    [Tooltip("Message Targets에 보낼 함수 이름. 예: OnCoreEvent, OpenDoor, Activate")]
    public string messageName = "OnCoreEvent";

    [Header("Repeat Settings")]
    [Tooltip("반복 작동하는 코어라면, 맞을 때마다 활성화 빛을 다시 0에서 시작합니다.")]
    public bool resetGlowOnEveryHit = true;

    [Tooltip("반복 작동하는 코어라면, 시퀀스 끝난 뒤 활성화 빛을 다시 꺼줍니다.")]
    public bool fadeOutGlowAfterSequence = false;

    [Tooltip("활성화 빛이 꺼지는 시간")]
    public float glowFadeOutTime = 0.4f;

    [Header("State")]
    public bool hasActivatedOnce;
    public bool isRunning;
    public bool isActivated;
    public bool isPropellerSpinning;
    public bool isDoorOpened;

    private Coroutine triggerCoroutine;
    private Coroutine coreEffectCoroutine;
    private Coroutine doorCoroutine;
    private Coroutine propellerSpinCoroutine;

    private float currentPropellerSpeed;

    private Vector3 doorClosedLocalPosition;
    private Vector3 doorOpenLocalPosition;
    private bool doorPositionCached;

    private void Awake()
    {
        CacheDoorPosition();

        SetRendererAlpha(hitFlashRenderer, 0f);

        if (resetGlowOnEveryHit)
        {
            SetRendererAlpha(activateGlowRenderer, 0f);
            SetRendererAlpha(stableGlowRenderer, 0f);
        }

        SetRendererArrayAlpha(propellerBaseRenderers, 0f);
        SetRendererArrayAlpha(propellerGlowRenderers, 0f);

        StopParticle(hitParticle);
        StopParticle(activateParticle);
        StopParticle(completeParticle);
        StopParticle(propellerRevealParticle);
        StopParticle(roomBlueDustParticle);
    }

    private void Update()
    {
        if (isActivated && rotatingLightRing != null)
        {
            rotatingLightRing.Rotate(0f, 0f, ringRotateSpeed * Time.deltaTime);
        }

        if (isPropellerSpinning && propellerPivot != null)
        {
            float dir = spinClockwise ? -1f : 1f;
            propellerPivot.Rotate(0f, 0f, currentPropellerSpeed * dir * Time.deltaTime);
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
        StopParticle(propellerRevealParticle);

        if (coreEffectCoroutine != null)
            StopCoroutine(coreEffectCoroutine);

        coreEffectCoroutine = StartCoroutine(CoreEffectRoutine());

        if (delayBeforePropellerReveal > 0f)
            yield return new WaitForSeconds(delayBeforePropellerReveal);

        RevealPropeller();

        if (delayBeforeDoorOpen > 0f)
            yield return new WaitForSeconds(delayBeforeDoorOpen);

        OpenDoor();

        if (doorCoroutine != null)
            yield return doorCoroutine;

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
        PlayAudio(hitAudio);
        PlayParticle(hitParticle);

        yield return StartCoroutine(FlashRenderer(hitFlashRenderer, hitFlashAlpha, hitFlashTime));

        if (delayBeforeActivate > 0f)
            yield return new WaitForSeconds(delayBeforeActivate);

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

    private void RevealPropeller()
    {
        PlayAudio(propellerRevealAudio);
        PlayParticle(propellerRevealParticle);

        if (roomBlueDustParticle != null && !roomBlueDustParticle.isPlaying)
            roomBlueDustParticle.Play();

        StartCoroutine(FadeRendererArrayAlpha(
            propellerBaseRenderers,
            0f,
            propellerBaseAlpha,
            propellerRevealTime
        ));

        StartCoroutine(FadeRendererArrayAlpha(
            propellerGlowRenderers,
            0f,
            propellerGlowAlpha,
            propellerGlowFadeTime
        ));

        if (propellerSpinCoroutine != null)
            StopCoroutine(propellerSpinCoroutine);

        propellerSpinCoroutine = StartCoroutine(SpinUpPropellerRoutine());
    }

    private IEnumerator SpinUpPropellerRoutine()
    {
        if (propellerPivot == null)
            yield break;

        isPropellerSpinning = true;
        currentPropellerSpeed = propellerStartSpeed;

        if (propellerSpinLoopAudio != null && !propellerSpinLoopAudio.isPlaying)
            propellerSpinLoopAudio.Play();

        float timer = 0f;

        while (timer < propellerSpinUpTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / propellerSpinUpTime);
            float easedT = EaseOutCubic(t);

            currentPropellerSpeed = Mathf.Lerp(
                propellerStartSpeed,
                propellerTargetSpeed,
                easedT
            );

            yield return null;
        }

        currentPropellerSpeed = propellerStableSpeed;
        propellerSpinCoroutine = null;
    }

    private void OpenDoor()
    {
        if (isDoorOpened && activateOnlyOnce)
            return;

        isDoorOpened = true;

        PlayAudio(doorOpenAudio);

        onDoorOpenStart?.Invoke();
        SendMessageToTargets();

        if (moveDoorDirectly && doorTransform != null)
        {
            if (doorCoroutine != null)
                StopCoroutine(doorCoroutine);

            doorCoroutine = StartCoroutine(OpenDoorRoutine());
        }
        else
        {
            onDoorOpenComplete?.Invoke();
        }
    }

    private IEnumerator OpenDoorRoutine()
    {
        CacheDoorPosition();

        float timer = 0f;

        while (timer < doorOpenTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / doorOpenTime);
            float easedT = EaseOutCubic(t);

            doorTransform.localPosition = Vector3.Lerp(
                doorClosedLocalPosition,
                doorOpenLocalPosition,
                easedT
            );

            yield return null;
        }

        doorTransform.localPosition = doorOpenLocalPosition;

        onDoorOpenComplete?.Invoke();

        doorCoroutine = null;
    }

    private void SendMessageToTargets()
    {
        if (messageTargets == null)
            return;

        if (string.IsNullOrEmpty(messageName))
            return;

        for (int i = 0; i < messageTargets.Length; i++)
        {
            if (messageTargets[i] == null)
                continue;

            messageTargets[i].SendMessage(
                messageName,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void CacheDoorPosition()
    {
        if (doorPositionCached)
            return;

        if (doorTransform == null)
            return;

        doorClosedLocalPosition = doorTransform.localPosition;
        doorOpenLocalPosition = doorClosedLocalPosition + doorOpenLocalOffset;

        doorPositionCached = true;
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

    private IEnumerator FadeRendererArrayAlpha(SpriteRenderer[] renderers, float fromAlpha, float toAlpha, float duration)
    {
        if (renderers == null || renderers.Length == 0)
            yield break;

        if (duration <= 0f)
        {
            SetRendererArrayAlpha(renderers, toAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            SetRendererArrayAlpha(renderers, alpha);

            yield return null;
        }

        SetRendererArrayAlpha(renderers, toAlpha);
    }

    private void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
            return;

        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private void SetRendererArrayAlpha(SpriteRenderer[] renderers, float alpha)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            SetRendererAlpha(renderers[i], alpha);
        }
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

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void OnDisable()
    {
        if (propellerSpinLoopAudio != null)
            propellerSpinLoopAudio.Stop();
    }
}