# User Story: Interacting with Locals (Influence)

**User story:** As a player, I want to spend **Influence** at inhabited locations to **recruit**, **learn**, **heal**, **train**, or other site actions, with costs modified by **Reputation**.

**Rules authority:** [`spec/definitions/reputation.json`](spec/definitions/reputation.json), [`spec/definitions/sites.json`](spec/definitions/sites.json), [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `reputation` & `sites`; [`spec/README.md`](../README.md) site summary; PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Influence pool

- **Total Influence** for a check = **printed Influence** from cards/units/effects **plus** the **Reputation modifier** from the current space on the Reputation track.
- **Reputation positions** (title → modifier) — see [`reputation.json`](spec/definitions/reputation.json):

| Position | Title | Modifier |
|----------|-------|----------|
| 0 | X (Criminal) | **-5** |
| 1 | Villain | -3 |
| 2 | Thug | -2 |
| 3 | Mercenary | -1 |
| 4 | Wanderer (start) | 0 |
| 5 | Captain | +1 |
| 6 | Guardian | +2 |
| 7 | Hero | +3 |
| 8 | Legend | +5 |

---

## Criminal / cannot interact

- At **Reputation 0 (X / Criminal)**, the player **cannot interact with locals peacefully** (recruit/heal/train/learn at peaceful sites as the rulebook forbids — **confirm exact list in PDF**). Implementation should block peaceful **Influence** interactions while allowing rulebook exceptions (e.g. forced combat, special cards).

---

## Site menu (examples — full detail in `sites.json`)

Costs and availability must match **current hex** `SiteType` and **conquest** state. Examples from project spec (tune to PDF):

- **Village:** Recruit **Regular** units; **Heal** (e.g. **3 Influence**); **Plunder** (negative Rep).
- **Monastery:** Recruit; **Heal** (e.g. **2 Influence**); **Training** (e.g. **6 Influence** → take Advanced Action from offer); **Burn** (large Fame, heavy Rep loss, Artifact — per `sites.json`).
- **Mage Tower:** **Learn Spell**; recruit spellcasters; defenders **Fortified** until conquered.
- **Keep / City:** Recruit per control; City offers broader recruit/training/spell options when conquered.

Always use **numeric costs and effect ids** from [`sites.json`](spec/definitions/sites.json) as the data source; README is a secondary index.

---

## One action type per turn (reminder)

During a **regular** turn, **one** type of **action phase** activity (combat **or** local interaction **or** PvP) unless PDF overrides ([`game_rules.json`](spec/definitions/game_rules.json) `player_turn.regular_turn`).

---

## Implementation pointers

| Area | Location |
|------|----------|
| Site definitions | [`spec/definitions/sites.json`](spec/definitions/sites.json) |
| Reputation | [`spec/definitions/reputation.json`](spec/definitions/reputation.json), `GameEngine` / `PlayerState.Reputation` |
| UI / flow | `SiteInteractionPanel`, `GameEngine` site interaction methods |

---

## Acceptance criteria

- [ ] Influence total includes reputation modifier from current track position.
- [ ] Criminal (Rep 0) cannot use blocked peaceful interactions per PDF.
- [ ] Each site exposes only legal actions for hero position and game state.
- [ ] Costs match `sites.json` (and PDF where JSON is incomplete).
- [ ] Rep changes from Plunder, Burn, heroic acts, etc., update modifier for future checks.
