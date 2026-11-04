# UI layout system: canvases, scaling, safe area, layout groups, popups

The standard for every screen, HUD element and panel in Pawtisserie. Companion: [`UNITY-HIERARCHY-AND-PREFABS.md`](UNITY-HIERARCHY-AND-PREFABS.md) (scene hierarchy, prefab nesting, folders). Short rules for agents: [`.claude/rules/unity-ui.md`](../../.claude/rules/unity-ui.md). Written 2026-11-04 at the owner's request from Sand Drop's `UI-SPEC.md` (sections 3 to 6), `UI-NESTING-DESIGN-NOTE.md`, `HOME-SETUP.md` (sections 2 to 7) and `TECHNICAL-KNOWLEDGE.md` (section 1, invariant 8), adapted to this project's GDD (sections 10, 11, 19) and its engine (Unity 2022.3, Built-in, uGUI 1.0, TextMeshPro 3.0.7, legacy Input Manager).

Labels: **[verified]** read in a project file on 2026-11-04, **[Sand Drop]** proven in the owner's other game, **[GDD]** specified by the design document, **[proposal]** my suggestion, **[unverified]** not seen in the Editor or Play. The current HUD and popup canvases were built and inspected in the Editor; future UI proposals remain labeled as such.

---

## 1. Principles

These are the owner's UI rules from Sand Drop (2026-10-28) and apply to all UI here [Sand Drop].

1. The **root of a UI prefab holds the script**; every part it uses is a `[SerializeField]` field. Nothing finds children by name.
2. An empty field **fails loudly** and names itself.
3. **Parts that belong together share a parent** placed by anchors and Layout Groups, not by typed offsets.
4. Split-up parts are packed (title inside its ribbon).
5. Small shifts of a few pixels are fine: the owner eyeballs the result and hand-adjusts.
6. **No instruction text on the play screen** outside the tutorial [GDD 11.1]; readability in three seconds comes from shape and color, never from reading.
7. The pure rules never read the UI and the UI never decides a rule: views show what the simulation's events say [GDD 18.1].

---

## 2. Canvas plan

The migrated scene uses separate HUD and popup canvases with a shared orthographic camera. A banners canvas remains future work.

| Canvas | Sorting order | Contents |
|---|---|---|
| `Canvas_Hud` | 100 (built) | top bar, booster bar, deck and hand |
| `Canvas_Banners` | TBD (future) | floating words and short toasts (`+1 CARD`, `COOLING RACK`, `CLEARS AT COMBO END`, tutorial coach bubble) |
| `Canvas_Popups` | 110 (built) | dim, then panels: outcome, Express offer, out of moves, tutorial done |

- Both built canvases: Render Mode **Screen Space - Camera**, the scene's single orthographic camera, same Canvas Scaler (section 3). World sprites include badges at order 50 (see the hierarchy document); canvases draw on top by plane distance and sorting order.
- Which canvas owns what is decided by **how often it changes and who blocks input**: the hand redraws every move, popups almost never, banners spawn and vanish. The HUD and popup canvases have been split; add the banners canvas with its first feature.
- One `EventSystem` per scene, outside the canvases (hierarchy document, section 2).
- `CounterRow` and `ShelfRack` each have a world-space child canvas for their associated label; `BoardRoot` has a world-space input/aim canvas. These use the scene camera and move or hide with their zone. Their text is not parented to `Canvas_Hud`.
- Never put a world object under a canvas, or a canvas element in the world.

---

## 3. Scaling and the board's coordinate space

- **Canvas Scaler:** *Scale With Screen Size*, reference **400 x 800**, match **width** by default [verified in `GameRoot.prefab`]. `ViewConfig` board size (400 x 800) **must equal** this reference; change both together or the world and the HUD drift apart.
- The board is laid out in a 400-unit-wide logical space and scaled to the screen width [GDD 11.2]. `GameController.FitCamera` picks the camera size and the scaler's match value from the same test, so camera and canvas always fit the **same axis** (width on tall screens, height on short ones) [verified in code]. Do not set one without the other.
- Needed height at the reference: 94 (counter) + 34 (queue) + 60 (shelf) + 38 (rack) + 4 x 6 (gaps) + 6 + rows x 50 + 10, plus 78 in the tutorial for the coach bubble [GDD 11.2].
- **Spare height.** GDD 11.2 grows the queue (up to +16) and the four gaps with extra height. The owner decided **not to implement it by default** (WP03 decision 9); zone spacing stays as authored. Do not add it without a new decision. A screen shorter than needed scales the whole board down to fit.
- **Sizes in canvas units.** At 400 reference units across a phone 360 to 430 dp wide, one unit is close to one dp [estimate]. The GDD asks for touch targets of at least 44 pt [GDD 19], so interactive elements are at least **44 x 44 units**, including the restart button, booster buttons and the card (cards are larger). Decorative parts may be smaller.
- Screens to check every UI change at: **360 x 640, 400 x 800, 430 x 932**, and 9:16 to 9:22 in general; tablets and foldables are best effort [Sand Drop guarantees 9:16 to 9:22].

---

## 4. Safe area

Status: **built in WP03 and applied to both migrated canvases; device verification pending**: HUD children are under a stretched `SafeArea` rect and a reference-sized `Content` child; `GameController` fits the content and world camera to `Screen.safeArea` using `SafeAreaLayout`. The convention is [Sand Drop, `SafeAreaFitter`]:

- Layout of every canvas: `Background` (full screen, **outside** the safe area so art bleeds behind the notch) and `SafeArea` (a stretched child, sized to `Screen.safeArea` whenever it changes). All interactive and informative UI goes inside `SafeArea`.
- **Edge bars that must touch the screen edge** (a top backdrop, a bottom bar) use two small components, as in Sand Drop: one that grows the bar upward by the top inset behind the notch, one that grows it downward by the bottom inset and lifts its content by the same amount. They are written when the first such bar exists.
- Top inset: respect it for text and buttons; a decorative backdrop may extend behind it. Decide per screen, in the design note [Sand Drop ignored the top inset on Home and regretted it at the shop header].
- Bottom: the hand sits in the lower half so one thumb reaches everything except restart [GDD 19]; keep the cards clear of the gesture bar.
- Check with a simulated notch: edit-mode renders with a fake `safeArea` rect (WP03 asks for one render with a notch). Real devices are the owner's check.

---

## 5. Zones: what is world and what is canvas

Mapping of GDD 11.1 (top to bottom) to the two spaces:

| Zone | Space | Notes |
|---|---|---|
| Top bar: level pill with cat icon (left), round restart (right) | `Canvas_Hud` | restart needs a 44-unit target |
| Counter: 3 lanes of pastry boxes, scalloped awning | world | boxes are flying/animated sprites |
| Box queue: `NEXT` + mini boxes | world `CounterRow` | `NEXT` on its child canvas; boxes are sprite views |
| Display shelf: 5 plates, stash tin and `SHELF n/5` | world `ShelfRack` | shelf counter on its child canvas; pastries land on plates |
| Cooling rack: 2 slots, folded flat when closed | world | label `RACK` and `CLEARS AT COMBO END` are banners |
| Baking tray: the grid | world `BoardRoot` | input through its world-space canvas raycast surface (`TrayHitArea`) |
| Hand: deck pile with count (left), 5 cards fanned | `Canvas_Hud` | fan positions computed by `HandView` (the one allowed computed layout) |
| Booster bar: Shuffle, Call box, Stash | `Canvas_Hud` | above the hand; a Horizontal Layout Group of three instances of one prefab |
| Aim pill, preview badges | `BoardRoot` child canvas and world | badge = shape + color, never color alone [GDD 19] |

---

## 6. Layout Groups and anchors

Use a Layout Group when a set of parts has a fixed order and spacing; use anchors for a lone part. The configuration that worked in Sand Drop [Sand Drop]:

- `childControlWidth/Height` **off**, `childForceExpand` **off**, alignment **upper** (vertical group) or **left** (horizontal), spacing and padding taken from the intended look. The group places a child; the child keeps its own size. Children sit on a centered anchor.
- A panel body is a Vertical Layout Group (message, window, buttons); a two-button footer is a Horizontal Layout Group; a hidden button leaves no gap.
- A repeated row is its own component on the row root (section 7 of the hierarchy document).
- **Never tween the `anchoredPosition` of a child that sits under a Layout Group.** Tween the group, or give the animated part its own wrapper (`Visual` child). The booster bar's buttons animate their icon, not their slot.
- **Measure only after the layout has run.** Code that sizes or fits a panel from its bounds (a shrink-to-fit) must call `Canvas.ForceUpdateCanvases()` first; `LayoutRebuilder.ForceRebuildLayoutImmediate` on a root with no layout controller does nothing [Sand Drop bug that shifted every popup sideways].
- **Exceptions that are computed, not authored:** the card fan (1 to 5 cards), the tray grid for the current size, the pastry flight paths. Everything else is authored in the prefab.
- Keep Layout Groups off objects that change every frame (the hand fan, the shelf counter text); a layout rebuild per frame costs on a phone [estimate].

---

## 7. Panels and popups

Anatomy of every panel prefab [Sand Drop]:

```text
XPopup            root: CanvasGroup + the popup script; fixed size; centered anchor
  Panel           stretch over the root (the frame art)
  Header          ribbon/title; its text is a CHILD of the ribbon
  Body            plain object with a Vertical Layout Group
  Footer/Buttons  anchored bottom, or a Horizontal Layout Group for two buttons
  Close           anchored top-right, last sibling so it draws on top
```

GDD 11.4 lists the panels: Win "Orders boxed!" (Next level, double coins via ad TBD), Overflow/rack "Shelf overflow" (Express box, Retry), Out of moves "Butter is out of moves" (+2 FLOP, Retry), Tutorial done (Start level 1). One `OutcomePanelView` on `Canvas_Popups` switches the title and buttons; the WP04 `PopupManager` manages its shared dim and popup stack. Owner Play review is pending.

WP04 introduced two small generic pieces:
- `UiPopup` is a component with a serialized `CanvasGroup`, unscaled opacity fades, a `Closed` event after its close fade and a `DismissOnBack` option. It activates an inactive popup when opened. The current outcome panel is saved active and hidden through its CanvasGroup.
- `PopupManager` (one on `Canvas_Popups`) accepts a `UiPopup` instance in `Open`, ignores duplicate opens of that instance, maintains a stack and a shared dim, and routes Escape/back to the top panel according to its dismissal policy. Future distinct popup types and their setup remain future work.

Rules for all panels
- Pop animation uses unscaled time (a paused or slow-motion game must not slow the panel).
- A panel is **shrunk to fit** the safe area with a margin (24 units), never enlarged.
- While a panel is visible, the hand and board ignore input (`CanvasGroup.blocksRaycasts` on the panel, `GameController.CanInput` already checks `PanelVisible`) [verified].
- WP04 timing uses 0.7 s after settlement for a win, 0.55 s after the first overflow pastry lands without waiting for the combo, and the settled-failure delay for other failures. See the WP04 design note §8; owner Play timing remains to be checked.
- Buttons are identified by serialized field, wired in the script (`onClick.AddListener` in `OnEnable`, removed in `OnDisable`, as `GameHud` does now) [verified].

---

## 8. Text and fonts

- **Today:** `GameHud` uses `UnityEngine.UI.Text` (legacy) [verified]. **Target:** TextMeshPro (3.0.7 is installed) with the GDD fonts: Fredoka for the level pill, card names and floating text; Rubik for body text [GDD 3.5]. Migrate when the real fonts and art arrive, in one pass per prefab, not piecemeal.
- Check that the chosen fonts cover Vietnamese diacritics **before** committing to them [GDD 19].
- Outlines and underlays are **material assets**, not set in code (setting TMP outline properties in code instantiates materials and logs errors) [Sand Drop].
- Dynamic font atlases start empty, so the first characters shown cost a spike on a phone: pre-populate the atlas with the characters the game uses [Sand Drop].
- Short strings only, English first; keep every string in one place per panel to ease localization later [GDD 19].
- Text color is never the only signal: the shelf counter turns red **and** pulses; badges carry a shape [GDD 5.6, 19].

---

## 9. Input through the UI

- All pointer input goes through the uGUI `EventSystem`: cards implement the pointer interfaces; the tray has an invisible, authored raycast surface (`TrayHitArea`, an alpha-0 `Image`) so board and card gestures arrive the same way on mouse and touch [WP03 design, verified in code].
- **Raycast Target is on only for elements that receive input.** Decorative images, labels and pills have it off; otherwise they block the tray or burn raycast time.
- Drag threshold comes from `ViewConfig.dragThresholdDp` (10 dp), not a pixel literal [verified].
- Temporary HTML card faces include their title and footprint; the old dynamic title/band/grid objects remain inactive while the inner card visual keeps the existing selection, fan and drag behavior. Baked text will need localizable replacements with final art.
- Escape and Android back: cancel the selection first, close the top panel next.
- Haptics and reduced motion are settings, not UI code: `IHapticCues` and `ViewConfig.reducedMotion` (WP03).

---

## 10. Performance notes for mobile [estimate until profiled]

- Target 60 fps on a mid-range Android phone, about 50 pastries on screen [GDD 18.6]. UI: few canvases, static art on a canvas that does not change, atlases per chapter, no per-frame allocations (no string concatenation in `Update`; cache label strings or only set text when the value changes).
- Do not enable a Layout Group, `ContentSizeFitter` or `Mask` on anything that animates each frame.
- Profile on the device before optimizing; report numbers before and after (`AGENTS.md` section 6).

---

## 11. Checklist for new UI

- [ ] Right canvas (by change frequency and input blocking); inside `SafeArea`; decorative bleed outside it.
- [ ] Root script + serialized parts; an empty field throws and names itself.
- [ ] Grouped parts under one parent with a Layout Group (off: control size, force expand; upper/left alignment); lone parts on an anchor.
- [ ] Animated parts under a wrapper/`Visual`; nothing tweens a group child's `anchoredPosition`.
- [ ] Interactive targets at least 44 x 44 units; Raycast Target off on everything else.
- [ ] Panel: pop on unscaled time, fit inside the safe area with 24-unit margin, `blocksRaycasts` while shown, Escape closes it.
- [ ] No instruction text on the play screen; no color-only signals.
- [ ] Values are fields with tooltips or in `ViewConfig`; no literals the owner may tune.
- [ ] Checked at 360x640, 400x800, 430x932 and a simulated notch (edit-mode renders); report what a render cannot show (text, tweens, timings) and what the owner must look at in Play.

---

## 12. Decisions for the owner [proposals, each with a recommendation]

1. **Canvas split:** HUD and popup canvases are built as part of the migration; banners are deferred until needed. The owner will review the result in Play.
2. **Popup framework:** build `UiPopup` + `PopupManager` when WP04 adds the second panel. *Recommend: yes; copy the behavior, not the Sand Drop code.*
3. **TextMeshPro and the GDD fonts:** migrate the HUD from legacy `Text` once Fredoka/Rubik are chosen and checked for Vietnamese. *Recommend: yes, one pass per prefab.*
4. **Sorting Layers:** add real Sorting Layers (Back, Tray, Pastries, Boxes, FX) instead of Order-in-Layer bands. This is a Tags & Layers change. *Recommend: stay with bands until the bands cause a bug.*
5. **Safe-area top inset:** respect it for HUD text and buttons, let backdrops bleed. *Recommend: yes.*
6. **Spare-height growth (GDD 11.2):** stays off (WP03 decision 9). Revisit only if tall phones leave visible empty bands.
7. **Orientation lock:** the Player setting is Auto Rotation while the GDD says portrait (`PROJECT.md` section 6). The layout assumes portrait; locking it is a Project Settings change for the owner.
