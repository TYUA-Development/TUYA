# 플레이어 조작 시스템 숙지 노트

## 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/Script/Player/PlayerController.cs` | 플레이어 중심 컴포넌트. 상태 관리, 물리, 애니메이터, 발소리, 화살 발사 |
| `Assets/Script/Player/PlayerState/PlayerState.cs` | 추상 기반 클래스 + 모든 구체 상태 클래스 |
| `Assets/Script/Player/PlayerInputReader.cs` | 키 입력 읽기 → `PlayerInputData` 구조체 |
| `Assets/Script/Player/Attack/Arrow.cs` | 화살 Rigidbody2D 발사체 |
| `Assets/Script/Player/UpperBodyArrowEventRelay.cs` | 애니메이션 이벤트 → PlayerController 브릿지 |

---

## PlayerController 핵심 구조

### 주요 필드
```
Rigidbody2D         - 물리 바디
charactorSprite     - 스프라이트 렌더러 (오타 주의: Charactor)
isGround            - 착지 여부 (FixedUpdate마다 false로 초기화 후 콜리전에서 set)
isOnRunway          - 런웨이 위 여부
isOnGrass / isOnStone - 발소리 타입 결정
setSpeed            - Inspector 기준 속도
moveSpeed           - 실제 속도 (SlowDownSpeed 코루틴으로 일시 변경 가능)
jumpPower / dashPower
attackTimer         - 공격 쿨다운 카운트다운
lockPlayerInput     - true면 InputReader.ClearInput() 강제
waitForAimingRelease - 공격 직후 조준키 재입력 방지
```

### 상태 객체
```
idleState / moveState / jumpState / dashState / fallState / attackState
currentState        - 현재 활성 상태
```

### Update / FixedUpdate 흐름
```
Update():
  1. CanInput && !lockPlayerInput → InputReader.ReadInput()
     아니면 → InputReader.ClearInput()
  2. UpdateAimingReleaseLock()   ← waitForAimingRelease 해제 체크
  3. currentState.LogicUpdate()
  4. CoolDown()                  ← attackTimer 감소
  5. PlayFootstep()

FixedUpdate():
  1. isGround / isOnRunway / isOnGrass / isOnStone = false (초기화)
  2. currentState.PhysicsUpdate()
  3. PreventSlide()              ← 정지 시 미끄러짐 방지 (gravityScale 0, velocity 0)
```

### 상태 전환 메서드
```
OnIdle()                  → HideHeldArrow, HideUpperBody, ChangeState(idleState)
OnMove()                  → HideHeldArrow, HideUpperBody, ChangeState(moveState)
OnJump()                  → HideHeldArrow, HideUpperBody, ChangeState(jumpState)
OnFall(grantCoyote=false) → grantCoyote이면 coyoteTimer 시작, ChangeState(fallState)
OnAttack()                → CanStartAttack() 체크 후 bowSFX.PlayPull(), ChangeState(attackState)
```

### 방향 전환
```csharp
ChangeDirection(float dir)
// dir == transform.localScale.x 일 때만 flip (같은 방향이면 반전)
// 반환값 true = 실제로 방향이 바뀜
```

### 코요테 타임 (Coyote Time)
```
coyoteTime   - Inspector 조절 가능 (기본 0.15f)
coyoteTimer  - 잔여 시간 카운트다운 (Update에서 매 프레임 감소)
CanCoyoteJump  → coyoteTimer > 0
StartCoyoteTime()   → coyoteTimer = coyoteTime
ConsumeCoyoteTime() → coyoteTimer = 0 (점프 후 즉시 소진)
```
- `OnFall(grantCoyote: true)` 호출 시 타이머 시작 (MoveState에서 낙하 시)
- 점프 후 하강(`JumpState → OnFall()`)은 `grantCoyote = false`이므로 타이머 미시작

### 입력 잠금
```csharp
LockPlayerInput(float time)      // 코루틴으로 time초간 lockPlayerInput = true
RequireAimingReleaseBeforeAttack() // attackState.Exit()에서 호출, 조준키 떼야 다시 공격 가능
```

### 화살 발사
```csharp
ShootArrow(Vector2 direction)
// 1. HideHeldArrow
// 2. arrowReleaseFXTemplate 스폰
// 3. bowSFX.PlayShoot()
// 4. arrowObject Instantiate → Arrow.Launch(direction, transform)
// 5. attackTimer = attackCoolTime
```

### 착지 판정
```
OnCollisionEnter2D / OnCollisionStay2D → HandleGroundCollision()
  "Floor" 태그 → isGround=true, isOnGrass=true
  "Runway" 태그 → isGround=true, isOnRunway=true, isOnStone=true
  (돌/신전 바닥 isOnStone은 다른 곳에서도 set 가능)
```

---

## PlayerState 상태 상세

### 공통 추상 인터페이스
```csharp
bool CanInput      // 기본 true. false면 Update에서 입력 안 읽음
Enter()            // 상태 진입 시
Exit()             // 상태 이탈 시
LogicUpdate()      // Update에서 호출
PhysicsUpdate()    // FixedUpdate에서 호출
```

### PlayerIdleState
- `Enter/Exit`: 비어있음
- `LogicUpdate`:
  - `SetTrigger("DetectFloor")` 매 프레임 호출
  - `ChangeDirection()` → 방향 전환
  - moveAxis.x != 0 → `OnMove()`
  - jumpPressed && isGround → `OnJump()`
  - aimingPressed → `OnAttack()`
- `PhysicsUpdate`: isGround일 때 velocity.x = 0

### PlayerMoveState
- `Enter`: `SetBool("IsMove", true)`
- `Exit`: `SetBool("IsMove", false)`
- `LogicUpdate`:
  - `wasGrounded && !isGround` → `StartCoyoteTime()` (지면 이탈 즉시 타이머 시작)
  - `wasGrounded = isGround` 갱신
  - aimingPressed → `OnAttack()`
  - `CheckFall()` && !isGround → `OnFall(grantCoyote: true)`
  - moveAxis.x == 0 && isGround → `OnIdle()`
  - jumpPressed && (`isGround` || `CanCoyoteJump`) → `ConsumeCoyoteTime()` → `OnJump()`
- `PhysicsUpdate`:
  - `CheckRunWayFromFront()` (런웨이 앞에서 RunwayObject.OnRunWayCollider 호출)
  - velocity.x = moveDirect * moveSpeed
  - `ChangeDirection(moveDirect)`
- `CheckFall()`: 발 아래 0.2f Raycast, 히트 없으면 true

### PlayerJumpState
- `Enter`: velocity.y = 0 → AddForce(up * jumpPower, Impulse), `SetBool("IsJump", true)`
- `Exit`: `SetBool("IsJump", false)`, moveSpeed 복원
- `LogicUpdate`: 비어있음 (Debug.Log만)
- `PhysicsUpdate`:
  - velocity.y <= 0.01 → `OnFall()`
  - velocity.x = moveDirect * moveSpeed

### PlayerFallState
- `Enter`: `SetBool("IsFall", true)`, `Play("JumpDown", 0, 0f)`
- `Exit`: `SetBool("IsFall", false)`
- `LogicUpdate`:
  - `CanCoyoteJump` && jumpPressed → `ConsumeCoyoteTime()` → `OnJump()` (코요테 점프)
- `PhysicsUpdate`:
  - `CheckLanding()` — OverlapBox로 바닥 감지 → isLanding=true, `SetTrigger("DetectFloor")`
  - isLanding이면 "JumpEnd" 애니메이션 완료 또는 "Idle"일 때 OnMove/OnIdle
  - velocity.x = moveDirect * moveSpeed
- `CheckLanding()`: 발 아래 OverlapBox (groundLayer = "Floor" 레이어만)
  - Runway이면 RunwayObject.OnRunWayCollider() 호출

### PlayerDashState (현재 비활성화)
- 입력: dashPressed (주석처리됨)
- `Enter`: dir 방향으로 velocity = (dir * dashPower, 0)
- `LogicUpdate`: velocity.x ≈ 0 되면 OnMove/OnIdle

### PlayerAttackState
- **플래그**: `isAiming`, `isFinishingAttack`, `attackQueued`
- **화살 캐시**: `arrowSpeed`, `arrowFlyTime`, `arrowGravityValue` — 생성자에서 arrowObject 프리팹의 Arrow + Rigidbody2D로부터 읽어 저장
- `Enter`:
  - isAiming=true, velocity.x=0 (수평 이동 정지)
  - `SetBool("IsAiming", true)`, `SetBool("IsAttack", false)`
- `Exit`:
  - `HideTrajectory()` → `LockPlayerInput(1.0f)` → `RequireAimingReleaseBeforeAttack()`
  - 애니메이터 bool 초기화
- `LogicUpdate` 흐름:
  1. isFinishingAttack → `UpdateFinishingAttack()` (애니메이션 완료 대기 → OnIdle)
  2. isAiming → "Attack" 애니메이션 진입 대기 (SetBool("IsAttack", true) 후 상태 확인)
  3. 조준 모드:
     - 마우스 월드 좌표 → 방향 벡터 계산
     - upperBodyMaxAngle/minAngle으로 각도 클램핑
     - 캐릭터 방향 자동 전환 (localScale.x 직접 조작)
     - upperBody.transform.localRotation으로 팔 회전
     - `UpdateTrajectory(direction, arrowSpeed, arrowFlyTime, arrowGravityValue)` 매 프레임 호출
     - attackPressed && attackTimer <= 0 → `ShootArrow()` → `StartAttackEnd()`
     - aimingPressed 해제 → `StartAttackEnd()`
- `StartAttackEnd()`: `HideTrajectory()` → HideHeldArrow → 애니메이터 초기화
- `PhysicsUpdate`: 비어있음 (공격 중 이동 없음)

### PlayerTurnState
- **미구현** — 모든 메서드가 `NotImplementedException`. 절대 전환하면 안 됨.

---

## PlayerInputReader

```csharp
struct PlayerInputData {
    Vector2 moveAxis      // x: -1/0/1 (MoveLeft/MoveRight 키)
    bool jumpPressed      // KeyBindingSettings.Jump (KeyDown)
    bool dashPressed      // Input.GetButtonDown("Dash")
    bool aimingPressed    // KeyBindingSettings.Aim (IsKeyHeld)
    bool attackPressed    // KeyBindingSettings.Shoot (KeyDown)
}
```

- `ReadInput()`: 매 Update에서 `CanInput && !lockPlayerInput`일 때만 호출
- `ClearInput()`: 잠금 상태일 때 빈 구조체로 초기화
- `IsAimingHeld()`: `KeyBindingSettings.IsKeyHeld(Aim)` — 상태 외부에서도 사용됨

---

## Arrow (발사체)

- `Launch(dir, shooter)`: velocity = dir * speed, 초기 gravityScale = 0
- `Update`: flyTime 카운트다운 → 0 이하면 gravityScale 복원 (포물선)
- `FixedUpdate`: velocity 방향으로 transform.right 갱신 (화살 회전)
- `OnTriggerEnter2D`: shooter 제외, IArrowHit 구현체 히트 시
  - StopFlightFX → PlayHit SFX → SpawnHitFX → target.OnHit() → Stick(target)
  - IArrowHit 없는 콜라이더는 무시

---

## UpperBodyArrowEventRelay

애니메이터 이벤트(Animation Event)에서 직접 PlayerController를 참조하기 어려울 때 사용하는 브릿지 컴포넌트. upperBody GameObject에 붙어있음.

- `ShowHeldArrow()` / `HideHeldArrow()`
- `ShowUpperBody()` / `HideUpperBody()`
- `FinishAttackAnimation()` ← 공격 애니메이션 완료 이벤트에서 호출됨

---

## 화살 궤적 미리보기 (Trajectory Preview)

Aiming 상태에서 화살이 날아갈 궤적을 점 40개로 미리 보여주는 기능.

### PlayerController 추가 필드 / 메서드

```csharp
[Header("Trajectory Preview")]
public GameObject trajectoryDotPrefab;   // Inspector에서 점 프리팹 할당 필요
public int trajectoryPointCount = 40;    // 점 개수
public float trajectoryMaxTime = 3f;     // 최대 시뮬레이션 시간(초)
private List<GameObject> trajectoryDots; // 런타임 dot 풀
```

- `EnsureTrajectoryDots()`: 풀이 없으면 `trajectoryDotPrefab`을 `trajectoryPointCount`개 Instantiate (지연 초기화)
- `UpdateTrajectory(Vector2 dir, float speed, float flyTime, float gravity)`:
  - `EnsureTrajectoryDots()` 호출
  - `CalculateTrajectoryPoints()` 결과로 각 dot 위치 설정 및 활성화
- `HideTrajectory()`: 모든 dot 비활성화
- `CalculateTrajectoryPoints()`: 2단계 물리 계산

### 2단계 궤적 수식

```
발사 위치 = transform.position (PlayerController의 위치)

Phase 1 (t ≤ flyTime): 직진 구간 (gravityScale = 0)
  pos = start + dir * speed * t

Phase 2 (t > flyTime): 포물선 구간 (gravityScale 복원)
  τ = t - flyTime
  phase1End = start + dir * speed * flyTime
  pos = phase1End + dir * speed * τ + Vector2(0, 0.5 * g * τ²)
  (g = gravity * Physics2D.gravity.y, 음수이므로 아래로 휨)
```

### PlayerAttackState 추가 내용

```csharp
// 생성자에서 한 번만 캐시
private float arrowSpeed;
private float arrowFlyTime;
private float arrowGravityValue;

// 생성자:
Arrow arrow = arrowObject.GetComponent<Arrow>();
arrowSpeed = arrow.speed;
arrowFlyTime = arrow.flyTime;
arrowGravityValue = arrowObject.GetComponent<Rigidbody2D>().gravityScale;
```

- Aiming 단계 `LogicUpdate()` 매 프레임: `controller.UpdateTrajectory(direction, arrowSpeed, arrowFlyTime, arrowGravityValue)`
- `Exit()` 및 `StartAttackEnd()`: `controller.HideTrajectory()` 호출

### Inspector 설정

- `PlayerController` 컴포넌트의 **Trajectory Dot Prefab** 슬롯에 dot 프리팹 할당 필요
- 프리팹은 작은 원형 스프라이트 등 원하는 비주얼로 제작

---

## 주요 주의사항

| 항목 | 내용 |
|---|---|
| `PlayerTurnState` | 미구현, 절대 전환 금지 |
| `ChangeDirection()` | 같은 방향일 때만 flip — 반환값 확인 필요 |
| `groundLayer` (FallState) | "Floor" 레이어만 체크 (Runway 제외) |
| `groundLayer` (MoveState) | "Floor" + "Default" 레이어 |
| 공격 후 1초 입력 잠금 | `attackState.Exit()`에서 `LockPlayerInput(1.0f)` 호출 |
| `waitForAimingRelease` | 공격 후 조준키 뗐다 다시 눌러야 재공격 가능 |
| `isGround` 초기화 | FixedUpdate 시작 시 false — CollisionStay로 매 프레임 갱신됨 |
| 코요테 타임 | MoveState 낙하 시만 부여. JumpState 하강 시 미부여 (더블점프 방지) |
| `wasGrounded` (MoveState) | MoveState 내부 필드 — 지면 이탈 한 프레임 공백을 커버하기 위해 추적 |
| `moveSpeed` vs `setSpeed` | setSpeed = Inspector 원본값, moveSpeed = 런타임 실제값 |
| 댐핑 주석 | PhysicsUpdate 내 이동 감속 코드 다수 주석 처리됨 (의도적 비활성화) |
| trajectory dot 프리팹 | Inspector 할당 안 하면 궤적 표시 안 됨 (null 체크로 조용히 스킵) |
