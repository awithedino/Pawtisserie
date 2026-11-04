# AGENTS.md: Instructions for AI Agents

This file is the operating manual for AI coding agents (Claude Code, Codex, Cursor, Copilot, etc.) working in this Unity repository. Humans should start with [README.md](README.md). The project reference (what the game is, how it is built, current values, known issues, roadmap) lives in **[`PROJECT.md`](PROJECT.md)**. This file assumes you will read the parts of it you need.

Claude Code also loads the rule files in [`.claude/rules/`](.claude/rules/). Other agents should read them before writing C# or touching Unity assets. Where they differ from this file, **this file describes the team standard**, except that **[`Docs/OWNER-PREFERENCES.md`](Docs/OWNER-PREFERENCES.md) wins over both** (the owner's standing rules: read it first).

How this repository is organised follows the owner's other project, Sand Drop: `PROJECT.md` (state), `Docs/Design/` (specs and design notes), `Docs/OWNER-PREFERENCES.md` (how to work with the owner), `.claude/rules/` (path-scoped rules), `Tools/` (fast checks without the Editor).

## Project facts

Pawtisserie is a hybrid-casual sorting puzzle for phones: a cat plays move cards onto a baking tray and the pastries it hits fly into boxes. The design source of truth is the **Pawtisserie GDD v1.0** (2026-11-03), at [`Docs/Design/GAMEPLAY-SPEC.md`](Docs/Design/GAMEPLAY-SPEC.md). Section 10 of this file lists the rules that come from it.

Facts below were checked on 2026-11-03 (files and the live Editor through Unity MCP). Rows marked **TBD** are open decisions: don't fill them in yourself.

| Fact | Value |
|---|---|
| Unity version | `2022.3.62f3` (matches `ProjectSettings/ProjectVersion.txt` and the running Editor). C# 9; `Awaitable` is **not** available (it is 2023.1+), so use coroutines or `destroyCancellationToken` |
| Render pipeline | Built-in · 2D (no SRP asset assigned, no URP package; `com.unity.feature.2d` installed) |
| Input | Legacy Input Manager (`activeInputHandler: 0`); the Input System package is not installed. Touch + mouse both needed (GDD §10) |
| Target platforms | iOS and Android phones, **portrait, one hand**, 60 fps on mid-range Android (GDD §1, §18.6). The Player setting is currently *Auto Rotation*; changing it is a Project Settings change, so ask first |
| Scenes | `Assets/Scenes/SampleScene.unity` only (default template, in Build Settings). Boot scene **TBD** |
| Content loading | **TBD.** GDD §18.1 allows `LevelData` as ScriptableObject or JSON. Not Addressables yet |
| Key packages | TextMeshPro 3.0.7, Unity UI 1.0.0, 2D feature set 2.0.1, Timeline 1.7.7, Test Framework 1.1.33, Unity MCP (CoplayDev, `v10.0.0`). Planned and **not installed**: LevelPlay (ads), Unity IAP, analytics |
| Assemblies | `Paw.Sim` and `Paw.Presentation` (pure C#, `noEngineReferences`), `Paw.Runtime` (views and flow; Sim, Presentation, UnityEngine.UI) and `Paw.Tests.EditMode`. `Paw.Editor` is not created yet: add it with its first script. Prefix is `Paw` (menu root `Paw/`, harness define `PAW_HARNESS`) |
| Tests | EditMode: `Paw.Tests.EditMode`, 69 cases expected (rules/model/simulation, review regressions, event contract, prototype levels, presentation, runtime adapters, pointer gestures, fan and safe area). Run with MCP `run_tests`, the Test Runner window, or with the Editor closed `unity test . --mode EditMode` (§6 "Run tests") |
| Fast checks without the Editor | `Tools/SimHarness` (`cd Tools/SimHarness && dotnet run -c Release`, runs the pure tests in seconds, NuGet NUnit 3) and `Tools/CompileCheck` (`cd Tools/CompileCheck && dotnet build -c Release`, compiles all scripts against Unity 2022.3.62f3). Need .NET SDK 8. See `.claude/rules/unity-tests.md` |
| Editor automation | Unity MCP connected: instance `Pawtisserie@02466c73593f71c9`, editor idle and ready. WP01 verification: 0 Console errors, 1 MCP WebSocket warning on refresh/reconnect (see `PROJECT.md` §6). Unity CLI `1.0.0-beta.13` is installed, but `com.unity.pipeline` is not, so `unity command` can't drive the Editor |
| Editor settings | Asset Serialization **Force Text**; Enter Play Mode Options **off** (domain reload on); `.meta` files visible |
| Version control | Git + LFS (`git-lfs 3.7.0`; `.gitattributes` has LFS rules for textures, audio, models). Local branch `feat/unity-core`, **no commits yet** or committed main baseline. The UnityYAMLMerge driver is **not configured** (see §2.3; the exe is at `C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/Tools/UnityYAMLMerge.exe`) |
| People and ownership | Ari, solo: design, code, content and project owner. Art/audio sourcing **TBD** |
| Project reference doc | `PROJECT.md` (state, dated) · `Docs/Design/TECHNICAL-KNOWLEDGE.md` (how it works, traps; stub) · **`Docs/OWNER-PREFERENCES.md` (how to work with this owner: read it first)** |

---

## 1. Ground rules

These rules are not optional. Unity projects break quietly: a lost GUID, a stray scene save, or a serialized value that silently reverts doesn't fail to compile. It ships a broken game, and it is often noticed days later by someone else.

**You MUST:**

- **Keep `PROJECT.md` true.** If your change makes a statement in it false (a value, a class, a rule, a known issue, a decision, the roadmap), update it **in the same commit**. Say in your final report which sections you changed, or why none needed it.
- Read the relevant parts of `PROJECT.md` and the code before writing. Match existing patterns.
- **Prefer the live Editor over raw files.** When an Editor is connected (Unity MCP, or the Unity CLI with the Pipeline package), inspect and change scenes, prefabs and assets through it. Hand-edit `.unity` / `.prefab` / `.asset` / `.meta` YAML only when no Editor is reachable, and say so ("no live Editor, editing the file directly").
- Keep every asset's **`.meta` file with it**. Move and rename assets in the Unity Project window, or move the `.meta` alongside, so the GUID survives.
- Work on a branch from an up-to-date main branch (§8). Never commit to the main branch.
- Before every commit: the project compiles, the Console shows **no new errors or warnings**, relevant tests pass, and `git status` shows only the files you meant to change (§7).
- Build new things as **prefabs**. Keep scene edits minimal, and list every scene change in your report.
- State assumptions and uncertainties. Ask before acting when the request is ambiguous. Label claims as verified in code, verified in the Editor/Play mode, or unverified.

**You MUST NOT, unless the user explicitly asks for that specific action in this conversation:**

- Push, open PRs, merge into the main branch, delete branches, rebase shared branches or force-push.
- Change the Unity version (`ProjectSettings/ProjectVersion.txt`), or add, remove or upgrade packages (`Packages/manifest.json`, `Packages/packages-lock.json`, embedded packages).
- Change Project Settings: Player, Quality, Graphics, Input, Tags & Layers, Physics/Physics 2D, Time, **Script Execution Order**, Editor (serialization mode, Enter Play Mode Options), build profiles, scripting defines.
- Hand-edit `guid` or `fileID` values, delete or regenerate `.meta` files, or delete assets/scripts that something may reference (missing references don't fail the build).
- Rename or reorder serialized enum members, rename serialized fields without `[FormerlySerializedAs]`, or rename assets whose names are used as keys (saves, `Resources.Load` paths, Addressables addresses).
- Change values in shared configuration assets (balance/tuning ScriptableObjects, settings assets) that have an owner. Propose the diff instead.
- Save a scene with incidental changes (moved objects, toggled actives, open UI, Play-mode leftovers).
- Disable, skip or weaken a test, validator or `OnValidate` check to make something pass.
- Touch `.gitattributes`, `.gitignore`, LFS tracking, or run `git lfs migrate`.
- Commit `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build*/`, `*.csproj`, `*.sln` or other generated files.
- Make release builds, sign builds, upload builds, or touch keystores, provisioning profiles, signing keys or service credentials.

Read-only actions are always fine: `git status/diff/log/show`, reading files, Editor inspection through MCP (hierarchy, components, Console), `unity --version`, `unity editors`, `unity projects info`.

## 2. Machine setup (do this first)

1. **Install the exact Unity version** in `ProjectSettings/ProjectVersion.txt`, with the modules for the target platforms. Unity Hub, or the Unity CLI:
   ```bash
   unity projects info . --format json                 # read "editorVersion"
   unity editors --installed --format json             # is it installed?
   unity install <version> --module <webgl|android|ios|…> --yes --accept-eula
   ```
   Opening a project with a different version upgrades it. Don't do that unasked.
2. **Git LFS** (if `.gitattributes` uses `filter=lfs`):
   ```bash
   git lfs install
   git lfs pull
   ```
3. **UnityYAMLMerge driver.** If `.gitattributes` routes scenes/prefabs/assets to `merge=unityyamlmerge`, each clone needs the driver configured, or git silently falls back to a text merge that can corrupt scenes. Use your Editor's path:
   ```bash
   # Windows: C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe
   # macOS:   /Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/Tools/UnityYAMLMerge
   # Linux:   ~/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge
   git config merge.unityyamlmerge.name "Unity SmartMerge"
   git config merge.unityyamlmerge.driver "\"<path-to-UnityYAMLMerge>\" merge -p %O %B %A %A"
   git config merge.unityyamlmerge.recursive binary
   git config --get merge.unityyamlmerge.driver        # verify
   ```
4. **Check the Editor settings** (read-only; report if wrong, don't change): Edit > Project Settings > Editor > Version Control **Visible Meta Files**, Asset Serialization **Force Text**.
5. **Editor automation** (optional but strongly preferred): a Unity MCP package for live inspection/editing, and/or the Unity CLI (`unity status`, `unity command`, which need the `com.unity.pipeline` package to drive a running Editor). Installing either is a package change: ask first.
6. Open the project once and wait for the import to finish before running anything. The first import can take a long time; don't interrupt it.

## 3. The Editor connection

You usually **cannot open or click the Unity Editor yourself**. When a tool says no Editor is connected:

1. Check: does the MCP respond, or does `unity status` show a ready Editor?
2. If not, ask the user to open the project in the right Unity version and connect the bridge.
3. Wait for them to confirm, then retry.

Don't fall back to YAML edits for scene/prefab work just because the Editor is closed: ask first. An unfocused Editor may throttle or pause; for Play-mode checks ask the user to keep it focused, or enable *Run In Background* for the session.

## 4. How Unity projects work (read before writing code)

Most Unity bugs agents introduce come from the items below. They are true in every project.

| Topic | What to know | What to do |
|---|---|---|
| **GUIDs and `.meta`** | Every asset is referenced by the GUID in its `.meta`, not by path | Move/rename in the Editor; never edit or regenerate `.meta` |
| **Serialized data wins** | A field's C# initialiser is only the default for *new* instances. Values saved in scenes, prefabs and assets override it | To change a live value, change the asset/prefab/scene, not the C# default |
| **Field renames** | Renaming a serialized field drops its saved value everywhere | `[FormerlySerializedAs("oldName")]` |
| **Enums** | Serialized as integers (and sometimes saved by name) | Append only; never reorder, rename or delete members |
| **Lifecycle order** | `Awake` → `OnEnable` → `Start`. All `Awake`s run before any `Start` in a load, but order *between* scripts is not guaranteed | Set up yourself in `Awake`; read other objects in `Start` |
| **Inactive objects** | A GameObject saved inactive (or under an inactive parent) doesn't run `Awake`/`OnEnable` until activated | Don't assume a disabled panel's singleton exists; queue state it applies on wake |
| **Fake null** | A destroyed `UnityEngine.Object` compares `== null` but is not a C# null | Use `!= null`, never `?.` / `??` on Unity objects |
| **Statics and domain reload** | Statics survive scene loads, and survive Play-mode restarts if *Enter Play Mode Options* skip domain reload | Reset mutable statics in a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` hook and on `sceneLoaded` if they are per-run |
| **ScriptableObjects in Play mode** | Changes to an SO asset during Play mode **persist** in the Editor (they are assets), unlike scene objects | Never mutate shared SO assets at runtime; copy runtime state into plain objects |
| **Play-mode edits** | Changes to scene objects in Play mode are lost on exit | Make edits outside Play mode |
| **Prefabs and overrides** | Scene instances store only overrides; nested and variant prefabs inherit | Change the prefab asset, not a pile of scene overrides |
| **Main thread** | Most Unity APIs only work on the main thread | No Unity calls from `Task.Run`/threads; marshal back |
| **Async lifetimes** | `async` methods and coroutines can outlive the object or Play mode | Use `destroyCancellationToken`/`Awaitable` or stop coroutines in `OnDisable`; no `async void` except event handlers |
| **Time** | `Time.timeScale = 0` stops `deltaTime`, `FixedUpdate` and scaled coroutines, not input or UI | Choose scaled vs unscaled time on purpose |
| **Assemblies** | `Editor/` folders and Editor-only asmdefs don't ship; `UnityEditor` APIs in runtime code break builds | Wrap Editor-only code in `#if UNITY_EDITOR` or put it in an Editor assembly |
| **Resources / Addressables** | `Resources` folders ship everything inside them; Addressables keys are strings | Don't dump large assets into `Resources`; treat keys and paths as API |
| **Physics** | Physics runs in `FixedUpdate`; moving Rigidbodies via `transform` fights the solver | Use the Rigidbody API in `FixedUpdate` |

Project-specific architecture (services, scenes, data flow, tuning assets): `PROJECT.md`.

## 5. Repository map


```
Assets/
  Scripts/Sim/             Paw.Sim: model, rules, deterministic simulation, effects and prediction. Presentation/, Runtime/ (Config, Services, Flow, UI...), Editor/ are added with their first script
  Tests/EditMode/          Paw.Tests.EditMode
  Scenes/                  SampleScene (template) and CoreGameplay (WP02 playable loop, not in Build Settings); boot scene TBD
  Prefabs/ Data/ Resources/ Art/   created when they get their first content (Data = ScriptableObject configs and level data; Art/Audio use LFS)
  Plugins/                 third-party code (don't edit)
Docs/OWNER-PREFERENCES.md  the owner's standing instructions (read first)
Docs/Design/               GAMEPLAY-SPEC.md (the GDD, rules to implement), UNITY-HIERARCHY-AND-PREFABS.md and UI-LAYOUT-SYSTEM.md (structure standards, §11), TECHNICAL-KNOWLEDGE.md, GAME-FLOW.md (stub); design notes go here
Tools/SimHarness/          pure-logic tests with the .NET SDK
Tools/CompileCheck/        Roslyn compile check against the Unity assemblies
Packages/manifest.json     packages (don't change unasked)
ProjectSettings/           settings (don't change unasked)
PROJECT.md AGENTS.md CLAUDE.md  reference, this manual, Claude entry point
.claude/rules/             path-scoped coding and asset rules
```

## 6. Recipes

### Change gameplay code

```bash
git switch <main> && git pull
git switch -c <type>/<short-description>      # e.g. feat/dash-ability
# edit Assets/.../*.cs
```

Let the Editor recompile, read the Console, run the tests (§6 "Run tests") and a Play-mode check (§7). Commit with the `PROJECT.md` update. Push/PR only when the user asks.

### Add or change a tunable value

1. Find where tuning lives in this project (`PROJECT.md`). Put the value there (a config ScriptableObject, a serialized field on a prefab), not as a literal in logic.
2. New field: `[SerializeField]` with a `[Tooltip]` (and `[Range]`/`[Min]` where useful); default = the intended value. Existing assets need to be re-saved in the Editor to serialize it.
3. Changing a live value in an owned asset: propose `field · old → new · why` and let the owner apply or approve it.

### Add a component (MonoBehaviour)

- One class per file, **file name = class name**, in the right assembly/folder.
- `[SerializeField] private` fields, cached component references (`GetComponent` in `Awake`, not every frame), `[RequireComponent]` for hard dependencies, `[DisallowMultipleComponent]` where two would break it.
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Stop coroutines/tweens you own in `OnDisable`.
- Add it to a **prefab**, not straight into a scene.

### Add a ScriptableObject type

`[CreateAssetMenu(menuName = "<Game>/<Category>/<Name>")]`, data only (no runtime mutation of the asset), validate inputs in `OnValidate` without side effects. Create instances in the Editor (or via MCP), never by writing YAML.

### Create or change a prefab

Edit the prefab asset (Prefab Mode). Prefer prefab variants over copies. Check the diff of every prefab that nests it. Don't leave unintended overrides on scene instances (Overrides dropdown → Revert).

### Edit a scene safely

1. Tell the owners if the scene is shared.
2. Exit Play mode, close transient UI, reset anything you moved while inspecting.
3. Make the change through the Editor or MCP. Prefer adding a prefab instance over loose objects.
4. Before saving, check `git diff --stat` touches only that scene (and intended prefabs).
5. List the scene changes in your report (object path, component, field, old → new).

### Rename or move a script or asset

In the Unity Project window (keeps GUIDs). For a script, keep file name = class name. For serialized fields, `[FormerlySerializedAs]`. Search for string references: `Resources.Load` paths, Addressables keys, `nameof`, animation event names, UnityEvent method names in scenes/prefabs (these break silently).

### Add a package or an assembly definition (only when asked)

Packages: through Package Manager or the user's chosen tool; commit `manifest.json` **and** `packages-lock.json`. asmdefs: map dependencies first (a new asmdef can't see `Assembly-CSharp`), set platform includes for Editor assemblies, check nothing in runtime code references `UnityEditor`.

### Run tests

- Unity Test Framework (`com.unity.test-framework`): EditMode tests for pure logic, PlayMode tests for behaviour that needs frames, physics or scenes.
  ```bash
  unity test . --mode EditMode --output ./test-results.xml --timeout 600   # Unity CLI (exit 6 = failures)
  # or batch mode (no -quit with -runTests):
  "<Unity>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./test-results.xml -logFile -
  ```
  Batch mode can't run while the same project is open in an Editor. With an Editor open, run tests through the Test Runner window or MCP.
- Put game rules in plain C# classes (no `MonoBehaviour`) so they are testable in EditMode.
- Fast loop without the Editor: `cd Tools/SimHarness && dotnet run -c Release`, then `cd ../CompileCheck && dotnet build -c Release`. Run both after every script change, before asking Unity to recompile.
- Project-specific test entry points (menu items, validators): `PROJECT.md`.

### Make a build (only when asked)

Through the Editor's Build Profiles/Build Settings, or a build script the project already has:
```bash
unity build . --target <WebGL|StandaloneWindows64|Android|…> --execute-method <Builder.PerformBuild>
```
Never upload, sign or publish a build.

### Investigate performance

Profile before changing anything (Profiler, Frame Debugger, Memory Profiler; on device for mobile). Common wins: no allocations in `Update` (LINQ, string concat, boxing, `new` lists), cache components, pool frequently spawned objects, avoid `Find*`/`GetComponent` in hot paths, batch UI canvas rebuilds. Report numbers before and after.

## 7. Verifying your work

Your change is not done until all of these hold:

1. The project compiles; the Console shows **no new errors or warnings** after a recompile and after a Play-mode run.
2. Tests pass (existing + new ones for new logic). Validators/self-checks the project has still pass.
3. **Play-mode check** of the feature, including the path back through a restart/reload of the scene (state must not leak between runs) and a quit/re-enter of Play mode.
4. `git status` / `git diff --stat` show only intended files: no stray scene/prefab churn, no `Library/`, every new asset has its `.meta`, no `.meta` without its asset.
5. No new `?.` on Unity objects, unreset mutable statics, Unity API calls off the main thread, `UnityEditor` references in runtime code, or tuning literals in logic.
6. `PROJECT.md` updated.

If you couldn't run a check (no Editor connected, no device), say which ones and why. A static read is not "tested".

## 8. Workflow

- **Branches:** one per feature from an up-to-date main branch; never reuse a merged branch; don't stack feature branches. <!-- FILL: naming, merge method (merge / squash / rebase), who merges -->
- **Commits:** small and focused; the message says what and why. Scene/prefab changes are described in words in the message or PR (YAML diffs are hard to review).
- **Shared binary/scene files:** if the team uses LFS file locking (`git lfs lock`) or Unity Version Control locks, lock before editing a shared scene/prefab and unlock after. Never break someone else's lock.
- **Reports** at the end of a task or a plan checkpoint: done · files changed (mark scene vs prefab vs code vs asset) · tests and results · deviations and why · problems found but not fixed · assumptions · evidence (Console, screenshots, test output) · questions · `PROJECT.md` sections updated.
- Don't fix unrelated bugs inside a feature change; note them in `PROJECT.md`.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `NullReferenceException` at startup on another object's reference | Read in `Awake`/`OnEnable` before the other object initialised, or the other object is inactive. Read in `Start`, or queue the work |
| `MissingReferenceException` | A destroyed object is still referenced (static, event, `?.` hid fake null). Unsubscribe in `OnDisable`/`OnDestroy`, use `!= null` |
| "The referenced script (Unknown) on this Behaviour is missing" | Script renamed/moved without its `.meta`, class ≠ file name, or a compile error. Fix compile errors first; restore the `.meta` from git |
| Value "won't change" in play | You edited the C# default; the serialized value in the scene/prefab/asset wins |
| Value reset to default after a rename | Missing `[FormerlySerializedAs]` |
| State from the last run leaks into the next | Mutable static not reset (domain reload disabled, or per-run state across scene loads) |
| ScriptableObject values changed after playing | Runtime code mutated the asset. Copy to runtime state instead |
| Scene/prefab merge conflict full of `fileID` noise | UnityYAMLMerge not configured (§2.3). Don't hand-merge; stop and ask (usually: take one side, redo the other change in the Editor) |
| Textures/audio are tiny text files | LFS not pulled: `git lfs install && git lfs pull` |
| Works in Editor, fails in build | `UnityEditor` API or `#if UNITY_EDITOR` path, stripping (`link.xml`), missing scene in build list, `Resources`/Addressables not built, platform-specific code |
| Coroutine/async keeps running after the object is gone or Play mode exits | Stop it in `OnDisable`; use `destroyCancellationToken` |
| Physics jittery or objects tunnel | Moving Rigidbodies via `transform`, physics in `Update`, or fast objects with discrete collision |
| UI clicks ignored | No `EventSystem`, a raycast-target image on top, wrong Input Module for the input system, `CanvasGroup.blocksRaycasts` |
| Batch mode / `unity test` fails immediately | The project is open in another Editor, wrong Unity version, or license not active (`unity license status`) |
| Agent tools say no Editor connected | Ask the user to open the project and connect the bridge (§3) |

---

## 10. Pawtisserie rules (from the GDD)

These come from GDD v1.0 and apply to all gameplay work. If code and GDD disagree, or the GDD says **TBD**, ask; don't pick.

1. **The GDD is the spec.** Section 7 (resolution order of operations) is the exact behavior the build must reproduce; sections 5, 6, 8, 9 give the numbers. Values marked *(proto)* are tuning starting points: put them in config, not literals in logic.
2. **Rules are plain C#.** `Model` (BoardState: tray, box lanes and queue, plates, rack, stash tin, hand, deck, inventory), `Rules` (MoveResolver, DestinationPicker, StoragePump, BoxReconciler, OutcomeChecker), `Simulation` (time-stepped event queue) and `Prediction` (dry-run on a cloned state) have **no MonoBehaviours and no scene access**. The same code drives the game, the aim preview and the level solver.
3. **The view plays events; the model never reads animation state.** Presentation (BoardView, BoxView, HandView, CatView, FxPool...) consumes timed events from the simulation.
4. **Deterministic outcomes.** Seeded RNG (per level + attempt) for layouts, box order and Shuffle; fixed or seeded flight durations; authored decks and gifts; **no physics engine, no `UnityEngine.Random`, no `Time.*` and no `DateTime` inside rules.** A replay is level id + seed + list of (card, tile, time).
5. **Fixed in v1:** hand of 5, 8 starting cards, 3 box lanes, 3 pastries per box, 5 plates, 2 rack slots, boosters Shuffle / Call box / Stash. Treat them as named constants or config, not scattered numbers.
6. **IDs are API.** Card IDs `pop` (TAP), `sweep` (SWIPE), `drill` (POUNCE), `box` (FLOP); pastry IDs 0-3. Append only (same rule as enums); they are used in level data.
7. **Mobile budget:** 400-unit-wide logical board scaled to screen width, at most ~50 pastries on screen, pooled FX and sprites, no allocations in `Update`.
8. **Open decisions are GDD §21.3** (final names, leftover cards, currency, revive limits and prices, pre-level boosters, pastry types 5+, next obstacle). Don't hard-code a guess.

## 11. Scene, prefab and UI structure (read before touching scenes, prefabs or UI)

Two standards, adapted from the owner's Sand Drop project on 2026-11-04. **New** scenes, prefabs and UI follow them; reshaping existing ones (`GameRoot.prefab`) is a work package with its own design note and the owner's go.

- [`Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md`](Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md): separator roots (`--CONTROLLERS--`, `--WORLD--`, `--RENDERING--`, `--USER INTERFACE--`), scene = prefab instances only, one `EventSystem`, prefab families and nesting, `Visual` child for animation, pooling, sorting bands, folder and naming layout, where hand-adjustable values live, the `GameRoot` migration plan.
- [`Docs/Design/UI-LAYOUT-SYSTEM.md`](Docs/Design/UI-LAYOUT-SYSTEM.md): canvas plan (Hud / Banners / Popups), 400 x 800 scaler tied to `ViewConfig`, safe area, which zones are world vs canvas, Layout Group settings, popup anatomy and manager, text and fonts, input through the UI, checklist, open decisions.
- Short form for agents: `.claude/rules/unity-ui.md` and `.claude/rules/unity-scenes-prefabs.md`.

The four rules that matter most: (1) the prefab root holds the script and every part is a `[SerializeField]`, never found by name; (2) positioned objects move the root, animations move only an inner `Visual`; (3) never tween a child of a Layout Group; (4) the owner's hand-set values are never recomputed in code or restored from git.
