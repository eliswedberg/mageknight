# Mage Knight Ultimate Edition - Rulebook Summary

## Overview
Mage Knight is a complex board game combining elements of deck-building, exploration, and RPG-style character development. Players control powerful Mage Knights exploring the Atlantean Empire, fighting enemies, and conquering cities.

## Documentation Structure
This summary is divided into the following sections:
1. **[Game Structure](01_Game_Structure.md)**: Rounds, Turns, and Day/Night cycles.
2. **[Player Mechanics](02_Player_Mechanics.md)**: Decks, Mana (including Temporary Mana system), Skills, and Units.
3. **[Movement & Exploration](03_Movement_and_Exploration.md)**: Moving on the map, terrain, Safe Movement, Flight, and Exploration rules.
4. **[Combat](04_Combat.md)**: Combat phases (including Swift phase), damage, wounds, and enemy abilities.
5. **[Scenarios](05_Scenarios.md)**: Scenario setup, Site Interactions, and winning conditions.

## Related Definition Files

The JSON files in `../definitions/` contain all game data:

| Rule Topic | Definition Files |
|------------|------------------|
| Combat | `combat_abilities.json`, `enemies.json` |
| Turn Structure | `game_rules.json` |
| Movement | `terrain_costs.json`, `map_tiles.json` |
| Cards | `basic_actions.json`, `advanced_actions.json`, `spells.json`, `artifacts.json` |
| Units | `units.json` |
| Sites | `sites.json`, `ruins.json` |
| Leveling | `leveling.json`, `reputation.json` |
| Heroes | `heroes.json`, `hero_skills.json` |

## Complete Combat Ability Reference

### Enemy Offensive Abilities

| Ability | Effect | JSON Reference |
|---------|--------|----------------|
| **Swift** | Requires 2× Block value to fully block | `combat_abilities.json → swift` |
| **Brutal** | Deals 2× damage if unblocked | `combat_abilities.json → brutal` |
| **Poison** | Units get 2 Wounds; Hero adds Wound to discard pile | `combat_abilities.json → poison` |
| **Paralyze** | Wounded Units destroyed; Hero discards non-Wounds | `combat_abilities.json → paralyze` |
| **Assassination** | Damage cannot be assigned to Units | `combat_abilities.json → assassination` |
| **Summon** | Draws replacement enemy for Block/Damage phases | `combat_abilities.json → summon` |
| **Cumbersome** | Spend Move points to reduce attack value | `combat_abilities.json → cumbersome` |
| **Vampiric** | Armor +1 per wound caused in combat | `combat_abilities.json → vampiric` |
| **Fire Attack** | Requires Ice/Cold Fire block (others halved) | `combat_abilities.json → fire_attack` |
| **Ice Attack** | Requires Fire/Cold Fire block (others halved) | `combat_abilities.json → ice_attack` |
| **Cold Fire Attack** | Requires Cold Fire block only (all others halved) | `combat_abilities.json → coldfire_attack` |

### Enemy Defensive Abilities

| Ability | Effect | JSON Reference |
|---------|--------|----------------|
| **Fortified** | Only Siege attacks in Ranged/Siege phase | `combat_abilities.json → fortified` |
| **Physical Resistance** | Physical attacks halved | `combat_abilities.json → physical_resistance` |
| **Fire Resistance** | Fire attacks halved, ignores red effects | `combat_abilities.json → fire_resistance` |
| **Ice Resistance** | Ice attacks halved, ignores blue effects | `combat_abilities.json → ice_resistance` |
| **Elusive** | Two armor values; lower only if all blocked | `combat_abilities.json → elusive` |
| **Arcane Immunity** | Immune to non-Attack/Block effects | `combat_abilities.json → arcane_immunity` |
| **Defend** | Must be targeted before other enemies | `combat_abilities.json → defend` |
| **Unfortified** | Ignores site fortification bonus | `combat_abilities.json → unfortified` |

## Implemented Features

### Mana System
* **Temporary Mana**: Take one mana die from Source per turn, gaining temporary mana of that color.
* **Powered Effects**: Basic Actions accept any mana; Advanced Actions/Spells may require specific colors.
* **Gold Mana** (Day): Substitutes for any basic color.
* **Black Mana** (Night): Substitutes for any basic color.
* **Crystals**: Stored in Inventory, persist between turns, usable without limit.

### Movement Mechanics
* **Safe Movement**: Move through enemy hexes without combat.
* **Flight**: Ignore terrain movement costs entirely.
* **Exploration**: Costs 2 Move points from edge hex. Draw and place new tile.

### Combat Phases
1. **Ranged/Siege Attack Phase**: Attack before enemies strike.
2. **Block Phase**: Block enemy attacks (each separately).
3. **Assign Damage Phase**: Unblocked damage to Units/Hero.
4. **Attack Phase**: Melee attacks against survivors.

### Site Interactions
* **Villages**: Recruit, Heal, Plunder.
* **Monasteries**: Recruit, Heal, Training (Advanced Actions), Burn.
* **Mage Towers**: Recruit spellcasters, Learn Spells (7 Inf + 1 Mana).
* **Keeps**: Recruit when controlled. Fortified.
* **Cities**: All interactions when conquered. Fortified.
* **Adventure Sites**: Mandatory combat, specific rewards.
* **Rampaging Enemies**: Provoke by moving between adjacent spaces.

## Game Setup (from Rulebook)

1. **Choose Scenario**: Select from Scenario Book.
2. **Hero Selection**: Each player chooses a Hero and takes components.
3. **Map Setup**: Place starting tile, reveal initial tiles per scenario.
4. **Decks Setup**:
   * Shuffle Artifact, Spell, Advanced Action, and Unit decks.
   * Create offers (3 cards each) for Spells, Advanced Actions, and Units.
5. **Token Setup**: Sort Enemy and Ruin tokens into face-down piles by type.
6. **Player Area**:
   * **Deed Deck**: Your draw deck (16 starting cards).
   * **Inventory**: Stores crystals.
   * **Level Tokens**: Stacked 1-10, showing Armor and Hand Limit.

## Round Structure

1. **Round Start**:
   * Flip Day/Night board.
   * Swap Tactics cards.
   * Reroll all Source dice (ensure ≥50% basic colors).

2. **Tactics Selection**:
   * Lowest Fame player picks first.
   * Tactics determine turn order and special ability.

3. **Player Turns**:
   * Take turns in Tactics order.
   * Continue until End of Round announced.

4. **Round End**:
   * Announced by player with empty Deed deck.
   * Other players get one final turn.
   * Shuffle discards into decks.
   * Refresh all Units.
   * Remove all mana tokens.

## Source Material

Based on **Mage Knight Board Game Ultimate Edition Rule Book (September 2018)**.

For complete rules, see `../Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf`.

For implementation details, see `../definitions/game_rules.json`.
