using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MageKnightOnline.Services;

public class MovementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MovementService> _logger;
    private readonly MapTileService _mapTileService;

    public MovementService(ApplicationDbContext context, ILogger<MovementService> logger, MapTileService mapTileService)
    {
        _context = context;
        _logger = logger;
        _mapTileService = mapTileService;
    }

    public async Task<bool> MovePlayerAsync(int gameSessionId, int playerId, int fromQ, int fromR, int toQ, int toR, int movementPointsUsed)
    {
        try
        {
            var player = await _context.GamePlayers
                .FirstOrDefaultAsync(p => p.Id == playerId && p.GameSessionId == gameSessionId);

            if (player == null)
            {
                _logger.LogWarning("Player {PlayerId} not found in game session {GameSessionId}", playerId, gameSessionId);
                return false;
            }

            // Validate movement
            var isValidMove = await ValidateMovementAsync(gameSessionId, fromQ, fromR, toQ, toR, movementPointsUsed);
            if (!isValidMove)
            {
                _logger.LogWarning("Invalid movement from ({FromQ}, {FromR}) to ({ToQ}, {ToR})", fromQ, fromR, toQ, toR);
                return false;
            }

            // Get current turn to check movement points
            var currentTurn = await _context.GameTurns
                .Where(t => t.GameSessionId == gameSessionId && t.CurrentPlayerId == playerId && t.IsActive)
                .OrderByDescending(t => t.TurnNumber)
                .FirstOrDefaultAsync();

            if (currentTurn == null)
            {
                _logger.LogWarning("No active turn found for player {PlayerId}", playerId);
                return false;
            }

            // Check if player has enough movement points
            if (currentTurn.UsedMovementPoints + movementPointsUsed > currentTurn.MovementPoints)
            {
                _logger.LogWarning("Player {PlayerId} does not have enough movement points. Required: {Required}, Available: {Available}", 
                    playerId, movementPointsUsed, currentTurn.MovementPoints - currentTurn.UsedMovementPoints);
                return false;
            }

            // Update player position
            var playerPosition = await _context.PlayerPositions
                .FirstOrDefaultAsync(pp => pp.PlayerId == playerId);

            if (playerPosition == null)
            {
                // Create new position record
                var gameBoard = await _context.GameBoards
                    .FirstOrDefaultAsync(gb => gb.GameSessionId == gameSessionId);

                if (gameBoard == null)
                {
                    _logger.LogError("No game board found for game session {GameSessionId}", gameSessionId);
                    return false;
                }

                playerPosition = new PlayerPosition
                {
                    GameBoardId = gameBoard.Id,
                    PlayerId = playerId,
                    X = toQ,
                    Y = toR,
                    MovedAt = DateTime.UtcNow,
                    MovementPointsUsed = movementPointsUsed
                };

                _context.PlayerPositions.Add(playerPosition);
            }
            else
            {
                playerPosition.X = toQ;
                playerPosition.Y = toR;
                playerPosition.MovedAt = DateTime.UtcNow;
                playerPosition.MovementPointsUsed += movementPointsUsed;

                _context.PlayerPositions.Update(playerPosition);
            }

            // Update turn movement points
            currentTurn.UsedMovementPoints += movementPointsUsed;
            _context.GameTurns.Update(currentTurn);

            // Log movement action
            await LogMovementActionAsync(currentTurn.Id, playerId, fromQ, fromR, toQ, toR, movementPointsUsed);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Player {PlayerId} moved from ({FromQ}, {FromR}) to ({ToQ}, {ToR}) using {MovementPoints} movement points", 
                playerId, fromQ, fromR, toQ, toR, movementPointsUsed);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving player {PlayerId} from ({FromQ}, {FromR}) to ({ToQ}, {ToR})", playerId, fromQ, fromR, toQ, toR);
            return false;
        }
    }

    public async Task<bool> ValidateMovementAsync(int gameSessionId, int fromQ, int fromR, int toQ, int toR, int movementPointsUsed)
    {
        try
        {
            // Check if destination is adjacent to source (hex-based movement)
            if (!IsAdjacentHex(fromQ, fromR, toQ, toR))
            {
                _logger.LogWarning("Destination ({ToQ}, {ToR}) is not adjacent to source ({FromQ}, {FromR})", toQ, toR, fromQ, fromR);
                return false;
            }

            // Check if destination hex exists and is accessible
            var destinationHex = await _context.HexSpaces
                .FirstOrDefaultAsync(hs => hs.GameSessionId == gameSessionId && hs.Q == toQ && hs.R == toR);

            if (destinationHex == null)
            {
                _logger.LogWarning("Destination hex ({ToQ}, {ToR}) does not exist", toQ, toR);
                return false;
            }

            if (!destinationHex.IsAccessible)
            {
                _logger.LogWarning("Destination hex ({ToQ}, {ToR}) is not accessible", toQ, toR);
                return false;
            }

            // Check if destination is occupied by another player
            var occupyingPlayer = await _context.PlayerPositions
                .Include(pp => pp.Player)
                .FirstOrDefaultAsync(pp => pp.X == toQ && pp.Y == toR && pp.Player.GameSessionId == gameSessionId);

            if (occupyingPlayer != null)
            {
                _logger.LogWarning("Destination hex ({ToQ}, {ToR}) is occupied by player {OccupyingPlayerId}", toQ, toR, occupyingPlayer.PlayerId);
                return false;
            }

            // Validate movement cost
            var movementCost = await CalculateMovementCostAsync(gameSessionId, fromQ, fromR, toQ, toR);
            if (movementCost != movementPointsUsed)
            {
                _logger.LogWarning("Movement cost mismatch. Expected: {Expected}, Provided: {Provided}", movementCost, movementPointsUsed);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating movement from ({FromQ}, {FromR}) to ({ToQ}, {ToR})", fromQ, fromR, toQ, toR);
            return false;
        }
    }

    public async Task<int> CalculateMovementCostAsync(int gameSessionId, int fromQ, int fromR, int toQ, int toR)
    {
        try
        {
            // Get destination hex
            var destinationHex = await _context.HexSpaces
                .FirstOrDefaultAsync(hs => hs.GameSessionId == gameSessionId && hs.Q == toQ && hs.R == toR);

            if (destinationHex == null)
            {
                return int.MaxValue; // Invalid destination
            }

            // Base movement cost from terrain type
            var baseCost = GetTerrainMovementCost(destinationHex.TerrainType);

            // Check for day/night modifiers
            var gameState = await _context.GameStates
                .FirstOrDefaultAsync(gs => gs.GameSessionId == gameSessionId);

            if (gameState != null && gameState.IsNightPhase)
            {
                baseCost = ApplyNightModifiers(baseCost, destinationHex.TerrainType);
            }

            // Apply any special modifiers from the hex
            baseCost += destinationHex.MovementCost;

            return Math.Max(1, baseCost); // Minimum cost is 1
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating movement cost from ({FromQ}, {FromR}) to ({ToQ}, {ToR})", fromQ, fromR, toQ, toR);
            return int.MaxValue;
        }
    }

    public async Task<List<(int Q, int R, int Cost)>> GetValidMovesAsync(int gameSessionId, int playerId, int currentQ, int currentR, int availableMovementPoints)
    {
        try
        {
            var validMoves = new List<(int Q, int R, int Cost)>();

            // Get all adjacent hexes
            var adjacentHexes = GetAdjacentHexes(currentQ, currentR);

            foreach (var (q, r) in adjacentHexes)
            {
                var cost = await CalculateMovementCostAsync(gameSessionId, currentQ, currentR, q, r);
                
                if (cost <= availableMovementPoints)
                {
                    // Check if hex is accessible and not occupied
                    var isValid = await ValidateMovementAsync(gameSessionId, currentQ, currentR, q, r, cost);
                    if (isValid)
                    {
                        validMoves.Add((q, r, cost));
                    }
                }
            }

            return validMoves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid moves for player {PlayerId} at ({CurrentQ}, {CurrentR})", playerId, currentQ, currentR);
            return new List<(int Q, int R, int Cost)>();
        }
    }

    public async Task<bool> ExploreTileAsync(int gameSessionId, int playerId, int q, int r)
    {
        try
        {
            // Check if player is adjacent to an unexplored area
            var playerPosition = await _context.PlayerPositions
                .FirstOrDefaultAsync(pp => pp.PlayerId == playerId);

            if (playerPosition == null)
            {
                _logger.LogWarning("Player {PlayerId} has no position", playerId);
                return false;
            }

            // Check if exploration position is adjacent to player
            if (!IsAdjacentHex(playerPosition.X, playerPosition.Y, q, r))
            {
                _logger.LogWarning("Exploration position ({Q}, {R}) is not adjacent to player at ({PlayerX}, {PlayerY})", 
                    q, r, playerPosition.X, playerPosition.Y);
                return false;
            }

            // Check if player has enough movement points for exploration (costs 2 movement points)
            var currentTurn = await _context.GameTurns
                .Where(t => t.GameSessionId == gameSessionId && t.CurrentPlayerId == playerId && t.IsActive)
                .OrderByDescending(t => t.TurnNumber)
                .FirstOrDefaultAsync();

            if (currentTurn == null)
            {
                _logger.LogWarning("No active turn found for player {PlayerId}", playerId);
                return false;
            }

            if (currentTurn.UsedMovementPoints + 2 > currentTurn.MovementPoints)
            {
                _logger.LogWarning("Player {PlayerId} does not have enough movement points for exploration", playerId);
                return false;
            }

            // Use MapTileService to explore the tile
            var explorationResult = await _mapTileService.ExploreTileAsync(gameSessionId, playerId, q, r);
            if (explorationResult == null)
            {
                _logger.LogWarning("Failed to explore tile at ({Q}, {R})", q, r);
                return false;
            }

            // Deduct movement points
            currentTurn.UsedMovementPoints += 2;
            _context.GameTurns.Update(currentTurn);

            // Log exploration action
            await LogMovementActionAsync(currentTurn.Id, playerId, playerPosition.X, playerPosition.Y, q, r, 2, "Exploration");

            await _context.SaveChangesAsync();

            _logger.LogInformation("Player {PlayerId} explored tile at ({Q}, {R})", playerId, q, r);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exploring tile at ({Q}, {R}) for player {PlayerId}", q, r, playerId);
            return false;
        }
    }

    private bool IsAdjacentHex(int fromQ, int fromR, int toQ, int toR)
    {
        // Hex adjacency check using axial coordinates
        var dq = toQ - fromQ;
        var dr = toR - fromR;
        
        // In axial coordinates, adjacent hexes have one of these patterns:
        // (0, ±1), (±1, 0), (±1, ∓1)
        return (dq == 0 && Math.Abs(dr) == 1) ||
               (Math.Abs(dq) == 1 && dr == 0) ||
               (Math.Abs(dq) == 1 && dq == -dr);
    }

    private List<(int Q, int R)> GetAdjacentHexes(int q, int r)
    {
        return new List<(int Q, int R)>
        {
            (q, r + 1),     // North
            (q + 1, r),     // Northeast
            (q + 1, r - 1), // Southeast
            (q, r - 1),     // South
            (q - 1, r),     // Southwest
            (q - 1, r + 1)  // Northwest
        };
    }

    private int GetTerrainMovementCost(TerrainType terrainType)
    {
        return terrainType switch
        {
            TerrainType.Grassland => 1,
            TerrainType.Forest => 2,
            TerrainType.Desert => 1,
            TerrainType.Lake => 3,
            TerrainType.Mountain => 3,
            TerrainType.Ruins => 1,
            TerrainType.Village => 1,
            TerrainType.Castle => 1,
            TerrainType.Mine => 2,
            TerrainType.Barren => 1,
            _ => 1
        };
    }

    private int ApplyNightModifiers(int baseCost, TerrainType terrainType)
    {
        // Some terrains become more expensive at night
        return terrainType switch
        {
            TerrainType.Forest => baseCost + 1, // Forests are harder to navigate at night
            TerrainType.Mountain => baseCost + 1, // Mountains are more dangerous at night
            TerrainType.Mine => baseCost + 1, // Mines are more treacherous at night
            _ => baseCost
        };
    }

    private async Task LogMovementActionAsync(int turnId, int playerId, int fromQ, int fromR, int toQ, int toR, int movementPoints, string actionType = "Movement")
    {
        try
        {
            var action = new TurnAction
            {
                GameTurnId = turnId,
                PlayerId = playerId,
                Type = ActionType.Movement,
                Description = $"{actionType} from ({fromQ}, {fromR}) to ({toQ}, {toR}) using {movementPoints} movement points",
                MovementPointsCost = movementPoints,
                Timestamp = DateTime.UtcNow,
                ActionSequence = await GetNextActionSequenceAsync(turnId),
                IsResolved = true,
                Result = "Success"
            };

            _context.TurnActions.Add(action);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging movement action");
        }
    }

    private async Task<int> GetNextActionSequenceAsync(int turnId)
    {
        var lastAction = await _context.TurnActions
            .Where(ta => ta.GameTurnId == turnId)
            .OrderByDescending(ta => ta.ActionSequence)
            .FirstOrDefaultAsync();

        return lastAction?.ActionSequence + 1 ?? 1;
    }

    public async Task<PlayerPosition?> GetPlayerPositionAsync(int playerId)
    {
        return await _context.PlayerPositions
            .FirstOrDefaultAsync(pp => pp.PlayerId == playerId);
    }

    public async Task<List<PlayerPosition>> GetAllPlayerPositionsAsync(int gameSessionId)
    {
        return await _context.PlayerPositions
            .Include(pp => pp.Player)
            .Where(pp => pp.Player.GameSessionId == gameSessionId)
            .ToListAsync();
    }
}
