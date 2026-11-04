---
paths:
  - "Assets/Resources/Levels/**"
  - "Assets/Data/Levels/**"
---
# Level data

The level format is GDD §14.1: `id, cols, rows, types, layout|seed, cans, cloches, boxes, deck[8], gifts, plates, difficulty`. The storage format (ScriptableObject or JSON) is **not decided yet**; do not create level files until it is.

- Invariants (GDD §5.2, §6.3, §14.2): pastries per type are a multiple of 3; pastries = `cols × rows − cans`; boxes = pastries ÷ 3; the first three boxes are three different types; `deck` has 8 entries (first 5 = hand); gift keys are one-based box arrival numbers within the box count. Avoiding the final two boxes is an authoring guideline, not validation (WP02 owner approval). Level 0 uses its explicit tutorial exceptions.
- Card IDs in data are `pop`, `sweep`, `drill`, `box`; pastry IDs are 0-3 (type 4+ is TBD).
- Levels are deterministic: a generated level stores its seed and parameters; a hand-authored level stores its layout.
- Never reformat or hand-edit generated level files; derived or fixed levels go to a new folder.
