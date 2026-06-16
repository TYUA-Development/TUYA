using UnityEngine;

[DefaultExecutionOrder(50000)]
public class CameraYLockZoomArea : MonoBehaviour
{
    public enum ExitYMode
    {
        KeepCurrentY,
        FixedY,
        OffsetFromCurrentY
    }

    [Header("Target")]
    public Camera targetCamera;
    public Transform cameraRig;
    public string playerTag = "Player";

    [Header("Normal Camera Control")]
    [Tooltip("체크하면 이 구역 안/나간 직후에도 일반 카메라 이동을 막지 않습니다. X축은 기존 CameraMovement가 계속 담당합니다.")]
    public bool forceNormalCameraMovement = true;

    [Header("Inside Area - Y Lock")]
    [Tooltip("5번 Area 안에 있을 때 고정할 CameraRig의 Y값")]
    public float fixedCameraY = 0f;

    [Tooltip("Area에 들어왔을 때 현재 Y에서 Fixed Camera Y까지 이동하는 시간")]
    public float yBlendTime = 1.2f;

    [Header("Inside Area - Zoom")]
    [Tooltip("5번 Area 안에서 도착할 줌 값")]
    public float targetFieldOfView = 55f;

    [Tooltip("Area에 들어왔을 때 줌 변화 시간")]
    public float zoomBlendTime = 1.2f;

    public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Legacy Restore On Exit")]
    [Tooltip("Exit Camera를 쓰지 않을 때, 나가면서 줌을 입장 전 값으로 되돌릴지")]
    public bool restoreZoomOnExit = false;

    [Tooltip("Exit Camera를 쓰지 않을 때, 나가면서 Y를 입장 전 값으로 되돌릴지")]
    public bool restoreYOnExit = false;

    public float exitBlendTime = 1f;

    [Header("Exit Camera After Leaving Area")]
    [Tooltip("5번 Area에서 나갈 때 추가로 확대/Y 이동을 실행합니다.")]
    public bool enableExitCameraOnLeave = true;

    [Tooltip("5번 Area에서 나간 뒤 도착할 줌 값")]
    public float exitTargetFieldOfView = 50f;

    [Tooltip("나간 뒤 줌 변화 시간")]
    public float exitZoomTime = 2.5f;

    [Tooltip("나간 뒤 Y를 어떻게 바꿀지")]
    public ExitYMode exitYMode = ExitYMode.OffsetFromCurrentY;

    [Tooltip("Exit Y Mode가 FixedY일 때 도착할 CameraRig Y값")]
    public float exitTargetCameraY = -3f;

    [Tooltip("Exit Y Mode가 OffsetFromCurrentY일 때 현재 Y에서 더할 값. 아래로 내리고 싶으면 음수")]
    public float exitTargetYOffset = -3f;

    [Tooltip("나간 뒤 Y 변화 시간. 0이면 Exit Zoom Time과 동일")]
    public float exitYTime = 0f;

    [Tooltip("Exit 연출이 끝난 뒤에도 Y값을 유지합니다.")]
    public bool keepExitYAfterComplete = true;

    [Tooltip("Exit 연출이 끝난 뒤에도 줌 값을 유지합니다.")]
    public bool keepExitZoomAfterComplete = true;

    public AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    public bool showDebugLog = false;

    private bool playerInside;
    private bool exitCameraRunning;
    private bool legacyRestoreRunning;
    private bool holdingExitY;
    private bool holdingExitZoom;

    private float insideTimer;

    private float enterCameraY;
    private float enterFieldOfView;

    private float exitTimer;
    private float exitStartCameraY;
    private float exitEndCameraY;
    private float exitStartFieldOfView;
    private float exitEndFieldOfView;

    private float legacyRestoreTimer;
    private float legacyRestoreStartY;
    private float legacyRestoreStartFOV;

    private void Awake()
    {
        RefreshReferences();
    }

    private void LateUpdate()
    {
        RefreshReferences();

        if (targetCamera == null || cameraRig == null)
            return;

        if (forceNormalCameraMovement && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        if (playerInside)
        {
            UpdateInsideAreaCamera();
            return;
        }

        if (exitCameraRunning)
        {
            UpdateExitCamera();
            return;
        }

        if (legacyRestoreRunning)
        {
            UpdateLegacyRestore();
            return;
        }

        if (holdingExitY)
        {
            ApplyCameraY(exitEndCameraY);
        }

        if (holdingExitZoom)
        {
            ApplyFieldOfView(exitEndFieldOfView);
        }
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

        if (targetCamera == null || cameraRig == null)
            return;

        playerInside = true;
        exitCameraRunning = false;
        legacyRestoreRunning = false;
        holdingExitY = false;
        holdingExitZoom = false;

        insideTimer = 0f;

        enterCameraY = cameraRig.position.y;
        enterFieldOfView = targetCamera.fieldOfView;

        if (forceNormalCameraMovement && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : CameraYLockZoomArea Enter");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        playerInside = false;

        if (enableExitCameraOnLeave)
        {
            StartExitCamera();
        }
        else
        {
            StartLegacyRestoreIfNeeded();
        }

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : CameraYLockZoomArea Exit");
    }

    private void UpdateInsideAreaCamera()
    {
        insideTimer += Time.deltaTime;

        float yT = yBlendTime <= 0f ? 1f : Mathf.Clamp01(insideTimer / yBlendTime);
        float zoomT = zoomBlendTime <= 0f ? 1f : Mathf.Clamp01(insideTimer / zoomBlendTime);

        float curvedYT = blendCurve.Evaluate(yT);
        float curvedZoomT = blendCurve.Evaluate(zoomT);

        float nextY = Mathf.Lerp(enterCameraY, fixedCameraY, curvedYT);
        float nextFOV = Mathf.Lerp(enterFieldOfView, targetFieldOfView, curvedZoomT);

        ApplyCameraY(nextY);
        ApplyFieldOfView(nextFOV);
    }

    private void StartExitCamera()
    {
        RefreshReferences();

        if (targetCamera == null || cameraRig == null)
            return;

        exitCameraRunning = true;
        legacyRestoreRunning = false;
        holdingExitY = false;
        holdingExitZoom = false;

        exitTimer = 0f;

        exitStartCameraY = cameraRig.position.y;
        exitStartFieldOfView = targetCamera.fieldOfView;

        exitEndFieldOfView = exitTargetFieldOfView;

        if (exitYMode == ExitYMode.KeepCurrentY)
        {
            exitEndCameraY = exitStartCameraY;
        }
        else if (exitYMode == ExitYMode.FixedY)
        {
            exitEndCameraY = exitTargetCameraY;
        }
        else
        {
            exitEndCameraY = exitStartCameraY + exitTargetYOffset;
        }

        if (showDebugLog)
        {
            Debug.Log($"{gameObject.name} : Exit Camera Start / StartY {exitStartCameraY} / EndY {exitEndCameraY}");
        }
    }

    private void UpdateExitCamera()
    {
        exitTimer += Time.deltaTime;

        float realExitYTime = exitYTime <= 0f ? exitZoomTime : exitYTime;

        float zoomT = exitZoomTime <= 0f ? 1f : Mathf.Clamp01(exitTimer / exitZoomTime);
        float yT = realExitYTime <= 0f ? 1f : Mathf.Clamp01(exitTimer / realExitYTime);

        float curvedZoomT = exitCurve.Evaluate(zoomT);
        float curvedYT = exitCurve.Evaluate(yT);

        float nextFOV = Mathf.Lerp(exitStartFieldOfView, exitEndFieldOfView, curvedZoomT);
        float nextY = Mathf.Lerp(exitStartCameraY, exitEndCameraY, curvedYT);

        ApplyFieldOfView(nextFOV);
        ApplyCameraY(nextY);

        bool zoomDone = zoomT >= 1f;
        bool yDone = yT >= 1f;

        if (zoomDone && yDone)
        {
            exitCameraRunning = false;

            holdingExitY = keepExitYAfterComplete;
            holdingExitZoom = keepExitZoomAfterComplete;

            if (keepExitYAfterComplete)
                ApplyCameraY(exitEndCameraY);

            if (keepExitZoomAfterComplete)
                ApplyFieldOfView(exitEndFieldOfView);

            if (showDebugLog)
                Debug.Log($"{gameObject.name} : Exit Camera Complete");
        }
    }

    private void StartLegacyRestoreIfNeeded()
    {
        if (!restoreZoomOnExit && !restoreYOnExit)
            return;

        RefreshReferences();

        if (targetCamera == null || cameraRig == null)
            return;

        legacyRestoreRunning = true;
        legacyRestoreTimer = 0f;

        legacyRestoreStartY = cameraRig.position.y;
        legacyRestoreStartFOV = targetCamera.fieldOfView;
    }

    private void UpdateLegacyRestore()
    {
        legacyRestoreTimer += Time.deltaTime;

        float t = exitBlendTime <= 0f ? 1f : Mathf.Clamp01(legacyRestoreTimer / exitBlendTime);
        float curvedT = blendCurve.Evaluate(t);

        if (restoreYOnExit)
        {
            float y = Mathf.Lerp(legacyRestoreStartY, enterCameraY, curvedT);
            ApplyCameraY(y);
        }

        if (restoreZoomOnExit)
        {
            float fov = Mathf.Lerp(legacyRestoreStartFOV, enterFieldOfView, curvedT);
            ApplyFieldOfView(fov);
        }

        if (t >= 1f)
        {
            legacyRestoreRunning = false;
        }
    }

    private void ApplyCameraY(float y)
    {
        if (cameraRig == null)
            return;

        Vector3 pos = cameraRig.position;
        pos.y = y;
        cameraRig.position = pos;
    }

    private void ApplyFieldOfView(float fov)
    {
        if (targetCamera == null)
            return;

        targetCamera.fieldOfView = fov;
    }

    public void StopExitCameraHold()
    {
        exitCameraRunning = false;
        legacyRestoreRunning = false;
        holdingExitY = false;
        holdingExitZoom = false;
    }
}