# User Story: Game Setup & Initialization

**User story:** As a system, I want to initialize the game board, shared components, per-player state, and decks so a new session matches the chosen scenario and Mage Knight Ultimate Edition setup rules.

**Rules authority:** When this document disagrees with [`spec/definitions/*.json`](spec/definitions/) or [`spec/rules/`](spec/rules/), resolve using *Mage Knight Board Game Ultimate Edition Rule Book (September 2018)* at [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf) (add the PDF locally if missing from the repo).

---

## Shared / global setup

1. **Day/Night:** Round 1 is **Day** (`IsDay = true`). First round begins in **Tactics Selection** (see story 03).
2. **Fame and Reputation:** Each player starts at **Fame 0** and **Reputation** at the starting space (Wanderer / modifier 0 per [`spec/definitions/reputation.json`](spec/definitions/reputation.json) `is_start`).
3. **Mana Source (digital):** Maintain a shared pool of **(player count + 2)** mana dice (colors per game definition). After rolling, **at least half** the dice must show **basic** colors (Red, Blue, White, Green); reroll **Black** and **Gold** until that condition holds (see [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `game_flow` → Round Start).
4. **Offers (digital):** Shuffle and prepare **Advanced Actions**, **Spells**, **Artifacts**, **Regular Units**, **Elite Units** decks; build **Ruins** token deck from [`spec/definitions/ruins.json`](spec/definitions/ruins.json) counts. Enemy tokens are grouped by **enemy type** into separate facedown supplies (one deck per type in code: `DeckState.EnemyDecks`).
5. **Map decks:** From [`spec/definitions/scenarios.json`](spec/definitions/scenarios.json), read `tiles_deck.countryside`, `core`, `cities` and build three draw piles using [`spec/definitions/map_tiles.json`](spec/definitions/map_tiles.json) (filter by `back_type`, exclude starting tile). Counts vary by scenario.
6. **Starting map:** Place the **starting tile** (e.g. `tile_01_start`), reveal its hexes, mark **Portal** (or equivalent) conquered. Unrevealed adjacent spaces exist for exploration (see story 05).
7. **Physical-table analog (reference only):** Seven facedown piles each for **Enemy** and **Ruin** tokens; **Level** token stack showing current armor/hand; one **Command** token for recruitment — mirror these in state fields, not as UI requirements.

---

## Per-player setup

1. **Hero:** Assign `HeroId` from lobby; load hero from [`spec/definitions/heroes.json`](spec/definitions/heroes.json).
2. **Deed deck:** Build from [`spec/definitions/basic_actions.json`](spec/definitions/basic_actions.json): include each card `count_per_hero` times, filtered by `heroes` list (empty = all heroes). **Shuffle**, draw **5** to hand, remainder is **Deed deck**.
3. **Starting stats:** **Level 1**, **Armor 2**, **Hand limit 5** (matches top of level track / [`spec/definitions/leveling.json`](spec/definitions/leveling.json) level 1). **Fame 0**.
4. **Command tokens (recruitment limit):** Rules use one **Command** token at game start for recruiting; maximum units equals available Command tokens. **Implementation note:** [`GameStateInitializer`](src/MageKnightOnline.Core/GameState/GameStateInitializer.cs) currently sets `CommandTokens = 1`; [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) uses `CommandTokensByLevel` starting at **2** for level 1 — align initializer and level-up with the PDF + [`leveling.json`](spec/definitions/leveling.json) `command_tokens` in a dedicated code task if they still differ after verification.
5. **Empty areas:** No units, no skills, empty discard; crystals and mana tokens at zero unless scenario says otherwise.

---

## Scenario-specific rules

- Load **scenario** by id: rounds limit, `city_levels` for city defenses, `special_rules`, PvP flags, etc. ([`scenarios.json`](spec/definitions/scenarios.json)).
- Apply any scenario overrides before the first tactics selection.

---

## Implementation pointers

| Area | Location |
|------|----------|
| Full state init | [`src/MageKnightOnline.Core/GameState/GameStateInitializer.cs`](src/MageKnightOnline.Core/GameState/GameStateInitializer.cs) |
| State shape | [`src/MageKnightOnline.Core/GameState/GameStateModel.cs`](src/MageKnightOnline.Core/GameState/GameStateModel.cs) |
| Definitions load | `IGameDefinitionService` / `GameDefinitionService` (Web + Core) |

---

## Acceptance criteria

- [ ] New game has `Round = 1`, `IsDay = true`, phase appropriate for first tactics selection.
- [ ] Each player has a shuffled Deed deck, hand of 5, correct hero filter on basic actions.
- [ ] Global decks and map tile piles match selected scenario counts and definitions.
- [ ] Starting tile and hexes match `map_tiles.json` / initializer mapping.
- [ ] Mana pool size is `(players + 2)` and basic-color majority rule is enforceable (reroll black/gold as needed).
- [ ] Reputation/Fame initialized per definitions.
