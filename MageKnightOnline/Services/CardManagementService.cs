using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class CardManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CardManagementService> _logger;

    public CardManagementService(ApplicationDbContext context, ILogger<CardManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MageKnightCard>> GetPlayerHandAsync(int playerId, int gameSessionId)
    {
        return await _context.PlayerCardAcquisitions
            .Include(pca => pca.Card)
            .Where(pca => pca.PlayerId == playerId && 
                         pca.GameSessionId == gameSessionId && 
                         pca.IsInHand)
            .Select(pca => pca.Card!)
            .ToListAsync();
    }

    public async Task<List<MageKnightCard>> GetPlayerDeckAsync(int playerId, int gameSessionId)
    {
        return await _context.PlayerCardAcquisitions
            .Include(pca => pca.Card)
            .Where(pca => pca.PlayerId == playerId && 
                         pca.GameSessionId == gameSessionId && 
                         pca.IsInDeck)
            .Select(pca => pca.Card!)
            .ToListAsync();
    }

    public async Task<List<MageKnightCard>> GetPlayerDiscardAsync(int playerId, int gameSessionId)
    {
        return await _context.PlayerCardAcquisitions
            .Include(pca => pca.Card)
            .Where(pca => pca.PlayerId == playerId && 
                         pca.GameSessionId == gameSessionId && 
                         pca.IsInDiscard)
            .Select(pca => pca.Card!)
            .ToListAsync();
    }

    public async Task<bool> DrawCardAsync(int playerId, int gameSessionId, int cardId)
    {
        var acquisition = await _context.PlayerCardAcquisitions
            .FirstOrDefaultAsync(pca => pca.PlayerId == playerId && 
                                       pca.GameSessionId == gameSessionId && 
                                       pca.CardId == cardId && 
                                       pca.IsInDeck);

        if (acquisition == null)
            return false;

        // Move card from deck to hand
        acquisition.IsInDeck = false;
        acquisition.IsInHand = true;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Drew card {cardId} for player {playerId}");

        return true;
    }

    public async Task<bool> PlayCardAsync(int playerId, int gameSessionId, int cardId)
    {
        var acquisition = await _context.PlayerCardAcquisitions
            .Include(pca => pca.Card)
            .FirstOrDefaultAsync(pca => pca.PlayerId == playerId && 
                                       pca.GameSessionId == gameSessionId && 
                                       pca.CardId == cardId && 
                                       pca.IsInHand);

        if (acquisition == null)
            return false;

        // Move card from hand to discard
        acquisition.IsInHand = false;
        acquisition.IsInDiscard = true;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Played card {cardId} for player {playerId}");

        return true;
    }

    public async Task<bool> DiscardCardAsync(int playerId, int gameSessionId, int cardId)
    {
        var acquisition = await _context.PlayerCardAcquisitions
            .FirstOrDefaultAsync(pca => pca.PlayerId == playerId && 
                                       pca.GameSessionId == gameSessionId && 
                                       pca.CardId == cardId && 
                                       pca.IsInHand);

        if (acquisition == null)
            return false;

        // Move card from hand to discard
        acquisition.IsInHand = false;
        acquisition.IsInDiscard = true;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Discarded card {cardId} for player {playerId}");

        return true;
    }

    public async Task<bool> ShuffleDiscardIntoDeckAsync(int playerId, int gameSessionId)
    {
        var discardCards = await _context.PlayerCardAcquisitions
            .Where(pca => pca.PlayerId == playerId && 
                         pca.GameSessionId == gameSessionId && 
                         pca.IsInDiscard)
            .ToListAsync();

        foreach (var card in discardCards)
        {
            card.IsInDiscard = false;
            card.IsInDeck = true;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Shuffled discard pile into deck for player {playerId}");

        return true;
    }

    public async Task<bool> AcquireCardAsync(int playerId, int gameSessionId, int cardId, string acquisitionMethod = "Reward")
    {
        // Check if player already has this card
        var existingAcquisition = await _context.PlayerCardAcquisitions
            .FirstOrDefaultAsync(pca => pca.PlayerId == playerId && 
                                       pca.GameSessionId == gameSessionId && 
                                       pca.CardId == cardId);

        if (existingAcquisition != null)
            return false;

        // Create new acquisition
        var acquisition = new PlayerCardAcquisition
        {
            PlayerId = playerId,
            GameSessionId = gameSessionId,
            CardId = cardId,
            AcquisitionMethod = acquisitionMethod,
            IsInDeck = true,
            IsInHand = false,
            IsInDiscard = false,
            IsPermanent = acquisitionMethod == "LevelUp" || acquisitionMethod == "Conquest"
        };

        _context.PlayerCardAcquisitions.Add(acquisition);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Acquired card {cardId} for player {playerId} via {acquisitionMethod}");

        return true;
    }

    public async Task<List<MageKnightCard>> GetAvailableCardsAsync()
    {
        return await _context.MageKnightCards
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<MageKnightCard>> GetCardsByTypeAsync(CardType cardType)
    {
        return await _context.MageKnightCards
            .Where(c => c.Type == cardType)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<MageKnightCard>> GetCardsBySubTypeAsync(CardSubType subType)
    {
        return await _context.MageKnightCards
            .Where(c => c.CardSubType == subType)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<bool> CanPlayCardAsync(int playerId, int gameSessionId, int cardId, int actionPoints, int movementPoints, int influencePoints, int attackPoints, int blockPoints)
    {
        var acquisition = await _context.PlayerCardAcquisitions
            .Include(pca => pca.Card)
            .FirstOrDefaultAsync(pca => pca.PlayerId == playerId && 
                                       pca.GameSessionId == gameSessionId && 
                                       pca.CardId == cardId && 
                                       pca.IsInHand);

        if (acquisition?.Card == null)
            return false;

        var card = acquisition.Card;

        // Check if player has enough points to play the card
        return actionPoints >= card.Cost &&
               movementPoints >= card.Move &&
               influencePoints >= card.Influence &&
               attackPoints >= card.Attack &&
               blockPoints >= card.Block;
    }

    public async Task<Dictionary<string, int>> GetCardEffectsAsync(int cardId)
    {
        var card = await _context.MageKnightCards
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card == null)
            return new Dictionary<string, int>();

        return new Dictionary<string, int>
        {
            { "Attack", card.Attack },
            { "Block", card.Block },
            { "Move", card.Move },
            { "Influence", card.Influence },
            { "Fame", card.Fame },
            { "Reputation", card.Reputation },
            { "Cost", card.Cost }
        };
    }

    public async Task<bool> InitializePlayerDeckAsync(int playerId, int gameSessionId)
    {
        // Get basic cards for starting deck
        var basicCards = await _context.MageKnightCards
            .Where(c => c.CardSubType == CardSubType.Basic)
            .Take(16) // Standard starting deck size
            .ToListAsync();

        if (basicCards.Count < 16)
            return false;

        // Create acquisitions for each card
        foreach (var card in basicCards)
        {
            var acquisition = new PlayerCardAcquisition
            {
                PlayerId = playerId,
                GameSessionId = gameSessionId,
                CardId = card.Id,
                AcquisitionMethod = "StartingDeck",
                IsInDeck = true,
                IsInHand = false,
                IsInDiscard = false,
                IsPermanent = true
            };

            _context.PlayerCardAcquisitions.Add(acquisition);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Initialized starting deck for player {playerId}");

        return true;
    }

    public async Task<bool> DrawHandAsync(int playerId, int gameSessionId, int handSize = 5)
    {
        var deckCards = await _context.PlayerCardAcquisitions
            .Where(pca => pca.PlayerId == playerId && 
                         pca.GameSessionId == gameSessionId && 
                         pca.IsInDeck)
            .Take(handSize)
            .ToListAsync();

        if (deckCards.Count < handSize)
        {
            // Shuffle discard pile into deck if not enough cards
            await ShuffleDiscardIntoDeckAsync(playerId, gameSessionId);
            
            // Try again
            deckCards = await _context.PlayerCardAcquisitions
                .Where(pca => pca.PlayerId == playerId && 
                             pca.GameSessionId == gameSessionId && 
                             pca.IsInDeck)
                .Take(handSize)
                .ToListAsync();
        }

        if (deckCards.Count < handSize)
            return false;

        // Move cards from deck to hand
        foreach (var card in deckCards)
        {
            card.IsInDeck = false;
            card.IsInHand = true;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Drew hand of {deckCards.Count} cards for player {playerId}");

        return true;
    }
}
