# Technical knowledge

How Pawtisserie works and why: invariants, traps and decisions that are not obvious from the code. `PROJECT.md` says what exists; this file says how it works. Updated for WP01 on 2026-11-03.

## 1. Sim and view

- Rules live in `Paw.Sim`, plain C# with no engine references (`noEngineReferences`). The view plays timed events and never reads sim state through animation state.
- The sim is deterministic (GDD §18.4): seeded RNG per level + attempt, fixed flight durations, no physics, no `Time.*`.
- Resolution order is GDD §7. Check order when settled: reconciliation → rack not cleared → win → out of moves (GDD §8.1).

## 2. Traps carried over from Sand Drop (verify here when first met)

- A popup or panel that starts inactive has not run `Awake`; set up in a manager's `Awake` before calling `Setup`.
- Layout Groups own their children's position; do not tween a child under one.
- A level reload in the middle of a win/fail coroutine leaves flags set and popups open: reset everything on every level load.
- Android defaults to 30 fps: set `Application.targetFrameRate` in a config (GDD target is 60).
- A mutable static survives Play-mode restarts when domain reload is off: reset in `SubsystemRegistration`. (Enter Play Mode Options are currently off, so domain reload is on.)
- Do not mutate a ScriptableObject at runtime.
- The MCP bridge can drop while the Editor recompiles.

## 3. Tooling facts

- **DOTween** is available at `Assets/Plugins/Demigiant/DOTween/`; use it where useful for presentation animation. Its core DLL can be referenced from `Paw.Runtime`; the UI/Sprite modules are in `Assembly-CSharp-firstpass`, which an asmdef cannot reference. `Tools/CompileCheck` needs an explicit DLL reference if game scripts start using DOTween. Kill owned tweens when their component is disabled or a level resets.
- **Toony Colors Pro** is available at `Assets/JMO Assets/Toony Colors Pro/`; its shaders and tools are available for suitable art and rendering work. The project currently uses the Built-in render pipeline; check compatibility and existing assets before choosing a shader.
- The owner wants agents to use these existing tools when appropriate. Do not re-run DOTween setup or change packages, scripting defines, or Project Settings without the owner's approval: setup previously added `DOTWEEN_EPO`, which required an uninstalled asset and caused compile errors (see `PROJECT.md` §6).
- `Tools/SimHarness` compiles `Scripts/Sim`, `Scripts/Presentation`, `Scripts/Runtime/Config`, `Scripts/Runtime/Services` and `Tests/EditMode` with NuGet NUnit 3 and `UnityStubs.cs`. Unity's own NUnit (2.6.4) cannot load on .NET 8.
- `Tools/CompileCheck` compiles all game scripts against `Editor/Data/Managed/UnityEngine/*.dll` of `2022.3.62f3` plus `Library/ScriptAssemblies/{Unity.TextMeshPro,UnityEngine.UI}.dll`.
- An asmdef with no scripts warns in the Console; create each asmdef together with its first script.

## 4. WP01 core API and invariants

- Construct `Simulator(LevelDefinition, SimulationConfig, uint seed, int[] inventory)`. `LevelDefinition` is an in-memory input, not a choice of serialized level format. Use the explicit tutorial flag only for the documented Level 0 exceptions.
- `SimulationConfig` takes paw/pounce/flop landing and gravity travel times explicitly. All times are nonnegative integer microseconds. Other default durations and Shuffle weights are the approved prototype starting points. The simulator copies config and source data.
- `TryPlay(handIndex, Cell, out reason)` accepts a command only while playing and settled. `AdvanceTo(absoluteMicroseconds)` processes scheduled work in timestamp/insertion order. `RunUntilSettled()` drains work with an event-count guard. Failed levels still drain previously accepted work so flights and chains finish.
- `State` returns a deep snapshot; editing it never edits the live simulation. `Clone()` additionally copies the clock, pending actions, RNG, event sequence and Express state. Predictor uses this same execution path and returns events for the new command, including intermediate routes.
- IDs are input-index based per run (tray and boxes have separate domains); events distinguish those domains by kind. No pastries or boxes are spawned by rules, so an allocation counter is unnecessary. A falling can retains its ID and explodes at its current cell.
- Box `Reserved` is cumulative promised occupancy: landed `Filled` is already included. Free capacity is `Need - Reserved`, never minus `Filled` again. Delivered pastries leave active pastry storage; `Delivered` and `RemovedByExpress` counters permit conservation checks.
- Plates/rack/stash lists contain pastry IDs; active pastry records include destination, landed state, arrival time and flight version. Reservations count flights immediately. Retargeting removes the old arrival action and schedules one replacement; an airborne rack-to-plate transfer preserves its original arrival time.
- Gravity updates logical cells at the gravity event and emits `TrayFell`; a separate configured travel interval keeps the combo running. Views will interpolate these events, never decide hit outcomes.
- Storage pumps after each action and before settlement: landed stash first, then plates, then rack; rack-to-plate transfer is FIFO and may retarget airborne pastry records.
- Reconciliation may schedule new departures, so settlement checks restart after that work. Owner-approved queued shrinking fixes fractional excess when Express removes a non-multiple of three while all matching boxes are queued. Queued room uses actual `Need`.
- `TryUseBooster` consumes caller-supplied per-run inventory only on an eligible effect. Shuffle uses instance-owned xorshift32 state (including a specified zero-seed fallback). `TryRevive` applies the matching effect; payments and revive limits are outside the core. The runtime must retain inventory across restarts.
- Express (approved R2) redirects only overflow-bound flights, collects landed plate/rack/overflow pastries (airborne plate/rack flights finish normally; new launches use box → plate → rack, and go into Express only when the rack is full), and waits for accepted work plus its minimum duration before departing and reconciling. Stash is preserved. Gifts are only paid while playing.
- `Events` is a read-only chronological transition log. `Kind` identifies the transition; `EntityId` names its pastry/box/card/booster as appropriate. `Routed.Value` is `Destination`, and `TargetId` is the reserved box ID (zero for storage). `BoxArriving.Value` is lane index; `BoxReady.Value` is pastry type. `TrayFell.Cell` is the new cell and `Value` is the old row. `Outcome.Value` is the outcome enum. No panel timing or visual tween system exists yet.

## 5. WP02 implementation checkpoint

- Owner approved all 13 WP02 section 10 proposals. Rule fixes R1–R5 are implemented and reflected in GDD sections 5, 8, 9, 14, 18 and 22. `PrototypeLevels` ports the HTML v12 generator; layouts verified against the prototype's own code run in Node.js.
- `SimEvent.Kind` is now append-only `SimEventKind`. `EndTime` gives the end of a started transition; instantaneous events end at `Time`. CardPlayed ends at impact, Routed at arrival, TrayFell at fall end, CanPrimed at explosion, BoxClosing at lid close, BoxLeaving at off-screen departure, BoxArriving at Ready, ExpressStarted at its minimum departure time.
- `EventCount` / `GetEvent` avoid allocating an event wrapper for a cursor; `Events` now caches its wrapper. `GetTrayCell`, `GetLane`, `GetQueuedBox`, `GetPastry`, storage ID/count, hand, deck-count and inventory accessors expose values, not mutable live objects. `State` still deep-clones.
- `Clone(includeHistory: false)` retains clock, RNG, sequence and pending actions but drops transcript history; Predictor uses it so preview cloning does not grow with past moves.
- `Paw.Presentation` exists with no engine references. EventCursor reads events once; FlightPath interpolates and retargets continuously; AimState handles selection/aim/confirm; PreviewSummary reports the first route at launch and includes chain-hit cells, with worst-case rack/overflow pill risk; BoardMapping shares pure logical coordinates for placement and hit testing. No Runtime or prefab wiring yet.
- Step 4 pure presentation helpers were built while step 3's exact level port waits for source. This independent work does not guess replacement prototype layouts/seeds. Runtime/prefab/scene steps remain pending in their approved order.
- Changed test expectations are explicit: the malformed-gift regression now rejects an out-of-range arrival instead of a valid late gift; the airborne-Express regression now preserves plate/rack flights, observes the remaining rack failure, then checks a second revive collecting landed storage. Other string-kind assertions changed mechanically to enums. No test disabled.

## 6. Verification limits

55 tests pass in both SimHarness and Unity EditMode after the WP02 build; CompileCheck is clean. The tests exercise documented rules and the Level 0 sequence, including a 20-seed authored-scenario matrix. This is not a comparison against the external HTML implementation. Edit-mode renders of the `CoreGameplay` setup exist (WP02 note §13). No Play, scene reload, touch behavior, visual performance or production movement timings have been verified. One Unity MCP WebSocket warning remains; the bridge recovered without package changes.

## 7. WP02 runtime traps

- The HUD canvas (Screen Space - Camera) shares the sorting layer with world sprites; its sorting order (100) must stay above the highest sprite order (badges 50) or the board draws over panels.
- Camera size and `CanvasScaler.matchWidthOrHeight` are switched together by `GameController.FitCamera`; `ViewConfig` board width/height must equal the scaler reference resolution (400×800), or board and HUD drift apart.
- `BoardView` owns per-pastry slot indices; a flight is re-issued only when the slot changes, because `PastryView.Move` restarts the arc from the current position.

## 8. HTML placeholder artwork

- The prototype export lives under `Assets/Art/HTML Placeholder/Unity/PawtisseriePlaceholder`. Its 87 manifest sprites import at 200 pixels per unit, but the game still uses 50 logical units per cell, a 400×800 reference and `BoardRoot` scale 0.8. Do not change the project coordinate system to the export's one-unit-per-cell convention. Prefab transforms and `PastryView.artworkScale` fit the source images to the existing layout.
- `PastryView` uses pre-coloured tray and flat flight sprites, separate cloche and normal/primed can sprites. It avoids the old placeholder tint/rotation in artwork mode. `stashScale` is a serialized 0.5 multiplier; `BoardView` restores normal scale as a pastry leaves the stash. Procedural movements, pools and simulation timings are unchanged.
- `BoxLaneView` maps pastry IDs to open, closed, queue, ghost and filled sprites and card IDs to gift tags. The `Queue` prefab instances use the miniature artwork. Because the closed source images bake in three pastries, boxes resized below three capacity retain their dynamic slots and use an authored simple lid instead. Keep `SetGift` and capacity/slot rendering synchronized with simulator events.
- `BoardView` owns the sliced tray frame, checker tiles and pooled aim overlays. `BadgeView` maps prediction kinds to sprite artwork. The tray pointer surface and the board's world-to-cell mapping are unchanged. `CatMarker` uses paw/arm/tail artwork, and switches POUNCE/FLOP cats from focused to happy at landing.
- The card faces already contain the title and footprint. `CardView.cardFaces` changes the inner `Visual` image; its old title, band and grid objects are retained but inactive so serialized references and future replacement are intact. Selection remains indicated by the approved lift. Card and Express artwork has baked text; use separate localizable text with final assets.
- The importer's reapply command can overwrite sprite import settings under matching manifests. Preserve the owner's `.meta` files, pivots, borders and any later Inspector adjustments. The source PNGs are covered by Git LFS; no settings or package changes were needed.
