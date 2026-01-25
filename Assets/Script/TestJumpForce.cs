using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestJumpForce : MonoBehaviour
{
    Rigidbody2D rb;

    public float jumpPower = 10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log("[Test] BodyType=" + rb.bodyType +
                  ", simulated=" + rb.simulated +
                  ", gravityScale=" + rb.gravityScale);
    }

    void Start()
    {
        Debug.Log("[Test] Start velY(before)=" + rb.velocity.y);
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        Debug.Log("[Test] Start velY(after AddForce)=" + rb.velocity.y);
    }

    void FixedUpdate()
    {
        Debug.Log("[Test] Fixed velY=" + rb.velocity.y);
    }
}
