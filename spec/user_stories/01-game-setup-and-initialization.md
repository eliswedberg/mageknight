# User Story: Game Setup & Initialization

**User Story:** As a system, I want to initialize the game board, player components, and decks so that a new game session can begin.

**Tasks for AI Agent:**
* **Investigate the codebase:** Identify where game state initialization occurs and add/modify functionality for the setup sequence.
* **Fame & Reputation Board:** Initialize the Fame and Reputation board. Place player shield tokens on the 0 space of the Fame track and the central 0 space of the Reputation track.
* **Token Stacks:** Setup 7 face-down piles for Enemy and Ruin tokens.
* **Hero Initialization:** Create the Hero's Level token stack sorted sequentially, with levels 1-2 on top displaying an Armor of 2 and a Hand limit of 5. Provide one blank Level token as the starting Command token.
* **Decks & Hands:** Build the player's starting 16-card Deed deck and draw the initial 5 cards.
* **Mana Source:** Initialize the mana Source. Roll mana dice equal to the number of players plus 2. Reroll any black and gold dice until at least half of the dice show basic colors (red, blue, white, or green).