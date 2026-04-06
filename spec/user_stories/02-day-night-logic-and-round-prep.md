# User Story: Day Time / Night Time Logic & Round Preparation

**User Story:** As a system, I want to manage the transition between Day and Night rounds so that the environment, offers, and mana availability accurately shift.

**Tasks for AI Agent:**
* **Investigate the codebase:** Locate the round progression logic and implement the trigger and preparation steps for alternating rounds.
* **Round End Trigger:** Implement the logic where if a player's Deed deck is empty at the start of their turn, they may announce the "End of the Round". Each other player takes one final turn before the Round officially ends.
* **Round Preparation Steps:**
  * Flip the Day/Night board to the opposite time of day.
  * Reroll all mana dice in the Source.
  * Refresh the Unit offer: put all current cards on the bottom of their decks and deal new Unit cards equal to the number of players plus 2.
  * Refresh the Advanced Action and Spell offers: remove the lowest position card to the bottom of the deck, shift the remaining cards down, and draw a new card to the top position.
  * Each player readies all Units, shuffles their Deed cards, and draws up to their Hand limit.
* **Mana Restrictions:** Implement logic so that gold mana can be used as any basic color during the Day, but black mana cannot be used. At Night, black mana powers strong effects, but gold mana cannot be used. Any black dice in the Source during the Day, or gold dice during the Night, are immediately depleted and cannot be used.