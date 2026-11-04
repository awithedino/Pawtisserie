---
paths:
  - "**/Editor/**"
---
# Editor scripts

- Editor-only code lives in an `Editor` folder or an Editor-only asmdef (`Paw.Editor`). No `UnityEditor` in runtime code (wrap in `#if UNITY_EDITOR` if unavoidable).
- Editor tools must be idempotent and undoable (`Undo.RecordObject`), never run on domain reload automatically, and never touch `ProjectSettings` silently.
- Menu root is `Paw/`.
