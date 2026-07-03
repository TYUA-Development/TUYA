using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreCameraFocus2D : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("���� ī�޶�. ���� Main Camera�� ������ �˴ϴ�.")]
    public Camera targetCamera;

    [Tooltip("������ ������ ī�޶� ��Ʈ. CameraRig�� ��������.")]
    public Transform cameraMoveRoot;

    [Header("Player")]
    [Tooltip("ī�޶� �ٽ� ���ƿ� �÷��̾�")]
    public Transform playerTarget;

    [Header("Focus Target Position")]
    [Tooltip("Ÿ�� ��ġ���� �߰��� �󸶳� ��/���� ����")]
    public Vector2 focusOffset = new Vector2(0f, 0.5f);

    [Header("Partial Focus")]
    [Tooltip("üũ�ϸ� Ÿ���� ������ �߾ӿ� ���� �ʰ�, ���� ī�޶� ��ġ���� �Ϻθ� �̵��մϴ�.")]
    public bool usePartialFocus = true;

    [Tooltip("X������ Ÿ���� �󸶳� ������. 0�̸� �� �����̰�, 1�̸� ������ Ÿ���� ���ϴ�.")]
    [Range(0f, 1f)]
    public float horizontalFocusAmount = 0.65f;

    [Tooltip("Y������ Ÿ���� �󸶳� ������. 0�̸� �� �����̰�, 1�̸� ������ Ÿ���� ���ϴ�.")]
    [Range(0f, 1f)]
    public float verticalFocusAmount = 0.12f;

    [Header("Max Camera Move Limit")]
    [Tooltip("X�� �ִ� �̵� �Ÿ�. �ʹ� �ָ� �̵��ϴ� �� �����ϴ�.")]
    public float maxMoveX = 5.5f;

    [Tooltip("Y�� �ִ� �̵� �Ÿ�. �ʹ� ���Ʒ��� �̵��ϴ� �� �����ϴ�.")]
    public float maxMoveY = 1f;

    [Header("Focus Settings")]
    [Tooltip("Ÿ�� ������ �̵��ϴ� �ð�")]
    public float moveToTargetTime = 1.8f;

    [Tooltip("Ÿ�� ���� �ٶ󺸸� �����ϴ� �ð�")]
    public float holdTime = 6f;

    [Tooltip("�÷��̾�� ���ƿ��� �ð�")]
    public float returnTime = 1.8f;

    [Tooltip("ī�޶� �̵� �")]
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hold Lock")]
    [Tooltip("üũ�ϸ� Hold Time ���� ī�޶� ��ġ�� ������ �����մϴ�. ���� ī�޶� ���� ��ũ��Ʈ�� ���� ���� ������ �����ϴ�.")]
    public bool forceHoldPosition = true;

    [Header("Return")]
    [Tooltip("üũ�ϸ� �÷��̾� ��ġ�� �ƴ϶�, ���� ���� �� ī�޶� ��ġ�� ���ư��ϴ�.")]
    public bool returnToStartPosition = true;

    [Header("Disable Camera Scripts During Focus")]
    [Tooltip("���� ī�޶� ���󰡱� ��ũ��Ʈ�� ������ ���⿡ ��������.")]
    public MonoBehaviour[] scriptsToDisableDuringFocus;

    [Tooltip("üũ�ϸ� CameraRig�� �پ��ִ� �ٸ� ī�޶� ��ũ��Ʈ�� �ڵ����� ��� ���ϴ�.")]
    public bool autoDisableOtherScriptsOnCameraRoot = true;

    [Header("State")]
    public bool isFocusing;

    private Coroutine focusCoroutine;
    private readonly List<MonoBehaviour> autoDisabledScripts = new List<MonoBehaviour>();

    private void Awake()
    {
         if(playerTarget == null)
        {
            playerTarget = FindObjectOfType<PlayerController>().transform;
        }

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