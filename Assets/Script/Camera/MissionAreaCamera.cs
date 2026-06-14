using UnityEngine;

public class MissionAreaCamera : MonoBehaviour
{
    public enum MissionCameraMode
    {
        HorizontalByPlayerX,
        FixedAreaPan
    }

    [Header("Mode")]
    [Tooltip("기존 좌우 진행 구역은 HorizontalByPlayerX, 구름 위 상승 구역은 FixedAreaPan")]
    public MissionCameraMode cameraMode = MissionCameraMode.HorizontalByPlayerX;

    [Header("Target")]
    public Vector3 targetPos;

    [Tooltip("targetPos.x 기준 좌우 거리. HorizontalByPlayerX 모드에서 사용")]
    public float maxSizeXPos = 5f;

    [Header("Zoom")]
    public float finalZoomSize;

    [Tooltip("카메라와 바닥 기준 Z 거리")]
    public float groundDistance = 28f;

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

    private bool isCameraControl;
    private bool isReturning;

    private Transform player;
    private GameObject cameraRig;
    private Camera targetCamera;
    private Collider2D areaCollider;

    private Vector3 enterCameraPos;
    private Vector3 exitCameraPos;
    private Vector3 smoothTargetPos;

    private float startZoomSize;
    private float startHalfHeight;

    private float enterX;
    private float exitX;

    private bool isLeftToRight;
    private float yVelocity;

    private float fixedPanTimer;
    private float returnTimer;

    private Vector3 returnStartPos;
    private float returnStartZoom;

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

        if (cameraMode == MissionCameraMode.FixedAreaPan)
        {
            ControlFixedAreaPan();
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

        cameraRig.transform.position = Vector3.Lerp(
            enterCameraPos,
            fixedTarget,
            curvedMoveT
        );

        targetCamera.fieldOfView = Mathf.Lerp(
            startZoomSize,
            finalZoomSize,
            curvedZoomT
        );
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

        if (isLeftToRight)
        {
            if (playerX < leftZoomEndX)
            {
                float t = Mathf.InverseLerp(enterX, leftZoomEndX, playerX);
                float smoothT = Smooth(t);

                ApplyCamera(
                    Vector3.Lerp(enterCameraPos, activeTargetPos, smoothT),
                    smoothT
                );
            }
            else if (playerX <= rightZoomStartX)
            {
                ApplyCamera(activeTargetPos, 1f);
            }
            else
            {
                float t = Mathf.InverseLerp(rightZoomStartX, exitX, playerX);
                float smoothT = Smooth(t);

                ApplyCamera(
                    Vector3.Lerp(activeTargetPos, activeExitCameraPos, smoothT),
                    1f - smoothT
                );
            }
        }
        else
        {
            if (playerX > rightZoomStartX)
            {
                float t = Mathf.InverseLerp(exitX, rightZoomStartX, playerX);
                float smoothT = Smooth(t);

                ApplyCamera(
                    Vector3.Lerp(enterCameraPos, activeTargetPos, smoothT),
                    smoothT
                );
            }
            else if (playerX >= leftZoomEndX)
            {
                ApplyCamera(activeTargetPos, 1f);
            }
            else
            {
                float t = Mathf.InverseLerp(leftZoomEndX, enterX, playerX);
                float smoothT = Smooth(t);

                ApplyCamera(
                    Vector3.Lerp(activeTargetPos, activeExitCameraPos, smoothT),
                    1f - smoothT
                );
            }
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

    private void ApplyCamera(Vector3 basePos, float zoomT)
    {
        if (cameraRig == null || targetCamera == null)
            return;

        zoomT = Mathf.Clamp01(zoomT);

        float currentFov = Mathf.Lerp(startZoomSize, finalZoomSize, zoomT);
        targetCamera.fieldOfView = currentFov;

        float currentHalfHeight =
            groundDistance * Mathf.Tan(currentFov * 0.5f * Mathf.Deg2Rad);

        float yOffset = currentHalfHeight - startHalfHeight;

        // 필요할 때만 사용
        // basePos.y += yOffset;

        cameraRig.transform.position = basePos;
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

        player = collision.transform;
        isCameraControl = true;
        isReturning = false;

        enterCameraPos = cameraRig.transform.position;
        startZoomSize = targetCamera.fieldOfView;

        startHalfHeight =
            groundDistance * Mathf.Tan(startZoomSize * 0.5f * Mathf.Deg2Rad);

        fixedPanTimer = 0f;

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

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        isCameraControl = false;
        player = null;

        if (cameraMode == MissionCameraMode.FixedAreaPan && smoothReturnOnExit)
        {
            isReturning = true;
            returnTimer = 0f;
            returnStartPos = cameraRig.transform.position;
            returnStartZoom = targetCamera.fieldOfView;
        }
        else
        {
            if (CameraMovement.Instance != null)
                CameraMovement.Instance.isMovingEvent = false;
        }
    }
}