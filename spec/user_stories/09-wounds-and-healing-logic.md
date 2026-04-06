# User Story: Wounds and Healing Logic

**User Story:** As a player, I want to manage and heal my wounds so that my hand size and units are not permanently crippled.

**Tasks for AI Agent:**
* **Investigate the codebase:** Implement logic for Wound card assignment, limitations, and removal.
* **Damage Assignment:** If an attack is unblocked, calculate damage against the player's Armor and insert Wound cards into the player's hand, or allow the player to assign the damage/Wound to a Ready Unit (wounding it).
* **Rest Action:** Implement the "Rest" action, allowing a player to skip a normal turn to discard one non-Wound card and one Wound card from their hand.
* **Healing Effects:** Implement specific Healing effects (e.g., spending Influence at Villages/Monasteries, or using Healing Spells) to remove Wounds from the hand or from Wounded Units. Wounds removed this way are returned to the Wound deck.