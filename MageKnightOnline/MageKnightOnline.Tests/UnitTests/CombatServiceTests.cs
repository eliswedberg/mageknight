using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

public class CombatServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<CombatService>> _mockLogger;
    private readonly CombatService _combatService;

    public CombatServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<CombatService>>();
        _combatService = new CombatService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task StartCombatAsync_ShouldCreateCombat()
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
        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        // Assert
        Assert.NotNull(combat);
        Assert.Equal(gameSession.Id, combat.GameSessionId);
        Assert.Equal(CombatType.SiteConquest, combat.CombatType);
        Assert.Equal(player.Id, combat.AttackingPlayerId);
        Assert.Equal(CombatStatus.Active, combat.CombatStatus);
        Assert.Equal(CombatPhase.Initiative, combat.CurrentPhase);
    }

    [Fact]
    public async Task AddPlayerParticipantAsync_ShouldAddPlayerToCombat()
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

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        // Act
        await _combatService.AddPlayerParticipantAsync(combat.Id, player.Id);

        // Assert
        var participant = await _context.CombatParticipants
            .FirstOrDefaultAsync(cp => cp.CombatId == combat.Id && cp.PlayerId == player.Id);

        Assert.NotNull(participant);
        Assert.Equal(player.Id, participant.PlayerId);
        Assert.Equal(ParticipantType.Player, participant.ParticipantType);
    }

    [Fact]
    public async Task AddSiteDefendersAsync_ShouldAddEnemiesToCombat()
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

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        var enemies = new List<Enemy>
        {
            new Enemy { Name = "Orc", Attack = 3, Block = 1, Health = 2, EnemyType = EnemyType.Orc },
            new Enemy { Name = "Goblin", Attack = 2, Block = 0, Health = 1, EnemyType = EnemyType.Goblin }
        };

        // Act
        await _combatService.AddSiteDefendersAsync(combat.Id, enemies);

        // Assert
        var participants = await _context.CombatParticipants
            .Where(cp => cp.CombatId == combat.Id && cp.ParticipantType == ParticipantType.Enemy)
            .ToListAsync();

        Assert.Equal(2, participants.Count);
        Assert.All(participants, p => Assert.NotNull(p.EnemyId));
    }

    [Fact]
    public async Task ProcessCombatActionAsync_ShouldProcessPlayerAction()
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

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        await _combatService.AddPlayerParticipantAsync(combat.Id, player.Id);

        var combatAction = new CombatAction
        {
            CombatId = combat.Id,
            PlayerId = player.Id,
            ActionType = CombatActionType.Attack,
            AttackValue = 5,
            BlockValue = 2
        };

        // Act
        await _combatService.ProcessCombatActionAsync(combatAction);

        // Assert
        var savedAction = await _context.CombatActions
            .FirstOrDefaultAsync(ca => ca.CombatId == combat.Id && ca.PlayerId == player.Id);

        Assert.NotNull(savedAction);
        Assert.Equal(CombatActionType.Attack, savedAction.ActionType);
        Assert.Equal(5, savedAction.AttackValue);
        Assert.Equal(2, savedAction.BlockValue);
    }

    [Fact]
    public async Task DetermineInitiativeOrderAsync_ShouldOrderByInitiative()
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

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        await _combatService.AddPlayerParticipantAsync(combat.Id, player.Id);

        var enemy = new Enemy { Name = "Orc", Attack = 3, Block = 1, Health = 2, EnemyType = EnemyType.Orc };
        _context.Enemies.Add(enemy);
        await _context.SaveChangesAsync();

        var enemyParticipant = new CombatParticipant
        {
            CombatId = combat.Id,
            EnemyId = enemy.Id,
            ParticipantType = ParticipantType.Enemy,
            Initiative = 2
        };
        _context.CombatParticipants.Add(enemyParticipant);
        await _context.SaveChangesAsync();

        // Act
        var initiativeOrder = await _combatService.DetermineInitiativeOrderAsync(combat.Id);

        // Assert
        Assert.NotNull(initiativeOrder);
        Assert.NotEmpty(initiativeOrder);
    }

    [Fact]
    public async Task CheckCombatEndConditionsAsync_ShouldEndCombatWhenAllEnemiesDefeated()
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

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        await _combatService.AddPlayerParticipantAsync(combat.Id, player.Id);

        var enemy = new Enemy { Name = "Orc", Attack = 3, Block = 1, Health = 0, EnemyType = EnemyType.Orc }; // Health = 0 (defeated)
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
    public async Task CheckCombatEndConditionsAsync_ShouldNotEndCombatWhenEnemiesAlive()
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

        var combat = await _combatService.StartCombatAsync(
            gameSession.Id, 
            CombatType.SiteConquest, 
            player.Id, 
            null, 
            1);

        await _combatService.AddPlayerParticipantAsync(combat.Id, player.Id);

        var enemy = new Enemy { Name = "Orc", Attack = 3, Block = 1, Health = 2, EnemyType = EnemyType.Orc }; // Health > 0 (alive)
        _context.Enemies.Add(enemy);
        await _context.SaveChangesAsync();

        var enemyParticipant = new CombatParticipant
        {
            CombatId = combat.Id,
            EnemyId = enemy.Id,
            ParticipantType = ParticipantType.Enemy,
            IsDefeated = false
        };
        _context.CombatParticipants.Add(enemyParticipant);
        await _context.SaveChangesAsync();

        // Act
        var combatEnded = await _combatService.CheckCombatEndConditionsAsync(combat.Id);

        // Assert
        Assert.False(combatEnded);
        
        var updatedCombat = await _context.Combats.FindAsync(combat.Id);
        Assert.Equal(CombatStatus.Active, updatedCombat.CombatStatus);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
