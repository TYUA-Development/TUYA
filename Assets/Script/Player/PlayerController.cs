using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

public class PlayerController : MonoBehaviour
{
    // 플레이어의 입력을 저장
    public PlayerInputReader InputReader { get; private set; }
    public Rigidbody2D Rigidbody2D;

    // 현재 플레이어가 땅에 닿아있는지
    public bool isGround;
    public bool isDash;

    // 플레이어의 기본적인 수치를 Inspector에서 설정하기 위해
    public float moveSpeed;
    public float jumpPower;
    public float dashPower;
    [SerializeField] private float attackCoolTime;
    public float attackTimer;

    // 플레이어의 상태들
    public PlayerState currentState;
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerJumpState jumpState;
    public PlayerDashState dashState;
    public PlayerAttackState attackState;

    //플레이어 상태들을 저장한 상태리스트
    public List<PlayerState> states = new List<PlayerState>();

    public Animator animator;

    // 플레이어 공격 화살 prefab
    public GameObject arrowObject;

    private void Awake()
    {
        InputReader = GetComponent<PlayerInputReader>();
        //Rigidbody2D = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();

        // PlayerState를 상속받는 상태들을 생성자로 PlayerController를 넘겨주며 초기화
        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        jumpState = new PlayerJumpState(this);
        dashState = new PlayerDashState(this);
        attackState = new PlayerAttackState(this); 

        states.Add(idleState); states.Add(moveState); states.Add(jumpState); states.Add(dashState); states.Add(attackState);

        foreach (PlayerState s in states)
        {
            //s.Init();
        }

        // 현재 상태를 Idle로 설정
        currentState = idleState;
    }

    // 매 프레임 로직을 체크해 상태 변환
    void Update()
    {
        currentState.LogicUpdate();
        CoolDown();
    }

    // 플레이어 현재 상태의 움직임에 관한 처리
    private void FixedUpdate()
    {
        currentState.PhysicsUpdate();
    }

    // 플레이어의 현재 상태를 변경시키기 위한 함수
    //----------------------------------
    public void OnIdle()
    {
        ChangeState(idleState);
    }

    public void OnMove()
    {
        ChangeState(moveState);
    }

    public void OnJump()
    {
        ChangeState(jumpState);
    }

    public void OnDash()
    {
        ChangeState(dashState);
    }

    public void OnAttack()
    {
        ChangeState(attackState);
    }
    //----------------------------------

    // 플레이어의 상태를 변경시키고 Enter 함수를 실행
    private void ChangeState(PlayerState state)
    {
        currentState.Exit();
        currentState = state;
        currentState.Enter();
    }

    // 플레이어의 방향을 바꾸는 함수
    public void ChangeDirection(float dir)
    {
        if (dir == 0)
            return;

        if(dir == transform.localScale.x)
            transform.localScale = new Vector3(dir * -1, transform.localScale.y, transform.localScale.z);
    }

    // 해당 방향으로 화살을 발사하는 함수
    public void ShootArrow(Vector2 direction)
    {
        Vector3 handLength = new Vector3(direction.x * 1.5f, direction.y * 1.5f, 0);
        Arrow arrow = Instantiate(arrowObject, transform.position + handLength, Quaternion.identity).GetComponent<Arrow>();
        arrow.Launch(direction);
        attackTimer = attackCoolTime;
    }

    // 쿨타임들을 관리해주는 함수
    private void CoolDown()
    {
        if(attackTimer > 0)
            attackTimer -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Floor"))
        {
            isDash = true;
            isGround = true;
        }
    }

    
}