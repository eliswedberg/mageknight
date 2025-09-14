# Mage Knight Board Game Rules Implementation Guide

## Table of Contents
1. [Game Overview](#game-overview)
2. [Game Setup](#game-setup)
3. [Turn Structure](#turn-structure)
4. [Card System](#card-system)
5. [Movement and Exploration](#movement-and-exploration)
6. [Combat System](#combat-system)
7. [Sites and Rewards](#sites-and-rewards)
8. [Leveling and Progression](#leveling-and-progression)
9. [Victory Conditions](#victory-conditions)
10. [Implementation Notes](#implementation-notes)

## Game Overview

Mage Knight is a deck-building, hand-management board game where players take on the role of powerful Mage Knights exploring and conquering a fantasy world. The game combines elements of deck building, tactical combat, and resource management.

### Core Concepts
- **Mage Knights**: Players control powerful heroes with unique abilities
- **Deck Building**: Players start with basic cards and acquire better ones
- **Hand Management**: Strategic card play with limited hand size
- **Exploration**: Discover new areas and sites on the map
- **Combat**: Tactical battles against monsters and fortifications
- **Fame and Reputation**: Victory points and character progression

## Game Setup

### Initial Configuration
1. **Player Count**: 1-4 players (solo, 2, 3, or 4 players)
2. **Game Length**: 3 rounds (Early, Mid, Late phases)
3. **Starting Resources**:
   - Level 1
   - 0 Fame, 0 Reputation
   - 0 Mana, 0 Crystals
   - 5 cards in hand
   - 16 cards in deck (basic cards)

### Starting Deck Composition
Each player starts with:
- 4x Basic Move cards
- 4x Basic Attack cards  
- 4x Basic Block cards
- 4x Basic Influence cards

### Map Setup
- Start with only the starting tile revealed
- All other tiles are face-down and unexplored
- Players begin at the center of the starting tile

## Turn Structure

### Round Phases
Each round consists of 3 phases:
1. **Preparation Phase** (all players simultaneously)
2. **Main Phase** (players take turns)
3. **End Phase** (all players simultaneously)

### Turn Sequence
1. **Preparation Phase**:
   - Draw 5 cards (or hand size limit)
   - Reset mana to 0
   - Reset crystals to 0
   - Apply any start-of-turn effects

2. **Main Phase**:
   - Play cards from hand
   - Move and explore
   - Attack sites or enemies
   - Use special abilities
   - Pass when done

3. **End Phase**:
   - Discard remaining hand
   - Apply end-of-turn effects
   - Check victory conditions

### Action Points
- Each card played provides action points
- Action points are used for:
  - Movement (Move points)
  - Combat (Attack points)
  - Influence (Influence points)
  - Blocking (Block points)

## Card System

### Card Types
1. **Basic Cards**: Starting cards (Move, Attack, Block, Influence)
2. **Advanced Cards**: Acquired through gameplay
3. **Spells**: Magical abilities with special effects
4. **Artifacts**: Permanent equipment
5. **Units**: Allies that can be recruited

### Card Properties
- **Cost**: Mana/Crystal cost to play
- **Move**: Movement points provided
- **Attack**: Attack points provided
- **Block**: Block points provided
- **Influence**: Influence points provided
- **Fame**: Fame points gained
- **Reputation**: Reputation change
- **Special Effects**: Unique abilities

### Hand Management
- Hand size limit: 5 cards (can be increased by artifacts)
- Must discard excess cards at end of turn
- Can hold cards between turns

## Movement and Exploration

### Movement Rules
- Move points are spent to move between tiles
- Different terrains have different movement costs:
  - Plains: 1 move point
  - Forest: 2 move points
  - Mountain: 3 move points
  - Desert: 2 move points
  - Hills: 2 move points

### Exploration
- When moving to an unexplored tile, flip it face-up
- Reveal any sites on the tile
- Sites may contain enemies, treasures, or other rewards

### Tile Types
1. **Starting Tile**: Mixed terrain, safe starting area
2. **Countryside**: Various terrains with basic sites
3. **Dungeons**: Dangerous areas with powerful enemies
4. **Cities**: Urban areas with shops and opportunities

## Combat System

### Combat Types
1. **Site Combat**: Attacking fortified locations
2. **Monster Combat**: Fighting creatures
3. **PvP Combat**: Fighting other players (in some scenarios)

### Combat Resolution
1. **Preparation**: Declare attack, gather attack points
2. **Defense**: Enemy defends with block points
3. **Damage**: Calculate damage dealt
4. **Rewards**: Gain fame, reputation, and other rewards

### Combat Mechanics
- **Attack Value**: Total attack points from cards and bonuses
- **Block Value**: Total block points from enemy
- **Damage**: Attack - Block = Damage dealt
- **Health**: Enemies have health that must be reduced to 0

### Combat Rewards
- **Fame**: Victory points for defeating enemies
- **Reputation**: Character progression
- **Crystals**: Currency for advanced cards
- **Artifacts**: Special equipment
- **Spells**: Magical abilities

## Sites and Rewards

### Site Types
1. **Villages**: Safe areas with basic rewards
2. **Keeps**: Fortified locations with better rewards
3. **Mage Towers**: Magical sites with spells
4. **Monasteries**: Religious sites with healing
5. **Dungeons**: Dangerous areas with great rewards

### Site Rewards
- **Fame**: Victory points
- **Reputation**: Character progression
- **Crystals**: Currency
- **Artifacts**: Equipment
- **Spells**: Magical abilities
- **Units**: Allies

### Site Conquest
- Some sites must be conquered through combat
- Conquered sites provide ongoing benefits
- Some sites can be visited multiple times

## Leveling and Progression

### Leveling Up
- Gain levels by spending fame points
- Each level provides:
  - Increased hand size
  - Better starting stats
  - Access to advanced cards
  - Special abilities

### Fame and Reputation
- **Fame**: Victory points and leveling currency
- **Reputation**: Character alignment and special abilities
- **Crystals**: Currency for advanced cards

### Character Progression
- Start at Level 1
- Can reach Level 5+ by end of game
- Each level provides significant benefits

## Victory Conditions

### Scoring
- **Fame**: Primary victory points
- **Reputation**: Secondary scoring
- **Conquered Sites**: Bonus points
- **Artifacts**: Bonus points
- **Spells**: Bonus points

### End Game
- Game ends after 3 rounds
- Player with highest fame wins
- Ties broken by reputation

## Implementation Notes

### Database Schema Updates Needed
1. **Card System**:
   - Add card subtypes (Basic, Advanced, Spell, Artifact, Unit)
   - Add card effects and special abilities
   - Add card acquisition methods

2. **Combat System**:
   - Add combat participants table
   - Add combat resolution tracking
   - Add damage and health tracking

3. **Site System**:
   - Add site types and subtypes
   - Add site rewards and effects
   - Add site conquest tracking

4. **Progression System**:
   - Add leveling mechanics
   - Add fame and reputation tracking
   - Add character abilities

### Game Logic Implementation
1. **Turn Management**:
   - Implement proper turn sequence
   - Handle action point calculations
   - Manage hand size limits

2. **Card Play**:
   - Validate card play legality
   - Apply card effects
   - Handle card costs

3. **Combat Resolution**:
   - Calculate attack and defense values
   - Resolve damage
   - Apply combat rewards

4. **Exploration**:
   - Handle tile revelation
   - Manage site discovery
   - Apply exploration rewards

### UI/UX Considerations
1. **Card Display**:
   - Show card costs and effects
   - Highlight playable cards
   - Display hand size limits

2. **Combat Interface**:
   - Show attack and defense values
   - Display combat results
   - Handle combat rewards

3. **Map Interface**:
   - Show explored vs unexplored tiles
   - Display site information
   - Handle movement validation

4. **Player Status**:
   - Display fame, reputation, crystals
   - Show level and abilities
   - Track hand size and deck size

### Testing Scenarios
1. **Basic Gameplay**:
   - Card play and hand management
   - Movement and exploration
   - Basic combat resolution

2. **Advanced Features**:
   - Leveling and progression
   - Advanced card acquisition
   - Complex combat scenarios

3. **Edge Cases**:
   - Hand size limits
   - Resource management
   - Victory condition checking

### Performance Considerations
1. **Database Queries**:
   - Optimize card lookups
   - Cache frequently accessed data
   - Minimize database calls

2. **Game State**:
   - Efficient state management
   - Proper caching
   - Real-time updates

3. **User Experience**:
   - Responsive UI
   - Smooth animations
   - Clear feedback

This implementation guide provides a comprehensive framework for implementing Mage Knight rules correctly in the online version. The key is to maintain the strategic depth and tactical complexity of the original game while making it accessible and enjoyable in a digital format.
