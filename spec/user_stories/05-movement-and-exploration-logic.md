# User Story: Movement and Exploration Logic

**User story:** As a player, I want to spend **Move** points to cross hexes with correct **terrain costs** (Day vs Night), **explore** new tiles from the correct decks, and respect **rampage** / **safe movement** / **flight** rules.

**Rules authority:** [`spec/definitions/terrain_costs.json`](spec/definitions/terrain_costs.json) (canonical numeric costs), [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `movement`, [`spec/rules/03_Movement_and_Exploration.md`](spec/rules/03_Movement_and_Exploration.md), PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Terrain movement costs (Day / Night)

Use **exact** values from [`terrain_costs.json`](spec/definitions/terrain_costs.json) for the **target hex** being entered. Summary (verify JSON if you add terrains):

| Terrain | Day | Night | Notes |
|---------|-----|-------|--------|
| Plains | 2 | 2 | |
| Hills | 3 | 3 | |
| **Forest** | **3** | **5** | **Harder at Night** (not harder by day) |
| Wasteland | 4 | 4 | |
| **Desert** | **5** | **3** | Hotter by day, cooler by night |
| Swamp | 5 | 5 | |
| Lake / Mountain / Ocean | 99 (impassable) | 99 | Unless Flight / special allows |

**Time of day** is the round’s Day/Night flag (story 02).

---

## Generating Move points

- Play cards (including **sideways** Move 1 — story 04) and effects that grant Move.
- Spend **Move** from the player’s **MovementRemaining** (or equivalent pool) when entering each hex; pathfinding must sum costs along the path.

---

## Exploration (new tiles)

Per [`game_rules.json`](spec/definitions/game_rules.json) `movement.exploration`:

1. Hero must be on an **edge** hex adjacent to an **unrevealed** map space (line of sight / adjacency per PDF).
2. Pay **2 Move** points to **draw and place** the next tile from the appropriate **tile deck** (Countryside vs Core vs City per scenario — story 01).
3. **Orientation:** New tile must match **connector** markings (roads, coastlines, wedge shape) per rulebook; digital implementation should use the same rotation rules as [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) / map placement tests.
4. **Rampaging enemies** may be placed when a tile is revealed (see [`game_rules.json`](spec/definitions/game_rules.json) `rampaging_enemies`).

---

## Rampaging enemies & provoking

- Moving **between two spaces** that are each **adjacent** to a rampaging enemy **provokes** immediate combat.
- **Challenge:** voluntary combat with an adjacent rampaging enemy.

---

## Special movement modes

- **Safe movement:** Limited uses; may pass through enemy hexes without starting combat (track `SafeMovementRemaining` or equivalent).
- **Flight:** Ignore terrain costs for remaining Flight points; still obey other rules (impassable unless PDF allows).
- **Underground travel:** Jump to Dungeons / Tombs / Mines per card rules (PDF).

---

## Implementation pointers

| Area | Location |
|------|----------|
| Terrain costs | [`terrain_costs.json`](spec/definitions/terrain_costs.json); `TerrainDefinitionService` / `GameEngine` movement validation |
| Map / tiles | [`GameStateInitializer`](src/MageKnightOnline.Core/GameState/GameStateInitializer.cs), `GameEngine` explore / place tile |
| Tests | [`MapPlacementTests`](tests/MageKnightOnline.Tests/MapPlacementTests.cs), movement tests |

---

## Acceptance criteria

- [ ] Forest costs **more at Night** than Day; Desert costs **more by Day** than Night (matches JSON).
- [ ] Impassable terrains block movement unless an effect explicitly bypasses.
- [ ] Exploration costs 2 Move from valid edge; tile comes from correct deck; orientation valid.
- [ ] Provoke and Challenge trigger combat per adjacency rules.
- [ ] Safe movement and Flight consume their pools and affect pathfinding correctly.
