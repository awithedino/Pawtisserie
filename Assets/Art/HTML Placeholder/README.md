# Pawtisserie placeholder sprites

87 transparent PNGs, exported from the HTML prototype's own drawing code, so they match what you see when you play it. They're meant for greyboxing only; swap them for real art later.

`preview.png` shows all of them on one sheet.

## Scale

- **1 prototype unit = 4 px**, so one tray cell (50 units) is **200 px**.
- In Unity, set **Pixels Per Unit = 200**. One tray cell is then 1 world unit, which keeps the board math simple: cell (c, r) sits at world (c, −r).
- Every sprite uses the same scale, so sizes relative to each other are already correct. Pastries need no scaling on the tray.
- In the prototype, pastries are drawn at 42/44 size in the tray (about 0.95 scale), at 28/44 inside boxes and at 15/44 on queue icons.

## Import into Unity

1. Copy `Unity/PawtisseriePlaceholder/` into your project's `Assets/` folder.
2. That's it. `Editor/PawtisserieSpriteImporter.cs` reads `sprites.json` on first import and sets these for each PNG:
   - Sprite (Single)
   - PPU 200
   - custom pivot
   - 9-slice border (where listed)
   - no mipmaps
   - Full Rect mesh
3. If the PNGs were imported before the script compiled, run **Tools → Pawtisserie → Reapply Placeholder Sprite Settings**.

The importer only changes a texture on its first import, so any Inspector changes you make afterwards are kept.

## What's in it

| Folder | Sprites | Pivot |
| --- | --- | --- |
| `pastries/` | donut, macaron, cupcake, croissant; each as normal (with floor shadow, for the tray), `_flat` (no shadow, for boxes, plates and flying) and `_ghost` (22% alpha, for empty box slots); plus `shadow` alone | cell center |
| `obstacles/` | `cloche` (glass dome, to layer over a pastry), `cloche_over_<pastry>` (combined), `cream_can`, `cream_can_primed` (flash frame) | cell center |
| `butter/` | `butter_focus` and `butter_happy` (the only two expressions the prototype has), `paw`, `paw_arm` (for TAP; the arm extends upward), `tail` (for SWIPE; the body end is at the pivot, the tip on the right) | the attach point |
| `boxes/` | per pastry: `box_<p>_open`, `_open_slots` (ghost slots), `_full`, `_closed` (lid and bow); `queue_box_<p>`, `queue_box_highlight` (first-in-queue ring); `gift_tag_<card>`; `express_box` | box-body center |
| `storage/` | `plate`, `plate_shelf_5`, `plate_shelf_board` (9-slice), `cooling_rack` (open), `cooling_rack_closed`, `stash_tin` | center (shelf: top-left) |
| `tray/` | `tile_light`, `tile_dark` (checker), `tray_frame_3x3` (9-slice for any size), `aim_<card>` (45% highlight), `aim_target` (dashed), `tutorial_glow`, `badge_*` (prediction badges: to_box, to_plate, to_rack, overflow, cloche_crack, can) | center |
| `cards/` | `card_tap`, `card_swipe`, `card_pounce`, `card_flop` (flat, untilted; apply the fan rotation in Unity) | center |
| `icons/` | boosters (`icon_shuffle`, `icon_call_box`, `icon_stash`), `icon_restart`, `icon_finger` (tutorial hand), `icon_butter_head` (level pill), 256 px each | center |
| `ui/` | `counter_bg` (3 lanes, full 400-unit width), `queue_strip`, `awning_stripe_tile` (repeat wrap) | top-left |

## Good to know

- **Some sprites have text baked in:** the cards, the express box, the stash tin and the can. Swap those for TextMeshPro labels once you localize. The cards' hint lines ("paw · plus" and so on) are baked in too.
- **The can label is drawn slightly smaller than in the prototype.** At 8 px, "CREAM" spills past the can's edge; here it's fitted inside. The prototype still has the overflow.
- **No animation frames are included.** The prototype animates everything with code (squash, spin, flight arcs, a lid that closes by sliding), and the GDD §19 timing table describes those motions. Do them in Unity with tweens (for example DOTween) on these single sprites.
- **Particles (sugar sparks, puff rings, tail streaks) aren't exported.** Use Unity's particle system with the `SUGAR` colors listed in the GDD.

## Re-export (different size, or after you change the prototype)

```bash
pip install playwright --break-system-packages   # Chromium must be installed for Playwright
python tools/export_sprites.py pawtisserie-prototype.html out --k 4 --fonts <folder with fredoka/rubik .woff2>
```

`--k` is px per prototype unit: 4 → 200 px cells, 8 → 400 px cells. If you change `--k`, change Pixels Per Unit to `50 × k`; the exporter writes that value into `sprites.json`. Without `--fonts`, text falls back to a system font. You can get the fonts from Google Fonts or from the npm packages `@fontsource/fredoka` and `@fontsource/rubik`.
