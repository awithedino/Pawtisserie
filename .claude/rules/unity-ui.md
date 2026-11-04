---
paths:
  - "Assets/Scripts/Runtime/Views/**"
  - "Assets/Prefabs/**"
---
# UI and view prefabs

Full standard: [`Docs/Design/UI-LAYOUT-SYSTEM.md`](../../Docs/Design/UI-LAYOUT-SYSTEM.md) (canvases, scaling, safe area, layout groups, popups) and [`Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md`](../../Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md) (scene roots, prefab nesting, folders, sorting). Summary:

- Scene roots: `--CONTROLLERS--`, `--WORLD--`, `--RENDERING--`, `--USER INTERFACE--`. The scene holds prefab instances only. Exactly one `EventSystem`, never inside a world prefab.
- A prefab's root holds the script; every part is a `[SerializeField]`; no `Find` by name; an empty field throws and names itself (keep the `IsConfigured` pattern, name the field in the message).
- Whatever positions an object moves the root; animations move only an inner `Visual` child. Never tween the `anchoredPosition` of a child under a Layout Group.
- Layout Groups: `childControl` off, `childForceExpand` off, upper/left alignment; call `Canvas.ForceUpdateCanvases()` before measuring a panel.
- Canvas Scaler reference is **400 x 800 and must equal `ViewConfig` board size**; camera and scaler fit the same axis. One canvas per feature (Hud 10, Banners 20, Popups 30) when the split is done; HUD content inside `SafeArea`, backdrops outside.
- Touch targets at least 44 x 44 units; Raycast Target on only for interactive elements; panels animate on unscaled time.
- No instruction text on the play screen; never color alone as a signal.
- Values (colors, sizes, offsets, durations) are tooltip fields or in `ViewConfig`; the owner hand-adjusts them, so do not compute a look in code and do not overwrite their values.
- Adding Sorting Layers, changing orientation or Build Settings are Project Settings changes: ask first.
