using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(50000)]
public class FallZoomCameraArea : MonoBehaviour
{
    public enum FollowYStartMode
    {
        OnEnter,
        WhenPlayerBelowY,
        ManualOnly
    }

    private enum EnterSide
    {
        None,
        Left,
        Right
    }

    [Header("Target")]
    public Camera targetCamera;
    public Transform cameraRig;
    public string playerTag = "Player";

    [Header("Camera Movement Rule")]
    [Tooltip("체크하면 이 스크립트는 X를 절대 건드리지 않습니다. X는 기존 CameraMovement가 담당합니다.")]
    public bool neverControlX = true;

    [Tooltip("Area 안에 있는 동안 일반 CameraMovement가 계속 작동하도록 합니다.")]
    public bool keepNormalCameraMovementActive = true;

    [Header("X Position Progress")]
    [Tooltip("시간이 아니라 Area 안에서의 X 위치 기준으로 줌/Y 변화가 됩니다.")]
    public bool useXPositionProgress = true;

    [Tooltip("Area에 진입한 쪽 끝에서부터 몇 % 지점에 도착했을 때 최종값이 될지. 0.5 = Area의 50% 지점에서 최종값.")]
    [Range(0.05f, 1f)]
    public float fullEffectAtWidthPercent = 0.5f;

    [Tooltip("최종값 도달 후 Area 안에 있는 동안 계속 최종값을 유지합니다.")]
    public bool holdFullEffectAfterReached = true;

    [Tooltip("X 위치에 따른 변화 곡선")]
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Zoom")]
    [Tooltip("최종 확대 줌 값")]
    public float zoomedFieldOfView = 55f;

    [Header("Y Framing")]
    [Tooltip("진행도에 따라 카메라 Y를 투야 기준으로 맞춥니다.")]
    public bool controlYByProgress = true;

    [Tooltip("낙하 전 투야 기준 카메라 Y 오프셋. 양수면 카메라가 투야보다 위를 봅니다.")]
    public float playerFrameYOffset = 1.8f;

    [Header("Fall Follow Y")]
    [Tooltip("Y Follow를 언제 켤지")]
    public FollowYStartMode followYStartMode = FollowYStartMode.WhenPlayerBelowY;

    [Tooltip("플레이어가 이 Y값 아래로 내려가면 낙하 추적이 켜집니다.")]
    public float followStartWorldY = 0f;

    [Tooltip("낙하 중 투야 기준 카메라 Y 오프셋. 양수면 투야보다 위를 봅니다.")]
    public float fallCameraYOffset = 1.8f;

    [Tooltip("낙하가 시작되면 X 진행도와 상관없이 최종 줌/Y를 유지합니다.")]
    public bool keepFullEffectAfterFallStarts = true;

    [Header("Zoom Out On Exit")]
    public bool zoomOutOnExit = true;

    [Tooltip("들어오기 전 줌 값으로 복구할지")]
    public bool restoreToEnterFieldOfView = true;

    [Tooltip("Restore To Enter Field Of View가 꺼져 있을 때 사용할 줌 복구값")]
    public float zoomOutFieldOfView = 60f;

    [Tooltip("축소 시간")]
    public float zoomOutTime = 1.2f;

    public AnimationCurve zoomOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Exit")]
    [Tooltip("Area 밖으로 나가면 낙하 Y Follow를 끕니다.")]
    public bool disableFollowYOnExit = true;

    [Tooltip("Area 밖으로 나가면 CameraMovement에게 제어권을 돌려줍니다.")]
    public bool releaseCameraMovementOnExit = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Collider2D areaCollider;

    private bool playerInside;
    private bool followYActive;

    private Transform playerTransform;
    private Coroutine zoomOutCoroutine;

    private float enterFieldOfView;
    private bool hasEnterFieldOfView;

    private float enterCameraY;
    private bool hasEnterCameraY;

    private EnterSide enterSide = EnterSide.None;

    private void Awake()
    {
        RefreshReferences();
        areaCollider = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        RefreshReferences();

        if (!playerInside || playerTransform == null)
            return;

        if (keepNormalCameraMovementActive && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        CheckAutoStartFollowY();

        float progress = GetProgressByPlayerX();

        if (followYActive && keepFullEffectAfterFallStarts)
            progress = 1f;

        ApplyZoomByProgress(progress);
        ApplyYByProgress(progress);
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

        playerInside = true;
        playerTransform = collision.transform;
        followYActive = false;

        enterSide = GetEnterSide(collision.transform.position);

        if (targetCamera != null)
        {
            enterFieldOfView = targetCamera.fieldOfView;
            hasEnterFieldOfView = true;
        }

        if (cameraRig != null)
        {
            enterCameraY = cameraRig.position.y;
            hasEnterCameraY = true;
        }

        if (zoomOutCoroutine != null)
        {
            StopCoroutine(zoomOutCoroutine);
            zoomOutCoroutine = null;
        }

        if (keepNormalCameraMovementActive && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        if (followYStartMode == FollowYStartMode.OnEnter)
            StartFollowY();

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : Enter / Side = {enterSide}");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        playerInside = false;
        playerTransform = null;
        enterSide = EnterSide.None;

        if (disableFollowYOnExit)
            StopFollowY();

        if (releaseCameraMovementOnExit && CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = false;

        if (zoomOutOnExit)
        {
            float targetFOV = zoomOutFieldOfView;

            if (restoreToEnterFieldOfView && hasEnterFieldOfView)
                targetFOV = enterFieldOfView;

            StartZoomOut(targetFOV);
        }

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : Exit");
    }

    private EnterSide GetEnterSide(Vector3 playerPosition)
    {
        Bounds bounds = GetAreaBounds();

        if (playerPosition.x < bounds.center.x)
            return EnterSide.Left;

        return EnterSide.Right;
    }

    private Bounds GetAreaBounds()
    {
        if (areaCollider != null)
            return areaCollider.bounds;

        return new Bounds(transform.position, new Vector3(10f, 10f, 1f));
    }

    private float GetProgressByPlayerX()
    {
        if (!useXPositionProgress)
            return 1f;

        if (playerTransform == null)
            return 0f;

        Bounds bounds = GetAreaBounds();

        float width = Mathf.Max(bounds.size.x, 0.001f);
        float fullDistance = width * fullEffectAtWidthPercent;

        if (fullDistance <= 0.001f)
            fullDistance = width * 0.5f;

        float distanceFromEnterEdge = 0f;

        if (enterSide == EnterSide.Left)
        {
            distanceFromEnterEdge = playerTransform.position.x - bounds.min.x;
        }
        else if (enterSide == EnterSide.Right)
        {
            distanceFromEnterEdge = bounds.max.x - playerTransform.position.x;
        }
        else
        {
            float distanceToLeft = Mathf.Abs(playerTransform.position.x - bounds.min.x);
            float distanceToRight = Mathf.Abs(playerTransform.position.x - bounds.max.x);
            distanceFromEnterEdge = Mathf.Min(distanceToLeft, distanceToRight);
        }

        float t = Mathf.Clamp01(distanceFromEnterEdge / fullDistance);

        if (holdFullEffectAfterReached && t >= 1f)
            return 1f;

        if (progressCurve != null)
            t = progressCurve.Evaluate(t);

        return Mathf.Clamp01(t);
    }

    private void ApplyZoomByProgress(float progress)
    {
        if (targetCamera == null)
            return;

        if (!hasEnterFieldOfView)
        {
            enterFieldOfView = targetCamera.fieldOfView;
            hasEnterFieldOfView = true;
        }

        progress = Mathf.Clamp01(progress);

        targetCamera.fieldOfView = Mathf.Lerp(
            enterFieldOfView,
            zoomedFieldOfView,
            progress
        );
    }

    private void ApplyYByProgress(float progress)
    {
        if (!controlYByProgress)
            return;

        if (cameraRig == null || playerTransform == null)
            return;

        if (!hasEnterCameraY)
        {
            enterCameraY = cameraRig.position.y;
            hasEnterCameraY = true;
        }

        progress = Mathf.Clamp01(progress);

        float targetY;

        if (followYActive)
            targetY = playerTransform.position.y + fallCameraYOffset;
        else
            targetY = playerTransform.position.y + playerFrameYOffset;

        float nextY = Mathf.Lerp(
            enterCameraY,
            targetY,
            progress
        );

        Vector3 pos = cameraRig.position;

        // 중요:
        // X는 절대 건드리지 않습니다.
        // X는 기존 CameraMovement가 계속 담당합니다.
        pos.y = nextY;

        cameraRig.position = pos;
    }

    private void CheckAutoStartFollowY()
    {
        if (followYActive)
            return;

        if (followYStartMode != FollowYStartMode.WhenPlayerBelowY)
            return;

        if (playerTransform == null)
            return;

        if (playerTransform.position.y <= followStartWorldY)
            StartFollowY();
    }

    public void StartFollowY()
    {
        followYActive = true;

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : Follow Y ON");
    }

    public void StopFollowY()
    {
        followYActive = false;

        if (showDebugLog)
            Debug.Log($"{gameObject.name} : Follow Y OFF");
    }

    private void StartZoomOut(float targetFOV)
    {
        RefreshReferences();

        if (targetCamera == null)
            return;

        if (zoomOutCoroutine != null)
            StopCoroutine(zoomOutCoroutine);

        zoomOutCoroutine = StartCoroutine(ZoomOutRoutine(targetFOV));
    }

    private IEnumerator ZoomOutRoutine(float targetFOV)
    {
        if (targetCamera == null)
            yield break;

        float startFOV = targetCamera.fieldOfView;
        float timer = 0f;

        while (timer < zoomOutTime)
        {
            timer += Time.deltaTime;

            float t = zoomOutTime <= 0f ? 1f : Mathf.Clamp01(timer / zoomOutTime);
            float curvedT = zoomOutCurve != null ? zoomOutCurve.Evaluate(t) : t;

            targetCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curvedT);

            yield return null;
        }

        targetCamera.fieldOfView = targetFOV;
        zoomOutCoroutine = null;
    }

    private void OnDisable()
    {
        if (disableFollowYOnExit)
            StopFollowY();

        if (releaseCameraMovementOnExit && CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = false;
    }
}