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

    [Tooltip("�÷��̾ ������ �ݴ������� ������ ������ ���� ���� �����մϴ�.")]
    public bool requireOppositeExit = true;

    [Header("Zoom")]
    [Tooltip("������ ī�޶� Field Of View. ���� �������� Ȯ��˴ϴ�.")]
    public float targetFieldOfView = 55f;

    [Tooltip("Ȯ��Ǵ� �� �ɸ��� �ð�")]
    public float zoomTime = 3f;

    [Tooltip("�� ���� �� ��� �ð�")]
    public float startDelay = 0f;

    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Safe Y Move")]
    [Tooltip("PassThrough ���� �� CameraRig�� Y�� �����ϰ� �̵��մϴ�.")]
    public bool useSafeRigYMove = true;

    [Tooltip("������ ī�޶� �Ʒ��� �������ϴ�. ��: -3, -4, -5")]
    public float targetYOffset = -4f;

    [Tooltip("Y �̵� �ð�. 0�̸� zoomTime�� �����ϰ� ����մϴ�.")]
    public float yMoveTime = 0f;

    public AnimationCurve yMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("X Follow During PassThrough")]
    [Tooltip("PassThrough ���� �߿��� �÷��̾� X�� ���󰩴ϴ�. ���θ� ��ȯ �� X�� ��� ���� �� �ֽ��ϴ�.")]
    public bool followPlayerXWhileRunning = true;

    [Tooltip("�÷��̾� X�� ���� �� ���� ������ ī�޶�-�÷��̾� X �Ÿ����� �����մϴ�.")]
    public bool keepCurrentXOffsetFromPlayer = true;

    [Tooltip("keepCurrentXOffsetFromPlayer�� ���� ���� �� ����� X ������")]
    public float playerXOffset = 0f;

    [Tooltip("X ���� �ε巯��")]
    public float followXSmoothTime = 0.08f;

    [Tooltip("X ���� �ִ� �ӵ�")]
    public float followXMaxSpeed = 200f;

    [Header("Camera Ownership")]
    [Tooltip("PassThrough ���� �� CameraMovement�� �Ϲ� ������ ��� ���� �浹�� �����ϴ�.")]
    public bool takeCameraOwnershipWhileRunning = true;

    [Tooltip("��ȯ�� ������ CameraMovement���� �ٽ� ������� �����ݴϴ�.")]
    public bool returnControlAfterComplete = true;

    [Tooltip("��ȯ �Ϸ� �� �� ���� �����մϴ�.")]
    public bool keepZoomAfterComplete = true;

    [Tooltip("��ȯ �Ϸ� �� Y ��ġ�� �����մϴ�. returnControlAfterComplete�� ���� ������ CameraMovement�� �ٽ� ���� �� �ֽ��ϴ�.")]
    public bool keepYAfterComplete = false;

    [Header("FOV Restore After Complete")]
    [Tooltip("애니메이션 완료 후 진입 시점의 원본 FOV로 복원합니다.")]
    public bool restoreFieldOfViewAfterComplete = false;
    [Tooltip("원본 FOV로 되돌아가는 데 걸리는 시간(초)")]
    public float fovRestoreTime = 1f;
    public AnimationCurve fovRestoreCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
    private Coroutine fovRestoreCoroutine;

    private float enterFieldOfView;

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

        // SH_MissionAreaCamera가 FOV를 바꾸기 전 원본 값 캡처 (entryBlendTime이 있어 즉시 변하지 않음)
        if (targetCamera != null)
            enterFieldOfView = targetCamera.fieldOfView;

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
            Debug.LogWarning($"{gameObject.name} : Target Camera �Ǵ� Camera Rig�� �����ϴ�.");
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
        // 플레이어 실제 Y 기준으로 목표 Y 계산 (이전 카메라 시스템이 Y를 고정했더라도 플레이어 위치로 확대)
        targetCameraY = playerTransform != null
            ? playerTransform.position.y + targetYOffset
            : startCameraY + targetYOffset;

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

                // CameraMovement 내부 타깃 Y도 동기화 (LateUpdate 덮어쓰기 방지)
                if (CameraMovement.Instance != null)
                    CameraMovement.Instance.SetCameraRigY(nextRigPos.y);
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
            CameraMovement.Instance.isMovingEvent = false;

        if (restoreFieldOfViewAfterComplete && targetCamera != null)
        {
            if (fovRestoreCoroutine != null)
                StopCoroutine(fovRestoreCoroutine);
            fovRestoreCoroutine = StartCoroutine(FovRestoreRoutine());
        }

        if (showDebugLog)
            Debug.Log($"{gameObject.name} Complete");
    }

    private IEnumerator FovRestoreRoutine()
    {
        float startFov = targetCamera.fieldOfView;
        float timer = 0f;
        float duration = fovRestoreTime > 0f ? fovRestoreTime : 0.001f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = fovRestoreCurve.Evaluate(Mathf.Clamp01(timer / duration));
            targetCamera.fieldOfView = Mathf.Lerp(startFov, enterFieldOfView, t);
            yield return null;
        }

        targetCamera.fieldOfView = enterFieldOfView;
        fovRestoreCoroutine = null;
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