using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.RuleTile.TilingRuleOutput;

// 추상 클래스로 생성
public abstract class PlayerState
{
    // 플레이어의 playerController를 저장하는 변수
    protected PlayerController controller;
    // 플레이어의 InputReader를 매번 불러와 InputData에 스크랩
    protected PlayerInputData InputData => controller.InputReader.InputData;
    // 생성자로 PlayerController를 받아서 저장

    public virtual bool CanInput => true;

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
        controller.animator.SetTrigger("DetectFloor");
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
        //else if(InputData.dashPressed && controller.isDash)
        //{
        //    controller.OnDash();
        //}
        else if(InputData.aimingPressed)
        {
            controller.OnAttack();
        }

        Debug.Log("Idle");
    }

    public override void PhysicsUpdate()
    {
        if (!controller.isGround) return;

        Vector2 v = controller.Rigidbody2D.velocity;
        v.x = 0f;
        controller.Rigidbody2D.velocity = v;
    }
}

public class PlayerMoveState : PlayerState
{
    private float moveSpeed;
    private CapsuleCollider2D col;
    private LayerMask groundLayer;

    private float sensorX;
    private float sensorDistance;
    public PlayerMoveState(PlayerController controller) : base(controller)
    {
        moveSpeed = controller.moveSpeed;
        col = controller.GetComponent<CapsuleCollider2D>();
        groundLayer = LayerMask.GetMask("Floor") | LayerMask.GetMask("Default");
        sensorDistance = 1.0f;
        sensorX = 3.0f;
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
        if (InputData.aimingPressed)
        {
            controller.OnAttack();
            return;
        }

        if (CheckFall() && !controller.isGround)
        {
            controller.OnFall();
            return;
        }

        // 멈추었는지 체크
        if (Mathf.Abs(InputData.moveAxis.x) == 0 && controller.isGround)
        {
            controller.OnIdle();
        }

        //if (InputData.dashPressed && controller.isDash)
        //{
        //    controller.OnDash();
        //}

        // 점프가 가능한 상태인지 체크
        if (InputData.jumpPressed && controller.isGround)
        {
            controller.OnJump();
        }

        Debug.Log("Move");
    }

    public override void PhysicsUpdate()
    {
        CheckRunWayFromFront();

        float moveDirect = InputData.moveAxis.x;

        Vector2 velocity = controller.Rigidbody2D.velocity;

        if (moveDirect == 0)
        {
            //velocity.x = Mathf.MoveTowards(controller.Rigidbody2D.velocity.x, 0f, 2000.0f * Time.deltaTime);
            velocity.x = 0f;
            controller.Rigidbody2D.velocity = velocity;
            return;
        }
        
        velocity.x = moveDirect * moveSpeed;
        controller.Rigidbody2D.velocity = velocity;

        if (controller.ChangeDirection(moveDirect))
        {
            Debug.Log("Turn");
            //controller.animator.SetTrigger("IsTurn");
        }
    }

    //TODO:: Runway가 바닥이면 velocity.x를 0으로. 기본 state로.

    private bool CheckFall()
    {
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);

        RaycastHit2D hit = Physics2D.Raycast( origin, Vector2.down, 0.2f, groundLayer);

        return hit.collider == null;
    }

    private bool CheckRunWayFromFront()
    {
        Bounds bounds = col.bounds;

        float footY = bounds.min.y;
        float footX = bounds.min.x + (controller.transform.localScale.x > 0 ? -sensorX : sensorX);

        Vector2 rayOrigin = new Vector2(footX, footY);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, sensorDistance, groundLayer);

        Debug.DrawRay(rayOrigin, Vector2.down * sensorDistance, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Runway"))
        {
            RunwayObject runway = hit.collider.GetComponent<RunwayObject>();
            if (runway == null)
            {
                Debug.LogError("Runway 태그는 있지만 RunwayObject 컴포넌트가 없습니다: " + hit.collider.name);
                return false;
            }

            runway.OnRunWayCollider();
            return true;
        }

        return false;
    }
}

public class PlayerJumpState : PlayerState
{
    private float moveSpeed;
    private float jumpPower;


    public PlayerJumpState(PlayerController controller) : base(controller)
    {
        moveSpeed = controller.moveSpeed;
        jumpPower = controller.jumpPower;
        
    }

    public override void Enter()
    {
        Vector2 velocity = controller.Rigidbody2D.velocity;
        velocity.y = 0.0f;
        controller.Rigidbody2D.velocity = velocity;

        controller.Rigidbody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        //controller.isGround = false;

        Debug.Log("점프 활성화 " + jumpPower);

        controller.isGround = false;

        //if(controller.animator.GetBool("IsJump"))
        //{
        //    controller.animator.CrossFadeInFixedTime("JumpStart", 0.5f);
        //    return;
        //}

        // 점프 

        controller.animator.SetBool("IsJump", true);
    }

    public override void Exit()
    {
        controller.animator.SetBool("IsJump", false);
        controller.moveSpeed = moveSpeed;
    }

    public override void LogicUpdate()
    {
        // 땅에 닿았을 때 상태 변환
        

        Debug.Log("OnJump");
    }

    public override void PhysicsUpdate()
    {
        if (controller.Rigidbody2D.velocity.y <= 0.01f)
        {
            controller.OnFall();
            return;
        }

        float moveDirect = InputData.moveAxis.x;

        controller.ChangeDirection(moveDirect);

        Vector2 velocity = controller.Rigidbody2D.velocity;
        velocity.x = moveDirect * controller.moveSpeed;
        controller.Rigidbody2D.velocity = velocity;
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
    private bool isAiming;
    private bool isFinishingAttack;
    private bool attackQueued;

    private float minAngle;
    private float maxAngle;

    public PlayerAttackState(PlayerController controller) : base(controller)
    {
        minAngle = controller.upperBodyMinAngle * -1;
        maxAngle = controller.upperBodyMaxAngle;

        isAiming = false;
        isFinishingAttack = false;
        attackQueued = false;
    }

    public override void Enter()
    {

        isAiming = true;
        isFinishingAttack = false;
        attackQueued = false;

        controller.Rigidbody2D.velocity = new Vector2(0f, controller.Rigidbody2D.velocity.y);

        controller.HideUpperBody();
        controller.animator.SetBool("IsAiming", true);
        controller.animator.SetBool("IsAttack", false);
    }

    public override void Exit()
    {
        controller.LockPlayerInput(1.0f);
        controller.RequireAimingReleaseBeforeAttack();

        controller.animator.SetBool("IsAiming", false);
        controller.animator.SetBool("IsAttack", false);
    }

    public override void LogicUpdate()
    {
        if (isFinishingAttack)
        {
            UpdateFinishingAttack();
            return;
        }

        if(isAiming)
        {
            Debug.Log("Aiming");

            if (!attackQueued)
            {
                controller.animator.SetBool("IsAttack", true);
                attackQueued = true;
            }

            AnimatorStateInfo info = controller.animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsName("Attack"))
            {
                isAiming = false;
                controller.animator.SetBool("IsAiming", false);
            }

            return;
        }

        Debug.Log("Attack");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.forward, controller.transform.position);

        float angle;

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(distance);

            Vector2 direction = new Vector2(mouseWorldPosition.x - controller.transform.position.x, mouseWorldPosition.y - controller.transform.position.y).normalized;

            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (angle >= 0)
            {
                if (angle >= 90)
                {
                    controller.transform.localScale = new Vector3(Math.Abs(controller.transform.localScale.x), controller.transform.localScale.y, controller.transform.localScale.z);

                    if (180 > angle && angle > 180 - maxAngle)
                        direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    else
                        direction = new Vector2(Mathf.Cos((180 - maxAngle) * Mathf.Deg2Rad), Mathf.Sin((180 - maxAngle) * Mathf.Deg2Rad));
                }
                else
                {
                    controller.transform.localScale = new Vector3(Math.Abs(controller.transform.localScale.x) * -1, controller.transform.localScale.y, controller.transform.localScale.z);

                    if (0 < angle && angle < maxAngle)
                        direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    else
                        direction = new Vector2(Mathf.Cos(maxAngle * Mathf.Deg2Rad), Mathf.Sin(maxAngle * Mathf.Deg2Rad));
                    //if (maxAngle < angle)
                    //    direction = new Vector2(Mathf.Cos(maxAngle * Mathf.Deg2Rad), Mathf.Sin(maxAngle * Mathf.Deg2Rad));
                }
            }
            else
            {
                if (angle < -90)
                {
                    controller.transform.localScale = new Vector3(Math.Abs(controller.transform.localScale.x), controller.transform.localScale.y, controller.transform.localScale.z);

                    if (-180 < angle && angle < -180 - minAngle)
                        direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    else
                        direction = new Vector2(Mathf.Cos((-minAngle - 180
                            ) * Mathf.Deg2Rad), Mathf.Sin((-minAngle - 180
                            ) * Mathf.Deg2Rad));
                }
                else
                {
                    controller.transform.localScale = new Vector3(Math.Abs(controller.transform.localScale.x) * -1, controller.transform.localScale.y, controller.transform.localScale.z);

                    if (0 > angle && angle > minAngle)
                        direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    else
                        direction = new Vector2(Mathf.Cos(minAngle * Mathf.Deg2Rad), Mathf.Sin(minAngle * Mathf.Deg2Rad));
                    //if (angle > -180 - minAngle)
                    //    direction = new Vector2(Mathf.Cos((-180 - minAngle) * Mathf.Deg2Rad), Mathf.Sin((-180 - minAngle) * Mathf.Deg2Rad));
                }
            }

            float armAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (controller.transform.localScale.x > 0)
            {
                if (controller.upperBody != null)
                    controller.upperBody.transform.localRotation = Quaternion.Euler(0, 0, (armAngle - 180f));
            }
            else
            {
                if (controller.upperBody != null)
                    controller.upperBody.transform.localRotation = Quaternion.Euler(0, 0, (armAngle * -1));
            }


            if (InputData.attackPressed && controller.attackTimer <= 0)
            {
                controller.ShootArrow(direction);
                StartAttackEnd();
            }

            if (!InputData.aimingPressed)
            {
                StartAttackEnd();
            }
        }
    }

    private void StartAttackEnd()
    {
        controller.HideHeldArrow();
        controller.animator.SetBool("IsAiming", false);
        controller.animator.SetBool("IsAttack", false);
        isAiming = false;
        isFinishingAttack = true;
    }

    private void UpdateFinishingAttack()
    {
        controller.animator.SetBool("IsAiming", false);
        controller.animator.SetBool("IsAttack", false);

        if (controller.animator.IsInTransition(0))
            return;

        AnimatorStateInfo info = controller.animator.GetCurrentAnimatorStateInfo(0);

        if (info.IsName("AttackEnd"))
        {
            if (info.normalizedTime >= 1f)
                controller.OnIdle();

            return;
        }

        if (info.IsName("Idle"))
        {
            controller.OnIdle();
            return;
        }

        if (info.IsName("Attack") && info.normalizedTime >= 1.05f)
        {
            controller.HideUpperBody();
            controller.animator.Play("AttackEnd", 0, 0f);
        }
    }

    public override void PhysicsUpdate()
    {
        
    }
}

public class PlayerFallState : PlayerState
{
    private LayerMask groundLayer;
    private bool isLanding;
    private bool isFalling;
    private CapsuleCollider2D col;

    public PlayerFallState(PlayerController controller) : base(controller)
    {
        col = controller.GetComponent<CapsuleCollider2D>();
        groundLayer = LayerMask.GetMask("Floor");
        isLanding = false;
        isFalling = true;
    }

    public override void Enter()
    {
        controller.animator.SetBool("IsFall", true);
        controller.animator.Play("JumpDown");

        isLanding = false;
        isFalling = true;

        Debug.Log("Fall State");
    }

    public override void Exit()
    {
        controller.animator.SetBool("IsFall", false);
    }

    public override void LogicUpdate()
    {
        //if (isLanding)
        //    return;

        //if (controller.isGround && controller.Rigidbody2D.velocity.y <= 0.01f)
        //{
        //    if (Mathf.Abs(InputData.moveAxis.x) > 0.01f)
        //        controller.OnMove();
        //    else
        //        controller.OnIdle();
        //}
    }

    public override void PhysicsUpdate()
    {
        CheckLanding();

        float moveDirect = InputData.moveAxis.x;

        controller.ChangeDirection(moveDirect);

        Vector2 velocity = controller.Rigidbody2D.velocity;
        velocity.x = moveDirect * controller.moveSpeed;
        controller.Rigidbody2D.velocity = velocity;

        if (isLanding)
        {
            AnimatorStateInfo info = controller.animator.GetCurrentAnimatorStateInfo(0);

            if ((info.IsName("JumpEnd") && info.normalizedTime >= 1f) || info.IsName("Idle"))
            {
                if (Mathf.Abs(InputData.moveAxis.x) > 0.01f)
                    controller.OnMove();
                else
                    controller.OnIdle();
            }
        }

        //if (isFalling)
        //{
        //    Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);

        //    RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.5f, groundLayer);

        //    if (hit.collider != null && !isLanding)
        //    {
        //        isLanding = true;
        //        controller.animator.SetTrigger("DetectFloor");
        //        Debug.Log("DetectFloor Falling");
        //    }
        //}

        //float moveDirect = InputData.moveAxis.x;

        //controller.ChangeDirection(moveDirect);

        //Vector2 velocity = controller.Rigidbody2D.velocity;
        //velocity.x = moveDirect * controller.moveSpeed;
        //controller.Rigidbody2D.velocity = velocity;

        //if (isLanding)
        //{
        //    Debug.Log("Landing");

        //    AnimatorStateInfo info = controller.animator.GetCurrentAnimatorStateInfo(0);
        //    if (info.IsName("JumpEnd") || info.IsName("Idle"))
        //    {
        //        if (Mathf.Abs(InputData.moveAxis.x) > 0.01f)
        //            controller.OnMove();
        //        else
        //            controller.OnIdle();
        //    }
        //}
    }

    private void CheckLanding()
    {
        if (isLanding)
            return;

        Vector2 checkPos = new Vector2(
            col.bounds.center.x,
            col.bounds.min.y - 0.05f
        );

        Vector2 checkSize = new Vector2(
            col.bounds.size.x * 0.9f,
            0.1f
        );

        Collider2D hit = Physics2D.OverlapBox(
            checkPos,
            checkSize,
            0f,
            groundLayer
        );

        if (hit == null)
            return;

        if (hit.CompareTag("Runway"))
        {
            
            RunwayObject runway = hit.GetComponent<RunwayObject>();
            if (runway != null)
                runway.OnRunWayCollider();
        }

        isLanding = true;
        controller.animator.SetTrigger("DetectFloor");

        Debug.Log("Landing Detect");
    }
}
