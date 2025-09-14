using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class TurnManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TurnManagementService> _logger;

    public TurnManagementService(ApplicationDbContext context, ILogger<TurnManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GameTurn> StartNewTurnAsync(int gameSessionId, int playerId)
    {
        var gameSession = await _context.GameSessions
            .Include(gs => gs.Players)
            .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

        if (gameSession == null)
            throw new ArgumentException("Game session not found");

        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId && p.GameSessionId == gameSessionId);

        if (player == null)
            throw new ArgumentException("Player not found");

        // Get the next turn number
        var lastTurn = await _context.GameTurns
            .Where(t => t.GameSessionId == gameSessionId)
            .OrderByDescending(t => t.TurnNumber)
            .FirstOrDefaultAsync();

        var turnNumber = lastTurn?.TurnNumber + 1 ?? 1;

        // Create new turn
        var turn = new GameTurn
        {
            GameSessionId = gameSessionId,
            TurnNumber = turnNumber,
            CurrentPlayerId = playerId,
            Phase = TurnPhase.Preparation,
            ActionPoints = 0,
            MaxActionPoints = 0,
            MovementPoints = 0,
            MaxMovementPoints = 0,
            InfluencePoints = 0,
            MaxInfluencePoints = 0,
            AttackPoints = 0,
            MaxAttackPoints = 0,
            BlockPoints = 0,
            MaxBlockPoints = 0,
            ManaAvailable = player.Mana,
            CrystalsAvailable = player.Crystals,
            IsActive = true
        };

        _context.GameTurns.Add(turn);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Started new turn {turnNumber} for player {playerId} in game session {gameSessionId}");

        return turn;
    }

    public async Task<bool> ExecuteActionAsync(int turnId, ActionType actionType, string description, 
        int actionPointsCost = 0, int movementPointsCost = 0, int influencePointsCost = 0, 
        int attackPointsCost = 0, int blockPointsCost = 0, int? cardId = null, string? data = null)
    {
        var turn = await _context.GameTurns
            .Include(t => t.Actions)
            .FirstOrDefaultAsync(t => t.Id == turnId);

        if (turn == null || !turn.IsActive)
            return false;

        // Check if player has enough points
        if (actionPointsCost > turn.ActionPoints - turn.UsedActionPoints ||
            movementPointsCost > turn.MovementPoints - turn.UsedMovementPoints ||
            influencePointsCost > turn.InfluencePoints - turn.UsedInfluencePoints ||
            attackPointsCost > turn.AttackPoints - turn.UsedAttackPoints ||
            blockPointsCost > turn.BlockPoints - turn.UsedBlockPoints)
        {
            _logger.LogWarning($"Insufficient points for action {actionType} in turn {turnId}");
            return false;
        }

        // Create action
        var action = new TurnAction
        {
            GameTurnId = turnId,
            PlayerId = turn.CurrentPlayerId,
            Type = actionType,
            Description = description,
            ActionPointsCost = actionPointsCost,
            MovementPointsCost = movementPointsCost,
            InfluencePointsCost = influencePointsCost,
            AttackPointsCost = attackPointsCost,
            BlockPointsCost = blockPointsCost,
            CardId = cardId,
            Data = data,
            ActionSequence = turn.Actions.Count + 1,
            Timestamp = DateTime.UtcNow
        };

        // Deduct points
        turn.UsedActionPoints += actionPointsCost;
        turn.UsedMovementPoints += movementPointsCost;
        turn.UsedInfluencePoints += influencePointsCost;
        turn.UsedAttackPoints += attackPointsCost;
        turn.UsedBlockPoints += blockPointsCost;

        _context.TurnActions.Add(action);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Executed action {actionType} in turn {turnId}");

        return true;
    }

    public async Task<bool> EndTurnAsync(int turnId)
    {
        var turn = await _context.GameTurns
            .Include(t => t.Actions)
            .FirstOrDefaultAsync(t => t.Id == turnId);

        if (turn == null || !turn.IsActive)
            return false;

        // Mark turn as completed
        turn.IsActive = false;
        turn.IsCompleted = true;
        turn.EndedAt = DateTime.UtcNow;

        // Create end turn action
        var endAction = new TurnAction
        {
            GameTurnId = turnId,
            PlayerId = turn.CurrentPlayerId,
            Type = ActionType.TurnEnded,
            Description = "End of turn",
            ActionSequence = turn.Actions.Count + 1,
            Timestamp = DateTime.UtcNow,
            IsUndoable = false
        };

        _context.TurnActions.Add(endAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Ended turn {turnId}");

        return true;
    }

    public async Task<bool> PassTurnAsync(int turnId)
    {
        var turn = await _context.GameTurns
            .FirstOrDefaultAsync(t => t.Id == turnId);

        if (turn == null || !turn.IsActive)
            return false;

        turn.HasPassed = true;
        turn.IsActive = false;

        // Create pass action
        var passAction = new TurnAction
        {
            GameTurnId = turnId,
            PlayerId = turn.CurrentPlayerId,
            Type = ActionType.Pass,
            Description = "Player passed",
            ActionSequence = turn.Actions.Count + 1,
            Timestamp = DateTime.UtcNow,
            IsUndoable = false
        };

        _context.TurnActions.Add(passAction);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Player passed in turn {turnId}");

        return true;
    }

    public async Task<GameTurn?> GetCurrentTurnAsync(int gameSessionId)
    {
        return await _context.GameTurns
            .Include(t => t.Actions)
            .Where(t => t.GameSessionId == gameSessionId && t.IsActive)
            .OrderByDescending(t => t.TurnNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GameTurn>> GetTurnHistoryAsync(int gameSessionId, int limit = 10)
    {
        return await _context.GameTurns
            .Include(t => t.Actions)
            .Where(t => t.GameSessionId == gameSessionId)
            .OrderByDescending(t => t.TurnNumber)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> CanExecuteActionAsync(int turnId, ActionType actionType, int actionPointsCost = 0, 
        int movementPointsCost = 0, int influencePointsCost = 0, int attackPointsCost = 0, int blockPointsCost = 0)
    {
        var turn = await _context.GameTurns
            .FirstOrDefaultAsync(t => t.Id == turnId);

        if (turn == null || !turn.IsActive)
            return false;

        return actionPointsCost <= turn.ActionPoints - turn.UsedActionPoints &&
               movementPointsCost <= turn.MovementPoints - turn.UsedMovementPoints &&
               influencePointsCost <= turn.InfluencePoints - turn.UsedInfluencePoints &&
               attackPointsCost <= turn.AttackPoints - turn.UsedAttackPoints &&
               blockPointsCost <= turn.BlockPoints - turn.UsedBlockPoints;
    }

    public async Task<bool> AdvancePhaseAsync(int turnId)
    {
        var turn = await _context.GameTurns
            .FirstOrDefaultAsync(t => t.Id == turnId);

        if (turn == null || !turn.IsActive)
            return false;

        switch (turn.Phase)
        {
            case TurnPhase.Preparation:
                turn.Phase = TurnPhase.Main;
                break;
            case TurnPhase.Main:
                turn.Phase = TurnPhase.Combat;
                break;
            case TurnPhase.Combat:
                turn.Phase = TurnPhase.EndOfTurn;
                break;
            case TurnPhase.EndOfTurn:
                turn.Phase = TurnPhase.Completed;
                turn.IsCompleted = true;
                turn.IsActive = false;
                break;
            default:
                return false;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Advanced turn {turnId} to phase {turn.Phase}");

        return true;
    }
}
