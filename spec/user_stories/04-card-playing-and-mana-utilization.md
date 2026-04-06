# User Story: Card Playing, Effects, and Mana Utilization

**User story:** As a player, I want to play **Deed** cards, spend **mana** from the Source/crystals/tokens, and respect **Wounds** and **sideways** limits so actions match Ultimate Edition.

**Rules authority:** [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) (`player_turn`, `mana`, `cards`); card JSON [`basic_actions.json`](spec/definitions/basic_actions.json), [`advanced_actions.json`](spec/definitions/advanced_actions.json), [`spells.json`](spec/definitions/spells.json), [`artifacts.json`](spec/definitions/artifacts.json); PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Card types — basic vs powered

- **Basic Actions:** Always offer a **basic** effect; **powered** effect uses **one** mana of a depicted color (any basic color can power most basics — see PDF).
- **Advanced Actions:** Basic effect; **powered** may require **specific** color(s) per card text.
- **Spells:** **Basic** effect using appropriate **basic** mana; **Night** — **strong** effect may use **basic + Black** per spell rules (see PDF / card text).
- **Artifacts:** **Basic** effect **or** **throw away permanently** (remove from game) for **strong** effect.

---

## Playing sideways

- Any **non-Wound** Deed card may be discarded for **+1** of its **primary** type: Move 1, Influence 1, Attack 1, Block 1, or Heal 1 (per [`game_rules.json`](spec/definitions/game_rules.json) `cards.playing_sideways`).
- **Limit:** **Normally one** card played sideways **per turn** unless a Skill or other effect explicitly allows more ([`game_rules.json`](spec/definitions/game_rules.json)).
- **Combat:** In the **Attack** phase, sideways cards count as **Attack 1** (see `combat` in `game_rules.json`).

---

## Wounds

- **Cannot** be played for an effect, **cannot** be discarded except via **Rest** or **specific healing/cleanse** effects.
- Count toward **hand size**.

---

## Mana — Source (shared dice)

- A player may take **at most one** die from the Source **per turn** (any turn type unless rules forbid).
- That die provides **temporary** mana of its color for the turn; at **end of turn** the die is **returned and rerolled** into the Source ([`game_rules.json`](spec/definitions/game_rules.json) `mana.source`, `end_of_turn`).
- **Day/Night:** Gold vs Black legality and depletion — story 02.

---

## Mana — Crystals

- Personal, **persist** across turns and rounds.
- **No** per-turn limit on how many crystals you **spend** (unlike Source dice).

---

## Mana — Mana tokens

- Temporary; cleared at **round** end with other mana tokens (story 02). **Crystals** are not mana tokens.

---

## End of turn (card/mana related)

Per [`game_rules.json`](spec/definitions/game_rules.json) `end_of_turn` (full turn, not Rest-only):

1. Return and reroll Source dice used this turn.
2. Forced withdrawal if applicable (not on safe space).
3. Discard played cards; return mana tokens (keep crystals).
4. Combat rewards and level-ups.
5. Optional hand discard (at least one card if nothing was played — PDF detail).
6. Draw up to **hand limit**.

---

## Implementation pointers

| Area | Location |
|------|----------|
| Play / powered | [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) (play card, mana consumption) |
| Sideways | `UseCardSideways` in `GameEngine`; enforce **once per turn** if not already |
| Definitions | `GameDefinitionService`, JSON under `wwwroot/data/definitions/` (synced from `spec/definitions/`) |

---

## Acceptance criteria

- [ ] Basic/Advanced/Spell/Artifact resolution matches card definitions and powered costs.
- [ ] Sideways: +1 correct type; **one per turn** unless ability overrides.
- [ ] Wounds cannot be played or freely discarded.
- [ ] At most one Source die per turn; reroll at end of turn.
- [ ] Crystals usable without the Source limit; persist correctly.
- [ ] End-of-turn sequence includes discard/draw and mana return per rules.
