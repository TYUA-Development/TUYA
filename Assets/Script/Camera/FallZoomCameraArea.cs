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
    [Tooltip("üũ�ϸ� �� ��ũ��Ʈ�� X�� ���� �ǵ帮�� �ʽ��ϴ�. X�� ���� CameraMovement�� ����մϴ�.")]
    public bool neverControlX = true;

    [Tooltip("Area �ȿ� �ִ� ���� �Ϲ� CameraMovement�� ��� �۵��ϵ��� �մϴ�.")]
    public bool keepNormalCameraMovementActive = true;

    [Header("X Position Progress")]
    [Tooltip("�ð��� �ƴ϶� Area �ȿ����� X ��ġ �������� ��/Y ��ȭ�� �˴ϴ�.")]
    public bool useXPositionProgress = true;

    [Tooltip("Area�� ������ �� ���������� �� % ������ �������� �� �������� ����. 0.5 = Area�� 50% �������� ������.")]
    [Range(0.05f, 1f)]
    public float fullEffectAtWidthPercent = 0.5f;

    [Tooltip("������ ���� �� Area �ȿ� �ִ� ���� ��� �������� �����մϴ�.")]
    public bool holdFullEffectAfterReached = true;

    [Tooltip("X ��ġ�� ���� ��ȭ �")]
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Zoom")]
    [Tooltip("���� Ȯ�� �� ��")]
    public float zoomedFieldOfView = 55f;

    [Header("Y Framing")]
    [Tooltip("���൵�� ���� ī�޶� Y�� ���� �������� ����ϴ�.")]
    public bool controlYByProgress = true;

    [Tooltip("���� �� ���� ���� ī�޶� Y ������. ����� ī�޶� ���ߺ��� ���� ���ϴ�.")]
    public float playerFrameYOffset = 1.8f;

    [Header("Fall Follow Y")]
    [Tooltip("Y Follow�� ���� ����")]
    public FollowYStartMode followYStartMode = FollowYStartMode.WhenPlayerBelowY;

    [Tooltip("�÷��̾ �� Y�� �Ʒ��� �������� ���� ������ �����ϴ�.")]
    public float followStartWorldY = 0f;

    [Tooltip("���� �� ���� ���� ī�޶� Y ������. ����� ���ߺ��� ���� ���ϴ�.")]
    public float fallCameraYOffset = 1.8f;

    [Tooltip("���ϰ� ���۵Ǹ� X ���൵�� ������� ���� ��/Y�� �����մϴ�.")]
    public bool keepFullEffectAfterFallStarts = true;

    [Header("Zoom Out On Exit")]
    public bool zoomOutOnExit = true;

    [Tooltip("������ �� �� ������ ��������")]
    public bool restoreToEnterFieldOfView = true;

    [Tooltip("Restore To Enter Field Of View�� ���� ���� �� ����� �� ������")]
    public float zoomOutFieldOfView = 60f;

    [Tooltip("��� �ð�")]
    public float zoomOutTime = 1.2f;

    public AnimationCurve zoomOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Exit")]
    [Tooltip("Area ������ ������ ���� Y Follow�� ���ϴ�.")]
    public bool disableFollowYOnExit = true;

    [Tooltip("Area ������ ������ CameraMovement���� ������� �����ݴϴ�.")]
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
        {
            // 줌: 콜라이더 중심 기준 X 거리 (양방향 zoom-out 가능)
            // Y: 항상 1f로 고정 — 플레이어 현재 위치를 완전 추적
            ApplyZoomByProgress(GetProgressByPlayerXFromCenter());
            ApplyYByProgress(1f);
        }
        else
        {
            ApplyZoomByProgress(progress);
            ApplyYByProgress(progress);
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

    private float GetProgressByPlayerXFromCenter()
    {
        if (playerTransform == null) return 0f;

        Bounds bounds = GetAreaBounds();
        float halfWidth = Mathf.Max(bounds.extents.x, 0.001f);
        float distFromCenter = Mathf.Abs(playerTransform.position.x - bounds.center.x);

        float t = 1f - Mathf.Clamp01(distFromCenter / halfWidth);

        if (progressCurve != null)
            t = progressCurve.Evaluate(t);

        return t;
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

        // �߿�:
        // X�� ���� �ǵ帮�� �ʽ��ϴ�.
        // X�� ���� CameraMovement�� ��� ����մϴ�.
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