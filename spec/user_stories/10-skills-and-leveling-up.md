# User Story: Skills and Leveling Up

**User Story:** As a player, I want to earn Fame from defeating enemies and utilize character skills so I can gain permanent advantages and level up.

**Tasks for AI Agent:**
* **Investigate the codebase:** Implement experience (Fame) thresholds, stat upgrades, and skill usage.
* **Fame Tracking:** Track Fame gains on the Fame board. 
* **Level Thresholds:** * When passing an even-numbered level threshold, trigger a Skill choice from the randomized Skill tokens offer. Also pick an Advanced Action card.
  * When passing an odd-numbered level threshold, add a new Command token to the player's Unit area, increasing their maximum unit capacity. Update base Armor and Hand Limit stats based on the topmost Level token.
* **Skill Utilization:** Implement the three types of Skills: 
  * Flip icons: Can be used once a Round on the player's turn, then flipped face down.
  * Persist icons: Placed in the center of the table and affect the game until the start of the player's next turn.
  * No special symbols: Can be used once every turn.