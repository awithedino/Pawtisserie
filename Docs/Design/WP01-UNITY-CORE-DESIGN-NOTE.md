# WP01 — Unity core

Date: 2026-11-03. Status: core implementation and automated verification complete; MCP-package warning and presentation/Play limitations recorded below.

## 1. Goal and scope

Build the deterministic, plain-C# gameplay core in `Paw.Sim` from GDD sections 5–9 and 18. The same simulator must drive a future game view, full-move preview and solver. This package ends with an executable simulation and tests, not a playable scene.

Included: cloneable board state, card shapes, impacts, cloches, can chains, gravity, destination reservations, storage pumping, box lifecycle, gifts, reconciliation, outcomes, booster effects, revive effects, timed events and prediction.

Excluded: presentation, input, art, prefabs, scene wiring, app flow, tutorial UI, generator/solver, persistent saves, lives/economy, ads/IAP and payment or revive-limit decisions. Booster inventory is supplied by the caller; persistent inventory ownership belongs to the later runtime. The full Unity-core roadmap phase still needs a separate presentation package and owner Play checks against the prototype.

No new assemblies, packages, Project Settings changes or serialized level assets. Use in-memory level definitions and test fixtures without deciding JSON versus ScriptableObject storage. Level 0 needs an explicit tutorial fixture: its three cards and two pastry types are exceptions to the standard eight-card and first-three-distinct-box rules (GDD sections 13.2 and 22.3).

## 2. Starting point — verified by file reads before implementation

- `Rules.cs`: six fixed v1 constants.
- `CardKind.cs`: four stable enum values and string IDs.
- `Paw.Sim.asmdef`: no engine references.
- `RulesTests.cs`: five existing tests for constants and IDs.
- SimHarness and CompileCheck include top-level `Sim/*.cs` files; keep this package's sim files there so the existing checks cover them.
- SimHarness supports `[Test]` and `[TestCase]`, not setup/teardown; new tests use the common NUnit API supported by both environments.
- No model, resolver, scheduler, predictor or runtime view exists.

Unity MCP was checked during planning: project root `A:/project game/Pawtisserie`, Unity 2022.3.62f3, Editor idle and outside Play. These are planning observations, not implementation validation.

## 3. Proposed model and API

Use a `Cell` value type, with column increasing right and row increasing down. Give pastries and boxes stable per-run IDs so reservations and scheduled events keep referring to the same entity after movement. Implementation uses input-index IDs in separate pastry/box domains; no rules spawn new entities, so no allocation counter is needed. Event sequence numbers are instance-owned and cloned.

`BoardState` owns tray contents, cloches and primed-can state, counter boxes, queue, ordered plates/rack/stash, flights, hand/deck, inventory, outcome and deterministic random state. Store scheduled actions and the simulation clock with the simulation snapshot; prediction clones both state and pending work, not just tray contents. No mutable static gameplay state.

`LevelDefinition` is an engine-free input object. `SimulationConfig` contains validated timing and Shuffle parameters. Caller data is copied on creation; the core never mutates the source level or shared configuration. Reject malformed input with a useful reason. Standard-level validation enforces GDD invariants; tutorial exceptions are explicit, not a weakened general validator.

Public operations are proposed as create simulation, submit a card command, advance to a simulation time, run until settled, request a booster/revive effect and clone for prediction. Commands report acceptance or a rejection reason. Rejecting a command must not consume a card, inventory or RNG. Accepted commands and state events carry simulation timestamps and stable IDs.

Types are split into one file per type. Exact supporting type names can follow implementation needs without changing the behavior agreed here.

## 4. Resolution and timing

- `MoveResolver`: enumerate the card shape, clip at edges, process each cell once in the agreed order; first cloche hit removes its cover only; prime a can once; each blast schedules gravity.
- `DestinationPicker`: Ready boxes left to right, then plate, rack, overflow. Reserve capacity immediately, including airborne storage occupants.
- `StoragePump`: stash, plates, rack; landed storage gets first claim on newly Ready boxes. Compact plates and transfer FIFO rack occupants, including retargeting in-flight ones without creating duplicate arrivals.
- `BoxReconciler`: GDD section 7.6; remove queue entries from the back, shrink counter needs and move gifts without loss or duplication. Document whether `reserved` is cumulative promised occupancy (including landed pastries); do not subtract `filled` twice.
- `OutcomeChecker`: overflow marks failure immediately; delayed panel cues do not delay the model failure. At quiescence reconcile first; if that starts more activity, finish it before checking rack failure, win and out of moves, in that order.
- `Simulator`: an event queue ordered by integer time and a stable sequence number. Proposal: microseconds, checked nonnegative durations. Advancing in different frame-sized increments must produce the same results.
- `Predictor`: clone the complete simulation, apply the same command path and run it to settlement/failure; return per-pastry route events and final outcome. A pastry may visit a plate and then a box, so report its route rather than losing intermediate storage pressure.

Delayed actions refer to entity IDs, not captured cell contents. If a primed can falls before it explodes, locate that can at its current cell. Cancel or invalidate superseded flight arrivals when retargeting. Drain already accepted work after overflow as needed for the visible failure and Express collection; reject new card commands while failed.

Presentation will interpolate core events. The rules never inspect animation state, use Unity physics or depend on render-frame frequency. Fall/landing durations that affect settled status must be supplied explicitly; they cannot be inferred from a future view.

## 5. Owner decisions

The owner approved the four proposals below on 2026-11-03 ("I approved. You can start the base building phase now"). They supplement the GDD for WP01. If prototype evidence disagrees, bring that difference back rather than silently changing the spec. For point 4, use the proposed out-of-moves extension for stored-only pastries.

1. **Impact order and coincident events.** Proposal: TAP center, up, right, down, left; SWIPE left to right; POUNCE top to bottom; FLOP row-major from top left. Same-time queued events use insertion order. Run the storage pump after each state-changing event and before settled checks. Order matters because destinations reserve immediately.
2. **Command overlap.** Proposal: permit one card move at a time, accepting the next card only when settled. Initially accept boosters only when settled and playing; revive effects only for the matching failure. If the prototype permits mid-combo commands, those need explicit timing rules and tests before claiming parity.
3. **Timing baseline.** Proposal: use configurable fixed pastry flights of 0.58 s, impact delay 0.10 s, gravity delay 0.12 s, can delay 0.20 s, tail action 0.32 s, box close 0.30 s then departure 0.32 s, arrival 0.40 s and rack hop 0.34 s. For the headless core, take paw/pounce/flop pre-impact landing and gravity travel durations as explicit config inputs; test fixtures supply them. Final visual durations remain for the presentation design, with no hidden default guess.
4. **Booster ties and incomplete end states.** Proposal: Call box ties choose lowest pastry type ID, then the first eligible lane and first matching queue entry; an empty box must have no filled or reserved slots. If playing settles with empty hand/deck and only stored pastries left, treat it as out of moves as well. GDD section 8.1 explicitly mentions pastries on the tray, so this last case is a proposed extension, not an existing rule. Alternatively, retain the literal rule and establish that this state is unreachable with tests.

No decisions about GDD section 21.3 are needed for this package. Payment, revive eligibility limits, currency and rewards remain outside the effect APIs.

Additional owner decision during implementation (2026-11-03): allow queued boxes to shrink when removing whole boxes and shrinking counter boxes leaves excess capacity. Count actual queued `need` on later reconciliations, remove whole queued capacities from the back, and shrink the first remaining matching queued box if necessary. This addresses Express removing 7 pastries when all boxes of that type are queued. GDD section 7.6 was updated with this approved rule, as requested by the owner.

## 6. Build sequence after approval

Implement in order; do not start several steps that modify the same sim files in parallel.

| Step | Deliverable | Main verification |
| --- | --- | --- |
| 1 | Model, level/config validation, stable IDs, cloning and card shapes | Edge/corner shapes, explicit tutorial exceptions, clone independence, malformed inputs |
| 2 | Event queue, command consumption/dealing, impacts, cloches, can chains and gravity | Stable equal-time order, one can prime, falling primed can, repeated hits, frame-step independence |
| 3 | Destinations, flights, storage, boxes, gifts, reconciliation and outcomes | Capacity reservations, storage priority, FIFO, retargeting, gift conservation, settle/check ordering |
| 4 | Shuffle, Call box, Stash, Express and +2 FLOP effects | Seed reproducibility, no-op inventory behavior, queue swaps, overflow collection, reconciliation after removal |
| 5 | Full simulation prediction and replay scenarios | Prediction equals execution, live state/RNG untouched, identical replay events and outcome |
| 6 | Integration checks and documentation | Harness, CompileCheck, Unity EditMode tests, clean Console, intended-file review |

After each script step, run SimHarness and CompileCheck before asking Unity to recompile. Let Unity generate new asset `.meta` files and keep each with its script. Read the Console after compilation. Never enter Play through a tool.

## 7. Files and Editor requirements

| Area | Expected changes | Editor needed? |
| --- | --- | --- |
| Code | New top-level `Assets/Scripts/Sim/*.cs` for the types above and supporting state/event/command types; existing constants/IDs preserved | Only import and compilation verification |
| Tests | New `Assets/Tests/EditMode/*.cs` for model, shapes, simulator, routing, reconciliation, effects, prediction and replay | Import and Unity EditMode run |
| Documentation | This note, `PROJECT.md`, technical knowledge and test-count references in `AGENTS.md` after implementation | No |
| Scenes / prefabs / config assets / settings / packages | None in WP01 | No changes |

Repository workflow constraint: this is an unborn `master` with no commits, so there is no committed main baseline to branch from. Before code changes, establish a feature branch using the available unborn repository state and report the deviation; do not create a baseline commit without the owner's instruction. No commits or remote operations are part of this package.

## 8. Risks and definition of done

Main risks: ambiguous prototype ordering, duplicate reservations/arrivals, reconciliation deleting gifts, early settled checks, partial clones, timing differences between .NET and Unity, and booster rules interacting with failed flights. Tests should target these behaviors and conservation properties rather than mirror individual methods.

Done means:

- All five existing tests and the new rule/replay tests pass in SimHarness and Unity EditMode; CompileCheck succeeds; Unity Console has no new errors or warnings.
- Same input seed and timed command sequence produce the same event stream and outcome; different advance step sizes agree; predictor leaves the original simulation unchanged.
- Pastries and gifts are neither lost nor duplicated across routing, retargeting, box completion, reconciliation and revives. Express removal is accounted for explicitly.
- Restarting from the same definition produces a clean independent simulation. No Unity references enter `Paw.Sim`.
- Documentation reflects the actual implementation, count and remaining gaps; all new scripts have Unity-generated metadata; no incidental scene or prefab changes.

Pure simulation checks can verify rules without Play. Feel, visual timings, touch input, actual scene reloads and prototype behavioral parity remain unverified until the later view exists and the owner checks it in Play. Do not describe fixture coverage as a completed prototype comparison.

## 9. Status after the build

Steps 1–6 implemented and checked, with the Console-warning exception below. Core code is in `Assets/Scripts/Sim`, with new `CoreModelTests.cs` and `SimulationTests.cs`. The six original fixed constants and card IDs are unchanged. SimHarness passes 35/35 and CompileCheck reports 0 errors/0 warnings. Final Unity EditMode run: 35/35 passed, 0 failed, 0 skipped (MCP job `a8aa95345a5a47509017375e2f5e68b1`, 2026-11-03). Editor idle and outside Play after the run; Console returned 0 errors and the one bridge warning.

Tests cover shapes and validation, tutorial completion, clone independence, frame-step invariance, cloches, falling primed cans, reservation capacity, storage/gifts, rack and overflow outcomes, Express reconciliation, +2 FLOP, seeded Shuffle, Call box/Stash priority, prediction and replay. A 20-seed scenario matrix checks conservation and prediction versus execution at each move. A separate airborne rack test verifies retargeting without duplicate landing.

Approved deviations and implementation details:

- Queued-box reconciliation was clarified with the owner and added to GDD section 7.6; a regression covers removing 7 pastries and retaining capacities 2 + 3.
- The four original owner decisions are recorded in GDD sections 7–9 and 18, including the stored-only out-of-moves extension. Production landing/gravity durations remain explicit inputs.
- Stable IDs derive from authored input indices rather than allocation counters; runtime spawning is outside this package. Internal rule helpers are private to the assembly; public integration is through `Simulator`, its snapshots/events, and `Predictor`.
- Work is on unborn `feat/unity-core`, since no committed main baseline exists. No commit made.
- Unity refresh recovered from a connection drop but emitted one MCP-package WebSocket warning. No game-code error was reported; this prevents a claim of a warning-free Console for this session. No package fix attempted.

Documentation updated: `PROJECT.md` sections 2, 4–7; GDD approved rules/timing; technical knowledge API/invariants; `AGENTS.md` test count and branch facts. No scenes, prefabs, settings, package files, existing metadata or test assertions were weakened. New metadata is generated by Unity. No Play mode, build or external prototype comparison performed. The next package is presentation/input and prefab wiring, with a separate design note before implementation.
