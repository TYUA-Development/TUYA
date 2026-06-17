using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(95000)]
public class ZoneParticleAndZoomRestore : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool activateOnlyOnce = true;

    [Header("Particles")]
    public ParticleSystem[] particlesToPlay;
    public GameObject[] objectsToEnable;

    public bool restartParticlesOnEnter = true;
    public bool stopParticlesOnExit = false;
    public bool clearParticlesOnExit = false;

    [Header("Camera")]
    public Camera targetCamera;

    [Tooltip("체크하면 게임 시작 시점의 카메라 줌 값을 원래 크기로 저장합니다.")]
    public bool useStartZoomAsNormal = true;

    [Tooltip("useStartZoomAsNormal을 끄면 이 값을 원래 카메라 크기로 사용합니다. 2D 카메라는 Orthographic Size입니다.")]
    public float normalZoomValue = 8f;

    [Tooltip("구역 진입 후 줌 복구 시작 전 대기 시간")]
    public float zoomStartDelay = 0f;

    [Tooltip("원래 크기로 돌아가는 시간")]
    public float zoomRestoreTime = 1.4f;

    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Camera Movement Conflict Safety")]
    [Tooltip("줌 복구 중 CameraMovement 이벤트 상태를 잠깐 켭니다. 위치 추적 충돌 방지용입니다.")]
    public bool takeCameraOwnershipWhileZooming = false;

    [Tooltip("줌 복구 후 CameraMovement 제어권을 돌려줍니다.")]
    public bool returnControlAfterZoom = true;

    [Header("State")]
    public bool hasActivated;
    public bool playerInside;
    public bool isZooming;

    private Coroutine zoomCoroutine;
    private float savedNormalZoomValue;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            if (useStartZoomAsNormal)
                savedNormalZoomValue = GetCurrentZoomValue();
            else
                savedNormalZoomValue = normalZoomValue;
        }
        else
        {
            savedNormalZoomValue = normalZoomValue;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        if (activateOnlyOnce && hasActivated)
            return;

        playerInside = true;
        hasActivated = true;

        EnableObjects();
        PlayParticles();
        StartZoomRestore();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        playerInside = false;

        if (stopParticlesOnExit)
            StopParticles();
    }

    private void StartZoomRestore()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ZoomRestoreRoutine());
    }

    private IEnumerator ZoomRestoreRoutine()
    {
        isZooming = true;

        if (zoomStartDelay > 0f)
            yield return new WaitForSeconds(zoomStartDelay);

        float startZoom = GetCurrentZoomValue();
        float targetZoom = savedNormalZoomValue;

        float timer = 0f;

        while (timer < zoomRestoreTime)
        {
            timer += Time.deltaTime;

            if (takeCameraOwnershipWhileZooming && CameraMovement.Instance != null)
            {
                CameraMovement.Instance.isMovingEvent = true;
            }

            float t = zoomRestoreTime <= 0f ? 1f : Mathf.Clamp01(timer / zoomRestoreTime);
            float curvedT = zoomCurve.Evaluate(t);

            float nextZoom = Mathf.Lerp(startZoom, targetZoom, curvedT);
            ApplyZoomValue(nextZoom);

            yield return null;
        }

        ApplyZoomValue(targetZoom);

        if (returnControlAfterZoom && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }

        isZooming = false;
        zoomCoroutine = null;
    }

    private float GetCurrentZoomValue()
    {
        if (targetCamera == null)
            return savedNormalZoomValue;

        if (targetCamera.orthographic)
            return targetCamera.orthographicSize;

        return targetCamera.fieldOfView;
    }

    private void ApplyZoomValue(float value)
    {
        if (targetCamera == null)
            return;

        if (targetCamera.orthographic)
            targetCamera.orthographicSize = value;
        else
            targetCamera.fieldOfView = value;
    }

    private void EnableObjects()
    {
        if (objectsToEnable == null)
            return;

        for (int i = 0; i < objectsToEnable.Length; i++)
        {
            if (objectsToEnable[i] == null)
                continue;

            objectsToEnable[i].SetActive(true);
        }
    }

    private void PlayParticles()
    {
        if (particlesToPlay == null)
            return;

        for (int i = 0; i < particlesToPlay.Length; i++)
        {
            if (particlesToPlay[i] == null)
                continue;

            if (restartParticlesOnEnter)
            {
                particlesToPlay[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particlesToPlay[i].Play(true);
            }
            else
            {
                if (!particlesToPlay[i].isPlaying)
                    particlesToPlay[i].Play(true);
            }
        }
    }

    private void StopParticles()
    {
        if (particlesToPlay == null)
            return;

        ParticleSystemStopBehavior stopBehavior = clearParticlesOnExit
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        for (int i = 0; i < particlesToPlay.Length; i++)
        {
            if (particlesToPlay[i] == null)
                continue;

            particlesToPlay[i].Stop(true, stopBehavior);
        }
    }

    private void OnDisable()
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        isZooming = false;

        if (returnControlAfterZoom && CameraMovement.Instance != null)
        {
            CameraMovement.Instance.isMovingEvent = false;
        }
    }
}