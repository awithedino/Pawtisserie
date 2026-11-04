# Unity hierarchy, prefab nesting and folder conventions

The standard every agent follows when it creates or changes scenes, prefabs and assets in Pawtisserie. Companion: [`UI-LAYOUT-SYSTEM.md`](UI-LAYOUT-SYSTEM.md) (canvases, safe area, layout groups, popups). Rules summary for agents: [`.claude/rules/unity-scenes-prefabs.md`](../../.claude/rules/unity-scenes-prefabs.md) and [`.claude/rules/unity-ui.md`](../../.claude/rules/unity-ui.md). Written 2026-11-04 at the owner's request, from the owner's other project (Sand Drop: `Docs/Design/UI-NESTING-DESIGN-NOTE.md`, `UI-SPEC.md` section 6, `TECHNICAL-KNOWLEDGE.md` sections 1 and 5, `HOME-SETUP.md`) and adapted to this project's engine and GDD.

Labels: **[verified]** read in a project file on 2026-11-04, **[Sand Drop]** a convention proven in Sand Drop (the owner played it), **[proposal]** my suggestion for Pawtisserie, **[unverified]** not seen in the Editor or in Play. No Editor or MCP was connected when this was written; nothing here was built or opened in Unity.

**Status.** This is the project convention. The approved `CoreGameplay` migration was implemented on 2026-11-04; the owner's review of the migrated scene is pending. Future scene and prefab work follows these rules.

---

## 1. Current `CoreGameplay` hierarchy (2026-11-04)

`CoreGameplay.unity` contains prefab instances under four identity separator roots:

```text
--CONTROLLERS-- / GameController
--WORLD-- / Board / CounterRow (including NEXT), ShelfRack (including SHELF), BoardRoot, Cat
--RENDERING-- / GameCamera
--USER INTERFACE-- / Canvas_Hud, Canvas_Popups, EventSystem
```

`Board` nests `CounterRow` and `ShelfRack`; the HUD nests `Hand` and the popup canvas nests `OutcomePanel`. One EventSystem is present. `GameController` has scene references to both canvases, their safe areas and the camera. The HUD and popup orders are 100 and 110, and each uses the 400×800 reference. `GameRoot.prefab` remains available as the legacy monolith but is no longer instanced in the scene. The scene is not in Build Settings. All `IsConfigured` checks passed after reopening; an owner Play check is outstanding.

The owner requested six visual groups: `TopBar` (level and restart), `CounterRow` (boxes, queue and NEXT), `ShelfRack` (plates and SHELF), `BoardRoot` (tray, input, preview and pooled pastries), `BoostersRoot` (empty WP04 placeholder), and `HandRoot` (cards and deck). `BoardRoot`, `CounterRow` and `ShelfRack` are in the world; `TopBar`, `BoostersRoot` and `HandRoot` remain under the HUD safe area. Pooled pastries keep their `BoardRoot` parent while flying and on storage. World-zone labels use child canvases; their cameras and the HUD's cross-prefab references are wired in the scene. Hiding the HUD leaves the world labels visible.

Within those zones, `CounterRow/Lanes` groups the three lanes and awning, and `CounterRow/Queue` groups the queue boxes and NEXT canvas. `ShelfRack/Plates` groups the plates, stash and SHELF canvas; `ShelfRack/Rack` groups the rack anchors, artwork and overflow anchor. `HandRoot/Cards` groups the five cards while the deck remains alongside it. These grouping parents have neutral transforms; the card fan is still animated by `HandView` rather than a Layout Group.

---

## 2. Scene hierarchy (target)

Same idea as Sand Drop's `Game.unity` [Sand Drop], reduced for a 2D, Built-in project: **separator roots** (empty objects named in capitals between dashes), one per concern. The scene holds **instances of prefabs and nothing else**; no loose sprites, no one-off UI.

```text
--CONTROLLERS--      GameController (one per scene), InputBridge/other flow scripts. No visuals.
--WORLD--            Board, counter and box lanes, queue strip, shelf and rack, stash tin, cat markers, FX pool root.
--RENDERING--        Main Camera (orthographic). One camera for world and UI (UI canvases are Screen Space - Camera).
--USER INTERFACE--   EventSystem (exactly one), Canvas_Hud, Canvas_Banners, Canvas_Popups.
```

Rules
1. A new scene starts from these four roots. Unused roots are not created (no `--LIGHTING--`: Built-in 2D needs no light).
2. **One `EventSystem` per scene**, under `--USER INTERFACE--`, never inside a world prefab (an extra copy breaks input silently). The legacy `GameRoot.prefab` still contains its own; do not add it to a scene that already has an EventSystem.
3. **One controller object per scene** (`GameController`). Views never find it; it holds the references (serialized) and subscribes to the views' events.
4. Scene-only references (a controller pointing at a view) are set on the **scene instance**. A prefab never references an object that lives in a scene, so any prefab can be dropped into another scene [Sand Drop].
5. Keep scene edits minimal and list every scene change in the report (object path, component, field, old to new).
6. Build Settings order is a Project Settings decision: ask the owner. Until a boot scene exists, `CoreGameplay` is opened directly.

---

## 3. Prefab families and nesting

### 3.1 The family list [proposal, extends what exists]

| Prefab (path under `Assets/Prefabs/`) | Root script | Holds (as nested prefabs or children) | Status |
|---|---|---|---|
| `Game/Board.prefab` | `BoardView` | `TrayCell` grid root, `Pastry` pool root, cat markers, badges | split from `GameRoot` in section 7 |
| `Game/CounterRow.prefab` | `CounterRowView`, nested `BoxLaneView` | 3 lanes of `Box`, `Awning`, `Rail`, queue strip | split from `GameRoot` |
| `Game/ShelfRack.prefab` | `ShelfRackView` | 5 `Plate`, rack visual, stash tin, anchors | split from `GameRoot` |
| `Game/Box.prefab`, `Pastry.prefab`, `Plate.prefab`, `TrayCell.prefab`, `Badge.prefab` | their views | children as today | exist [verified] |
| `UI/Hud/Hud.prefab` | `GameHud` | `TopBar` (level pill, restart), `QueueStrip`, `DeckPile`, `Hand`, `BoosterBar` | split from `GameCanvas` |
| `UI/Hud/Card.prefab` | `CardView` | `Visual` child with `Band`, `Title`, 5x5 mini grid | exists, `Visual` added in WP03 |
| `UI/Hud/Hand.prefab` | `HandView` (WP03) | 5 `Card` instances | WP03 |
| `UI/Popups/OutcomePanel.prefab` and later `ExpressPopup`, `OutOfMovesPopup`, `TutorialDonePopup` (GDD 11.4) | one script each | `Panel`, `Header`, `Body`, `Footer`, `Close` (UI-LAYOUT-SYSTEM section 7) | `OutcomePanelView` exists as a separate nested prefab |
| `UI/Banners/*.prefab` | one script each | floating words (`+1 CARD`, `COOLING RACK`, `CLEARS AT COMBO END`) | later |
| `Game/GameRoot.prefab` | legacy `GameController` | former monolithic setup | retained, not instanced in `CoreGameplay` |

Only create a prefab when something is built; this table is the target list.

### 3.2 Nesting rules [Sand Drop, owner rules 2026-10-28]

1. **The root of a prefab holds the script.** Every part the script uses is a `[SerializeField]` field on that root, assigned in the Inspector. Nothing uses `transform.Find`, `GameObject.Find`, `GetComponentInChildren` by name or string paths.
2. **An empty field fails loudly and says which one.** Keep the existing `IsConfigured` pattern and make the exception text name the field (for example "GameHud: the field 'shelfLabel' is empty"). When UI grows past a handful of components, add a tiny shared helper (`UiParts.Need(field, owner, "field")` in Sand Drop) instead of repeating the checks. Do not add it speculatively.
3. **Parts that go together live under one parent**, placed by anchors and Layout Groups, not by hand-typed offsets (the exceptions are in UI-LAYOUT-SYSTEM section 6).
4. **Pack split-up parts**: a title goes inside its ribbon, a count inside its icon, a label inside its pill.
5. **Positioned object vs animated object.** Whatever moves an object into its slot (a layout, `HandView`, an anchor) moves the **root**; every animation (lift, tilt, squash, deal-in, drag ghost) moves only an inner `Visual` child. Then layout and animation never fight [Sand Drop; already decided for `Card` in WP03].
6. **Nest prefabs, do not copy them.** A repeated thing (the three booster buttons, the five plates, the lanes) is one prefab with N instances; per-instance differences are the smallest possible overrides (a sprite, a label). Variants (`Prefab Variant`) for "the same thing with another look". Revert unintended overrides on scene instances.
7. **Repeated rows are components**: a row with several parts gets its own component on its root (`HeartRow`, `ToggleRow`, `CoinPack` in Sand Drop). The popup holds the row, the row holds its parts.
8. **Pooled objects reset themselves.** Anything taken from a pool (pastries, markers, badges, floating text) implements one `Initialize`/`ResetView` that restores every field a previous life changed (alpha, scale, color, tween targets, parent). Kill tweens in `OnDisable` and on level reload.
9. **Values live on the prefab, not in code.** Colors, sizes, offsets, angles, durations, curves: serialized fields with a `[Tooltip]` (or in `ViewConfig`). The owner hand-adjusts looks in the Scene view and Inspector between sessions: read a prefab before editing it, never restore it from git, and **remove code that computes a look the owner has hand-authored** (owner rule 11 in `Docs/OWNER-PREFERENCES.md`). Only genuinely dynamic layout is computed (the 1 to 5 card fan, the tray grid for the current level size).
10. **Prefab edits go through the Editor** (Prefab Mode, via MCP when connected), not YAML. `.meta` and GUIDs are never touched. Check the diff of every prefab that nests the one you changed.

---

## 4. World objects in a 2D Built-in project

- **Logical units.** The board is laid out in a 400-unit-wide logical space; one tray cell is 50 units (GDD 5.1, 11.2). `ViewConfig` holds the board size, and the camera and the HUD canvas scale to it. Sprites are authored in these units (the `Placeholder` sprites are 2.56 units at their import size, so views scale them) [verified in the prefab]. Do not mix a second unit system in another prefab.
- **Pivots and anchors.** Cells, plates and boxes have a centered pivot; zones are placed by authored **anchor objects** (`TrayAnchor`, `RackAnchor0/1`, `OverflowAnchor`: already in `GameRoot`) and the code reads their positions. New zones get an anchor object, not a number in code.
- **Sorting.** Built-in 2D draws by Sorting Layer, then Order in Layer. Adding Sorting Layers is a Project Settings (Tags & Layers) change: ask first. Until then use **Order in Layer bands on the Default layer** [proposal; existing prefabs use order 1 for many sprites and have not been audited]:

  | Band | Order | Contents |
  |---|---|---|
  | Back | 0 to 9 | counter, awning, rail, wires, shelf, rack |
  | Tray | 10 to 19 | tray cells, cans, cloches |
  | Pastries | 20 to 39 | pastries at rest and in flight |
  | Boxes | 40 to 49 | boxes, lids, gift tags |
  | Markers and FX | 50 to 79 | cat markers, badges, puffs, floating text |
  | UI | canvases | by canvas order, see UI-LAYOUT-SYSTEM section 2 |

  Record the real numbers in this table when a work package settles them.
- **Sprite renderers vs UI images.** Things that fly between zones (pastries, markers, boxes) are `SpriteRenderer`s in the world. Things anchored to the screen (hand, labels, panels, buttons) are uGUI under a canvas. A world object never lives under a canvas and the reverse.
- **No physics for outcomes** (GDD 18.4). Colliders only where input needs them, and prefer the canvas raycast surface (`TrayHitArea`) over 2D physics.
- **Art placeholders** live in `Assets/Art/Placeholder/` and are swapped by replacing the sprite reference on the prefab, not by editing code. Every pastry type has a distinct color **and** silhouette (GDD 3.3): keep that in the prefab data, not in `if` chains.
- **Pooling.** Pastry, marker, badge and floating-text pools are created once per board size and reused; `ViewConfig.poolSize` (60) is the pastry pool [verified]. No `Instantiate`/`Destroy` per move.

---

## 5. Folders and names [proposal; extends the map in `AGENTS.md` section 5]

```text
Assets/
  Art/Placeholder|Pastries|Cat|UI|Fonts|Audio/      art (LFS); Placeholder exists
  Data/Config/ViewConfig.asset ...                   ScriptableObject configs; Data/Levels/ when the format is decided
  Prefabs/Game/                                      world prefabs
  Prefabs/UI/Hud|Popups|Banners/                     UI prefabs
  Resources/                                         only what is loaded by string (DOTweenSettings today); document each entry
  Scenes/                                            CoreGameplay today; Boot and MainMenu later
  Scripts/Sim|Presentation|Runtime|Editor/           one asmdef each (Editor created with its first script)
```

- Create a folder when it gets its first asset. Move and rename assets only in the Unity Project window (GUIDs).
- Prefab names are nouns in PascalCase (`BoxLane`, `OutcomePanel`); children are named by role (`Visual`, `Header`, `Body`, `Footer`, `Close`), numbered only when they are an array (`Slot0..2`). Scene separator roots are `--UPPER CASE--`.
- Script file name = class name; a view class ends in `View`, a config in `Config`, a flow class describes its job (`GameController`).
- Assets whose names are used as keys (`Resources.Load` strings, save keys) are API: do not rename them.

---

## 6. Hand-adjustable values: where each kind lives

| Kind | Home |
|---|---|
| Rule numbers fixed in v1 (hand 5, plates 5, rack 2, box 3) | `Paw.Sim.Rules` constants |
| Simulation durations | `SimulationConfig` (microseconds), fed from `ViewConfig` for the prototype |
| Board size, colors per pastry type and card band, arc height, pool size, panel delay | `ViewConfig.asset` |
| Looks of one prefab (offsets, sizes, tilt limits, bump heights, tween times) | serialized fields with tooltips on that prefab's root script |
| Anything the owner tunes by hand | never recomputed in code; tell the owner where the field is |

Propose a change to a shared config as `field · old → new · why`; do not overwrite owner values.

---

## 7. Completed `GameRoot` migration

The approved migration split the scene into the prefab instances shown in section 1. The legacy `GameRoot` asset was preserved. The card prefab was moved through Unity to `UI/Hud/Card.prefab` with its GUID preserved. `TrayHitArea` forwards pointer events through `GameHud` to the controller, and `OutcomePanelView` handles the separate outcome panel. Its button footer uses a Horizontal Layout Group. Scene cross-references are wired on the controller instance; the two canvas scalers and safe-area transforms are fitted together.

Unity EditMode and SimHarness each passed 69 cases; CompileCheck passed. An isolated prefab composition produced a simulated win and verified board input on release. A 400×800 edit-mode render was inspected. The migrated scene still needs the owner's Play and visual check, including restart, other screen sizes and a real device. WP04 will introduce the popup manager.

---

## 8. Checklist for a new prefab or scene

- [ ] Root has the one script; every part is a serialized field; an empty field throws and names itself.
- [ ] Grouped parts share a parent; no hand-placed offsets where an anchor or a layout group can do it.
- [ ] Animated children are under `Visual`; the root is positioned by layout or code.
- [ ] No reference from the prefab to a scene object; no `Find` by name; no second `EventSystem`.
- [ ] Values are fields with tooltips (or in `ViewConfig`); nothing the owner may tune is a literal.
- [ ] Pooled? It has one reset path and kills its tweens in `OnDisable`.
- [ ] Sorting order is in the right band; UI is on the right canvas.
- [ ] Reported: files changed (prefab vs scene), scene changes listed, what was checked (compile, tests, render) and what needs the owner in Play.

---

## 9. Traps carried over from Sand Drop (verify when first met here)

- A GameObject saved inactive has not run `Awake`; a manager that calls `Setup` on a panel must switch it on and off once in its own `Awake`, or the panel's parts are not initialised.
- Layout Groups own their children's positions: tweening a child's `anchoredPosition` under one fights the group. Tween the group, or give the child a wrapper.
- A level reload during a win/fail coroutine leaves flags set and panels open: reset everything on every load, with a timed safety net.
- Setting TextMeshPro outline properties in code creates material instances and logs errors in edit mode: use material assets.
- A render in edit mode cannot show UI text, tweens or timings; say so in the report instead of claiming the look is verified.
- Mutable statics survive Play-mode restarts if domain reload is off (currently on): reset them in a `SubsystemRegistration` hook anyway.
- Pooled shooters/pastries keep the last life's values for a frame unless the reset runs before the first use.
