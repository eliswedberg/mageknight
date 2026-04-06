# User Story: Unit Recruitment and Management

**User Story:** As a player, I want to recruit Units to my Command tokens so I can activate their special abilities during my turn.

**Tasks for AI Agent:**
* **Investigate the codebase:** Restrict Unit capacity and manage Unit statuses (Ready, Spent, Wounded).
* **Capacity Limits:** Enforce the rule that a player cannot recruit or have more Units than they have Command tokens.
* **Status Tracking:** Track Unit states: a Unit with a Command token above it is "Ready". Activating a Unit requires placing the Command token on it, marking it as "Spent".
* **Round Refresh:** Automatically "Ready" all spent, non-wounded Units at the end of each Round.
* **Banner Artifacts:** Allow Banner Artifacts to be assigned to Units at any time; if the Unit is destroyed or disbanded, the assigned Banner goes to the discard pile.