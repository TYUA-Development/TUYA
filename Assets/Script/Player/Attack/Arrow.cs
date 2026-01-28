using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public float flyTime = 2.0f;
    public float gravityValue = 3.0f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        gravityValue = rb.gravityScale;
        rb.gravityScale = 0.0f;
    }

    // 외부(플레이어)에서 방향을 넘겨서 쏘는 함수
    public void Launch(Vector2 dir)
    {
        dir = dir.normalized;

        rb.velocity = dir * speed;        // 초기 속도
        transform.right = dir;            // 화살 앞부분이 방향 보게 회전

        Destroy(gameObject, lifeTime);    // 일정 시간 뒤 자동 삭제
    }

    private void Update()
    {
        if(flyTime > 0)
            flyTime -= Time.deltaTime;
        else
            rb.gravityScale = gravityValue;
    }

    void FixedUpdate()
    {
        // 속도 방향으로 계속 회전(포물선 꺾일 때 화살도 같이 숙여짐)
        if (rb.velocity.sqrMagnitude > 0.01f)
            transform.right = rb.velocity;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 맞으면 멈추고, 데미지 주고, 파괴 등등
        // rb.isKinematic = true;  // 벽에 꽂히게 고정하고 싶으면 이런 것도 가능
        Destroy(gameObject);
    }
}
