using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(20000)]
public class PassThroughExitCameraZoom : MonoBehaviour
{
    private enum EnterSide
    {
        None,
        Left,
        Right
    }

    public enum PassDirection
    {
        Both,
        LeftToRightOnly,
        RightToLeftOnly
    }

    [Header("Target")]
    public Camera targetCamera;
    public Transform cameraRig;
    public string playerTag = "Player";

    [Header("Pass Through Condition")]
    public PassDirection passDirection = PassDirection.Both;

    [Tooltip("플레이어가 구역의 반대편으로 완전히 나갔을 때만 줌을 시작합니다.")]
    public bool requireOppositeExit = true;

    [Header("Zoom")]
    [Tooltip("도착할 카메라 Field Of View. 값이 작을수록 확대됩니다.")]
    public float targetFieldOfView = 55f;

    [Tooltip("확대되는 데 걸리는 시간")]
    public float zoomTime = 3f;

    [Tooltip("줌 시작 전 대기 시간")]
    public float startDelay = 0f;

    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Safe Y Move")]
    [Tooltip("PassThrough 실행 중 CameraRig의 Y를 안전하게 이동합니다.")]
    public bool useSafeRigYMove = true;

    [Tooltip("음수면 카메라가 아래로 내려갑니다. 예: -3, -4, -5")]
    public float targetYOffset = -4f;

    [Tooltip("Y 이동 시간. 0이면 zoomTime과 동일하게 사용합니다.")]
    public float yMoveTime = 0f;

    public AnimationCurve yMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("X Follow During PassThrough")]
    [Tooltip("PassThrough 실행 중에도 플레이어 X를 따라갑니다. 꺼두면 전환 중 X가 잠깐 멈출 수 있습니다.")]
    public bool followPlayerXWhileRunning = true;

    [Tooltip("플레이어 X를 따라갈 때 진입 순간의 카메라-플레이어 X 거리차를 유지합니다.")]
    public bool keepCurrentXOffsetFromPlayer = true;

    [Tooltip("keepCurrentXOffsetFromPlayer가 꺼져 있을 때 사용할 X 오프셋")]
    public float playerXOffset = 0f;

    [Tooltip("X 추적 부드러움")]
    public float followXSmoothTime = 0.08f;

    [Tooltip("X 추적 최대 속도")]
    public float followXMaxSpeed = 200f;

    [Header("Camera Ownership")]
    [Tooltip("PassThrough 실행 중 CameraMovement의 일반 추적을 잠깐 멈춰 충돌을 막습니다.")]
    public bool takeCameraOwnershipWhileRunning = true;

    [Tooltip("전환이 끝나면 CameraMovement에게 다시 제어권을 돌려줍니다.")]
    public bool returnControlAfterComplete = true;

    [Tooltip("전환 완료 후 줌 값을 유지합니다.")]
    public bool keepZoomAfterComplete = true;

    [Tooltip("전환 완료 후 Y 위치를 유지합니다. returnControlAfterComplete가 켜져 있으면 CameraMovement가 다시 덮을 수 있습니다.")]
    public bool keepYAfterComplete = false;

    [Header("Repeat")]
    public bool activateOnlyOnce = false;
    public bool restartIfTriggeredAgain = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Collider2D areaCollider;
    private EnterSide enterSide = EnterSide.None;

    private bool hasActivated;
    private bool isRunning;

    private Coroutine zoomCoroutine;
    private Coroutine clearYOffsetCoroutine;

    private Transform playerTransform;

    private Vector3 startRigPosition;
    private Vector3 targetRigPosition;

    private float startFieldOfView;
    private float startCameraY;
    private float targetCameraY;

    private float activePlayerXOffset;
    private float xVelocity;

    private void Awake()
    {
        RefreshReferences();
        areaCollider = GetComponent<Collider2D>();
    }

    private void RefreshReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraRig == null && CameraMovement.Instance != null)
            cameraRig = CameraMovement.Instance.transform;

        if (cameraRig == null && targetCamera != null)
        {
            if (targetCamera.transform.parent != null)
                cameraRig = targetCamera.transform.parent;
            else
                cameraRig = targetCamera.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        RefreshReferences();

        playerTransform = collision.transform;
        enterSide = GetPlayerSide(collision.transform.position);

        if (showDebugLog)
            Debug.Log($"{gameObject.name} Enter Side : {enterSide}");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        RefreshReferences();

        playerTransform = collision.transform;

        EnterSide exitSide = GetPlayerSide(collision.transform.position);

        if (showDebugLog)
            Debug.Log($"{gameObject.name} Exit Side : {exitSide}");

        if (!CanStartZoom(enterSide, exitSide))
        {
            enterSide = EnterSide.None;
            return;
        }

        enterSide = EnterSide.None;

        if (activateOnlyOnce && hasActivated)
            return;

        hasActivated = true;

        if (targetCamera == null || cameraRig == null)
        {
            Debug.LogWarning($"{gameObject.name} : Target Camera 또는 Camera Rig가 없습니다.");
            return;
        }

        if (zoomCoroutine != null)
        {
            if (restartIfTriggeredAgain)
                StopCoroutine(zoomCoroutine);
            else
                return;
        }

        if (clearYOffsetCoroutine != null)
        {
            StopCoroutine(clearYOffsetCoroutine);
            clearYOffsetCoroutine = null;
        }

        zoomCoroutine = StartCoroutine(ZoomAndMoveRoutine());
    }

    private EnterSide GetPlayerSide(Vector3 playerPosition)
    {
        float centerX = transform.position.x;

        if (areaCollider != null)
            centerX = areaCollider.bounds.center.x;

        if (playerPosition.x < centerX)
            return EnterSide.Left;

        return EnterSide.Right;
    }

    private bool CanStartZoom(EnterSide startSide, EnterSide endSide)
    {
        if (startSide == EnterSide.None || endSide == EnterSide.None)
            return false;

        if (requireOppositeExit && startSide == endSide)
            return false;

        if (passDirection == PassDirection.LeftToRightOnly)
            return startSide == EnterSide.Left && endSide == EnterSide.Right;

        if (passDirection == PassDirection.RightToLeftOnly)
            return startSide == EnterSide.Right && endSide == EnterSide.Left;

        return startSide != endSide;
    }

    private IEnumerator ZoomAndMoveRoutine()
    {
        isRunning = true;
        xVelocity = 0f;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        RefreshReferences();

        if (targetCamera == null || cameraRig == null)
        {
            isRunning = false;
            zoomCoroutine = null;
            yield break;
        }

        startRigPosition = cameraRig.position;
        startFieldOfView = targetCamera.fieldOfView;

        startCameraY = cameraRig.position.y;
        targetCameraY = startCameraY + targetYOffset;

        targetRigPosition = startRigPosition;
        targetRigPosition.y = targetCameraY;

        if (playerTransform != null)
        {
            if (keepCurrentXOffsetFromPlayer)
                activePlayerXOffset = cameraRig.position.x - playerTransform.position.x;
            else
                activePlayerXOffset = playerXOffset;
        }

        float realYMoveTime = yMoveTime <= 0f ? zoomTime : yMoveTime;

        float timer = 0f;

        while (timer < zoomTime)
        {
            timer += Time.deltaTime;

            if (takeCameraOwnershipWhileRunning && CameraMovement.Instance != null)
            {
                CameraMovement.Instance.isMovingEvent = true;
            }

            float zoomT = zoomTime <= 0f ? 1f : Mathf.Clamp01(timer / zoomTime);
            float curvedZoomT = zoomCurve.Evaluate(zoomT);

            float nextFOV = Mathf.Lerp(
                startFieldOfView,
                targetFieldOfView,
                curvedZoomT
            );

            targetCamera.fieldOfView = nextFOV;

            Vector3 nextRigPos = cameraRig.position;

            if (followPlayerXWhileRunning && playerTransform != null)
            {
                float desiredX = playerTransform.position.x + activePlayerXOffset;

                nextRigPos.x = Mathf.SmoothDamp(
                    cameraRig.position.x,
                    desiredX,
                    ref xVelocity,
                    followXSmoothTime,
                    followXMaxSpeed
                );
            }

            if (useSafeRigYMove)
            {
                float yT = realYMoveTime <= 0f ? 1f : Mathf.Clamp01(timer / realYMoveTime);
                float curvedYT = yMoveCurve.Evaluate(yT);

                nextRigPos.y = Mathf.Lerp(
                    startCameraY,
                    targetCameraY,
                    curvedYT
                );
            }

            cameraRig.position = nextRigPos;

            if (showDebugLog)
            {
                Debug.Log($"{gameObject.name} Running / FOV: {targetCamera.fieldOfView} / RigY: {cameraRig.position.y}");
            }

            yield return null;
        }

        FinishCameraMove();

        isRunning = false;
        zoomCoroutine = null;
    }

    private void FinishCameraMove()
    {
        RefreshReferences();

        if (targetCamera != null && keepZoomAfterComplete)
            targetCamera.fieldOfView = targetFieldOfView;

        if (cameraRig != null && keepYAfterComplete && useSafeRigYMove)
        {
            Vector3 pos = cameraRig.position;
            pos.y = targetCameraY;
            cameraRig.position = pos;
        }

        if (returnControlAfterComplete && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        if (showDebugLog)
            Debug.Log($"{gameObject.name} Complete");
    }

    public void StopZoom()
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        isRunning = false;

        if (returnControlAfterComplete && CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = false;
    }

    public void ClearYOffset()
    {
        StopZoom();

        if (clearYOffsetCoroutine != null)
        {
            StopCoroutine(clearYOffsetCoroutine);
            clearYOffsetCoroutine = null;
        }
    }

    public void ClearYOffsetSmooth(float clearTime)
    {
        StopZoom();

        if (clearYOffsetCoroutine != null)
            StopCoroutine(clearYOffsetCoroutine);

        if (clearTime <= 0f)
        {
            ClearYOffset();
            return;
        }

        clearYOffsetCoroutine = StartCoroutine(ClearYOffsetRoutine(clearTime));
    }

    private IEnumerator ClearYOffsetRoutine(float clearTime)
    {
        float timer = 0f;

        while (timer < clearTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        ClearYOffset();
        clearYOffsetCoroutine = null;
    }

    private void OnDisable()
    {
        if (isRunning && returnControlAfterComplete && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        isRunning = false;
    }
}