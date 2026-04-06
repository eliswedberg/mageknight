# User Story: Interacting with Locals (Influence)

**User Story:** As a player, I want to spend Influence points at inhabited locations to recruit units, learn spells, or purchase actions.

**Tasks for AI Agent:**
* **Investigate the codebase:** Implement the Influence currency system and site-specific interaction menus.
* **Reputation Modifiers:** Calculate the player's total Influence points by applying their current standing on the Reputation track (positive or negative modifiers).
* **Site Interactions:** Create specific interaction checks based on the player's current hex:
  * Monasteries: Allow purchasing Advanced Actions or Healing.
  * Mage Towers: Allow purchasing Spells.
  * Villages/Keeps/Cities: Allow recruiting specific types of Units based on the site icons.