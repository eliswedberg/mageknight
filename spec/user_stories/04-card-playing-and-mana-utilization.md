# User Story: Card Playing, Effects, and Mana Utilization

**User Story:** As a player, I want to play Deed cards from my hand and utilize the mana Source so that I can perform actions and trigger strong effects.

**Tasks for AI Agent:**
* **Investigate the codebase:** Process card rules, mana spending mechanics, and stacked card effects.
* **Card Effects:** * Action cards provide a basic effect, or a strong effect if powered by one mana of their depicted color.
  * Spells provide a basic effect using basic mana, but at Night, they can be powered by both basic mana and black mana for their strong effect.
  * Artifacts provide a basic effect or can be permanently thrown away (removed from the game) for their strong effect.
* **Sideways Cards:** Allow players to play any non-Wound card sideways to generate a basic Move 1, Influence 1, Attack 1, or Block 1.
* **Wound Card Rules:** Enforce that Wounds cannot be played, discarded normally, or thrown away.
* **Source Limitations:** Limit Source usage: a player may take exactly one mana die from the Source per turn, which must be rerolled at the end of the turn.