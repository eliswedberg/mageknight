using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MageKnightOnline.Services;

/// <summary>
/// Service för att hantera Action Cards enligt actions.json schema
/// </summary>
public class ActionCardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActionCardService> _logger;

    public ActionCardService(ApplicationDbContext context, ILogger<ActionCardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Skapar en ny action card
    /// </summary>
    public async Task<ActionCard> CreateActionCardAsync(string cardId, string name, ActionCardType type, 
        ActionCardColor color, List<CardEffect> effects, bool strongRequiresMana = false)
    {
        var card = new ActionCard
        {
            CardId = cardId,
            Name = name,
            Type = type,
            Color = color,
            Effects = JsonSerializer.Serialize(effects),
            StrongRequiresMana = strongRequiresMana,
            Location = CardLocation.Deck
        };

        _context.ActionCards.Add(card);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created action card: {CardId} - {Name}", cardId, name);
        return card;
    }

    /// <summary>
    /// Hämtar alla effects för en card
    /// </summary>
    public List<CardEffect> GetCardEffects(ActionCard card)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CardEffect>>(card.Effects) ?? new List<CardEffect>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize card effects for card {CardId}", card.CardId);
            return new List<CardEffect>();
        }
    }

    /// <summary>
    /// Uppdaterar card effects
    /// </summary>
    public async Task UpdateCardEffectsAsync(ActionCard card, List<CardEffect> effects)
    {
        card.Effects = JsonSerializer.Serialize(effects);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Spelar en card från handen
    /// </summary>
    public async Task<bool> PlayCardAsync(ActionCard card, bool sideways = false, bool useStrongEffect = false)
    {
        if (card.Location != CardLocation.Hand)
        {
            _logger.LogWarning("Cannot play card {CardId} - not in hand", card.CardId);
            return false;
        }

        card.IsPlayed = true;
        card.IsPlayedSideways = sideways;
        card.UsingStrongEffect = useStrongEffect;
        card.Location = CardLocation.Played;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Played card {CardId} sideways={Sideways} strong={Strong}", 
            card.CardId, sideways, useStrongEffect);
        return true;
    }

    /// <summary>
    /// Flyttar en card till discard pile
    /// </summary>
    public async Task DiscardCardAsync(ActionCard card)
    {
        card.IsPlayed = false;
        card.IsPlayedSideways = false;
        card.UsingStrongEffect = false;
        card.Location = CardLocation.Discard;
        card.Position = await GetNextDiscardPositionAsync(card.PlayerId);

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Discarded card {CardId}", card.CardId);
    }

    /// <summary>
    /// Drar en card från deck till hand
    /// </summary>
    public async Task<ActionCard?> DrawCardAsync(int playerId)
    {
        var card = await _context.ActionCards
            .Where(c => c.PlayerId == playerId && c.Location == CardLocation.Deck)
            .OrderBy(c => c.Position)
            .FirstOrDefaultAsync();

        if (card == null)
        {
            _logger.LogWarning("No cards available to draw for player {PlayerId}", playerId);
            return null;
        }

        card.Location = CardLocation.Hand;
        card.Position = await GetNextHandPositionAsync(playerId);

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Drew card {CardId} for player {PlayerId}", card.CardId, playerId);
        return card;
    }

    /// <summary>
    /// Blandar discard pile tillbaka till deck
    /// </summary>
    public async Task ShuffleDiscardToDeckAsync(int playerId)
    {
        var discardCards = await _context.ActionCards
            .Where(c => c.PlayerId == playerId && c.Location == CardLocation.Discard)
            .ToListAsync();

        var random = new Random();
        var shuffledCards = discardCards.OrderBy(x => random.Next()).ToList();

        for (int i = 0; i < shuffledCards.Count; i++)
        {
            shuffledCards[i].Location = CardLocation.Deck;
            shuffledCards[i].Position = i;
            shuffledCards[i].IsPlayed = false;
            shuffledCards[i].IsPlayedSideways = false;
            shuffledCards[i].UsingStrongEffect = false;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Shuffled {Count} cards from discard to deck for player {PlayerId}", 
            shuffledCards.Count, playerId);
    }

    /// <summary>
    /// Hämtar nästa position i handen
    /// </summary>
    private async Task<int> GetNextHandPositionAsync(int playerId)
    {
        var maxPosition = await _context.ActionCards
            .Where(c => c.PlayerId == playerId && c.Location == CardLocation.Hand)
            .MaxAsync(c => (int?)c.Position) ?? -1;
        
        return maxPosition + 1;
    }

    /// <summary>
    /// Hämtar nästa position i discard pile
    /// </summary>
    private async Task<int> GetNextDiscardPositionAsync(int? playerId)
    {
        if (playerId == null) return 0;
        
        var maxPosition = await _context.ActionCards
            .Where(c => c.PlayerId == playerId && c.Location == CardLocation.Discard)
            .MaxAsync(c => (int?)c.Position) ?? -1;
        
        return maxPosition + 1;
    }

    /// <summary>
    /// Hämtar alla cards för en spelare
    /// </summary>
    public async Task<List<ActionCard>> GetPlayerCardsAsync(int playerId, CardLocation? location = null)
    {
        var query = _context.ActionCards.Where(c => c.PlayerId == playerId);
        
        if (location.HasValue)
        {
            query = query.Where(c => c.Location == location.Value);
        }

        return await query.OrderBy(c => c.Position).ToListAsync();
    }

    /// <summary>
    /// Skapar en standard deck för en spelare
    /// </summary>
    public async Task CreateStandardDeckAsync(int playerId)
    {
        var standardCards = new List<(string id, string name, ActionCardType type, ActionCardColor color, List<CardEffect> effects)>
        {
            // Basic cards - Move
            ("move_1", "Move 1", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "movement", Effect = "move", Values = new EffectValues { Move = 1 } }
            }),
            ("move_2", "Move 2", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "movement", Effect = "move", Values = new EffectValues { Move = 2 } }
            }),
            
            // Basic cards - Influence
            ("influence_1", "Influence 1", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "influence", Effect = "influence", Values = new EffectValues { Influence = 1 } }
            }),
            ("influence_2", "Influence 2", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "influence", Effect = "influence", Values = new EffectValues { Influence = 2 } }
            }),
            
            // Basic cards - Attack
            ("attack_1", "Attack 1", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "combat", Effect = "attack", Values = new EffectValues { Attack = 1 } }
            }),
            ("attack_2", "Attack 2", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "combat", Effect = "attack", Values = new EffectValues { Attack = 2 } }
            }),
            
            // Basic cards - Block
            ("block_1", "Block 1", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "combat", Effect = "block", Values = new EffectValues { Block = 1 } }
            }),
            ("block_2", "Block 2", ActionCardType.Basic, ActionCardColor.None, new List<CardEffect>
            {
                new() { Phase = "combat", Effect = "block", Values = new EffectValues { Block = 2 } }
            })
        };

        var cards = new List<ActionCard>();
        foreach (var (id, name, type, color, effects) in standardCards)
        {
            // Lägg till 2-3 kopior av varje basic card
            for (int i = 0; i < 3; i++)
            {
                var card = new ActionCard
                {
                    CardId = $"{id}_{i}",
                    Name = name,
                    Type = type,
                    Color = color,
                    Effects = JsonSerializer.Serialize(effects),
                    StrongRequiresMana = false,
                    PlayerId = playerId,
                    Location = CardLocation.Deck,
                    Position = cards.Count
                };
                cards.Add(card);
            }
        }

        _context.ActionCards.AddRange(cards);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created standard deck with {Count} cards for player {PlayerId}", cards.Count, playerId);
    }
}
