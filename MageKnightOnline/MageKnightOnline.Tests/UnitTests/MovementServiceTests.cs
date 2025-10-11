using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

public class MovementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<MovementService>> _mockLogger;
    private readonly Mock<MapTileService> _mockMapTileService;
    private readonly MovementService _movementService;

    public MovementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<MovementService>>();
        _mockMapTileService = new Mock<MapTileService>(_context, _mockLogger.Object, null);
        _movementService = new MovementService(_context, _mockLogger.Object, _mockMapTileService.Object);
    }

    [Fact]
    public async Task MovePlayerAsync_ShouldMovePlayerToNewHex()
    {
        // Arrange
        var gameSession = new GameSession
        {
            Id = 1,
            Name = "Test Game",
            HostUserId = "test-user",
            ScenarioId = "first_reconnaissance",
            IsActive = true
        };
        var player = new GamePlayer
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            UserId = "test-user",
            Name = "Test Player",
            IsActive = true,
            CurrentHexQ = 0,
            CurrentHexR = 0
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        await _context.SaveChangesAsync();

        // Act
        var result = await _movementService.MovePlayerAsync(gameSession.Id, player.Id, 1, 0);

        // Assert
        Assert.True(result);
        
        var updatedPlayer = await _context.GamePlayers.FindAsync(player.Id);
        Assert.Equal(1, updatedPlayer.CurrentHexQ);
        Assert.Equal(0, updatedPlayer.CurrentHexR);
    }

    [Fact]
    public async Task MovePlayerAsync_ShouldReturnFalseForInvalidMove()
    {
        // Arrange
        var gameSession = new GameSession
        {
            Id = 1,
            Name = "Test Game",
            HostUserId = "test-user",
            ScenarioId = "first_reconnaissance",
            IsActive = true
        };
        var player = new GamePlayer
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            UserId = "test-user",
            Name = "Test Player",
            IsActive = true,
            CurrentHexQ = 0,
            CurrentHexR = 0
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        await _context.SaveChangesAsync();

        // Act - Try to move to a very far hex (should fail)
        var result = await _movementService.MovePlayerAsync(gameSession.Id, player.Id, 10, 10);

        // Assert
        Assert.False(result);
        
        var updatedPlayer = await _context.GamePlayers.FindAsync(player.Id);
        Assert.Equal(0, updatedPlayer.CurrentHexQ);
        Assert.Equal(0, updatedPlayer.CurrentHexR);
    }

    [Fact]
    public async Task ExploreAdjacentTileAsync_ShouldExploreNewTile()
    {
        // Arrange
        var gameSession = new GameSession
        {
            Id = 1,
            Name = "Test Game",
            HostUserId = "test-user",
            ScenarioId = "first_reconnaissance",
            IsActive = true
        };
        var player = new GamePlayer
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            UserId = "test-user",
            Name = "Test Player",
            IsActive = true,
            CurrentHexQ = 0,
            CurrentHexR = 0
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        await _context.SaveChangesAsync();

        var mockTile = new MapTileNew
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            TileType = TileType.COUNTRYSIDE,
            Position = new HexPosition { Q = 1, R = 0 },
            Revealed = false
        };

        _mockMapTileService.Setup(x => x.ExploreTileAsync(gameSession.Id, player.Id, 1, 0))
            .ReturnsAsync(mockTile);

        // Act
        var result = await _movementService.ExploreAdjacentTileAsync(gameSession.Id, player.Id, 1, 0);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TileType.COUNTRYSIDE, result.TileType);
        _mockMapTileService.Verify(x => x.ExploreTileAsync(gameSession.Id, player.Id, 1, 0), Times.Once);
    }

    [Fact]
    public async Task GetReachableHexesAsync_ShouldReturnReachableHexes()
    {
        // Arrange
        var gameSession = new GameSession
        {
            Id = 1,
            Name = "Test Game",
            HostUserId = "test-user",
            ScenarioId = "first_reconnaissance",
            IsActive = true
        };
        var player = new GamePlayer
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            UserId = "test-user",
            Name = "Test Player",
            IsActive = true,
            CurrentHexQ = 0,
            CurrentHexR = 0
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        await _context.SaveChangesAsync();

        // Act
        var reachableHexes = await _movementService.GetReachableHexesAsync(gameSession.Id, player.Id, 3);

        // Assert
        Assert.NotNull(reachableHexes);
        Assert.NotEmpty(reachableHexes);
        
        // Should include the starting position
        Assert.Contains(reachableHexes, h => h.Q == 0 && h.R == 0);
    }

    [Fact]
    public void GetMovementCost_ShouldReturnCorrectCostForTerrain()
    {
        // Arrange
        var movementService = new MovementService(_context, _mockLogger.Object, _mockMapTileService.Object);

        // Act & Assert
        Assert.Equal(1, movementService.GetMovementCost(TerrainType.Grassland, false)); // Day
        Assert.Equal(2, movementService.GetMovementCost(TerrainType.Forest, false)); // Day
        Assert.Equal(3, movementService.GetMovementCost(TerrainType.Desert, false)); // Day
        Assert.Equal(1, movementService.GetMovementCost(TerrainType.Lake, false)); // Day
        Assert.Equal(2, movementService.GetMovementCost(TerrainType.Mountain, false)); // Day
        
        // Night costs (should be higher)
        Assert.Equal(2, movementService.GetMovementCost(TerrainType.Grassland, true)); // Night
        Assert.Equal(3, movementService.GetMovementCost(TerrainType.Forest, true)); // Night
        Assert.Equal(4, movementService.GetMovementCost(TerrainType.Desert, true)); // Night
    }

    [Fact]
    public void ApplyNightModifiers_ShouldIncreaseMovementCosts()
    {
        // Arrange
        var movementService = new MovementService(_context, _mockLogger.Object, _mockMapTileService.Object);
        var dayCost = 2;
        var terrainType = TerrainType.Forest;

        // Act
        var nightCost = movementService.ApplyNightModifiers(dayCost, terrainType, true);

        // Assert
        Assert.True(nightCost > dayCost);
        Assert.Equal(3, nightCost); // Forest at night should cost 3
    }

    [Fact]
    public void ApplyNightModifiers_ShouldNotModifyCostsDuringDay()
    {
        // Arrange
        var movementService = new MovementService(_context, _mockLogger.Object, _mockMapTileService.Object);
        var dayCost = 2;
        var terrainType = TerrainType.Forest;

        // Act
        var resultCost = movementService.ApplyNightModifiers(dayCost, terrainType, false);

        // Assert
        Assert.Equal(dayCost, resultCost);
    }

    [Fact]
    public async Task LogMovementActionAsync_ShouldLogMovementAction()
    {
        // Arrange
        var gameSession = new GameSession
        {
            Id = 1,
            Name = "Test Game",
            HostUserId = "test-user",
            ScenarioId = "first_reconnaissance",
            IsActive = true
        };
        var player = new GamePlayer
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            UserId = "test-user",
            Name = "Test Player",
            IsActive = true
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        await _context.SaveChangesAsync();

        // Act
        await _movementService.LogMovementActionAsync(gameSession.Id, player.Id, 0, 0, 1, 0, 2);

        // Assert
        var action = await _context.GameActions
            .FirstOrDefaultAsync(ga => ga.GameSessionId == gameSession.Id && 
                                      ga.PlayerId == player.Id && 
                                      ga.ActionType == ActionType.Movement);

        Assert.NotNull(action);
        Assert.Equal("Moved from (0,0) to (1,0) with cost 2", action.Description);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
