# User Story: Unit Recruitment and Management

**User story:** As a player, I want to **recruit** Units up to my **Command** limit, **activate** them once per round, track **Ready / Spent / Wounded**, and handle **Banner** artifacts per Ultimate Edition.

**Rules authority:** [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `units`; [`spec/definitions/units.json`](spec/definitions/units.json); PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Command tokens (capacity)

- A player cannot **recruit** or **keep** more Units than they have **Command tokens** (maximum concurrent Units = `CommandTokens` / equivalent).
- **Gaining** Command tokens: primarily **odd-numbered level ups** (see story 10 / [`leveling.json`](spec/definitions/leveling.json)). **Implementation note:** align [`GameStateInitializer`](src/MageKnightOnline.Core/GameState/GameStateInitializer.cs) with [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) level arrays (story 01).

---

## Recruiting

- Pay **Influence** (modified by Reputation — story 06) and take the Unit from the offer or site rules.
- New Unit enters the **Unit area** with a **Command token** on it → **Ready**.

---

## Ready, spent (exhausted), refresh

- **Ready:** Unit has not been activated this round (face-up with token in physical game).
- **Activate:** Spend the activation for this round — Unit becomes **Spent** / exhausted (face-down) until **round end**.
- **Round end:** All **non-wounded** spent Units become **Ready** again ([`game_rules.json`](spec/definitions/game_rules.json) `units.using_units`).

---

## Wounded units

- When assigned damage, a Unit may take **Wounds** (Wound cards on the Unit).
- A Unit with **2 Wounds** is **destroyed** and **removed from the game** ([`game_rules.json`](spec/definitions/game_rules.json) `units.wounding_units`).
- **Wounded** Units typically **cannot** be activated until healed (PDF detail — implement per rulebook).

---

## Banner artifacts

- **Banner** may be assigned to a Unit **any time** (per original user story / PDF).
- If the Unit is **destroyed** or **disbanded**, the Banner goes to the **discard** pile (or appropriate pile per PDF).

---

## Implementation pointers

| Area | Location |
|------|----------|
| Unit state | [`UnitState`](src/MageKnightOnline.Core/GameState/GameStateModel.cs) (or equivalent), `PlayerState.Units` |
| Recruitment validation | `GameEngine` (unit limit, site rules) |
| Definitions | [`units.json`](spec/definitions/units.json), [`artifacts.json`](spec/definitions/artifacts.json) for Banners |

---

## Acceptance criteria

- [ ] Recruitment blocked when Units.Count >= CommandTokens.
- [ ] Activation toggles ready/spent; one activation per Unit per round.
- [ ] Round end readies all eligible Units.
- [ ] Two wounds on a Unit removes it from the game.
- [ ] Banners return to discard when Unit leaves play.
