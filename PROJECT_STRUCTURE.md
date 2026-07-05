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
+-- NewAudioMixer.mixer   Legacy/root-level mixer asset (see Audio/ for the current one)
+-- URP_*.asset          URP pipeline/renderer assets
+-- *.renderTexture      Render textures
```

Note: the `Assets/Prefab/` folder named in earlier notes has been renamed to `Assets/Prefabs/` (plural) and gained a `Prefabs/UI/` subfolder. `Assets/Physics/` is a new, currently empty folder. `Assets/Audio/` is new and holds `GameAudioMixer.mixer`; a second, likely legacy, `NewAudioMixer.mixer` still sits at the `Assets/` root.

## Main Script Layout

`Assets/Script` is the primary runtime code area.

```text
Assets/Script/
+-- Arrow/              Arrow interfaces, small utilities, and arrow prefabs/materials
+-- Camera/             Camera follow, zoom, parallax, trigger areas, title camera logic
+-- Object/             Puzzle and interactive objects
|   +-- CoreObjects/    Core activation, temple, bridge, floor movement, rising objects
|   +-- Stone Pillar/   Pillar and windmill objects
|   +-- StoneCircle/    Circle rotation, propeller, wind machine, passage looper
|   +-- StoneFloor/     Breakable platform events
|   +-- Wind/           Wind force objects
+-- Particle/           Custom particle/object-pool system
+-- Player/             Player controller, input, state machine, attack, animations
|   +-- Animation/       Player .anim clips and Animator controllers (new)
|   +-- Attack/          Arrow.cs
|   +-- PlayerState/     PlayerState.cs (all state classes live in this one file)
+-- Scene/              Scene-specific intro/cutscene controllers
+-- Settings/           Settings persistence, settings UI, key bindings, in-game settings
+-- Shader/             Shader helper scripts, shader/material assets
+-- Sky/                Sky/background manager, zone particle activator
+-- UI/                 Title, fade, menu UI, tutorial prompts
+-- Utils/              Shared interfaces, noise, generic Pair
```

Approximate C# file counts (90 total under `Assets/Script`):

- `Camera` (incl. `Parallax`, `DistanceParallax`): 20
- `Object` (incl. `CoreObjects`, `Stone Pillar`, `StoneCircle`, `StoneFloor`, `Wind`): 24
- `UI`: 11
- `Settings`: 9
- `Player` (incl. `PlayerState`, `Attack`): 7
- `Particle` (incl. `ParticleComponent`): 9
- `Utils`: 3
- `Arrow`: 2
- `Sky`: 2
- `Scene`: 1
- `Shader`: 1

Additional related script files outside `Assets/Script`:

- `Assets/ParticleSystemOption.cs` (Assets root)
- `Assets/sounds/BGM/BGMFadeIn.cs`, `Assets/sounds/SFX/Bow/BowSFXRandomizer.cs`, `Assets/sounds/SteppeZoneTrigger.cs`
- `Assets/Editor/ReplaceSelectedWithPrefab.cs`

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
- Footsteps depend on `isGround`, `isOnGrass`, horizontal Rigidbody2D velocity, and `grassFootsteps`.
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
- `Assets/Script/Arrow/DestroyAfterSeconds.cs`

`Arrow` is a Rigidbody2D projectile.

- `Launch(Vector2 dir, Transform shooter)` sets normalized velocity and orientation.
- Gravity is disabled at launch and restored after `flyTime`.
- `OnTriggerEnter2D` ignores the shooter and checks `other.TryGetComponent<IArrowHit>`.
- On a valid hit, it stops flight FX, plays hit SFX, spawns hit FX, calls `target.OnHit()`, and sticks the arrow to the target transform.
- Colliders without `IArrowHit` are currently ignored.

Most puzzle interactions are connected through the `IArrowHit.OnHit()` contract.

Arrow-related prefabs/materials live in `Assets/Script/Arrow/`:

- `Arrow.prefab`, `ArrowHitFX.prefab`, `ArrowTrajectoryPrefab.prefab` (new — likely a trajectory/aim-preview prefab)
- `M_Tuya_ArrowTrail.mat`, `M_Tuya_DustParticle.mat`

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
- `CameraRestoreAreaTrigger`: restores camera to default follow state when player re-enters a zone.
- `FallZoomCameraArea`: adjusts camera zoom during fall zones.
- `DemoEndFadeToTitle`: fades screen and loads the title scene when the player reaches the demo end.

### Puzzle And Interactive Objects

Important folders:

- `Assets/Script/Object/`
- `Assets/Script/Object/CoreObjects/`
- `Assets/Script/Object/Stone Pillar/`
- `Assets/Script/Object/StoneCircle/`
- `Assets/Script/Object/StoneFloor/`
- `Assets/Script/Object/Wind/`

Common contracts:

- `IArrowHit.OnHit()` receives arrow-hit events.
- `ICoreEvent.OnCoreEvent()` represents a broader puzzle/core activation event.
- Many effects are Coroutine-based movement, rotation, activation, and camera staging.

Key files:

- `BasicObject.cs`: helper for drawing/instantiating sprite objects.
- `RunwayObject.cs`: toggles a runway collider while the player is inside/staying on it.
- `SampleObject.cs`: minimal `IArrowHit` sample.
- `CoreActivationController.cs`: implements both `IArrowHit` and `ICoreEvent`. On arrow hit, fires a full cutscene sequence — letterbox, player lock via `PlayerCutsceneLocker2D`, camera focus, tutorial prompt, hint ring — then broadcasts the core activation event.
- `CoreObjectTemple.cs`: raises temple pieces and optionally moves the player with a selected piece.
- `CoreObjectMoveFloor.cs`: toggles wind objects, toggles propeller rotation, and moves floors between previous/next positions.
- `CoreCameraFocus2D.cs`: smoothly pans and zooms the camera to a focus point during core events.
- `CorePropellerDoorSequence.cs`: sequences a propeller spin → door open animation on core activation.
- `CoreTimedStoneGroupTrigger.cs`: activates a group of stone objects after a timed delay on core event.
- `RisingObjectController.cs`: moves a set of objects upward on activation.
- `TimedRisingObjectController.cs`: same as `RisingObjectController` but with configurable per-object delay.
- `StoneBridge.cs`: moves bridge pieces, raises core, and triggers camera movement/noise.
- `StonePillarManager.cs`: creates stone pillars and windmills; windmill hits move connected pillars by step.
- `WindMillObject.cs`: `IArrowHit` adapter that calls `StonePillarManager.PillarMove`.
- `StoneCircleManager.cs`: rotates connected circles for a trigger id.
- `CircleHitObject.cs`: `IArrowHit` adapter that calls `StoneCircleManager.RotateCircles`.
- `PropellerSpinner.cs`: spins a propeller object continuously or on activation.
- `RotatingPassageLooper.cs`: loops a passage object's rotation for ambient motion.
- `WindMachineActivationController.cs`: activates the wind machine sequence on core event.
- `PassThroughExitCameraZoom.cs`: adjusts camera zoom when the player exits a pass-through area.
- `Object_Wind.cs`: applies directional wind force to Rigidbody2D objects inside its trigger.
- `WindSystemManager.cs`: mostly empty placeholder at the time of writing.
- `BreakableFragmentPlatformEvent.cs` (`StoneFloor/`): on player contact, disables the platform collider and triggers a fall sequence via `PlayerController.OnFall()` after a configurable FixedUpdate delay.

When changing puzzles, check Inspector-serialized lists and scene/prefab references. Many connections depend on list index order.

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
- `Assets/ParticleSystemOption.cs` (Assets root, not under `Script/`)

`ParticleManager` implements a custom ScriptableObject-driven particle system with object pooling. `ParticleScriptable` assets are created through `Create > Custom > Particle Preset`.

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
- `Assets/sounds/SFX/Bow/`, `Assets/sounds/SFX/step/`, `Assets/sounds/SFX/stone_shaker/`
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
- `SFX/` root: `core.wav`, `windmill.wav`, `windmill_drum.wav`, plus one CC-licensed crumbling-wall SFX (`829103__squirrel_404__...`).
- `ambient/`: `forest_ambient.mp3`, `Forest_Bird.mp3`, `steppe_ambient.mp3`, `sky_temple_ambient.wav`, `Temple2_ambient.wav`.

This is a larger and more organized audio set than earlier notes suggested (footstep and stone-shaker SFX are now split into dedicated per-surface subfolders).

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
- `Propeller`
- `Sky`
- `stone`
- `temple`
- `temple2`
- `temple2_stone`
- `temple_Stair`
- `tree`
- `tuya_arrow_sprite_pack`

The folder name `Charactor` appears to be misspelled, but it is the real path. Do not rename it casually because Unity asset references may depend on it.

`Assets/Texture/UI/` is new since the last snapshot and holds `CoreHintRing.controller` + `CoreHintRing_Pulse.anim` (the core-activation hint ring referenced from `CoreActivationController`) alongside UI sprites (`Ellipse 18.png`, `Group 10 (1).png`, `Rectangle 30/31/35.png`, `UI_Background.png`). `Assets/Texture/effect/` is also new and currently holds a single sprite (`Ellipse 147.png`).

## Prefabs And Assets

Shared prefabs now live under `Assets/Prefabs/` (renamed from the singular `Prefab/`):

- `Assets/Prefabs/MissionArea.prefab`
- `Assets/Prefabs/Settings.prefab`
- `Assets/Prefabs/Player(-1~1).prefab` (new)
- `Assets/Prefabs/UI/SettingsMenuPrefab.prefab` (new subfolder)

Other prefabs are stored near their systems:

- `Assets/Script/Arrow/Arrow.prefab`, `ArrowHitFX.prefab`, `ArrowTrajectoryPrefab.prefab`
- `Assets/Script/Object/Stone Pillar/StonePillar.prefab`
- `Assets/Script/Object/Wind/WindMill.prefab`
- `Assets/Texture/artwork/grass/PrefabsReeds/*.prefab` (13 reed/flame-grass prefabs)

Approximate asset counts:

- `.prefab`: 22
- `.asset`: 29
- `.mat`: 15
- `.shader`: 14 (1 gameplay shader in `Script/Shader/`, the remaining 13 are TextMesh Pro built-ins)
- `.shadergraph`: 3
- `.renderTexture`: 2
- `.mixer`: 2 (`Assets/Audio/GameAudioMixer.mixer`, `Assets/NewAudioMixer.mixer`)

## Editor Tools

`Assets/Editor/ReplaceSelectedWithPrefab.cs` is an EditorWindow tool. It belongs to editor-only code and is compiled into `Assembly-CSharp-Editor`, not the runtime assembly.

## Change Safety Notes

- Unity `.meta` files preserve GUIDs. Be careful when moving, deleting, or renaming assets.
- Do not edit `Library`, `Temp`, `obj`, or `.vs`.
- Runtime scripts generally have no namespace. New scripts should usually follow that local style unless a broader refactor is intended.
- Many fields are serialized in Inspector. Renaming serialized fields can break scene or prefab references.
- `IArrowHit` and `ICoreEvent` are the key puzzle/event contracts. `CoreActivationController` is the primary concrete implementation of both.
- `KeyBindingSettings` is a static class; it is not a MonoBehaviour and should not be added to a GameObject.
- `CameraMovement.Instance` and `SettingsManager.Instance` assume required scene objects exist.
- Some comments are mojibake. Trust code flow and Unity references over damaged comments.
- `PlayerTurnState` is not implemented.
- `SettingsManager.Update()` logs the master volume every frame.
- Several player states also log every frame. Console noise may be high during Play Mode.
- `Assets/Prefab/` was renamed to `Assets/Prefabs/` at some point after the last structure snapshot; if you find stale references or docs mentioning `Assets/Prefab/`, treat `Assets/Prefabs/` as authoritative.
- `Assets/Physics/` currently contains no assets — do not assume physics materials/settings live there yet.
- Two AudioMixer assets exist (`Assets/Audio/GameAudioMixer.mixer` and `Assets/NewAudioMixer.mixer`); confirm which one scene/audio scripts actually reference before editing mixer routing.

## Recommended AI Inspection Order

1. Check `ProjectSettings/ProjectVersion.txt`.
2. Check `Packages/manifest.json`.
3. Check `ProjectSettings/EditorBuildSettings.asset`.
4. Read `Assets/Script/Player/PlayerController.cs`.
5. Read `Assets/Script/Player/PlayerState/PlayerState.cs`.
6. Read `Assets/Script/Player/Attack/Arrow.cs` and `Assets/Script/Arrow/IArrowHit.cs`.
7. For the requested area, inspect one of `Camera`, `Object`, `Particle`, `Settings`, `UI`, or `sounds`.
8. For scene/prefab-dependent work, verify Unity Inspector references before assuming a serialized field is unused.

## Feature Entry Points

- Player movement/jump/fall: `PlayerState.cs`, `PlayerController.cs`
- Player animation: `Script/Player/Animation/PlayerAnimation.controller`, `PlayerUpperAnimation.controller`
- Aiming and arrow shooting: `PlayerAttackState`, `PlayerController.ShootArrow`, `Arrow.cs`
- Arrow-hit puzzles: `IArrowHit`, `CoreActivationController`, `CoreObject*`, `StoneBridge`, `StoneCircleManager`, `StonePillarManager`
- Camera follow/staging: `CameraMovement.cs`, `MissionAreaCamera.cs`, `FakeZZoomManager.cs`
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

This revision was generated by re-scanning `Assets/`, `Packages/manifest.json`, and `ProjectSettings/` directly; see the "Change Safety Notes" and inline notes above for what has moved or been added since the previous snapshot.
