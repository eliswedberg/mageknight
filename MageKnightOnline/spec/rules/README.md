# Mage Knight Ultimate Edition - Rulebook Summary

## Overview
Mage Knight is a complex board game combining elements of deck-building, exploration, and RPG-style character development. Players control powerful Mage Knights exploring the Atlantean Empire, fighting enemies, and conquering cities.

## Documentation Structure
This summary is divided into the following sections:
1. **[Game Structure](01_Game_Structure.md)**: Rounds, Turns, and Day/Night cycles.
2. **[Player Mechanics](02_Player_Mechanics.md)**: Decks, Mana (including Temporary Mana system), Skills, and Units.
3. **[Movement & Exploration](03_Movement_and_Exploration.md)**: Moving on the map, terrain, Safe Movement, Flight, and Exploration rules.
4. **[Combat](04_Combat.md)**: Combat phases (including Swift phase), damage, wounds, and enemy abilities (Vampiric, Summon, Swift, Fortified, Poison, Paralyze, Brutal).
5. **[Scenarios](05_Scenarios.md)**: Scenario setup, Site Interactions (Ruins Tokens, Conquered Cities), and winning conditions.

## Implemented Features

### Mana System
* **Temporary Mana**: Players can take one mana die from the Source per round, gaining temporary mana that persists until end of round.
* **Powered Effects**: Cards with powered effects can use temporary mana. Basic Actions accept any mana color; Advanced Actions/Spells may require specific colors.
* **Mana Rerolling**: Used mana dice are rerolled and returned to Source at the end of the round.

### Movement Mechanics
* **Safe Movement**: Allows moving through enemy hexes without initiating combat.
* **Flight**: Allows ignoring terrain movement costs.
* **Exploration**: Requires 1 movement point and must be done from an edge hex. New tiles are placed edge-to-edge with automatic rotation.

### Combat Abilities
* **Swift**: Enemies attack before Ranged phase.
* **Vampiric**: Enemies heal when dealing unblocked damage or when player flees.
* **Summon**: Enemies can summon additional enemies when defeated.
* **Fortified**: Can only be damaged by Siege attacks.
* **Poison, Paralyze, Brutal**: Various status effects implemented.

### Site Interactions
* **Ruins Tokens**: Drawing and resolving tokens from Ancient Ruins (Combat or Loot tokens).
* **Conquered Cities**: Buy Fame and Learn Spell interactions.

## [cite_start]Game Setup [cite: 11723]
1.  [cite_start]**Choose Scenario**: Select a scenario from the Scenario Book[cite: 11717].
2.  [cite_start]**Hero Selection**: Players choose a Hero and take their components (Hero card, tokens, deck)[cite: 11720].
3.  [cite_start]**Map Setup**: Arrange tiles as per the scenario description[cite: 11723].
4.  **Decks Setup**:
    * [cite_start]Shuffle Artifact, Spell, Advanced Action, and Unit decks[cite: 11768, 11770, 11772, 11774].
    * [cite_start]Create offers (3 cards each) for Spells, Advanced Actions, and Units[cite: 11771].
5.  [cite_start]**Token Setup**: Sort Enemy and Ruin tokens into face-down piles[cite: 11765].
6.  **Player Area**:
    * **Deed Deck**: Your draw deck.
    * **Inventory**: Stores crystals.
    * [cite_start]**Level Tokens**: Stacked 1-10, indicating Armor and Hand Limit[cite: 11777, 11778].

> [cite_start]**Note**: Always keep the Site Description cards nearby for specific site rules[cite: 11690].