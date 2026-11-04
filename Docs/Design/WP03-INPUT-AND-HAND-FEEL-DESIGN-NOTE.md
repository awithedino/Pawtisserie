# WP03 — Input and hand feel

Date: 2026-11-04. Status: **implementation built; owner reports the Play check looks good. Real-device safe-area and touch checks remain unverified.** Package 3 of [`CORE-LOOP-PLAN.md`](CORE-LOOP-PLAN.md); builds on the WP02 core loop.

## 1. Goal

Play `CoreGameplay` with mouse **and touch**: drag a card from the hand onto the tray, see its affected tiles while it is above the board, and release to drop the move. The card and preview follow the pointer without an added vertical offset. The selected-card touch-and-release interaction is also available. Cancel by releasing outside or with Escape. During a combo cards stay colorful and selectable for inspection, but cannot be dragged or played until the board settles. The hand is a real fan that lifts, straightens and deals in. The cat's move is visible as a simple marker on the real timings. The layout fits any portrait phone inside the safe area.

Not in WP03: booster bar and revive buttons (WP04), tutorial restrictions and coach (WP05; the input code only leaves a hook for them), native haptics, audio, final art, the parity table and profiling (WP06).

## 2. Starting point (checked 2026-11-04)

Verified in files:

- `GameController.Update` polls `Input.mousePosition` / `GetMouseButtonDown(0)` and skips the board when `EventSystem.IsPointerOverGameObject()`. Cards are uGUI `Button`s (`CardView.onClick`). There is no touch or drag path.
- `AimState` (pure) handles select / cancel / hover / tap-same-tile / mouse click and keeps the aim when the card changes. Commands are accepted only when the sim is settled and playing (approved WP01 policy); the hand is disabled during a combo.
- Cards are five `Card` prefab instances at fixed positions in `GameRoot/GameCanvas` (y −350, x −112…112, 52×88). The card's parts are direct children of its root, so there is no inner visual to lift or tilt separately from the layout slot.
- `FitCamera` fits width, or height (800) on screens shorter than 1:2, and switches the canvas scaler with it. No safe-area handling. Zone positions are authored on the prefab.
- `CardPlayed` carries the impact time as `EndTime`; landing = impact − `ImpactDelay` (0.10 s). Landing times come from `ViewConfig` (TAP 0.32, POUNCE 0.45, FLOP 0.36 s; SWIPE 0.32 s in `SimulationConfig`).
- DOTween (owner-imported) is installed under `Assets/Plugins/Demigiant` with `createASMDEF: 0`. Its core `DOTween.dll` is usable from `Paw.Runtime`; its UI/Sprite modules compile into `Assembly-CSharp-firstpass`, which an asmdef cannot reference.
- Build Settings point at the deleted `SampleScene`; `CoreGameplay` is opened directly (unchanged by WP03).

Not available in the session that wrote this note: the Unity MCP bridge (no Editor inspection, recompile, EditMode run or renders). Building WP03 needs it from step 4 on.

## 3. Prototype reference (read from the v12 source, verified in code)

| Behavior | Prototype |
| --- | --- |
| Card press | Becomes a drag candidate; release without moving toggles selection (tap selected card again = cancel) |
| Drag start | Pointer moves more than **10 px** (CSS px) from the press point. Selection and aim are cleared; a ghost of the card follows the pointer; the card in the hand dims |
| Touch lift | While dragging with touch the target cell is the one under **finger − one cell** (one cell = 50 logical units on screen); the ghost is drawn at the lifted point |
| Ghost over tray | Shrinks to 0.55 and 50 % opacity |
| Drag release | Over a cell: **drops at once** (mouse and touch). Elsewhere: cancel |
| Tray press, card selected | Mouse: drop at once. Touch: same cell as the aim → drop; otherwise aim there and capture the pointer |
| Slide | The captured touch moves the aim tile by tile (4 ms haptic per new tile); lifting the finger keeps the aim |
| Switch card | Tapping another card while aimed selects it and keeps the aim |
| Hover | Mouse hover previews only while a card is selected |
| Tray press, no card | The cell wobbles; a one-time toast ("Pick one of Butter's move cards first") |
| Escape | Clears selection and aim |
| Fan | Step = min(card width + 6, available / (n − 1)); for slot offset m = i − (n − 1)/2: dip 2.6·m² px, tilt 4.5°·m, later cards on top |
| Selected | Lifts 22 px, straightens to 0°, drawn on top, gold outline. Mouse hover lifts 8 px |
| Layout motion | 0.22 s ease with a small overshoot (`cubic-bezier(.2, 1.25, .4, 1)`) |
| Deal in | 0.42 s from (−110, +24) px, −18°, scale 0.6, transparent, overshoot ease (`cubic-bezier(.2, 1.3, .5, 1)`) |
| Moves | Paw falls 130 u at 2600 u/s²; POUNCE cat falls from 120 u above the tray top; FLOP cat falls 190 u at 3000 u/s²; tail sweeps the row in 0.32 s; impact 0.10 s after landing; marker bounces off and fades over 0.5 s |
| Haptics | select 6 ms, drag start 8, aim move 4, drop 15, explosion 20 (TAP) / 35 ms |
| Reduced motion | No transitions or deal/glow animations |

This table describes the prototype, not the chosen touch interaction. The owner chose release-to-play instead of touch tap-aim-tap. The prototype also allows dropping a card while a combo runs; the Unity core does not (approved WP01 policy). See decisions 4–5.

## 4. Design

### 4.1 Pure input state (`Paw.Presentation`, tested in SimHarness)

`PointerGesture` owns the existing `AimState` and turns pointer facts into intents. Inputs carry a pointer id, a screen position, whether it is touch, and the cell under the pointer if any:

- `CardDown(index)`, `CardMove`, `CardUp`; `BoardDown(cell)`, `BoardMove(cell)`, `BoardUp`; `Hover(cell)` (mouse only); `Cancel` (Escape, focus loss, panel shown, level reload).
- Outputs: `SelectedIndex`, `AimTarget`, `Drag` (card index, ghost position, hovered cell, over-tray flag), and one-shot intents: `Drop(index, cell)`, `Wobble(cell)`, haptic cues (`Select`, `DragStart`, `AimMove`, `Drop`).
- One active pointer owns a gesture; a second finger is ignored until the first lifts.
- With a selected card, a touch down on the tray starts aiming; movement updates the target, and release on a valid tile plays once. Release outside cancels that placement. Both this gesture and card dragging use the touched tile.
- During a combo, allow card selection for inspection but do not start a card drag, aim on the board, or play a card. Do not queue a play for later; once settled, normal interactions resume.
- The drag threshold is an input in screen pixels, so the state machine has no Unity types and no screen math.
- A `Filter` hook (allowed card kinds, allowed cells, "first tap drops") is left empty for WP05's tutorial.

### 4.2 Runtime adapter (`Paw.Runtime`)

- **All pointers go through the uGUI `EventSystem`** (`StandaloneInputModule` already handles touch and mouse without double events). Cards implement `IPointerDownHandler`, `IDragHandler` and `IPointerUpHandler`, forwarding to the gesture. The tray gets an invisible, authored raycast surface on the canvas (`TrayHitArea`, an alpha-0 `Image` covering the largest tray), so board presses and drags arrive the same way; screen → world → `BoardMapping.TryCell` stays the hit test. The current `Input.mousePosition` polling is removed.
- Drag threshold: `ViewConfig.dragThresholdDp` (10) × `Screen.dpi` / 160, falling back to 160 dpi when Unity reports 0. Touch dragging uses the finger position directly, with no vertical lift.
- `GameController` keeps command gating and the preview cache; it now reads the gesture's target (aim, or drag hover) for the preview and executes `Drop` intents through `TryPlay`.
- While a dragged card's target is over a valid board tile, show the full move preview, including the affected tiles and destination badges; update it as the target moves. Hide it when the target leaves the board. On valid release, drop once at the last target; the simulator resolves the displayed move at its normal impact time. A release outside cancels without playing.

### 4.3 Hand (`HandView`)

- New `HandView` on the card row owns the five card views and lays them out as the prototype fan: step, dip, tilt, selected lift and straighten, mouse hover lift, z-order. Every number is a serialized field on `HandView` (prefab, with tooltips). Only the fan positions are computed, because they depend on 1–5 cards and cannot be authored per slot.
- A selected card is bumped up slightly while it remains in the hand, as a selection indicator. This hand-only effect is separate from dragging: the dragged card and target follow the pointer without a vertical offset.
- The `Card` prefab gets an inner `Visual` child holding its parts: `HandView` moves the card root to its slot; lift, tilt, the deal-in and the drag ghost move only `Visual`, so layout and animation never fight.
- Held cards have a Balatro-inspired, velocity-responsive visual tilt that settles smoothly when movement stops. Aim and release still use the exact pointer position, with no vertical offset. Tilt limit, response and settling time are adjustable on `CardView`.
- Deal-in starts from the deck pile anchor (authored) on each `CardDealt`; a played card leaves the fan at once (the cat marker takes over visually).
- Tweens use DOTween core (`DOLocalMove`, `DOLocalRotate`, `DOScale`, `DOTween.To` for alpha through a `CanvasGroup`), killed in `OnDisable` and on level reload. They are visual only and never feed back into the sim.

### 4.4 Cat move markers (`CatView`)

One pooled marker per move, driven by `CardPlayed`: landing time = `EndTime` − `ImpactDelay`, fall with gravity-style easing from the authored start offset so it touches down exactly at landing, squash, then bounce off and fade over 0.5 s. Shapes: TAP paw (circle on a capsule arm) onto the target cell; SWIPE tail (capsule) sweeping the target row over `SweepLanding`; POUNCE cat (large circle) down the column from above the tray; FLOP cat (larger circle) onto the 3×3. Sizes, start offsets, squash and fade on the `CatView` prefab. The timing comes from the sim config, so the visual landing and the sim impact cannot drift.

### 4.5 Layout and safe area

- HUD children move under a `SafeArea` rect sized to `Screen.safeArea` each time it changes; the world camera shifts so the board is centered on the same safe rect. Fit-to-width / fit-to-height stays as built in WP02.
- Authored zone spacing is kept as the owner sets it. GDD §11.2's growth of spare height (30 % to the queue up to +16, the rest to the four gaps) is **not** implemented by default (decision 9).
- Renders at 360×640, 400×800 and 430×932 with 3×3 and 7×7 trays, plus one with a simulated notch safe area.

### 4.6 Haptics and reduced motion

- `IHapticCues` receives the GDD §10 cues; WP03 ships a no-op implementation (optionally logging in the Editor). Native haptics are phase 3.
- `ViewConfig.reducedMotion` (default off): cards snap without tweens, no deal flight, markers fade without falling. A settings screen is later work.

## 5. Files and Editor work

| Type | Paths | Editor? |
| --- | --- | --- |
| Presentation | `Assets/Scripts/Presentation/PointerGesture.cs` (+ small value types) | No |
| Runtime | `Runtime/Input/{CardPointerHandler,TrayHitArea}.cs`, `Runtime/Views/{HandView,CatView,CatMarker}.cs`, `Runtime/Flow/{SafeAreaFitter,IHapticCues,NullHapticCues}.cs`; changes to `GameController`, `CardView`, `ViewConfig` | Recompile |
| Tests | `Assets/Tests/EditMode/PointerGestureTests.cs` (+ a fan-layout math test if the math is kept pure) | EditMode run |
| Prefabs | `Card` (add `Visual` child, move parts under it), `GameRoot` (HandView, TrayHitArea, SafeArea rect, deck anchor, CatView), new `CatMarker` | Through MCP / Prefab Mode; read current values first |
| Assets | Placeholder paw/tail sprites only if the existing circle/capsule are not enough | Editor |
| Tools | `Tools/CompileCheck/CompileCheck.csproj`: add a reference to `Assets/Plugins/Demigiant/DOTween/DOTween.dll` | No |
| Scene | none expected (the `GameRoot` instance picks up prefab changes) | — |
| Docs | this note, `PROJECT.md`, `TECHNICAL-KNOWLEDGE.md`, `AGENTS.md` (test count, assemblies) | No |

No package, Project Settings, Build Settings or DOTween setup changes.

## 6. Build order (after the go)

| Step | Work | Exit evidence |
| --- | --- | --- |
| 1 | `PointerGesture` + tests for every row of section 3 (tap/drag/slide/switch/cancel/second pointer/threshold edge) | SimHarness |
| 2 | Runtime input adapter, `GameController` rewiring, `ViewConfig` fields | CompileCheck; Unity recompile and Console |
| 3 | `HandView` and the `Card` prefab `Visual` split; deal-in | Edit-mode render of 1–5 card fans, selected and hovered |
| 4 | `CatView` markers | Renders at landing time for each card kind |
| 5 | Safe area and the three screen sizes | Render sheet (sizes × tray sizes × notch) |
| 6 | Final checks and docs | SimHarness, CompileCheck, Unity EditMode, Console against the known MCP warning, `git status` |

Steps 1–2 can be written without the Editor; steps 3–6 need the MCP bridge. No Play mode through any tool; if you are in Play, edits wait.

## 7. How it is checked

Automated: gesture tests in SimHarness and Unity EditMode; existing 55 tests stay green; CompileCheck clean; renders as listed (they show poses, not motion).

Owner Play checklist:

1. Mouse: hover preview, click drop, drag a card onto the tray (drops on release), drag and release outside (cancels), Escape.
2. Touch (Unity Remote 5 in the Editor, or a device build you make): drag a card over the board and verify the affected tiles and badges appear and follow the finger without an offset; release to play the previewed move, or release outside to cancel. Check the selected-card touch-and-release path and that a second finger is ignored.
3. Hand: fan with 5, 4, 3 cards; selected card lifts and straightens; deal-in after each play and after a gift card.
4. Cat markers land exactly when the pastries launch for TAP, SWIPE, POUNCE and FLOP.
5. Phone shapes with a notch: nothing under the notch or the home indicator; restart and re-enter Play leave no stuck drag ghost or tween.
6. During a combo, cards retain their colors and can be selected for inspection, but cannot be dragged out or played; after settling, they can be played normally.

## 8. Owner decisions

On 2026-11-04 the owner chose the revised touch and combo behavior in decisions 4–5 and kept the other proposals.

1. **Scope.** Proposal: as sections 1 and 4.
2. **Input routing.** Proposal: every pointer through the uGUI `EventSystem` (card drag handlers plus an invisible authored tray hit area), replacing the raw mouse polling. Alternative: poll `Input.touches` and the mouse directly.
3. **Units.** Drag threshold 10 dp (converted with the screen's dpi). On 2026-11-04 the owner removed the previously approved one-tile touch lift; the pointer position is used directly.
4. **Drop rules.** Drag a card from the hand over the board to preview affected tiles and destinations; the preview follows its target, and releasing on a valid tile plays that move once. Release outside cancels. Touch dragging uses the finger position without an offset. Alternatively, selecting a card and pressing a tile starts aim; slide if desired and release on a valid tile to play, without a second tap. This release-to-play behavior applies to mouse and touch, as confirmed by the owner on 2026-11-04. Pressing the tray without a selected card only wobbles the cell, without a toast. This selected-card touch gesture differs from the prototype; the GDD records the owner's decision.
5. **Input during a combo.** Cards remain colorful and selectable for inspection; card dragging, board aiming and playing are blocked until the board settles. No play is queued. This preserves the settled-only command policy and differs from the prototype's mid-combo drops.
6. **Tweening.** Proposal: DOTween core from `Paw.Runtime`, no DOTween setup or ASMDEF change. Alternative: hand-written tweens.
7. **Fan and feel numbers.** Use the prototype values in section 3, converted from CSS px to canvas units at the 400-wide reference, as serialized fields on `HandView`; the card keeps its current 52×88 size. The owner confirmed that a selected card should bump up as an indicator while in the hand; this does not add an offset while dragging.
8. **Cat markers.** Proposal: placeholder shapes and timings as in 4.4.
9. **Spare height.** Proposal: keep the authored zone spacing and only center the board in the safe area; no computed gap growth. Alternative: implement GDD §11.2's distribution as offsets on authored zone groups.
10. **Haptics.** Proposal: cue hooks with a no-op implementation now; native haptics in phase 3.
11. **Reduced motion.** Proposal: a `ViewConfig` toggle, default off, until a settings screen exists.
12. **Touch testing.** Proposal: you test touch with Unity Remote 5 first; a device build only when you ask for one.

## 9. Risks

- `StandaloneInputModule` with legacy input on some Android devices reports touches as mouse too; the gesture ignores a second pointer, and the adapter ignores mouse events while a touch is active.
- The drag ghost is a moved `Visual` inside the hand canvas; it must stay above the board sprites (canvas order 100 already) and return to its slot on cancel, level reload and focus loss.
- `Screen.dpi` is 0 or wrong on some devices; the 160 dpi fallback is a guess (estimate).
- Moving the `Card` parts under a `Visual` child keeps their references only if done in Prefab Mode with references re-checked afterwards; the five instances in `GameRoot` must not keep stale overrides.
- DOTween tweens do not tick in edit mode, so renders will show end poses unless the render code completes or manually updates them.

## 10. Implementation checkpoint (2026-11-04)

- Added `PointerGesture` and five initial tests. Card and tray pointer events are wired through the EventSystem; `GameRoot/GameCanvas/TrayHitArea` is a transparent input surface created in the prefab. Dragging currently moves the card root as a temporary visual, not the planned separate ghost and inner `Visual` child.
- `SimHarness` and Unity EditMode each passed 60/60; `CompileCheck` reported zero warnings and errors. The Unity Console showed the known MCP WebSocket reconnect warning and no errors.
- No tool-driven Play was performed. The runtime pointer integration, including drag feel, touch, preview and restart/cancel behavior, needs the owner's Play check after WP03 is ready.
- On 2026-11-04, added an initial drag tilt on `CardView`: horizontal movement causes a damped rotation that settles when movement stops; it is visual only and preserves pointer-based aiming. It needs owner Play feedback. The temporary moving card root remains in use until the planned `Visual` split.
- The WP03 review fixes now use `PointerGesture` as the sole aim-state owner, keep selection after a refused drop, play mouse board clicks on release, and refresh a stationary mouse preview after settling. The existing 10 dp drag threshold is serialized in `ViewConfig.asset`. A selected card rises 22 units in the hand (adjustable on `CardView`); an isolated Editor prefab check verified selection, repeated render and restoration, and a two-card edit-mode render shows the selected pose. Feel and runtime interactions require owner Play verification.
- Still to build: fan, hover/selection motion, deal-in and final drag visual, cat markers, safe-area fitting, haptic hooks, reduced-motion setting, render checkpoints and full verification. The existing layout and cat visuals are not the WP03 design yet.

## 11. Build checkpoint (2026-11-04)

- Built `HandView` with a 1–5-card fan, DOTween layout, hover and selected poses, deal animation from the deck anchor, and the existing velocity-based drag tilt. `Card.prefab` has an inner `Visual` child so hand slots and animations do not fight. The existing hand anchor moved from y −350 to −335 to keep the fanned cards visible.
- Built pooled `CatMarker`/`CatView` placeholders for all four moves, driven from `CardPlayed` and its impact time; the marker landing squashes, exits and fades. Marker shapes, offsets and timing are serialized on the prefab. Native art remains future work.
- Added a stretched `SafeArea` with reference-sized `Content`; `SafeAreaLayout` fits the HUD and camera to the same rectangle. `ViewConfig.reducedMotion` defaults to false; reduced motion suppresses card tweens, drag tilt, marker travel/squash and empty-tile wobble. Haptic cues have a no-op implementation.
- Intentional presentation difference: dragging moves the card's inner visual from its slot instead of drawing a second dimmed copy in the hand. It follows the pointer directly; the owner asked to avoid greyed-out cards and vertical drag offsets.
- SimHarness and Unity EditMode: **69 passed / 0 failed**; CompileCheck: **0 errors / 0 warnings**. This includes the optional card/target filter hook for WP05. The Unity Console has the known MCP WebSocket reconnect warning. A separate edit-mode execution ran the Level 0 three-move simulation through the controller/views to a win; it is not a Play-mode test.
- Rendered Level 0 and Level 3 at 360×640, 400×800, 430×932 and 400×800 with simulated insets; rendered 1–5-card hands with selected/hover poses and four marker landing poses. Renders use an isolated preview scene and show static poses, not input, animation timing or device performance. The local HTML reference could not be opened for a side-by-side comparison because the browser access attempt timed out.
- **Still pending:** specific real-device touch and safe-area verification, and HTML side-by-side parity. The hierarchy migration was subsequently implemented; the owner's check of that migrated scene is pending.

**Owner Play feedback:** on 2026-11-04 the owner reported that WP03 looks good. No specific device or individual checklist results were provided, so real-device safe-area/touch and HTML parity remain unverified.
