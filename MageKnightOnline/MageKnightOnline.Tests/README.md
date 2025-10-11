# MageKnight Online - Test Suite

This test suite validates the game logic implementation against the requirements specified in the README files.

## Test Categories

### 1. Unit Tests
- **MapTileServiceTests**: Tests map tile placement, exploration, and adjacency rules
- **ActionCardServiceTests**: Tests action card creation, drawing, playing, and deck management
- **TurnManagementServiceTests**: Tests turn structure, phase progression, and action point management
- **CombatServiceTests**: Tests combat initiation, participant management, and resolution
- **MovementServiceTests**: Tests movement validation, terrain costs, and night modifiers
- **SiteServiceTests**: Tests site creation, revelation, interaction, conquest, and burning

### 2. Integration Tests
- **GameLogicValidationTests**: Comprehensive tests that validate game logic against README requirements

## Rules Validation

The tests validate implementation against the following rule documents:

### Map_tile_rules.md
- ✅ Starting tile placement at (0,0)
- ✅ Tile exploration preconditions (adjacency requirement)
- ✅ Tile types (STARTING, COUNTRYSIDE, CORE_NON_CITY, CORE_CITY)
- ✅ Hex space properties and relationships
- ✅ Map graph structure and exploration phase

### cards_and_actions.md
- ✅ Standard deck creation (16 cards)
- ✅ Action card types (Move, Influence, Block, Attack)
- ✅ Card location management (Deck, Hand, Played, Discard)
- ✅ Deck shuffling and drawing mechanics

### turn_structure.md
- ✅ Turn phases (Preparation, Main, End)
- ✅ Action point management
- ✅ Phase progression rules
- ✅ Turn completion and tracking

### combat.md
- ✅ Combat initiation and types
- ✅ Combat phases (Initiative, Attack, Block, Resolution)
- ✅ Participant management (Player, Enemy)
- ✅ Combat end conditions
- ✅ Combat status tracking

### movement.md
- ✅ Terrain movement costs
- ✅ Day/night modifiers
- ✅ Movement validation
- ✅ Hex-based movement system

### site_descriptions.md
- ✅ Site creation from templates
- ✅ Site revelation mechanics
- ✅ Site interaction options
- ✅ Site conquest and burning
- ✅ Site rewards and penalties

### README_MAIN.md
- ✅ Game state consistency
- ✅ Service integration
- ✅ Database persistence
- ✅ Entity relationships

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run with detailed output
dotnet test --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Data

Tests use in-memory databases to ensure:
- Fast execution
- Isolation between tests
- No external dependencies
- Consistent test data

## Coverage

The test suite aims for comprehensive coverage of:
- All service methods
- All business logic paths
- All rule validations
- Error conditions and edge cases
- Integration between services
