# Core loop: implementation plan

Date: 2026-11-03. Status: package split and all WP02 section 10 proposals approved; WP02 in progress. Later packages still need their own design notes and go-ahead. This file is the step list from "WP01 rules exist" to "the GDD §4.2 loop plays in Unity like the HTML prototype". Each step is one work package with its own design note (`Docs/Design/<ID>-<NAME>-DESIGN-NOTE.md`), written first and built after the owner's go (`Docs/OWNER-PREFERENCES.md` rule 2).

## 1. The loop being built (GDD §4.2)

Pick a card → aim on a tile (preview + badges) → drop: the cat performs the move → every hit pastry launches → box, plate, rack or overflow → boxes fill, close, leave and the next arrives → the combo settles → rack check, win, out of moves, or back to picking a card. Fail leads to a revive offer or Retry; win leads to the next level.

GDD §20 phase 2 exit criterion: **prototype levels play identically to the HTML build**, with placeholder art. Everything below serves that; final art, juice and audio are phase 3.

## 2. Work packages

Packages run strictly one after another (they share sim and runtime files). Each ends with SimHarness + CompileCheck + Unity EditMode green, an edit-mode render where there are visuals, an owner Play checklist, and `PROJECT.md` updated.

| WP | Name | Delivers | Exit evidence |
| --- | --- | --- | --- |
| 01 | Unity core (**done**) | `Paw.Sim`: model, rules §5–9, event-queue simulator, boosters/revives, full-move predictor; 35 tests | 35/35 SimHarness and EditMode; CompileCheck clean |
| **02** | **Core loop playable** | Review fixes in the sim; sim→view event contract; `Paw.Presentation` (event cursor, flight math, preview badges, tap-aim state machine); `Paw.Runtime` views for tray, boxes + queue, plates, stash, rack, overflow, flights, gravity, hand (tap to select), deck count, aim preview, win/fail panel with **Retry** and restart; prototype levels 0–3 in memory; placeholder sprites; `GameRoot` prefab; `CoreGameplay` scene | The owner can play Level 0–3 with mouse in Play: select, aim, preview, drop, watch the combo, win or fail, retry |
| 03 | Input and hand feel | GDD §10: drag a card after the threshold, preview at the pointer without a vertical offset, release to play, slide aim, release-outside cancel, Escape, real fan (tilt/dip), deal-in animation; simple cat move markers (paw, tail, cat) on the real timings; authored spacing inside the safe area as approved in WP03 | Pure input tests; renders at 360×640, 400×800, 430×932; owner touch check on a phone |
| 04 | Boosters and revives | Booster bar (Shuffle, Call box, Stash; disabled at 0 and in the tutorial), Express box and +2 FLOP revive buttons on the fail panels, overflow panel delay 0.55 s after landing, win panel 0.7 s, session inventory kept across Retry/Next/scene reload | Panel timing tests; owner Play checklist for every fail type and its revive |
| 05 | Level 0 tutorial and level flow | Scripted tutorial (GDD §13.2: card locks, glowing tile, touch-and-release drop, coach bubble, hand pointer, Skip), "Tutorial done" → level 1, Next level, rack hidden until needed per GDD §13.3 only if the owner wants it now | Tutorial step tests; owner plays it cold |
| 06 | Parity and performance pass | Side-by-side comparison with the HTML build on levels 0–3 (same layouts, same moves); allocation-free sim advance and pooled views at ~50 pastries; frame-time profile | A written parity table (match / differs / why); Profiler capture; open differences brought to the owner |

After WP06 the phase 2 exit can be claimed (or the remaining differences are listed as owner decisions). Phase 3 (vertical slice: final art, cat animation, juice, audio, haptics) and the app shell (Boot/menu/save) come after.

## 3. Why this order

- **WP02 makes the loop visible end to end before polishing any part of it.** Tap-to-aim with the mouse is enough to play; drag and touch can wait. A playable loop is where wrong rules and wrong timings show up, so it comes first.
- **Preview is in WP02, not later**, because GDD pillar 2 ("the preview always shows the outcome before you commit") is part of the core choice. WP03 updates the touch interaction to release-to-play.
- **Revives and boosters (WP04) come after input (WP03)** because they are help systems on top of the loop; the loop already ends in a real outcome with Retry.
- **Performance work waits until WP06** except for the rules that are cheap to follow from the start (no per-frame `State` clone, pooled pastry views, no LINQ in `Update`). Optimising the sim's internal allocations before the views exist would be guesswork.

## 4. Decisions this plan needs from the owner

Most are asked in the WP02 note (section 10). Plan-level ones:

1. **Package split.** Proposal: the six packages above, replacing the earlier single large WP02 (presentation + input + boosters + revives + tutorial + scenarios + editor render tooling in one package). The earlier note is superseded; a copy was kept outside the repo.
2. **Level storage.** Proposal: stay in memory (C# definitions and a port of the prototype generator) until phase 4; the JSON vs ScriptableObject choice is not needed for the core loop.
3. **Parity source.** Proposal: the HTML prototype's code is the reference where the GDD is silent, and every difference found is brought to the owner before the Unity rule changes (as the WP02 review already does).
