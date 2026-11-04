# C# in Unity (always loaded)

- Lifecycle: set up in `Awake`, read other objects in `Start`. Subscribe in `OnEnable`, unsubscribe in `OnDisable`. One class per file, file name = class name.
- Unity objects: compare with `== null` / `!= null`; never `?.` or `??` on `UnityEngine.Object`.
- Serialization: `[SerializeField] private` + `[Tooltip]`; renamed fields need `[FormerlySerializedAs]`; enums are append-only.
- Statics: reset per-run statics in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- Async: no `async void` except event handlers; stop coroutines/tweens in `OnDisable`; Unity APIs on the main thread only. Unity 2022.3: no `Awaitable`; use coroutines or `destroyCancellationToken`.
- Hot paths: no allocations in `Update` (LINQ, string concat, `new` lists, boxing); cache components; pool pastries, particles and labels.
- No tuning literals in logic: values live in ScriptableObjects, serialized fields, or the named constants in `Paw.Sim.Rules` (see `PROJECT.md` §5).

## Pawtisserie architecture rules

- **Sim/view split.** Game rules (GDD section 7) go in plain C# in `Paw.Sim` (`noEngineReferences`: no `UnityEngine` types at all, not even `Vector2Int`; write your own `Cell` struct) so they run in EditMode tests, in `Tools/SimHarness`, in the aim preview and in the level solver. MonoBehaviours only present sim events and forward input.
- The sim must be deterministic: no `Random` without a seeded instance, no `Time.*`, no `DateTime`, no physics, no frame-dependent logic. Flight durations are fixed or seeded (GDD §18.2, §18.4).
- The model never reads animation state; views follow timed events.
- IDs are API: card IDs `pop`, `sweep`, `drill`, `box` and pastry IDs 0-3 are used in level data. Append only.
- Open GDD decisions (section 21.3) are not hard-coded; ask.
