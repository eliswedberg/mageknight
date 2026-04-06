# User Story: User Round (Turn Sequencing & Tactic Selection)

**User Story:** As a player, I want to select a Tactic card and take turns in the appropriate order so that the game flows systematically.

**Tasks for AI Agent:**
* **Investigate the codebase:** Adjust the round order determination logic based on Fame tracks and Tactic cards.
* **Tactic Selection:** Enforce Tactic Card selection order: the player with the lowest Fame picks their Tactic first.
* **Round Order Calculation:** Rearrange the Round Order tokens according to the selected Tactic card numbers, placing the lowest number on top.
* **Turn Sequence:** Sequence turns from top to bottom of the Round Order tokens; after the last player goes, the cycle loops back to the first player until the round ends.