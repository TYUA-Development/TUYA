using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerInputReader InputReader { get; private set; }
    public Rigidbody2D Rigidbody2D;
    public SpriteRenderer charactorSprite;

    public bool isGround;
    [HideInInspector] public bool isDash;
    [HideInInspector] public bool isOnRunway;

    // 풀 위인지
    [HideInInspector] public bool isOnGrass;

    // 돌 / 신전바닥 / Runway 위인지
    [HideInInspector] public bool isOnStone;

    public float setSpeed;
    [HideInInspector] public float moveSpeed;
    public float jumpPower;
    public float dashPower;
    [SerializeField] private float attackCoolTime;
    public float attackTimer;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;
    public bool CanCoyoteJump => coyoteTimer > 0f;

    [Header("Footstep SFX")]
    public AudioSource footstepSource;

    [Tooltip("풀밭 발소리")]
    public AudioClip[] grassFootsteps;

    [Tooltip("돌 / 신전바닥 / Runway 발소리")]
    public AudioClip[] stoneFootsteps;

    [Tooltip("풀밭 발소리 볼륨")]
    [Range(0f, 1f)]
    public float grassFootstepVolume = 0.3f;

    [Tooltip("돌 / 신전바닥 / Runway 발소리 볼륨")]
    [Range(0f, 1f)]
    public float stoneFootstepVolume = 0.4f;

    [Tooltip("발소리 간격")]
    public float footstepInterval = 0.35f;

    private float footstepTimer;

    [Header("Jump SFX")]
    public AudioClip[] jumpSounds;
    public float jumpVolume = 0.5f;

    [Header("etc.")]
    public BowSFXRandomizer bowSFX;

    private float defaultGravityScale;
    private bool lockPlayerInput;
    private bool waitForAimingRelease;

    public PlayerState currentState;
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerJumpState jumpState;
    public PlayerDashState dashState;
    public PlayerFallState fallState;
    public PlayerAttackState attackState;

    public List<PlayerState> states = new List<PlayerState>();

    public Animator animator;
    public Animator upperAnimator;
    public GameObject upperBody;

    [Tooltip("0~90, 위쪽 조준 각도")]
    public float upperBodyMaxAngle;

    [Tooltip("0~90, 아래쪽 조준 각도")]
    public float upperBodyMinAngle;

    public float aimingTime;

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

    [Header("Trajectory Preview")]
    public GameObject trajectoryDotPrefab;
    public int trajectoryPointCount = 40;
    public float trajectoryMaxTime = 3f;
    [Range(0f, 1f)] [SerializeField] private float trajectoryFadeStart = 0.6f;

    private List<GameObject> trajectoryDots = new List<GameObject>();
    private List<SpriteRenderer> trajectoryRenderers = new List<SpriteRenderer>();

    private SpriteRenderer[] heldArrowRenderers;
    private Coroutine heldArrowFadeCoroutine;
    private bool heldArrowVisible;

    private void Awake()
    {
        InputReader = GetComponent<PlayerInputReader>();

        if (Rigidbody2D == null)
            Rigidbody2D = GetComponent<Rigidbody2D>();

        defaultGravityScale = Rigidbody2D.gravityScale;

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
        waitForAimingRelease = false;

        CacheHeldArrowRenderers();
        HideHeldArrow();

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
        else
        {
            InputReader.ClearInput();
        }

        UpdateAimingReleaseLock();

        if (coyoteTimer > 0f)
            coyoteTimer -= Time.deltaTime;

        currentState.LogicUpdate();
        CoolDown();
        PlayFootstep();
    }

    private void FixedUpdate()
    {
        bool wasOnRunway = isOnRunway;
        bool wasGround   = isGround;

        isGround   = false;
        isOnRunway = false;
        isOnGrass  = false;
        isOnStone  = false;

        currentState.PhysicsUpdate();
        PreventSlide(wasOnRunway, wasGround);
    }

    private void PreventSlide(bool onRunway, bool onGround)
    {
        bool shouldFreeze = onGround &&
                            InputReader.InputData.moveAxis.x == 0 &&
                            InputReader.InputData.moveAxis.y >= 0 &&
                            currentState != jumpState &&
                            currentState != fallState &&
                            currentState != dashState;

        if (shouldFreeze)
        {
            Rigidbody2D.gravityScale = 0f;
            Rigidbody2D.velocity = Vector2.zero;
        }
        else
        {
            Rigidbody2D.gravityScale = defaultGravityScale;
        }
    }

    public void OnIdle()
    {
        HideHeldArrow();
        HideUpperBody();
        ChangeState(idleState);
    }

    public void OnMove()
    {
        HideHeldArrow();
        HideUpperBody();
        ChangeState(moveState);
    }

    public void OnJump()
    {
        HideHeldArrow();
        HideUpperBody();
        ChangeState(jumpState);
    }

    public void OnFall(bool grantCoyote = false)
    {
        if (grantCoyote)
            coyoteTimer = coyoteTime;

        HideHeldArrow();
        HideUpperBody();
        ChangeState(fallState);
    }

    public void StartCoyoteTime()
    {
        coyoteTimer = coyoteTime;
    }

    public void ConsumeCoyoteTime()
    {
        coyoteTimer = 0f;
    }

    public void OnAttack()
    {
        if (!CanStartAttack())
            return;

        HideHeldArrow();

        if (bowSFX != null)
            bowSFX.PlayPull();

        ChangeState(attackState);
    }

    public bool CanStartAttack()
    {
        return !waitForAimingRelease;
    }

    public void RequireAimingReleaseBeforeAttack()
    {
        waitForAimingRelease = true;
    }

    private void UpdateAimingReleaseLock()
    {
        if (waitForAimingRelease && !InputReader.IsAimingHeld())
            waitForAimingRelease = false;
    }

    public void ShowUpperBody()
    {
        if (upperBody != null)
            upperBody.SetActive(true);

        if (upperAnimator != null)
            upperAnimator.SetBool("IsAttack", true);
    }

    public void HideUpperBody()
    {
        if (upperAnimator != null)
            upperAnimator.SetBool("IsAttack", false);

        if (upperBody != null)
            upperBody.SetActive(false);
    }

    public void FinishAttackAnimation()
    {
        HideHeldArrow();

        if (animator != null)
        {
            animator.SetBool("IsAiming", false);
            animator.SetBool("IsAttack", false);
        }

        if (currentState == attackState)
            OnIdle();
    }

    private void ChangeState(PlayerState state)
    {
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

    public void UpdateTrajectory(Vector2 dir, float arrowSpeed, float arrowFlyTime, float arrowGravityValue)
    {
        if (trajectoryDotPrefab == null)
            return;

        EnsureTrajectoryDots();

        Vector3[] points = CalculateTrajectoryPoints(GetFirePointPosition(), dir, arrowSpeed, arrowFlyTime, arrowGravityValue);

        int count = trajectoryDots.Count;
        int fadeStartIndex = Mathf.RoundToInt((count - 1) * trajectoryFadeStart);
        int fadeRange = (count - 1) - fadeStartIndex;

        for (int i = 0; i < count; i++)
        {
            trajectoryDots[i].transform.position = points[i];
            trajectoryDots[i].SetActive(true);

            if (trajectoryRenderers[i] != null)
            {
                float alpha = i < fadeStartIndex
                    ? 1f
                    : 1f - (fadeRange > 0 ? (float)(i - fadeStartIndex) / fadeRange : 1f);
                Color c = trajectoryRenderers[i].color;
                c.a = alpha;
                trajectoryRenderers[i].color = c;
            }
        }
    }

    public void HideTrajectory()
    {
        for (int i = 0; i < trajectoryDots.Count; i++)
            trajectoryDots[i].SetActive(false);
    }

    private void EnsureTrajectoryDots()
    {
        while (trajectoryDots.Count < trajectoryPointCount)
        {
            GameObject dot = Instantiate(trajectoryDotPrefab, transform);
            dot.SetActive(false);
            trajectoryDots.Add(dot);
            trajectoryRenderers.Add(dot.GetComponent<SpriteRenderer>());
        }
    }

    private Vector3[] CalculateTrajectoryPoints(Vector2 start, Vector2 dir, float speed, float flyTime, float gravityValue)
    {
        Vector3[] points = new Vector3[trajectoryPointCount];
        float g = Physics2D.gravity.y * gravityValue;
        Vector2 phase1End = start + dir * speed * flyTime;

        for (int i = 0; i < trajectoryPointCount; i++)
        {
            float t = (i / (float)(trajectoryPointCount - 1)) * trajectoryMaxTime;
            Vector2 pos;

            if (t <= flyTime)
            {
                pos = start + dir * speed * t;
            }
            else
            {
                float tau = t - flyTime;
                pos = phase1End + dir * speed * tau + new Vector2(0f, 0.5f * g * tau * tau);
            }

            points[i] = new Vector3(pos.x, pos.y, 0f);
        }

        return points;
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

        bool isMoving = Mathf.Abs(Rigidbody2D.velocity.x) > 0.05f;

        if (!isGround || !isMoving)
        {
            footstepTimer = footstepInterval;
            return;
        }

        AudioClip[] currentFootsteps = null;
        float currentVolume = 1f;

        // 돌 / 신전바닥 / Runway가 우선
        if (isOnStone)
        {
            currentFootsteps = stoneFootsteps;
            currentVolume = stoneFootstepVolume;
        }
        else if (isOnGrass)
        {
            currentFootsteps = grassFootsteps;
            currentVolume = grassFootstepVolume;
        }

        if (currentFootsteps == null || currentFootsteps.Length == 0)
            return;

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            int randomIndex = UnityEngine.Random.Range(0, currentFootsteps.Length);
            AudioClip clipToPlay = currentFootsteps[randomIndex];

            if (clipToPlay != null)
            {
                footstepSource.PlayOneShot(clipToPlay, currentVolume);
            }

            footstepTimer = 0f;
        }
    }

    public void PlayJumpSound()
    {
        if (footstepSource == null)
            return;

        if (jumpSounds == null || jumpSounds.Length == 0)
            return;

        int randomIndex = UnityEngine.Random.Range(0, jumpSounds.Length);
        AudioClip clipToPlay = jumpSounds[randomIndex];

        if (clipToPlay != null)
            footstepSource.PlayOneShot(clipToPlay, jumpVolume);
    }

    private void HandleGroundCollision(Collision2D collision)
    {
        Collider2D col = collision.collider;

        bool isTopContact = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                isTopContact = true;
                break;
            }
        }

        if (!isTopContact)
            return;

        if (col.CompareTag("Floor") || col.CompareTag("Runway"))
        {
            isGround = true;

            if (CameraMovement.Instance != null)
                CameraMovement.Instance.SetCameraPosY(transform.position.y);
        }

        if (col.CompareTag("Floor"))
            isOnGrass = true;

        if (col.CompareTag("Runway"))
        {
            isOnRunway = true;
            isOnStone  = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleGroundCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleGroundCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
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