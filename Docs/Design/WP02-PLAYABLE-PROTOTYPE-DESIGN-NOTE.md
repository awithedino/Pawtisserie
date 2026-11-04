# WP02 — Core loop playable (placeholder art)

Date: 2026-11-03. Status: **all 13 section 10 proposals approved; built (steps 1–7, see §13); owner Play check pending.** This version replaces the earlier single large WP02 proposal (a copy of it was kept outside the repo). The package list it belongs to is [`CORE-LOOP-PLAN.md`](CORE-LOOP-PLAN.md).

## 1. Goal

Open `CoreGameplay`, press Play, and play prototype levels 0–3 with the mouse: select a card, hover/tap a tile to see the full-move preview, drop, watch pastries fly into boxes, plates, rack or overflow, watch boxes close and arrive, and end in win or fail with **Retry**. Placeholder art; the rules are the WP01 sim plus the fixes in section 3.

Not in WP02 (later packages): drag and touch-lift input, slide aim, the real fan and deal animation, cat move art, adaptive safe-area layout (WP03); booster bar and revive buttons (WP04); scripted tutorial and level flow (WP05); allocation-free sim advance, profiling, the written parity table (WP06); audio, haptics, final art, app shell, save.

## 2. Starting point (checked 2026-11-03)

Verified by running: SimHarness 35 passed / 0 failed; CompileCheck 0 errors / 0 warnings. Verified in the live Editor (Unity MCP): one instance `Pawtisserie@02466c73593f71c9`, idle, not in Play, `SampleScene` active; Console has only the known MCP WebSocket warning.

Verified in files: `Assets` holds only `Scenes`, `Scripts/Sim`, `Tests/EditMode`. No Presentation/Runtime/Editor assemblies, prefabs, configs, sprites or fonts. TextMeshPro 3.0.7 is installed but its Essential Resources are not imported. `com.unity.feature.2d` is installed. Player orientation is still Auto Rotation (unchanged, owner decision).

The HTML prototype source (v12, read on 2026-11-03) was compared with the WP01 code for the points below. Prototype values derived from its code are **estimates** (its animations are frame-rate dependent easing).

## 3. Review of WP01 against the GDD and the prototype

| # | Finding | Evidence | Proposal |
| --- | --- | --- | --- |
| R1 | **A cream can left on the tray blocks the win.** `OutcomeChecker` counts any tray item, cans included. With every pastry boxed and one untouched can, the level stays Playing (a card must be wasted on the can) or fails Out of moves with zero pastries left. | `OutcomeChecker.cs:10`. Prototype `crateCount()` counts pastries only; GDD §4.1 and §8.1 speak of pastries. | Count pastries only, for both win and out of moves. Regression test. |
| R2 | **Express box takes more than the prototype.** (a) While Express is on screen, a newly launched pastry with no Ready box goes into Express ahead of the plates. (b) Pastries still flying to a plate or the rack are redirected into Express. | `DestinationPicker.cs:19`, `Simulator.cs:216-218`. Prototype `launchCrate` has no Express branch; `reviveExpress` takes only **landed** plate/rack pastries plus everything that reaches the overflow spot. GDD §8.3 lists plates, rack and overflow, and redirects only flights to the overflow spot. | Match the prototype for (a) and (b). Keep the Unity choices the GDD text supports: overflow-bound flights redirect at once, and reconciliation runs after Express leaves (GDD §7.6). |
| R3 | **Call box can refuse when the prototype would not.** Unity picks the most-waiting type first and gives up if it has no queued box; the prototype only considers types that still have a queued box, so it calls the next-most-waiting one. | `BoosterEffects.cs:63-75`; prototype `callTruck()`. | Match the prototype. Test. |
| R4 | **Box cycle is about 0.6 s faster than the prototype.** Unity: lid closed at 0.30 s, off screen at 0.62 s, next box Ready at 1.02 s after the last landing. Prototype: closed at 0.30 s, starts leaving at 0.62 s, off screen ≈ 1.01 s, next box Ready ≈ 1.59 s. Over a long combo this changes how many pastries reach a box instead of a plate. | `SimulationConfig` 0.30 / 0.32 / 0.40; prototype `closeDock`, leaving acceleration 1700 u/s² over 130 u, arrival ease ≈ 0.58 s (**estimates**). | `BoxDeparture` 0.32 → 0.71 s, `BoxArrival` 0.40 → 0.58 s. These are the owner-approved WP01 timings, so this needs an explicit go. |
| R5 | **Gift numbers in the prototype are 0-based.** The prototype says "0 = first box"; GDD §22.3 copies those numbers, while GDD §5.5/§14.1 and `LevelDefinition` use 1-based arrival numbers. Read correctly, levels 1–3 each put a gift on the second-to-last box (index 10 of 12, 11 of 13, 13 of 15), which the validator (and GDD §14.5) forbid. | Prototype `LEVELS`; `LevelDefinition.cs:53`. | Port the exact prototype gifts (key + 1). The validator then checks 1..box count only; the "not on the last two boxes" rule becomes an authoring guideline (GDD §14.5 calls it a guideline). Correct GDD §22.3 to 1-based numbers. This relaxes a validator, so it needs the owner's go. |
| R6 | Dealt card is appended to the right of the hand. | `Simulator.cs:112`; prototype `useCard` does the same. | No change (verified parity). |
| R7 | A gift box that leaves while the level is failed loses its gift, even if the player then revives. | `Simulator.cs:120-121`; prototype `boxDone` does the same; GDD §5.5. | No change (verified parity). |
| R8 | Technical: event kinds are free strings (a typo makes a new kind silently); `Events` allocates a wrapper on each read; `Predictor` copies the whole event history of the level on every call, so hover cost grows through the level. | `SimEvent.cs`, `Simulator.cs:18,36`. | Fix in the event contract (section 4). No rule change. |

Also noted, no change proposed: the prototype closes a box reconciled to "full" after 0.2 s instead of 0.3 s; the prototype re-flies overflow pastries to Express after they land, Unity redirects them in flight (same result, smoother view).

## 4. Sim changes

Existing rule assertions are preserved except where they explicitly encode a behavior changed by approved R1–R5. Those expectations are replaced with regressions for the approved behavior, and each affected test is listed in the build report. Event-string assertions change mechanically to the enum. No test is disabled or made less specific.

1. **Review fixes** R1–R5 as approved, each with a regression test and a replay check.
2. **Event contract:**
   - `SimEventKind` enum replaces the string `Kind` (append only, same names as today).
   - `SimEvent.EndTime`: when the started transition finishes (Routed → arrival, TrayFell → fall end, BoxArriving → Ready, BoxClosing → lid closed, BoxLeaving → off screen, CardPlayed → impact, CanPrimed → explosion, ExpressStarted → earliest departure). The view needs this to interpolate without guessing.
   - `EventCount` and `GetEvent(i)` for a cursor with no allocation; `Events` stays for tests.
   - Read-only live accessors (value copies of tray cells, lanes, queue, plates, rack, stash, hand, deck count, inventory) so the view can lay out the current state **without** cloning the board each frame. `State` stays the safe deep snapshot.
   - `Predictor` clones without the event history.
3. **Prototype levels** (`PrototypeLevels`, pure C# in `Paw.Sim`): Level 0 as authored, and levels 1–3 from a port of the prototype generator (mulberry32 seed, pastry shuffle, can placement from row 1 with no adjacent cans, cloches, box order with the first-three-different fix-up, decks and gifts from GDD §22.3). Same seed, same layout as the HTML, which is what makes the WP06 side-by-side check possible. Every level passes `Validate()`. Not a level storage format and not the phase 4 generator.

## 5. Presentation (`Paw.Presentation`, pure C#, runs in SimHarness)

- `EventCursor`: hands each new event to the views once, in order.
- `FlightPath`: position at time *t* on an arc from a start point to an end point between two sim times. A retarget starts a new path from the current position and ends at the model's new arrival time.
- `PreviewSummary`: turns a `Prediction` into one badge per affected cell (box tick, plate arrow, rack arrow, overflow `!`, cloche crack, can dot), including cells hit by chains outside the card's footprint, plus the aim-pill state (normal / rack / overflow) as the worst case of the whole move.
- `AimState`: tap card = select, tap again = cancel; tap or hover tile = aim; tap the aimed tile or click = drop; tap another card while aimed = switch and keep the aim; Escape = cancel. Commands only when the sim is settled and playing (WP01 policy).

## 6. Runtime and visuals (`Paw.Runtime`)

| Part | Job |
| --- | --- |
| `GameController` on `GameRoot` | Owns the `Simulator` and level/attempt; Retry and the top-bar restart rebuild it; feeds the clock; routes `AimState` commands |
| `SimClock` | Microseconds from unscaled time; pauses on focus loss and rebases on return (no `timeScale` changes) |
| `BoardView` | Tray cells, pastries, cans (primed flash), cloches (crack); gravity falls follow `TrayFell` start/end times |
| `BoxLaneView` ×3, `QueueView` | Box phases (arriving, ready, closing, leaving), filled/need marks, gift tags, reduced need after reconciliation; mini boxes in the queue |
| `ShelfView`, `RackView` | Five plates in FIFO order with compaction, stash tin, rack (open during a combo or while it holds pastries), overflow spot, `SHELF n/5` |
| `FlightLayer` | Pooled flying pastries driven by `FlightPath` (pool prewarmed for 60) |
| `HandView`, `DeckView` | Five cards (name + 5×5 mini grid + band color), selected and disabled states, deck count |
| `AimPreviewView` | Blast-area tint, badges and the `TAP AGAIN TO DROP` / rack / overflow pill |
| `OutcomePanel`, `TopBar` | Win / Shelf overflow / Out of moves with **Retry** (revive buttons arrive in WP04); level pill and restart |

Tuning lives in one `ViewConfig` ScriptableObject (`Assets/Data/Config/ViewConfig.asset`, each field with a `[Tooltip]`): sizes, arc heights, colors, panel delays, the pool size, and the four sim timing inputs copied into a fresh `SimulationConfig` per run. Zone positions are anchors on the `GameRoot` prefab that the owner can move by hand; code computes only the screen-fit scale (GDD §11.2's "scaled to the screen width").

Placeholder art: one plain shape per pastry so color is never the only cue (donut = circle, macaron = capsule, cupcake = triangle, croissant = hexagon), GDD §3.3 colors; boxes, plates and rack as tinted rectangles. Shapes come from the 2D package's sprite templates (Assets > Create > 2D > Sprites; availability **unverified** until checked in the Editor). Text uses the built-in legacy font; TMP and the final fonts wait for art.

## 7. Files and Editor work

| Type | Paths | Editor? |
| --- | --- | --- |
| Sim | `Assets/Scripts/Sim/` (fixes, event contract, `PrototypeLevels.cs`) | Recompile |
| Presentation | `Assets/Scripts/Presentation/*.cs` + `Paw.Presentation.asmdef` (no engine references) | Created with its first script |
| Runtime | `Assets/Scripts/Runtime/{Flow,Views,Config}/` + `Paw.Runtime.asmdef` (Sim, Presentation, UnityEngine.UI) | Created with its first script |
| Tests | `Assets/Tests/EditMode` (+ reference to `Paw.Presentation`): review regressions, contract, levels, presentation | EditMode run |
| Assets | `Assets/Art/Placeholder/` sprites; `Assets/Data/Config/ViewConfig.asset` | Created in the Editor |
| Prefabs | `Assets/Prefabs/Game/GameRoot.prefab` plus pastry, box, plate, card and badge prefabs it uses | Through MCP / Prefab Mode |
| Scene | `Assets/Scenes/CoreGameplay.unity`: one `GameRoot` instance (camera, canvas, EventSystem inside) | New scene; `SampleScene` and Build Settings untouched |
| Docs | this note, `PROJECT.md`, `TECHNICAL-KNOWLEDGE.md`, `AGENTS.md` (assemblies, test count, map), GDD (approved R-items) | Files |

No package, Project Settings or Unity version changes. No `Paw.Editor` assembly in WP02.

## 8. Build order (after the go)

| Step | Work | Exit evidence |
| --- | --- | --- |
| 1 | Review fixes R1–R5 as decided | Harness + regressions; existing 35 green |
| 2 | Event contract, read-only accessors, predictor clone | Harness; EndTime and cursor tests; prediction history test |
| 3 | `PrototypeLevels` | Validation, determinism, first-three-different, no adjacent cans; layouts compared with the prototype's generator run outside Unity if Node.js is available (otherwise stated as unverified) |
| 4 | `Paw.Presentation` | Pure tests: flight retarget, badge mapping incl. chains, aim state transitions |
| 5 | Runtime scripts and `ViewConfig` | CompileCheck; Unity recompile, Console read |
| 6 | Sprites, prefabs, `GameRoot` | Edit-mode renders (camera screenshots) of levels 0 and 3; **checkpoint: renders shown to the owner before the scene** |
| 7 | `CoreGameplay` scene, final checks, docs | EditMode tests; `git status` shows only intended files; Console compared with the baseline warning |

SimHarness and CompileCheck after every script step. No Play mode through any tool. If the owner is in Play, stop edits and wait.

## 9. How it is checked

Automated: all tests green in SimHarness and Unity EditMode; CompileCheck clean; no new Console errors or warnings beyond the known MCP one; renders at one portrait size (400×800) for levels 0 and 3.

Owner Play checklist (I never enter Play):

1. Level 0: TAP the center, SWIPE the middle row, SWIPE the bottom row → win panel. (The tutorial script itself is WP05.)
2. Hover and click on levels 1–3: preview badges match what then happens, including can chains and cloches; the pill turns steel/red for rack/overflow moves.
3. Watch a long combo: boxes close, leave and arrive; plates compact; a rack pastry hops to a freed plate mid-flight without a double landing.
4. Fail by overflow and by rack; check the panel cause; Retry gives the same starting board.
5. Restart mid-combo, then quit and re-enter Play: no leftover pastries, flights or panels.

Not covered by WP02: touch input, phone sizes and safe area, frame time, exact HTML parity (WP03/WP06).

## 10. Owner decisions — approved

The owner approved all 13 proposals on 2026-11-03: "I approved all the proposals given in the section 10, you may continue to build the core loop for the game now." The proposals below are the chosen options, not their alternatives. The render checkpoint in section 8 remains part of the build order.

1. **Scope.** Approve WP02 as in sections 1 and 6, with boosters/revives, tutorial, drag/touch and performance in later packages (`CORE-LOOP-PLAN.md`). Proposal: yes.
2. **R1 cans and win.** Proposal: cans never block a win or cause out of moves.
3. **R2 Express.** Proposal: new launches during Express follow the normal box → plate → rack order, and only landed plate/rack pastries plus overflow-bound ones go into Express.
4. **R3 Call box.** Proposal: only types with a queued box are candidates.
5. **R4 box timing.** Proposal: departure 0.71 s and arrival 0.58 s to match the prototype (estimates); alternative: keep 0.32 / 0.40.
6. **R5 gift numbering.** Proposal: port prototype gifts exactly (key + 1), validator checks the range only, GDD §22.3 corrected; alternative: keep the validator rule and move those three gifts one box earlier.
7. **Move timings** (WP01 left them as required inputs). Proposal from the prototype code: TAP landing 0.32 s, POUNCE 0.45 s (prototype 0.33–0.59 s depending on the row), FLOP 0.36 s, gravity travel 0.25 s (prototype 0.20 s for one cell to 0.50 s for six), all in `ViewConfig`. Alternative: make POUNCE and gravity depend on distance like the prototype (more parity, more sim code).
8. **Rendering.** Proposal: the board, pastries and flights as world-space sprites (cheap to move 50 objects, natural for particles and a future cat rig), and the hand, top bar and panels as uGUI on a Screen Space - Camera canvas. Alternative: everything in uGUI (one coordinate system, but each moving pastry dirties the canvas).
9. **Placeholder look.** Proposal: plain sprite shapes per pastry (section 6) and the legacy font; no TMP Essentials import yet.
10. **Preview badge rule.** Proposal: each badge shows where the pastry goes **at launch** (a plate pastry that later reaches a box still shows the plate arrow), chain-hit cells get badges too, and the pill shows the worst case of the move.
11. **Levels.** Proposal: Level 0 played freely (boosters off as in WP01) plus prototype levels 1–3; Next after a win, Retry after a fail; a level index field on `GameController` to jump straight to a level.
12. **Retry seed.** Proposal: Shuffle seed derived from level id and attempt number (GDD §18.4), the attempt counting up on each Retry; layouts do not change between attempts.
13. **Scene and assemblies.** Proposal: new `CoreGameplay` scene, `Paw.Presentation` and `Paw.Runtime` assemblies; `SampleScene`, Build Settings and Project Settings untouched.

## 11. Risks

- Prefab building through MCP is slow and can drop during recompiles: build in small steps, re-read Editor state after each.
- World-space board + Screen Space - Camera UI must share one camera and one fit scale, or hit tests drift from visuals; covered by a pure mapping test and the renders.
- The generator port depends on JavaScript number behavior (`Math.imul`, unsigned shifts, `Math.floor(rng() * n)`); the C# port uses the same 32-bit integer steps and double division, checked against a Node.js run when available.
- Changing event kinds touches every test that filters events; it is mechanical but large. No assertion values change.
- R4 changes combo outcomes on purpose. The test fixtures use the default box timings, so a scenario whose point is a rule (not a timing) may need the old 0.32 / 0.40 pinned in its fixture to keep testing the same situation; any such change will be listed, and no assertion is loosened.

## 12. Implementation checkpoint (2026-11-03)

Completed: step 1 (R1–R5), step 2 (typed/timed events, cached/indexed access, value-copy live accessors, history-free predictor clones), and the independent pure code from step 4 (EventCursor, FlightPath, PreviewSummary, AimState, BoardMapping). New `Paw.Presentation` assembly references only Sim; the EditMode assembly references Presentation. Every new Unity file has Editor-generated metadata.

Verification: SimHarness **49 passed / 0 failed**; CompileCheck **0 warnings / 0 errors**; Unity EditMode **49 passed / 0 failed / 0 skipped**, MCP job `4a630700547f4b0aa48b64bb6c24438a`. Console: no errors, only the known MCP WebSocket warning. No Play or commits.

Two behavior expectations changed because they encoded explicitly superseded rules: `CoreModelTests.MalformedCountsCardsAndGiftsAreRejected` now uses arrival 4 for a three-box level (out of range), and the renamed airborne-Express regression expects only overflow flights redirected, with plate/rack flights preserved. It verifies the subsequent rack failure and second revive as well. All other Kind-string comparisons became enum comparisons. No tests were skipped or weakened, and no old timing fixture needed pinning.

Five new review regressions cover leftover-can wins/replay, newly launched pastries during Express, eligible Call box candidates/ties, the approved 1.59 s box cycle, and valid late gifts/replay. Three event-contract tests cover EndTime, cached/index access, read value isolation and history-free clones. Six presentation tests cover cursor/reset, flight retarget continuity, aim transitions, first-route badges, chains/overflow and mapping edges.

Dependency: step 3 requires the actual HTML v12 generator, including its random-call order and seeds. This session's artifact retrieval returned only the shell; the public content endpoint returned a browser challenge; browser access host was unavailable. No challenge bypass attempted. No source file was found in this repo or artifact scratch folders; owner asked for a local path or source URL. Exact levels 1–3 have not been fabricated from the GDD's approximate generation description. While waiting, only independent pure presentation work was advanced out of sequence.

Still pending: source-dependent prototype port/comparison, Runtime/ViewConfig, placeholder sprites, all prefabs, level 0/3 renders and the pre-scene render checkpoint, CoreGameplay scene and final owner Play checks. No Runtime or Editor assembly, scene, prefab, config asset, package or Project Settings changed. Documentation updated in PROJECT (sections 1, 2, 4–7), technical knowledge, GDD, core-loop plan, AGENTS and the level-data rule.

## 13. Status after the build (2026-11-03)

Steps 1–7 done. The checkpoint in §12 is superseded: the prototype source was recovered and the steps it lists as pending were built afterwards.

- **Step 3 (prototype levels):** `PrototypeLevels` ports the HTML generator. The prototype's own generator code, run in Node.js, gives exactly the layouts, cloches and box orders asserted in `PrototypeLevelTests` (verified by test and by an independent Node run); decks and +1 gifts match the HTML.
- **Steps 5–6 (runtime, prefabs):** `Paw.Runtime` (`GameController`, `SimClock`, `BoardView`, `BoxLaneView`, `PastryView`, `CardView`, `BadgeView`, `GameHud`), `ViewConfig` at `Assets/Data/Config/ViewConfig.asset`, placeholder sprites in `Assets/Art/Placeholder`, prefabs in `Assets/Prefabs/Game` (`GameRoot`, `Pastry`, `Box`, `Plate`, `TrayCell`, `Card`, `Badge`).
- **Review fixes after the build:** HUD canvas sorting order 0 → 100 so the outcome panel draws over the board; the camera and the canvas scaler fit height (`ViewConfig.boardHeight` 800) on screens shorter than 1:2 and width otherwise; `ViewConfig.panelDelay` 0.2 → 0.7 s (GDD §8.1 win panel); cards have their own band colors (`ViewConfig.cardColors`, prototype values) instead of pastry colors; view colors moved from code literals to serialized fields on `CardView`, `GameHud`, `BadgeView`, `BoardView`, `PastryView` (same values); compaction moves only pastries whose slot changed, so airborne plate pastries keep their arc.
- **Step 7:** `Assets/Scenes/CoreGameplay.unity` holds one `GameRoot` prefab instance and nothing else (root overrides only). `SampleScene` and Build Settings untouched. Creating the scene made Unity re-save `ProjectSettings/ProjectSettings.asset` and write its default `ProjectSettings/SceneTemplateSettings.json`; orientation, input handler and defines are unchanged, the rest cannot be diffed (no git baseline).
- **Render checkpoint:** edit-mode renders at 400×800 (camera render, scene reloaded from disk afterwards, not saved): level 0 start, level 3 start, level 3 FLOP aim preview (10 badges incl. a chain-hit cloche pastry and a can dot), level 3 mid-flight at 0.9 s, boxes cycling at 2.6 s, outcome panel over the board. Badge glyphs render in the Editor on Windows; they are small and low-contrast on the gold blast tint (size is on the `Badge` prefab). Not seen in a render: tweens and timing, real mouse input, other screen shapes.
- **Verification:** SimHarness 55/55, CompileCheck 0/0, Unity EditMode 55/55 (job `364c147754fa44c08f980ccbbf038f2c`), Console only the known MCP WebSocket warning. No Play, no commits.
- **Still open, outside WP02:** stash pastries share one anchor (stash is reachable only through the WP04 booster bar); the camera fit is a single axis, not GDD §11.2's adaptive gaps (WP03); `ViewConfig.boardWidth/boardHeight` must stay equal to the canvas scaler reference resolution (400×800).

Owner Play checklist: §9. Open `Assets/Scenes/CoreGameplay.unity`, set the Game view to a portrait size (for example 1080×1920 or 1080×2340), pick the level on `GameRoot` → `GameController` → Level Index.
