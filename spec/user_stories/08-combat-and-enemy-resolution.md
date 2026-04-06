# User Story: Combat and Enemy Resolution

**User Story:** As a player, I want to engage in combat with enemies so I can defeat them, gain Fame, and conquer sites.

**Tasks for AI Agent:**
* **Investigate the codebase:** Build the Combat Phase flow and damage calculation logic.
* **Combat Phases:** Implement the 4 distinct combat steps sequentially: 
  1. Ranged/Siege Attack Phase
  2. Block Phase
  3. Damage Assignment Phase
  4. Melee Attack Phase
* **Enemy Abilities:** Parse and apply enemy abilities (e.g., Swiftness requires double block, Brutal deals double damage, Assassination prevents assigning damage to units, Vampiric increases enemy armor).
* **Multiple Enemies:** Ensure each enemy attack is handled separately unless specific group-block effects are played.