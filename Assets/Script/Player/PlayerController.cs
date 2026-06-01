using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

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

    [Header("Arrow Visual")]
    public GameObject heldArrowVisual;
    public Transform bowPoint;
    public Transform firePoint;

    [Header("Arrow FX Templates")]
    public GameObject arrowGatherFXTemplate;
    public GameObject arrowReleaseFXTemplate;

    [Header("Held Arrow Fade")]
    public float heldArrowFadeTime = 1.0f;

    private SpriteRenderer[] heldArrowRenderers;
    private Coroutine heldArrowFadeCoroutine;
    private bool heldArrowVisible;

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

        CacheHeldArrowRenderers();
        HideHeldArrow();

        // 템플릿 오브젝트는 게임 시작 시 안 보이게 꺼둠
        if (arrowGatherFXTemplate != null)
            arrowGatherFXTemplate.SetActive(false);

        if (arrowReleaseFXTemplate != null)
            arrowReleaseFXTemplate.SetActive(false);
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
        HideHeldArrow();
        ChangeState(idleState);
    }

    public void OnMove()
    {
        HideHeldArrow();
        ChangeState(moveState);
    }

    public void OnJump()
    {
        HideHeldArrow();
        ChangeState(jumpState);
    }

    public void OnFall()
    {
        HideHeldArrow();
        ChangeState(fallState);
    }

    public void OnAttack()
    {
        HideHeldArrow();

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
        HideHeldArrow();

        SpawnFXFromTemplate(
            arrowReleaseFXTemplate,
            GetFirePointPosition(),
            GetFirePointRotation(),
            1.2f
        );

        if (bowSFX != null)
            bowSFX.PlayShoot();

        Vector3 spawnPosition = GetFirePointPosition();

        Instantiate(arrowObject, spawnPosition, Quaternion.identity)
            .GetComponent<Arrow>()
            .Launch(direction, transform);

        attackTimer = attackCoolTime;
    }

    public void ShowHeldArrow()
    {
        if (heldArrowVisual == null)
            return;

        if (heldArrowVisible)
            return;

        heldArrowVisible = true;

        heldArrowVisual.SetActive(true);
        CacheHeldArrowRenderers();
        SetHeldArrowAlpha(0f);

        SpawnFXFromTemplate(
            arrowGatherFXTemplate,
            GetBowPointPosition(),
            GetBowPointRotation(),
            1.8f
        );

        if (heldArrowFadeCoroutine != null)
            StopCoroutine(heldArrowFadeCoroutine);

        heldArrowFadeCoroutine = StartCoroutine(FadeHeldArrow(0f, 1f, heldArrowFadeTime));
    }

    public void HideHeldArrow()
    {
        heldArrowVisible = false;

        if (heldArrowFadeCoroutine != null)
        {
            StopCoroutine(heldArrowFadeCoroutine);
            heldArrowFadeCoroutine = null;
        }

        SetHeldArrowAlpha(0f);

        if (heldArrowVisual != null)
            heldArrowVisual.SetActive(false);
    }

    private Vector3 GetBowPointPosition()
    {
        if (bowPoint != null)
            return bowPoint.position;

        if (firePoint != null)
            return firePoint.position;

        return transform.position;
    }

    private Quaternion GetBowPointRotation()
    {
        if (bowPoint != null)
            return bowPoint.rotation;

        if (firePoint != null)
            return firePoint.rotation;

        return transform.rotation;
    }

    private Vector3 GetFirePointPosition()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position;
    }

    private Quaternion GetFirePointRotation()
    {
        if (firePoint != null)
            return firePoint.rotation;

        return transform.rotation;
    }

    private void SpawnFXFromTemplate(GameObject template, Vector3 position, Quaternion rotation, float destroyTime)
    {
        if (template == null)
            return;

        GameObject fx = Instantiate(template, position, rotation);
        fx.SetActive(true);

        ParticleSystem[] particles = fx.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play();
        }

        Destroy(fx, destroyTime);
    }

    private IEnumerator FadeHeldArrow(float fromAlpha, float toAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetHeldArrowAlpha(toAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            SetHeldArrowAlpha(alpha);
            yield return null;
        }

        SetHeldArrowAlpha(toAlpha);
        heldArrowFadeCoroutine = null;
    }

    private void CacheHeldArrowRenderers()
    {
        if (heldArrowVisual == null)
        {
            heldArrowRenderers = null;
            return;
        }

        heldArrowRenderers = heldArrowVisual.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void SetHeldArrowAlpha(float alpha)
    {
        if (heldArrowRenderers == null || heldArrowRenderers.Length == 0)
            return;

        for (int i = 0; i < heldArrowRenderers.Length; i++)
        {
            if (heldArrowRenderers[i] == null)
                continue;

            Color c = heldArrowRenderers[i].color;
            c.a = alpha;
            heldArrowRenderers[i].color = c;
        }
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