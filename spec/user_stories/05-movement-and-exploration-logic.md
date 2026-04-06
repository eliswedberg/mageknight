# User Story: Movement and Exploration Logic

**User Story:** As a player, I want to spend movement points to travel across different terrain hexes and reveal new map tiles so I can expand the board.

**Tasks for AI Agent:**
* **Investigate the codebase:** Add and modify functionality for grid movement, pathfinding, and map expansion.
* **Dynamic Movement Costs:** Calculate movement costs dynamically based on the terrain type of the target hex and whether it is Day or Night (e.g., forests cost more during the day, deserts cost more at night).
* **Exploration Logic:** Implement exploration: when a player moves to an edge hex and has line of sight to an empty space, allow them to spend 2 movement points to reveal and place a new tile from the Tile deck.
* **Tile Orientation:** Ensure the newly placed tile is correctly oriented according to the rulebook markings.