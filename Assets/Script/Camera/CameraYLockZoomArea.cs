using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraYLockZoomArea : MonoBehaviour
{
    [Header("Target")]
    public Camera targetCamera;
    public Transform cameraRig;
    public string playerTag = "Player";

    [Header("Normal Camera Movement")]
    [Tooltip("체크하면 이 구역 안에서도 일반 CameraMovement가 계속 X를 따라갑니다.")]
    public bool forceNormalCameraMovement = true;

    [Header("Y Lock")]
    [Tooltip("이 구역 안에서 고정할 CameraRig의 Y값")]
    public float fixedCameraY = 0f;

    [Tooltip("현재 카메라 Y에서 Fixed Camera Y까지 자연스럽게 이동하는 시간")]
    public float yBlendTime = 1.2f;

    [Header("Zoom")]
    [Tooltip("이 구역 안에서 도착할 카메라 Field Of View")]
    public float targetFieldOfView = 55f;

    [Tooltip("현재 줌에서 Target Field Of View까지 자연스럽게 이동하는 시간")]
    public float zoomBlendTime = 1.2f;

    public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Exit")]
    [Tooltip("구역을 나갈 때 줌을 입장 전 값으로 되돌릴지")]
    public bool restoreZoomOnExit = false;

    [Tooltip("구역을 나갈 때 Y를 입장 전 값으로 되돌릴지")]
    public bool restoreYOnExit = false;

    [Tooltip("나갈 때 되돌리는 시간")]
    public float exitBlendTime = 1f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private bool playerInside;
    private bool restoringOnExit;

    private float insideTimer;
    private float exitTimer;

    private float enterCameraY;
    private float enterFieldOfView;

    private float exitStartCameraY;
    private float exitStartFieldOfView;

    private void Awake()
    {
        RefreshReferences();
    }

    private void LateUpdate()
    {
        RefreshReferences();

        if (targetCamera == null || cameraRig == null)
            return;

        // 중요:
        // 플레이어가 이 Area 안에 있을 때만 CameraMovement 상태를 건드립니다.
        // Area 밖에서는 절대 isMovingEvent를 false로 덮어쓰지 않습니다.
        if (playerInside)
        {
            if (forceNormalCameraMovement && CameraMovement.Instance != null)
            {
                CameraMovement.Instance.isMovingEvent = false;
            }

            UpdateInsideArea();
            return;
        }

        if (restoringOnExit)
        {
            UpdateExitRestore();
            return;
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
        restoringOnExit = false;

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

        if (restoreZoomOnExit || restoreYOnExit)
        {
            restoringOnExit = true;
            exitTimer = 0f;

            if (cameraRig != null)
                exitStartCameraY = cameraRig.position.y;

            if (targetCamera != null)
                exitStartFieldOfView = targetCamera.fieldOfView;
        }
        else
        {
            restoringOnExit = false;
        }

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : CameraYLockZoomArea Exit");
    }

    private void UpdateInsideArea()
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

    private void UpdateExitRestore()
    {
        exitTimer += Time.deltaTime;

        float t = exitBlendTime <= 0f ? 1f : Mathf.Clamp01(exitTimer / exitBlendTime);
        float curvedT = blendCurve.Evaluate(t);

        if (restoreYOnExit)
        {
            float nextY = Mathf.Lerp(exitStartCameraY, enterCameraY, curvedT);
            ApplyCameraY(nextY);
        }

        if (restoreZoomOnExit)
        {
            float nextFOV = Mathf.Lerp(exitStartFieldOfView, enterFieldOfView, curvedT);
            ApplyFieldOfView(nextFOV);
        }

        if (t >= 1f)
        {
            restoringOnExit = false;
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
        playerInside = false;
        restoringOnExit = false;
    }
}