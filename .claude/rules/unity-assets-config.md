---
paths:
  - "**/*.asset"
  - "Assets/Data/**"
---
# ScriptableObjects, settings, data assets

- Data only; never mutate a shared SO at runtime (copy to runtime state).
- Create instances through the Editor or MCP, not by writing YAML.
- Tuning lives in config assets (cards, boosters, economy, feel). Values marked *(proto)* in the GDD are starting points; values marked **TBD** are not decided and must not be filled in.
- Changing a tuning value: propose `field · old → new · why`.
