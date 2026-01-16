# Player Mechanics

## The Deed Deck
Your deck represents your Hero's capabilities. It evolves during the game (deck-building).
* **Basic Actions**: Starting cards for movement, influence, combat, and blocking.
* [cite_start]**Advanced Actions**: Powerful cards gained from Monasteries or leveling up[cite: 10515].
* **Spells**: High-impact magic, often requiring mana to power.
* [cite_start]**Artifacts**: Rare items found in ruins or dungeons[cite: 11768].
* [cite_start]**Wounds**: Useless cards that clog your hand, gained from taking damage[cite: 11769].

## Mana
Mana fuels strong card effects.
* **Source**: A pool of Mana Dice available to all players.
* [cite_start]**Crystals**: Stored in your Inventory for personal use (persist between turns)[cite: 11776].
* **Colors**: 
    * **Basic**: Red, Blue, White, Green.
    * **Special**: Gold (Day only), Black (Night only).
* **Temporary Mana (from Source)**:
    * You can take **one mana die** from the Source per round.
    * Taking a die gives you **temporary mana** of the same color as the die.
    * Temporary mana persists until the end of your round.
    * When you use temporary mana for a powered card effect, the mana is consumed but the die stays in the Source pool.
    * At the end of the round, the used die is rerolled and returned to the Source pool.
    * You can only have one temporary mana at a time.
* **Powered Card Effects**:
    * Cards with powered effects (indicated by `effects_powered` in card definitions) can be played with enhanced effects.
    * Basic Actions: Can use any temporary mana color for powered effects (no specific color requirement).
    * Advanced Actions/Spells: May require a specific mana color (indicated by `color` field in card definition).
    * Gold mana can be used as a substitute for any color requirement.
* **Crystals**: Can be used freely and persist between turns.

## Units
Players can recruit Units (Regular or Elite) at various sites (Villages, Keeps, Mage Towers).
* **Recruiting**: Pay Influence points equal to the unit's cost.
* **Activation**: Units are exhausted (flipped) after use and refreshed at the end of the round.
* [cite_start]**Wounding Units**: Units can take wounds to absorb damage for the Hero[cite: 11051].

## Skills
* **Leveling Up**: Gaining Fame allows you to level up, acquiring new Skill tokens and Advanced Action cards.
* [cite_start]**Common Skills**: Skills not chosen by a player are placed in the Common Skill offer for others to learn[cite: 11773].