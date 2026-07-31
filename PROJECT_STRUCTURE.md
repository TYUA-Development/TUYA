# TUYA Project Structure Guide

This document is an onboarding map for AI coding agents such as Codex or Claude. It explains the Unity project layout, the main runtime systems, and the files to inspect before making changes.

## Project Snapshot

- Project name: `TUYA`
- Engine: Unity `2022.3.45f1`
- Render pipeline: Universal Render Pipeline `14.0.11`
- Project type: 2D Unity action/platformer-style game
- Main gameplay shape: player movement, jump, aim, arrow shooting, arrow-hit puzzle objects, camera staging, background/parallax, particles, and audio
- Solution: `TUYA-fork.sln`
- Main generated C# projects: `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`

## Root Layout

```text
TUYA-fork/
+-- Assets/             Game source, scenes, prefabs, textures, sounds
+-- Packages/           Unity Package Manager dependencies
+-- ProjectSettings/    Unity project settings
+-- README.md           Minimal repository description
+-- TUYA-fork.sln       Visual Studio/Rider solution
+-- *.csproj            Unity-generated C# project files
+-- Library/            Unity local cache; do not edit or analyze by default
+-- Temp/               Unity temp files; do not edit
+-- obj/                Build intermediate files; do not edit
+-- Logs/               Unity logs; useful only for debugging
+-- UserSettings/       Local user settings
+-- .vscode/, .vs/      IDE settings/cache
+-- .claude/            Local Claude-related settings
+-- .codex/             Local Codex-related settings, currently untracked
```

For most tasks, inspect `Assets/`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, and `ProjectSettings/EditorBuildSettings.asset` first. Avoid treating `Library`, `Temp`, `obj`, and `.vs` as source.

## Unity And Packages

Unity version is defined in `ProjectSettings/ProjectVersion.txt`:

```text
m_EditorVersion: 2022.3.45f1
```

Important packages from `Packages/manifest.json`:

- `com.unity.render-pipelines.universal`: URP `14.0.11`
- `com.unity.feature.2d`: Unity 2D feature set `2.0.1`
- `com.unity.textmeshpro`: TextMesh Pro `3.0.6`
- `com.unity.postprocessing`: Post Processing `3.4.0`
- `com.unity.recorder`: Recorder `4.0.3`
- `com.unity.timeline`: Timeline `1.7.6` (newly added; backs the `*_Profiles` Volume Profile assets under `Assets/Scenes`)
- `com.unity.ugui`: uGUI `1.0.0`
- `com.unity.visualscripting`: Visual Scripting `1.9.4`
- `com.unity.test-framework`: Unity Test Framework `1.1.33`
- `com.unity.collab-proxy`: Unity Version Control `2.11.2`
- `com.coplaydev.unity-mcp`: Unity MCP package from GitHub
- IDE integration packages: Rider, Visual Studio, VS Code

## Build Scenes

Enabled in `ProjectSettings/EditorBuildSettings.asset`:

- `Assets/Scenes/InGameScene/TitleScene.unity`
- `Assets/Scenes/InGameScene/Forest.unity`
- `Assets/Scenes/InGameScene/SeungHyun2_Restore.unity`

Present but disabled in build settings:

- `Assets/Scenes/1 Stage.unity`
- `Assets/Scenes/Jinho.unity`
- `Assets/Scenes/SeungHyun.unity`

Not referenced in `EditorBuildSettings.asset` at all (not even as a disabled entry) — sandbox/test scenes, not shippable levels:

- `Assets/Scenes/Mechanism.unity` — sandbox for testing Wind/Rope/Box/Pressure-platform mechanisms.
- `Assets/Scenes/InGameScene/Forest 1.unity` (renamed from `Forest 2.unity`) — a second copy of the Forest scene alongside `Forest.unity`; likely a working/backup copy used while iterating on the puzzle mechanisms below. Confirm which of `Forest.unity` / `Forest 1.unity` is the live level before editing either.

`Assets/Scenes/InGameScene/Forest.unity` and `Assets/Scenes/Mechanism.unity` both received further scene-data edits (level placement + mechanism tuning) since the previous snapshot; as of this revision both are committed and the working tree is clean.

Note: the enabled/disabled set and scene paths have changed since the last snapshot — `TitleScene` and `SeungHyun2_Restore` moved under `Assets/Scenes/InGameScene/` and are now enabled, while `SeungHyun.unity` (root-level, distinct from the `InGameScene` copy) is now disabled. Each main scene has a matching `<SceneName>_Profiles/` folder next to it holding Volume Profile `.asset` files (e.g. `Scenes/SeungHyun_Profiles/`, `Scenes/InGameScene/Forest_Profiles/`, `Scenes/1 Stage_Profiles/`) — treat these as scene-owned post-processing/volume data, not shared assets.

## Assets Layout

```text
Assets/
+-- Audio/              Project-wide AudioMixer (GameAudioMixer.mixer)
+-- Editor/             Unity Editor-only tools
+-- Physics/             Currently empty folder (reserved, no assets yet)
+-- Prefabs/            Shared prefabs (formerly "Prefab/", now plural; includes Prefabs/UI/)
+-- Scenes/             Unity scenes, per-scene Volume Profile folders
+-- Script/              Main gameplay C# scripts (also holds some prefabs/materials/animations colocated with their systems)
+-- sounds/              BGM, SFX, ambient audio, audio helper scripts
+-- TextMesh Pro/        TMP default resources
+-- Texture/             Character, background, object, environment, UI, and effect art
+-- LeafShaderGraph.mat   Root-level material (new; leaf shader graph instance)
+-- NewAudioMixer.mixer   Legacy/root-level mixer asset (see Audio/ for the current one)
+-- URP_*.asset          URP pipeline/renderer assets
+-- *.renderTexture      Render textures (`BaseRT.renderTexture`, `New Render Texture.renderTexture`)
```

Note: the `Assets/Prefab/` folder named in earlier notes has been renamed to `Assets/Prefabs/` (plural) and gained a `Prefabs/UI/` subfolder. `Assets/Physics/` is still a new, currently empty folder — no physics materials/settings live there yet. `Assets/Audio/` holds `GameAudioMixer.mixer`; a second, likely legacy, `NewAudioMixer.mixer` still sits at the `Assets/` root.

## Main Script Layout

`Assets/Script` is the primary runtime code area.

```text
Assets/Script/
+-- Arrow/              Arrow interfaces, small utilities, arrow prefabs/materials: ArrowBlocker, IArrowPassThrough.cs, IArrowKnockbackReceiver.cs (new)
+-- Camera/             Camera follow, zoom, parallax, trigger areas, title camera logic (incl. CameraEndingAreaTrigger.cs)
+-- Object/             Puzzle and interactive objects
|   +-- CoreObjects/    Core activation, temple, bridge, floor movement, rising/pressure objects
|   +-- MusicPuzzle/    Sound/note puzzle: hanging note objects, area controller, core bridges
|   +-- Rope/           Procedural cuttable rope + regeneration (Rope.cs, RopeSegment.cs, RopeRegenerator.cs (new))
|   +-- Stone Pillar/   Pillar and windmill objects
|   +-- StoneCircle/    Circle rotation, propeller, wind machine, passage looper
|   +-- StoneFloor/     Breakable platform events
|   +-- Wind/           Wind force objects (incl. particle-affecting wind; directional enum + distance falloff + player-blocking push)
|   +-- BasicObject.cs   Loose helper for drawing/instantiating sprite objects
|   +-- BoxObject.cs     Loose, IArrowHit/IArrowKnockbackReceiver box (new)
|   +-- DisappearMethod.cs  Loose, legacy-Animation "play then destroy" helper (new)
|   +-- Magnetic.cs      Loose rider/carry-along script
|   +-- PressurePlate.cs Loose, simple on/off ICoreEvent pressure plate (no weight comparison)
|   +-- RunwayObject.cs  Loose, player-triggered drop-through platform
|   +-- SampleObject.cs  Loose, minimal IArrowHit sample
+-- Particle/           Custom particle/object-pool system
+-- Player/             Player controller, input, state machine, attack, animations
|   +-- Animation/       Player .anim clips and Animator controllers
|   +-- Attack/          Arrow.cs
|   +-- PlayerState/     PlayerState.cs (all state classes live in this one file)
+-- Scene/              Scene-specific intro/cutscene controllers
+-- Settings/           Settings persistence, settings UI, key bindings, in-game settings
+-- Shader/             Shader helper scripts, shader/material assets (incl. SpriteFlash.shader (new), used for the Rope regeneration glow)
+-- Sky/                Sky/background manager, zone particle activator
+-- UI/                 Title, fade, menu UI, tutorial prompts
+-- Utils/              Shared interfaces, noise, generic Pair, TY_Weight.cs (new)
+-- TestJumpForce.cs     Loose debug/test script at Script root, not in any subfolder
```

Approximate C# file counts (113 total under `Assets/Script`):

- `Object` (incl. `CoreObjects`, `MusicPuzzle`, `Rope`, `Stone Pillar`, `StoneCircle`, `StoneFloor`, `Wind`, and 7 loose files: `BasicObject.cs`, `BoxObject.cs` (new), `DisappearMethod.cs` (new), `Magnetic.cs`, `PressurePlate.cs`, `RunwayObject.cs`, `SampleObject.cs`): 42
- `Camera` (incl. `Parallax`, `DistanceParallax`, `CameraEndingAreaTrigger.cs`): 21
- `UI`: 11
- `Settings`: 9
- `Particle` (incl. `ParticleComponent`): 9
- `Player` (incl. `PlayerState`, `Attack`): 7
- `Arrow` (incl. `IArrowPassThrough.cs`, new `IArrowKnockbackReceiver.cs`): 5
- `Utils` (incl. new `TY_Weight.cs`): 4
- `Sky`: 2
- `Scene`: 1
- `Shader`: 1

`CoreObjects/` alone now holds 15 scripts (up from 10): the original `CoreActivationController.cs`, `CoreCameraFocus2D.cs`, `CoreObjectMoveFloor.cs`, `CoreObjectTemple.cs`, `CorePropellerDoorSequence.cs`, `CoreTimedStoneGroupTrigger.cs`, `ICoreEvent.cs`, `RisingObjectController.cs`, `StoneBridge.cs`, `TimedRisingObjectController.cs`, plus new `CoreActivation.cs`, `CoreObjectToggle.cs`, `PressureCorePlatform.cs`, `PressureTopRelay.cs`, `RiseObject.cs`.
- Script root (loose): 1 (`TestJumpForce.cs`)

Additional related script files outside `Assets/Script` (119 total `.cs` files project-wide):

- `Assets/ParticleSystemOption.cs` (Assets root)
- `Assets/sounds/BGM/BGMFadeIn.cs`, `Assets/sounds/SFX/Bow/BowSFXRandomizer.cs`, `Assets/sounds/SteppeZoneTrigger.cs`
- `Assets/Editor/ReplaceSelectedWithPrefab.cs`
- `Assets/Texture/artwork/Propeller/RotateObject.cs` (new — generic continuous-rotation script, colocated with art rather than `Script/`)

## Runtime Architecture

### Player

Key files:

- `Assets/Script/Player/PlayerController.cs`
- `Assets/Script/Player/PlayerInputReader.cs`
- `Assets/Script/Player/PlayerState/PlayerState.cs`
- `Assets/Script/Player/Attack/Arrow.cs`
- `Assets/Script/Player/UpperBodyArrowEventRelay.cs`
- `Assets/Script/Player/PlayerSilhouetteController.cs`
- `Assets/Script/Player/PlayerBloomAreaTrigger.cs`

`PlayerController` is the central player component. In `Awake`, it initializes the input reader, Rigidbody2D, audio references, bow SFX, held-arrow visuals, FX templates, and state objects. In `Update`, it reads input when the current state allows it, updates the current state, handles attack cooldown, and plays footstep audio.

The state machine is implemented by `PlayerState` and these concrete states, **all defined in the single file `Assets/Script/Player/PlayerState/PlayerState.cs`**:

- `PlayerIdleState`
- `PlayerMoveState`
- `PlayerJumpState`
- `PlayerTurnState` - currently not implemented; its methods throw `NotImplementedException`
- `PlayerDashState`
- `PlayerAttackState`
- `PlayerFallState`

Important flow:

1. `PlayerController.Update()`
2. `currentState.LogicUpdate()`
3. `PlayerController.FixedUpdate()`
4. `currentState.PhysicsUpdate()`
5. State transitions go through `OnIdle`, `OnMove`, `OnJump`, `OnFall`, and `OnAttack`
6. Aiming is handled in `PlayerAttackState` by converting mouse position to world direction
7. Shooting calls `PlayerController.ShootArrow(Vector2 direction)`, instantiates `arrowObject`, and calls `Arrow.Launch`

Notes:

- Do not transition into `PlayerTurnState` until it is implemented.
- `PlayerController.ChangeDirection(float dir)` flips `transform.localScale.x`.
- Footsteps depend on `isGround`, `isOnGrass`, `grassFootsteps`, and (as of this snapshot) `InputReader.InputData.moveAxis.x != 0f` for the "is moving" check — previously this read horizontal Rigidbody2D velocity, which could report movement from external forces (e.g. wind push, knockback) even with no move input; it now reflects actual player input intent instead.
- `PlayerController.PreventSlide` now also freezes the player while `currentState == attackState` (previously it only froze when there was no horizontal/negative-vertical move input), so aiming/shooting on a slope no longer slides the player.
- Input locking has two entry points now: `LockPlayerInput(float time)` (timer-based, via `lockInputCoroutine`) and the new `SetInputLocked(bool locked)` (open-ended, cancels any running timer-based lock first). Callers that need to unlock based on a runtime condition (e.g. "player has landed") rather than a fixed duration should use `SetInputLocked` — see `BreakableFragmentPlatformEvent` below.
- `PlayerJumpState.PhysicsUpdate` and `PlayerFallState`'s ground probe now also check `controller.isGround` directly (not just Rigidbody2D vertical velocity / a small overlap box), reducing missed jump-to-fall / fall-to-ground transitions. `PlayerFallState`'s ground raycast now also discards a hit whose collider is a trigger (`hit.isTrigger`) — trigger volumes (e.g. Wind/detection triggers) can no longer be mistaken for solid ground.
- Several source comments have mojibake/encoding damage. Prefer actual code flow over comments.
- `PlayerSilhouetteController` lerps SpriteRenderer colors toward a silhouette color using `transitionSpeed`. Call `SetSilhouette(float)` to trigger.
- `PlayerBloomAreaTrigger` enables/disables a bloom effect when the player enters or exits a trigger zone.

### Player Animation

`Assets/Script/Player/Animation/` holds the player's Animator assets, colocated with code rather than under `Assets/Texture`:

- `PlayerAnimation.controller`, `PlayerUpperAnimation.controller` — lower/upper body Animator Controllers.
- Root clips: `Idle.anim`, `Move.anim`, `MoveTurn.anim`, `MovingStart.anim`, `MovingEnd.anim`, `JumpStart.anim`, `JumpUp.anim`, `JumpDown.anim`, `JumpEnd.anim`, `JumpEnd 1.anim`.
- `Attack/` subfolder: `Aiming.anim`, `Attack.anim`, `AttackEnd.anim`, `NoneAnimation.anim`.

### Input

`PlayerInputReader` writes per-frame input into `PlayerInputData`.

Fields:

- `moveAxis`
- `jumpPressed`
- `dashPressed`
- `attackPressed`
- `aimingPressed`

When changing input behavior, inspect both `PlayerInputReader.cs` and the relevant state methods in `PlayerState.cs`.

### Arrow And Hit Contract

Key files:

- `Assets/Script/Player/Attack/Arrow.cs`
- `Assets/Script/Arrow/IArrowHit.cs`
- `Assets/Script/Arrow/IArrowPassThrough.cs`
- `Assets/Script/Arrow/IArrowKnockbackReceiver.cs` (new)
- `Assets/Script/Arrow/DestroyAfterSeconds.cs`

`Arrow` is a Rigidbody2D projectile.

- `Launch(Vector2 dir, Transform shooter)` sets normalized velocity and orientation.
- Gravity is disabled at launch and restored after `flyTime`.
- `OnTriggerEnter2D` ignores the shooter, then checks `other.TryGetComponent<IArrowPassThrough>` **before** `IArrowHit`: if the collider implements `IArrowPassThrough`, the arrow calls `OnArrowPass(hitPoint, direction)` and `return`s immediately — it does **not** set `hasHit`, stop, or stick, so the arrow keeps flying straight through.
- Only if there is no `IArrowPassThrough` does it fall through to the existing `IArrowHit` check: on a valid hit, it stops flight FX, plays hit SFX, spawns hit FX. Just **before** calling `target.OnHit()`, it now also checks `other.TryGetComponent<IArrowKnockbackReceiver>` and, if present, calls `OnArrowKnockback(hitPoint, rb.velocity.normalized)` — this is checked in addition to `IArrowHit`, not instead of it (unlike the `IArrowPassThrough`/`IArrowHit` split, a single collider can implement both `IArrowHit` and `IArrowKnockbackReceiver` and get both effects). `target.OnHit()` then runs and the arrow sticks to the target transform.
- Colliders with neither `IArrowPassThrough` nor `IArrowHit` are ignored and the arrow passes through unaffected (physically, since these are triggers, not solid colliders). `IArrowKnockbackReceiver` alone (without `IArrowHit`) has no effect, since the knockback call only happens on the `IArrowHit` hit path.

Most puzzle interactions are connected through the `IArrowHit.OnHit()` contract. `IArrowPassThrough.OnArrowPass()` is for objects the arrow should visually/physically fly through while still reacting to the hit — currently used by rope cutting (see `Rope`/`RopeSegment` below). `IArrowKnockbackReceiver.OnArrowKnockback()` (new) is for objects that should physically recoil from the hit; `BoxObject.cs` (new, `Assets/Script/Object/BoxObject.cs`) is the only current implementer — it has an empty `IArrowHit.OnHit()` (so the arrow still sticks) and applies a horizontal `ForceMode2D.Impulse` of `knockbackForce` in the arrow's direction via `Rigidbody2D.AddForce`.

Arrow-related prefabs/materials live in `Assets/Script/Arrow/`:

- `Arrow.prefab`, `ArrowHitFX.prefab`, `ArrowTrajectoryPrefab.prefab` (likely a trajectory/aim-preview prefab)
- `M_Tuya_ArrowTrail.mat`, `M_Tuya_DustParticle.mat`

`Assets/Script/Arrow/ArrowBlocker.cs` (new) is a minimal `IArrowHit` implementer with an empty `OnHit()` body — its only purpose is to make `Arrow.OnTriggerEnter2D` recognize the object as a valid hit target so the arrow sticks to it via the existing `Stick()` logic, without triggering any puzzle/game logic of its own. Use it on decorative or blocking geometry that should simply catch arrows.

### Camera

Important files:

- `Assets/Script/Camera/CameraMovement.cs`
- `Assets/Script/Camera/CameraMoveManager.cs`
- `Assets/Script/Camera/MissionAreaCamera.cs`
- `Assets/Script/Camera/SH_MissionAreaCamera.cs`
- `Assets/Script/Camera/StartInsideMissionCamera.cs`
- `Assets/Script/Camera/FakeZZoomManager.cs`
- `Assets/Script/Camera/NaturalCameraSway.cs`
- `Assets/Script/Camera/SkyZoomScaler.cs`
- `Assets/Script/Camera/TitlePerspectiveManager.cs`
- `Assets/Script/Camera/CameraPerspectiveData.cs`
- `Assets/Script/Camera/PlayerCutsceneLocker2D.cs`
- `Assets/Script/Camera/BacklightAreaTrigger.cs`
- `Assets/Script/Camera/CameraYLockZoomArea.cs`
- `Assets/Script/Camera/CameraRestoreAreaTrigger.cs`
- `Assets/Script/Camera/FallZoomCameraArea.cs`
- `Assets/Script/Camera/DemoEndFadeToTitle.cs`
- `Assets/Script/Camera/CameraEndingAreaTrigger.cs` (new)
- `Assets/Script/Camera/Parallax/ParallaxManager.cs`
- `Assets/Script/Camera/Parallax/ParallaxImage.cs`
- `Assets/Script/Camera/DistanceParallax/DistanceParallaxManager.cs`
- `Assets/Script/Camera/DistanceParallax/DistanceParallaxObject.cs`
- `Assets/Script/Camera/PinLightBlend.shadergraph`

`CameraMovement` is the core follow/staging camera class and exposes `CameraMovement.Instance`. It follows the player object named by the `Charactor` field, supports fixed/event camera moves, optional player-Y following, and shake/noise.

Puzzle scripts such as `StoneBridge` and `CoreObjectTemple` call into `CameraMovement` for staging.

Additional camera area scripts:
- `PlayerCutsceneLocker2D`: locks player input/movement during cutscene sequences; released by timeout or explicit call.
- `BacklightAreaTrigger`: toggles backlight/bloom camera effects on player enter/exit.
- `CameraYLockZoomArea`: locks camera Y axis and adjusts zoom while player is inside the trigger.
- `CameraRestoreAreaTrigger`: restores camera to default follow state when player re-enters a zone. Its Y-target and finalize logic now live in `protected virtual` methods (`GetTargetCameraY`, `FinalizeCameraY`) specifically so subclasses can override them (see `CameraEndingAreaTrigger` below).
- `CameraEndingAreaTrigger` (new): subclasses `CameraRestoreAreaTrigger` and overrides `GetTargetCameraY`/`FinalizeCameraY` to lock the camera rig to a fixed `fixedCameraY` instead of following the player's Y offset — used for the ending sequence. As of this snapshot it exists as a script but is not yet attached to any GameObject in a scene or prefab.
- `FallZoomCameraArea`: adjusts camera zoom during fall zones.
- `DemoEndFadeToTitle`: fades screen and loads the title scene when the player reaches the demo end.

### Puzzle And Interactive Objects

Important folders:

- `Assets/Script/Object/`
- `Assets/Script/Object/CoreObjects/`
- `Assets/Script/Object/MusicPuzzle/`
- `Assets/Script/Object/Stone Pillar/`
- `Assets/Script/Object/StoneCircle/`
- `Assets/Script/Object/StoneFloor/`
- `Assets/Script/Object/Wind/`

Common contracts:

- `IArrowHit.OnHit()` receives arrow-hit events.
- `ICoreEvent.OnCoreEvent(bool isPressed = true)` represents a broader puzzle/core activation event; the `isPressed` argument distinguishes press vs. release for callers that care (e.g. `PressurePlate`'s `isCancel` mode).
- `IArrowKnockbackReceiver.OnArrowKnockback(hitPoint, hitDirection)` (new, see Arrow And Hit Contract above) is a physical-recoil-only contract, orthogonal to `IArrowHit`.
- Many effects are Coroutine-based movement, rotation, activation, and camera staging.

Key files:

- `BasicObject.cs`: helper for drawing/instantiating sprite objects.
- `RunwayObject.cs`: toggles a runway collider while the player is inside/staying on it. Now tracks `playerInsideDetection` (set in `OnTriggerStay2D`, cleared in `OnTriggerExit2D`): while true, neither the delayed re-enable coroutine (`DropRoutine`) nor the external `OnRunWayCollider()` call will turn the collider back on — only actually leaving the detection trigger re-arms it. Fixes the platform re-enabling underneath the player while they are still standing in the detection zone.
- `SampleObject.cs`: minimal `IArrowHit` sample.
- `PressurePlate.cs`: simple binary (no weight comparison) `ICoreEvent` trigger — any layer-matched `Collision2D` pressed from above (contact normal `y >= minUpwardNormal`) calls `OnCoreEvent(true)` on each `ICoreEvent` in `targetObjects`; if `isCancel` is set, releasing all pressing colliders calls `OnCoreEvent(false)`. Distinct from the newer weight-comparison `PressureCorePlatform` below — the two are separate systems that happen to share the word "Pressure".
- `CoreActivationController.cs`: implements both `IArrowHit` and `ICoreEvent`. On arrow hit, fires a full cutscene sequence — letterbox, player lock via `PlayerCutsceneLocker2D`, camera focus, tutorial prompt, hint ring — then broadcasts the core activation event.
- `CoreActivation.cs` (`CoreObjects/`, new): a second, self-contained core-activation implementation — also `IArrowHit`+`ICoreEvent`, also exposes an `onActivated` event — but instead of delegating to a hint-ring/tutorial/letterbox sequence, it owns its own visuals directly (`hitFlashRenderer`/`activateGlowRenderer`/`stableGlowRenderer` alpha fades, `hitParticle`/`activateParticle`, `hitAudio`/`activateAudio`) and locks the player via `PlayerCutsceneLocker2D` or a plain `PlayerController.LockPlayerInput` fallback. It activates on either `OnTriggerEnter2D` or `OnCollisionEnter2D` from an object tagged/typed as `Arrow`, not only via the `IArrowHit`/`Arrow.cs` stick path. **`CoreActivation` and `CoreActivationController` are two different classes with similar purposes** — check which one a given core prefab actually uses before assuming shared behavior.
- `CoreObjectToggle.cs` (`CoreObjects/`, new): subscribes to one or more `CoreActivationController.onActivated` events (`coreObjects` list — any one firing runs the same handler) and, on activation, flips each `targetObjects` entry: if the target has a `RiseObject` component, calls `Rise()` on it; otherwise, if it (or a child) has `Object_Wind`/`Object_Wind_Particle`, smoothly fades `Object_Wind.windPower` / `Object_Wind_Particle.powerScale` to/from 0 over `windFadeDuration` (via coroutine `FadeWindAndToggle`, tracked per-object in `windFadeCoroutines`) instead of an instant on/off, additionally fading `Object_Wind_Particle` particle alpha out over `particleFadeOutDuration` before disabling; otherwise just calls `GameObject.SetActive(!activeSelf)`.
- `CoreObjectTemple.cs`: raises temple pieces and optionally moves the player with a selected piece.
- `CoreObjectMoveFloor.cs`: toggles wind objects, toggles propeller rotation, and moves floors between previous/next positions.
- `CoreCameraFocus2D.cs`: smoothly pans and zooms the camera to a focus point during core events.
- `CorePropellerDoorSequence.cs`: sequences a propeller spin → door open animation on core activation.
- `CoreTimedStoneGroupTrigger.cs`: activates a group of stone objects after a timed delay on core event.
- `RisingObjectController.cs`: moves a set of objects upward on activation.
- `TimedRisingObjectController.cs`: same as `RisingObjectController` but with configurable per-object delay.
- `RiseObject.cs` (`CoreObjects/`, new): a single-object, richer alternative to `RisingObjectController`/`TimedRisingObjectController` — `Rise()` (called externally, e.g. by `CoreObjectToggle`) runs a one-shot coroutine that optionally pre-shakes in place (`usePreShake`), then moves from the current position to `targetPosition` over `riseDuration` along `riseCurve`, with optional continuous shake during the move (`useShakeDuringRise`, fading out near the end via `fadeOutShakeNearEnd`) and dust/light/debris/complete particles + matching audio cues at each phase. `enableReturn` makes it a round trip: hold at the target for `holdDuration`, then move back and reset `hasRisen` so `Rise()` can run again.
- `StoneBridge.cs`: moves bridge pieces, raises core, and triggers camera movement/noise.
- `StonePillarManager.cs`: creates stone pillars and windmills; windmill hits move connected pillars by step. Each pillar's next target position is now tracked in a `currentTargetPosition` list (updated from the *target*, not read back from the possibly-still-moving `transform.position`), and each pillar's move coroutine is tracked/stopped-and-restarted per index (`pillarMoveCoroutines`) — fixes drift/desync when a pillar is re-triggered while still mid-move.
- `WindMillObject.cs`: `IArrowHit` adapter that calls `StonePillarManager.PillarMove`.
- `StoneCircleManager.cs`: rotates connected circles for a trigger id. Same fix pattern as `StonePillarManager`: target rotation per circle is tracked in a `currentTargetRotation` dictionary and compounded from there (not from `transform.localRotation`), and the running rotate coroutine per circle is tracked/stopped in `circleRotateCoroutines` before starting a new one, so rapid re-triggers don't desync the rotation.
- `CircleHitObject.cs`: `IArrowHit` adapter that calls `StoneCircleManager.RotateCircles`.
- `PropellerSpinner.cs`: spins a propeller object continuously or on activation.
- `RotatingPassageLooper.cs`: loops a passage object's rotation for ambient motion.
- `WindMachineActivationController.cs`: activates the wind machine sequence on core event.
- `PassThroughExitCameraZoom.cs`: adjusts camera zoom when the player exits a pass-through area.
- `Object_Wind.cs`: applies directional wind force to Rigidbody2D objects inside its trigger. Direction is chosen via a `WindDirection` enum dropdown (`Right/UpRight/Up/UpLeft/Left/DownLeft/Down/DownRight`, resolved by the static `Object_Wind.GetDirectionVector(WindDirection)`) instead of being derived from `transform.rotation.eulerAngles.z`; negative `windPower` still flips the effective direction. `distanceFalloff` (0-10, `[Range]`) scales force down with distance from the wind object's own `transform.position` via `1f / (1f + distanceFalloff * distance)` — `0` means no falloff. New `ignoredLayer` mask makes matching colliders immune to the wind entirely. The `blockPlayer` path was reworked: it no longer just refuses entry — `BlockPlayer()` now measures the player/wind collider AABB overlap (`ComputeAxisPushDirection`, picking the shallower-overlapped axis) and pushes the player rigidbody out along that axis at up to `blockPushSpeed` units/sec (so the correction is visible motion, not a teleport), with `blockBounciness` (0-1) controlling how much of the player's into-the-wall velocity bounces back versus is simply absorbed. A player falling in from directly above (`IsFallingFromAbove`, comparing the previous-frame Y position against the wind collider's top bound) is exempted from both push force and blocking via `fallThroughExempt` until they fully exit the trigger — except for `WindDirection.Up` wind, which is meant to catch falling players. `IsBlocked()`'s line-of-sight cast now originates from the wind trigger's actual collider bounds center (not `transform.position`, which is often flush with the floor and would self-intersect) and shortens the cast by the target's own half-extent along the cast direction so the target's own ground/wall no longer registers as a false block.
- `Object_Wind_Particle.cs` (`Wind/`): pushes particles of assigned `ParticleSystem`s that are inside its collider by directly rewriting `ParticleSystem.Particle.velocity` via `GetParticles`/`SetParticles` (bypasses Rigidbody2D physics, so it works on non-physical particle-based foliage/dust). Each particle is assigned a fixed target speed for its whole lifetime, deterministically derived from its `randomSeed` (no per-particle dictionary needed — the seed never changes) via `GetAssignedSpeed()`: it picks one of the stepped values in `[windSpeedMin, windSpeedMax]` at `speedStep` increments (e.g. Min=5/Max=8/Step=1 → each particle randomly and permanently gets 5, 6, 7, or 8). The wind-axis velocity component is *assigned* each frame to `assignedSpeed * powerScale` (dot-product decomposition into an along-wind and perpendicular component, so gravity/other-force-driven perpendicular velocity is preserved) rather than accumulated with `+=` — the old accumulating form let particles pushed for longer keep gaining speed indefinitely. `powerScale` (0-1, default 1) is a separate overall multiplier used by `CoreObjectToggle` to fade the wind in/out; it does not affect which stepped speed a particle was assigned, only scales it. There is no longer a `distanceFalloff` (removed — replaced by the explicit speed range above; `Object_Wind`'s own `distanceFalloff` is unrelated and unaffected). Also has a "Stretch By Speed" option (`stretchBySpeed`, `stretchLengthScale`, `stretchVelocityScale`, applied once in `Init()` via `ApplyStretchSettings()`) so particles visually elongate in proportion to push speed. New helper methods (added to support `CoreObjectToggle`'s fade-in/out): `SetEmissionEnabled(bool)` toggles the emission module, `SetParticlesAlpha(float)` rewrites every live particle's `startColor` alpha via `GetParticles`/`SetParticles`, and `StopAndClearParticles()` stops with `StopEmittingAndClear`.
  - **Lifetime Fade** (`fadeOutOverLifetime`, default on; `fadeStartLifetimePercent`, default 0.5): `ApplyLifetimeFadeSettings()` (called from `Start()`) builds a `ColorOverLifetimeModule` alpha gradient per assigned `ParticleSystem` — full alpha until `fadeStartLifetimePercent` of the particle's lifetime, then linear to 0 — so particles fade out near end-of-life instead of popping when the underlying system's lifetime expires. This is a separate alpha pipeline from `SetParticlesAlpha`'s `startColor.a` (final visible alpha is their product), so `CoreObjectToggle`'s whole-system fade and this per-particle lifetime fade don't overwrite each other.
  - **Blocked Fade-Out** (`blockedFadeOutDuration`, default 0.15s): a particle blocked by `blockingLayer` no longer disappears immediately (`remainingLifetime = 0`) unless `blockedFadeOutDuration` is 0 — instead its `startColor` alpha lerps from the alpha it had when blocking began down to 0 over that duration, tracked frame-to-frame per particle (by `randomSeed`) in `blockedFadeStates` (`Dictionary<ParticleSystem, Dictionary<uint, BlockedFadeState>>`), rebuilt each frame from only the particles still blocked so entries for dead/unblocked particles don't leak. `killOnCollisionLayer` (Floor-type kills) is unaffected and still instant.
- `WindSystemManager.cs`: mostly empty placeholder at the time of writing.
- `BreakableFragmentPlatformEvent.cs` (`StoneFloor/`): on player contact, disables the platform collider and triggers a fall sequence via `PlayerController.OnFall()` after a configurable FixedUpdate delay. Player input is now re-locked/unlocked via `PlayerController.SetInputLocked(bool)` instead of a fixed-duration `LockPlayerInput(time)` call — `UnlockInputAfterLandingRoutine()` waits for the player to actually leave and then re-touch the ground (`playerController.isGround`) before unlocking, plus an optional `extraInputLockAfterFinalImpact` grace period, rather than assuming a fixed fall duration.
- `Magnetic.cs` (`Object/`, loose file not in a subfolder): tracks its own per-`FixedUpdate` position delta and, for each `GameObject` in its `attachedObjects` list that is currently touching its collider, applies the same delta to that object (via `Rigidbody2D.MovePosition` if it has one, otherwise directly to `transform.position`). Used to carry riders/objects along with a moving platform-like object without a physics joint. Caches colliders per attached object in a `Dictionary`. As of this snapshot it exists as a script but is not yet attached to any GameObject in a scene or prefab.
- `BoxObject.cs` (`Object/`, loose): see Arrow And Hit Contract above — `IArrowHit`+`IArrowKnockbackReceiver`, applies a horizontal impulse on arrow hit. Also used as the hanging-box payload for `RopeRegenerator` (see Rope below). `Awake()` now also calls `IgnorePlayerCollision()` (new): if a `boxCollider2D` reference is assigned, it finds the scene's `PlayerController` via `FindObjectOfType` and calls `Physics2D.IgnoreCollision` between the box's collider and the player's, so a knocked-back/pushed box can't shove or block the player. No-ops if `boxCollider2D` is left unassigned.
- `DisappearMethod.cs` (`Object/`, loose, new): `[RequireComponent(typeof(Animation))]`. `PlayAndDestroy()` plays a legacy `AnimationClip` (`disappearClip`) via the (non-Animator) `Animation` component, waits for its length, then destroys the GameObject. Other scripts that need to remove an object with a fade/disappear animation check for this component first (see `RopeRegenerator.RemoveBox` below) and fall back to a plain `Destroy()` if it's absent.

When changing puzzles, check Inspector-serialized lists and scene/prefab references. Many connections depend on list index order.

### Box, Pressure Plates, and Weighted Platforms (new)

Two independent "weight/pressure" systems now exist — do not conflate them:

- **Simple on/off**: `PressurePlate.cs` (documented above) — any qualifying contact presses it; no comparison, no weight value.
- **Weight-comparison seesaw**: `Assets/Script/Object/CoreObjects/PressureCorePlatform.cs` + `PressureTopRelay.cs` + `Assets/Script/Utils/TY_Weight.cs`, all new.
  - `TY_Weight.cs`: a trivial `MonoBehaviour` carrying a single `public float weight = 1f`. Attach to anything that should count toward a `PressureCorePlatform`'s load (e.g. `BoxObject`).
  - `PressureCorePlatform.cs`: sits on the platform's parent (which owns the `Rigidbody2D`); `topCollider` points at a child "Top" object's `Collider2D` that actually receives contact. Because Unity collision callbacks fire on the collider's own GameObject, the parent can't receive `OnCollisionEnter2D` for a child collider directly — `PressureTopRelay.cs` sits on the Top child and forwards `OnCollisionEnter2D/Stay2D/Exit2D` up to the parent's `HandleTopCollisionEnter/Stay/Exit` methods. Each contacting collider's `TY_Weight.weight` (found via `GetComponentInParent`) is summed into `currentWeight` (`pressingWeights` dictionary, contact accepted only if `|normal.y| >= minUpwardNormal`). Each platform has a `connectedPlatform` partner; `EvaluatePair()` compares `currentWeight` between the pair (equal weight = keep previous state, a hysteresis to avoid flapping) and drives the heavier one down (`MoveDownRoutine`, moving `topCollider.transform` in world space toward `bottomStopper` until `ColliderDistance2D` reports ~0) and the lighter one up (`MoveUpRoutine`, toward `upLocalPosition` resolved into world space via the Top's parent transform) — both always share the heavier side's `moveSpeed` so up/down motion stays in sync between the pair.
- **Box payload**: `BoxObject.cs` (documented above) is the object typically weighed/knocked around by these systems; `Assets/Prefabs/Box (1).prefab` and `Assets/Prefabs/PressurePlatformCore.prefab` are the corresponding level-placeable prefabs, alongside art in `Assets/Texture/artwork/Puzzle_esset_Wind/` (pressure-plate and platform sprites) and `Assets/Prefabs/Windgate.prefab` (a Wind-based gate object, art also in `Puzzle_esset_Wind/`). `Box (1).prefab` was reworked this revision: its old separate trigger `BoxCollider2D` was removed and `BoxObject.boxCollider2D` now points at the same solid `BoxCollider2D` used for physics, its sprite/scale were swapped to redesigned art (root scale `2` → `~4.13`). `Assets/Prefabs/Box_Middle.prefab` (new) is a second, distinct box variant that layers `RunwayObject` (drop-through platform behavior, on a child object named `Runway`) and `TY_Weight` on top of the same `BoxObject`+`ArrowBlocker` combo (`ArrowBlocker` on a child object named `Collider`) — a box that can also be dropped through and counted as pressure-plate weight, not just knocked around.

### Rope

Key files, all under `Assets/Script/Object/Rope/`:

- `Rope.cs`: procedurally builds a cuttable rope out of `RopeSegment` pieces connected by `HingeJoint2D`s. `[ContextMenu("Build Rope")]` → `BuildRope()` spawns `ropeLength / segmentLength` segments (each with a child `Visual` GameObject holding the `SpriteRenderer` — scaled by `segmentSpriteScale`, previously the `SpriteRenderer` sat directly on the segment — plus a dynamic `Rigidbody2D` + trigger `BoxCollider2D` (sized by `segmentSpriteScale` too) + `HingeJoint2D` chained to the previous segment's body, first segment anchored to a static `Rigidbody2D` on `anchor`/`transform`) under a generated `GeneratedRopeSegments` child; `[ContextMenu("Clear Rope")]` → `ClearRope()` destroys them (and any hanging-object joints, see below). Segment direction/rotation comes from a normalized `direction` vector (defaults `Vector2.down`). Optional `useJointLimits`/`jointLimitAngle` constrain each hinge's swing. `segmentOrderInLayer` sets the generated `SpriteRenderer.sortingOrder`. `NotifySegmentCut(segment, cutPoint)` is the callback a `RopeSegment` invokes when cut — spawns `cutFXPrefab` and plays `cutClip` via `audioSource`. New `RopeSegment[] Segments` (read-only) and `bool IsCut` (true if *any* segment is cut) public accessors exist for external observers like `RopeRegenerator`.
  - New **Hanging Objects** feature: a `RopeHangingAttachment[] hangingAttachments` array (`target` Rigidbody2D, `segmentIndex` — negative means "last segment" — plus per-side local anchors) lets `BuildRope()`'s `AttachHangingObjects()` add a `HingeJoint2D` on each `target` connecting it to the resolved segment's body, so objects (e.g. a box) dangle from a specific point on the rope. `SetHangingTarget(attachmentIndex, newTarget)` lets external code (`RopeRegenerator`) swap which Rigidbody2D is hooked to a given slot before the next `BuildRope()`.
- `RopeSegment.cs`: implements `IArrowPassThrough` (not `IArrowHit`) on each generated segment's trigger collider. `OnArrowPass(hitPoint, direction)` calls `Cut(hitPoint)`, which destroys the segment's own `HingeJoint2D` (severing it from the previous segment/anchor) and notifies the owning `Rope`. `IsCut` reports `joint == null`. Because the arrow uses `IArrowPassThrough`, it keeps flying through the rope instead of sticking — the rope segment falls away (still simulated by its `Rigidbody2D`) rather than the arrow embedding in it. `Body` (the segment's `Rigidbody2D`) is exposed for `Rope.AttachHangingObjects()`.
- `RopeRegenerator.cs` (new): a separate observer script (does not modify `Rope`/`RopeSegment` directly) that watches `rope.IsCut` every `Update()` and, once true, runs `RegenerateRoutine()`: wait `regenerateDelay`, then `CollapseRopeSegments()` (finds the topmost cut segment via `FindTopmostCutIndex` and fades-and-destroys segments outward from that pivot in `segmentDisappearStepDelay`-spaced steps via `BuildCollapseSteps`), then `AdvanceHangingBoxes()` (per `HangingBoxSlot`: destroys the previous fallen box — via its `DisappearMethod` if present, else `Destroy` — advances `currentBox` to `previousFallenBox`, and `Instantiate`s a fresh `boxPrefab` at the slot's recorded `spawnPosition`/`spawnRotation`, wiring it back into the rope via `rope.SetHangingTarget`), then calls `rope.BuildRope()` to regrow the rope, then `PlayGlowFade()` — swaps affected `SpriteRenderer`s (rope segments + newly spawned boxes) onto a shared `Custom/SpriteFlash`-shader `Material` (`Assets/Script/Shader/SpriteFlash.shader`, new) and fades `_FlashAmount` from 1 to 0 over `glowFadeDuration` so regenerated pieces visibly flash white before settling to normal.

The rope is purely physics-visual (no `ICoreEvent`/puzzle wiring itself) — cutting a segment just lets gravity/joints take over for everything downstream of the cut; `RopeRegenerator` is what turns that into a repeatable puzzle mechanic (shoot the rope, the box falls, wait, a fresh box appears). `Assets/Prefabs/Wind (4).prefab` and level placements in `Assets/Scenes/Mechanism.unity` / `Assets/Scenes/InGameScene/Forest.unity` (and `Forest 1.unity`) are where Wind and Rope objects are actually composed together (e.g. rope bridges that sway in wind and can be shot down, or hanging boxes that regenerate after being cut loose).

### Music/Sound Puzzle (new)

Key files, all under `Assets/Script/Object/MusicPuzzle/`:

- `MusicPuzzleAreaController.cs`: the puzzle's central coordinator. Holds a "question" melody (`expectedNoteIndexes`) and drives a play sequence: question core lights up and plays its expected note sequence, the player sets note dots on hanging note objects, an "answer" core submits the current note sequence, and the controller compares it against the expected sequence to trigger `SuccessRoutine` (opens a path by lerping `pathMoveTargets` positions, deactivates walls/colliders, plays a guide line/particle effect toward the exit) or `FailRoutine` (fail SFX only). All timings/audio are coroutine-driven and fully Inspector-configurable (per-step delays, clips, volumes). While a question/answer sequence is playing, `SetPuzzleCoresLocked(true)` locks both cores via `MusicPuzzleCoreBridge.SetExternalActivationLocked` (unlocked again afterward unless the puzzle is already solved), preventing re-triggering mid-sequence; `IsSequenceRunning` exposes this state. Answer playback now runs through a dedicated `PlayAnswerNoteSequence` (distinct from the question's `PlayNoteSequence`) that also fires a `dotSparkleEffect` particle at each hit note's active-dot position via `HangingMusicPuzzleNoteObject.GetActiveDotTransform()`.
- `HangingMusicPuzzleNoteObject.cs`: a single hanging chime/note object. Arrow hits on its propeller collider (routed through `MusicPuzzlePropellerHitProxy`) cycle its `currentNoteIndex`, spin the propeller sprite, apply a physics impulse to the hanging body, and fade dot sprites to show the active note. `GetActiveDotTransform()` (new) returns the transform of the currently active dot sprite, used by the area controller to position answer-playback sparkle FX. Also contains editor-only `[ContextMenu]` builders (`WirePropellerHitProxy`, `BuildChainFromSettings`) that procedurally generate a `HingeJoint2D` chain of link GameObjects between an anchor and the body — a level-building convenience, not runtime logic.
- `MusicPuzzleCoreBridge.cs`: adapts a puzzle core (question or answer, via `MusicPuzzleCoreRole`) to the puzzle controller. Implements `IArrowHit` and can optionally wrap an existing `CoreActivationController` (subscribing to its `onActivated` event, and using `activationLocked` / `FadeInActivateGlow()` to reuse its visuals) so puzzle cores can piggyback on the existing core-activation system instead of duplicating visuals. `OnHit()` now also no-ops while `puzzleController.IsSequenceRunning` is true, in addition to the existing solved-puzzle check.
- `MusicPuzzlePropellerHitProxy.cs`: a small `IArrowHit` proxy placed on a propeller's trigger collider; on `OnTriggerEnter2D` with an `Arrow`, it computes the hit point and forwards to the owning `HangingMusicPuzzleNoteObject.HandlePropellerHit()`.
- `MusicPuzzleAreaTriggerBridge.cs`: a trigger volume that calls one of `MusicPuzzleAreaController.StartMusicPuzzle/ActivatePuzzle/BeginPuzzleFromArea` when the player enters (`startOnPlayerEnter`, `startOnlyOnce`), plus a `UnityEvent onPuzzleStart` for extra scene wiring.

Related non-script assets:

- Art: `Assets/Texture/artwork/Sound_Puzzle/` (`Body.png`, `Chain_Link_A/B.png`, `Dot_Active.png`, `Dot_Base.png`, `Propeller.png`).
- Audio: `Assets/sounds/SFX/MusicPuzzle/` (`Note_0`-`Note_3.wav`, `Sound1`-`Sound4.wav`, `fail.mp3`).
- `Assets/Texture/artwork/Propeller/RotateObject.cs`: a generic, puzzle-agnostic continuous-rotation script (`transform.Rotate` per frame) colocated with propeller art — distinct from the coroutine-based decelerating spin in `HangingMusicPuzzleNoteObject`.

### Scene Controllers

Key file:

- `Assets/Script/Scene/ForestIntroController.cs`

`ForestIntroController` drives the Forest scene intro sequence on `Start`:

1. Disables player control and camera follow scripts.
2. Teleports the player to `startPoint`, resets Rigidbody2D, and snaps the camera to `introCameraPoint`.
3. Starts a concurrent `FadeSceneFromBlack` coroutine.
4. Walks the player to `targetPoint` using `Rigidbody2D.MovePosition` synchronized to `WaitForFixedUpdate` with `Time.fixedDeltaTime`.
5. After the walk, runs `OpenBarsAndZoomOut` to animate letterbox bars out and zoom the camera back to normal size.
6. Re-enables player control and camera follow, then triggers `TutorialAreaPrompt.ShowPrompt()`.

Important: the walk loop reads `playerRigidbody.position.x` (not `player.position.x`) and uses `Time.fixedDeltaTime` + `yield return new WaitForFixedUpdate()` to avoid frame-rate-dependent movement.

### Particles

Important files:

- `Assets/Script/Particle/ParticleManager.cs`
- `Assets/Script/Particle/ParticleScriptable.cs`
- `Assets/Script/Particle/ParticleEmitter.cs`
- `Assets/Script/Particle/ParticleScript.cs`
- `Assets/Script/Particle/ParticleComponent/ParticleSpin.cs`
- `Assets/Script/Particle/ParticleComponent/ParticlePulse.cs`
- `Assets/Script/Particle/ParticleComponent/ParticleFade.cs`
- `Assets/Script/Particle/ParticleComponent/ParticleMovement.cs`
- `Assets/Script/Particle/ParticleFreezeAfterSeconds.cs` (new)
- `Assets/ParticleSystemOption.cs` (Assets root, not under `Script/`)
- `Assets/Script/Particle/esset/` (new) — colocated art, not scripts: materials `wind_1.mat`-`wind_4.mat` and sprites `레이어 1.png`-`레이어 4.png` ("layer" in Korean), for a stone/windmill dust-particle effect. Added alongside the "돌 디자인 수정" (stone design fix) commit that also redrew `Assets/Texture/artwork/stone/stone_1`-`15.png` and added `Assets/Texture/artwork/stone/wind_particle_esset.psd`. Follows the same pattern as `Assets/Texture/artwork/Propeller/RotateObject.cs` — art colocated with the script folder it's used from rather than under `Assets/Texture/`. No script currently references these materials by name; check the relevant particle prefab/scene GameObject for actual wiring before assuming they're in use.

`ParticleManager` implements a custom ScriptableObject-driven particle system with object pooling. `ParticleScriptable` assets are created through `Create > Custom > Particle Preset`.

Note: `ParticleFreezeAfterSeconds.cs` actually declares class `ParticleSimulationSoftStopper` (filename/class name mismatch — search by class name, not filename, if `ParticleFreezeAfterSeconds` doesn't resolve). It ramps a target `ParticleSystem`'s `simulationSpeed` down to near-zero over `slowDownSeconds` before `stopAfterSeconds` elapses, then optionally pauses it — a soft alternative to instantly stopping emission. Its source comments are mojibake-damaged (non-UTF8 Korean), consistent with other files in this codebase.

Important constraint:

- `ParticleManager.targetObject` and `ParticleManager.particles` must have matching counts and aligned indices. If counts differ, `Init()` fails and the manager destroys itself.

### Settings And UI

Important files:

- `Assets/Script/Settings/SettingsData.cs`
- `Assets/Script/Settings/SettingsManager.cs`
- `Assets/Script/Settings/SettingsUI.cs`
- `Assets/Script/Settings/StageUI.cs`
- `Assets/Script/Settings/StageUIInput.cs`
- `Assets/Script/Settings/KeyBindingSettings.cs`
- `Assets/Script/Settings/InGameSettingsBootstrap.cs`
- `Assets/Script/Settings/InGameSettingsMenuController.cs`
- `Assets/Script/Settings/InGameTitleReturnButton.cs`
- `Assets/Script/UI/TitleMenuController.cs`
- `Assets/Script/UI/TitleUIController.cs`
- `Assets/Script/UI/TitleFadeSceneLoader.cs`
- `Assets/Script/UI/PortalFadeSceneLoader.cs`
- `Assets/Script/UI/SceneFadeIn.cs`
- `Assets/Script/UI/MenuTextHover.cs`
- `Assets/Script/UI/TutorialAreaPrompt.cs`
- `Assets/Script/UI/CutsceneLetterboxUI.cs`
- `Assets/Script/UI/ResolutionArrowSelectorUI.cs`
- `Assets/Script/UI/ScreenModeBoxSelectorUI.cs`
- `Assets/Script/UI/SettingsMenuButtonAlpha.cs`

`SettingsManager` is a singleton and uses `DontDestroyOnLoad`. Settings are persisted through `PlayerPrefs`.

Currently applied setting behavior:

- Master volume is applied through `AudioListener.volume`.
- BGM/SFX values are saved and logged, but not routed to separate AudioMixer groups in the current code (note: `Assets/Audio/GameAudioMixer.mixer` now exists as a dedicated mixer asset — check whether it has been wired in before assuming this is still true).

Key binding:

- `KeyBindingSettings` is a static class that loads/saves `KeyCode` values from `PlayerPrefs` for MoveLeft, MoveRight, Jump, Aim, and Shoot actions.
- `KeyBindingAction` enum defines the five bindable actions.

In-game settings:

- `InGameSettingsBootstrap` uses `[RuntimeInitializeOnLoadMethod]` to auto-create an `InGameSettingsMenuController` when the `Forest` or `SeungHyun2_Restore` scene loads.
- `InGameSettingsMenuController` provides a pause-style settings overlay usable during gameplay.
- `InGameTitleReturnButton` handles returning to the title scene from within a gameplay scene.

Title flow:

- `TitleMenuController.NewGame()` loads `newGameSceneName`.
- `TitleFadeSceneLoader.StartNewGame()` fades screen/audio and loads `nextSceneName`, defaulting to `Forest`.
- `PortalFadeSceneLoader`: fades the screen and loads the next scene when the player enters a portal trigger.
- `MenuTextHover` controls hover/click alpha and selection behavior for TMP text.

Tutorial UI:

- `TutorialAreaPrompt`: trigger-based UI prompt. When the player enters the collider, fades a TMP message in/out with optional motion. Supports a follow-up message and can wait until `CoreActivationController.isActivated` is true before fading out.
- `CutsceneLetterboxUI`: animates top/bottom letterbox bars in and out for cutscene framing.
- `ResolutionArrowSelectorUI` / `ScreenModeBoxSelectorUI`: UI selectors for display resolution and screen mode in the settings menu.

### Audio

Important folders:

- `Assets/Audio/` — project AudioMixer asset (`GameAudioMixer.mixer`)
- `Assets/sounds/BGM/`
- `Assets/sounds/SFX/Bow/`, `Assets/sounds/SFX/step/`, `Assets/sounds/SFX/stone_shaker/`, `Assets/sounds/SFX/cloak/` (new), `Assets/sounds/SFX/MusicPuzzle/` (new)
- `Assets/sounds/ambient/`

Important scripts:

- `Assets/sounds/BGM/BGMFadeIn.cs`: fades an AudioSource to a target volume.
- `Assets/sounds/SFX/Bow/BowSFXRandomizer.cs`: randomizes bow pull/shoot/hit clips and pitch.
- `Assets/sounds/SteppeZoneTrigger.cs`: fades steppe BGM and ambience on trigger enter/exit.

Audio content by folder:

- `BGM/`: 9 tracks (`BlueSteppe_BGM`, `Forest2_BGM`, `Forest3_BGM`, `sky_temple_BGM`, `Steppe_BGM`, `temple_BGM`, `Temple3_BGM`, `title_BGM_1`, `title_BGM_2`).
- `SFX/Bow/`: bow pull/shoot/hit variants (`bow_pull_1/2`, `bow_shoot_1/2`, `bow_hit_1/2/3`).
- `SFX/step/`: `step_grass_1`-`6`, `step_stone_1`-`5` (footstep variants by surface).
- `SFX/stone_shaker/`: `stone_shaker_1` through `6` (with a `stone_shaker_2_1` variant).
- `SFX/cloak/` (new): `cloak_1` through `cloak_4.wav`. No script under `Assets/Script` currently references "cloak" — likely wired directly onto an AudioSource/animation event in a scene or prefab, or reserved for an unimplemented feature.
- `SFX/MusicPuzzle/` (new): `Note_0`-`Note_3.wav` (the four playable notes), `Sound1`-`Sound4.wav`, `fail.mp3` — consumed by `MusicPuzzleAreaController`/`HangingMusicPuzzleNoteObject`.
- `SFX/` root: `core.wav`, `windmill.wav`, `windmill_drum.wav`, `Chain_SFX_1/2.mp3` (new), `temple2_core.mp3` (new), plus one CC-licensed crumbling-wall SFX (`829103__squirrel_404__...`).
- `ambient/`: `forest_ambient.mp3`, `Forest_Bird.mp3`, `steppe_ambient.mp3`, `sky_temple_ambient.wav`, `Temple2_ambient.wav`.

This is a larger and more organized audio set than earlier notes suggested (footstep and stone-shaker SFX are split into dedicated per-surface subfolders, and the music puzzle/cloak systems each got their own SFX subfolder).

### Shader, Wind, Sky

Important files:

- `Assets/Script/Shader/SpriteTopWind.shader`
- `Assets/Script/Shader/ReedWindChain.cs`
- `Assets/Script/Shader/M_Reed_Wind.mat`
- `Assets/Texture/artwork/Leaf/*.shadergraph`
- `Assets/Script/Camera/PinLightBlend.shadergraph`
- `Assets/Script/Sky/SkyManager.cs`
- `Assets/Script/Sky/ZoneParticleActivator.cs`

`ReedWindChain` applies wind-like rotation to a root/mid/top bone chain. `SkyManager` manages sky/background objects based on the player position. `ZoneParticleActivator` (in `Sky/`) toggles ambient particle effects by zone.

## Texture And Art Layout

`Assets/Texture` contains most 2D art.

```text
Assets/Texture/
+-- artwork/            Environment/object/stage art
+-- Charactor/          Character sprites and animation frames
+-- effect/             Standalone VFX sprites (new: e.g. Ellipse 147.png)
+-- Leaf/               Leaf/wind materials
+-- material/           Shared materials
+-- Object/             Circle, windmill, pillar object sprites
+-- Sky/                Time-of-day sky images
+-- test/               Test images
+-- Title/              Title screen images
+-- UI/                 UI sprites and the CoreHintRing animator/animation (new)
```

Important `Assets/Texture/artwork` folders:

- `blue_steppe`
- `Branch`
- `core`
- `fog`
- `foliage`
- `grass` (contains `PrefabsReeds/` with 13 reed/grass prefabs)
- `Leaf`
- `light`
- `Mountain`
- `Propeller` (also holds `RotateObject.cs`, a loose rotation script)
- `Puzzle_esset_Wind` (new — sprites for the Wind/Rope/Pressure-platform puzzles: `PressurePlate_*`, `Windgate_*`, `Puzzle_5_pillar_*`/`Puzzle_6_temple`/`puzzle_10_*`/`puzzle_11_*`, `Temple_Pillar_1`-`5`, `chandelier_1`/`2`, `hanging_platform`, `pillar`, `rope_rock_middle`/`small`. Note the folder name itself is a typo for "asset" — that's the real, tracked path.)
- `Sky`
- `Sound_Puzzle` (sprites for the hanging music/note puzzle: body, chain links, active/base dots, propeller)
- `stone`
- `temple`
- `temple2`
- `temple2_stone`
- `temple_Stair`
- `tree`
- `tuya_arrow_sprite_pack`

The folder name `Charactor` appears to be misspelled, but it is the real path. Do not rename it casually because Unity asset references may depend on it.

`Assets/Texture/UI/` holds `CoreHintRing.controller` + `CoreHintRing_Pulse.anim` (the core-activation hint ring referenced from `CoreActivationController`) alongside UI sprites (`Ellipse 18.png`, `Group 10 (1).png`, `Rectangle 30/31/35.png`, `UI_Background.png`). `Assets/Texture/effect/` holds a single sprite (`Ellipse 147.png`).

`Assets/Texture/test/` has churned: the earlier phone-photo test images (`IMG_2050.PNG`-`IMG_2061.PNG`) were removed, replaced with pressure-plate/rope/line placeholder sprites (`PressurePlate_*`, `Rope.png`, `Line_1`-`3.png`, `steppe/`) — treat this folder as scratch/reference art, not final in-game assets (the finalized equivalents live under `artwork/Puzzle_esset_Wind/`).

## Prefabs And Assets

Shared prefabs live under `Assets/Prefabs/` (renamed from the singular `Prefab/`):

- `Assets/Prefabs/MissionArea.prefab`
- `Assets/Prefabs/Settings.prefab`
- `Assets/Prefabs/Player(-1~1).prefab`
- `Assets/Prefabs/UI/SettingsMenuPrefab.prefab`
- `Assets/Prefabs/Box (1).prefab` (new) — the `BoxObject`/`TY_Weight`-bearing crate used as `RopeRegenerator` payload and `PressureCorePlatform` weight. Reworked this revision — see Box, Pressure Plates section above.
- `Assets/Prefabs/Box_Middle.prefab` (new) — second box variant combining `BoxObject`+`ArrowBlocker` with `RunwayObject`+`TY_Weight`; see Box, Pressure Plates section above.
- `Assets/Prefabs/PressurePlatformCore.prefab` (new) — a configured `PressureCorePlatform`/`PressureTopRelay` pair-platform prefab.
- `Assets/Prefabs/Windgate.prefab` (new) — a Wind-based gate object built from `Puzzle_esset_Wind` art.

Other prefabs are stored near their systems:

- `Assets/Script/Arrow/Arrow.prefab`, `ArrowHitFX.prefab`, `ArrowTrajectoryPrefab.prefab`
- `Assets/Script/Object/Stone Pillar/StonePillar.prefab`
- `Assets/Script/Object/Wind/WindMill.prefab`
- `Assets/Texture/artwork/grass/PrefabsReeds/*.prefab` (13 reed/flame-grass prefabs)
- `Assets/Prefabs/Wind (4).prefab` — a configured `Object_Wind`/`Object_Wind_Particle` prefab (naming suggests a 4th variant/iteration; check its Inspector values rather than assuming defaults). Updated again in this revision alongside the `Object_Wind` blocking rework above.

Approximate asset counts:

- `.prefab`: 27 (up from 26; new: `Box_Middle.prefab`)
- `.asset`: 29
- `.mat`: 22 (up from 18; new: `wind_1.mat`-`wind_4.mat` under `Script/Particle/esset/`, see Particles section)
- `.shader`: 15 (2 gameplay shaders in `Script/Shader/` — `SpriteTopWind.shader` plus new `SpriteFlash.shader` — the remaining 13 are TextMesh Pro built-ins)
- `.shadergraph`: 3
- `.renderTexture`: 2 (`BaseRT.renderTexture`, `New Render Texture.renderTexture`)
- `.mixer`: 2 (`Assets/Audio/GameAudioMixer.mixer`, `Assets/NewAudioMixer.mixer`)

The MusicPuzzle system (see below) does not add new prefabs — its GameObjects (note objects, cores, chain links) appear to be built/wired directly in scenes and via the `[ContextMenu]` builder on `HangingMusicPuzzleNoteObject`, not shared prefabs.

## Editor Tools

`Assets/Editor/ReplaceSelectedWithPrefab.cs` is an EditorWindow tool. It belongs to editor-only code and is compiled into `Assembly-CSharp-Editor`, not the runtime assembly.

## Change Safety Notes

- Unity `.meta` files preserve GUIDs. Be careful when moving, deleting, or renaming assets.
- Do not edit `Library`, `Temp`, `obj`, or `.vs`.
- Runtime scripts generally have no namespace. New scripts should usually follow that local style unless a broader refactor is intended.
- Many fields are serialized in Inspector. Renaming serialized fields can break scene or prefab references.
- `IArrowHit` and `ICoreEvent` are the key puzzle/event contracts. `CoreActivationController` and the newer `CoreActivation` are both concrete implementations of both — they are separate classes, not a subclass relationship, so check which one a given core prefab actually uses. `IArrowPassThrough` is a separate, narrower contract for objects the arrow should fly through while still reacting (currently only `RopeSegment`) — it is checked first in `Arrow.OnTriggerEnter2D` and is mutually exclusive in effect with `IArrowHit` on the same collider. `IArrowKnockbackReceiver` (new) is a third, additive contract — checked alongside (not instead of) `IArrowHit` on the `IArrowHit` path, so a collider can implement both and get both effects (see `BoxObject`).
- `KeyBindingSettings` is a static class; it is not a MonoBehaviour and should not be added to a GameObject.
- `CameraMovement.Instance` and `SettingsManager.Instance` assume required scene objects exist.
- Some comments are mojibake. Trust code flow and Unity references over damaged comments.
- `PlayerTurnState` is not implemented.
- `SettingsManager.Update()` logs the master volume every frame.
- Several player states also log every frame. Console noise may be high during Play Mode.
- `Assets/Prefab/` was renamed to `Assets/Prefabs/` at some point after the last structure snapshot; if you find stale references or docs mentioning `Assets/Prefab/`, treat `Assets/Prefabs/` as authoritative.
- `Assets/Physics/` currently contains no assets — do not assume physics materials/settings live there yet.
- Two AudioMixer assets exist (`Assets/Audio/GameAudioMixer.mixer` and `Assets/NewAudioMixer.mixer`); confirm which one scene/audio scripts actually reference before editing mixer routing.
- `Assets/Script/Particle/ParticleFreezeAfterSeconds.cs` declares class `ParticleSimulationSoftStopper`, not `ParticleFreezeAfterSeconds` — grep by class name when searching for it.
- `MusicPuzzleCoreBridge` can wrap an existing `CoreActivationController` (subscribing to `onActivated`, toggling `activationLocked`); when editing either script, check whether the other is still consistent with its public API (`onActivated`, `activationLocked`, `FadeInActivateGlow()`).
- `Assets/Script/TestJumpForce.cs`, `Assets/Texture/artwork/Propeller/RotateObject.cs`, and now `Assets/Script/Object/Magnetic.cs` are loose debug/utility scripts living outside the normal `Script/<System>/` organization — treat as informal/leftover rather than part of the designed architecture.
- `CameraEndingAreaTrigger.cs` and `Magnetic.cs` are new scripts that are not yet attached to any GameObject in a tracked scene or prefab — confirm they are actually wired up in-editor before assuming they affect current gameplay.
- Prefer `PlayerController.SetInputLocked(bool)` over `LockPlayerInput(float time)` when the unlock condition is a runtime event (e.g. landing) rather than a fixed duration; both share the same `lockInputCoroutine` bookkeeping so calling one cancels a pending call to the other.
- `StonePillarManager` and `StoneCircleManager` now track each pillar's/circle's in-flight *target* position/rotation (`currentTargetPosition`, `currentTargetRotation`) rather than reading the live `transform`, and stop/replace the previous move coroutine before starting a new one — if you add similar step-move/rotate logic elsewhere, follow this pattern to avoid drift on rapid re-triggers.
- `Arrow.OnTriggerEnter2D` now checks `IArrowPassThrough` *before* `IArrowHit`. If you add a new arrow-reactive script, decide up front whether the arrow should stick (`IArrowHit`) or fly through while still reacting (`IArrowPassThrough`, e.g. `RopeSegment`) — implementing both on the same collider means the pass-through path always wins and `IArrowHit.OnHit()` never fires.
- `Object_Wind` and `Object_Wind_Particle` no longer derive push direction from the GameObject's Z rotation; they use the `WindDirection` enum field (via `Object_Wind.GetDirectionVector`) instead. If you find a wind object that seems to point the wrong way, check `windDirection` in the Inspector rather than the transform's rotation.
- `Assets/Scenes/Mechanism.unity` and `Assets/Scenes/InGameScene/Forest 1.unity` (renamed from `Forest 2.unity`) are not wired into `EditorBuildSettings.asset` at all (not even disabled) — treat both as editor-only sandbox/backup scenes, not shippable levels.
- `PressurePlate.cs` (simple on/off, `Object/`) and `PressureCorePlatform.cs` (weight-comparison seesaw, `CoreObjects/`) are two unrelated systems with similar names — don't assume a "pressure" object in a scene uses the weight/seesaw logic just because the word appears.
- `CoreActivation.cs` and `CoreActivationController.cs` are likewise two unrelated classes with overlapping purpose (both `IArrowHit`+`ICoreEvent`+`onActivated`) — grep the actual component list on a given core GameObject rather than assuming which one it uses.
- `BoxObject.OnHit()` is intentionally empty — its puzzle behavior comes entirely from `IArrowKnockbackReceiver.OnArrowKnockback()`, not from `OnHit()`. Don't add logic to `OnHit()` expecting it to run on arrow-stick; it already always sticks via `Arrow.cs`'s `IArrowHit` path regardless.
- `RopeRegenerator` does not modify `Rope`/`RopeSegment`; it only reads `Rope.IsCut`/`Rope.Segments` and calls `Rope.SetHangingTarget()` + `Rope.BuildRope()`. If you change `Rope`'s public surface (`IsCut`, `Segments`, `SetHangingTarget`), check `RopeRegenerator` for breakage.
- `Assets/Script/Object/Rope/RopeRegenerator.cs`'s glow effect depends on `Shader.Find("Custom/SpriteFlash")` succeeding at runtime — if `Assets/Script/Shader/SpriteFlash.shader` is renamed, stripped from a build, or its shader name string changed, `flashMaterial` silently stays null and `PlayGlowFade()` no-ops (no error, the flash just never appears).
- `Object_Wind.BlockPlayer()`'s push logic and `IsFallingFromAbove()`'s fall-through exemption are new and physics-sensitive (AABB overlap axis, previous-frame position estimate from `rb.velocity * Time.fixedDeltaTime`); if a wind-blocking bug appears, check these two methods and the `fallThroughExempt` set before assuming the older `IsBlocked()` boxcast is at fault.
- `Object_Wind_Particle`'s wind-axis particle velocity is now assigned each frame to a per-particle fixed target speed (not accumulated with `+=`) — if you add similar per-particle force logic elsewhere, prefer assignment over accumulation to avoid a runaway-speed bug.
- `Object_Wind_Particle.windPower`/`distanceFalloff` were removed and replaced with `windSpeedMin`/`windSpeedMax`/`speedStep` (explicit per-particle speed range) and `powerScale` (0-1 fade multiplier, renamed from `windPower`); `CoreObjectToggle.cs` was updated to fade `powerScale` instead. Existing scenes/prefabs still carry the old `windPower`/`distanceFalloff` serialized values, which Unity now silently ignores — their `Object_Wind_Particle` components will fall back to the new fields' script defaults until re-tuned in the Inspector. `Object_Wind.windPower`/`distanceFalloff` (the separate rigidbody-wind class) are unrelated and untouched.
- `Object_Wind_Particle`'s lifetime fade (`ColorOverLifetimeModule`) and blocked-particle fade-out both write alpha through a different pipeline than `SetParticlesAlpha()` (used by `CoreObjectToggle`'s whole-system fade) — final visible alpha is the product of both, so changing one doesn't fully control what's on screen.
- `BoxObject.boxCollider2D` must be assigned in the Inspector for the new player-collision-ignore behavior to take effect; `IgnorePlayerCollision()` silently no-ops (no error) if it's left empty.

## Recommended AI Inspection Order

1. Check `ProjectSettings/ProjectVersion.txt`.
2. Check `Packages/manifest.json`.
3. Check `ProjectSettings/EditorBuildSettings.asset`.
4. Read `Assets/Script/Player/PlayerController.cs`.
5. Read `Assets/Script/Player/PlayerState/PlayerState.cs`.
6. Read `Assets/Script/Player/Attack/Arrow.cs`, `Assets/Script/Arrow/IArrowHit.cs`, and `Assets/Script/Arrow/IArrowPassThrough.cs`.
7. For the requested area, inspect one of `Camera`, `Object`, `Particle`, `Settings`, `UI`, or `sounds`.
8. For scene/prefab-dependent work, verify Unity Inspector references before assuming a serialized field is unused.

## Feature Entry Points

- Player movement/jump/fall: `PlayerState.cs`, `PlayerController.cs`
- Player animation: `Script/Player/Animation/PlayerAnimation.controller`, `PlayerUpperAnimation.controller`
- Aiming and arrow shooting: `PlayerAttackState`, `PlayerController.ShootArrow`, `Arrow.cs`
- Arrow-hit puzzles: `IArrowHit`, `CoreActivationController`, `CoreActivation`, `CoreObject*`, `StoneBridge`, `StoneCircleManager`, `StonePillarManager`
- Arrow blocking (no hit logic, just catches arrows): `ArrowBlocker.cs`
- Arrow pass-through (react without sticking): `IArrowPassThrough.cs`, `RopeSegment.cs`
- Arrow knockback (physical recoil, additive to `IArrowHit`): `IArrowKnockbackReceiver.cs`, `BoxObject.cs` (`Box (1).prefab`, `Box_Middle.prefab`)
- Cuttable rope + hanging payload regeneration: `Rope.cs`, `RopeSegment.cs`, `RopeRegenerator.cs`, `DisappearMethod.cs`
- Music/sound note puzzle: `MusicPuzzleAreaController.cs`, `HangingMusicPuzzleNoteObject.cs`, `MusicPuzzleCoreBridge.cs`, `MusicPuzzlePropellerHitProxy.cs`, `MusicPuzzleAreaTriggerBridge.cs`
- Directional wind (rigidbody + particle) with distance falloff and player-blocking push: `Object_Wind.cs`, `Object_Wind_Particle.cs` (shared `WindDirection` enum)
- Particle-affecting wind: `Object_Wind_Particle.cs`
- Particle soft-stop: `ParticleFreezeAfterSeconds.cs` (class `ParticleSimulationSoftStopper`)
- Simple on/off pressure plate: `PressurePlate.cs`
- Weight-comparison seesaw platform: `PressureCorePlatform.cs`, `PressureTopRelay.cs`, `TY_Weight.cs`
- Core-driven toggle/rise/wind-fade of level objects: `CoreObjectToggle.cs`, `RiseObject.cs`
- Camera follow/staging: `CameraMovement.cs`, `MissionAreaCamera.cs`, `FakeZZoomManager.cs`
- Camera restore/ending: `CameraRestoreAreaTrigger.cs`, `CameraEndingAreaTrigger.cs`
- Rider/carry-along movement: `Magnetic.cs`
- Drop-through platform: `RunwayObject.cs` (also combined with `BoxObject`/`TY_Weight` in `Box_Middle.prefab`)
- Parallax/background depth: `ParallaxManager.cs`, `ParallaxImage.cs`, `DistanceParallaxManager.cs`
- Particles: `ParticleManager.cs`, `ParticleScriptable.cs`
- Settings persistence: `SettingsData.cs`, `SettingsManager.cs`
- Title/fade UI: `TitleMenuController.cs`, `TitleFadeSceneLoader.cs`, `SceneFadeIn.cs`
- Tutorial UI: `TutorialAreaPrompt.cs`, `CutsceneLetterboxUI.cs`
- Forest intro sequence: `ForestIntroController.cs`
- In-game settings: `InGameSettingsMenuController.cs`, `InGameSettingsBootstrap.cs`
- Key bindings: `KeyBindingSettings.cs`
- Breakable platform: `BreakableFragmentPlatformEvent.cs`
- Player visual effects: `PlayerSilhouetteController.cs`, `PlayerBloomAreaTrigger.cs`
- Audio: `BowSFXRandomizer.cs`, `BGMFadeIn.cs`, `SteppeZoneTrigger.cs`, `Assets/Audio/GameAudioMixer.mixer`

## Git Status Note

Before creating the original version of this document, `.codex/` was already untracked. It appears to be local Codex configuration and is unrelated to project source structure.

This revision (scanned at commit `9f46b5c` "Merge pull request #57 from backsani/main", working tree clean) was generated by diffing against the previous snapshot's commit (`56399fc` "1차 수정") across the 4 intervening commits (`f0f59e5`, `ff6f88a`, `147c30b`, `9f46b5c`) and re-scanning the changed script/prefab/scene/texture areas. Unity version, package dependencies, and enabled build scenes (`EditorBuildSettings.asset`) are unchanged from the previous snapshot. Summary of what changed:

- **Box rework + new `Box_Middle` variant.** `BoxObject.Awake()` now calls new `IgnorePlayerCollision()`, which — if a `boxCollider2D` field is wired up — finds the scene's `PlayerController` and calls `Physics2D.IgnoreCollision` so a knocked-back/pushed box can't shove the player. `Assets/Prefabs/Box (1).prefab` had its old separate trigger collider removed in favor of reusing its solid `BoxCollider2D` as `boxCollider2D`, plus a redesigned sprite and a scale increase (`2` → `~4.13`). New `Assets/Prefabs/Box_Middle.prefab` layers `RunwayObject`+`TY_Weight` on top of the same `BoxObject`+`ArrowBlocker` combo — a droppable-platform, pressure-weighted box.
- **`Object_Wind_Particle` fixes and new fade behavior.** The wind-axis particle velocity is now assigned each frame to a distance-scaled target speed (dot-product decomposition, preserving the perpendicular/gravity component) instead of accumulated with `+=` — fixes particles endlessly gaining speed the longer/farther they were pushed. New `fadeOutOverLifetime`/`fadeStartLifetimePercent` drive a `ColorOverLifetimeModule` alpha gradient so particles fade out near end-of-life instead of popping. New `blockedFadeOutDuration` makes particles blocked by `blockingLayer` fade out over time (tracked per-particle in `blockedFadeStates`) instead of disappearing instantly.
- **Stone art redesign.** `Assets/Texture/artwork/stone/stone_1`-`15.png` were redrawn ("돌 디자인 수정"), and a new `wind_particle_esset.psd` plus a colocated `Assets/Script/Particle/esset/` folder (materials `wind_1`-`4.mat` + sprites `레이어 1`-`4.png`) were added for an associated dust/wind particle effect — not yet referenced by any script by name; check the relevant prefab/scene wiring before assuming it's live.
- **Scene renames/edits.** `Assets/Scenes/InGameScene/Forest 2.unity` was renamed to `Forest 1.unity` (git-tracked rename, still unlisted in `EditorBuildSettings.asset` like `Mechanism.unity`). `Forest.unity` and `Mechanism.unity` both received further scene-data edits (forest level placement, mechanism tuning per commit `f0f59e5`); `PressurePlatformCore.prefab` and `Wind (4).prefab` also got minor Inspector-value tuning (transform positions, particle shape scale, layer masks, `distanceFalloff`). None of this is reflected line-by-line in this document.
- No changes this revision to `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, or `ProjectSettings/EditorBuildSettings.asset`.

<details>
<summary>Older note (commit `56399fc`, for historical context)</summary>

This revision (scanned at commit `56399fc` "1차 수정", with local uncommitted edits to `Assets/Scenes/InGameScene/Forest.unity` and `Assets/Scenes/Mechanism.unity` present on top of that commit) was generated by diffing against the previous snapshot's commit (`077724c` "바람이 오브젝트에 가려지는 기능 추가, 발판 구현") across the 25 intervening commits and re-scanning the changed script/prefab/texture areas. Unity version, package dependencies, and enabled build scenes are unchanged from the previous snapshot. Summary of what changed:

- **New: Box + arrow knockback.** `Assets/Script/Arrow/IArrowKnockbackReceiver.cs` is a new interface, checked in `Arrow.OnTriggerEnter2D` *in addition to* (not instead of) `IArrowHit` on the same hit path. `BoxObject.cs` (`Object/`) is the first implementer — empty `IArrowHit.OnHit()`, and `OnArrowKnockback()` applies a horizontal `Rigidbody2D.AddForce` impulse. New prefab `Assets/Prefabs/Box (1).prefab`.
- **New: weight-comparison seesaw platforms.** `Assets/Script/Object/CoreObjects/PressureCorePlatform.cs` + `PressureTopRelay.cs` (child-to-parent collision relay) + `Assets/Script/Utils/TY_Weight.cs` (per-object weight value). Paired platforms (`connectedPlatform`) compare summed contact weight and move the heavier one down / lighter one up at a shared speed, with hysteresis on equal weight. Distinct from the pre-existing simple on/off `PressurePlate.cs`, which this snapshot also documents in-place for the first time (it existed since `077724c` but was missing from the prior doc revision). New prefab `Assets/Prefabs/PressurePlatformCore.prefab`, new art `Assets/Texture/artwork/Puzzle_esset_Wind/`.
- **New: core-driven toggle/rise helpers.** `CoreObjectToggle.cs` (`CoreObjects/`) subscribes to one or more `CoreActivationController.onActivated` and, per target, either calls a `RiseObject.Rise()`, wind-fades an `Object_Wind`/`Object_Wind_Particle` child in/out, or does a plain `SetActive` toggle. `RiseObject.cs` (`CoreObjects/`) is a new single-object rise-with-shake-and-particles animation, with an optional round-trip return.
- **New: second core-activation implementation.** `CoreActivation.cs` (`CoreObjects/`) is a self-contained `IArrowHit`+`ICoreEvent` cutscene core (owns its own flash/glow renderer fades, particles, audio, player lock) — a parallel, unrelated implementation to the pre-existing `CoreActivationController.cs`. Check which one a given core prefab uses.
- **New: rope hanging objects + regeneration.** `Rope.cs` gained a `RopeHangingAttachment[]` array so objects (typically a `BoxObject`) can hang from a specific rope segment via a `HingeJoint2D`, plus `Segments`/`IsCut` accessors and `SetHangingTarget()` for external control. New `RopeRegenerator.cs` (`Rope/`) watches `Rope.IsCut`, collapses the severed segments with a staggered fade, spawns a fresh hanging box (destroying the previously-fallen one, via `DisappearMethod.cs` if present), rebuilds the rope, and plays a white flash-fade over the regenerated pieces using new shader `Assets/Script/Shader/SpriteFlash.shader`. `Rope.cs` also gained `segmentSpriteScale` (moved the `SpriteRenderer` onto a child `Visual` object) and `segmentOrderInLayer`.
- **Wind blocking rework.** `Object_Wind.BlockPlayer()` now pushes the player out along the shallower-overlapped AABB axis at a capped `blockPushSpeed`, with `blockBounciness` controlling velocity rebound, instead of just refusing entry. New `ignoredLayer` mask and an `IsFallingFromAbove()` exemption (skipped for `WindDirection.Up`) so a player falling in from above isn't pushed/blocked until fully exiting. `IsBlocked()`'s boxcast origin moved from `transform.position` to the actual collider bounds center, and now shortens the cast by the target's own half-extent to avoid false-positive blocks from the target's own floor/wall.
- **Small fixes:** `RunwayObject` no longer re-enables its collider while the player is still standing in the detection trigger (`playerInsideDetection` guard). `PlayerController`'s footstep "is moving" check now reads input axis instead of Rigidbody2D velocity (so external forces like wind/knockback no longer trigger footsteps). `PlayerFallState`'s ground raycast now discards trigger-collider hits.
- New scene `Assets/Scenes/InGameScene/Forest 2.unity` (unlisted in build settings, like `Mechanism.unity`) and new prefab `Assets/Prefabs/Windgate.prefab`; `Forest.unity` and `Mechanism.unity` both received further large scene-data edits (committed and, as of this snapshot, also uncommitted), not reflected line-by-line in this document.

<details>
<summary>Older note (commit `227c17f`, for historical context)</summary>

This revision (as of commit `227c17f` "WindObject 개선 및 밧줄 구현", merged to `main` as `a4d2870`) was generated by diffing against the previous snapshot's commit (`9d27468` "각종 오류 개선(7.16)") and re-scanning the changed areas. **Note:** the previous snapshot text was itself never actually updated in commit `227c17f` (that commit's `PROJECT_STRUCTURE.md` edit only caught the doc up to the `9d27468` code state, not its own Rope/Wind changes) — so this revision covers two generations of change at once: everything listed in the older nested note below, plus the new Wind/Rope work from `227c17f`:

- **New: cuttable rope system.** `Assets/Script/Object/Rope/Rope.cs` procedurally builds a `HingeJoint2D` chain of `RopeSegment`s (editor `[ContextMenu]` `Build Rope`/`Clear Rope`). `RopeSegment.cs` implements the new `IArrowPassThrough` interface — an arrow hitting it triggers `Cut()` (destroys that segment's joint, dropping everything downstream) but the arrow keeps flying instead of sticking.
- **New: `IArrowPassThrough` interface** (`Assets/Script/Arrow/IArrowPassThrough.cs`). `Arrow.OnTriggerEnter2D` now checks this *before* `IArrowHit` — objects implementing it get notified (`OnArrowPass`) but do not stop or embed the arrow.
- **Wind rework.** `Object_Wind.cs` and `Object_Wind_Particle.cs` both switched from rotation-derived push direction to an explicit `WindDirection` enum (8-way, via `Object_Wind.GetDirectionVector`), and both gained a `distanceFalloff` setting that weakens force with distance from the wind object. `Object_Wind_Particle` also gained an optional "stretch by speed" particle-renderer mode (`stretchBySpeed`/`stretchLengthScale`/`stretchVelocityScale`) so particles visually elongate based on push velocity.
- New prefab `Assets/Prefabs/Wind (4).prefab` and new (build-settings-unlisted) scene `Assets/Scenes/Mechanism.unity`, likely a sandbox for testing the above.
- `Assets/Scenes/InGameScene/Forest.unity` received another large scene-data edit in this range (level content/wiring for the new mechanisms), not reflected line-by-line in this document.

Unity version, enabled build scenes (aside from the new unlisted `Mechanism.unity`), and package dependencies are unchanged from the previous snapshot.

<details>
<summary>Even older note (commit `9d27468`, for historical context)</summary>

This revision (as of commit `9d27468` "각종 오류 개선(7.16)") was generated by diffing against the previous snapshot's commit (`5697f20` "맵디자인") and re-scanning `Assets/Script`; see the "Change Safety Notes" and inline notes above for what has moved or been added since. This update spans three bugfix commits (`69e82fe`, `34dd7cb`, `9d27468`, all "각종 오류 개선" / "various error fixes") and is mostly behavior fixes rather than new systems:

- New scripts: `Assets/Script/Camera/CameraEndingAreaTrigger.cs` and `Assets/Script/Object/Magnetic.cs` — both exist but are not yet wired into any tracked scene/prefab.
- `CameraRestoreAreaTrigger`'s Y-target/finalize logic was refactored into `protected virtual` methods to support `CameraEndingAreaTrigger` overriding them for a fixed ending-camera height.
- `PlayerController` gained `SetInputLocked(bool)` alongside the existing timer-based `LockPlayerInput(float)`, and `PreventSlide` now also freezes the player during `attackState`.
- `PlayerJumpState`/`PlayerFallState` ground checks now also consult `controller.isGround` directly.
- `BreakableFragmentPlatformEvent` switched from a fixed-duration input lock to `SetInputLocked` plus a landing-detection coroutine (`UnlockInputAfterLandingRoutine`).
- `StonePillarManager` and `StoneCircleManager` both got a drift fix: they now track in-flight target position/rotation and stop/replace the previous move coroutine instead of re-reading a possibly-still-animating `transform`.
- `MusicPuzzleAreaController`/`MusicPuzzleCoreBridge` now lock both puzzle cores while a note sequence is playing (`SetPuzzleCoresLocked` / `IsSequenceRunning`), and answer playback fires a dot-sparkle particle effect (`PlayAnswerNoteSequence`, `HangingMusicPuzzleNoteObject.GetActiveDotTransform()`).
- `Assets/Scenes/InGameScene/Forest.unity` and `SeungHyun2_Restore.unity` both received large scene-data edits in this range (level content/wiring), not reflected line-by-line in this document.

Unity version, enabled build scenes, and package dependencies are unchanged from the previous snapshot.

</details>

</details>

</details>
