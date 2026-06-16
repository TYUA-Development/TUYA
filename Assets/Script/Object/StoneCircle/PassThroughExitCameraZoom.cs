using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(50000)]
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

    [Header("Camera Local Y Offset")]
    [Tooltip("확대되는 동안 Main Camera의 local Y를 직접 내립니다.")]
    public bool useCameraLocalYOffset = true;

    [Tooltip("확대가 끝난 뒤에도 Y 보정값을 유지합니다.")]
    public bool keepYOffsetAfterZoom = true;

    [Tooltip("음수면 화면이 아래로 내려갑니다. 예: -3, -4, -5")]
    public float targetYOffset = -4f;

    [Tooltip("Y 보정이 적용되는 시간. 0이면 zoomTime과 동일하게 사용합니다.")]
    public float yOffsetTime = 0f;

    public AnimationCurve yOffsetCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Stop Condition")]
    public bool stopWhenAnotherCameraEventStarts = true;
    public float ignoreCameraEventCheckTime = 0.25f;

    [Header("Repeat")]
    public bool activateOnlyOnce = false;
    public bool restartIfTriggeredAgain = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Collider2D areaCollider;
    private EnterSide enterSide = EnterSide.None;

    private bool hasActivated;
    private Coroutine zoomCoroutine;
    private Coroutine clearYOffsetCoroutine;

    private Vector3 originalCameraLocalPosition;
    private bool hasOriginalCameraLocalPosition;

    private float currentYOffset;

    private void Awake()
    {
        RefreshReferences();
        areaCollider = GetComponent<Collider2D>();
        CacheOriginalCameraLocalPosition();
    }

    private void LateUpdate()
    {
        ApplyCameraLocalYOffset();
    }

    private void RefreshReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void CacheOriginalCameraLocalPosition()
    {
        RefreshReferences();

        if (targetCamera == null)
            return;

        if (hasOriginalCameraLocalPosition)
            return;

        originalCameraLocalPosition = targetCamera.transform.localPosition;
        hasOriginalCameraLocalPosition = true;
    }

    private void ApplyCameraLocalYOffset()
    {
        if (!useCameraLocalYOffset)
            return;

        RefreshReferences();

        if (targetCamera == null)
            return;

        CacheOriginalCameraLocalPosition();

        Vector3 localPos = originalCameraLocalPosition;
        localPos.y += currentYOffset;

        targetCamera.transform.localPosition = localPos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        enterSide = GetPlayerSide(collision.transform.position);

        if (showDebugLog)
            Debug.Log($"{gameObject.name} Enter Side : {enterSide}");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

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

        RefreshReferences();

        if (targetCamera == null)
            return;

        CacheOriginalCameraLocalPosition();

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

        zoomCoroutine = StartCoroutine(ZoomRoutine());
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

    private IEnumerator ZoomRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        RefreshReferences();

        if (targetCamera == null)
            yield break;

        float startFieldOfView = targetCamera.fieldOfView;
        float startYOffset = currentYOffset;

        float realYOffsetTime = yOffsetTime <= 0f ? zoomTime : yOffsetTime;

        float timer = 0f;
        bool stoppedByNextCameraArea = false;

        while (timer < zoomTime)
        {
            timer += Time.deltaTime;

            if (
                stopWhenAnotherCameraEventStarts &&
                timer > ignoreCameraEventCheckTime &&
                CameraMovement.Instance != null &&
                CameraMovement.Instance.isMovingEvent
            )
            {
                stoppedByNextCameraArea = true;
                break;
            }

            float zoomT = zoomTime <= 0f ? 1f : Mathf.Clamp01(timer / zoomTime);
            float curvedZoomT = zoomCurve.Evaluate(zoomT);

            targetCamera.fieldOfView = Mathf.Lerp(
                startFieldOfView,
                targetFieldOfView,
                curvedZoomT
            );

            if (useCameraLocalYOffset)
            {
                float yT = realYOffsetTime <= 0f ? 1f : Mathf.Clamp01(timer / realYOffsetTime);
                float curvedYT = yOffsetCurve.Evaluate(yT);

                currentYOffset = Mathf.Lerp(
                    startYOffset,
                    targetYOffset,
                    curvedYT
                );
            }

            ApplyCameraLocalYOffset();

            if (showDebugLog)
            {
                Debug.Log($"{gameObject.name} Zooming / FOV: {targetCamera.fieldOfView} / YOffset: {currentYOffset}");
            }

            yield return null;
        }

        if (!stoppedByNextCameraArea)
        {
            targetCamera.fieldOfView = targetFieldOfView;

            if (useCameraLocalYOffset)
            {
                if (keepYOffsetAfterZoom)
                    currentYOffset = targetYOffset;
                else
                    currentYOffset = 0f;
            }

            ApplyCameraLocalYOffset();
        }

        zoomCoroutine = null;
    }

    public void StopZoom()
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }
    }

    public void ClearYOffset()
    {
        StopZoom();

        if (clearYOffsetCoroutine != null)
        {
            StopCoroutine(clearYOffsetCoroutine);
            clearYOffsetCoroutine = null;
        }

        currentYOffset = 0f;
        ApplyCameraLocalYOffset();
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
        float startYOffset = currentYOffset;
        float timer = 0f;

        while (timer < clearTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / clearTime);
            float curvedT = yOffsetCurve.Evaluate(t);

            currentYOffset = Mathf.Lerp(startYOffset, 0f, curvedT);

            ApplyCameraLocalYOffset();

            yield return null;
        }

        ClearYOffset();

        clearYOffsetCoroutine = null;
    }
}