using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

// 추상 클래스로 생성
public abstract class PlayerState
{
    // 플레이어의 playerController를 저장하는 변수
    protected PlayerController controller;
    // 플레이어의 InputReader를 매번 불러와 InputData에 스크랩
    protected PlayerInputData InputData => controller.InputReader.InputData;
    // 생성자로 PlayerController를 받아서 저장
    protected PlayerState(PlayerController controller)
    {
        this.controller = controller;
    }

    public abstract void Enter();
    public abstract void Exit();
    public virtual void HandleInput() { }
    public abstract void LogicUpdate();
    public abstract void PhysicsUpdate();
}


public class PlayerIdleState : PlayerState
{


    public PlayerIdleState(PlayerController controller) : base(controller)
    {

    }

    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void LogicUpdate()
    {
        if (controller.ChangeDirection(InputData.moveAxis.x))
        {
            Debug.Log("Turn");
            //controller.animator.SetTrigger("IsTurn");
            return;
        }

        if (InputData.moveAxis.x != 0)
        {
            controller.OnMove();
        }
        else if(InputData.jumpPressed && controller.isGround)
        {
            controller.OnJump();
        }
        else if(InputData.dashPressed && controller.isDash)
        {
            controller.OnDash();
        }
        else if(InputData.aimingPressed)
        {
            controller.OnAttack();
        }

        Debug.Log("Idle");
    }

    public override void PhysicsUpdate()
    {
        
    }
}

public class PlayerMoveState : PlayerState
{
    private float moveSpeed;
    public PlayerMoveState(PlayerController controller) : base(controller)
    {
        moveSpeed = controller.moveSpeed;
    }

    public override void Enter()
    {
        controller.animator.SetBool("IsMove", true);
    }

    public override void Exit()
    {
        controller.animator.SetBool("IsMove", false);
    }

    public override void LogicUpdate()
    {
        // 멈추었는지 체크
        if(Mathf.Abs(InputData.moveAxis.x) == 0 && Mathf.Abs(controller.Rigidbody2D.velocity.x) < 0.01f)
        {
            Debug.Log("오류 발생");
            controller.OnIdle();
        }

        if (InputData.dashPressed && controller.isDash)
        {
            controller.OnDash();
        }

        // 점프가 가능한 상태인지 체크
        if (InputData.jumpPressed && controller.isGround)
        {
            controller.OnJump();
        }

        Debug.Log("Move");
    }

    public override void PhysicsUpdate()
    {
        float moveDirect = InputData.moveAxis.x;
        Vector2 velocity;

        if (moveDirect == 0)
        {
            velocity.x = Mathf.MoveTowards(controller.Rigidbody2D.velocity.x, 0f, 2000.0f * Time.deltaTime);
            return;
        }
        else
        {
            velocity = controller.Rigidbody2D.velocity;
            velocity.x = moveDirect * moveSpeed;

            if (controller.ChangeDirection(moveDirect))
            {
                Debug.Log("Turn");
                //controller.animator.SetTrigger("IsTurn");
            }
        }
        
        controller.Rigidbody2D.velocity = velocity;
    }
}

public class PlayerJumpState : PlayerState
{
    private float moveSpeed;
    private float jumpPower;
    private bool isFalling;
    private bool isLanding;
    private bool landingSlow;

    private float checkGroundDistance = 1.0f;

    private Collider2D col;
    private LayerMask groundLayer;

    public PlayerJumpState(PlayerController controller) : base(controller)
    {
        moveSpeed = controller.moveSpeed;
        jumpPower = controller.jumpPower;
        isFalling = false;
        landingSlow = false;

        groundLayer = LayerMask.GetMask("Floor");
        col = controller.GetComponent<Collider2D>();
    }

    public override void Enter()
    {
        isFalling = false;
        isLanding = false;

        Vector2 velocity = controller.Rigidbody2D.velocity;
        velocity.y = 0.0f;
        controller.Rigidbody2D.velocity = velocity;

        controller.Rigidbody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        //controller.isGround = false;

        Debug.Log("점프 활성화 " + jumpPower);

        controller.isGround = false;

        if(controller.animator.GetBool("IsJump"))
        {
            controller.animator.CrossFadeInFixedTime("JumpStart", 0.5f);
            return;
        }

        controller.animator.SetBool("IsJump", true);
    }

    public override void Exit()
    {
        landingSlow = false;
        controller.animator.SetBool("IsJump", false);
        controller.moveSpeed = moveSpeed;
    }

    public override void LogicUpdate()
    {
        if (InputData.dashPressed && controller.isDash)
        {
            controller.OnDash();
        }

        // 땅에 닿았을 때 상태 변환
        if (controller.isGround && controller.Rigidbody2D.velocity.y <= 0.01f)
        {
            if (Mathf.Abs(InputData.moveAxis.x) > 0.01f)
                controller.OnMove();
            else

            //if (isLanding)
            //    return;
            //else
                controller.OnIdle();
        }

        Debug.Log("OnJump");
    }

    public override void PhysicsUpdate()
    {
        if(isFalling)
        {
            Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, checkGroundDistance, groundLayer);

            if(hit.collider != null && !isLanding)
            {
                isLanding = true;
                controller.animator.SetTrigger("DetectFloor");
            }
        }
        else
        {
            if(controller.Rigidbody2D.velocity.y < 0)
            { 
                isFalling = true;
                controller.animator.Play("JumpDown");
            }
        }

        float moveDirect = InputData.moveAxis.x;

        controller.ChangeDirection(moveDirect);

        Vector2 velocity = controller.Rigidbody2D.velocity;
        velocity.x = moveDirect * controller.moveSpeed;
        controller.Rigidbody2D.velocity = velocity;

        if(isLanding)
        {
            Debug.Log("Landing");
            //if(!landingSlow)
            //{
            //    Debug.Log("LandingSlow");
            //    landingSlow = true;
            //    controller.StartCoroutine(controller.SlowDownSpeed(moveSpeed * 0.0f, 5.0f));
            //}

            AnimatorStateInfo info = controller.animator.GetCurrentAnimatorStateInfo(0);
            if(info.IsName("JumpEnd") && info.normalizedTime >= 1f)
            {
                ////if (Mathf.Abs(InputData.moveAxis.x) > 0.01f)
                //    controller.OnMove();
                
                    controller.OnIdle();
            }
        }
    }
}

public class PlayerTurnState : PlayerState
{
    public PlayerTurnState(PlayerController controller) : base(controller)
    {

    }

    public override void Enter()
    {
        throw new System.NotImplementedException();
    }

    public override void Exit()
    {
        throw new System.NotImplementedException();
    }

    public override void LogicUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void PhysicsUpdate()
    {
        throw new System.NotImplementedException();
    }
}

public class PlayerDashState : PlayerState
{
    private float dashPower;
    public PlayerDashState(PlayerController controller) : base(controller)
    {
        dashPower = controller.dashPower;
        controller.isDash = true;
    }

    public override void Enter()
    {
        float dir = controller.transform.localScale.x;

        controller.Rigidbody2D.velocity = new Vector2(0, 0);

        controller.Rigidbody2D.velocity = new Vector2(dir * dashPower, 0);
        controller.isDash = false;
    }

    public override void Exit()
    {
    }

    public override void LogicUpdate()
    {
        if (Mathf.Abs(controller.Rigidbody2D.velocity.x) <= 0.01f)
        {
            if(InputData.moveAxis.x != 0)
                controller.OnMove();
            else
                controller.OnIdle();
        }
    }

    public override void PhysicsUpdate()
    {
        
    }
}

public class PlayerAttackState : PlayerState
{
    private float minAngle;
    private float maxAngle;

    public PlayerAttackState(PlayerController controller) : base(controller)
    {
        minAngle = controller.upperBodyMinAngle * -1;
        maxAngle = controller.upperBodyMaxAngle;
    }

    public override void Enter()
    {
        controller.upperAnimator.SetBool("IsAttack", true);
    }

    public override void Exit()
    {
        controller.upperAnimator.SetBool("IsAttack", false);
    }

    public override void LogicUpdate()
    {
        Debug.Log("Attack");

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = new Vector2(mousePosition.x - controller.transform.position.x, mousePosition.y - controller.transform.position.y).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (angle >= 0)
        {
            if (angle >= 90)
            {
                if (angle < 180 - maxAngle)
                    direction = new Vector2(Mathf.Cos((180 - maxAngle) * Mathf.Deg2Rad), Mathf.Sin((180 - maxAngle) * Mathf.Deg2Rad));
            }
            else
            {
                if (maxAngle < angle)
                    direction = new Vector2(Mathf.Cos(maxAngle * Mathf.Deg2Rad), Mathf.Sin(maxAngle * Mathf.Deg2Rad));
            }
        }
        else
        {
            if (angle > -90)
            {
                if (angle < minAngle)
                    direction = new Vector2(Mathf.Cos(minAngle * Mathf.Deg2Rad), Mathf.Sin(minAngle * Mathf.Deg2Rad));
            }
            else
            {
                if (angle > -180 - minAngle)
                    direction = new Vector2(Mathf.Cos((-180 - minAngle) * Mathf.Deg2Rad), Mathf.Sin((-180 - minAngle) * Mathf.Deg2Rad));
            }
        }

        float armAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        controller.upperBody.transform.localRotation = Quaternion.Euler(0, 0, armAngle);


        if (InputData.attackPressed && controller.attackTimer <= 0)
        {
            controller.ShootArrow(direction);
        }

        if(!InputData.aimingPressed)
        {
            controller.OnIdle();
        }

        // TODO:: AttackState가 해제되는 조건 추가
    }

    public override void PhysicsUpdate()
    {
        
    }
}