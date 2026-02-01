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
    * **Special**: Gold (wild during Day), Black (wild during Night).
* **Using a mana die from the Source**:
    * The die **color must match** the card you want to play or power, **or** you may use the **wild color** for the current time of day: **Gold** during Day, **Black** during Night. Gold/Black then substitute for any basic color.
    * You can take **one** mana die from the Source per turn (only dice that are **not** depleted).
    * Taking a die gives you **temporary mana** of that color. It persists until the end of your turn and is consumed when you use it for a powered effect.
    * **Depleted dice**: During **Day**, any die in the Source that shows **Black** is set aside and **depleted** — it cannot be used for the rest of the round. During **Night**, any die showing **Gold** is set aside and depleted. When the round switches (Day ↔ Night at Round Start), all Source dice are rerolled and depleted dice re-enter the pool.
    * At the **end of the round**, all mana dice in the Source are rerolled.
    * You can only have one temporary mana at a time.
    * **Undo**: You can undo taking a mana die as long as you haven't done anything irreversible (played a card, moved, revealed new information, etc.).
* **Powered Card Effects**:
    * Cards with powered effects (indicated by `effects_powered` in card definitions) can be played with enhanced effects.
    * Basic Actions: Can use any temporary mana color for powered effects (no specific color requirement).
    * Advanced Actions/Spells: May require a specific mana color (indicated by `color` field in card definition). Use a die that matches that color, or the current wild (Gold on Day, Black on Night).
    * Gold mana (Day) and Black mana (Night) can substitute for any required color.
* **Crystals**: Can be used freely and persist between turns.

## Units
Players can recruit Units (Regular or Elite) at various sites (Villages, Keeps, Mage Towers).
* **Recruiting**: Pay Influence points equal to the unit's cost.
* **Activation**: Units are exhausted (flipped) after use and refreshed at the end of the round.
* [cite_start]**Wounding Units**: Units can take wounds to absorb damage for the Hero[cite: 11051].

## Skills
* **Leveling Up**: Gaining Fame allows you to level up, acquiring new Skill tokens and Advanced Action cards.
* [cite_start]**Common Skills**: Skills not chosen by a player are placed in the Common Skill offer for others to learn[cite: 11773].