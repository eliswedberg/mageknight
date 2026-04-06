# User Story: User Round (Turn Sequencing & Tactic Selection)

**User story:** As a player, I want **Tactics** chosen in the correct order and **turn order** derived from those cards so the round proceeds systematically.

**Rules authority:** [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `game_flow` → Tactics Selection; [`spec/definitions/tactics.json`](spec/definitions/tactics.json); PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Tactics selection order

1. Only **Day** tactics are available during Day rounds; only **Night** tactics during Night rounds (swap the offer when the Day/Night board flips — story 02).
2. **First picker:** Player with the **lowest Fame**.
3. **Tie on Fame:** The player **later in the previous round’s turn order** picks first among tied players (stated in [`game_rules.json`](spec/definitions/game_rules.json) as “later in Round Order if tied”).
4. Proceed in selection order until each player has exactly **one** Tactics card for the round.
5. Each Tactics card defines:
   - **Initiative number** (and/or ordering) for this round’s turns.
   - **Special rule** text for the round (movement discount, extra card, etc.) — resolve per card definition in [`tactics.json`](spec/definitions/tactics.json).

---

## Turn order for the round

1. After all players have chosen, **sort** by the **initiative / order value** on the chosen Tactics card (ascending: **lowest goes first**).
2. **Tie on initiative:** Use rulebook tie-break (typically relative Fame or previous round order — **confirm in PDF**; implement consistently and document in code comments).
3. **Turn sequence:** Players take turns in that fixed order. After the last player finishes, **wrap** to the first player and repeat until **End of Round** is announced (story 02).
4. **Round order tokens (physical analog):** Stack or list mirrors `GameStateModel.TurnOrder` / `CurrentPlayerIndex` in code.

---

## Implementation pointers

| Area | Location |
|------|----------|
| Tactics definitions | [`spec/definitions/tactics.json`](spec/definitions/tactics.json) |
| Engine / state | [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs), [`GameStateModel`](src/MageKnightOnline.Core/GameState/GameStateModel.cs) (`AvailableTactics`, `SelectedTactics`, `TurnOrder`, `CurrentPlayerIndex`, `Phase`) |
| UI | Tactics selection components under `MageKnightOnline.Web` (e.g. PlayGame / tactics dialog) |

---

## Acceptance criteria

- [ ] Day vs Night tactic pools are mutually exclusive per round.
- [ ] Lowest Fame selects first; Fame ties use “later in previous round order” among tied players.
- [ ] Each player ends with one tactic; turn order derived from tactic order values (low first).
- [ ] Play cycles through turn order until end-of-round flow triggers.
- [ ] Tactic special abilities are applied for the round per `tactics.json` / PDF.
