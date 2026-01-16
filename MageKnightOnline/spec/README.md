** Technical
Create a online board game site, using:
- DotNet 10
- Blazor
- Entity Framework


** Overview
User shall be able to login to the site and play bordgame. At first only the game mage knight shall be possible to play.



** User case
A user shall be able to:
- Login, Logout, Create an account, etc. No verify email adress shall be needed. This version shall not support any notification with email.
- User shall be able to create new game:
-- What game to play from available games (right now only mage knigh)
-- Name of the game
-- How many players that can join, note it shall not be possible to select more or fewer players that the game supports.
-- Each game shall have its own setting that is required for the selected game, these settings shall also be possible to set when creating the game, for example what scenario etc.
-- User can join a game that have not been started, or is full, or created by the user
-- A user that have created a game shall be able to start the game even if the number of player requested have not been met. But minimum number of players must have been met.
-- A user that have create a game shall be able to cancel/delete a game, event if other players have joined.

-- When a user have join a game and the game is started, the user shall be able to goto the game and make his/her moves.

** Structure of spec
-- In Rules is files of the actual rules of the game, make sure when implementing that the rules are followed and fullfilled.
-- In definitions are the json files that describes what the game consists of, all of this must be represented in the game.
-- In entities are some example of c# classes that can help to implement the json files etc.
-- In wwwroot\images are some images for the game, try to map the images to entities like cards, maptiles etc


** Design
The design of the site shall look like boardgame gaming site, with modern look and feel.

