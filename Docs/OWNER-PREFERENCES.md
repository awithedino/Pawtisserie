# Owner preferences and working agreement

Everything the owner (Ari, the solo developer) has told the agents about **how to work on Pawtisserie**. It is the "people" half of the documentation; `PROJECT.md` says what exists, `Docs/Design/TECHNICAL-KNOWLEDGE.md` says how it works and why, `AGENTS.md` is the operating manual. Where this file and the generic parts of `AGENTS.md` / `CLAUDE.md` differ, **this file wins** (it is newer and specific to this owner). Last updated 2026-11-03.

The rules in section 2 were carried over from the owner's other project (Sand Drop) on 2026-11-03, at the owner's request. Items tied to Sand Drop's machine or tooling are marked and need re-checking here.

## 1. Who and what

- One person owns everything (code, art, content, settings). "Ask the owner" means ask Ari. There is no review team.
- **Pawtisserie** is a hybrid-casual sorting puzzle for phones, meant to ship (store, ads, IAP; see `Docs/Design/GAMEPLAY-SPEC.md` sections 2 and 16). Unity `2022.3.62f3`, Built-in render pipeline, 2D, portrait, iOS and Android.
- The game design lives in `Docs/Design/GAMEPLAY-SPEC.md` (the GDD, v1.0 of 2026-11-03). Section 7 is the exact rules spec. **TBD** values are open decisions: ask, do not invent.
- The owner tests the game **in Play mode on their own machine** and reports back (screenshots, short notes). The agent builds, checks what can be checked without Play, and says exactly what is left for the owner to look at.
- The HTML prototype (v12) is the behavior reference for the rules: <https://claude.ai/artifact/FoZ1cVV1zGEUpwYYNs2Tyj>.
- Sand Drop (`A:\project game\Sand Drop`) is the owner's other game and the **source of this project's structure** (docs, rules, tools, layering). It is read-only reference: its game logic does not transfer.

## 2. The standing rules

Each rule says what the owner asked for and why, so a new agent can apply it to cases the list does not cover.

| # | Rule | Why / how to apply |
|---|---|---|
| 1 | **Clarify before acting.** If a request is unclear or has two sensible readings, ask first (name what is unclear and why it blocks you). If you have to proceed on a reading, say which reading in the first lines of the reply. | A wrong guess in Unity lands in a scene, prefab or asset, where it is hard to see and undo. |
| 2 | **Plan major work, and for a work package write the design note first.** New systems, several scripts, prefabs and scenes, assembly or package changes: write the plan (scope, steps, files marked as needing the Editor, risks, definition of done) and get it validated. For a work package: "show me the design note first, then build", then **wait for the go**. Simple low-risk changes need no plan. | The owner reviews design before code. Notes live in `Docs/Design/<ID>-<NAME>-DESIGN-NOTE.md`. |
| 3 | **Validate, then say what you validated.** Compile (the Editor, or `Tools/CompileCheck`), pure tests (`Tools/SimHarness`), edit-mode renders for looks. Label every claim: *verified in code*, *verified by test*, *verified in an edit-mode render*, *unverified (needs the owner in Play)*. Never write "tested" for a static read. | Many looks and all feel/timing cannot be seen without Play. Say so plainly. |
| 4 | **No Play mode through any tool. Ever.** Do not enter Play via Unity MCP, the CLI or anything else. | Sand Drop rule ("the RAM bubble is gonna implode"); the owner may enter Play themselves. Re-check whether it applies on this machine for this project. |
| 5 | **While the owner is in Play: read only.** Do not edit scripts (a recompile ends the session), prefabs, scenes or assets. Reads and preview renders in a preview scene are fine. When a change is needed, finish the analysis, then ask the owner to stop Play and wait for the answer. | Editing during Play is lost or breaks their session. |
| 6 | **No commits unless the owner says so; never push, open PRs or merge.** A grant such as "you can commit all the works first" covers the work that existed at that moment only. Keep out of commits: `.idea/` and Unity's incidental re-saves of assets nobody meant to change. Commit messages end with the Co-Authored-By line the session gives. | The owner decides what goes into history and when. |
| 7 | **No package, Unity-version or project-setting changes unless asked.** Adding an asset (a shader, a material, a prefab, a texture reference) for the task is fine; settings and packages are not. | Settings changes ripple through the whole project. |
| 8 | **Respect scope. No drive-by changes.** Do the asked thing. If you notice something else, mention it (and note it in `PROJECT.md`), do not silently fix it. Keep diffs reviewable. | Easier review, no surprises in prefabs and scenes. |
| 9 | **Raise problems right away.** A blocker, a surprise, a rule you cannot satisfy: say it immediately, with what you tried. Do not work around it silently. Subagents are fine for audits and read-only research, but their findings are summarised to the owner. | The owner wants to decide, not discover later. |
| 10 | **Honesty about state.** If a check was skipped, say it. If a number is a guess, say "estimate". If a fix is partial, say what you did instead and why. | Trust. |
| 11 | **Hand-adjustable values beat computed ones.** The owner tunes visuals by hand in the Scene view, the inspector and the config assets. Do not hide a look in code that calculates it. Put sizes, offsets, angles and timings on prefabs or in config assets with a `[Tooltip]`, and tell the owner where. When the owner hand-authors a prefab, **remove the code that computed it** and never overwrite their values. | Sand Drop's chain-band episode: "remove all the code using to calculate, and I will hand-adjust myself". |
| 12 | **Show, do not describe, for visuals.** Render in edit mode and, when the owner gave a reference image or the HTML prototype, put the render **next to the reference**. Say what the render cannot show (text, tweens, timings). | The owner works from screenshots. |
| 13 | **Do not break what the owner already tuned.** Before editing a prefab, read what is there; the owner edits prefabs and the scene by hand between sessions. Re-read a file that changed on disk. | Their values are the truth. |
| 14 | **Keep `PROJECT.md` true, in the same pass.** Every change that makes a line false updates it. New knowledge goes in `PROJECT.md` (state) or `Docs/Design/TECHNICAL-KNOWLEDGE.md` (understanding), a new package gets a design note, preferences go here. | `AGENTS.md` ground rule 1. |
| 15 | **Teach briefly.** One or two sentences of why, tied to a principle (why Awake did not run, why a static survived), not essays. | `CLAUDE.md` section 9. |
| 16 | **Comments stay short.** One-line `<summary>` only on types and non-obvious members; `//` only for a real WHY or trap. No dates, owner history, work-package names, "unverified" notes or dividers in code: those belong in `PROJECT.md` and the design notes. | Sand Drop's cleanup audit. |

## 3. How a request is usually phrased, and what it means

| The owner writes | Do |
|---|---|
| "No Play mode, no commits." | Rules 4 and 6 for this task. Edit, verify with harness/compile/renders, report, stop. |
| "Start WP... (...): show me the design note first, then build." | Write the design note (rule 2), then build in the order of its steps. Packages that touch the same sim files run strictly one after another. |
| "I will check X in Play and report back." | Do nothing in Unity. Do not start the next package or commit. Wait. Offer what to look at (a short list). |
| "While I am testing, you may create the next document." | Documents only. No script, prefab, scene or asset edits (rule 5). |
| "Keep the proposals in the note" / answers to a numbered open-points list | Treat as the go for those decisions; record them in the note ("Owner decisions"). |
| "Show me a render." | Edit-mode render, saved in the scratchpad, shown, with the limits named. |
| A screenshot with a short complaint | Find the cause first (read code, render), say it in two or three sentences, then ask only what you must. Do not change things the screenshot does not complain about. |
| "Push X down more", "10% more", "20% closer" | Numeric nudges: apply the number, say the resulting value, and if it cannot be reached without a side effect say so and what you did instead. |

## 4. Working protocol for a work package

1. Read: the GDD sections the package touches, `Docs/Design/TECHNICAL-KNOWLEDGE.md`, the code you will touch.
2. Check the facts in the data again; do not trust a plan blindly.
3. Write the design note (what exists / what is missing / rules / sim changes / presentation changes / how it is checked / open points for the owner / risks). Put open points as a **numbered list with a proposal each** so the owner can answer "keep all".
4. After the go: pure logic first (sim, tests), then presentation cues, then views, then prefab wiring, then the scene (last and minimal). Run `Tools/SimHarness` and `Tools/CompileCheck` after each step.
5. Edit-mode renders for looks; name what was not seen.
6. Update `PROJECT.md`, the design note (a "Status after the build" section, including deviations) and the test counts in `AGENTS.md`.
7. Final reply: what changed (files, with links), what is verified and how, what the owner should look at in Play, what is not done. Short. No commit.

## 5. Decisions the owner has made (newest last)

- 2026-11-03: Category, theme, mascot, pastries, hand and deck, shelf and rack, boosters and revives are fixed by the GDD decisions log (`Docs/Design/GAMEPLAY-SPEC.md` section 21.2). Open questions are its section 21.3.
- 2026-11-03: Pawtisserie takes Sand Drop's documentation structure, rules, assembly layering and fast-check tools. Not carried over yet: the runtime shell (Boot/Splash/MainMenu/Game scenes, popups, save, lives, coins), because it needs the Editor and package changes.
- 2026-11-03: Code prefix is **`Paw`** (namespaces `Paw.*`, asmdefs `Paw.*`, Editor menu root `Paw/`, harness define `PAW_HARNESS`).
- 2026-11-03: The GDD lives in the repo at `Docs/Design/GAMEPLAY-SPEC.md`.
- 2026-11-03: Unity stays on `2022.3.62f3` with Built-in 2D and the legacy Input Manager for now. Changing any of that is a project-setting or package change (rule 7).
- 2026-11-04: Sand Drop's scene hierarchy, prefab nesting and UI layout conventions (its 2026-10-28 UI rules: root script, parts by field, grouped parts under layout groups, no name lookups) are adopted for Pawtisserie as `Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md` and `Docs/Design/UI-LAYOUT-SYSTEM.md`. They apply to new work; the `GameRoot` migration is a separate work package (design note, then the owner's go).

## 6. Environment practices (what works on this machine)

- **From Sand Drop, not yet checked for Pawtisserie:** Unity MCP can drop while the Editor recompiles unless it is focused; a failed call right after a script edit usually means "still compiling". Focus loop (PowerShell):
  ```powershell
  $ws = New-Object -ComObject WScript.Shell; for ($i=0; $i -lt 6; $i++) { [void]$ws.AppActivate('Pawtisserie'); Start-Sleep -Seconds 4 }
  ```
- Several Unity editors can be open; check `Application.dataPath` ends in `Pawtisserie/Assets` before editing.
- Avoid `??` / `?.` on Unity objects (a destroyed `MonoBehaviour` throws `MissingComponentException`).
- Prefer a material **asset** over setting TextMeshPro outline properties in code (setting them in edit mode instantiates a material and logs errors).
- Unity 2022.3 has no `Awaitable` (2023.1+); use coroutines or `destroyCancellationToken`.
- Temporary files and renders go to the session scratchpad, not the repo.
- Tool projects (`Tools/SimHarness`, `Tools/CompileCheck`) need the **.NET SDK 8** (installed: 8.0.121) and, for `CompileCheck`, an Editor-built `Library/`.

## 7. Report format the owner likes

Short, in plain sentences: what is now true, what is verified and how, what needs the owner in Play, what is not done. File names as links. Numbers when you changed a value. No long recaps of earlier work. A numbered list of questions when decisions are needed, each with a recommendation. Say plainly when a request was only partly met.
