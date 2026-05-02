# User Story: Wounds and Healing Logic

**User story:** As a player, I want **Wounds** in hand and on **Units** handled with correct **restrictions**, **Rest**, and **healing** so hand size and armies recover per Ultimate Edition.

**Rules authority:** [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) (`player_turn.rest_turn`, `cards.wounds`, `combat` assign damage); PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Wound cards (hero hand)

- **Cannot** be played for an effect.
- **Cannot** be discarded **except** via **Rest** (below), **healing** effects, or **explicit** card/site rules.
- **Count** against **hand limit**.

---

## Damage to the hero

- Unblocked damage after blocking assigns to **Hero**: each **Wound** card taken reduces remaining damage by **Hero Armor** (PDF sequence — match [`game_rules.json`](spec/definitions/game_rules.json) Assign Damage rules).
- **Poison / Paralyze** interactions modify wounds to hand or discards per [`combat_abilities.json`](spec/definitions/combat_abilities.json) (story 08).

---

## Damage to units

- Assign to **Ready** Units first per rulebook; Unit gains **Wound** markers; at **2 Wounds** Unit is **destroyed** (story 07).

---

## Rest turn (full turn — not a partial action)

Per [`game_rules.json`](spec/definitions/game_rules.json) `player_turn.rest_turn`:

1. **Standard Rest:** Discard **exactly one non-Wound** card from hand **and all Wound** cards from hand.
2. **Slow Recovery:** **Only** if **every** card in hand is a **Wound**: discard **one Wound** from hand.

Rest replaces a normal turn structure for that turn (movement/action as per PDF).

---

## Healing from sites and effects

- **Village / Monastery / Magical Glade** etc.: pay **Influence** or use site action to heal — costs in [`sites.json`](spec/definitions/sites.json) (story 06).
- **Spells / artifacts:** Remove Wounds from hand or heal Units per card text.
- Wounds removed return to the **Wound deck** (supply), not the discard pile.

---

## Implementation pointers

| Area | Location |
|------|----------|
| Wound card ids | Card definitions + `GameEngine` deck/hand |
| Rest | `GameEngine` rest / `HasRested` on `PlayerState` |
| Healing | Site interactions + heal effects in `GameEngine` |

---

## Acceptance criteria

- [ ] Wounds cannot be played or arbitrarily discarded.
- [ ] Standard Rest and Slow Recovery conditions enforced.
- [ ] Hero damage produces correct Wound draws accounting for Armor.
- [ ] Healing returns Wounds to wound supply and clears Unit wounds per effect.
- [ ] Poison/Paralyze wound side effects interact with hand discard rules.
