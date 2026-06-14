using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreCameraFocus2D : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("실제 카메라. 보통 Main Camera를 넣으면 됩니다.")]
    public Camera targetCamera;

    [Tooltip("실제로 움직일 카메라 루트. CameraRig를 넣으세요.")]
    public Transform cameraMoveRoot;

    [Header("Player")]
    [Tooltip("카메라가 다시 돌아올 플레이어")]
    public Transform playerTarget;

    [Header("Focus Target Position")]
    [Tooltip("타겟 위치에서 추가로 얼마나 옆/위로 볼지")]
    public Vector2 focusOffset = new Vector2(0f, 0.5f);

    [Header("Partial Focus")]
    [Tooltip("체크하면 타겟을 완전히 중앙에 두지 않고, 현재 카메라 위치에서 일부만 이동합니다.")]
    public bool usePartialFocus = true;

    [Tooltip("X축으로 타겟을 얼마나 따라갈지. 0이면 안 움직이고, 1이면 완전히 타겟을 봅니다.")]
    [Range(0f, 1f)]
    public float horizontalFocusAmount = 0.65f;

    [Tooltip("Y축으로 타겟을 얼마나 따라갈지. 0이면 안 움직이고, 1이면 완전히 타겟을 봅니다.")]
    [Range(0f, 1f)]
    public float verticalFocusAmount = 0.12f;

    [Header("Max Camera Move Limit")]
    [Tooltip("X축 최대 이동 거리. 너무 멀리 이동하는 걸 막습니다.")]
    public float maxMoveX = 5.5f;

    [Tooltip("Y축 최대 이동 거리. 너무 위아래로 이동하는 걸 막습니다.")]
    public float maxMoveY = 1f;

    [Header("Focus Settings")]
    [Tooltip("타겟 쪽으로 이동하는 시간")]
    public float moveToTargetTime = 1.8f;

    [Tooltip("타겟 쪽을 바라보며 유지하는 시간")]
    public float holdTime = 6f;

    [Tooltip("플레이어에게 돌아오는 시간")]
    public float returnTime = 1.8f;

    [Tooltip("카메라 이동 곡선")]
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hold Lock")]
    [Tooltip("체크하면 Hold Time 동안 카메라 위치를 강제로 고정합니다. 기존 카메라 추적 스크립트가 끌고 가는 문제를 막습니다.")]
    public bool forceHoldPosition = true;

    [Header("Return")]
    [Tooltip("체크하면 플레이어 위치가 아니라, 연출 시작 전 카메라 위치로 돌아갑니다.")]
    public bool returnToStartPosition = true;

    [Header("Disable Camera Scripts During Focus")]
    [Tooltip("기존 카메라 따라가기 스크립트가 있으면 여기에 넣으세요.")]
    public MonoBehaviour[] scriptsToDisableDuringFocus;

    [Tooltip("체크하면 CameraRig에 붙어있는 다른 카메라 스크립트를 자동으로 잠깐 끕니다.")]
    public bool autoDisableOtherScriptsOnCameraRoot = true;

    [Header("State")]
    public bool isFocusing;

    private Coroutine focusCoroutine;
    private readonly List<MonoBehaviour> autoDisabledScripts = new List<MonoBehaviour>();

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraMoveRoot == null)
            cameraMoveRoot = transform;
    }

    public void FocusOnTarget(Transform focusTarget)
    {
        FocusOnTarget(focusTarget, holdTime);
    }

    public void FocusOnTarget(Transform focusTarget, float customHoldTime)
    {
        if (focusTarget == null)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraMoveRoot == null)
            cameraMoveRoot = transform;

        if (focusCoroutine != null)
            StopCoroutine(focusCoroutine);

        focusCoroutine = StartCoroutine(FocusRoutine(focusTarget, customHoldTime));
    }

    private IEnumerator FocusRoutine(Transform focusTarget, float customHoldTime)
    {
        isFocusing = true;

        Vector3 startPosition = cameraMoveRoot.position;

        SetCameraScriptsEnabled(false);

        Vector3 targetPosition = CalculateFocusPosition(startPosition, focusTarget);

        yield return StartCoroutine(MoveCameraRoot(startPosition, targetPosition, moveToTargetTime));

        yield return StartCoroutine(HoldCameraPosition(targetPosition, customHoldTime));

        Vector3 returnPosition = startPosition;

        if (!returnToStartPosition && playerTarget != null)
        {
            returnPosition = new Vector3(
                playerTarget.position.x,
                playerTarget.position.y,
                cameraMoveRoot.position.z
            );
        }

        yield return StartCoroutine(MoveCameraRoot(cameraMoveRoot.position, returnPosition, returnTime));

        SetCameraScriptsEnabled(true);

        isFocusing = false;
        focusCoroutine = null;
    }

    private Vector3 CalculateFocusPosition(Vector3 startPosition, Transform focusTarget)
    {
        Vector3 fullTargetPosition = new Vector3(
            focusTarget.position.x + focusOffset.x,
            focusTarget.position.y + focusOffset.y,
            startPosition.z
        );

        if (!usePartialFocus)
            return fullTargetPosition;

        float deltaX = fullTargetPosition.x - startPosition.x;
        float deltaY = fullTargetPosition.y - startPosition.y;

        deltaX *= horizontalFocusAmount;
        deltaY *= verticalFocusAmount;

        deltaX = Mathf.Clamp(deltaX, -maxMoveX, maxMoveX);
        deltaY = Mathf.Clamp(deltaY, -maxMoveY, maxMoveY);

        Vector3 partialTargetPosition = new Vector3(
            startPosition.x + deltaX,
            startPosition.y + deltaY,
            startPosition.z
        );

        return partialTargetPosition;
    }

    private IEnumerator MoveCameraRoot(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            cameraMoveRoot.position = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float curveT = cameraMoveCurve.Evaluate(t);

            cameraMoveRoot.position = Vector3.Lerp(from, to, curveT);

            yield return null;
        }

        cameraMoveRoot.position = to;
    }

    private IEnumerator HoldCameraPosition(Vector3 holdPosition, float duration)
    {
        if (duration <= 0f)
            yield break;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (forceHoldPosition)
                cameraMoveRoot.position = holdPosition;

            yield return null;
        }

        if (forceHoldPosition)
            cameraMoveRoot.position = holdPosition;
    }

    private void SetCameraScriptsEnabled(bool value)
    {
        if (!value)
        {
            autoDisabledScripts.Clear();

            if (autoDisableOtherScriptsOnCameraRoot && cameraMoveRoot != null)
            {
                MonoBehaviour[] behaviours = cameraMoveRoot.GetComponents<MonoBehaviour>();

                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];

                    if (behaviour == null)
                        continue;

                    if (behaviour == this)
                        continue;

                    if (!behaviour.enabled)
                        continue;

                    behaviour.enabled = false;
                    autoDisabledScripts.Add(behaviour);
                }
            }

            if (scriptsToDisableDuringFocus != null)
            {
                for (int i = 0; i < scriptsToDisableDuringFocus.Length; i++)
                {
                    MonoBehaviour script = scriptsToDisableDuringFocus[i];

                    if (script == null)
                        continue;

                    if (script == this)
                        continue;

                    if (!script.enabled)
                        continue;

                    script.enabled = false;

                    if (!autoDisabledScripts.Contains(script))
                        autoDisabledScripts.Add(script);
                }
            }
        }
        else
        {
            for (int i = 0; i < autoDisabledScripts.Count; i++)
            {
                if (autoDisabledScripts[i] == null)
                    continue;

                autoDisabledScripts[i].enabled = true;
            }

            autoDisabledScripts.Clear();
        }
    }
}