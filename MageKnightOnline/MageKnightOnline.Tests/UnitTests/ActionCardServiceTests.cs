using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MageKnightOnline.Data;
using MageKnightOnline.Models;
using MageKnightOnline.Services;

namespace MageKnightOnline.Tests;

public class ActionCardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<ActionCardService>> _mockLogger;
    private readonly ActionCardService _actionCardService;

    public ActionCardServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<ActionCardService>>();
        _actionCardService = new ActionCardService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateStandardDeckAsync_ShouldCreateCorrectNumberOfCards()
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
        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);

        // Assert
        var cards = await _context.ActionCards
            .Where(ac => ac.GameSessionId == gameSession.Id && ac.PlayerId == player.Id)
            .ToListAsync();

        Assert.Equal(16, cards.Count); // Standard deck has 16 cards
        Assert.Contains(cards, c => c.Name == "Move");
        Assert.Contains(cards, c => c.Name == "Influence");
        Assert.Contains(cards, c => c.Name == "Block");
        Assert.Contains(cards, c => c.Name == "Attack");
    }

    [Fact]
    public async Task DrawCardsAsync_ShouldDrawCorrectNumberOfCards()
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

        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);

        // Act
        var drawnCards = await _actionCardService.DrawCardsAsync(gameSession.Id, player.Id, 3);

        // Assert
        Assert.Equal(3, drawnCards.Count);
        Assert.All(drawnCards, card => Assert.Equal(CardLocation.Hand, card.Location));
    }

    [Fact]
    public async Task PlayCardAsync_ShouldMoveCardToPlayed()
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

        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);
        var cards = await _actionCardService.DrawCardsAsync(gameSession.Id, player.Id, 1);
        var cardToPlay = cards.First();

        // Act
        await _actionCardService.PlayCardAsync(cardToPlay.Id);

        // Assert
        var playedCard = await _context.ActionCards.FindAsync(cardToPlay.Id);
        Assert.Equal(CardLocation.Played, playedCard.Location);
    }

    [Fact]
    public async Task DiscardCardAsync_ShouldMoveCardToDiscard()
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

        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);
        var cards = await _actionCardService.DrawCardsAsync(gameSession.Id, player.Id, 1);
        var cardToDiscard = cards.First();

        // Act
        await _actionCardService.DiscardCardAsync(cardToDiscard.Id);

        // Assert
        var discardedCard = await _context.ActionCards.FindAsync(cardToDiscard.Id);
        Assert.Equal(CardLocation.Discard, discardedCard.Location);
    }

    [Fact]
    public async Task ShuffleDeckAsync_ShouldShuffleDeckCards()
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

        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);

        // Get initial deck order
        var initialDeck = await _context.ActionCards
            .Where(ac => ac.GameSessionId == gameSession.Id && 
                        ac.PlayerId == player.Id && 
                        ac.Location == CardLocation.Deck)
            .OrderBy(ac => ac.DeckOrder)
            .Select(ac => ac.Id)
            .ToListAsync();

        // Act
        await _actionCardService.ShuffleDeckAsync(gameSession.Id, player.Id);

        // Assert
        var shuffledDeck = await _context.ActionCards
            .Where(ac => ac.GameSessionId == gameSession.Id && 
                        ac.PlayerId == player.Id && 
                        ac.Location == CardLocation.Deck)
            .OrderBy(ac => ac.DeckOrder)
            .Select(ac => ac.Id)
            .ToListAsync();

        // The deck should be shuffled (very unlikely to be in same order)
        Assert.NotEqual(initialDeck, shuffledDeck);
    }

    [Fact]
    public async Task GetPlayerHandAsync_ShouldReturnOnlyHandCards()
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

        await _actionCardService.CreateStandardDeckAsync(gameSession.Id, player.Id);
        await _actionCardService.DrawCardsAsync(gameSession.Id, player.Id, 5);

        // Act
        var hand = await _actionCardService.GetPlayerHandAsync(gameSession.Id, player.Id);

        // Assert
        Assert.Equal(5, hand.Count);
        Assert.All(hand, card => Assert.Equal(CardLocation.Hand, card.Location));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
