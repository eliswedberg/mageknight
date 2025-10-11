using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

public class SiteServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<SiteService>> _mockLogger;
    private readonly SiteService _siteService;

    public SiteServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<SiteService>>();
        _siteService = new SiteService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateSiteAsync_ShouldCreateSiteFromTemplate()
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
        var hexSpace = new HexSpace
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            Q = 1,
            R = 0,
            TerrainType = TerrainType.Grassland,
            IsAccessible = true
        };

        _context.GameSessions.Add(gameSession);
        _context.HexSpaces.Add(hexSpace);
        await _context.SaveChangesAsync();

        // Act
        var site = await _siteService.CreateSiteAsync(gameSession.Id, "village_1", hexSpace.Id);

        // Assert
        Assert.NotNull(site);
        Assert.Equal("village_1", site.SiteId);
        Assert.Equal(SiteType.Village, site.Type);
        Assert.Equal(gameSession.Id, site.GameSessionId);
        Assert.Equal(hexSpace.Id, site.HexSpaceId);
        Assert.False(site.IsRevealed);
        Assert.False(site.IsConquered);
    }

    [Fact]
    public async Task RevealSiteAsync_ShouldMarkSiteAsRevealed()
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
        var site = new Site
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            SiteId = "village_1",
            Type = SiteType.Village,
            IsRevealed = false
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        // Act
        await _siteService.RevealSiteAsync(site.Id, player.Id, 1);

        // Assert
        var revealedSite = await _context.Sites.FindAsync(site.Id);
        Assert.True(revealedSite.IsRevealed);
    }

    [Fact]
    public async Task InteractWithSiteAsync_ShouldProcessInteraction()
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
        var site = new Site
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            SiteId = "village_1",
            Type = SiteType.Village,
            IsRevealed = true,
            IsConquered = false,
            InteractOptions = "[{\"type\":\"heal\",\"cost\":1,\"effect\":\"heal_2\"}]"
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        // Act
        var result = await _siteService.InteractWithSiteAsync(site.Id, player.Id, 1, "heal", null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ConquerSiteAsync_ShouldMarkSiteAsConquered()
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
        var site = new Site
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            SiteId = "village_1",
            Type = SiteType.Village,
            IsRevealed = true,
            IsConquered = false,
            Rewards = "[{\"type\":\"fame\",\"amount\":2}]"
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        // Act
        await _siteService.ConquerSiteAsync(site.Id, player.Id, 1);

        // Assert
        var conqueredSite = await _context.Sites.FindAsync(site.Id);
        Assert.True(conqueredSite.IsConquered);
        Assert.Equal(player.Id, conqueredSite.ConqueredByPlayerId);
        Assert.Equal(1, conqueredSite.ConqueredOnTurn);
        Assert.NotNull(conqueredSite.ConqueredAt);
    }

    [Fact]
    public async Task BurnSiteAsync_ShouldMarkSiteAsBurned()
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
        var site = new Site
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            SiteId = "village_1",
            Type = SiteType.Village,
            IsRevealed = true,
            IsConquered = false,
            Burn = "[{\"type\":\"reputation\",\"amount\":-1}]"
        };

        _context.GameSessions.Add(gameSession);
        _context.GamePlayers.Add(player);
        _context.Sites.Add(site);
        await _context.SaveChangesAsync();

        // Act
        await _siteService.BurnSiteAsync(site.Id, player.Id, 1);

        // Assert
        var burnedSite = await _context.Sites.FindAsync(site.Id);
        Assert.True(burnedSite.IsBurned);
        Assert.Equal(player.Id, burnedSite.BurnedByPlayerId);
        Assert.NotNull(burnedSite.BurnedAt);
    }

    [Fact]
    public async Task GetSitesAsync_ShouldReturnAllSitesForGameSession()
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
        var sites = new List<Site>
        {
            new Site { Id = 1, GameSessionId = gameSession.Id, SiteId = "village_1", Type = SiteType.Village },
            new Site { Id = 2, GameSessionId = gameSession.Id, SiteId = "monastery_1", Type = SiteType.Monastery },
            new Site { Id = 3, GameSessionId = 999, SiteId = "village_2", Type = SiteType.Village } // Different game session
        };

        _context.GameSessions.Add(gameSession);
        _context.Sites.AddRange(sites);
        await _context.SaveChangesAsync();

        // Act
        var result = await _siteService.GetSitesAsync(gameSession.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.SiteId == "village_1");
        Assert.Contains(result, s => s.SiteId == "monastery_1");
        Assert.DoesNotContain(result, s => s.SiteId == "village_2");
    }

    [Fact]
    public async Task GetSitesAtHexAsync_ShouldReturnSitesAtSpecificHex()
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
        var hexSpace1 = new HexSpace
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            Q = 1,
            R = 0,
            TerrainType = TerrainType.Grassland
        };
        var hexSpace2 = new HexSpace
        {
            Id = 2,
            GameSessionId = gameSession.Id,
            Q = 2,
            R = 0,
            TerrainType = TerrainType.Forest
        };
        var sites = new List<Site>
        {
            new Site { Id = 1, GameSessionId = gameSession.Id, SiteId = "village_1", Type = SiteType.Village, HexSpaceId = hexSpace1.Id },
            new Site { Id = 2, GameSessionId = gameSession.Id, SiteId = "monastery_1", Type = SiteType.Monastery, HexSpaceId = hexSpace2.Id }
        };

        _context.GameSessions.Add(gameSession);
        _context.HexSpaces.AddRange(hexSpace1, hexSpace2);
        _context.Sites.AddRange(sites);
        await _context.SaveChangesAsync();

        // Act
        var result = await _siteService.GetSitesAtHexAsync(gameSession.Id, 1, 0);

        // Assert
        Assert.Single(result);
        Assert.Equal("village_1", result.First().SiteId);
    }

    [Fact]
    public async Task GetSitesAtHexAsync_ShouldReturnEmptyListForNoSites()
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
        var result = await _siteService.GetSitesAtHexAsync(gameSession.Id, 1, 0);

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
