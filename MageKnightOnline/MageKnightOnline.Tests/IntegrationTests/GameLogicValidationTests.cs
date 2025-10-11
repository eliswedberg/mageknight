using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

/// <summary>
/// Integration tests that validate game logic against the rules from README files
/// </summary>
public class GameLogicValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<MapTileService>> _mockMapTileLogger;
    private readonly Mock<ILogger<SiteService>> _mockSiteLogger;
    private readonly Mock<ILogger<ActionCardService>> _mockCardLogger;
    private readonly Mock<ILogger<TurnManagementService>> _mockTurnLogger;
    private readonly Mock<ILogger<CombatService>> _mockCombatLogger;
    private readonly Mock<ILogger<MovementService>> _mockMovementLogger;
    
    private readonly SiteService _siteService;
    private readonly ActionCardService _actionCardService;
    private readonly TurnManagementService _turnManagementService;
    private readonly CombatService _combatService;
    private readonly MovementService _movementService;
    private readonly MapTileService _mapTileService;

    public GameLogicValidationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        
        // Setup mocks
        _mockMapTileLogger = new Mock<ILogger<MapTileService>>();
        _mockSiteLogger = new Mock<ILogger<SiteService>>();
        _mockCardLogger = new Mock<ILogger<ActionCardService>>();
        _mockTurnLogger = new Mock<ILogger<TurnManagementService>>();
        _mockCombatLogger = new Mock<ILogger<CombatService>>();
        _mockMovementLogger = new Mock<ILogger<MovementService>>();
        
        // Initialize services
        _siteService = new SiteService(_context, _mockSiteLogger.Object);
        _actionCardService = new ActionCardService(_context, _mockCardLogger.Object);
        _turnManagementService = new TurnManagementService(_context, _mockTurnLogger.Object);
        _combatService = new CombatService(_context, _mockCombatLogger.Object);
        _movementService = new MovementService(_context, _mockMovementLogger.Object, null);
        _mapTileService = new MapTileService(_context, _mockMapTileLogger.Object, _siteService);
    }

    [Fact]
    public async Task MapTileRules_StartingTile_ShouldBePlacedCorrectly()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();

        // Act
        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Assert - Validate Map_tile_rules.md requirements
        var mapGraph = await _mapTileService.GetMapGraphAsync(gameSession.Id);
        Assert.NotNull(mapGraph);
        Assert.True(mapGraph.ExplorationPhase);
        Assert.Equal("first_reconnaissance", mapGraph.ScenarioId);

        var startingTile = mapGraph.PlacedTiles.FirstOrDefault(t => t.TileType == TileType.STARTING);
        Assert.NotNull(startingTile);
        Assert.True(startingTile.Revealed);
        Assert.Equal(7, startingTile.Hexes.Count);
        Assert.Equal(0, startingTile.Position.Q);
        Assert.Equal(0, startingTile.Position.R);
    }

    [Fact]
    public async Task MapTileRules_Exploration_ShouldFollowPreconditions()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Act - Try to explore adjacent to starting tile
        var result = await _mapTileService.ExploreTileAsync(gameSession.Id, 1, 1, 0);

        // Assert - Should succeed as it's adjacent to starting tile
        Assert.NotNull(result);
        Assert.Equal(TileType.COUNTRYSIDE, result.TileType);
        Assert.False(result.Revealed); // New tiles start unrevealed
    }

    [Fact]
    public async Task MapTileRules_Exploration_ShouldFailForNonAdjacentPosition()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Act - Try to explore non-adjacent position
        var result = await _mapTileService.ExploreTileAsync(gameSession.Id, 1, 5, 5);

        // Assert - Should fail as it's not adjacent to any placed tile
        Assert.Null(result);
    }

    [Fact]
    public async Task TurnStructureRules_ShouldFollowPhaseSequence()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);

        // Act & Assert - Validate turn_structure.md requirements
        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);
        
        // Should start in Preparation phase
        Assert.Equal(GamePhase.Preparation, turn.CurrentPhase);
        Assert.Equal(0, turn.ActionPoints);

        // Advance to Main phase
        await _turnManagementService.AdvancePhaseAsync(turn.Id);
        var mainTurn = await _context.GameTurns.FindAsync(turn.Id);
        Assert.Equal(GamePhase.Main, mainTurn.CurrentPhase);

        // Advance to End phase
        await _turnManagementService.AdvancePhaseAsync(mainTurn.Id);
        var endTurn = await _context.GameTurns.FindAsync(mainTurn.Id);
        Assert.Equal(GamePhase.End, endTurn.CurrentPhase);
    }

    [Fact]
    public async Task ActionCardRules_ShouldCreateStandardDeck()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);

        // Act
        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);

        // Assert - Validate cards_and_actions.md requirements
        var cards = await _context.ActionCards
            .Where(ac => ac.GameSessionId == gameSession.Id && ac.PlayerId == player.Id)
            .ToListAsync();

        Assert.Equal(16, cards.Count); // Standard deck size
        
        // Should contain basic action types
        var cardNames = cards.Select(c => c.Name).ToList();
        Assert.Contains("Move", cardNames);
        Assert.Contains("Influence", cardNames);
        Assert.Contains("Block", cardNames);
        Assert.Contains("Attack", cardNames);
    }

    [Fact]
    public async Task ActionCardRules_ShouldOnlyAllowPlayingInMainPhase()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);
        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);
        
        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);
        var cards = await _actionCardService.DrawCardsAsync(gameSession.Id, player.Id, 1);
        var card = cards.First();

        // Act & Assert - Should fail in Preparation phase
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _turnManagementService.PlayActionCardAsync(card.Id, turn.Id));

        // Advance to Main phase
        await _turnManagementService.AdvancePhaseAsync(turn.Id);
        
        // Should succeed in Main phase
        await _turnManagementService.PlayActionCardAsync(card.Id, turn.Id);
        
        var playedCard = await _context.ActionCards.FindAsync(card.Id);
        Assert.Equal(CardLocation.Played, playedCard.Location);
    }

    [Fact]
    public async Task MovementRules_ShouldCalculateCorrectTerrainCosts()
    {
        // Arrange
        var movementService = new MovementService(_context, _mockMovementLogger.Object, null);

        // Act & Assert - Validate movement.md requirements
        // Day movement costs
        Assert.Equal(1, movementService.GetMovementCost(TerrainType.Grassland, false));
        Assert.Equal(2, movementService.GetMovementCost(TerrainType.Forest, false));
        Assert.Equal(3, movementService.GetMovementCost(TerrainType.Desert, false));
        Assert.Equal(1, movementService.GetMovementCost(TerrainType.Lake, false));
        Assert.Equal(2, movementService.GetMovementCost(TerrainType.Mountain, false));

        // Night movement costs (should be higher)
        Assert.Equal(2, movementService.GetMovementCost(TerrainType.Grassland, true));
        Assert.Equal(3, movementService.GetMovementCost(TerrainType.Forest, true));
        Assert.Equal(4, movementService.GetMovementCost(TerrainType.Desert, true));
    }

    [Fact]
    public async Task CombatRules_ShouldFollowCombatPhases()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);

        // Act
        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        // Assert - Validate combat.md requirements
        Assert.NotNull(combat);
        Assert.Equal(CombatStatus.Active, combat.CombatStatus);
        Assert.Equal(CombatPhase.Initiative, combat.CurrentPhase);
        Assert.Equal(CombatType.SiteConquest, combat.CombatType);
    }

    [Fact]
    public async Task CombatRules_ShouldEndWhenAllEnemiesDefeated()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        await _combatService.AddPlayerParticipantAsync(combat.Id, player.Id);

        // Add defeated enemy
        var enemy = new Enemy 
        { 
            Name = "Orc", 
            Attack = 3, 
            Block = 1, 
            Health = 0, // Defeated
            EnemyType = EnemyType.Orc 
        };
        _context.Enemies.Add(enemy);
        await _context.SaveChangesAsync();

        var enemyParticipant = new CombatParticipant
        {
            CombatId = combat.Id,
            EnemyId = enemy.Id,
            ParticipantType = ParticipantType.Enemy,
            IsDefeated = true
        };
        _context.CombatParticipants.Add(enemyParticipant);
        await _context.SaveChangesAsync();

        // Act
        var combatEnded = await _combatService.CheckCombatEndConditionsAsync(combat.Id);

        // Assert
        Assert.True(combatEnded);
        var updatedCombat = await _context.Combats.FindAsync(combat.Id);
        Assert.Equal(CombatStatus.Resolved, updatedCombat.CombatStatus);
    }

    [Fact]
    public async Task SiteRules_ShouldCreateSitesFromTemplates()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var hexSpace = new HexSpace
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            Q = 1,
            R = 0,
            TerrainType = TerrainType.Grassland,
            IsAccessible = true
        };
        _context.HexSpaces.Add(hexSpace);
        await _context.SaveChangesAsync();

        // Act
        var site = await _siteService.CreateSiteAsync(gameSession.Id, "village_1", hexSpace.Id);

        // Assert - Validate site_descriptions.md requirements
        Assert.NotNull(site);
        Assert.Equal("village_1", site.SiteId);
        Assert.Equal(SiteType.Village, site.Type);
        Assert.False(site.IsRevealed);
        Assert.False(site.IsConquered);
        Assert.False(site.IsBurned);
    }

    [Fact]
    public async Task SiteRules_ShouldAllowConquestAndBurning()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);
        var site = new Site
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            SiteId = "village_1",
            Type = SiteType.Village,
            IsRevealed = true,
            IsConquered = false,
            Rewards = "[{\"type\":\"fame\",\"amount\":2}]",
            Burn = "[{\"type\":\"reputation\",\"amount\":-1}]"
        };
        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        // Act - Conquer site
        await _siteService.ConquerSiteAsync(site.Id, player.Id, 1);

        // Assert - Should be conquered
        var conqueredSite = await _context.Sites.FindAsync(site.Id);
        Assert.True(conqueredSite.IsConquered);
        Assert.Equal(player.Id, conqueredSite.ConqueredByPlayerId);

        // Reset for burning test
        conqueredSite.IsConquered = false;
        conqueredSite.ConqueredByPlayerId = null;
        await _context.SaveChangesAsync();

        // Act - Burn site
        await _siteService.BurnSiteAsync(site.Id, player.Id, 1);

        // Assert - Should be burned
        var burnedSite = await _context.Sites.FindAsync(site.Id);
        Assert.True(burnedSite.IsBurned);
        Assert.Equal(player.Id, burnedSite.BurnedByPlayerId);
    }

    [Fact]
    public async Task GameStateRules_ShouldMaintainConsistentState()
    {
        // Arrange
        var gameSession = await CreateTestGameSession();
        var player = await CreateTestPlayer(gameSession.Id);

        // Act - Initialize complete game state
        await _mapTileService.InitializeMapAsync(gameSession.Id);
        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);
        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);

        // Assert - Validate README_MAIN.md requirements
        var mapGraph = await _mapTileService.GetMapGraphAsync(gameSession.Id);
        var cards = await _actionCardService.GetPlayerHandAsync(gameSession.Id, player.Id);
        var currentTurn = await _turnManagementService.GetCurrentTurnAsync(gameSession.Id);

        Assert.NotNull(mapGraph);
        Assert.True(mapGraph.ExplorationPhase);
        Assert.NotNull(cards);
        Assert.NotNull(currentTurn);
        Assert.Equal(turn.Id, currentTurn.Id);
        Assert.Equal(GamePhase.Preparation, currentTurn.CurrentPhase);
    }

    private async Task<GameSession> CreateTestGameSession()
    {
        var gameSession = new GameSession
        {
            Id = 1,
            Name = "Test Game",
            HostUserId = "test-user",
            ScenarioId = "first_reconnaissance",
            IsActive = true
        };
        _context.GameSessions.Add(gameSession);
        await _context.SaveChangesAsync();
        return gameSession;
    }

    private async Task<GamePlayer> CreateTestPlayer(int gameSessionId)
    {
        var player = new GamePlayer
        {
            Id = 1,
            GameSessionId = gameSessionId,
            UserId = "test-user",
            Name = "Test Player",
            IsActive = true,
            CurrentHexQ = 0,
            CurrentHexR = 0
        };
        _context.GamePlayers.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
