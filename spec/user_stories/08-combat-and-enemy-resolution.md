# User Story: Combat and Enemy Resolution

**User story:** As a player, I want **combat** to run in strict **phase order**, with correct **attacks, blocks, damage assignment, and enemy abilities**, including **multiple enemies** handled separately unless a card groups them.

**Rules authority:** [`spec/definitions/game_rules.json`](spec/definitions/game_rules.json) `combat`; [`spec/definitions/combat_abilities.json`](spec/definitions/combat_abilities.json); [`spec/rules/04_Combat.md`](spec/rules/04_Combat.md); PDF [`spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`](spec/Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf).

---

## Combat phases (strict order)

1. **Ranged and Siege Attack Phase** — Play Ranged/Siege; **Siege** required to hit **Fortified** enemies in this phase; attacks must meet or exceed **Armor** to defeat.
2. **Block Phase** — Each **surviving** enemy attacks **separately**; each attack is blocked **separately**. **Elemental** attacks use **efficient** blocks only at full value; others **halved** (see `block_efficiency` in [`combat_abilities.json`](spec/definitions/combat_abilities.json)).
3. **Assign Damage Phase** — Unblocked damage assigned to **Units** (wound) and/or **Hero** (Wound cards into hand) per rules; **Poison, Paralyze, Assassination, Vampiric** apply here per definitions.
4. **Attack Phase (melee)** — Any **Attack** type unless restricted; sideways = Attack 1; **Fortified** already resolved in ranged/siege for defenders on sites per PDF.

**Fleeing:** Only in **Block** or **Attack** phase; remaining enemies auto-hit; **no rewards** ([`game_rules.json`](spec/definitions/game_rules.json) `combat.fleeing`).

---

## Enemy offensive abilities (data: `combat_abilities.json`)

Implement semantics to match JSON descriptions:

| ID | Summary |
|----|---------|
| **swift** | Need **2× Block** value to fully block |
| **brutal** | If unblocked, damage **×2** |
| **poison** | Extra wounds to Units; extra Wound to discard for Hero |
| **paralyze** | Wounded Unit destroyed; Hero discards non-Wounds |
| **assassination** | Damage cannot go to Units |
| **summon** | At **Block** phase start, replace attacker with drawn token for later phases |
| **fire_attack / ice_attack / coldfire_attack** | Element type for block efficiency |
| **cumbersome** | Spend **Move** to reduce attack value |
| **vampiric** | Armor **+1** per wound caused (scaling during combat) |

---

## Enemy defensive abilities

| ID | Summary |
|----|---------|
| **fortified** | Only **Siege** in Ranged/Siege phase; site rules may forbid all ranged vs fortified garrison |
| **physical_resistance** | Physical attacks **halved** |
| **fire_resistance / ice_resistance** | Matching element halved; ignores non-Attack color effects |
| **elusive** | Two **Armor** values; lower used in **Attack** phase **only if** all enemy attacks were blocked |
| **arcane_immunity** | Immune to non-Attack/Block effects |
| **unfortified** | Ignores site fortification |
| **defend** | Must be targeted first |

---

## Multiple enemies

- Resolve **each enemy’s attack** as its own block/damage pipeline unless a **specific card** allows **group** block (PDF + card text).

---

## Rewards

After **all** enemies defeated: Fame, level-ups, site loot, remove tokens ([`game_rules.json`](spec/definitions/game_rules.json) `combat.rewards`).

---

## Implementation pointers

| Area | Location |
|------|----------|
| Combat engine | [`GameEngine`](src/MageKnightOnline.Core/GameEngine/GameEngine.cs) combat section, `CombatTests` |
| Ability data | [`combat_abilities.json`](spec/definitions/combat_abilities.json) |
| UI | `CombatPanel` and related components |

---

## Acceptance criteria

- [ ] Phases cannot be skipped or reordered.
- [ ] Fortified + Siege rules enforced in Ranged/Siege phase.
- [ ] Each attack blocked separately; elemental halving correct.
- [ ] All listed offensive/defensive abilities behave per JSON (and PDF where JSON is thin).
- [ ] Fleeing rules and no-reward outcome correct.
- [ ] Multiple enemies processed with correct independence.
