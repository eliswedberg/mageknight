using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

public class MapTileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<MapTileService>> _mockLogger;
    private readonly Mock<SiteService> _mockSiteService;
    private readonly MapTileService _mapTileService;

    public MapTileServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<MapTileService>>();
        _mockSiteService = new Mock<SiteService>(_context, _mockLogger.Object);
        _mapTileService = new MapTileService(_context, _mockLogger.Object, _mockSiteService.Object);
    }

    [Fact]
    public async Task InitializeMapAsync_ShouldCreateStartingTile()
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
        _context.GameSessions.Add(gameSession);
        await _context.SaveChangesAsync();

        // Act
        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Assert
        var mapGraph = await _context.MapGraphs
            .Include(mg => mg.PlacedTiles)
            .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSession.Id);

        Assert.NotNull(mapGraph);
        Assert.Equal("first_reconnaissance", mapGraph.ScenarioId);
        Assert.True(mapGraph.ExplorationPhase);
        Assert.Single(mapGraph.PlacedTiles);
        
        var startingTile = mapGraph.PlacedTiles.First();
        Assert.Equal(TileType.STARTING, startingTile.TileType);
        Assert.True(startingTile.Revealed);
        Assert.Equal(7, startingTile.Hexes.Count);
    }

    [Fact]
    public async Task ExploreTileAsync_ShouldPlaceNewTileWhenValid()
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
        _context.GameSessions.Add(gameSession);
        await _context.SaveChangesAsync();

        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Act
        var result = await _mapTileService.ExploreTileAsync(gameSession.Id, 1, 0, 0);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TileType.COUNTRYSIDE, result.TileType);
        Assert.False(result.Revealed);
        Assert.Equal(1, result.Position.Q);
        Assert.Equal(0, result.Position.R);
    }

    [Fact]
    public async Task ExploreTileAsync_ShouldReturnNullWhenInvalidPosition()
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
        _context.GameSessions.Add(gameSession);
        await _context.SaveChangesAsync();

        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Act
        var result = await _mapTileService.ExploreTileAsync(gameSession.Id, 1, 10, 10);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExploreTileAsync_ShouldCreateSitesOnTile()
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
        _context.GameSessions.Add(gameSession);
        await _context.SaveChangesAsync();

        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Setup mock to return a site
        _mockSiteService.Setup(x => x.CreateSiteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(new Site { Id = 1, SiteId = "village_1", Type = SiteType.Village });

        // Act
        var result = await _mapTileService.ExploreTileAsync(gameSession.Id, 1, 0, 0);

        // Assert
        Assert.NotNull(result);
        _mockSiteService.Verify(x => x.CreateSiteAsync(
            It.IsAny<int>(), 
            It.IsAny<string>(), 
            It.IsAny<int?>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetMapGraphAsync_ShouldReturnCorrectMapGraph()
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
        _context.GameSessions.Add(gameSession);
        await _context.SaveChangesAsync();

        await _mapTileService.InitializeMapAsync(gameSession.Id);

        // Act
        var result = await _mapTileService.GetMapGraphAsync(gameSession.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(gameSession.Id, result.GameSessionId);
        Assert.Equal("first_reconnaissance", result.ScenarioId);
        Assert.True(result.ExplorationPhase);
    }

    [Fact]
    public async Task GetMapGraphAsync_ShouldReturnNullForNonExistentGame()
    {
        // Act
        var result = await _mapTileService.GetMapGraphAsync(999);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
