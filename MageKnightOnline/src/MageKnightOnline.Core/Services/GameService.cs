using System.Text.Json;
using MageKnightOnline.Core.Entities;
using MageKnightOnline.Core.GameEngine;
using MageKnightOnline.Core.GameState;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Core.Services;

public class GameService : IGameService
{
    private readonly DbContext _dbContext;
    private readonly IGameDefinitionService _definitionService;
    private readonly GameStateInitializer _stateInitializer;

    public GameService(DbContext dbContext, IGameDefinitionService definitionService)
    {
        _dbContext = dbContext;
        _definitionService = definitionService;
        _stateInitializer = new GameStateInitializer(definitionService);
    }

    public async Task<IReadOnlyList<Game>> GetAvailableGamesAsync()
    {
        return await _dbContext.Set<Game>()
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .Include(g => g.CreatedBy)
            .Where(g => g.Status == GameStatus.WaitingForPlayers)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Game>> GetMyGamesAsync(Guid userId)
    {
        return await _dbContext.Set<Game>()
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .Include(g => g.CreatedBy)
            .Where(g => g.CreatedByUserId == userId || g.Players.Any(p => p.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Game?> GetGameAsync(Guid gameId)
    {
        return await _dbContext.Set<Game>()
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .Include(g => g.CreatedBy)
            .FirstOrDefaultAsync(g => g.Id == gameId);
    }

    public async Task<GameResult> CreateGameAsync(Guid userId, CreateGameRequest request)
    {
        // Validate scenario
        var scenario = await _definitionService.GetScenarioAsync(request.ScenarioId);
        if (scenario == null)
            return GameResult.Fail("Invalid scenario selected.");

        // Validate player count
        if (request.MaxPlayers < scenario.MinPlayers || request.MaxPlayers > scenario.MaxPlayers)
            return GameResult.Fail($"Player count must be between {scenario.MinPlayers} and {scenario.MaxPlayers} for this scenario.");

        // Create game
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{scenario.Name} Game" : request.Name,
            ScenarioId = request.ScenarioId,
            MinPlayers = scenario.MinPlayers,
            MaxPlayers = request.MaxPlayers,
            Status = GameStatus.WaitingForPlayers,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Set<Game>().Add(game);

        // Add creator as first player
        var player = new GamePlayer
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            UserId = userId,
            TurnOrder = 1,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.Set<GamePlayer>().Add(player);
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(await GetGameAsync(game.Id) ?? game);
    }

    public async Task<GameResult> JoinGameAsync(Guid gameId, Guid userId)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.Status != GameStatus.WaitingForPlayers)
            return GameResult.Fail("This game is no longer accepting players.");

        if (game.Players.Count >= game.MaxPlayers)
            return GameResult.Fail("This game is full.");

        if (game.Players.Any(p => p.UserId == userId))
            return GameResult.Fail("You have already joined this game.");

        var player = new GamePlayer
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserId = userId,
            TurnOrder = game.Players.Count + 1,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.Set<GamePlayer>().Add(player);
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(await GetGameAsync(gameId) ?? game);
    }

    public async Task<GameResult> LeaveGameAsync(Guid gameId, Guid userId)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.Status != GameStatus.WaitingForPlayers)
            return GameResult.Fail("Cannot leave a game that has already started.");

        if (game.CreatedByUserId == userId)
            return GameResult.Fail("The game creator cannot leave. Cancel the game instead.");

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null)
            return GameResult.Fail("You are not in this game.");

        _dbContext.Set<GamePlayer>().Remove(player);
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(await GetGameAsync(gameId) ?? game);
    }

    public async Task<GameResult> StartGameAsync(Guid gameId, Guid userId)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.CreatedByUserId != userId)
            return GameResult.Fail("Only the game creator can start the game.");

        if (game.Status != GameStatus.WaitingForPlayers)
            return GameResult.Fail("This game has already started or been cancelled.");

        if (game.Players.Count < game.MinPlayers)
            return GameResult.Fail($"Need at least {game.MinPlayers} players to start.");

        // Check all players have selected heroes
        if (game.Players.Any(p => string.IsNullOrEmpty(p.HeroId)))
            return GameResult.Fail("All players must select a hero before starting.");

        // Check for duplicate heroes
        var heroIds = game.Players.Select(p => p.HeroId).ToList();
        if (heroIds.Count != heroIds.Distinct().Count())
            return GameResult.Fail("Each player must select a different hero.");

        game.Status = GameStatus.InProgress;
        game.StartedAt = DateTime.UtcNow;
        
        // Initialize game state
        var scenario = await _definitionService.GetScenarioAsync(game.ScenarioId);
        if (scenario != null)
        {
            var gameState = await _stateInitializer.InitializeAsync(game, scenario);
            game.GameState = JsonSerializer.Serialize(gameState);
        }
        
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(game);
    }

    public async Task<GameResult> CancelGameAsync(Guid gameId, Guid userId)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.CreatedByUserId != userId)
            return GameResult.Fail("Only the game creator can cancel the game.");

        if (game.Status == GameStatus.Completed)
            return GameResult.Fail("Cannot cancel a completed game.");

        game.Status = GameStatus.Cancelled;
        game.EndedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(game);
    }

    public async Task<GameResult> SelectHeroAsync(Guid gameId, Guid userId, string heroId)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.Status != GameStatus.WaitingForPlayers)
            return GameResult.Fail("Cannot change hero after the game has started.");

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null)
            return GameResult.Fail("You are not in this game.");

        // Validate hero
        var hero = await _definitionService.GetHeroAsync(heroId);
        if (hero == null)
            return GameResult.Fail("Invalid hero selected.");

        // Check if hero is already taken
        if (game.Players.Any(p => p.HeroId == heroId && p.UserId != userId))
            return GameResult.Fail("This hero has already been selected by another player.");

        player.HeroId = heroId;
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(game);
    }

    public async Task<GameResult> SetReadyAsync(Guid gameId, Guid userId, bool isReady)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.Status != GameStatus.WaitingForPlayers)
            return GameResult.Fail("Cannot change ready status after the game has started.");

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null)
            return GameResult.Fail("You are not in this game.");

        if (isReady && string.IsNullOrEmpty(player.HeroId))
            return GameResult.Fail("You must select a hero before marking yourself as ready.");

        player.IsReady = isReady;
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(game);
    }

    // In-game operations

    public async Task<GameResult> MovePlayerAsync(Guid gameId, Guid userId, int q, int r)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            var destination = new HexPosition { Q = q, R = r };
            return engine.MovePlayer(destination);
        });
    }

    public async Task<GameResult> PlayCardAsync(Guid gameId, Guid userId, string cardId, bool powered, int? manaIndex = null)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            ManaColor? manaUsed = null;
            
            // If powered and mana is specified, use the mana first
            if (powered && manaIndex.HasValue)
            {
                if (manaIndex.Value >= 0 && manaIndex.Value < engine.State.ManaPool.Count)
                {
                    manaUsed = engine.State.ManaPool[manaIndex.Value];
                    engine.UseMana(manaIndex.Value);
                }
            }
            
            return engine.PlayCard(cardId, powered, manaUsed);
        });
    }

    public async Task<GameResult> UseCardSidewaysAsync(Guid gameId, Guid userId, string cardId)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            return engine.UseCardSideways(cardId);
        });
    }

    public async Task<GameResult> UseManaAsync(Guid gameId, Guid userId, int dieIndex)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            return engine.UseMana(dieIndex);
        });
    }

    public async Task<GameResult> UseCrystalAsync(Guid gameId, Guid userId, string color)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            if (!Enum.TryParse<ManaColor>(color, true, out var manaColor))
                return GameActionResult.Fail($"Invalid mana color: {color}");
            
            return engine.UseCrystal(manaColor);
        });
    }

    public async Task<GameResult> RerollManaAsync(Guid gameId, Guid userId)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            return engine.RerollManaPool();
        });
    }

    public async Task<GameResult> EndTurnAsync(Guid gameId, Guid userId)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            return engine.EndTurn();
        });
    }

    public async Task<GameResult> RestAsync(Guid gameId, Guid userId)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            return engine.Rest();
        });
    }

    public async Task<GameResult> SelectTacticAsync(Guid gameId, Guid userId, string tacticId)
    {
        return await ExecuteGameAction(gameId, userId, engine =>
        {
            return engine.SelectTactic(tacticId);
        }, checkTurn: false); // During tactics selection, all players select simultaneously
    }

    public async Task<IReadOnlyList<(int Q, int R)>> GetValidMovesAsync(Guid gameId, Guid userId)
    {
        var game = await GetGameAsync(gameId);
        if (game == null || game.Status != GameStatus.InProgress)
            return Array.Empty<(int, int)>();

        var engine = new GameEngine.GameEngine(_definitionService);
        engine.LoadState(game.GameState);

        // Check if it's the user's turn
        var currentPlayer = engine.GetCurrentPlayer();
        if (currentPlayer == null || currentPlayer.UserId != userId)
            return Array.Empty<(int, int)>();

        // Get valid moves based on current movement points
        var validMoves = engine.GetValidMoves(currentPlayer.MovementRemaining);
        return validMoves.Select(p => (p.Q, p.R)).ToList().AsReadOnly();
    }

    // Combat operations

    public async Task<GameResult> InitiateCombatAsync(Guid gameId, Guid userId)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.InitiateCombat());
    }

    public async Task<GameResult> RangedAttackAsync(Guid gameId, Guid userId, int enemyIndex, int attackValue)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.RangedAttack(enemyIndex, attackValue));
    }

    public async Task<GameResult> BlockEnemyAsync(Guid gameId, Guid userId, int enemyIndex, int blockValue)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.BlockEnemy(enemyIndex, blockValue));
    }

    public async Task<GameResult> AttackEnemyAsync(Guid gameId, Guid userId, int enemyIndex, int attackValue)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.AttackEnemy(enemyIndex, attackValue));
    }

    public async Task<GameResult> AssignDamageAsync(Guid gameId, Guid userId, int damage)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.AssignDamage(damage));
    }

    public async Task<GameResult> EndCombatPhaseAsync(Guid gameId, Guid userId)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.EndCombatPhase());
    }

    public async Task<GameResult> FleeCombatAsync(Guid gameId, Guid userId)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.FleeCombat());
    }

    // Site interaction operations
    public async Task<GameResult> InteractWithSiteAsync(Guid gameId, Guid userId, string interactionType)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.InteractWithSite(interactionType));
    }

    public async Task<GameResult> RecruitUnitAsync(Guid gameId, Guid userId, string unitId)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.RecruitUnit(unitId));
    }

    public async Task<GameResult> HealAtSiteAsync(Guid gameId, Guid userId, int woundsToHeal)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.HealAtSite(woundsToHeal));
    }

    // Level up operations
    public async Task<GameResult> LevelUpAsync(Guid gameId, Guid userId, string? advancedActionId, string? skillId)
    {
        return await ExecuteGameAction(gameId, userId, engine => engine.LevelUp(advancedActionId, skillId));
    }

    private async Task<GameResult> ExecuteGameAction(Guid gameId, Guid userId, Func<IGameEngine, GameActionResult> action, bool checkTurn = true)
    {
        var game = await GetGameAsync(gameId);
        if (game == null)
            return GameResult.Fail("Game not found.");

        if (game.Status != GameStatus.InProgress)
            return GameResult.Fail("Game is not in progress.");

        // Check if user is in the game
        if (!game.Players.Any(p => p.UserId == userId))
            return GameResult.Fail("You are not in this game.");

        // Load game engine with current state
        var engine = new GameEngine.GameEngine(_definitionService);
        engine.LoadState(game.GameState);

        // Verify it's the user's turn (unless checkTurn is false, e.g., for tactics selection)
        if (checkTurn)
        {
            var currentPlayer = engine.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.UserId != userId)
                return GameResult.Fail("It's not your turn.");
        }
        else
        {
            // For tactics selection, verify the player hasn't already selected
            var playerIndex = engine.State.Players.FindIndex(p => p.UserId == userId);
            if (playerIndex >= 0 && engine.State.SelectedTactics.ContainsKey(playerIndex))
                return GameResult.Fail("You have already selected a tactic.");
            
            // Set current player index to this player for the action
            engine.State.CurrentPlayerIndex = playerIndex;
        }

        // Execute the action
        var result = action(engine);
        if (!result.Success)
            return GameResult.Fail(result.ErrorMessage ?? "Action failed.");

        // Save updated state
        game.GameState = engine.SaveState();
        await _dbContext.SaveChangesAsync();

        return GameResult.Ok(game);
    }
}
