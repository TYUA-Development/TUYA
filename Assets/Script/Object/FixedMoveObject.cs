using System.Collections;
using UnityEngine;

// 물리로 자연스럽게 낙하하다가 지정된 바닥 레이어와 처음 충돌하면 Kinematic으로 전환하고,
// 오브젝트 자신의 원점(transform.position)이 targetPosition으로, 각도가 targetAngle로
// 향하도록 서서히 맞춘다. 회전은 pivot(축)을 중심으로 일어나 자연스럽게 보이지만, 최종적으로
// targetPosition/targetAngle에 도달하는 것은 pivot이 아니라 오브젝트 원점이다. 이를 위해
// "오브젝트가 targetPosition/targetAngle에 도달했을 때 pivot이 있어야 할 위치"를 먼저
// 역산해두고, pivot이 그 지점으로 이동하도록 보간한다.
[RequireComponent(typeof(Rigidbody2D))]
public class FixedMoveObject : MonoBehaviour
{
    [Header("Landing")]
    [Tooltip("바닥으로 인식할 레이어. 이 레이어와 처음 충돌하는 순간 물리 낙하를 멈추고 Kinematic으로 전환한 뒤 아래 목표 위치/각도로 서서히 자세를 맞춥니다.")]
    public LayerMask floorLayer;

    [Tooltip("회전의 기준이 되는 축(피벗). 예: 오브젝트 맨 아래에 놓인 자식 Transform. 비워두면 오브젝트 자신의 Transform을 기준으로 씁니다. 전환 도중 회전 중심점으로만 쓰이며, 최종 위치/각도의 기준은 이 지점이 아니라 오브젝트 원점(Transform.position)입니다.")]
    public Transform pivot;

    [Tooltip("착지 후 오브젝트(Transform.position)가 최종적으로 위치해야 할 월드 좌표")]
    public Vector2 targetPosition;

    [Tooltip("착지 후 오브젝트의 최종 각도 (Z축, degree)")]
    public float targetAngle;

    [Tooltip("착지 후 목표 위치/각도로 맞춰지는 데 걸리는 시간(초)")]
    public float settleDuration = 0.3f;

    [Tooltip("착지 후 자세를 맞추는 보간 진행 곡선 (0=착지 시점, 1=목표 위치/각도 도달)")]
    public AnimationCurve settleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Rigidbody2D rb;
    private bool hasLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded)
            return;

        if (((1 << collision.gameObject.layer) & floorLayer.value) == 0)
            return;

        TriggerSettle();
    }

    // 물리 충돌(OnCollisionEnter2D)이 아니라 외부 코드(예: FixedMoveObject_Rope)가 직접 정착을
    // 시작시키고 싶을 때 쓴다. Kinematic Rigidbody2D는 Static 콜라이더(바닥)와 충돌/트리거
    // 이벤트를 만들지 않으므로(Unity 2D 충돌 매트릭스), 다른 정착 스크립트가 이미 이 오브젝트를
    // Kinematic으로 바꿔놓은 뒤에는 OnCollisionEnter2D가 애초에 발생하지 않아 스스로 트리거되지
    // 못한다.
    public void TriggerSettle()
    {
        if (hasLanded)
            return;

        hasLanded = true;
        StartCoroutine(SettleRoutine());
    }

    private IEnumerator SettleRoutine()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Transform pivotTransform = pivot != null ? pivot : transform;

        float startAngle = transform.eulerAngles.z;
        float startZ = transform.position.z;

        // pivot이 오브젝트 원점 기준으로 갖는, 회전에 무관한(un-rotate된) 로컬 오프셋.
        Vector2 worldOffsetAtStart = (Vector2)pivotTransform.position - (Vector2)transform.position;
        Vector2 localOffset = Quaternion.Inverse(transform.rotation) * (Vector3)worldOffsetAtStart;

        Vector2 startPivotWorldPos = pivotTransform.position;

        // 오브젝트 원점이 targetPosition/targetAngle에 도달했을 때 pivot이 있어야 할 위치를 역산.
        // pivot을 (직접 targetPosition이 아니라) 이 지점으로 보간해야, 회전은 pivot을 중심으로
        // 일어나면서도 최종적으로는 오브젝트 원점이 정확히 targetPosition에 도달한다.
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        Vector2 targetPivotWorldPos = targetPosition + (Vector2)(targetRotation * (Vector3)localOffset);

        float elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = settleCurve.Evaluate(Mathf.Clamp01(elapsed / settleDuration));

            ApplyPose(startPivotWorldPos, targetPivotWorldPos, startAngle, localOffset, startZ, t);

            yield return null;
        }

        ApplyPose(startPivotWorldPos, targetPivotWorldPos, startAngle, localOffset, startZ, 1f);
    }

    private void ApplyPose(Vector2 startPivotWorldPos, Vector2 targetPivotWorldPos, float startAngle, Vector2 localOffset, float startZ, float t)
    {
        float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, t);
        Vector2 currentPivotWorldPos = Vector2.Lerp(startPivotWorldPos, targetPivotWorldPos, t);

        Quaternion currentRotation = Quaternion.Euler(0f, 0f, currentAngle);
        Vector2 currentWorldOffset = currentRotation * (Vector3)localOffset;

        transform.rotation = currentRotation;
        transform.position = new Vector3(
            currentPivotWorldPos.x - currentWorldOffset.x,
            currentPivotWorldPos.y - currentWorldOffset.y,
            startZ);
    }
}
