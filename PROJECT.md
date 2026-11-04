# PROJECT.md: Pawtisserie

Project reference: what the game is, how it is built, current state. Keep it true (AGENTS.md §1). Design source of truth: [`Docs/Design/GAMEPLAY-SPEC.md`](Docs/Design/GAMEPLAY-SPEC.md) (the GDD). How to work with the owner: [`Docs/OWNER-PREFERENCES.md`](Docs/OWNER-PREFERENCES.md).

## 1. What this is

A hybrid-casual sorting puzzle for iOS and Android phones (portrait, one hand), built to ship as a free-to-play game. A chubby head-baker cat plays **move cards** (TAP, SWIPE, POUNCE, FLOP) onto a baking tray. Every pastry the move hits flies into the box of its kind; pastries with no open box wait on a 5-plate display shelf, then a 2-slot cooling rack. Win = no pastries on tray, plates, rack or stash (remaining cans do not block it). Fail = overflow, rack not cleared at the end of a combo, or out of cards. Pitch: *pick a move, drop it, watch pastries fly into their boxes.*

Working title **Pawtisserie** (name and mascot name pending store and trademark search; "Butter" is a placeholder, "Brioche" suggested). Solo developer: Ari. The HTML prototype (v12) is the behavior reference.

## 2. Status (update every session)

| Area | State |
|---|---|
| **Current state (2026-11-04)** | WP02–WP04 implementation is built in `CoreGameplay`. The owner considers WP04 complete; HTML prototype artwork is now linked as temporary presentation and awaits the owner's Play/visual review. Specific real-device touch and safe-area checks remain unverified. |
| Unity / Editor | Unity `2022.3.62f3`, Built-in 2D, legacy Input Manager; DOTween (core only usable from asmdefs: `createASMDEF: 0`) and Toony Colors Pro imported by the owner. Unity MCP connected on 2026-11-04; refresh produced the known WebSocket warning. EditMode verification only; no tool-driven Play. |
| Docs | All 13 proposals in the revised [WP02 note](Docs/Design/WP02-PLAYABLE-PROTOTYPE-DESIGN-NOTE.md) approved 2026-11-03; implementation checkpoint recorded there. GDD R1–R5 clarifications updated with that approval. [Core-loop plan](Docs/Design/CORE-LOOP-PLAN.md) records the package split. `GAME-FLOW.md` remains a stub. [WP03 input and hand feel note](Docs/Design/WP03-INPUT-AND-HAND-FEEL-DESIGN-NOTE.md) records owner decisions and the input implementation checkpoint. The owner approved all 13 recommendations in the [WP04 boosters and revives note](Docs/Design/WP04-BOOSTERS-AND-REVIVES-DESIGN-NOTE.md); its build checkpoint and remaining Play checks are recorded there. |
| Code | `Assets/Scripts/Sim`: model and validation, shapes, `Simulator`, resolvers, storage/boxes/outcomes, boosters/revives and `Predictor`. Existing constants and IDs preserved; no engine references. |
| Tests | 74 cases including WP03 gesture, fan and safe-area tests and WP04 wallet, timing and grant coverage; all pass in SimHarness and Unity EditMode (MCP job `0cf47aeaf42a413c8d22467519ae2b53`, 0 failed, 0 skipped, 2026-11-04). |
| Fast checks | SimHarness: 74 passed, 0 failed. CompileCheck: 0 errors, 0 warnings (2026-11-04). |
| Not done on purpose | Boot/Splash/MainMenu/Game scenes, disk save, lives, coins, audio, real ads/IAP/analytics. They need later work and, in some cases, package or setting changes; see the Roadmap. |

## 3. Content and IP

- All design, names and art direction are the owner's (GDD sections 3 and 22). No third-party game assets are in the project.
- Naming risk: *CATO: Buttered Cat* owns the "buttered cat" idea, so the final mascot name should differ (GDD §3.2). Store and trademark search for "Pawtisserie" is still open (GDD §21.4).
- The repo structure, documentation layout, rules and tool projects were adapted from the owner's other project, Sand Drop (`A:\project game\Sand Drop`). No Sand Drop game code or art was copied.
- Art and audio sourcing is undecided.
- **HTML placeholder art (2026-11-04):** the owner supplied 87 exported prototype sprites under `Assets/Art/HTML Placeholder/Unity/PawtisseriePlaceholder` with an Editor importer and preview sheet. The current gameplay prefabs now use the supplied pastry, can, cloche, box, queue, gift, shelf, rack, stash, tray, aim, badge, card, cat marker, booster, restart, counter and Express artwork. These are placeholders pending final art; sprites and their metadata remain uncommitted for owner review. Baked card, stash and Express text needs replacement for localization. See `Docs/Design/TECHNICAL-KNOWLEDGE.md` §8 for scale and wiring details.

## 4. Architecture (target)

From GDD §18. Rules are plain C# so the same code runs the game, the aim preview and the level solver.

| Layer | Assembly | Status |
|---|---|---|
| Data + Model + Rules + Simulation + Prediction | `Paw.Sim` (`noEngineReferences`) | WP01 implemented; in-memory authored level input, cloned snapshots and deterministic event queue |
| Timing of the screen (events become timed cues) | `Paw.Presentation` (pure C#) | exists: EventCursor, FlightPath/Point2, AimState, PreviewSummary/badges and BoardMapping |
| Views, flow, services, UI, audio, configs | `Paw.Runtime` | exists: WP02 controller, clock, placeholder views and config |
| Editor tools (menu root `Paw/`) | `Paw.Editor` | not created (same reason) |
| EditMode tests | `Paw.Tests.EditMode` | exists |

Implemented core classes (GDD §18.1): `BoardState` (tray, box lanes, queue, plates, rack, stash tin, hand, deck, inventory), `MoveResolver`, `DestinationPicker`, `StoragePump`, `BoxReconciler`, `OutcomeChecker`, `Simulator` (event queue), `Predictor`. Input/config: `LevelDefinition`, `SimulationConfig`; public `State` is a deep copy. Planned views: `BoardView`, `BoxView`, `PlateRowView`, `RackView`, `HandView`, `CatView`, `FxPool`; flow: `GameController`, `LevelFlow`, `PanelController`, `TutorialDirector`.

Invariants (details in `AGENTS.md` §10 and `.claude/rules/unity-csharp.md`): deterministic sim (seeded RNG, fixed durations, no physics outcomes), the model never reads animation state, IDs are append-only.

Folders: `Assets/Scripts/{Sim,Presentation,Runtime,Editor}`, `Assets/Tests/EditMode`, `Assets/{Scenes,Prefabs,Data,Resources,Art}` (created when they get their first content), `Tools/{SimHarness,CompileCheck}`, `Docs/{Design}`.

**Unity structure standards (2026-11-04):** see [`Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md`](Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md) and [`Docs/Design/UI-LAYOUT-SYSTEM.md`](Docs/Design/UI-LAYOUT-SYSTEM.md). `CoreGameplay` contains separate controller, board, camera, HUD, popup canvas and EventSystem prefab instances under four separator roots. The legacy `GameRoot.prefab` is retained but no longer instanced in the scene. WP04 adds the popup manager, booster bar and Express collector.

## 5. Tuning values

Starting points from the prototype (GDD *(proto)* values). They are not final; when they become config assets, record the asset path here.

| Value | Number | Source |
|---|---|---|
| Hand size / starting cards / deck at start | 5 / 8 / 3 | GDD §6.2, fixed in v1 (`Rules`) |
| Box capacity / lanes | 3 / 3 | GDD §5.3 (`Rules`) |
| Plates / rack slots | 5 / 2 | GDD §5.6, §5.7 (`Rules`) |
| Tray size | 3×3 (tutorial) to 7×7; 8×8 max TBD | GDD §5.1 |
| Cell size / logical board width | 50 / 400 units | GDD §5.1, §11.2 |
| Impact delay / gravity delay / can delay | 0.1 s / 0.12 s / 0.2 s | GDD §18.2 |
| Pastry flight | 0.58 s fixed in `SimulationConfig`; owner-approved WP01 baseline | GDD §7.4, §18.2 |
| Tail swipe / box lid close / off-screen departure / arrival | 0.32 s / 0.30 s / +0.71 s / 0.58 s | GDD §18.2; WP02 approved fixed estimates |
| Shuffle weights (TAP/SWIPE/POUNCE/FLOP) | 3/8, 2/8, 2/8, 1/8 | GDD §9 |
| Starting boosters (Shuffle / Call box / Stash) | 2 / 2 / 1 | GDD §9 *(proto)* |
| Lives, coin amounts, prices, revive limit | **TBD** | GDD §8.3, §8.4, §16 |

Core timing and Shuffle tuning currently live in the engine-free `SimulationConfig` (microseconds); the simulator copies it. Paw/pounce/flop landing and gravity travel durations are required inputs; for the WP02 prototype they come from `Assets/Data/Config/ViewConfig.asset` (TAP 0.32 s, POUNCE 0.45 s, FLOP 0.36 s, gravity travel 0.25 s), which also holds board size (400×800, must equal the HUD canvas scaler reference), pastry and card colors, arc height, pool size (60), compaction 0.18 s, panel delays (win 0.7 s, overflow 0.55 s after landing, settled failure 0 s) and session-start boosters 2/2/1. Per-view colors are serialized on the prefabs. The session wallet supplies inventory to each new simulation and copies it back after use/grant.

## 6. Known issues and decisions

- **Open design decisions** (GDD §21.3): final game and mascot name; leftover cards at win (stars or coins); soft-only or soft + premium currency; revive limit and prices; pre-level boosters; pastry types 5+; first obstacle after the cloche. Do not hard-code guesses.
- **Player orientation** is *Auto Rotation* (`defaultScreenOrientation: 4`); the GDD says portrait. Changing it is a Project Settings change: ask first.
- **Build Settings reference a missing scene** (verified 2026-11-04): the enabled entry is `Assets/Scenes/SampleScene.unity`, which is absent on disk. `Assets/Scenes/CoreGameplay.unity` exists but is not in the build list. Open `CoreGameplay` directly for Editor testing; ask the owner before changing Build Settings or restoring a scene.
- The local feature branch is `feat/game-root-migration`; `master` was renamed from `feat/unity-core` on 2026-11-04 at the owner's request. The project has an initial commit. UnityYAMLMerge remains unconfigured (`AGENTS.md` §2.3).
- **Unity 2022.3** has no `Awaitable`; Sand Drop's template guide assumes Unity 6, URP, the Input System package and DOTween. Here Unity 6, URP and the Input System are not installed (a package or setting change, owner decision); DOTween (`Assets/Plugins/Demigiant/DOTween`) and Toony Colors Pro (`Assets/JMO Assets`) were imported by the owner on 2026-11-03.
- **Scripting defines (2026-11-03):** DOTween's setup had added `DOTWEEN_EPO`, which compiles `DOTweenModuleEPOOutline.cs` and needs the EPOOutline asset (not installed): 18 compile errors. At the owner's request `DOTWEEN_EPO` was removed from all platforms through `PlayerSettings`; `DOTWEEN;DOTWEEN_UITOOLKIT` remain. Re-running DOTween's setup panel may add it back; if the errors return, remove it again. Console then showed 0 errors and 2 `CS0618` warnings from the Toony Colors Pro demo scripts (third-party, left alone).
- **SimHarness** uses NuGet NUnit 3 because Unity 2022.3's NUnit 2.6.4 does not load on .NET 8. A restore needs network the first time.
- **Empty asmdefs** are avoided: Presentation and Runtime exist; Editor is not created yet.
- The root `.gitignore` does not ignore `Tools/*/bin` and `Tools/*/obj`; `Tools/.gitignore` does. Markdown documents are no longer ignored and can be added to Git.
- GDD §11.3: the prototype preview does not simulate chains or gravity; production must dry-run the full move (GDD §18.3).
- **Approved WP01 rules:** deterministic impact/tie order, settled-only card/booster commands, stored-only out-of-moves handling and fixed configurable flight timing are now recorded in the GDD. After Express, queued boxes can shrink and reconciliation counts their actual capacity (owner approved during implementation).
- **WP03 touch drag:** on 2026-11-04 the owner removed the one-tile vertical lift; the dragged card and its target now use the pointer position directly. The owner subsequently reported WP03 looks good in Play.
- **WP03 drag feel:** a Balatro-inspired visual tilt responds to horizontal pointer speed and settles when movement stops. `CardView` exposes its tilt limit, response and smoothing; the pointer still determines aim. The owner subsequently reported WP03 looks good in Play.
- **WP03 selection clarification:** the owner confirmed a selected card should bump up while in the hand as a selection indicator; this does not restore the removed drag offset. Both mouse and touch board interactions now play on release. The initial 22-unit hand bump is adjustable on `CardView` and was checked in an isolated Editor prefab preview; The owner subsequently reported WP03 looks good in Play.
- **WP03 review (2026-11-04), addressed in code:** `PointerGesture` now owns aim state, preserving selection until a play succeeds; mouse releases use the same board gesture as touch. A stationary mouse target is re-evaluated when a combo settles. `dragThresholdDp` is saved at its existing 10 dp value in `ViewConfig.asset`. The obsolete control descriptions in `CORE-LOOP-PLAN.md` were corrected. The owner subsequently reported WP03 looks good in Play; real-device checks remain open.
- **WP04 build (2026-11-04):** session inventory, simulated ad grants, booster bar, popup manager, outcome timing, stash slots, Call box queue events and the Express collector are implemented. Editor integration checks exercised Shuffle, stash, Call box and an Express revive. The owner considers WP04 complete and approved its commit; specific device checks remain unverified. Real ads and final art are not implemented.
- **WP04 layout clarification:** the owner's later six-zone reference puts the booster bar above `HandRoot`, in `BoostersRoot`, superseding the earlier bottom-of-screen proposal. The WP04 note now reflects this and the existing 44-unit minimum touch-target convention.
- **WP04 board fit:** the owner approved scaling `BoardRoot` to 80% and moving it up 15 units so the 7×7 tray clears the booster bar. The Express collector is positioned over the shelf. These are authored prefab transforms, not changes to the 400×800 config.
- **Hierarchy migration (2026-11-04):** `CoreGameplay` was rebuilt with separate prefab instances and a single EventSystem; serialized references were verified after reopening. HUD order 100 and popup order 110 retain layering above existing world sprites (including badges at 50), rather than the provisional 10/30. Both canvases share camera and safe-area fitting. The outcome footer uses a Horizontal Layout Group with 105×44 buttons. The owner's Play/visual check of the migrated scene is pending; popup management is WP04 work. An isolated prefab composition completed a simulated win and verified board play on release; a 400×800 edit-mode render was inspected.
- **Gameplay zones (2026-11-04):** `CounterRow` owns the `NEXT` label and `ShelfRack` owns the shelf counter through world-space child canvases; their scene camera references and the HUD's cross-prefab references are serialized. `BoardRoot` holds the tray anchor, input surface, aim label and pooled cells, badges and pastries. Pooled pastries remain under `BoardRoot` while flying or stored, so disabling it hides them even if they are on the shelf. `TopBar` groups level and restart, `HandRoot` groups cards and deck, and `BoostersRoot` holds the WP04 bar. The hand stays in the HUD safe area. Isolated toggle checks and edit-mode renders at 360×640, 400×800, 430×932 and a simulated inset passed; the owner must check input, flights and restart in Play, and real-device behavior remains unverified.
- **Nested groups (2026-11-04):** `CounterRow` contains `Lanes` and `Queue`; `ShelfRack` contains `Plates` and `Rack`; the hand contains `Cards` and `Deck`. Prefab references and world transforms were verified, and the 400×800 edit-mode render matched the preceding render pixel for pixel. The owner should check the updated hierarchy and game feel in Play.
- **Placeholder-art integration (2026-11-04):** a 3×3 edit-mode render at 360×640 and 7×7 renders at 400×800, 430×932 and a simulated inset were inspected. Isolated Editor checks verified card-face mapping and selection lift, all four box types and partial-capacity lid, aim overlays, stash/Call box routing, stash size restoration, primed can sprites and cat landing sprites. The saved gameplay scene retains its existing wiring and one EventSystem. Interactive Play, visual timing, HTML side-by-side comparison and real-device performance remain unverified.

- **MCP bridge warning:** refresh recovered from a disconnect and logged `[WebSocket] Unexpected receive error: WebSocket is not initialised` in the package. No package code changed. Compilation and EditMode tests are independent evidence; do not call the Console warning-free for this session.
- **Git whitespace check:** `git diff --check` reports trailing spaces on empty values emitted by Unity in `GameRoot.prefab` and `CoreGameplay.unity`. The assets were saved through the Editor and have not been hand-edited to remove these spaces.
- **Prototype parity is unverified:** fixtures reproduce the documented Level 0 and authored rule scenarios; the linked HTML source and its exact action timings/layout seeds have not been compared. No view or device checks yet.
- `AGENTS.md` refers to `README.md`, which is absent; not created as part of WP01.
- **WP02 review fixes R1–R5 implemented with owner approval:** cans do not block win; Express collects landed plate/rack and overflow pastries, not other airborne storage; Call box filters for queued candidates; box timings updated to 0.30/0.71/0.58 s; gift range is one-based 1..box count with late gifts permitted. GDD and level-data rule updated. Regression checks pass; external prototype source still needed for the exact generator port.
- **Prototype source** (resolved 2026-11-03): an earlier session could not fetch the HTML v12 source; it was later read and `PrototypeLevels` was verified against the prototype's own generator code run in Node.js.
- **WP02 build review (2026-11-03): fixed** HUD canvas order (0 → 100, panel now over the board, seen in a render), camera/canvas height fit on short screens, win panel delay 0.2 → 0.7 s, card band colors, view color literals moved to serialized fields, compaction arc restart; docs brought up to date. **Still open:** every stash pastry shares one anchor (only reachable via the WP04 booster bar); badge glyphs render in the Editor on Windows but are small/low-contrast and unverified on device; camera fit is one axis, not GDD §11.2's adaptive gaps (WP03). Creating `CoreGameplay` made Unity re-save `ProjectSettings.asset` and write `SceneTemplateSettings.json` (key Player settings unchanged; no baseline to diff).
- **`.gitignore` also ignores `*.csproj`**, so `Tools/SimHarness/SimHarness.csproj` and `Tools/CompileCheck/CompileCheck.csproj` would be left out of any commit and the fast-check tools would not build from a clone. WP03 added the required DOTween DLL reference to the local CompileCheck project. The ignore rule is unchanged.

## 7. Roadmap

From GDD §20; phases are estimates for a solo developer.

1. **Pre-production** (in progress): lock rules, name, mascot, art direction; store and trademark search; playtest the HTML prototype.
2. **Unity core** (in progress): WP01–WP04 implementation built; the owner reports [WP03](Docs/Design/WP03-INPUT-AND-HAND-FEEL-DESIGN-NOTE.md) looked good in Play before the hierarchy changes and considers [WP04](Docs/Design/WP04-BOOSTERS-AND-REVIVES-DESIGN-NOTE.md) complete. Real-device checks remain unverified. Later packages: WP05 tutorial/flow, WP06 parity/performance. Phase exit still requires prototype parity and device verification.
3. **Vertical slice:** final art for one chapter, cat animations, juice, audio, Level 0 + 10 levels.
4. **Content pipeline:** generator, solver, level editor; 50 levels. Level storage format (ScriptableObject or JSON) is undecided.
5. **Meta and monetization:** lives, coins, boosters shop, ads, IAP, analytics. This is also where the runtime shell (Boot -> Splash -> MainMenu -> Game, popups, save) from Sand Drop's `Docs/Template/TEMPLATE-BUILD-GUIDE.md` would be adopted; it needs packages and settings decided first.
6. **Soft launch:** 200 levels.
