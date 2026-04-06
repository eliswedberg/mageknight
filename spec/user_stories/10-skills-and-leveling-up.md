# User Story: Skills and Leveling Up

**User story:** As a player, I want **Fame** to advance **levels**, grant **Skills** and **Advanced Actions** on even levels, **Command** capacity on odd levels, and update **Armor** / **Hand limit** from the level track per Ultimate Edition.

**Rules authority:** [`spec/definitions/leveling.json`](spec/definitions/leveling.json) (numeric thresholds and rewards), [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `leveling`, [`spec/definitions/hero_skills.json`](spec/definitions/hero_skills.json), PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Fame and timing

- **Fame** increases from defeating enemies, objectives, and card/site effects.
- **Level up** is processed when Fame **reaches or passes** the next threshold — typically **end of turn** after claiming rewards ([`game_rules.json`](spec/definitions/game_rules.json) `leveling.process`).

---

## Thresholds and stat row (canonical table)

Use [`leveling.json`](spec/definitions/leveling.json): for each `level` entry, read `xp_start` (Fame required for that level), `armor`, `hand_size`, `command_tokens` (maximum Units / Command capacity at that level), and `reward`.

| Level | Fame (xp_start) | Example reward (from JSON) |
|-------|-----------------|----------------------------|
| 1 | 0 | None |
| 2 | 3 | AdvancedAction (Offer) + Skill |
| 3 | 8 | CommandToken |
| 4 | 15 | AdvancedAction (Offer) + Skill |
| … | … | Alternating pattern through 10 |

**Even-numbered levels (2,4,6,8,10):** Choose **Skill** (from hero’s skill offer / random skill tokens per PDF) **and** take **Advanced Action** from the offer.

**Odd-numbered levels (3,5,7,9):** Gain **Command** capacity (additional Command token); **Armor** and **Hand limit** update to the **current level row** (top of level stack in the physical game).

> **Note:** [`leveling.json`](spec/definitions/leveling.json) `command_tokens` is the **authoritative** per-level capacity for implementation data. [`GameStateInitializer`](src/MageKnightOnline.Core/GameState/GameStateInitializer.cs) vs [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) `CommandTokensByLevel` must match this row for level 1 after you verify the PDF (see story 01).

---

## Skill types (physical Skill tokens — behavior categories)

Per original specification / rulebook:

1. **Flip skills:** Usable **once per round** on your turn; then flip **face-down** until refreshed per PDF.
2. **Persistent (“persist”) skills:** Token placed in **center**; effect lasts until the **start of your next turn** (or rulebook duration).
3. **No special symbol:** Usable **once per turn** (every turn).

**Digital:** Map each [`hero_skills.json`](spec/definitions/hero_skills.json) entry’s `effects` (`Passive`, `Block`, `Move`, etc.) to these cadences and to `GameEngine` skill activation.

---

## Implementation pointers

| Area | Location |
|------|----------|
| Level thresholds | [`leveling.json`](spec/definitions/leveling.json) |
| Skill definitions | [`hero_skills.json`](spec/definitions/hero_skills.json) |
| Level-up logic | [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) (Fame, level, offers) |
| Player stats | `PlayerState.Level`, `Fame`, `Armor`, `HandLimit`, `CommandTokens`, `Skills` |

---

## Acceptance criteria

- [ ] Fame increases trigger level ups at correct thresholds from `leveling.json`.
- [ ] Even levels offer Skill + Advanced Action selection; odd levels add Command per rules.
- [ ] Armor and hand size match current level row.
- [ ] Skill frequency (per turn / per round / persistent) matches PDF + `hero_skills.json` effect types.
- [ ] Command token maximum stays consistent with recruitment (story 07).
