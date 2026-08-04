using UnityEngine;

public class MissionAreaCamera : MonoBehaviour
{
    public enum MissionCameraMode
    {
        HorizontalByPlayerX,
        FixedAreaPan,
        HorizontalByPlayerXWithExit
    }

    [Header("Mode")]
    [Tooltip("���� �¿� ���� ������ HorizontalByPlayerX, ���� �� ��� ������ FixedAreaPan")]
    public MissionCameraMode cameraMode = MissionCameraMode.HorizontalByPlayerX;

    [Header("Target")]
    public Vector3 targetPos;

    [Tooltip("targetPos.x ���� �¿� �Ÿ�. HorizontalByPlayerX ��忡�� ���")]
    public float maxSizeXPos = 5f;

    [Header("Zoom")]
    public float finalZoomSize;

    [Tooltip("ī�޶�� �ٴ� ���� Z �Ÿ�")]
    public float groundDistance = 28f;

    [Header("Y Follow - Horizontal Mode")]
    public bool fixPosY = false;

    [Tooltip("���� �¿� ���� ��忡�� �÷��̾�� ī�޶� �󸶳� ���� ����")]
    public float playerYOffset = 15.13f;

    [Tooltip("üũ�� ���������� �÷��̾� Y���� �ε巴�� ���󰩴ϴ�.")]
    public bool useSmoothYFollow = false;

    public float yFollowSmoothTime = 0.35f;
    public float yMaxFollowSpeed = 50f;

    [Header("Fixed Area Pan Mode")]
    [Tooltip("FixedAreaPan ��忡�� targetPos�� �̵��ϴ� �ð�")]
    public float fixedPanMoveTime = 1.8f;

    [Tooltip("FixedAreaPan ��忡�� ���� ���ϴ� �ð�")]
    public float fixedPanZoomTime = 1.8f;

    [Tooltip("FixedAreaPan ��忡�� ī�޶� �̵� �")]
    public AnimationCurve fixedPanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("������ ���� �� ī�޶� ���� �� ��ġ�� �ε巴�� �ǵ�����")]
    public bool smoothReturnOnExit = false;

    [Tooltip("������ ���� �� �ǵ��ư��� �ð�")]
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
            // CameraMovement.isMovingEvent는 여러 스크립트가 공유하는 전역 플래그라 다른
            // 스크립트가 중간에 false로 되돌릴 수 있다. 이 컴포넌트가 카메라를 붙잡고 있는
            // 동안은 매 프레임 다시 true로 재확인해서 즉시 되찾아온다.
            if (CameraMovement.Instance != null)
                CameraMovement.Instance.isMovingEvent = true;

            UpdateReturnCamera();
            return;
        }

        if (!isCameraControl || player == null)
            return;

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = true;

        if (cameraMode == MissionCameraMode.FixedAreaPan)
        {
            ControlFixedAreaPan();
        }
        else if (cameraMode == MissionCameraMode.HorizontalByPlayerXWithExit)
        {
            ControlHorizontalCameraWithExit();
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

    private void ControlHorizontalCameraWithExit()
    {
        if (cameraRig == null || targetCamera == null)
            return;

        Vector3 activeTargetPos = GetActiveTargetPos();

        float playerX = player.position.x;

        // isLeftToRight에 따라 진입 경계 -> 이탈 경계 방향을 정하고, 중앙 밴드 없이
        // 이 전체 구간을 하나의 연속된 보간으로 처리한다 (이탈 직전까지 계속 targetPos/finalZoomSize에 가까워짐).
        float entryBoundX = isLeftToRight ? enterX : exitX;
        float exitBoundX = isLeftToRight ? exitX : enterX;

        float t = Mathf.InverseLerp(entryBoundX, exitBoundX, playerX);
        float smoothT = Smooth(t);

        ApplyCamera(
            Vector3.Lerp(enterCameraPos, activeTargetPos, smoothT),
            smoothT
        );
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

        // �ʿ��� ���� ���
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

        if (cameraMode == MissionCameraMode.HorizontalByPlayerX ||
            cameraMode == MissionCameraMode.HorizontalByPlayerXWithExit)
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