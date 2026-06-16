using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(70000)]
public class BreakableFragmentPlatformEvent : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool activateOnlyOnce = true;

    [Header("Player Lock")]
    public bool lockPlayerMovement = true;
    public PlayerController playerController;
    public Rigidbody2D playerRigidbody;
    public Animator playerAnimator;

    public bool stopPlayerVelocityOnLock = true;
    public bool resetAnimatorOnLock = true;
    public string idleStateName = "Idle";

    [Tooltip("바닥이 부서진 뒤 조작을 다시 풀기까지 기다리는 시간")]
    public float unlockDelayAfterBreak = 0.25f;

    [Header("Previous Camera Area Release")]
    [Tooltip("파괴 직전에 5번 Area 같은 이전 카메라 스크립트를 꺼서 Y 고정을 해제합니다.")]
    public bool disablePreviousCameraScriptsOnBreak = true;

    [Tooltip("여기에 5번 Area에 붙어있는 CameraYLockZoomArea를 넣으세요.")]
    public MonoBehaviour[] previousCameraScriptsToDisable;

    [Header("Forced Fall Camera Follow")]
    [Tooltip("CameraMovement에 맡기지 않고, 이 스크립트가 직접 카메라 Y를 투야에게 맞춥니다.")]
    public bool useForcedFallCameraFollow = true;

    [Tooltip("비워두면 CameraMovement.Instance.transform을 자동으로 사용합니다.")]
    public Transform cameraRig;

    [Tooltip("비워두면 트리거에 닿은 Player를 자동으로 사용합니다.")]
    public Transform playerTransform;

    [Tooltip("투야 기준 카메라 Y 오프셋. 투야보다 약간 위를 보고 싶으면 양수.")]
    public float fallCameraYOffset = 1.2f;

    [Tooltip("카메라가 투야를 따라가는 부드러움. 작을수록 빠르게 따라감.")]
    public float fallCameraSmoothTime = 0.15f;

    [Tooltip("낙하 카메라 직접 추적 지속 시간. 0이면 계속 유지.")]
    public float forcedFallCameraDuration = 3f;

    [Tooltip("낙하 추적 중 CameraMovement의 이벤트 상태를 계속 풀어줍니다.")]
    public bool keepCameraMovementReleasedDuringFall = true;

    [Header("Camera Follow When Falling")]
    [Tooltip("기존 CameraMovement의 Y Follow도 같이 켜봅니다. 직접 추적이 메인이므로 보조용입니다.")]
    public bool enableCameraFollowYBeforeBreak = true;

    [Tooltip("MissionArea 같은 카메라 이벤트 상태를 파괴 직전에 풀어줍니다.")]
    public bool releaseCameraEventBeforeBreak = true;

    [Tooltip("카메라 FollowY를 켠 뒤 한 프레임 기다리고 바닥을 끕니다.")]
    public bool waitOneFrameAfterCameraFollow = true;

    [Tooltip("낙하 후 일정 시간 뒤 다시 Y Follow를 끌지")]
    public bool disableCameraFollowYAfterDelay = false;

    [Tooltip("몇 초 뒤 카메라 Y Follow를 끌지")]
    public float cameraFollowYDisableDelay = 3f;

    [Header("Platform Collider")]
    [Tooltip("부서지는 순간 꺼질 실제 발판 콜라이더들")]
    public Collider2D[] platformCollidersToDisable;

    [Tooltip("비워두면 이 부모 아래의 Collider2D를 자동으로 수집합니다.")]
    public Transform platformColliderParent;

    public bool autoCollectPlatformColliders = true;

    [Header("Fragments")]
    [Tooltip("22개 파편 스프라이트들이 들어있는 부모 오브젝트")]
    public Transform fragmentsParent;

    public Transform[] fragments;
    public bool autoCollectFragments = true;

    [Header("Event Timing")]
    public float delayBeforeFirstRumble = 0.35f;
    public float delayBetweenRumbles = 0.75f;
    public float delayBeforeBreak = 0.35f;

    [Header("Camera Shake")]
    public Transform cameraShakeTarget;

    public float firstShakeTime = 0.25f;
    public float firstShakePower = 0.08f;

    public float secondShakeTime = 0.35f;
    public float secondShakePower = 0.14f;

    public float breakShakeTime = 0.45f;
    public float breakShakePower = 0.22f;

    [Header("Audio")]
    public AudioSource rumbleAudio1;
    public AudioSource rumbleAudio2;
    public AudioSource breakAudio;

    [Header("Particles")]
    public ParticleSystem dustOnFirstRumble;
    public ParticleSystem dustOnSecondRumble;
    public ParticleSystem dustOnBreak;

    [Header("Fragment Scatter")]
    public float scatterDuration = 1.8f;
    public float randomPieceDelay = 0.25f;

    public float minHorizontalSpeed = 0f;
    public float maxHorizontalSpeed = 0.25f;

    public float minUpwardSpeed = 0f;
    public float maxUpwardSpeed = 0.05f;

    public float fallGravity = 5.5f;
    public float maxSpinSpeed = 0f;

    public bool fadeOutFragments = true;

    [Range(0f, 1f)]
    public float fadeStartNormalizedTime = 0.65f;

    [Header("State")]
    public bool isRunning;
    public bool hasActivated;
    public bool forcedFallCameraActive;

    private Collider2D triggerCollider;

    private SpriteRenderer[] fragmentRenderers;
    private Vector3[] fragmentStartLocalPositions;
    private Quaternion[] fragmentStartLocalRotations;
    private Color[] fragmentStartColors;

    private bool playerControllerWasEnabled;
    private Coroutine eventCoroutine;
    private Coroutine cameraShakeCoroutine;
    private Coroutine cameraFollowDisableCoroutine;
    private Coroutine forcedFallCameraStopCoroutine;

    private float fallCameraVelocityY;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        CollectPlatformColliders();
        CollectFragments();
        CacheFragmentOriginalState();

        StopParticle(dustOnFirstRumble);
        StopParticle(dustOnSecondRumble);
        StopParticle(dustOnBreak);
    }

    private void LateUpdate()
    {
        UpdateForcedFallCameraFollow();
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

        if (fragmentRenderers == null || fragmentRenderers.Length != fragments.Length)
        {
            fragmentRenderers = new SpriteRenderer[fragments.Length];

            for (int i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                    fragmentRenderers[i] = fragments[i].GetComponent<SpriteRenderer>();
            }
        }

        fragmentStartLocalPositions = new Vector3[fragments.Length];
        fragmentStartLocalRotations = new Quaternion[fragments.Length];
        fragmentStartColors = new Color[fragments.Length];

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            fragmentStartLocalPositions[i] = fragments[i].localPosition;
            fragmentStartLocalRotations[i] = fragments[i].localRotation;

            if (fragmentRenderers[i] != null)
                fragmentStartColors[i] = fragmentRenderers[i].color;
            else
                fragmentStartColors[i] = Color.white;
        }
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
        if (playerTransform == null)
            playerTransform = collision.transform;

        if (playerController == null)
            playerController = collision.GetComponentInParent<PlayerController>();

        if (playerRigidbody == null)
            playerRigidbody = collision.GetComponentInParent<Rigidbody2D>();

        if (playerAnimator == null)
            playerAnimator = collision.GetComponentInParent<Animator>();

        if (playerTransform == null && playerController != null)
            playerTransform = playerController.transform;

        if (playerTransform == null && playerRigidbody != null)
            playerTransform = playerRigidbody.transform;
    }

    private IEnumerator CollapseEventRoutine()
    {
        isRunning = true;
        hasActivated = true;

        LockPlayer();

        if (delayBeforeFirstRumble > 0f)
            yield return new WaitForSeconds(delayBeforeFirstRumble);

        PlayAudio(rumbleAudio1);
        PlayParticle(dustOnFirstRumble);
        StartCameraShake(firstShakeTime, firstShakePower);

        if (delayBetweenRumbles > 0f)
            yield return new WaitForSeconds(delayBetweenRumbles);

        PlayAudio(rumbleAudio2);
        PlayParticle(dustOnSecondRumble);
        StartCameraShake(secondShakeTime, secondShakePower);

        if (delayBeforeBreak > 0f)
            yield return new WaitForSeconds(delayBeforeBreak);

        PlayAudio(breakAudio);
        PlayParticle(dustOnBreak);
        StartCameraShake(breakShakeTime, breakShakePower);

        DisablePreviousCameraScripts();

        StartFallCameraFollow();

        StartForcedFallCameraFollow();

        if (waitOneFrameAfterCameraFollow)
            yield return null;

        DisablePlatformColliders();

        StartCoroutine(ScatterFragmentsRoutine());

        if (unlockDelayAfterBreak > 0f)
            yield return new WaitForSeconds(unlockDelayAfterBreak);

        UnlockPlayer();

        isRunning = false;
        eventCoroutine = null;
    }

    private void DisablePreviousCameraScripts()
    {
        if (!disablePreviousCameraScriptsOnBreak)
            return;

        if (previousCameraScriptsToDisable == null)
            return;

        for (int i = 0; i < previousCameraScriptsToDisable.Length; i++)
        {
            if (previousCameraScriptsToDisable[i] == null)
                continue;

            previousCameraScriptsToDisable[i].enabled = false;
        }
    }

    private void StartFallCameraFollow()
    {
        if (!enableCameraFollowYBeforeBreak)
            return;

        if (CameraMovement.Instance == null)
            return;

        if (releaseCameraEventBeforeBreak)
            CameraMovement.Instance.isMovingEvent = false;

        CameraMovement.Instance.SetFollowPlayerY(true);

        if (disableCameraFollowYAfterDelay)
        {
            if (cameraFollowDisableCoroutine != null)
                StopCoroutine(cameraFollowDisableCoroutine);

            cameraFollowDisableCoroutine = StartCoroutine(DisableCameraFollowYAfterDelayRoutine());
        }
    }

    private IEnumerator DisableCameraFollowYAfterDelayRoutine()
    {
        yield return new WaitForSeconds(cameraFollowYDisableDelay);

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.SetFollowPlayerY(false);

        cameraFollowDisableCoroutine = null;
    }

    private void StartForcedFallCameraFollow()
    {
        if (!useForcedFallCameraFollow)
            return;

        if (cameraRig == null && CameraMovement.Instance != null)
            cameraRig = CameraMovement.Instance.transform;

        if (cameraRig == null && Camera.main != null)
        {
            if (Camera.main.transform.parent != null)
                cameraRig = Camera.main.transform.parent;
            else
                cameraRig = Camera.main.transform;
        }

        if (playerTransform == null && playerController != null)
            playerTransform = playerController.transform;

        if (playerTransform == null && playerRigidbody != null)
            playerTransform = playerRigidbody.transform;

        if (cameraRig == null || playerTransform == null)
        {
            Debug.LogWarning($"{gameObject.name} : Forced Fall Camera Follow 실패. CameraRig 또는 PlayerTransform이 없습니다.");
            return;
        }

        fallCameraVelocityY = 0f;
        forcedFallCameraActive = true;

        if (forcedFallCameraStopCoroutine != null)
            StopCoroutine(forcedFallCameraStopCoroutine);

        if (forcedFallCameraDuration > 0f)
            forcedFallCameraStopCoroutine = StartCoroutine(StopForcedFallCameraAfterDelayRoutine());
    }

    private IEnumerator StopForcedFallCameraAfterDelayRoutine()
    {
        yield return new WaitForSeconds(forcedFallCameraDuration);

        forcedFallCameraActive = false;
        forcedFallCameraStopCoroutine = null;
    }

    private void UpdateForcedFallCameraFollow()
    {
        if (!forcedFallCameraActive)
            return;

        if (!useForcedFallCameraFollow)
            return;

        if (cameraRig == null || playerTransform == null)
            return;

        if (keepCameraMovementReleasedDuringFall && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        float targetY = playerTransform.position.y + fallCameraYOffset;

        Vector3 camPos = cameraRig.position;

        camPos.y = Mathf.SmoothDamp(
            camPos.y,
            targetY,
            ref fallCameraVelocityY,
            fallCameraSmoothTime
        );

        cameraRig.position = camPos;
    }

    private void LockPlayer()
    {
        if (!lockPlayerMovement)
            return;

        if (playerRigidbody != null && stopPlayerVelocityOnLock)
        {
            playerRigidbody.velocity = Vector2.zero;
        }

        if (playerAnimator != null && resetAnimatorOnLock)
        {
            ResetAnimatorMoveParameters();

            if (!string.IsNullOrEmpty(idleStateName))
                playerAnimator.CrossFade(idleStateName, 0.05f);
        }

        if (playerController != null)
        {
            playerControllerWasEnabled = playerController.enabled;
            playerController.enabled = false;
        }
    }

    private void UnlockPlayer()
    {
        if (!lockPlayerMovement)
            return;

        if (playerController != null)
        {
            playerController.enabled = playerControllerWasEnabled;
        }
    }

    private void ResetAnimatorMoveParameters()
    {
        if (playerAnimator == null)
            return;

        TrySetFloat("Speed", 0f);
        TrySetFloat("speed", 0f);
        TrySetFloat("Horizontal", 0f);
        TrySetFloat("MoveX", 0f);
        TrySetFloat("moveX", 0f);
        TrySetFloat("VelocityX", 0f);
        TrySetFloat("velocityX", 0f);

        TrySetBool("IsMoving", false);
        TrySetBool("isMoving", false);
        TrySetBool("Moving", false);
        TrySetBool("moving", false);
        TrySetBool("IsRunning", false);
        TrySetBool("isRunning", false);
        TrySetBool("Running", false);
        TrySetBool("running", false);
        TrySetBool("Run", false);
        TrySetBool("run", false);
        TrySetBool("Walk", false);
        TrySetBool("walk", false);
        TrySetBool("IsWalking", false);
        TrySetBool("isWalking", false);
    }

    private void TrySetFloat(string parameterName, float value)
    {
        if (playerAnimator == null)
            return;

        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            return;

        playerAnimator.SetFloat(parameterName, value);
    }

    private void TrySetBool(string parameterName, bool value)
    {
        if (playerAnimator == null)
            return;

        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            return;

        playerAnimator.SetBool(parameterName, value);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (playerAnimator == null)
            return false;

        AnimatorControllerParameter[] parameters = playerAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == type)
                return true;
        }

        return false;
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

    private IEnumerator ScatterFragmentsRoutine()
    {
        if (fragments == null || fragments.Length == 0)
            yield break;

        CacheFragmentOriginalState();

        Vector3 localCenter = GetFragmentsLocalCenter();

        float[] pieceDelays = new float[fragments.Length];
        Vector3[] velocities = new Vector3[fragments.Length];
        float[] spinSpeeds = new float[fragments.Length];

        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
                continue;

            pieceDelays[i] = Random.Range(0f, randomPieceDelay);

            float xFromCenter = fragmentStartLocalPositions[i].x - localCenter.x;
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

                float t = Mathf.Clamp01(localTime / scatterDuration);

                Vector3 startLocalPos = fragmentStartLocalPositions[i];
                Quaternion startLocalRot = fragmentStartLocalRotations[i];

                Vector3 velocityOffset = velocities[i] * localTime;
                Vector3 gravityOffset = Vector3.down * 0.5f * fallGravity * localTime * localTime;

                fragments[i].localPosition = startLocalPos + velocityOffset + gravityOffset;
                fragments[i].localRotation = startLocalRot * Quaternion.Euler(0f, 0f, spinSpeeds[i] * localTime);

                if (fadeOutFragments && fragmentRenderers != null && i < fragmentRenderers.Length)
                {
                    if (fragmentRenderers[i] != null)
                    {
                        float fadeT = Mathf.InverseLerp(fadeStartNormalizedTime, 1f, t);
                        fadeT = Mathf.Clamp01(fadeT);

                        Color color = fragmentStartColors[i];
                        color.a = Mathf.Lerp(fragmentStartColors[i].a, 0f, fadeT);
                        fragmentRenderers[i].color = color;
                    }
                }
            }

            yield return null;
        }
    }

    private Vector3 GetFragmentsLocalCenter()
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

            float t = Mathf.Clamp01(timer / duration);
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
}