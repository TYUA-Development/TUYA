using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.IO.LowLevel.Unsafe;

public class PlayerController : MonoBehaviour
{
    // 플레이어의 입력을 저장
    public PlayerInputReader InputReader { get; private set; }
    public Rigidbody2D Rigidbody2D;
    public SpriteRenderer charactorSprite;

    // 현재 플레이어가 땅에 닿아있는지
    public bool isGround;
    public bool isDash;

    // 풀밭 위에 있는지
    public bool isOnGrass;

    // 플레이어의 기본적인 수치를 Inspector에서 설정하기 위해
    public float setSpeed;
    [HideInInspector] public float moveSpeed;
    public float jumpPower;
    public float dashPower;
    [SerializeField] private float attackCoolTime;
    public float attackTimer;

    // 발소리 관련
    public AudioSource footstepSource;
    public AudioClip[] grassFootsteps;
    public float footstepInterval = 0.35f;
    private float footstepTimer;

    // 활 사운드 관련
    public BowSFXRandomizer bowSFX;

    private bool lockPlayerInput;

    // 플레이어의 상태들
    public PlayerState currentState;
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerJumpState jumpState;
    public PlayerDashState dashState;
    public PlayerFallState fallState;
    public PlayerAttackState attackState;

    // 플레이어 상태들을 저장한 상태리스트
    public List<PlayerState> states = new List<PlayerState>();

    public Animator animator;
    public Animator upperAnimator;
    public GameObject upperBody;

    [Tooltip("0~90, 정면에서 위로")]
    public float upperBodyMaxAngle;
    [Tooltip("0~90, 정면에서 아래로")]
    public float upperBodyMinAngle;

    public float aimingTime;

    // 플레이어 공격 화살 prefab
    public GameObject arrowObject;

    private void Awake()
    {
        InputReader = GetComponent<PlayerInputReader>();

        if (Rigidbody2D == null)
            Rigidbody2D = GetComponent<Rigidbody2D>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        if (bowSFX == null)
            bowSFX = GetComponent<BowSFXRandomizer>();

        moveSpeed = setSpeed;

        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        jumpState = new PlayerJumpState(this);
        dashState = new PlayerDashState(this);
        fallState = new PlayerFallState(this);
        attackState = new PlayerAttackState(this);

        states.Add(idleState);
        states.Add(moveState);
        states.Add(jumpState);
        states.Add(dashState);
        states.Add(attackState);

        currentState = idleState;
        lockPlayerInput = false;
    }

    void Update()
    {
        if (currentState.CanInput && !lockPlayerInput)
        {
            InputReader.ReadInput();
        }

        currentState.LogicUpdate();
        CoolDown();
        PlayFootstep();
    }

    private void FixedUpdate()
    {
        currentState.PhysicsUpdate();
    }

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

    public void OnFall()
    {
        ChangeState(fallState);
    }

    public void OnAttack()
    {
        if (bowSFX != null)
            bowSFX.PlayPull();

        ChangeState(attackState);
    }

    private void ChangeState(PlayerState state)
    {
        Debug.Log(state.ToString());
        currentState.Exit();
        currentState = state;
        currentState.Enter();
    }

    public bool ChangeDirection(float dir)
    {
        if (dir == 0)
            return false;

        if (dir == transform.localScale.x)
        {
            transform.localScale = new Vector3(dir * -1, transform.localScale.y, transform.localScale.z);
            return true;
        }

        return false;
    }

    public void ShootArrow(Vector2 direction)
    {
        if (bowSFX != null)
            bowSFX.PlayShoot();

        Vector3 handLength = new Vector3(direction.x * 0.3f, direction.y * 0.3f, 0);

        Instantiate(arrowObject, transform.position + handLength, Quaternion.identity)
            .GetComponent<Arrow>()
            .Launch(direction, transform);

        attackTimer = attackCoolTime;
    }

    private void CoolDown()
    {
        if (attackTimer >= 0)
            attackTimer -= Time.deltaTime;
    }

    private void PlayFootstep()
    {
        if (Rigidbody2D == null)
            Rigidbody2D = GetComponent<Rigidbody2D>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        if (Rigidbody2D == null)
            return;

        if (footstepSource == null)
            return;

        if (grassFootsteps == null || grassFootsteps.Length == 0)
            return;

        bool isMoving = Mathf.Abs(Rigidbody2D.velocity.x) > 0.05f;

        if (isGround && isOnGrass && isMoving)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                int randomIndex = UnityEngine.Random.Range(0, grassFootsteps.Length);
                AudioClip clipToPlay = grassFootsteps[randomIndex];

                if (clipToPlay != null)
                {
                    footstepSource.PlayOneShot(clipToPlay);
                }

                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = footstepInterval;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Floor") || collision.collider.CompareTag("Runway"))
        {
            isGround = true;
        }

        if (collision.collider.CompareTag("Floor"))
        {
            isOnGrass = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Floor") || collision.collider.CompareTag("Runway"))
        {
            isGround = false;
        }

        if (collision.collider.CompareTag("Floor"))
        {
            isOnGrass = false;
        }
    }

    public IEnumerator SlowDownSpeed(float speed, float time, int divide = 0)
    {
        moveSpeed = speed;

        if (divide == 0)
        {
            yield return new WaitForSeconds(time);

            moveSpeed = setSpeed;
        }
        else
        {
            float addSpeed = (setSpeed - speed) / divide;

            for (int i = 0; i < divide; i++)
            {
                yield return new WaitForSeconds(time / divide);
                moveSpeed += addSpeed;
            }
        }

        moveSpeed = setSpeed;
    }

    public void LockPlayerInput(float time)
    {
        StartCoroutine(LockPlayerInputHelper(time));
    }

    public IEnumerator LockPlayerInputHelper(float time)
    {
        lockPlayerInput = true;
        yield return new WaitForSeconds(time);
        lockPlayerInput = false;
    }
}