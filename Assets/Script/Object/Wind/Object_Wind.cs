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

    [Tooltip("이 레이어의 오브젝트는 바람 영역에 들어와도 영향을 받지 않습니다.")]
    public LayerMask ignoredLayer;

    [Header("Block Feel")]
    [Tooltip("blockPlayer로 막힌 플레이어를 트리거 밖으로 밀어내는 최대 속도(유닛/초). 겹친 깊이를 한 프레임에 순간 보정하지 않고 이 속도로 서서히 밀어내 순간이동처럼 보이지 않게 한다.")]
    public float blockPushSpeed = 15f;

    [Range(0f, 1f)]
    [Tooltip("막힐 때 진입 속도를 얼마나 반동으로 되돌릴지. 0 = 그 자리에서 멈춤, 1 = 들어온 속도 그대로 튕겨나감.")]
    public float blockBounciness = 0.3f;

    private Vector2 direction;
    private Vector2 power;

    private Dictionary<Collider2D, Rigidbody2D> colliderList = new Dictionary<Collider2D, Rigidbody2D> ();
    private readonly HashSet<Collider2D> fallThroughExempt = new HashSet<Collider2D>();

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
        if (((1 << collision.gameObject.layer) & ignoredLayer.value) != 0)
            return;

        if (collision.CompareTag("Player"))
        {
            // 위에서 낙하해 들어온 경우 힘도 적용하지 않고 막지도 않는다. 완전히 빠져나갈
            // 때까지(OnTriggerExit2D) 이 상태를 유지해서, 낙하 도중 다시 힘/차단이 끼어들지 않게 한다.
            if (IsFallingFromAbove(collision))
            {
                fallThroughExempt.Add(collision);
                return;
            }

            if (blockPlayer)
            {
                BlockPlayer(collision);
                return;
            }
        }

        if (collision.TryGetComponent(out Rigidbody2D rb))
        {
            colliderList[collision] = rb;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || fallThroughExempt.Contains(collision))
            return;

        if (blockPlayer)
            BlockPlayer(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        colliderList.Remove(collision);
        fallThroughExempt.Remove(collision);
    }

    private bool IsFallingFromAbove(Collider2D playerCollider)
    {
        if (playerCollider.attachedRigidbody == null)
            return false;

        Collider2D selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null)
            return false;

        Vector2 previousPosition = playerCollider.attachedRigidbody.position
            - playerCollider.attachedRigidbody.velocity * Time.fixedDeltaTime;

        // 직전 프레임에 바람 콜라이더 윗면보다 위에 있었다면(=위에서 낙하해 들어온 것)
        return previousPosition.y >= selfCollider.bounds.max.y;
    }

    // 미는 방향은 속도가 아니라 두 콜라이더의 AABB 겹침 깊이로 정한다. 더 얕게 겹친 축이
    // 실제로 플레이어가 파고든 축이라고 보고 그 축의 반대 방향(플레이어가 있던 쪽)으로 민다.
    // 속도 기반 방식은 점프/낙하로 인한 수직 속도가 섞이면 엉뚱한 축으로 미는 문제가 있었다.
    private static Vector2 ComputeAxisPushDirection(Bounds playerBounds, Bounds windBounds)
    {
        float overlapX = Mathf.Min(playerBounds.max.x, windBounds.max.x) - Mathf.Max(playerBounds.min.x, windBounds.min.x);
        float overlapY = Mathf.Min(playerBounds.max.y, windBounds.max.y) - Mathf.Max(playerBounds.min.y, windBounds.min.y);

        if (overlapX <= 0f && overlapY <= 0f)
            return Vector2.zero;

        if (overlapX < overlapY)
            return playerBounds.center.x < windBounds.center.x ? Vector2.left : Vector2.right;

        return playerBounds.center.y < windBounds.center.y ? Vector2.down : Vector2.up;
    }

    // blockPlayer가 켜져 있고 blockingLayer로 가로막는 실제 벽이 없을 때,
    // 바람 콜라이더 자체를 벽처럼 취급해 플레이어가 겹친 만큼 트리거 경계 밖으로 밀어낸다.
    private void BlockPlayer(Collider2D playerCollider)
    {
        Rigidbody2D rb = playerCollider.attachedRigidbody;
        if (rb == null)
            return;

        if (IsBlocked(playerCollider, rb.position))
            return;

        Collider2D selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null)
            return;

        ColliderDistance2D dist = playerCollider.Distance(selfCollider);
        if (!dist.isOverlapped)
            return;

        Vector2 pushDirection = ComputeAxisPushDirection(playerCollider.bounds, selfCollider.bounds);
        if (pushDirection == Vector2.zero)
            return;

        // 겹친 깊이를 한 번에 전부 보정하면 순간이동처럼 보이므로, blockPushSpeed로
        // 제한된 거리만큼만 서서히 밀어낸다.
        float penetration = -dist.distance;
        float step = Mathf.Min(blockPushSpeed * Time.deltaTime, penetration);
        rb.position += pushDirection * step;

        // 진입 방향 속도를 그대로 0으로 죽이지 않고, blockBounciness만큼 반동으로 되돌려
        // 벽에 튕기는 듯한 자연스러운 느낌을 준다.
        float velocityAlongPush = Vector2.Dot(rb.velocity, pushDirection);
        if (velocityAlongPush < 0f)
        {
            float bouncedVelocity = -velocityAlongPush * blockBounciness;
            rb.velocity += pushDirection * (bouncedVelocity - velocityAlongPush);
        }
    }

    public void OnCoreEvent(bool isPressed = true)
    {

    }
}
