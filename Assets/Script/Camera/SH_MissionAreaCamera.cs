using UnityEngine;

public class SH_MissionAreaCamera : MonoBehaviour
{
    public enum MissionCameraMode
    {
        HorizontalByPlayerX,
        FixedAreaPan,
        FollowPlayerXFixedY
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
    [Tooltip("구역 진입 시 현재 카메라에서 목표 카메라로 부드럽게 전환합니다.")]
    public bool useSmoothEntryBlend = true;

    [Tooltip("구역 진입 후 목표 카메라 상태로 넘어가는 시간")]
    public float entryBlendTime = 1.0f;

    [Tooltip("구역 진입 전환 곡선")]
    public AnimationCurve entryBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Y Follow - Horizontal Mode")]
    public bool fixPosY = false;

    [Tooltip("기존 좌우 진행 모드에서 플레이어보다 카메라가 얼마나 위를 볼지")]
    public float playerYOffset = 15.13f;

    [Tooltip("체크한 구역에서만 플레이어 Y값을 부드럽게 따라갑니다.")]
    public bool useSmoothYFollow = false;

    public float yFollowSmoothTime = 0.35f;
    public float yMaxFollowSpeed = 50f;

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
    [Tooltip("켜면 진입 순간의 카메라-플레이어 X 거리차를 유지합니다.")]
    public bool useEnterXOffset = false;

    [Tooltip("useEnterXOffset을 끄면 이 값을 플레이어 X 오프셋으로 사용합니다. 0이면 플레이어 중심.")]
    public float playerXOffset = 0f;

    [Tooltip("X축이 플레이어를 따라가는 부드러움")]
    public float followXSmoothTime = 0.08f;

    [Tooltip("X축 추적 최대 속도")]
    public float followXMaxSpeed = 200f;

    [Tooltip("Y축이 targetPos.y로 고정되기까지 걸리는 시간")]
    public float fixedYMoveTime = 1.2f;

    [Tooltip("FollowPlayerXFixedY 모드에서 줌이 변하는 시간")]
    public float fixedYZoomTime = 1.2f;

    private static SH_MissionAreaCamera activeArea;

    private bool isCameraControl;
    private bool isReturning;

    private Transform player;
    private GameObject cameraRig;
    private Camera targetCamera;
    private Collider2D areaCollider;

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

    private float fixedPanTimer;
    private float followFixedYTimer;
    private float returnTimer;

    private Vector3 returnStartPos;
    private float returnStartZoom;

    private float activePlayerXOffset;

    private void Start()
    {
        if (CameraMovement.Instance != null)
            cameraRig = CameraMovement.Instance.gameObject;

        targetCamera = Camera.main;
        areaCollider = GetComponent<Collider2D>();

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

        Vector3 activeExitCameraPos = exitCameraPos;

        if (useSmoothYFollow && !fixPosY)
            activeExitCameraPos.y = activeTargetPos.y;

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
            entryBlendTimer += Time.deltaTime;

            float t = Mathf.Clamp01(entryBlendTimer / entryBlendTime);
            float curvedT = entryBlendCurve.Evaluate(t);

            cameraRig.transform.position = Vector3.Lerp(
                entryBlendStartPos,
                desiredPos,
                curvedT
            );

            targetCamera.fieldOfView = Mathf.Lerp(
                entryBlendStartZoom,
                desiredZoom,
                curvedT
            );
        }
        else
        {
            cameraRig.transform.position = desiredPos;
            targetCamera.fieldOfView = desiredZoom;
        }
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

        cameraRig.transform.position = Vector3.Lerp(
            returnStartPos,
            enterCameraPos,
            curvedT
        );

        targetCamera.fieldOfView = Mathf.Lerp(
            returnStartZoom,
            startZoomSize,
            curvedT
        );

        if (t >= 1f)
        {
            isReturning = false;

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

        if (cameraRig == null && CameraMovement.Instance != null)
            cameraRig = CameraMovement.Instance.gameObject;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraRig == null || targetCamera == null)
            return;

        activeArea = this;

        player = collision.transform;
        isCameraControl = true;
        isReturning = false;

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
                enterCameraPos.y,
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

        isCameraControl = false;
        player = null;

        if (activeArea != this)
            return;

        if (cameraMode == MissionCameraMode.FixedAreaPan && smoothReturnOnExit)
        {
            isReturning = true;
            returnTimer = 0f;
            returnStartPos = cameraRig.transform.position;
            returnStartZoom = targetCamera.fieldOfView;
        }
        else
        {
            activeArea = null;

            if (CameraMovement.Instance != null)
                CameraMovement.Instance.isMovingEvent = false;
        }
    }
}