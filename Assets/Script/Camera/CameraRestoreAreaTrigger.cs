using System.Collections;
using UnityEngine;

public class CameraRestoreAreaTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform cameraRig;

    [Header("Restore")]
    [SerializeField] private float restoreTime = 4f;
    [SerializeField] private float defaultFieldOfView = 60f;
    [SerializeField] private float defaultYOffset = 15.13f;
    [SerializeField] private bool restoreXToPlayer = true;
    [SerializeField] private bool restoreYToPlayerOffset = true;
    [SerializeField] private bool releaseCameraMovementAfterRestore = true;
    [SerializeField] private bool snapToExactDefaultOnComplete = false;
    [SerializeField] private AnimationCurve restoreCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private Coroutine restoreCoroutine;

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        RefreshReferences();

        if (targetCamera == null || cameraRig == null)
        {
            Debug.LogWarning($"{gameObject.name} : CameraRestoreAreaTrigger needs Camera and CameraRig.");
            return;
        }

        StopMissionAreaCameraControl();

        if (restoreCoroutine != null)
            StopCoroutine(restoreCoroutine);

        restoreCoroutine = StartCoroutine(RestoreCameraRoutine(collision.transform));
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

    private void StopMissionAreaCameraControl()
    {
        SH_MissionAreaCamera.ReleaseExitFollowToNormalCamera();

        PassThroughExitCameraZoom[] zoomControllers = FindObjectsOfType<PassThroughExitCameraZoom>();

        for (int i = 0; i < zoomControllers.Length; i++)
        {
            if (zoomControllers[i] != null)
                zoomControllers[i].StopZoom();
        }

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.isMovingEvent = true;
    }

    private IEnumerator RestoreCameraRoutine(Transform player)
    {
        Vector3 startPosition = cameraRig.position;
        float startFieldOfView = targetCamera.fieldOfView;
        float elapsed = 0f;
        float safeRestoreTime = Mathf.Max(0.01f, restoreTime);

        if (showDebugLog)
        {
            Debug.Log(
                $"{gameObject.name} Camera Restore Start / startFOV: {startFieldOfView:F3}, targetFOV: {defaultFieldOfView:F3}"
            );
        }

        while (elapsed < safeRestoreTime)
        {
            elapsed += Time.deltaTime;

            if (player == null)
                break;

            float t = Mathf.Clamp01(elapsed / safeRestoreTime);
            float curvedT = restoreCurve.Evaluate(t);

            Vector3 targetPosition = cameraRig.position;

            if (restoreXToPlayer)
                targetPosition.x = player.position.x;

            if (restoreYToPlayerOffset)
                targetPosition.y = player.position.y + defaultYOffset;

            targetPosition.z = startPosition.z;

            cameraRig.position = Vector3.Lerp(startPosition, targetPosition, curvedT);
            targetCamera.fieldOfView = Mathf.Lerp(startFieldOfView, defaultFieldOfView, curvedT);

            if (CameraMovement.Instance != null)
                CameraMovement.Instance.SetCameraRigY(cameraRig.position.y);

            yield return null;
        }

        if (snapToExactDefaultOnComplete)
        {
            if (player != null)
            {
                Vector3 finalPosition = cameraRig.position;

                if (restoreXToPlayer)
                    finalPosition.x = player.position.x;

                if (restoreYToPlayerOffset)
                    finalPosition.y = player.position.y + defaultYOffset;

                cameraRig.position = finalPosition;
            }

            targetCamera.fieldOfView = defaultFieldOfView;
        }

        if (CameraMovement.Instance != null)
        {
            if (player != null && restoreYToPlayerOffset)
                CameraMovement.Instance.SetCameraPosY(player.position.y);
            else
                CameraMovement.Instance.SetCameraRigY(cameraRig.position.y);

            if (releaseCameraMovementAfterRestore)
                CameraMovement.Instance.isMovingEvent = false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                $"{gameObject.name} Camera Restore Complete / finalFOV: {targetCamera.fieldOfView:F3}"
            );
        }

        restoreCoroutine = null;
    }

    private void OnDisable()
    {
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
            restoreCoroutine = null;
        }
    }
}
