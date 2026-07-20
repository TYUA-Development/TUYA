using System.Collections.Generic;
using UnityEngine;

public enum WindDirection
{
    Right,
    UpRight,
    Up,
    UpLeft,
    Left,
    DownLeft,
    Down,
    DownRight
}

public class Object_Wind : MonoBehaviour, ICoreEvent
{
    public float windPower;

    [Tooltip("바람이 향하는 방향. windPower가 음수이면 이 방향의 반대로 힘이 작용합니다.")]
    public WindDirection windDirection = WindDirection.Right;

    public bool blockPlayer;

    [Header("Distance Falloff")]
    [Range(0f, 10f)]
    [Tooltip("이 오브젝트(transform.position)에서 멀어질수록 바람 세기가 감소하는 정도. 0 = 감소 없음(거리와 무관하게 동일한 힘), 10 = 아주 빠르게 감소")]
    public float distanceFalloff = 0f;

    [Header("Object Blocking")]
    [Tooltip("이 레이어의 콜라이더가 바람 오브젝트와 대상 사이를 가로막으면(Box 형태로 경로 체크) 그 대상은 바람 영향을 받지 않습니다. 예: Wall, Floor")]
    public LayerMask blockingLayer;

    private Vector2 direction;
    private Vector2 power;

    private Dictionary<Collider2D, Rigidbody2D> colliderList = new Dictionary<Collider2D, Rigidbody2D> ();

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    public void Init()
    {
        direction = GetDirectionVector(windDirection);
        power = direction * windPower;
    }

    public static Vector2 GetDirectionVector(WindDirection dir)
    {
        switch (dir)
        {
            case WindDirection.Right: return Vector2.right;
            case WindDirection.UpRight: return new Vector2(1f, 1f).normalized;
            case WindDirection.Up: return Vector2.up;
            case WindDirection.UpLeft: return new Vector2(-1f, 1f).normalized;
            case WindDirection.Left: return Vector2.left;
            case WindDirection.DownLeft: return new Vector2(-1f, -1f).normalized;
            case WindDirection.Down: return Vector2.down;
            case WindDirection.DownRight: return new Vector2(1f, -1f).normalized;
            default: return Vector2.right;
        }
    }

    public void FixedUpdate()
    {
        foreach (var kvp in colliderList)
        {
            Rigidbody2D rb = kvp.Value;

            if (IsBlocked(kvp.Key, rb.position))
                continue;

            float falloff = GetFalloffMultiplier(rb.position);
            rb.velocity += power * falloff * Time.deltaTime;
        }
    }

    private float GetFalloffMultiplier(Vector2 targetPosition)
    {
        if (distanceFalloff <= 0f)
            return 1f;

        float distance = Vector2.Distance(transform.position, targetPosition);
        return 1f / (1f + distanceFalloff * distance);
    }

    private bool IsBlocked(Collider2D targetCollider, Vector2 targetPosition)
    {
        if (blockingLayer.value == 0)
            return false;

        Vector2 origin = transform.position;
        Vector2 toTarget = targetPosition - origin;
        float distance = toTarget.magnitude;

        if (distance <= 0.0001f)
            return false;

        Vector2 direction = toTarget / distance;
        Vector2 boxSize = targetCollider != null ? (Vector2)targetCollider.bounds.size : new Vector2(0.1f, 0.1f);

        return Physics2D.BoxCast(origin, boxSize, 0f, direction, distance, blockingLayer);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (blockPlayer && collision.CompareTag("Player"))
            return;

        if (collision.TryGetComponent(out Rigidbody2D rb))
        {
            colliderList[collision] = rb;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        colliderList.Remove(collision);
    }

    public void OnCoreEvent(bool isPressed = true)
    {

    }
}
