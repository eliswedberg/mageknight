using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class GameService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GameService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GameService(ApplicationDbContext context, ILogger<GameService> logger, IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // Game Session Management
    public async Task<GameSession?> CreateGameSessionAsync(string hostUserId, string name, string? description = null, int maxPlayers = 4)
    {
        try
        {
            _logger.LogInformation("CreateGameSessionAsync started for user {UserId} with name {Name}", hostUserId, name);
            var gameSession = new GameSession
            {
                Name = name,
                Description = description,
                MaxPlayers = maxPlayers,
                HostUserId = hostUserId,
                Status = GameStatus.WaitingForPlayers
            };

            _context.GameSessions.Add(gameSession);
            await _context.SaveChangesAsync();

            // Add host as first player
            await JoinGameSessionAsync(gameSession.Id, hostUserId);

            _logger.LogInformation("CreateGameSessionAsync succeeded for user {UserId}, session {SessionId}", hostUserId, gameSession.Id);
            return gameSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game session for user {UserId}: {Message}", hostUserId, ex.Message);
            return null;
        }
    }

    public async Task<GamePlayer?> JoinGameSessionAsync(int gameSessionId, string userId)
    {
        try
        {
            var gameSession = await _context.GameSessions
                .Include(gs => gs.Players)
                .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

            if (gameSession == null)
            {
                _logger.LogWarning("Game session {GameSessionId} not found", gameSessionId);
                return null;
            }

            if (gameSession.Status != GameStatus.WaitingForPlayers)
            {
                _logger.LogWarning("Cannot join game session {GameSessionId} - status is {Status}", gameSessionId, gameSession.Status);
                return null;
            }

            if (gameSession.Players.Count >= gameSession.MaxPlayers)
            {
                _logger.LogWarning("Game session {GameSessionId} is full", gameSessionId);
                return null;
            }

            // Check if user is already in the game
            var existingPlayer = gameSession.Players.FirstOrDefault(p => p.UserId == userId);
            if (existingPlayer != null)
            {
                _logger.LogWarning("User {UserId} is already in game session {GameSessionId}", userId, gameSessionId);
                return existingPlayer;
            }

            var playerNumber = gameSession.Players.Count + 1;
            var gamePlayer = new GamePlayer
            {
                GameSessionId = gameSessionId,
                UserId = userId,
                PlayerNumber = playerNumber,
                Status = PlayerStatus.Joined
            };

            _context.GamePlayers.Add(gamePlayer);
            gameSession.CurrentPlayers = gameSession.Players.Count;
            
            await _context.SaveChangesAsync();

            // Log the action
            await LogGameActionAsync(gameSessionId, userId, ActionType.PlayerJoined, $"Player {playerNumber} joined the game");

            return gamePlayer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game session {GameSessionId} for user {UserId}", gameSessionId, userId);
            return null;
        }
    }

    public async Task<bool> LeaveGameSessionAsync(int gameSessionId, string userId)
    {
        try
        {
            var gamePlayer = await _context.GamePlayers
                .FirstOrDefaultAsync(gp => gp.GameSessionId == gameSessionId && gp.UserId == userId);

            if (gamePlayer == null)
            {
                _logger.LogWarning("Player {UserId} not found in game session {GameSessionId}", userId, gameSessionId);
                return false;
            }

            gamePlayer.Status = PlayerStatus.Left;
            gamePlayer.LeftAt = DateTime.UtcNow;

            var gameSession = await _context.GameSessions.FindAsync(gameSessionId);
            if (gameSession != null)
            {
                gameSession.CurrentPlayers--;
            }

            await _context.SaveChangesAsync();

            // Log the action
            await LogGameActionAsync(gameSessionId, userId, ActionType.PlayerLeft, $"Player left the game");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving game session {GameSessionId} for user {UserId}", gameSessionId, userId);
            return false;
        }
    }

    public async Task<List<GameSession>> GetAvailableGameSessionsAsync()
    {
        try
        {
            return await _context.GameSessions
                .Include(gs => gs.HostUser)
                .Include(gs => gs.Players)
                .Where(gs => gs.Status == GameStatus.WaitingForPlayers && gs.CurrentPlayers < gs.MaxPlayers)
                .OrderByDescending(gs => gs.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available game sessions");
            return new List<GameSession>();
        }
    }

    public async Task<List<GameSession>> GetUserRelatedGameSessionsAsync(string userId)
    {
        try
        {
            // Get games where user is host or player, and show different statuses
            return await _context.GameSessions
                .Include(gs => gs.HostUser)
                .Include(gs => gs.Players)
                .Where(gs => 
                    // Show games user is involved in
                    (gs.HostUserId == userId || gs.Players.Any(p => p.UserId == userId && p.Status != PlayerStatus.Left)) ||
                    // Show available games user can join
                    (gs.Status == GameStatus.WaitingForPlayers && gs.CurrentPlayers < gs.MaxPlayers && !gs.Players.Any(p => p.UserId == userId))
                )
                .OrderByDescending(gs => gs.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user related game sessions");
            return new List<GameSession>();
        }
    }

    public async Task<GameSession?> GetGameSessionAsync(int gameSessionId)
    {
        try
        {
            return await _context.GameSessions
                .Include(gs => gs.HostUser)
                .Include(gs => gs.Players)
                    .ThenInclude(gp => gp.User)
                .Include(gs => gs.Actions)
                    .ThenInclude(ga => ga.Player)
                .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game session {GameSessionId}", gameSessionId);
            return null;
        }
    }

    public async Task<List<GameSession>> GetUserGameSessionsAsync(string userId)
    {
        try
        {
            return await _context.GameSessions
                .Include(gs => gs.HostUser)
                .Include(gs => gs.Players)
                .Where(gs => gs.HostUserId == userId || gs.Players.Any(p => p.UserId == userId))
                .OrderByDescending(gs => gs.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user game sessions for {UserId}", userId);
            return new List<GameSession>();
        }
    }

    // Debug method to see all games in database
    public async Task<List<GameSession>> GetAllGameSessionsAsync()
    {
        try
        {
            return await _context.GameSessions
                .Include(gs => gs.HostUser)
                .Include(gs => gs.Players)
                .OrderByDescending(gs => gs.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all game sessions");
            return new List<GameSession>();
        }
    }

    // Fix player count for existing games
    public async Task FixPlayerCountsAsync()
    {
        try
        {
            var games = await _context.GameSessions
                .Include(gs => gs.Players)
                .ToListAsync();

            foreach (var game in games)
            {
                var actualPlayerCount = game.Players.Count(p => p.Status != PlayerStatus.Left);
                if (game.CurrentPlayers != actualPlayerCount)
                {
                    _logger.LogInformation("Fixing player count for game {GameId}: {Current} -> {Actual}", 
                        game.Id, game.CurrentPlayers, actualPlayerCount);
                    game.CurrentPlayers = actualPlayerCount;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Player counts fixed for all games");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing player counts");
        }
    }

    public async Task<bool> StartGameAsync(int gameSessionId, string userId)
    {
        try
        {
            _logger.LogInformation("StartGameAsync called for game {GameSessionId} by user {UserId}", gameSessionId, userId);
            
            var gameSession = await _context.GameSessions
                .Include(gs => gs.Players)
                .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

            if (gameSession == null || gameSession.HostUserId != userId)
            {
                _logger.LogWarning("User {UserId} cannot start game session {GameSessionId}", userId, gameSessionId);
                return false;
            }

            if (gameSession.Players.Count < 2)
            {
                _logger.LogWarning("Cannot start game session {GameSessionId} - not enough players", gameSessionId);
                return false;
            }

            gameSession.Status = GameStatus.InProgress;
            gameSession.StartedAt = DateTime.UtcNow;

            // Set first player as current player
            var firstPlayer = gameSession.Players.OrderBy(p => p.PlayerNumber).First();
            firstPlayer.IsCurrentPlayer = true;
            firstPlayer.Status = PlayerStatus.Playing;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Game session {GameSessionId} status updated to InProgress", gameSessionId);

            // Initialize the game (create board, tiles, etc.)
            _logger.LogInformation("Initializing game {GameSessionId}", gameSessionId);
            bool initResult = false;
            try
            {
                var mageKnightService = _serviceProvider.GetRequiredService<MageKnightGameService>();
                _logger.LogInformation("MageKnightGameService resolved successfully");
                initResult = await mageKnightService.InitializeGameAsync(gameSessionId);
                _logger.LogInformation("Game initialization result: {Result}", initResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game initialization for {GameSessionId}", gameSessionId);
                return false;
            }
            
            if (!initResult)
            {
                _logger.LogError("Failed to initialize game {GameSessionId}", gameSessionId);
                return false;
            }

            // Log the action
            await LogGameActionAsync(gameSessionId, userId, ActionType.GameStarted, "Game started");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game session {GameSessionId}", gameSessionId);
            return false;
        }
    }

    // Game Actions
    public async Task LogGameActionAsync(int gameSessionId, string? userId, ActionType actionType, string description, string? data = null)
    {
        try
        {
            var gameAction = new GameAction
            {
                GameSessionId = gameSessionId,
                PlayerId = userId != null ? (await _context.GamePlayers
                    .FirstOrDefaultAsync(gp => gp.GameSessionId == gameSessionId && gp.UserId == userId))?.Id : null,
                Type = actionType,
                Description = description,
                Data = data
            };

            _context.GameActions.Add(gameAction);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging game action for session {GameSessionId}", gameSessionId);
        }
    }

    public async Task<List<GameAction>> GetGameActionsAsync(int gameSessionId, int? limit = null)
    {
        try
        {
            var query = _context.GameActions
                .Include(ga => ga.Player)
                .Where(ga => ga.GameSessionId == gameSessionId)
                .OrderByDescending(ga => ga.Timestamp)
                .AsQueryable();

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game actions for session {GameSessionId}", gameSessionId);
            return new List<GameAction>();
        }
    }

    public async Task<bool> PlayCardAsync(int playerId, int cardId)
    {
        try
        {
            // This is a placeholder implementation
            // In a real implementation, this would handle card playing logic
            await LogGameActionAsync(0, null, ActionType.CardPlayed, "Card played");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing card {CardId} for player {PlayerId}", cardId, playerId);
            return false;
        }
    }
}
