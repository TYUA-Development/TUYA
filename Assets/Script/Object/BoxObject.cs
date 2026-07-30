using UnityEngine;

public class BoxObject : MonoBehaviour, IArrowHit, IArrowKnockbackReceiver
{
    [Header("Knockback")]
    [Tooltip("화살에 맞았을 때 화살이 날아간 방향으로 가해지는 힘의 크기")]
    public float knockbackForce = 10f;

    [Header("Player Collision")]
    [Tooltip("Player와 충돌을 무시할 콜라이더")]
    public BoxCollider2D boxCollider2D;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        IgnorePlayerCollision();
    }

    private void IgnorePlayerCollision()
    {
        if (boxCollider2D == null)
            return;

        var player = FindObjectOfType<PlayerController>();
        if (player != null && player.TryGetComponent<Collider2D>(out var playerCollider))
            Physics2D.IgnoreCollision(boxCollider2D, playerCollider, true);
    }

    public void OnHit()
    {
    }

    public void OnArrowKnockback(Vector2 hitPoint, Vector2 hitDirection)
    {
        if (rb == null)
            return;

        float xDir = Mathf.Sign(hitDirection.x);
        rb.AddForce(new Vector2(xDir * knockbackForce, 0f), ForceMode2D.Impulse);
    }
}
