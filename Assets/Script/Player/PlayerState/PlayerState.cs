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

    // 같은 위치에 솔리드 바닥과 트리거(예: RunwayObject 감지용 트리거)가 겹쳐 있을 때
    // OverlapBox 단일 결과만으로는 어느 쪽이 반환될지 보장되지 않으므로,
    // OverlapBoxAll로 받은 후보들 중 트리거가 아닌 것을 우선으로 찾는다.
    protected static Collider2D FindSolidGround(Collider2D[] hits)
    {
        foreach (Collider2D hit in hits)
        {
            if (!hit.isTrigger)
                return hit;
        }

        return null;
    }
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
        else if (InputData.jumpPressed && controller.isGround)
        {
            controller.OnJump();
        }
        //else if(InputData.dashPressed && controller.isDash)
        //{
        //    controller.OnDash();
        //}
        else if (InputData.aimingPressed)
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
    private bool wasGrounded;

    private float groundLostTimer;
    private const float FallGracePeriod = 0.2f;

    public PlayerMoveState(PlayerController controller) : base(controller)
    {
        moveSpeed = controller.moveSpeed;
        col = controller.GetComponent<CapsuleCollider2D>();
        groundLayer = LayerMask.GetMask("Floor") | LayerMask.GetMask("Default") | LayerMask.GetMask("Runway");
        sensorDistance = 1.0f;
        sensorX = 3.0f;
    }

    public override void Enter()
    {
        controller.animator.SetBool("IsMove", true);
        groundLostTimer = 0f;
    }

    public override void Exit()
    {
        controller.animator.SetBool("IsMove", false);
    }

    public override void LogicUpdate()
    {
        if (wasGrounded && !controller.isGround)
            controller.StartCoyoteTime();

        wasGrounded = controller.isGround;

        if (InputData.aimingPressed)
        {
            controller.OnAttack();
            return;
        }

        if (controller.isGround)
        {
            groundLostTimer = 0f;
        }
        else if (CheckFall())
        {
            groundLostTimer += Time.deltaTime;
        }

        if (groundLostTimer >= FallGracePeriod)
        {
            controller.OnFall(grantCoyote: true);
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
        if (InputData.jumpPressed && (controller.isGround || controller.CanCoyoteJump))
        {
            controller.ConsumeCoyoteTime();
            controller.OnJump();
        }

        Debug.Log("Move");
    }

    public override void PhysicsUpdate()
    {
        CheckRunWayFromFront();

        float moveDirect = InputData.moveAxis.x;

        if (moveDirect == 0)
        {
            Vector2 stopVelocity = controller.Rigidbody2D.velocity;
            stopVelocity.x = 0f;
            controller.Rigidbody2D.velocity = stopVelocity;
            return;
        }

        if (controller.isGround)
        {
            Vector2 normal = controller.groundNormal;
            Vector2 tangent = new Vector2(normal.y, -normal.x);

            if (tangent.x < 0f)
                tangent = -tangent;

            float horizontalSpeed = moveDirect * moveSpeed;

            if (Mathf.Abs(tangent.x) > 0.0001f)
            {
                float scale = horizontalSpeed / tangent.x;
                controller.Rigidbody2D.velocity = tangent * scale;
            }
            else
            {
                controller.Rigidbody2D.velocity = new Vector2(horizontalSpeed, controller.Rigidbody2D.velocity.y);
            }
        }
        else
        {
            Vector2 velocity = controller.Rigidbody2D.velocity;
            velocity.x = moveDirect * moveSpeed;
            controller.Rigidbody2D.velocity = velocity;
        }

        if (controller.ChangeDirection(moveDirect))
        {
            Debug.Log("Turn");
            //controller.animator.SetTrigger("IsTurn");
        }
    }

    //TODO:: Runway가 바닥이면 velocity.x를 0으로. 기본 state로.

    private bool CheckFall()
    {
        Vector2 checkPos = new Vector2(
            col.bounds.center.x,
            col.bounds.min.y - 0.05f
        );

        Vector2 checkSize = new Vector2(
            col.bounds.size.x * 0.9f,
            0.5f
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkPos, checkSize, 0f, groundLayer);

        return FindSolidGround(hits) == null;
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

    private CapsuleCollider2D col;
    private LayerMask groundLayer;

    private float groundCheckGraceTimer;
    private const float GroundCheckGracePeriod = 0.08f;

    public PlayerJumpState(PlayerController controller) : base(controller)
    {
        moveSpeed = controller.moveSpeed;
        jumpPower = controller.jumpPower;

        col = controller.GetComponent<CapsuleCollider2D>();
        groundLayer = LayerMask.GetMask("Floor") | LayerMask.GetMask("Default") | LayerMask.GetMask("Runway");
    }

    public override void Enter()
    {
        Vector2 velocity = controller.Rigidbody2D.velocity;
        velocity.y = 0.0f;
        controller.Rigidbody2D.velocity = velocity;

        controller.Rigidbody2D.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        //controller.isGround = false;

        controller.PlayJumpSound();

        Debug.Log("점프 활성화 " + jumpPower);

        controller.isGround = false;

        groundCheckGraceTimer = GroundCheckGracePeriod;

        //if(controller.animator.GetBool("IsJump"))
        //{
        //    controller.animator.CrossFadeInFixedTime("JumpStart", 0.5f);
        //    return;
        //}

        // 점프

        controller.animator.ResetTrigger("DetectFloor");

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
        if (groundCheckGraceTimer > 0f)
            groundCheckGraceTimer -= Time.fixedDeltaTime;

        if (controller.isGround || controller.Rigidbody2D.velocity.y <= 0.01f ||
            (groundCheckGraceTimer <= 0f && CheckGrounded()))
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

    // 경사로에 몸이 파고들며 물리 솔버가 velocity.y를 계속 밀어올려서
    // isGround/velocity.y 조건만으로는 Fall 전환이 안 되는 경우를 잡기 위한 보조 지면 감지.
    // PlayerMoveState.CheckFall() / PlayerFallState.CheckLanding()과 동일한 방식.
    private bool CheckGrounded()
    {
        Vector2 checkPos = new Vector2(
            col.bounds.center.x,
            col.bounds.min.y - 0.05f
        );

        Vector2 checkSize = new Vector2(
            col.bounds.size.x * 0.9f,
            0.5f
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkPos, checkSize, 0f, groundLayer);

        return FindSolidGround(hits) != null;
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
            if (InputData.moveAxis.x != 0)
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

    // 이번 조준에서 실제로 화살을 쐈는지. AttackEnd에서 조준 버튼을 계속/다시 누르고 있을 때만
    // Idle을 거치지 않고 바로 재조준으로 이어가기 위해 필요하다 - 쏘지 않고 조준만 취소한
    // 경우(aimingPressed가 false가 되어 StartAttackEnd가 불린 경우)까지 재조준으로 이어지면
    // 안 되기 때문에 "발사했는가"를 별도로 구분한다.
    private bool firedThisAim;

    private float minAngle;
    private float maxAngle;

    private float arrowSpeed;
    private float arrowFlyTime;
    private float arrowGravityValue;

    public PlayerAttackState(PlayerController controller) : base(controller)
    {
        minAngle = controller.upperBodyMinAngle * -1;
        maxAngle = controller.upperBodyMaxAngle;

        isAiming = false;
        isFinishingAttack = false;
        attackQueued = false;

        if (controller.arrowObject != null)
        {
            Arrow arrow = controller.arrowObject.GetComponent<Arrow>();
            Rigidbody2D arrowRb = controller.arrowObject.GetComponent<Rigidbody2D>();
            if (arrow != null)
            {
                arrowSpeed = arrow.speed;
                arrowFlyTime = arrow.flyTime;
                arrowGravityValue = arrowRb != null ? arrowRb.gravityScale : arrow.gravityValue;
            }
        }
    }

    public override void Enter()
    {

        isAiming = true;
        isFinishingAttack = false;
        attackQueued = false;
        firedThisAim = false;

        controller.Rigidbody2D.velocity = new Vector2(0f, controller.Rigidbody2D.velocity.y);

        controller.HideUpperBody();
        controller.animator.SetBool("IsAiming", true);
        controller.animator.SetBool("IsAttack", false);
    }

    public override void Exit()
    {
        controller.HideTrajectory();
        controller.LockPlayerInput(0.25f);
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

        if (isAiming)
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


            controller.UpdateTrajectory(direction, arrowSpeed, arrowFlyTime, arrowGravityValue);

            if (InputData.attackPressed && controller.attackTimer <= 0)
            {
                controller.ShootArrow(direction);
                firedThisAim = true;
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
        controller.HideTrajectory();
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

        if (info.IsName("Aiming"))
        {
            // AttackEnd -> Aiming으로 바로 이어져 재조준이 시작된 경우. ChangeState 없이(Idle을
            // 거치지 않고) 같은 attackState 안에서 Enter()와 동일하게 내부 상태만 리셋한다.
            // 기존 흐름(Idle -> Aiming)과 달리, 여기서는 조준 버튼을 계속 누르고 있어 UpperBody가
            // 발사 시점부터 계속 꺼져 있었으므로 Aiming 애니메이션이 다시 시작되는 이 시점에
            // 바로 켜줘야 어색하게 안 보인다 (원래는 Attack.anim의 애니메이션 이벤트가 Aiming이
            // 다 끝난 뒤에야 켠다).
            controller.ShowUpperBody();
            RestartAiming();
            return;
        }

        if (info.IsName("AttackEnd"))
        {
            if (info.normalizedTime >= 0.1f)
            {
                // 화살을 쐈고, 조준 버튼을 계속(또는 다시) 누르고 있으면 Idle로 나가지 않고
                // IsAiming을 다시 켜서 Animator의 AttackEnd -> Aiming 전환이 이어받게 한다.
                if (firedThisAim && InputData.aimingPressed)
                {
                    // AttackEnd.anim의 0% 지점 애니메이션 이벤트(HideUpperBody)에만 의존하지
                    // 않고 코드에서 직접 확실히 꺼준다 - OnIdle() 경로는 OnIdle() 내부에서
                    // HideUpperBody()를 코드로 보장하는데, 이 경로(조준 버튼을 계속 누르고
                    // 있어 Idle로 안 나가는 경우)만 애니메이션 이벤트 하나에 전적으로
                    // 의존하고 있어서, IsAiming=true로 전환되는 타이밍에 따라 이벤트가
                    // 샘플링되지 않고 넘어가면 UpperBody가 계속 켜진 채로 남는 문제가 있었다.
                    controller.HideUpperBody();
                    controller.animator.SetBool("IsAiming", true);
                    return;
                }

                controller.OnIdle();
            }

            return;
        }

        if (info.IsName("Idle"))
        {
            controller.OnIdle();
            return;
        }

        if (info.IsName("Attack") && info.normalizedTime >= 0.1f)
        {
            controller.HideUpperBody();
            controller.animator.Play("AttackEnd", 0, 0f);
        }
    }

    private void RestartAiming()
    {
        isAiming = true;
        isFinishingAttack = false;
        attackQueued = false;
        firedThisAim = false;

        controller.Rigidbody2D.velocity = new Vector2(0f, controller.Rigidbody2D.velocity.y);
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
        controller.animator.ResetTrigger("DetectFloor");

        controller.animator.SetBool("IsJump", false);
        controller.animator.SetBool("IsMove", false);
        controller.animator.SetBool("IsFall", true);

        controller.animator.Play("JumpDown", 0, 0f);

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
        if (controller.CanCoyoteJump && InputData.jumpPressed)
        {
            controller.ConsumeCoyoteTime();
            controller.OnJump();
            return;
        }

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
            controller.animator.SetBool("IsMove", moveDirect != 0);

            AnimatorStateInfo info = controller.animator.GetCurrentAnimatorStateInfo(0);

            if ((info.IsName("JumpEnd") && info.normalizedTime >= 1f) || info.IsName("Idle") || info.IsName("Move"))
            {
                if (Mathf.Abs(InputData.moveAxis.x) > 0.01f)
                    controller.OnMove();
                else
                    controller.OnIdle();
            }
        }
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
            0.5f
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            checkPos,
            checkSize,
            0f,
            groundLayer
        );

        Collider2D hit = FindSolidGround(hits);

        if (hit == null && !controller.isGround)
            return;

        if (hit != null && hit.CompareTag("Runway"))
        {

            RunwayObject runway = hit.GetComponent<RunwayObject>();
            if (runway != null)
                runway.OnRunWayCollider();
        }

        isLanding = true;
        controller.animator.SetBool("IsFall", false);
        controller.animator.SetTrigger("DetectFloor");

        Debug.Log("Landing Detect");
    }
}