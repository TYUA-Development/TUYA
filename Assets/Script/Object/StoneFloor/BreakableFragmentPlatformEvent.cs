using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(70000)]
public class BreakableFragmentPlatformEvent : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool activateOnlyOnce = true;

    [Header("Player")]
    public PlayerController playerController;
    public Rigidbody2D playerRigidbody;

    [Tooltip("PlayerController는 끄지 않고 입력만 잠급니다.")]
    public bool usePlayerControllerInputLock = true;

    [Tooltip("도착 순간 속도를 0으로 멈춥니다.")]
    public bool stopPlayerVelocityOnStart = true;

    [Tooltip("바닥 Collider가 꺼진 뒤 PlayerController.OnFall()을 한 번 호출합니다.")]
    public bool callFallOnceAfterPlatformBreak = true;

    [Tooltip("Collider OFF 후 몇 번의 FixedUpdate를 기다린 뒤 OnFall을 호출할지")]
    public int fixedFramesBeforeCallFall = 2;

    [Tooltip("OnFall 호출 직전에 아래 방향 속도를 살짝 줍니다.")]
    public float fallStartDownVelocity = -0.35f;

    [Tooltip("최종 쿵 이후 입력 잠금을 조금 더 유지하는 시간")]
    public float extraInputLockAfterFinalImpact = 0.25f;

    [Header("Cinematic Fall Speed Curve")]
    [Tooltip("파편 구간 낙하 속도를 연출용으로 조절합니다.")]
    public bool useCinematicFallSpeed = true;

    [Tooltip("파편 낙하에서 사용할 원래 중력값. Player Rigidbody2D의 기본 Gravity Scale 값을 직접 넣으세요.")]
    public float normalFallGravityScale = 1f;

    [Tooltip("처음 떨어질 때 툭 떨어지는 느낌을 주는 시작 속도")]
    public float initialNormalFallVelocity = -2.2f;

    [Tooltip("낙하가 끝나는 바닥 위치를 알려주는 빈 오브젝트. 최종 도착 바닥 근처에 두세요.")]
    public Transform fallEndPoint;

    [Tooltip("Fall End Point가 없을 때 사용할 도착 Y값")]
    public float fallEndY = -10f;

    [Tooltip("처음 이 비율만큼은 원래 속도로 떨어집니다.")]
    [Range(0f, 0.45f)]
    public float topNormalPercent = 0.2f;

    [Tooltip("마지막 이 비율만큼은 원래 속도로 떨어집니다.")]
    [Range(0f, 0.45f)]
    public float bottomNormalPercent = 0.22f;

    [Tooltip("느린 구간으로 부드럽게 들어가고 나오는 폭")]
    [Range(0.01f, 0.3f)]
    public float slowBlendPercent = 0.08f;

    [Tooltip("중간 구간 중력. 낮을수록 천천히 떨어집니다.")]
    public float middleGravityScale = 0.35f;

    [Tooltip("중간 구간 최대 낙하 속도. 0에 가까울수록 천천히 떨어집니다.")]
    public float middleMaxFallSpeed = -2.4f;

    [Tooltip("처음/끝 구간 최대 낙하 속도. 보통 크게 둡니다.")]
    public float normalMaxFallSpeed = -20f;

    [Tooltip("낙하 시작 후 이 시간 전에는 착지 판정을 보지 않습니다.")]
    public float minCinematicFallTime = 0.25f;

    [Tooltip("복구 안전장치 시간")]
    public float maxCinematicFallTime = 8f;

    [Header("Cinematic Fall Camera Lag")]
    [Tooltip("파편 낙하 중 카메라가 캐릭터를 살짝 늦게 따라가게 합니다.")]
    public bool useFallCameraLag = true;

    [Tooltip("비워두면 Main Camera를 사용합니다. 가능하면 Camera Root/Rig를 넣는 게 좋습니다.")]
    public Transform fallCameraTarget;

    [Tooltip("카메라가 캐릭터보다 위를 보게 하는 값. 클수록 캐릭터가 화면 아래쪽에 보입니다.")]
    public float fallCameraYOffset = 2.3f;

    [Tooltip("카메라 따라가는 느림 정도. 클수록 늦게 따라갑니다.")]
    public float fallCameraSmoothTime = 0.9f;

    [Tooltip("착지 직전에는 카메라가 조금 더 빨리 따라오게 할지")]
    public bool fasterCameraNearBottom = true;

    [Tooltip("착지 직전 카메라 Smooth Time")]
    public float bottomCameraSmoothTime = 0.45f;

    [Tooltip("카메라 최대 이동 속도")]
    public float fallCameraMaxSpeed = 30f;

    [Tooltip("낙하 시작 후 카메라가 따라가기 시작하기까지 딜레이")]
    public float fallCameraStartDelay = 0.08f;

    [Header("Platform Collider")]
    public Collider2D[] platformCollidersToDisable;
    public Transform platformColliderParent;
    public bool autoCollectPlatformColliders = true;

    [Header("Fragments")]
    public Transform fragmentsParent;
    public Transform[] fragments;
    public bool autoCollectFragments = true;

    [Header("Event Timing")]
    public float warningDuration = 5f;
    public float delayAfterCrackBeforeFinalImpact = 2f;

    [Header("Warning Cues")]
    public float warningCue1Time = 1.1f;
    public float warningCue2Time = 3.0f;
    public float warningCue3Time = 4.45f;

    [Header("Camera Shake")]
    public Transform cameraShakeTarget;

    public float firstShakeTime = 0.22f;
    public float firstShakePower = 0.045f;

    public float secondShakeTime = 0.28f;
    public float secondShakePower = 0.075f;

    public float thirdShakeTime = 0.22f;
    public float thirdShakePower = 0.045f;

    public float crackShakeTime = 0.32f;
    public float crackShakePower = 0.16f;

    public float finalImpactShakeTime = 0.45f;
    public float finalImpactShakePower = 0.26f;

    [Header("Audio")]
    public AudioSource warningLoopAudio;
    public AudioSource rumbleAudio1;
    public AudioSource rumbleAudio2;
    public AudioSource rumbleAudio3;
    public AudioSource crackAudio;
    public AudioSource finalImpactAudio;

    [Header("Particles")]
    public ParticleSystem dustOnFirstRumble;
    public ParticleSystem dustOnSecondRumble;
    public ParticleSystem dustOnThirdRumble;
    public ParticleSystem dustOnCrack;
    public ParticleSystem dustOnFinalImpact;

    [Header("Crack Burst")]
    public float crackBurstDuration = 0.28f;
    public float crackRandomPieceDelay = 0.06f;

    public float crackMinHorizontalOffset = 0.03f;
    public float crackMaxHorizontalOffset = 0.16f;
    public float crackDownOffset = 0.04f;
    public float crackVerticalJitter = 0.05f;

    [Tooltip("대지 포함 파편이면 0 유지 추천")]
    public float crackMaxSpinAngle = 0f;

    [Header("Final Collapse")]
    public float scatterDuration = 1.8f;
    public float randomPieceDelay = 0.18f;

    public float minHorizontalSpeed = 0f;
    public float maxHorizontalSpeed = 0.25f;

    public float minUpwardSpeed = 0f;
    public float maxUpwardSpeed = 0.05f;

    public float fallGravity = 5.5f;

    [Tooltip("대지 포함 파편이면 0 유지 추천")]
    public float maxSpinSpeed = 0f;

    public bool fadeOutFragments = true;

    [Range(0f, 1f)]
    public float fadeStartNormalizedTime = 0.65f;

    [Header("Camera On Final Impact")]
    public bool releaseCameraEventOnFinalImpact = true;

    [Tooltip("FallZoomCameraArea가 낙하 카메라를 담당하면 꺼두세요.")]
    public bool enableCameraFollowYOnFinalImpact = false;

    [Header("State")]
    public bool isRunning;
    public bool hasActivated;
    public bool cinematicFallActive;

    private Collider2D triggerCollider;

    private SpriteRenderer[] fragmentRenderers;
    private Vector3[] fragmentOriginalLocalPositions;
    private Quaternion[] fragmentOriginalLocalRotations;
    private Color[] fragmentOriginalColors;

    private Vector3[] crackTargetLocalPositions;
    private Quaternion[] crackTargetLocalRotations;

    private Coroutine eventCoroutine;
    private Coroutine cameraShakeCoroutine;
    private Coroutine callFallCoroutine;

    private float originalGravityScale;
    private bool hasOriginalGravityScale;

    private float fallStartY;
    private float resolvedFallEndY;
    private float cinematicFallStartTime;

    private float fallCameraYVelocity;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        CollectPlatformColliders();
        CollectFragments();
        CacheFragmentOriginalState();

        StopParticle(dustOnFirstRumble);
        StopParticle(dustOnSecondRumble);
        StopParticle(dustOnThirdRumble);
        StopParticle(dustOnCrack);
        StopParticle(dustOnFinalImpact);
    }

    private void FixedUpdate()
    {
        UpdateCinematicFallSpeed();
    }

    private void LateUpdate()
    {
        UpdateCinematicFallSpeed();
        UpdateFallCameraLag();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        if (activateOnlyOnce && hasActivated)
            return;

        if (isRunning)
            return;

        FindPlayerReferences(collision);

        eventCoroutine = StartCoroutine(CollapseEventRoutine());
    }

    private void FindPlayerReferences(Collider2D collision)
    {
        if (playerController == null)
            playerController = collision.GetComponentInParent<PlayerController>();

        if (playerRigidbody == null)
            playerRigidbody = collision.GetComponentInParent<Rigidbody2D>();
    }

    private IEnumerator CollapseEventRoutine()
    {
        isRunning = true;
        hasActivated = true;

        LockInputOnly();

        if (warningLoopAudio != null)
        {
            warningLoopAudio.Stop();
            warningLoopAudio.Play();
        }

        yield return StartCoroutine(WarningRoutine());

        if (warningLoopAudio != null)
            warningLoopAudio.Stop();

        PlayAudio(crackAudio);
        PlayParticle(dustOnCrack);
        StartCameraShake(crackShakeTime, crackShakePower);

        yield return StartCoroutine(CrackBurstRoutine());

        if (delayAfterCrackBeforeFinalImpact > 0f)
            yield return new WaitForSeconds(delayAfterCrackBeforeFinalImpact);

        PlayAudio(finalImpactAudio);
        PlayParticle(dustOnFinalImpact);
        StartCameraShake(finalImpactShakeTime, finalImpactShakePower);

        PrepareCameraForFinalImpact();

        DisablePlatformColliders();

        StartCinematicFall();

        if (callFallCoroutine != null)
            StopCoroutine(callFallCoroutine);

        callFallCoroutine = StartCoroutine(CallFallOnceAfterPlatformBreakRoutine());

        StartCoroutine(FinalCollapseFragmentsRoutine());

        isRunning = false;
        eventCoroutine = null;
    }

    private void LockInputOnly()
    {
        if (playerController == null)
            return;

        float lockTime =
            warningDuration +
            crackBurstDuration +
            crackRandomPieceDelay +
            delayAfterCrackBeforeFinalImpact +
            extraInputLockAfterFinalImpact;

        if (playerRigidbody != null && stopPlayerVelocityOnStart)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if (usePlayerControllerInputLock)
        {
            playerController.LockPlayerInput(lockTime);
        }
    }

    private IEnumerator CallFallOnceAfterPlatformBreakRoutine()
    {
        if (!callFallOnceAfterPlatformBreak)
            yield break;

        if (playerController == null)
            yield break;

        int waitCount = Mathf.Max(1, fixedFramesBeforeCallFall);

        for (int i = 0; i < waitCount; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        playerController.isGround = false;
        playerController.isOnRunway = false;
        playerController.isOnGrass = false;
        playerController.isOnStone = false;

        if (playerRigidbody != null)
        {
            Vector2 velocity = playerRigidbody.velocity;

            if (velocity.y > fallStartDownVelocity)
                velocity.y = fallStartDownVelocity;

            playerRigidbody.velocity = velocity;
            playerRigidbody.WakeUp();
        }

        playerController.OnFall();

        callFallCoroutine = null;
    }

    private void StartCinematicFall()
    {
        if (!useCinematicFallSpeed && !useFallCameraLag)
            return;

        if (playerRigidbody == null)
            return;

        if (!hasOriginalGravityScale)
        {
            originalGravityScale = normalFallGravityScale;
            hasOriginalGravityScale = true;
        }

        playerRigidbody.gravityScale = originalGravityScale;

        Vector2 startVelocity = playerRigidbody.velocity;

        if (startVelocity.y > initialNormalFallVelocity)
            startVelocity.y = initialNormalFallVelocity;

        playerRigidbody.velocity = startVelocity;
        playerRigidbody.WakeUp();

        fallStartY = playerRigidbody.transform.position.y;
        resolvedFallEndY = fallEndPoint != null ? fallEndPoint.position.y : fallEndY;

        if (resolvedFallEndY >= fallStartY - 0.1f)
            resolvedFallEndY = fallStartY - 6f;

        cinematicFallActive = true;
        cinematicFallStartTime = Time.time;
        fallCameraYVelocity = 0f;
    }

    private void StopCinematicFall()
    {
        if (!cinematicFallActive)
            return;

        cinematicFallActive = false;

        if (playerRigidbody != null && hasOriginalGravityScale)
        {
            playerRigidbody.gravityScale = originalGravityScale;
        }
    }

    private void UpdateCinematicFallSpeed()
    {
        if (!cinematicFallActive)
            return;

        if (!useCinematicFallSpeed)
            return;

        if (playerRigidbody == null)
        {
            cinematicFallActive = false;
            return;
        }

        float progress = GetFallProgress01();
        float slowWeight = GetMiddleSlowWeight(progress);

        float targetGravity = Mathf.Lerp(originalGravityScale, middleGravityScale, slowWeight);
        float targetMaxFallSpeed = Mathf.Lerp(normalMaxFallSpeed, middleMaxFallSpeed, slowWeight);

        playerRigidbody.gravityScale = targetGravity;

        Vector2 velocity = playerRigidbody.velocity;

        if (velocity.y < targetMaxFallSpeed)
            velocity.y = targetMaxFallSpeed;

        playerRigidbody.velocity = velocity;

        if (Time.time - cinematicFallStartTime < minCinematicFallTime)
            return;

        if (Time.time - cinematicFallStartTime >= maxCinematicFallTime)
        {
            StopCinematicFall();
            return;
        }

        if (playerController != null && playerController.isGround)
        {
            StopCinematicFall();
        }
    }

    private void UpdateFallCameraLag()
    {
        if (!cinematicFallActive)
            return;

        if (!useFallCameraLag)
            return;

        if (playerRigidbody == null)
            return;

        if (Time.time - cinematicFallStartTime < fallCameraStartDelay)
            return;

        Transform cam = GetFallCameraTarget();

        if (cam == null)
            return;

        float progress = GetFallProgress01();

        float smoothTime = fallCameraSmoothTime;

        if (fasterCameraNearBottom)
        {
            float bottomStart = 1f - bottomNormalPercent;
            float bottomT = Mathf.InverseLerp(bottomStart, 1f, progress);
            bottomT = Mathf.Clamp01(bottomT);

            smoothTime = Mathf.Lerp(fallCameraSmoothTime, bottomCameraSmoothTime, bottomT);
        }

        float targetY = playerRigidbody.transform.position.y + fallCameraYOffset;

        Vector3 pos = cam.position;
        pos.y = Mathf.SmoothDamp(
            pos.y,
            targetY,
            ref fallCameraYVelocity,
            Mathf.Max(0.01f, smoothTime),
            fallCameraMaxSpeed,
            Time.deltaTime
        );

        cam.position = pos;
    }

    private Transform GetFallCameraTarget()
    {
        if (fallCameraTarget != null)
            return fallCameraTarget;

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
            return mainCamera.transform;

        return null;
    }

    private float GetFallProgress01()
    {
        if (playerRigidbody == null)
            return 0f;

        float currentY = playerRigidbody.transform.position.y;
        float totalDrop = Mathf.Max(0.01f, fallStartY - resolvedFallEndY);
        float dropped = fallStartY - currentY;

        return Mathf.Clamp01(dropped / totalDrop);
    }

    private float GetMiddleSlowWeight(float progress)
    {
        float topEnd = topNormalPercent;
        float middleInEnd = topEnd + slowBlendPercent;

        float bottomStart = 1f - bottomNormalPercent;
        float middleOutStart = bottomStart - slowBlendPercent;

        float inWeight = Mathf.InverseLerp(topEnd, middleInEnd, progress);
        inWeight = Smooth01(Mathf.Clamp01(inWeight));

        float outWeight = 1f - Mathf.InverseLerp(middleOutStart, bottomStart, progress);
        outWeight = Smooth01(Mathf.Clamp01(outWeight));

        return Mathf.Min(inWeight, outWeight);
    }

    private float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private IEnumerator WarningRoutine()
    {
        bool cue1Played = false;
        bool cue2Played = false;
        bool cue3Played = false;

        float timer = 0f;

        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            if (!cue1Played && timer >= warningCue1Time)
            {
                cue1Played = true;
                PlayAudio(rumbleAudio1);
                PlayParticle(dustOnFirstRumble);
                StartCameraShake(firstShakeTime, firstShakePower);
            }

            if (!cue2Played && timer >= warningCue2Time)
            {
                cue2Played = true;
                PlayAudio(rumbleAudio2);
                PlayParticle(dustOnSecondRumble);
                StartCameraShake(secondShakeTime, secondShakePower);
            }

            if (!cue3Played && timer >= warningCue3Time)
            {
                cue3Played = true;
                PlayAudio(rumbleAudio3);
                PlayParticle(dustOnThirdRumble);
                StartCameraShake(thirdShakeTime, thirdShakePower);
            }

            yield return null;
        }
    }

    private void CollectPlatformColliders()
    {
        if (!autoCollectPlatformColliders)
            return;

        if (platformCollidersToDisable != null && platformCollidersToDisable.Length > 0)
            return;

        if (platformColliderParent == null)
            return;

        platformCollidersToDisable = platformColliderParent.GetComponentsInChildren<Collider2D>(true);
    }

    private void CollectFragments()
    {
        if (!autoCollectFragments)
            return;

        if (fragments != null && fragments.Length > 0)
            return;

        if (fragmentsParent == null)
            return;

        fragmentRenderers = fragmentsParent.GetComponentsInChildren<SpriteRenderer>(true);
        fragments = new Transform[fragmentRenderers.Length];

        for (int i = 0; i < fragmentRenderers.Length; i++)
        {
            fragments[i] = fragmentRenderers[i].transform;
        }
    }

    private void CacheFragmentOriginalState()
    {
        if (fragments == null)
            return;

        fragmentOriginalLocalPositions = new Vector3[fragments.Length];
        fragmentOriginalLocalRotations = new Quaternion[fragments.Length];
        fragmentOriginalColors = new Color[fragments.Length];

        if (fragmentRenderers == null || fragmentRenderers.Length != fragments.Length)
            fragmentRenderers = new SpriteRenderer[fragments.Length];

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            fragmentOriginalLocalPositions[i] = fragments[i].localPosition;
            fragmentOriginalLocalRotations[i] = fragments[i].localRotation;

            if (fragmentRenderers[i] == null)
                fragmentRenderers[i] = fragments[i].GetComponent<SpriteRenderer>();

            if (fragmentRenderers[i] != null)
                fragmentOriginalColors[i] = fragmentRenderers[i].color;
            else
                fragmentOriginalColors[i] = Color.white;
        }
    }

    private IEnumerator CrackBurstRoutine()
    {
        if (fragments == null || fragments.Length == 0)
            yield break;

        CacheFragmentOriginalState();

        crackTargetLocalPositions = new Vector3[fragments.Length];
        crackTargetLocalRotations = new Quaternion[fragments.Length];

        Vector3 localCenter = GetFragmentsLocalCenterFromOriginal();

        float[] pieceDelays = new float[fragments.Length];

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            Vector3 startPos = fragmentOriginalLocalPositions[i];

            float xFromCenter = startPos.x - localCenter.x;
            float dirX = Mathf.Sign(xFromCenter);

            if (Mathf.Abs(xFromCenter) < 0.05f)
                dirX = Random.value > 0.5f ? 1f : -1f;

            float horizontalOffset = Random.Range(crackMinHorizontalOffset, crackMaxHorizontalOffset) * dirX;
            float verticalOffset = -crackDownOffset + Random.Range(-crackVerticalJitter, crackVerticalJitter);

            crackTargetLocalPositions[i] = startPos + new Vector3(horizontalOffset, verticalOffset, 0f);

            float spinAngle = Random.Range(-crackMaxSpinAngle, crackMaxSpinAngle);
            crackTargetLocalRotations[i] = fragmentOriginalLocalRotations[i] * Quaternion.Euler(0f, 0f, spinAngle);

            pieceDelays[i] = Random.Range(0f, crackRandomPieceDelay);
        }

        float totalTime = crackBurstDuration + crackRandomPieceDelay;
        float timer = 0f;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;

            for (int i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] == null)
                    continue;

                float localTime = timer - pieceDelays[i];

                if (localTime < 0f)
                    continue;

                float t = crackBurstDuration <= 0f ? 1f : Mathf.Clamp01(localTime / crackBurstDuration);
                float easedT = EaseOutCubic(t);

                fragments[i].localPosition = Vector3.Lerp(
                    fragmentOriginalLocalPositions[i],
                    crackTargetLocalPositions[i],
                    easedT
                );

                fragments[i].localRotation = Quaternion.Lerp(
                    fragmentOriginalLocalRotations[i],
                    crackTargetLocalRotations[i],
                    easedT
                );
            }

            yield return null;
        }

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            fragments[i].localPosition = crackTargetLocalPositions[i];
            fragments[i].localRotation = crackTargetLocalRotations[i];
        }
    }

    private IEnumerator FinalCollapseFragmentsRoutine()
    {
        if (fragments == null || fragments.Length == 0)
            yield break;

        Vector3[] startPositions = new Vector3[fragments.Length];
        Quaternion[] startRotations = new Quaternion[fragments.Length];
        Color[] startColors = new Color[fragments.Length];

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            startPositions[i] = fragments[i].localPosition;
            startRotations[i] = fragments[i].localRotation;

            if (fragmentRenderers != null && i < fragmentRenderers.Length && fragmentRenderers[i] != null)
                startColors[i] = fragmentRenderers[i].color;
            else
                startColors[i] = Color.white;
        }

        Vector3 localCenter = GetFragmentsLocalCenterCurrent();

        float[] pieceDelays = new float[fragments.Length];
        Vector3[] velocities = new Vector3[fragments.Length];
        float[] spinSpeeds = new float[fragments.Length];

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            pieceDelays[i] = Random.Range(0f, randomPieceDelay);

            float xFromCenter = startPositions[i].x - localCenter.x;
            float directionX = Mathf.Sign(xFromCenter);

            if (Mathf.Abs(xFromCenter) < 0.05f)
                directionX = Random.value > 0.5f ? 1f : -1f;

            float horizontalSpeed = Random.Range(minHorizontalSpeed, maxHorizontalSpeed) * directionX;
            float upwardSpeed = Random.Range(minUpwardSpeed, maxUpwardSpeed);

            velocities[i] = new Vector3(horizontalSpeed, upwardSpeed, 0f);
            spinSpeeds[i] = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        }

        float timer = 0f;
        float totalTime = scatterDuration + randomPieceDelay;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;

            for (int i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] == null)
                    continue;

                float localTime = timer - pieceDelays[i];

                if (localTime < 0f)
                    continue;

                float t = scatterDuration <= 0f ? 1f : Mathf.Clamp01(localTime / scatterDuration);

                Vector3 velocityOffset = velocities[i] * localTime;
                Vector3 gravityOffset = Vector3.down * 0.5f * fallGravity * localTime * localTime;

                fragments[i].localPosition = startPositions[i] + velocityOffset + gravityOffset;
                fragments[i].localRotation = startRotations[i] * Quaternion.Euler(0f, 0f, spinSpeeds[i] * localTime);

                if (fadeOutFragments && fragmentRenderers != null && i < fragmentRenderers.Length)
                {
                    if (fragmentRenderers[i] != null)
                    {
                        float fadeT = Mathf.InverseLerp(fadeStartNormalizedTime, 1f, t);
                        fadeT = Mathf.Clamp01(fadeT);

                        Color color = startColors[i];
                        color.a = Mathf.Lerp(startColors[i].a, 0f, fadeT);
                        fragmentRenderers[i].color = color;
                    }
                }
            }

            yield return null;
        }
    }

    private void PrepareCameraForFinalImpact()
    {
        if (CameraMovement.Instance == null)
            return;

        if (releaseCameraEventOnFinalImpact)
            CameraMovement.Instance.isMovingEvent = false;

        if (enableCameraFollowYOnFinalImpact)
            CameraMovement.Instance.SetFollowPlayerY(true);
    }

    private void DisablePlatformColliders()
    {
        if (platformCollidersToDisable == null || platformCollidersToDisable.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name} : Platform Colliders To Disable이 비어있습니다. 실제 발판 Collider를 넣어주세요.");
            return;
        }

        for (int i = 0; i < platformCollidersToDisable.Length; i++)
        {
            if (platformCollidersToDisable[i] == null)
                continue;

            if (platformCollidersToDisable[i] == triggerCollider)
                continue;

            platformCollidersToDisable[i].enabled = false;
        }
    }

    private Vector3 GetFragmentsLocalCenterFromOriginal()
    {
        if (fragments == null || fragments.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            sum += fragmentOriginalLocalPositions[i];
            count++;
        }

        if (count <= 0)
            return Vector3.zero;

        return sum / count;
    }

    private Vector3 GetFragmentsLocalCenterCurrent()
    {
        if (fragments == null || fragments.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            sum += fragments[i].localPosition;
            count++;
        }

        if (count <= 0)
            return Vector3.zero;

        return sum / count;
    }

    private void StartCameraShake(float duration, float power)
    {
        if (cameraShakeCoroutine != null)
            StopCoroutine(cameraShakeCoroutine);

        cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(duration, power));
    }

    private IEnumerator CameraShakeRoutine(float duration, float power)
    {
        Transform target = GetCameraShakeTarget();

        if (target == null)
            yield break;

        Vector3 baseLocalPosition = target.localPosition;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);
            float fade = 1f - t;

            Vector2 random = Random.insideUnitCircle * power * fade;

            target.localPosition = baseLocalPosition + new Vector3(
                random.x,
                random.y,
                0f
            );

            yield return null;
        }

        target.localPosition = baseLocalPosition;
        cameraShakeCoroutine = null;
    }

    private Transform GetCameraShakeTarget()
    {
        if (cameraShakeTarget != null)
            return cameraShakeTarget;

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
            return mainCamera.transform;

        return null;
    }

    private void PlayAudio(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.Play();
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

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void OnDisable()
    {
        if (warningLoopAudio != null)
            warningLoopAudio.Stop();

        StopCinematicFall();

        if (callFallCoroutine != null)
        {
            StopCoroutine(callFallCoroutine);
            callFallCoroutine = null;
        }
    }
}