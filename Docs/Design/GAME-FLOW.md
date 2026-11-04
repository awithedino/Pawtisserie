# Game flow

Screen-to-screen flow and the rules behind it. **Stub (2026-11-03):** the GDD defines the in-level loop (GAMEPLAY-SPEC §4.2) and the session flow in one line (§4.4); the full app flow (boot, splash, menu, shop, settings) is not designed yet.

## 1. Known from the GDD

Home → level start (level number, pastry types, any new element) → play → win panel (rewards) **or** fail panel (revive offer → retry) → next level.

- A fail costs one life; winning costs nothing; quitting mid-level counts as a fail (GDD §4.4, §8.4).
- Fail panels: overflow or rack-not-cleared offers the Express box; out of moves offers +2 FLOP cards (GDD §8).
- Level 0 is a scripted tutorial with a Skip link to level 1 (GDD §13.2).

## 2. To decide before building the app shell

1. Scene list and order (Sand Drop uses Boot → SplashScreen → MainMenu → Game).
2. Does a first launch go straight to the tutorial?
3. Lives: maximum, refill time and price (GDD §8.4 TBD).
4. Where the level map and chapters (GDD §15) live.

Answer these, then write this file's real flow chart and rules table.
