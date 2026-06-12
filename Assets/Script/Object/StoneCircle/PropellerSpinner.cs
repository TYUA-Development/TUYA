using UnityEngine;

public class PropellerSpinner : MonoBehaviour
{
    public enum RotateAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation")]
    public RotateAxis rotateAxis = RotateAxis.Z;

    [Tooltip("현재 회전 속도")]
    public float currentSpeed = 0f;

    [Tooltip("목표 회전 속도")]
    public float targetSpeed = 0f;

    [Tooltip("목표 속도까지 천천히 도달하는 힘")]
    public float acceleration = 120f;

    [Tooltip("처음부터 돌게 할지 여부")]
    public bool spinOnStart = false;

    [Tooltip("처음부터 돌릴 때 사용할 속도")]
    public float startTargetSpeed = 180f;

    private void Start()
    {
        if (spinOnStart)
        {
            targetSpeed = startTargetSpeed;
        }
    }

    private void Update()
    {
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        Vector3 axis = GetAxisVector();
        transform.Rotate(axis * currentSpeed * Time.deltaTime, Space.Self);
    }

    public void SetTargetSpeed(float speed)
    {
        targetSpeed = speed;
    }

    public void StopSpin()
    {
        targetSpeed = 0f;
    }

    private Vector3 GetAxisVector()
    {
        if (rotateAxis == RotateAxis.X)
            return Vector3.right;

        if (rotateAxis == RotateAxis.Y)
            return Vector3.up;

        return Vector3.forward;
    }
}