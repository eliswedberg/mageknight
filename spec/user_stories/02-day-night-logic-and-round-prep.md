# User Story: Day / Night Logic & Round Preparation

**User story:** As a system, I want correct **Day/Night** timing, **End of Round** handling, **round-start refresh** of shared components, and **per-player** cleanup so each round matches Ultimate Edition flow.

**Rules authority:** [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `game_flow`; PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf) when in doubt.

---

## Round structure (summary)

1. **Round Start:** Flip Day/Night, swap available Tactics sets (Day deck vs Night deck), reroll **all** Source dice, enforce **basic-color majority** on Source (reroll Gold/Black until at least half are Red/Blue/White/Green).
2. **Tactics Selection:** Lowest **Fame** first; ties broken by **later position in previous round order** (see story 03).
3. **Player Turns:** Until End of Round is announced.
4. **End of Round:** See below, then begin next round (back to Round Start).

---

## End of Round — when it can be announced

- A player whose **Deed deck is empty** at the **start** of their turn may **announce End of Round** **instead of taking that turn**.
- If their **hand is also empty**, they **must** announce End of Round (cannot take a normal turn).
- After announcement, **each other player takes exactly one final turn** in order; then perform **cleanup** and start the **next** round.

*(Align exact wording with the PDF; the conditions above mirror [`game_rules.json`](spec/definitions/game_rules.json) `game_flow.End of Round`.)*

---

## End of Round — cleanup (before next Round Start)

Per [`game_rules.json`](spec/definitions/game_rules.json):

- Every player **shuffles their discard pile into their Deed deck**.
- **All Units readied** (no longer exhausted for the new round).
- **Remove all mana tokens** from play (**crystals** stay in inventory).
- Then apply **Round Start** steps for the new round (flip Day/Night, tactics, reroll Source, refresh offers as in your engine — see below).

**Rest turn (not the same as End of Round):** A player may take a **Rest** turn during the round; **Standard Rest** = discard **one non-Wound** and **all Wounds** from hand; **Slow Recovery** = only if hand contains **only** Wounds, discard **one Wound**. See story 09.

---

## Round preparation — offers (match rulebook + digital data)

After flipping Day/Night and rerolling Source:

- **Unit offer:** Return current unit offer cards to **bottom** of their decks; deal new Unit cards equal to **players + 2** (Ultimate Edition common table rule).
- **Advanced Action / Spell offers:** Remove **lowest** slot card to bottom of deck, shift others down, fill **top** slot from deck (staircase refresh).

*(If scenario or PDF varies slot count, follow PDF and [`scenarios.json`](spec/definitions/scenarios.json) special rules.)*

---

## Mana color restrictions by time of day

| Period | Gold | Black |
|--------|------|-------|
| **Day** | May be used as any **basic** color | **Not** usable; Black dice in Source are **depleted** (unusable) |
| **Night** | **Not** usable; Gold dice depleted | May be used (Strong spell effects, etc.) per rules |

- **Basic** colors: Red, Blue, White, Green — always legal when on a die you are allowed to use.
- Any Gold showing during Night or Black during Day in the Source should behave as **unusable** for that round (depleted), consistent with the rulebook.

---

## Implementation pointers

| Area | Location |
|------|----------|
| Phase / day flag | [`GameStateModel`](src/MageKnightOnline.Core/GameState/GameStateModel.cs) (`IsDay`, `Phase`, `Round`) |
| End round / cleanup | [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) (`EndRound`, mana pool reroll, unit ready) |
| Flow spec | [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) |
| Narrative rules | [`spec/rules/01_Game_Structure.md`](spec/rules/01_Game_Structure.md) |

---

## Acceptance criteria

- [ ] Day and Night alternate at round boundaries; tactics pools switch Day vs Night decks.
- [ ] Source dice rerolled at round start; basic-majority constraint applied.
- [ ] End of Round: empty deck optional announcement; empty deck + empty hand forces announcement; other players get one final turn each.
- [ ] Cleanup: shuffle discards into deed decks, ready all units, clear mana tokens (not crystals).
- [ ] Gold/Black availability matches Day/Night; wrong-time dice treated as depleted in Source.
- [ ] Unit and AA/Spell offers refresh per staircase / P+N+2 unit rule.
