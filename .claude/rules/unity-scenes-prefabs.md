---
paths:
  - "**/*.unity"
  - "**/*.prefab"
  - "**/*.meta"
---
# Scenes, prefabs, GUIDs

- Edit through the Editor / Unity MCP, not YAML, whenever an Editor is connected. Say so if you must hand-edit.
- Never edit or regenerate `.meta` or `guid`/`fileID` values. Move assets in the Editor.
- Build new things as prefabs (a pastry, a box, a plate, a card, the cat); keep scene edits minimal and list them in the report (object path, component, field, old -> new).
- Scene saves must not carry incidental changes. Check `git diff --stat` first.
- UI prefab convention: the root holds the script and every part it uses is a `[SerializeField]` on it (no name-path lookups; an empty field throws and names itself). Parts that go together sit under one parent controlled by anchors and Layout Groups.
- The owner hand-adjusts prefabs between sessions: read before editing, never restore from git.
- Scene roots, prefab families, nesting, sorting bands, folders and the `GameRoot` migration plan: [`Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md`](../../Docs/Design/UNITY-HIERARCHY-AND-PREFABS.md). UI: [`Docs/Design/UI-LAYOUT-SYSTEM.md`](../../Docs/Design/UI-LAYOUT-SYSTEM.md) and `unity-ui.md`.
