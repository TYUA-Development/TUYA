using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SH_MissionAreaCamera : MonoBehaviour
{
    public enum MissionCameraMode
    {
        HorizontalByPlayerX,
        FixedAreaPan,
        FollowPlayerXFixedY
    }

    private enum CameraControlState
    {
        None,
        Entering,
        Inside,
        Exiting
    }

    [Header("Mode")]
    public MissionCameraMode cameraMode = MissionCameraMode.HorizontalByPlayerX;

    [Header("Target")]
    public Vector3 targetPos;

    [Tooltip("targetPos.x 기준 좌우 거리. HorizontalByPlayerX 모드에서 사용")]
    public float maxSizeXPos = 5f;

    [Header("Zoom")]
    public float finalZoomSize;

    [Tooltip("카메라와 바닥 기준 Z 거리")]
    public float groundDistance = 28f;

    [Header("Smooth Entry Blend")]
    [Tooltip("영역 진입 시 현재 카메라에서 목표 카메라로 부드럽게 전환합니다.")]
    public bool useSmoothEntryBlend = true;

    [Tooltip("영역 진입 시 목표 카메라 상태로 넘어가는 시간")]
    public float entryBlendTime = 1.0f;

    [Tooltip("영역 진입 전환 곡선")]
    public AnimationCurve entryBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Y Follow - Horizontal Mode")]
    public bool fixPosY = false;

    [Tooltip("기존 좌우 진행 모드에서 플레이어보다 카메라가 얼마나 위를 볼지")]
    public float playerYOffset = 15.13f;

    [Tooltip("체크한 구역에서만 플레이어 Y값을 부드럽게 따라갑니다.")]
    public bool useSmoothYFollow = false;

    public float yFollowSmoothTime = 0.35f;
    public float yMaxFollowSpeed = 50f;

    [Header("Exit Y Correction")]
    [SerializeField] private bool useCurrentPlayerYOnExit = true;
    [SerializeField] private float exitYOffset = 15.13f;
    [SerializeField] private bool centerPlayerOnExit = true;
    [SerializeField] private Vector2 exitCenterOffset = Vector2.zero;
    [SerializeField] private float exitTransitionTime = 1.2f;
    [SerializeField] private float exitCenteringTime = 4f;
    [SerializeField] private float missionArea2ExitCenteringTime = 6f;
    [SerializeField] private bool holdExitCenterUntilNextArea = true;
    [SerializeField] private float exitCenterHoldTime = 2f;
    [SerializeField] private AnimationCurve exitCenteringCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float exitYFollowSmoothTime = 0.25f;
    [SerializeField] private bool centerPlayerDuringExitZoom = true;
    [SerializeField] private float exitXOffset = 0f;
    [SerializeField] private float exitXFollowSmoothTime = 0.25f;
    [SerializeField] private bool syncCameraMovementY = true;
    [SerializeField] private bool logExitCameraYDebug = true;
    [SerializeField] private bool useNormalCameraWhenMissionArea2ExitsLeft = true;
    [SerializeField] private string exitFeatureSceneName = "SeungHyun2_Restore";

    [Header("Fixed Area Pan Mode")]
    [Tooltip("FixedAreaPan 모드에서 targetPos로 이동하는 시간")]
    public float fixedPanMoveTime = 1.8f;

    [Tooltip("FixedAreaPan 모드에서 줌이 변하는 시간")]
    public float fixedPanZoomTime = 1.8f;

    [Tooltip("FixedAreaPan 모드에서 카메라 이동 곡선")]
    public AnimationCurve fixedPanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("구역을 나갈 때 카메라를 진입 전 위치로 부드럽게 되돌릴지")]
    public bool smoothReturnOnExit = false;

    [Tooltip("구역을 나갈 때 되돌아가는 시간")]
    public float returnMoveTime = 1f;

    [Header("Follow Player X + Fixed Y Mode")]
    [Tooltip("처음 들어온 순간의 카메라-플레이어 X 거리값을 유지합니다.")]
    public bool useEnterXOffset = false;

    [Tooltip("useEnterXOffset이 꺼져 있을 때 플레이어 X 기준으로 더할 오프셋입니다. 0이면 플레이어 중심.")]
    public float playerXOffset = 0f;

    [Tooltip("X축으로 플레이어를 따라가는 부드러움")]
    public float followXSmoothTime = 0.08f;

    [Tooltip("X축 이동 최대 속도")]
    public float followXMaxSpeed = 200f;

    [Tooltip("Y값이 targetPos.y로 고정되기까지 걸리는 시간")]
    public float fixedYMoveTime = 1.2f;

    [Tooltip("FollowPlayerXFixedY 모드에서 줌이 변하는 시간")]
    public float fixedYZoomTime = 1.2f;

    [Header("On Exit - Stop Wind Machine")]
    [Tooltip("영역 퇴장 시 멈출 WindMachineActivationController")]
    public WindMachineActivationController windMachineToStop;
    [Tooltip("영역 퇴장 시 리셋할 CircleHitObject (프로펠러 재타격 가능하게)")]
    public CircleHitObject circleHitObjectToReset;

    private static SH_MissionAreaCamera activeArea;
    private static SH_MissionAreaCamera exitFollowArea;

    private CameraControlState controlState = CameraControlState.None;

    private bool isCameraControl;
    private bool isReturning;

    private Transform player;
    private Transform exitingPlayer;
    private GameObject cameraRig;
    private Camera targetCamera;
    private Collider2D areaCollider;
    private PassThroughExitCameraZoom passThroughExitZoom;

    private Coroutine exitTransitionCoroutine;

    private Vector3 enterCameraPos;
    private Vector3 exitCameraPos;
    private Vector3 smoothTargetPos;

    private Vector3 entryBlendStartPos;
    private float entryBlendStartZoom;
    private float entryBlendTimer;

    private float startZoomSize;
    private float startHalfHeight;

    private float enterX;
    private float exitX;

    private bool isLeftToRight;
    private float yVelocity;
    private float xVelocity;
    private float exitXVelocity;
    private float exitYVelocity;

    private float fixedPanTimer;
    private float followFixedYTimer;
    private float returnTimer;

    private Vector3 returnStartPos;
    private float returnStartZoom;

    private float activePlayerXOffset;

    private void Start()
    {
        RefreshCameraReferences();
        areaCollider = GetComponent<Collider2D>();
        passThroughExitZoom = GetComponent<PassThroughExitCameraZoom>();

        if (areaCollider != null)
        {
            enterX = areaCollider.bounds.min.x;
            exitX = areaCollider.bounds.max.x;
        }

        smoothTargetPos = targetPos;
    }

    private void Update()
    {
        if (isReturning)
        {
            UpdateReturnCamera();
            return;
        }

        if (!isCameraControl || player == null)
            return;

        if (activeArea != this)
            return;

        if (cameraMode == MissionCameraMode.FixedAreaPan)
        {
            ControlFixedAreaPan();
        }
        else if (cameraMode == MissionCameraMode.FollowPlayerXFixedY)
        {
            ControlFollowPlayerXFixedY();
        }
        else
        {
            ControlHorizontalCamera();
        }
    }

    private void RefreshCameraReferences()
    {
        if (cameraRig == null && CameraMovement.Instance != null)
            cameraRig = CameraMovement.Instance.gameObject;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void ControlFixedAreaPan()
    {
        if (cameraRig == null || targetCamera == null)
            return;

        fixedPanTimer += Time.deltaTime;

        float moveT = fixedPanMoveTime <= 0f ? 1f : Mathf.Clamp01(fixedPanTimer / fixedPanMoveTime);
        float zoomT = fixedPanZoomTime <= 0f ? 1f : Mathf.Clamp01(fixedPanTimer / fixedPanZoomTime);

        float curvedMoveT = fixedPanCurve.Evaluate(moveT);
        float curvedZoomT = fixedPanCurve.Evaluate(zoomT);

        Vector3 fixedTarget = new Vector3(
            targetPos.x,
            targetPos.y,
            enterCameraPos.z
        );

        Vector3 desiredPos = Vector3.Lerp(
            enterCameraPos,
            fixedTarget,
            curvedMoveT
        );

        float desiredZoom = Mathf.Lerp(
            startZoomSize,
            finalZoomSize,
            curvedZoomT
        );

        ApplyDesiredCamera(desiredPos, desiredZoom);
    }

    private void ControlFollowPlayerXFixedY()
    {
        if (cameraRig == null || targetCamera == null || player == null)
            return;

        followFixedYTimer += Time.deltaTime;

        float yT = fixedYMoveTime <= 0f ? 1f : Mathf.Clamp01(followFixedYTimer / fixedYMoveTime);
        float zoomT = fixedYZoomTime <= 0f ? 1f : Mathf.Clamp01(followFixedYTimer / fixedYZoomTime);

        float curvedYT = fixedPanCurve.Evaluate(yT);
        float curvedZoomT = fixedPanCurve.Evaluate(zoomT);

        float desiredXRaw = player.position.x + activePlayerXOffset;

        float desiredX = Mathf.SmoothDamp(
            cameraRig.transform.position.x,
            desiredXRaw,
            ref xVelocity,
            followXSmoothTime,
            followXMaxSpeed
        );

        float desiredY = Mathf.Lerp(
            enterCameraPos.y,
            targetPos.y,
            curvedYT
        );

        Vector3 desiredPos = new Vector3(
            desiredX,
            desiredY,
            enterCameraPos.z
        );

        float desiredZoom = Mathf.Lerp(
            startZoomSize,
            finalZoomSize,
            curvedZoomT
        );

        ApplyDesiredCamera(desiredPos, desiredZoom);
    }

    private void ControlHorizontalCamera()
    {
        if (cameraRig == null || targetCamera == null)
            return;

        Vector3 activeTargetPos = GetActiveTargetPos();

        float playerX = player.position.x;

        float leftZoomEndX = targetPos.x - maxSizeXPos;
        float rightZoomStartX = targetPos.x + maxSizeXPos;

        Vector3 activeExitCameraPos = GetActiveExitCameraPos(activeTargetPos);

        Vector3 desiredPos = cameraRig.transform.position;
        float desiredZoomT = 0f;

        if (isLeftToRight)
        {
            if (playerX < leftZoomEndX)
            {
                float t = Mathf.InverseLerp(enterX, leftZoomEndX, playerX);
                float smoothT = Smooth(t);

                desiredPos = Vector3.Lerp(enterCameraPos, activeTargetPos, smoothT);
                desiredZoomT = smoothT;
            }
            else if (playerX <= rightZoomStartX)
            {
                desiredPos = activeTargetPos;
                desiredZoomT = 1f;
            }
            else
            {
                float t = Mathf.InverseLerp(rightZoomStartX, exitX, playerX);
                float smoothT = Smooth(t);

                desiredPos = Vector3.Lerp(activeTargetPos, activeExitCameraPos, smoothT);
                desiredZoomT = 1f - smoothT;
            }
        }
        else
        {
            if (playerX > rightZoomStartX)
            {
                float t = Mathf.InverseLerp(exitX, rightZoomStartX, playerX);
                float smoothT = Smooth(t);

                desiredPos = Vector3.Lerp(enterCameraPos, activeTargetPos, smoothT);
                desiredZoomT = smoothT;
            }
            else if (playerX >= leftZoomEndX)
            {
                desiredPos = activeTargetPos;
                desiredZoomT = 1f;
            }
            else
            {
                float t = Mathf.InverseLerp(leftZoomEndX, enterX, playerX);
                float smoothT = Smooth(t);

                desiredPos = Vector3.Lerp(activeTargetPos, activeExitCameraPos, smoothT);
                desiredZoomT = 1f - smoothT;
            }
        }

        desiredZoomT = Mathf.Clamp01(desiredZoomT);

        float desiredZoom = Mathf.Lerp(
            startZoomSize,
            finalZoomSize,
            desiredZoomT
        );

        ApplyDesiredCamera(desiredPos, desiredZoom);
    }

    private void ApplyDesiredCamera(Vector3 desiredPos, float desiredZoom)
    {
        if (cameraRig == null || targetCamera == null)
            return;

        if (useSmoothEntryBlend && entryBlendTime > 0f && entryBlendTimer < entryBlendTime)
        {
            controlState = CameraControlState.Entering;
            entryBlendTimer += Time.deltaTime;

            float t = Mathf.Clamp01(entryBlendTimer / entryBlendTime);
            float curvedT = entryBlendCurve.Evaluate(t);

            Vector3 blendedPos = Vector3.Lerp(
                entryBlendStartPos,
                desiredPos,
                curvedT
            );

            ApplyCameraRigPosition(blendedPos);

            targetCamera.fieldOfView = Mathf.Lerp(
                entryBlendStartZoom,
                desiredZoom,
                curvedT
            );

            if (t >= 1f)
                controlState = CameraControlState.Inside;
        }
        else
        {
            if (controlState == CameraControlState.Entering)
                controlState = CameraControlState.Inside;

            ApplyCameraRigPosition(desiredPos);
            targetCamera.fieldOfView = desiredZoom;
        }
    }

    private Vector3 GetActiveExitCameraPos(Vector3 activeTargetPos)
    {
        Vector3 activeExitCameraPos = exitCameraPos;

        if (useCurrentPlayerYOnExit && player != null)
        {
            activeExitCameraPos.y = CalculateExitTargetY(player);
        }
        else if (useSmoothYFollow && !fixPosY)
        {
            activeExitCameraPos.y = activeTargetPos.y;
        }

        return activeExitCameraPos;
    }

    private float CalculateExitTargetY(Transform targetPlayer)
    {
        if (targetPlayer == null)
            return exitCameraPos.y;

        return targetPlayer.position.y + GetExitYOffset();
    }

    private void ApplyCameraRigPosition(Vector3 position)
    {
        if (cameraRig == null)
            return;

        cameraRig.transform.position = position;
        SyncCameraMovementY(position.y);
    }

    private void SyncCameraMovementY(float cameraY)
    {
        if (!syncCameraMovementY || CameraMovement.Instance == null)
            return;

        CameraMovement.Instance.SetCameraRigY(cameraY);
    }

    private Vector3 GetActiveTargetPos()
    {
        if (fixPosY)
        {
            smoothTargetPos = targetPos;
            return targetPos;
        }

        float desiredY = player.position.y + playerYOffset;

        if (!useSmoothYFollow)
        {
            targetPos.y = desiredY;
            return targetPos;
        }

        smoothTargetPos.x = targetPos.x;
        smoothTargetPos.z = targetPos.z;

        smoothTargetPos.y = Mathf.SmoothDamp(
            smoothTargetPos.y,
            desiredY,
            ref yVelocity,
            yFollowSmoothTime,
            yMaxFollowSpeed
        );

        return smoothTargetPos;
    }

    private float Smooth(float t)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }

    private void UpdateReturnCamera()
    {
        if (cameraRig == null || targetCamera == null)
        {
            isReturning = false;
            return;
        }

        returnTimer += Time.deltaTime;

        float t = returnMoveTime <= 0f ? 1f : Mathf.Clamp01(returnTimer / returnMoveTime);
        float curvedT = fixedPanCurve.Evaluate(t);

        Vector3 returnPos = Vector3.Lerp(
            returnStartPos,
            enterCameraPos,
            curvedT
        );

        ApplyCameraRigPosition(returnPos);

        targetCamera.fieldOfView = Mathf.Lerp(
            returnStartZoom,
            startZoomSize,
            curvedT
        );

        if (t >= 1f)
        {
            isReturning = false;
            controlState = CameraControlState.None;

            if (activeArea == this)
                activeArea = null;

            if (CameraMovement.Instance != null)
                CameraMovement.Instance.isMovingEvent = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        RefreshCameraReferences();

        if (cameraRig == null || targetCamera == null)
            return;

        if (controlState == CameraControlState.Exiting)
            StopExitTransition(true);

        CancelOtherExitFollowAreas();

        activeArea = this;

        player = collision.transform;
        exitingPlayer = null;
        isCameraControl = true;
        isReturning = false;
        controlState = CameraControlState.Entering;

        enterCameraPos = cameraRig.transform.position;
        startZoomSize = targetCamera.fieldOfView;

        entryBlendStartPos = cameraRig.transform.position;
        entryBlendStartZoom = targetCamera.fieldOfView;
        entryBlendTimer = 0f;

        startHalfHeight =
            groundDistance * Mathf.Tan(startZoomSize * 0.5f * Mathf.Deg2Rad);

        fixedPanTimer = 0f;
        followFixedYTimer = 0f;
        returnTimer = 0f;

        yVelocity = 0f;
        xVelocity = 0f;
        exitXVelocity = 0f;
        exitYVelocity = 0f;

        if (cameraMode == MissionCameraMode.HorizontalByPlayerX)
        {
            smoothTargetPos = targetPos;

            if (!fixPosY && useSmoothYFollow)
            {
                smoothTargetPos.y = enterCameraPos.y;
                yVelocity = 0f;
            }

            exitCameraPos = new Vector3(
                targetPos.x + (targetPos.x - enterCameraPos.x),
                CalculateExitTargetY(player),
                enterCameraPos.z
            );

            isLeftToRight = player.position.x < targetPos.x;
        }
        else if (cameraMode == MissionCameraMode.FollowPlayerXFixedY)
        {
            if (useEnterXOffset)
            {
                activePlayerXOffset = enterCameraPos.x - player.position.x;
            }
            else
            {
                activePlayerXOffset = playerXOffset;
            }
        }

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        RefreshCameraReferences();

        if (windMachineToStop != null)
            windMachineToStop.StopGradual();

        if (circleHitObjectToReset != null)
            circleHitObjectToReset.Reset();

        if (activeArea != this)
            return;

        if (!CanUseExitCenterFeature() || ShouldUseNormalCameraOnExit(collision.transform))
        {
            StopExitTransition(false);
            exitingPlayer = null;
            player = null;
            isCameraControl = false;
            isReturning = false;
            controlState = CameraControlState.None;

            if (activeArea == this)
                activeArea = null;

            if (CameraMovement.Instance != null)
                CameraMovement.Instance.isMovingEvent = false;

            if (logExitCameraYDebug)
                Debug.Log($"{gameObject.name} Exit Camera Follow Skipped / using normal camera");

            return;
        }

        exitingPlayer = collision.transform;
        isCameraControl = false;
        isReturning = false;
        controlState = CameraControlState.Exiting;
        exitXVelocity = 0f;
        exitYVelocity = 0f;

        if (exitTransitionCoroutine != null)
            StopCoroutine(exitTransitionCoroutine);

        if (cameraMode == MissionCameraMode.FixedAreaPan && smoothReturnOnExit)
        {
            isReturning = true;
            returnTimer = 0f;
            returnStartPos = cameraRig.transform.position;
            returnStartZoom = targetCamera.fieldOfView;
            return;
        }

        exitFollowArea = this;
        exitTransitionCoroutine = StartCoroutine(ExitTransitionRoutine(exitingPlayer));
    }

    private IEnumerator ExitTransitionRoutine(Transform transitionPlayer)
    {
        if (cameraRig == null || targetCamera == null)
            yield break;

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = true;

        Vector3 startCameraPos = cameraRig.transform.position;
        float startPlayerY = transitionPlayer != null ? transitionPlayer.position.y : startCameraPos.y - GetExitYOffset();

        if (logExitCameraYDebug)
        {
            Debug.Log(
                $"{gameObject.name} Exit Camera Follow Start / currentCameraX: {startCameraPos.x:F3}, currentCameraY: {startCameraPos.y:F3}, playerY: {startPlayerY:F3}"
            );
        }

        float elapsed = 0f;
        float duration = GetExitFollowDuration();
        float centeringDuration = GetExitCenteringDuration();
        Vector3 currentPos = startCameraPos;
        Vector2 startOffsetFromPlayer = transitionPlayer != null
            ? new Vector2(
                startCameraPos.x - transitionPlayer.position.x,
                startCameraPos.y - transitionPlayer.position.y
            )
            : exitCenterOffset;

        while (elapsed < duration)
        {
            if (controlState != CameraControlState.Exiting || transitionPlayer == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / centeringDuration);
            float centerT = centerPlayerOnExit ? exitCenteringCurve.Evaluate(t) : 1f;

            currentPos = cameraRig.transform.position;
            Vector2 blendedOffset = Vector2.Lerp(
                startOffsetFromPlayer,
                centerPlayerOnExit ? exitCenterOffset : new Vector2(exitXOffset, exitYOffset),
                centerT
            );

            if (centerPlayerDuringExitZoom)
            {
                float desiredX = transitionPlayer.position.x + blendedOffset.x;
                currentPos.x = Mathf.SmoothDamp(
                    currentPos.x,
                    desiredX,
                    ref exitXVelocity,
                    exitXFollowSmoothTime,
                    followXMaxSpeed
                );
            }

            float desiredY = useCurrentPlayerYOnExit
                ? transitionPlayer.position.y + blendedOffset.y
                : exitCameraPos.y;

            currentPos.y = Mathf.SmoothDamp(
                currentPos.y,
                desiredY,
                ref exitYVelocity,
                exitYFollowSmoothTime,
                yMaxFollowSpeed
            );

            ApplyCameraRigPosition(currentPos);

            yield return null;
        }

        if (!holdExitCenterUntilNextArea)
            CompleteExitTransition(transitionPlayer);
    }

    private float GetExitFollowDuration()
    {
        if (holdExitCenterUntilNextArea)
            return float.PositiveInfinity;

        float duration = Mathf.Max(0.001f, exitTransitionTime);

        if (passThroughExitZoom != null)
            duration = Mathf.Max(duration, passThroughExitZoom.TotalZoomDuration);

        return duration + Mathf.Max(0f, exitCenterHoldTime);
    }

    private float GetExitCenteringDuration()
    {
        if (gameObject.name == "MissionArea (2)")
            return Mathf.Max(0.001f, missionArea2ExitCenteringTime);

        if (exitCenteringTime > 0f)
            return Mathf.Max(0.001f, exitCenteringTime);

        float zoomDuration = exitTransitionTime;

        if (passThroughExitZoom != null)
            zoomDuration = Mathf.Max(0.001f, passThroughExitZoom.TotalZoomDuration);

        return Mathf.Max(0.001f, zoomDuration);
    }

    private float GetExitXOffset()
    {
        return centerPlayerOnExit ? exitCenterOffset.x : exitXOffset;
    }

    private bool ShouldUseNormalCameraOnExit(Transform targetPlayer)
    {
        if (!useNormalCameraWhenMissionArea2ExitsLeft)
            return false;

        if (targetPlayer == null || gameObject.name != "MissionArea (2)")
            return false;

        float centerX = transform.position.x;

        if (areaCollider != null)
            centerX = areaCollider.bounds.center.x;

        return targetPlayer.position.x < centerX;
    }

    private bool CanUseExitCenterFeature()
    {
        if (SceneManager.GetActiveScene().name != exitFeatureSceneName)
            return false;

        switch (gameObject.name)
        {
            case "MissionArea (2)":
            case "MissionArea (3)":
            case "MissionArea (4)":
            case "MissionArea (5)":
                return true;
            default:
                return false;
        }
    }

    private float GetExitYOffset()
    {
        return centerPlayerOnExit ? exitCenterOffset.y : exitYOffset;
    }

    private void CompleteExitTransition(Transform transitionPlayer)
    {
        float finalCameraX = cameraRig != null ? cameraRig.transform.position.x : 0f;
        float finalCameraY = cameraRig != null ? cameraRig.transform.position.y : 0f;

        if (logExitCameraYDebug)
        {
            Debug.Log(
                $"{gameObject.name} Exit Camera Follow Complete / finalCameraX: {finalCameraX:F3}, finalCameraY: {finalCameraY:F3}"
            );
        }

        exitTransitionCoroutine = null;
        exitingPlayer = null;
        player = null;
        controlState = CameraControlState.None;

        if (activeArea == this)
            activeArea = null;

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = false;
    }

    private void CancelOtherExitFollowAreas()
    {
        if (exitFollowArea != null && exitFollowArea != this)
            exitFollowArea.StopExitTransition(true);

        exitFollowArea = null;
    }

    private void StopExitTransition(bool cancelledByReenter)
    {
        if (exitTransitionCoroutine != null)
        {
            StopCoroutine(exitTransitionCoroutine);
            exitTransitionCoroutine = null;
        }

        if (cancelledByReenter && logExitCameraYDebug)
            Debug.Log($"{gameObject.name} Exit Camera Follow Cancelled By Re-enter");

        exitingPlayer = null;
        exitXVelocity = 0f;
        exitYVelocity = 0f;

        if (exitFollowArea == this)
            exitFollowArea = null;
    }

    public static void ReleaseExitFollowToNormalCamera()
    {
        if (exitFollowArea != null)
            exitFollowArea.StopExitTransition(false);

        exitFollowArea = null;

        if (activeArea != null && activeArea.controlState == CameraControlState.Exiting)
        {
            activeArea.StopExitTransition(false);
            activeArea.isCameraControl = false;
            activeArea.isReturning = false;
            activeArea.player = null;
            activeArea.exitingPlayer = null;
            activeArea.controlState = CameraControlState.None;
            activeArea = null;
        }
    }
}
