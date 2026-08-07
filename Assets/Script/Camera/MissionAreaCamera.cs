using System.Collections.Generic;
using UnityEngine;

public class MissionAreaCamera : MonoBehaviour
{
    public enum MissionCameraMode
    {
        HorizontalByPlayerX,
        FixedAreaPan,
        HorizontalByPlayerXWithExit,
        FixedByPlayer
    }

    [Header("Mode")]
    [Tooltip("���� �¿� ���� ������ HorizontalByPlayerX, ���� �� ��� ������ FixedAreaPan")]
    public MissionCameraMode cameraMode = MissionCameraMode.HorizontalByPlayerX;

    [Header("Priority")]
    [Tooltip("여러 MissionArea가 겹쳐서 플레이어가 동시에 여러 트리거 안에 있을 때, 이 값이 가장 큰 영역이 카메라를 소유합니다. 동점이면 먼저 겹친(활성화된) 쪽이 유지됩니다.")]
    public int priority = 0;

    [Header("Target")]
    public Vector3 targetPos;

    [Tooltip("targetPos.x ���� �¿� �Ÿ�. HorizontalByPlayerX ��忡�� ���")]
    public float maxSizeXPos = 5f;

    [Header("Zoom")]
    public float finalZoomSize;

    [Tooltip("ī�޶�� �ٴ� ���� Z �Ÿ�")]
    public float groundDistance = 28f;

    [Tooltip("진입 시 줌의 시작점(startZoomSize)을 CameraMovement.defaultFieldOfView(고정 기준값)로 캡처할지. 켜면 인접한 다른 MissionAreaCamera가 아직 복귀 중이라 값이 덜 풀린 상태를 잘못 캡처하는 걸 막아주지만, 그 순간 실제 카메라 FOV와 defaultFieldOfView가 다르면 진입 시 살짝 튀어 보일 수 있습니다. 끄면 기존처럼 그 순간의 실제 카메라 FOV를 그대로 시작점으로 씁니다.")]
    public bool useCameraMovementDefaultZoom = true;

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

    [Tooltip("영역을 나갈 때 카메라 줌(Field of View)과 위치를 원래 상태로 서서히 되돌릴지. FixedAreaPan과 FixedByPlayer 모드에 적용됩니다. FixedByPlayer는 원래 항상 켜진 것처럼 동작했으므로, 그 동작을 유지하려면 이 옵션을 켜두세요.")]
    public bool smoothReturnOnExit = false;

    [Tooltip("������ ���� �� �ǵ��ư��� �ð�")]
    public float returnMoveTime = 1f;

    [Header("Exit Zoom To Default")]
    [Tooltip("영역을 나갈 때 카메라 FieldOfView를 CameraMovement.defaultFieldOfView(전역 고정 기본값)로 되돌릴지. smoothReturnOnExit와는 완전히 독립적으로 동작한다 - smoothReturnOnExit가 꺼져있어도(위치는 안 돌아가도) 줌만 따로 기본값으로 되돌릴 수 있고, 둘 다 켜져 있으면 위치는 smoothReturnOnExit가, 줌은 이 기능이 전담한다(서로 값을 덮어쓰지 않음).")]
    public bool exitCameraDefaultZoom = false;

    [Tooltip("영역을 나간 뒤 FieldOfView가 defaultFieldOfView에 도달하기까지 걸리는 시간(초).")]
    public float exitCameraDefaultZoomDuration = 1f;

    // 현재 카메라를 소유 중인 인스턴스. activeAreas 중 priority가 가장 높은 쪽으로
    // ReevaluateOwnership()이 갱신한다.
    private static MissionAreaCamera activeInstance;

    // 지금 플레이어와 겹쳐 있는(트리거 안에 있는) 모든 MissionAreaCamera. 두 영역이 겹쳐
    // 배치되어 플레이어가 동시에 여러 트리거 안에 있을 수 있는 상황을 다루기 위한 목록이다 -
    // 소유권은 항상 이 목록 중 priority가 가장 높은 쪽에게 있고, 진입/이탈로 목록이 바뀔
    // 때마다 ReevaluateOwnership()이 다시 계산한다.
    private static readonly List<MissionAreaCamera> activeAreas = new List<MissionAreaCamera>();

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

    private bool isExitingToDefaultZoom;
    private float exitZoomTimer;
    private float exitZoomStartValue;

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
        // isReturning/isCameraControl과 완전히 독립적으로 매 프레임 먼저 처리한다 - isReturning
        // 블록 아래에 있는 조기 return 때문에 이 갱신이 묻히지 않게 하기 위함. smoothReturnOnExit가
        // 꺼져 있어도(또는 isReturning이 아예 없어도) 줌만 단독으로 기본값으로 되돌아갈 수 있다.
        if (isExitingToDefaultZoom)
            UpdateExitDefaultZoom();

        if (isReturning)
        {
            // FixedByPlayer는 나가는 순간까지 카메라가 플레이어를 1:1로 계속 따라오고 있었으므로
            // 이미 정상 위치 근처다. 위치를 여기서 붙잡고 있으면 오히려 진입 당시의 옛 위치로
            // 갔다가(플레이어에게서 멀어짐) 반환이 끝난 뒤 CameraMovement가 다시 따라잡는
            // 부자연스러운 왕복이 생긴다. 그래서 이 모드만 위치 제어를 즉시 CameraMovement에게
            // 돌려주고 줌만 서서히 되돌린다. 다른 모드는 위치 자체를 enterCameraPos로 되돌려야
            // 하므로 그동안 계속 CameraMovement.isMovingEvent를 true로 재확인해서 follow를 막는다
            // (여러 스크립트가 공유하는 전역 플래그라 중간에 false로 되돌아갈 수 있어서).
            bool holdCameraPosition = cameraMode != MissionCameraMode.FixedByPlayer;

            if (CameraMovement.Instance != null)
            {
                // CameraMovement가 다시 팔로우를 시작하기 전에, 그 내부 목표 Y(CameraPosY)를
                // 지금 카메라 리그가 실제로 있는 위치로 동기화한다. 안 하면 CameraPosY가
                // 이 영역에 들어오기 이전의 오래된 값을 그대로 들고 있다가, isMovingEvent가
                // false가 되는 순간 그 낡은 Y로 확 튀는 SmoothDamp가 발생한다.
                if (!holdCameraPosition && cameraRig != null)
                    CameraMovement.Instance.SetCameraRigY(cameraRig.transform.position.y);

                CameraMovement.Instance.isMovingEvent = holdCameraPosition;
            }

            UpdateReturnCamera();
            return;
        }

        // activeInstance != this: 이 인스턴스가 자기 트리거 안에 여전히 있어도(isCameraControl
        // true), 겹쳐 있는 다른 MissionArea가 priority 경쟁에서 이겨 소유권을 가져갔다면 더
        // 이상 카메라에 쓰지 않는다 - 두 스크립트가 같은 카메라를 매 프레임 서로 덮어써서
        // 떨리는 것을 막기 위함.
        if (!isCameraControl || player == null || activeInstance != this)
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
        else if (cameraMode == MissionCameraMode.FixedByPlayer)
        {
            ControlFixedByPlayer();
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

    // FixedAreaPan과 같은 fixedPanMoveTime/fixedPanZoomTime/fixedPanCurve 타이밍을 쓰지만,
    // 목표 위치가 Inspector에 고정된 targetPos가 아니라 플레이어의 현재 위치다. moveT가
    // fixedPanMoveTime 뒤에 1로 클램프된 이후에도 이 메서드는 Update()에서 매 프레임
    // 계속 호출되므로, Lerp(enterCameraPos, playerFollowPos, 1) == playerFollowPos가 되어
    // 그 이후로는 플레이어를 그대로 따라간다 - 영역을 벗어나 OnTriggerExit2D가 호출될
    // 때까지 계속.
    private void ControlFixedByPlayer()
    {
        if (cameraRig == null || targetCamera == null)
            return;

        fixedPanTimer += Time.deltaTime;

        float moveT = fixedPanMoveTime <= 0f ? 1f : Mathf.Clamp01(fixedPanTimer / fixedPanMoveTime);
        float zoomT = fixedPanZoomTime <= 0f ? 1f : Mathf.Clamp01(fixedPanTimer / fixedPanZoomTime);

        float curvedMoveT = fixedPanCurve.Evaluate(moveT);
        float curvedZoomT = fixedPanCurve.Evaluate(zoomT);

        // 수평 추적 모드들과 동일하게 GetActiveTargetPos()를 통해 playerYOffset(및
        // useSmoothYFollow)이 반영된 Y를 구한다. X는 이 모드 특성상 항상 플레이어를
        // 그대로 따라가므로 GetActiveTargetPos()의 X는 쓰지 않는다.
        float followY = GetActiveTargetPos().y;
        Vector3 playerFollowPos = new Vector3(player.position.x, followY, enterCameraPos.z);

        cameraRig.transform.position = Vector3.Lerp(
            enterCameraPos,
            playerFollowPos,
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

        // exitCameraPos.x/.y는 OnTriggerEnter2D 시점에 딱 한 번 계산된 스냅샷이다 (X는
        // "진입 시 카메라 위치를 targetPos 기준으로 반사"한 추정치, Y는 진입 직전 카메라 Y).
        // targetPos가 콜라이더 중심과 정확히 일치하지 않거나 진입 시점 카메라 팔로우가
        // 조금이라도 지연되어 있으면 이 추정이 어긋나고, 미러링 특성상 그 오차가 배로
        // 증폭되어 이탈 경계 근처에서 플레이어 위치와 무관한 곳으로 튀어 보인다. 매 프레임
        // 실제 플레이어의 현재 위치로 덮어써서, 경계에 닿는 순간(t=1) 항상 정확히 플레이어
        // 위치로 수렴하도록 한다.
        activeExitCameraPos.x = playerX;
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

        // FixedByPlayer는 위치를 여기서 되돌리지 않는다 - CameraMovement.isMovingEvent가 이미
        // false로 풀려 있어(Update() 참고) 그 자체 SmoothDamp follow가 계속 플레이어를 따라간다.
        if (cameraMode != MissionCameraMode.FixedByPlayer)
        {
            cameraRig.transform.position = Vector3.Lerp(
                returnStartPos,
                enterCameraPos,
                curvedT
            );
        }

        // exitCameraDefaultZoom이 켜져 있으면 줌은 UpdateExitDefaultZoom()이 전담한다 -
        // 여기서 같이 startZoomSize로 덮어쓰면 두 시스템이 매 프레임 서로의 값을 덮어써서
        // 떨리거나 exitCameraDefaultZoom의 결과가 무시되는 문제가 생긴다.
        if (!exitCameraDefaultZoom)
        {
            targetCamera.fieldOfView = Mathf.Lerp(
                returnStartZoom,
                startZoomSize,
                curvedT
            );
        }

        if (t >= 1f)
        {
            isReturning = false;

            if (activeInstance == this)
                activeInstance = null;

            if (CameraMovement.Instance != null)
            {
                // 위와 동일한 이유로, 제어를 넘기기 직전에 CameraPosY를 지금 리그 위치로 맞춘다.
                if (cameraRig != null)
                    CameraMovement.Instance.SetCameraRigY(cameraRig.transform.position.y);

                CameraMovement.Instance.isMovingEvent = false;
            }
        }
    }

    // smoothReturnOnExit(위치+줌 복귀)와 완전히 독립적으로, 줌(FieldOfView)만
    // CameraMovement.defaultFieldOfView로 되돌린다. exitCameraDefaultZoomDuration으로
    // 속도를 별도로 지정할 수 있다.
    private void UpdateExitDefaultZoom()
    {
        if (targetCamera == null || CameraMovement.Instance == null)
        {
            isExitingToDefaultZoom = false;
            return;
        }

        exitZoomTimer += Time.deltaTime;

        float t = exitCameraDefaultZoomDuration <= 0f
            ? 1f
            : Mathf.Clamp01(exitZoomTimer / exitCameraDefaultZoomDuration);
        float curvedT = fixedPanCurve.Evaluate(t);

        targetCamera.fieldOfView = Mathf.Lerp(
            exitZoomStartValue,
            CameraMovement.Instance.defaultFieldOfView,
            curvedT
        );

        if (t >= 1f)
            isExitingToDefaultZoom = false;
    }

    // 다른 MissionAreaCamera가 새로 활성화되면서 이 인스턴스가 아직 원래 상태로 되돌아오는
    // 중이었다면, 진행 중이던 lerp 값을 새 인스턴스가 "원래 상태"로 잘못 캡처하지 않도록
    // 즉시(보간 없이) 자신의 원래 위치/줌으로 완료시킨다. isReturning과 isExitingToDefaultZoom은
    // 서로 독립적인 상태라 각각 따로 확인하고 끝낸다.
    public void ForceFinishReturn()
    {
        if (isReturning)
        {
            isReturning = false;

            if (targetCamera != null && !exitCameraDefaultZoom)
                targetCamera.fieldOfView = startZoomSize;

            if (cameraRig != null && cameraMode != MissionCameraMode.FixedByPlayer)
                cameraRig.transform.position = enterCameraPos;

            if (CameraMovement.Instance != null)
            {
                if (cameraRig != null)
                    CameraMovement.Instance.SetCameraRigY(cameraRig.transform.position.y);

                CameraMovement.Instance.isMovingEvent = false;
            }
        }

        if (isExitingToDefaultZoom)
        {
            isExitingToDefaultZoom = false;

            if (targetCamera != null && CameraMovement.Instance != null)
                targetCamera.fieldOfView = CameraMovement.Instance.defaultFieldOfView;
        }
    }

    // OnTriggerEnter2D로 처음 들어올 때뿐 아니라, 겹쳐 있던 더 높은 priority의 MissionArea가
    // 이탈해서 이 인스턴스에게 소유권이 "다시" 넘어올 때도 반드시 호출해야 한다 - 매번 "지금
    // 이 순간의 실제 카메라 위치"를 enterCameraPos로 새로 캡처해야만, 이전 소유자가 카메라를
    // 어디에 두고 갔든 상관없이 튐 없이 이어서 시작할 수 있다. (이전 소유자가 애초에 자기
    // enterCameraPos 근처로 되돌아가는 걸 기다렸다가 넘겨받는 게 아니라, 그 순간 그대로의
    // 위치/줌을 자기 시작점으로 삼는다는 뜻.)
    private void BeginCameraControl()
    {
        if (cameraRig == null && CameraMovement.Instance != null)
            cameraRig = CameraMovement.Instance.gameObject;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraRig == null || targetCamera == null || player == null)
            return;

        isReturning = false;

        enterCameraPos = cameraRig.transform.position;

        // useCameraMovementDefaultZoom이 켜져 있으면 CameraMovement.defaultFieldOfView(고정된
        // 기준값)를 우선 쓴다 - 그 순간의 targetCamera.fieldOfView를 그대로 캡처하면, 인접한
        // 다른 MissionAreaCamera가 아직 복귀 중이라 값이 덜 풀린 상태를 "원래 상태"로 잘못
        // 저장할 수 있기 때문이다. 다만 이 경우 진입 순간 실제 FOV와 다르면 살짝 튈 수 있으므로,
        // 그게 더 거슬리는 영역은 이 옵션을 꺼서 기존처럼 그 순간의 실제 FOV를 그대로 쓰게 한다.
        // 겹친 영역끼리 소유권을 주고받는 체인을 구성할 때는, 두 번째 이후 영역은 이 옵션을
        // 꺼서 실제 순간 FOV(이전 소유자가 만들어둔 줌)를 그대로 이어받게 해야 줌도 끊기지
        // 않는다.
        startZoomSize = (useCameraMovementDefaultZoom && CameraMovement.Instance != null && CameraMovement.Instance.defaultFieldOfView > 0f)
            ? CameraMovement.Instance.defaultFieldOfView
            : targetCamera.fieldOfView;

        startHalfHeight =
            groundDistance * Mathf.Tan(startZoomSize * 0.5f * Mathf.Deg2Rad);

        fixedPanTimer = 0f;

        // smoothTargetPos/yVelocity 초기화는 GetActiveTargetPos()를 쓰는 모든 모드(수평 추적
        // 두 종류 + FixedByPlayer)에 공통으로 필요하다 - 안 해두면 useSmoothYFollow가 켜져
        // 있을 때 SmoothDamp의 시작값이 이전 상태(또는 기본값 0)로 남아있어 진입 직후 한 프레임
        // 확 튀어 보인다.
        if (cameraMode == MissionCameraMode.HorizontalByPlayerX ||
            cameraMode == MissionCameraMode.HorizontalByPlayerXWithExit ||
            cameraMode == MissionCameraMode.FixedByPlayer)
        {
            smoothTargetPos = targetPos;

            if (!fixPosY && useSmoothYFollow)
            {
                smoothTargetPos.y = enterCameraPos.y;
                yVelocity = 0f;
            }
        }

        // exitCameraPos/isLeftToRight는 진입-이탈 경계 사이를 보간하는 수평 추적 모드
        // 둘에서만 쓰인다 (FixedByPlayer는 이탈 경계 보간 없이 계속 플레이어를 따라가다가
        // 트리거를 벗어나는 방식이라 필요 없음).
        if (cameraMode == MissionCameraMode.HorizontalByPlayerX ||
            cameraMode == MissionCameraMode.HorizontalByPlayerXWithExit)
        {
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

    // activeAreas(지금 플레이어와 겹쳐 있는 모든 MissionArea) 중 priority가 가장 높은 쪽을
    // 소유자로 정한다. 동점이면 먼저 activeAreas에 들어온 쪽이 유지된다(이미 소유자였다면
    // 그대로, 아니라면 먼저 겹친 쪽). 소유자가 바뀔 때만 새 소유자의 BeginCameraControl()을
    // 호출해 "지금" 상태를 새로 캡처한다 - 밀려나는 쪽은 다음 Update()의 activeInstance 가드에
    // 걸려 자동으로 조용히 멈추므로 별도 처리가 필요 없다.
    private static void ReevaluateOwnership()
    {
        MissionAreaCamera newOwner = null;
        int bestPriority = int.MinValue;

        for (int i = 0; i < activeAreas.Count; i++)
        {
            if (activeAreas[i].priority > bestPriority)
            {
                bestPriority = activeAreas[i].priority;
                newOwner = activeAreas[i];
            }
        }

        if (newOwner == activeInstance)
            return;

        activeInstance = newOwner;

        if (newOwner != null)
            newOwner.BeginCameraControl();
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

        // 지금 어떤 MissionArea와도 겹쳐 있지 않은 상태에서 새로 들어오는 경우(레벨에 영역이
        // 경계 없이 순차적으로 배치된 경우)에만 의미가 있다 - 마지막으로 활성화됐던 인스턴스가
        // 아직 자기 복귀 애니메이션을 돌리고 있을 수 있으니 즉시 끝낸다. 겹친 상태에서의
        // 소유권 이전/반환은 ReevaluateOwnership()과 OnTriggerExit2D가 전담하므로 여기서
        // 건드릴 필요가 없다.
        if (activeAreas.Count == 0 && activeInstance != null && activeInstance != this)
            activeInstance.ForceFinishReturn();

        player = collision.transform;
        isCameraControl = true;

        if (!activeAreas.Contains(this))
            activeAreas.Add(this);

        ReevaluateOwnership();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        bool wasOwner = activeInstance == this;

        isCameraControl = false;
        player = null;
        activeAreas.Remove(this);

        if (!wasOwner)
        {
            // priority 경쟁에서 이미 밀려나 있던 상태였다면(카메라를 쓰고 있지 않았다면)
            // 전역 카메라 상태를 건드릴 게 없다.
            return;
        }

        if (activeAreas.Count > 0)
        {
            // 아직 겹쳐 있는 다른 MissionArea가 있다 - 그쪽(들 중 priority가 가장 높은 쪽)으로
            // 소유권을 넘긴다. 복귀 애니메이션이나 CameraMovement로의 반환 없이, 새 소유자가
            // BeginCameraControl()로 "지금" 카메라 위치를 그대로 이어받아 시작하므로 튀지 않는다.
            ReevaluateOwnership();
            return;
        }

        // 더 이상 어떤 MissionArea와도 겹쳐 있지 않다 - 여기서만 실제로 CameraMovement에게
        // 돌려준다.
        activeInstance = null;

        // HorizontalByPlayerX(WithExit)는 경계까지 연속 보간이 이미 끝나 있어서(경계=줌 0%
        // 지점) 나갈 때 이미 원래 상태나 다름없어 이 옵션과 무관하게 항상 되돌릴 필요가 없다.
        // FixedAreaPan과 FixedByPlayer는 연출/추적용으로 나간 뒤에도 그 자리에 머무는 걸
        // 의도적으로 선택할 수 있어 smoothReturnOnExit 하나로 통일해서 켜고 끈다. 이 옵션을
        // 끄더라도(FixedByPlayer의 경우) 카메라 위치 제어는 아래 else 분기에서 즉시
        // CameraMovement에게 돌아가므로 위치가 완전히 멈춰있지는 않는다 - 다만 줌은 그 값
        // 그대로 유지된다.
        bool shouldSmoothReturn =
            smoothReturnOnExit &&
            (cameraMode == MissionCameraMode.FixedAreaPan || cameraMode == MissionCameraMode.FixedByPlayer);

        if (shouldSmoothReturn)
        {
            isReturning = true;
            returnTimer = 0f;
            returnStartPos = cameraRig.transform.position;
            returnStartZoom = targetCamera.fieldOfView;
        }
        else
        {
            if (CameraMovement.Instance != null)
            {
                if (cameraRig != null)
                    CameraMovement.Instance.SetCameraRigY(cameraRig.transform.position.y);

                CameraMovement.Instance.isMovingEvent = false;
            }
        }

        // smoothReturnOnExit/shouldSmoothReturn과 완전히 독립적으로 동작한다 - 위치 복귀
        // 여부와 무관하게 줌만 별도로 기본값으로 되돌릴 수 있다.
        if (exitCameraDefaultZoom && targetCamera != null && CameraMovement.Instance != null)
        {
            isExitingToDefaultZoom = true;
            exitZoomTimer = 0f;
            exitZoomStartValue = targetCamera.fieldOfView;
        }
    }
}