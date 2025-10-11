using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MageKnightOnline.Services;

/// <summary>
/// Service för att hantera turn structure enligt MAGE_KNIGHT_RULES_IMPLEMENTATION.md
/// </summary>
public class TurnManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ActionCardService _actionCardService;
    private readonly ILogger<TurnManagementService> _logger;

    public TurnManagementService(ApplicationDbContext context, ActionCardService actionCardService, ILogger<TurnManagementService> logger)
    {
        _context = context;
        _actionCardService = actionCardService;
        _logger = logger;
    }

    /// <summary>
    /// Startar en ny turn för en spelare
    /// </summary>
    public async Task<GameTurn> StartNewTurnAsync(int gameSessionId, int playerId)
    {
        try
        {
            var gameState = await _context.GameStates
                .FirstOrDefaultAsync(gs => gs.GameSessionId == gameSessionId);

            if (gameState == null)
            {
                throw new InvalidOperationException($"Game state not found for session {gameSessionId}");
            }

            var player = await _context.GamePlayers
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                throw new InvalidOperationException($"Player {playerId} not found");
            }

            // Avsluta föregående turn om den finns
            var previousTurn = await _context.GameTurns
                .Where(t => t.GameSessionId == gameSessionId && t.IsActive)
                .FirstOrDefaultAsync();

            if (previousTurn != null)
            {
                await EndTurnAsync(previousTurn.Id);
            }

            // Skapa ny turn
            var newTurn = new GameTurn
            {
                GameSessionId = gameSessionId,
                TurnNumber = gameState.TurnNumber,
                CurrentPlayerId = playerId,
                Phase = TurnPhase.Preparation,
                StartedAt = DateTime.UtcNow,
                IsActive = true,
                HasPassed = false,
                IsCompleted = false
            };

            _context.GameTurns.Add(newTurn);
            await _context.SaveChangesAsync();

            // Uppdatera game state
            gameState.CurrentPlayerId = playerId;
            gameState.CurrentPhase = TurnPhase.Preparation;
            gameState.TurnNumber++;
            gameState.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Starta preparation phase
            await StartPreparationPhaseAsync(newTurn.Id);

            _logger.LogInformation("Started new turn {TurnNumber} for player {PlayerId} in game {GameSessionId}", 
                newTurn.TurnNumber, playerId, gameSessionId);

            return newTurn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start new turn for player {PlayerId} in game {GameSessionId}", 
                playerId, gameSessionId);
            throw;
        }
    }

    /// <summary>
    /// Startar preparation phase - drar kort och resetar resurser
    /// </summary>
    public async Task<bool> StartPreparationPhaseAsync(int turnId)
    {
        try
        {
            var turn = await _context.GameTurns
                .Include(t => t.CurrentPlayer)
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null) return false;

            turn.Phase = TurnPhase.Preparation;

            // Dra 5 kort (eller hand size limit)
            var handSize = 5; // Standard hand size, kan ökas av artifacts
            var currentHandCount = await _actionCardService.GetPlayerCardsAsync(turn.CurrentPlayerId, CardLocation.Hand);
            
            var cardsToDraw = handSize - currentHandCount.Count;
            for (int i = 0; i < cardsToDraw; i++)
            {
                var drawnCard = await _actionCardService.DrawCardAsync(turn.CurrentPlayerId);
                if (drawnCard == null) break; // Inga fler kort att dra
            }

            // Reset mana och crystals
            turn.ManaAvailable = 0;
            turn.CrystalsAvailable = 0;

            // Reset action points
            turn.ActionPoints = 0;
            turn.MovementPoints = 0;
            turn.InfluencePoints = 0;
            turn.AttackPoints = 0;
            turn.BlockPoints = 0;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Started preparation phase for turn {TurnId}", turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start preparation phase for turn {TurnId}", turnId);
            return false;
        }
    }

    /// <summary>
    /// Startar main phase - spelaren kan spela kort och utföra actions
    /// </summary>
    public async Task<bool> StartMainPhaseAsync(int turnId)
    {
        try
        {
            var turn = await _context.GameTurns
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null) return false;

            turn.Phase = TurnPhase.Main;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started main phase for turn {TurnId}", turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start main phase for turn {TurnId}", turnId);
            return false;
        }
    }

    /// <summary>
    /// Spelar en action card och uppdaterar action points
    /// </summary>
    public async Task<bool> PlayActionCardAsync(int turnId, int cardId, bool sideways = false, bool useStrongEffect = false)
    {
        try
        {
            var turn = await _context.GameTurns
                .Include(t => t.Actions)
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null || turn.Phase != TurnPhase.Main) return false;

            var card = await _context.ActionCards
                .FirstOrDefaultAsync(c => c.Id == cardId && c.PlayerId == turn.CurrentPlayerId);

            if (card == null || card.Location != CardLocation.Hand) return false;

            // Spela kortet
            var success = await _actionCardService.PlayCardAsync(card, sideways, useStrongEffect);
            if (!success) return false;

            // Hämta card effects
            var effects = _actionCardService.GetCardEffects(card);
            
            // Uppdatera action points baserat på effects
            foreach (var effect in effects)
            {
                switch (effect.Effect.ToLower())
                {
                    case "move":
                        turn.MovementPoints += effect.Values.Move;
                        turn.MaxMovementPoints += effect.Values.Move;
                        break;
                    case "attack":
                        turn.AttackPoints += effect.Values.Attack;
                        turn.MaxAttackPoints += effect.Values.Attack;
                        break;
                    case "block":
                        turn.BlockPoints += effect.Values.Block;
                        turn.MaxBlockPoints += effect.Values.Block;
                        break;
                    case "influence":
                        turn.InfluencePoints += effect.Values.Influence;
                        turn.MaxInfluencePoints += effect.Values.Influence;
                        break;
                }
            }

            // Skapa turn action record
            var action = new TurnAction
            {
                GameTurnId = turnId,
                PlayerId = turn.CurrentPlayerId,
                Type = ActionType.CardPlayed,
                Description = $"Played {card.Name}" + (sideways ? " sideways" : "") + (useStrongEffect ? " (strong)" : ""),
                CardId = cardId,
                ActionSequence = turn.Actions.Count + 1,
                Timestamp = DateTime.UtcNow
            };

            _context.TurnActions.Add(action);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Played action card {CardName} in turn {TurnId}", card.Name, turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play action card {CardId} in turn {TurnId}", cardId, turnId);
            return false;
        }
    }

    /// <summary>
    /// Använder movement points för att röra sig
    /// </summary>
    public async Task<bool> UseMovementPointsAsync(int turnId, int points)
    {
        try
        {
            var turn = await _context.GameTurns
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null || turn.Phase != TurnPhase.Main) return false;

            if (turn.UsedMovementPoints + points > turn.MovementPoints)
            {
                _logger.LogWarning("Not enough movement points. Available: {Available}, Requested: {Requested}", 
                    turn.MovementPoints - turn.UsedMovementPoints, points);
                return false;
            }

            turn.UsedMovementPoints += points;

            // Skapa turn action record
            var action = new TurnAction
            {
                GameTurnId = turnId,
                PlayerId = turn.CurrentPlayerId,
                Type = ActionType.Movement,
                Description = $"Used {points} movement points",
                MovementPointsCost = points,
                ActionSequence = await GetNextActionSequenceAsync(turnId),
                Timestamp = DateTime.UtcNow
            };

            _context.TurnActions.Add(action);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Used {Points} movement points in turn {TurnId}", points, turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to use movement points in turn {TurnId}", turnId);
            return false;
        }
    }

    /// <summary>
    /// Använder attack points för att attackera
    /// </summary>
    public async Task<bool> UseAttackPointsAsync(int turnId, int points)
    {
        try
        {
            var turn = await _context.GameTurns
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null || turn.Phase != TurnPhase.Main) return false;

            if (turn.UsedAttackPoints + points > turn.AttackPoints)
            {
                _logger.LogWarning("Not enough attack points. Available: {Available}, Requested: {Requested}", 
                    turn.AttackPoints - turn.UsedAttackPoints, points);
                return false;
            }

            turn.UsedAttackPoints += points;

            // Skapa turn action record
            var action = new TurnAction
            {
                GameTurnId = turnId,
                PlayerId = turn.CurrentPlayerId,
                Type = ActionType.Attack,
                Description = $"Used {points} attack points",
                AttackPointsCost = points,
                ActionSequence = await GetNextActionSequenceAsync(turnId),
                Timestamp = DateTime.UtcNow
            };

            _context.TurnActions.Add(action);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Used {Points} attack points in turn {TurnId}", points, turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to use attack points in turn {TurnId}", turnId);
            return false;
        }
    }

    /// <summary>
    /// Spelaren passerar sin turn
    /// </summary>
    public async Task<bool> PassTurnAsync(int turnId)
    {
        try
        {
            var turn = await _context.GameTurns
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null || turn.Phase != TurnPhase.Main) return false;

            turn.HasPassed = true;
            turn.Phase = TurnPhase.EndOfTurn;

            // Skapa turn action record
            var action = new TurnAction
            {
                GameTurnId = turnId,
                PlayerId = turn.CurrentPlayerId,
                Type = ActionType.Pass,
                Description = "Passed turn",
                ActionSequence = await GetNextActionSequenceAsync(turnId),
                Timestamp = DateTime.UtcNow
            };

            _context.TurnActions.Add(action);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Player passed turn {TurnId}", turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pass turn {TurnId}", turnId);
            return false;
        }
    }

    /// <summary>
    /// Avslutar en turn - kastar kvarvarande kort och cleanup
    /// </summary>
    public async Task<bool> EndTurnAsync(int turnId)
    {
        try
        {
            var turn = await _context.GameTurns
                .Include(t => t.CurrentPlayer)
                .FirstOrDefaultAsync(t => t.Id == turnId);

            if (turn == null) return false;

            turn.Phase = TurnPhase.EndOfTurn;
            turn.EndedAt = DateTime.UtcNow;
            turn.IsActive = false;
            turn.IsCompleted = true;

            // Kasta alla kvarvarande kort i handen
            var handCards = await _actionCardService.GetPlayerCardsAsync(turn.CurrentPlayerId, CardLocation.Hand);
            foreach (var card in handCards)
            {
                await _actionCardService.DiscardCardAsync(card);
            }

            // Kasta alla played cards
            var playedCards = await _actionCardService.GetPlayerCardsAsync(turn.CurrentPlayerId, CardLocation.Played);
            foreach (var card in playedCards)
            {
                await _actionCardService.DiscardCardAsync(card);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Ended turn {TurnId}", turnId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end turn {TurnId}", turnId);
            return false;
        }
    }

    /// <summary>
    /// Hämtar aktuell turn för en spelare
    /// </summary>
    public async Task<GameTurn?> GetCurrentTurnAsync(int gameSessionId, int playerId)
    {
        return await _context.GameTurns
            .Include(t => t.CurrentPlayer)
            .Include(t => t.Actions)
            .Where(t => t.GameSessionId == gameSessionId && t.CurrentPlayerId == playerId && t.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Hämtar alla turns för en game session
    /// </summary>
    public async Task<List<GameTurn>> GetGameTurnsAsync(int gameSessionId)
    {
        return await _context.GameTurns
            .Include(t => t.CurrentPlayer)
            .Include(t => t.Actions)
            .Where(t => t.GameSessionId == gameSessionId)
            .OrderBy(t => t.TurnNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Hämtar nästa action sequence number
    /// </summary>
    private async Task<int> GetNextActionSequenceAsync(int turnId)
    {
        var maxSequence = await _context.TurnActions
            .Where(a => a.GameTurnId == turnId)
            .MaxAsync(a => (int?)a.ActionSequence) ?? 0;
        
        return maxSequence + 1;
    }

    /// <summary>
    /// Hämtar tillgängliga action points för en turn
    /// </summary>
    public async Task<ActionPointsSummary> GetActionPointsSummaryAsync(int turnId)
    {
        var turn = await _context.GameTurns
            .FirstOrDefaultAsync(t => t.Id == turnId);

        if (turn == null)
        {
            return new ActionPointsSummary();
        }

        return new ActionPointsSummary
        {
            MovementPoints = turn.MovementPoints - turn.UsedMovementPoints,
            AttackPoints = turn.AttackPoints - turn.UsedAttackPoints,
            BlockPoints = turn.BlockPoints - turn.UsedBlockPoints,
            InfluencePoints = turn.InfluencePoints - turn.UsedInfluencePoints,
            ManaAvailable = turn.ManaAvailable,
            CrystalsAvailable = turn.CrystalsAvailable
        };
    }
}

/// <summary>
/// Summary av tillgängliga action points
/// </summary>
public class ActionPointsSummary
{
    public int MovementPoints { get; set; }
    public int AttackPoints { get; set; }
    public int BlockPoints { get; set; }
    public int InfluencePoints { get; set; }
    public int ManaAvailable { get; set; }
    public int CrystalsAvailable { get; set; }
}