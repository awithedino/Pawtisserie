# WP04 — Boosters and revives

Date: 2026-11-04. Status: **all 13 recommendations approved; ready for implementation after hierarchy Play review.** Package 4 of [`CORE-LOOP-PLAN.md`](CORE-LOOP-PLAN.md). The scene now uses the grouped layout described in [`UNITY-HIERARCHY-AND-PREFABS.md`](UNITY-HIERARCHY-AND-PREFABS.md).

**Owner direction (2026-11-04):** finish WP03, then organize the hierarchy before WP04; introduce the reusable popup manager as part of WP04. The agent arranges and wires the result for the owner's visual review, using Layout Groups where appropriate.

**Later layout clarification:** the owner's six-zone reference places boosters **above the hand**, with the hand and deck at the bottom. Use the existing `Canvas_Hud/SafeArea/Content/BoostersRoot` placeholder for the bar. This supersedes the earlier proposal to put the bar below the hand. Follow the existing 44-unit minimum touch-target convention; the prototype's 40-unit button height is a visual reference.

## 1. Goal

Play `CoreGameplay` with the booster bar and the revives working the way GDD §8–9 and §11.4 describe and the HTML prototype behaves:

- Shuffle, Call box and Stash in a bar above the hand, each with its count, or a green `AD` badge at 0.
- An inventory that lasts the whole session, so Retry, Next, restart and a scene reload don't refill it.
- Every fail panel offers its revive (Express box for overflow and rack-not-cleared, +2 FLOP cards for out of moves) next to Retry.
- The Express box is visible: it slides in, pastries hop in, and it drives off.
- Each panel opens at the GDD's moment (overflow 0.55 s after the pastry lands; win 0.7 s after the board settles).

Not in WP04: real rewarded ads, coins and prices (phase 5, GDD §16), lives (§8.4), saving the inventory to disk (§18.5), pre-level boosters (TBD), the tutorial's booster lock beyond "disabled in Level 0" (WP05), final art, sound and haptics (phase 3).

## 2. Starting point (checked 2026-11-04, verified in code)

- **Sim (WP01).**
  - `Simulator.TryUseBooster(kind, out reason)` follows the GDD rules. It is accepted only when the board is settled and playing, never in the tutorial, never at 0, and fails without spending when there is no target. A success emits `BoosterUsed`, plus `Stashed` per pastry, `BoxCalled`, or the box arrival from a swap.
  - `TryRevive` gives +2 FLOP for out of moves (only when settled), or starts the Express for overflow and rack-not-cleared (also mid-combo). It emits `Revived`, `ExpressStarted`, `Routed` to `Express` and `ExpressDeparted`. Inventory lives in the sim (`GetInventory`).
- **Runtime (WP02).** `GameController.LoadLevel` creates a fresh inventory from `ViewConfig` (2/2/1) on every load, so Retry refills it today, against GDD §9.
  - **Panel timing.** Every outcome uses the single `ViewConfig.panelDelay` (0.7 s) from the `Outcome` event and waits for the board to settle. An overflow outcome is emitted when the pastry *launches*, so its panel comes 0.7 s after the launch, and only once the whole combo has ended.
  - **Outcome panel.** It has Retry and Next only. Titles: "Orders boxed!", "Out of moves", "Shelf overflow".
- **`BoardView` doesn't handle `Stashed`, `BoxCalled`, `ExpressStarted`/`ExpressDeparted` or `Revived`.** As a result:
  - Pastries sent to `Express` fly to the overflow anchor and vanish.
  - A stashed pastry stays drawn on its plate.
  - A Call box swap replaces the lane box at once, and the swapped-out box never shows up in the queue view (the sim emits no event for it; see decision 9).
  - All stash pastries share one anchor.
- **Layout.** The migrated `Canvas_Hud` is 400×800. `HandRoot` owns the fan and deck, and `BoostersRoot` reserves the area immediately above it. Layout and spacing still need to be fitted and checked against the existing tray and hand at supported screen sizes.
- **Level 0** is `IsTutorial`, so the sim already refuses boosters there.

## 3. Prototype reference (v12 source, verified in code)

| Behavior | Prototype |
| --- | --- |
| Bar | 3 equal buttons, 40 px tall, 8 px gap, icon + name; count badge top-right (butter yellow), shows `AD` on green `#3fb37f` at 0. Disabled look: opacity .45 |
| When usable | `state === 'play'` and not the tutorial, **including during a combo** (the Unity sim accepts boosters only when settled: approved WP01 policy) |
| At 0 | Press = "rewarded ad watched": +1 of that booster at once, with a toast. It is not used in the same press |
| No target | Toast ("No cards in your hand to shuffle.", "Nothing on the plates to stash yet.", "Boxes for your waiting pastries are already here." / "Nothing is waiting for a box yet."); nothing spent |
| Shuffle | Re-rolls the hand, clears selection and aim, re-renders the hand, floating "SHUFFLED!" over the tray |
| Stash | Landed plate pastries hop into the tin (0.32 s, arc 34), floating "STASHED" by the tin |
| Call box | Swap: "CALLED!" over the lane; front of queue: "NEXT UP!" over the queue |
| Inventory | Global; `loadLevel` doesn't reset it (Retry and Next keep it) |
| Overflow panel | 0.55 s after the first overflow pastry **lands**, with a screen shake; not waiting for the combo to end |
| Rack-not-cleared panel | At once on settle, with a shake |
| Out-of-moves panel | At once on settle |
| Win panel | 0.7 s after settle, with confetti |
| Panels | Title + one body line + buttons. Revive buttons carry an `AD` tag and are free. No revive limit |
| Express | 124×50 golden box over the plates, "EXPRESS BOX" label. Slides in from the left (eases to centre), takes landed plate, rack and overflow pastries staggered 0.25 s + 0.05 s each with 0.38–0.5 s hops, waits until it has everything and 0.9 s has passed, then accelerates off to the right. Floating "EXPRESS BOX!" |
| +2 FLOP | Two FLOP cards appended to the hand, panel hidden, play resumes |

## 4. Design

### 4.1 Sim additions (`Paw.Sim`, append-only)

1. **`Simulator.TryGrantBooster(BoosterKind kind, out string reason)`** with a new event **`SimEventKind.BoosterGranted = 27`**. It adds +1 to the inventory and works while playing, also mid-combo, because it doesn't touch the board. It is refused in the tutorial and after an outcome. The grant is part of the replay (GDD §18.4), and the inventory stays in the sim, so prediction and a future solver see the same count.
2. **`SimEventKind.BoxQueued = 28`**, emitted by `SwapIn` when the box it replaces goes back to the front of the queue. Without it the view can't show the swap (decision 9).

Nothing else in the rules changes. Existing events, IDs and enum members stay as they are.

### 4.2 Pure presentation logic (`Paw.Presentation`, tested in SimHarness)

- **`OutcomePanelClock`** reads events and says when the panel is due:
  - Win: settle time + `winDelay`.
  - Overflow: the first `Landed` with destination `Overflow` + `overflowDelay`. It does **not** wait for the combo to end.
  - Rack-not-cleared and out of moves: outcome time + `settledFailDelay`.
  - `Revived` and `ExpressStarted` reset it, so a second fail after a revive gets its own timing.
  - It replaces `GameController.outcomeAt`.
- **`BoosterButtonState`** (a small struct from inventory, outcome, settled, tutorial and panel): `Count`, `ShowsAd`, `Interactable`. It keeps the bar's rules out of the view.

### 4.3 Session inventory (`Paw.Runtime/Services`, pure, harness-compiled)

- `BoosterWallet` holds the three counts. A static `SessionInventory` owns one wallet for the session:
  - It is filled from `ViewConfig` (2/2/1) the first time.
  - It is reset by a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` hook, so every Play entry starts fresh. Domain reload is on anyway; the hook keeps it right if that changes.
- `LoadLevel` passes the wallet's counts into the new `Simulator`. After every `BoosterUsed` or `BoosterGranted`, the wallet copies the sim's counts back. A Retry, Next, restart or scene reload therefore keeps what was spent or granted.
- No disk save (phase 5).

### 4.4 Booster bar (`BoosterBarView`, `BoosterButtonView`)

- **Look.** Three buttons in `BoostersRoot` above the hand, inside the HUD safe area. Each has a placeholder icon, a name and a count badge. Height, gap, colours (badge butter, `AD` green, disabled alpha .45) and the label strings are serialized fields on the prefab; touch targets are at least 44 units high.
- **Press.**
  - At 0, with `ViewConfig.simulateRewardedAds` on: `TryGrantBooster`.
  - Otherwise `TryUseBooster`. On refusal, the button shakes, nothing is spent, and no toast appears (GDD §11.1: no instruction text outside the tutorial).
- **Disabled** while a panel is open, in the tutorial level and after an outcome. During a combo, see decision 2.
- **Shuffle** also cancels the aim and the drag (`CancelAim`). The new hand comes in with WP03's deal-in.

### 4.5 Board views

- **Stash.** The `Stashed` event hops the pastry from its plate to a stash slot (0.32 s, arc 34, both in `ViewConfig`), and the plates compact.
  - The tin shows while it holds pastries. Its slots are authored anchors (`stashSlots`, 6; any extra stack on the last one).
  - A `Routed` event out of the stash in the same tick starts from wherever the pastry is, so it works without special cases.
- **Call box.**
  - `BoxCalled` moves that box to the front of the queue view and pulses the first mini box.
  - `BoxQueued` hides the lane box and adds it to the front of the queue view.
  - The arriving box then plays the existing `Arrive`.
- **Express (`ExpressView`, `Express` prefab under the board).**
  - On `ExpressStarted` it slides in from the left over the plates.
  - `Routed` to `Express` flies to one of its 5 authored slot anchors (prototype spacing 15).
  - `Landed` releases the pastry with a small bounce.
  - On `ExpressDeparted` it drives off to the right.
  - Slide-in and drive-off times are `ViewConfig` fields. Sim timing is unchanged (decision 10).
- **Floating feedback** ("SHUFFLED!", "STASHED", "CALLED!", "NEXT UP!", "EXPRESS BOX!"): one pooled placeholder label, with strings and colours on the prefab (decision 12).

### 4.6 Panels (`GameHud`)

- **Popup framework.** Add a reusable `UiPopup` and `PopupManager` using the layout standard: popup roots own their serialized parts, a shared dim and a stack live under the popup canvas, pop animations use unscaled time, and back/Escape handles the top popup according to its dismissal policy. Outcome popups remain modal until Retry, Next or an accepted revive. Initialize inactive popup components before setup, release event subscriptions on disable, and preserve the game-specific interaction blocking and approved panel timing below. Reuse an outcome popup for the fail and win variants where it fits; do not create extra distinct panels just to exercise the manager.
- **Revive button.** The panel gets a third button for the revive, with a label and an `AD` tag:
  - "Express box" for overflow and rack-not-cleared.
  - "+2 FLOP cards" for out of moves.
  - It is hidden on the win panel and once the revive limit is reached.
- **Press.** `TryRevive` hides the panel, resets the panel clock and counts the revive for this attempt.
- **Titles and body lines** are serialized strings (decision 12).
- **Panel delays** become three `ViewConfig` fields: `panelDelay` stays as the win delay (same name, value 0.7), plus new `overflowPanelDelay` (0.55) and `settledFailPanelDelay` (0).

## 5. Files and Editor work

| File | Kind | Editor needed |
| --- | --- | --- |
| `Sim/Simulator.cs`, `Sim/BoosterEffects.cs`, `Sim/SimEventKind.cs` | code (grant, `BoxQueued`) | no |
| `Presentation/OutcomePanelClock.cs`, `Presentation/BoosterButtonState.cs` | code | no |
| `Runtime/Services/BoosterWallet.cs`, `Runtime/Services/SessionInventory.cs` | code | no |
| `Runtime/Views/BoosterBarView.cs`, `BoosterButtonView.cs`, `ExpressView.cs`, `FloatTextView.cs` | code | no |
| `Runtime/UI/UiPopup.cs`, `Runtime/UI/PopupManager.cs` and the outcome popup view | code | no |
| `Runtime/Views/BoardView.cs`, `GameHud.cs`, `Flow/GameController.cs`, `Config/ViewConfig.cs` | code | no |
| `Tests/EditMode/BoosterReviveTests.cs` (new), `EventContractTests.cs` (extended) | tests | no |
| `Prefabs/Game/BoosterButton.prefab`, `Express.prefab`, `FloatText.prefab` (new); `Hud.prefab`, `Board.prefab` and popup prefabs (bar, revive button, stash slots, Express, layout) | prefab | **yes** |
| `Data/Config/ViewConfig.asset` (new fields saved) | asset | **yes** |
| `Scenes/CoreGameplay.unity` | scene | no change expected (prefab instance) |
| Placeholder sprites (golden box, tin) only if the existing ones don't fit | asset | yes |

## 6. Build order (after the go and after WP03)

1. Sim: `TryGrantBooster`, `BoosterGranted`, `BoxQueued`, plus tests. SimHarness and CompileCheck.
2. `OutcomePanelClock`, `BoosterButtonState`, `BoosterWallet`, `SessionInventory`, plus tests.
3. Popup framework, views and controller wiring (bar, stash, Call box, Express, float text, revive button, panel clock). CompileCheck.
4. Editor (needs Unity MCP): prefabs, migrated scene wiring and layout, re-save `ViewConfig.asset`, recompile, Console, EditMode run.
5. Edit-mode renders at 400×800 (and the WP03 sizes): the bar with counts and with `AD`, each panel with its buttons, the Express mid-collection, the stash tin holding pastries.
6. Docs: this note's "Status after the build", `PROJECT.md`, the test count in `AGENTS.md`.

## 7. How it is checked

Automated:

- Grant and replay determinism; grant refused in the tutorial and after an outcome.
- `BoxQueued` order on a swap.
- Panel clock: win 0.7 s after settle; overflow 0.55 s after landing even mid-combo; rack and out of moves at settle; reset on revive.
- The wallet survives three `LoadLevel`s and resets on the hook.
- Button state for every combination.
- Every existing test stays green.

Owner Play checklist:

1. Each booster with a target: Shuffle (new hand, aim cleared), Stash (plates hop into the tin, tin pastries go home first), Call box (both the swap and the front-of-queue case).
2. Each booster without a target: shake, count unchanged.
3. Count at 0: `AD` badge; press gives +1, and the next press uses it.
4. Inventory after a Retry, the restart button, Next, and stopping and re-entering Play (back to 2/2/1).
5. Overflow: the panel opens about half a second after the pastry lands beside the rack. Express collects plate, rack and overflow pastries, drives off, and play continues.
6. Rack not cleared: the panel opens on settle; Express works.
7. Out of moves: +2 FLOP appears in the hand and can be played.
8. Win panel about 0.7 s after the last box; Next works.
9. The bar is disabled in Level 0 and under a panel; during a combo as decided.
10. After a revive, a second fail shows its panel again with the right timing.

## 8. Owner decisions

On 2026-11-04 the owner approved the 13 recommendations presented in the conversation. Decision 2 uses the colorful-button alternative recommended there; decision 6 uses unlimited revives as the prototype default.

1. **Scope.** Proposal: as sections 1 and 4.
2. **Boosters during a combo.** The sim refuses them until the board settles (approved WP01 policy); the prototype allows them. Buttons keep their normal colorful look, like the cards since WP03, and a press only shakes.
3. **Booster with no target.** Proposal: shake, nothing spent, no toast (GDD §11.1). Prototype: a toast message.
4. **`AD` at 0.** Proposal: as in the prototype, a press grants +1 at once (simulated ad), behind `ViewConfig.simulateRewardedAds` (on), using the new sim grant command and event. Alternative: buttons simply disabled at 0 (the core-loop plan's wording) until the ads SDK exists.
5. **Revive payment.** Proposal: free, with an `AD` tag on the button (prototype), until ads and coins exist.
6. **Revive limit per attempt (GDD TBD).** Use a `ViewConfig.revivesPerAttempt` field, with 0 = unlimited as the approved prototype default. The final limit and payment policy remain undecided.
7. **Panel timing.** Proposal: win 0.7 s after settle (GDD); overflow 0.55 s after the first overflow pastry lands, without waiting for the combo to end (GDD and prototype; the sim keeps running under the panel and Express handles the pastries still flying); rack-not-cleared and out of moves at once on settle (prototype, `settledFailPanelDelay` 0). No screen shake or confetti yet (phase 3 juice).
8. **Inventory lifetime.** Proposal: the whole Play session, kept across Retry, restart, Next and scene reload, reset on each Play entry; no disk save before phase 5.
9. **Call box swap event.** Proposal: add `BoxQueued` to the sim (append-only) so the view shows the swapped-out box going back to the queue. Alternative: the view infers it from `BoxArriving` on an occupied lane (no sim change, but the view then guesses sim state).
10. **Express timing.** Proposal: keep the sim's timing (all collected pastries hop at once over `RackHop`; reconciliation after it leaves, as the GDD says) and make the drive-off visual only, so input resumes at `ExpressDeparted`. The prototype staggers the hops, counts the drive-off as part of the combo, and reconciles at revive start. The differences are recorded for the WP06 parity table.
11. **Stash tin.** Proposal: 6 authored slot anchors on the prefab, extras stacked on the last; the tin shows only while it holds pastries (GDD §11.1).
12. **Texts.**
    - Proposal: panel titles as GDD §11.4, except "Out of moves" stays until the mascot's name is decided.
    - One short body line per panel, from the prototype, so overflow and rack-not-cleared can be told apart.
    - The floating feedback words as placeholder labels.
    - All strings are serialized on the prefab. Alternative: no body lines and no floating words until phase 3.
13. **Bar layout.** The original approved proposal placed a 40-unit bar below the hand. The owner's later six-zone reference places it above the hand instead; use `BoostersRoot` and at least 44-unit touch targets, with the approved 8-unit gap as the starting point. Retain hand-adjustable placement and check the 400×800 reference at supported screen sizes. Any change to the owned `ViewConfig` dimensions requires the owner's approval.

## 9. Risks

- **Layout.** Fitting the bar above the hand may touch WP03's safe-area and fan work. The final positions are authored on the prefab and must be checked in renders at every WP03 size.
- **Overflow panel over a running combo.** Pastries keep landing and boxes keep cycling under the panel. The panel blocks input, but the clock and views must keep ticking. Retry must clear in-flight views (`board.Clear` already does).
- **`SessionInventory` is a static.** It needs its reset hook and must never hold Unity objects. It survives scene loads on purpose.
- **Grant event in replays.** Old replays have no grants, so this doesn't break them. Prediction clones carry the counts, which is harmless.
- **Express slots.** In-flight overflow pastries are re-routed mid-air. Their arc restarts from their current position, so a visible kink is possible (to check in Play).
- **Editor.** Steps 4–5 need the Unity MCP bridge, which isn't connected in the session that wrote this note.
