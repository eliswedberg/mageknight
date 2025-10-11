using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

public class TurnManagementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<TurnManagementService>> _mockLogger;
    private readonly TurnManagementService _turnManagementService;

    public TurnManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<TurnManagementService>>();
        _turnManagementService = new TurnManagementService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task StartNewTurnAsync_ShouldCreateNewTurn()
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
        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);

        // Assert
        Assert.NotNull(turn);
        Assert.Equal(gameSession.Id, turn.GameSessionId);
        Assert.Equal(player.Id, turn.CurrentPlayerId);
        Assert.Equal(GamePhase.Preparation, turn.CurrentPhase);
        Assert.Equal(0, turn.ActionPoints);
        Assert.Equal(1, turn.TurnNumber);
    }

    [Fact]
    public async Task AdvancePhaseAsync_ShouldAdvanceToNextPhase()
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

        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);

        // Act
        await _turnManagementService.AdvancePhaseAsync(turn.Id);

        // Assert
        var updatedTurn = await _context.GameTurns.FindAsync(turn.Id);
        Assert.Equal(GamePhase.Main, updatedTurn.CurrentPhase);
    }

    [Fact]
    public async Task AdvancePhaseAsync_ShouldAdvanceToEndPhase()
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

        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);
        await _turnManagementService.AdvancePhaseAsync(turn.Id); // Preparation -> Main

        // Act
        await _turnManagementService.AdvancePhaseAsync(turn.Id); // Main -> End

        // Assert
        var updatedTurn = await _context.GameTurns.FindAsync(turn.Id);
        Assert.Equal(GamePhase.End, updatedTurn.CurrentPhase);
    }

    [Fact]
    public async Task PlayActionCardAsync_ShouldReduceActionPoints()
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

        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);
        await _turnManagementService.AdvancePhaseAsync(turn.Id); // Move to Main phase

        var actionCard = new ActionCard
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            PlayerId = player.Id,
            Name = "Move",
            ActionPoints = 2,
            Location = CardLocation.Hand
        };
        _context.ActionCards.Add(actionCard);
        await _context.SaveChangesAsync();

        // Act
        await _turnManagementService.PlayActionCardAsync(actionCard.Id, turn.Id);

        // Assert
        var updatedTurn = await _context.GameTurns.FindAsync(turn.Id);
        Assert.Equal(2, updatedTurn.ActionPoints);

        var turnAction = await _context.TurnActions
            .FirstOrDefaultAsync(ta => ta.GameTurnId == turn.Id && ta.ActionCardId == actionCard.Id);
        Assert.NotNull(turnAction);
        Assert.Equal(ActionType.CardPlayed, turnAction.ActionType);
    }

    [Fact]
    public async Task PlayActionCardAsync_ShouldNotAllowInPreparationPhase()
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

        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);
        // Stay in Preparation phase

        var actionCard = new ActionCard
        {
            Id = 1,
            GameSessionId = gameSession.Id,
            PlayerId = player.Id,
            Name = "Move",
            ActionPoints = 2,
            Location = CardLocation.Hand
        };
        _context.ActionCards.Add(actionCard);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _turnManagementService.PlayActionCardAsync(actionCard.Id, turn.Id));
    }

    [Fact]
    public async Task GetCurrentTurnAsync_ShouldReturnCurrentTurn()
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

        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);

        // Act
        var currentTurn = await _turnManagementService.GetCurrentTurnAsync(gameSession.Id);

        // Assert
        Assert.NotNull(currentTurn);
        Assert.Equal(turn.Id, currentTurn.Id);
        Assert.Equal(GamePhase.Preparation, currentTurn.CurrentPhase);
    }

    [Fact]
    public async Task GetCurrentTurnAsync_ShouldReturnNullWhenNoActiveTurn()
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
        var currentTurn = await _turnManagementService.GetCurrentTurnAsync(gameSession.Id);

        // Assert
        Assert.Null(currentTurn);
    }

    [Fact]
    public async Task EndTurnAsync_ShouldMarkTurnAsCompleted()
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

        var turn = await _turnManagementService.StartNewTurnAsync(gameSession.Id, player.Id);

        // Act
        await _turnManagementService.EndTurnAsync(turn.Id);

        // Assert
        var updatedTurn = await _context.GameTurns.FindAsync(turn.Id);
        Assert.True(updatedTurn.IsCompleted);
        Assert.NotNull(updatedTurn.CompletedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
