using MageKnightOnline.Core.Entities;

namespace MageKnightOnline.Core.Services;

public interface IGameService
{
    // Lobby operations
    Task<IReadOnlyList<Game>> GetAvailableGamesAsync();
    Task<IReadOnlyList<Game>> GetMyGamesAsync(Guid userId);
    Task<Game?> GetGameAsync(Guid gameId);

    /// <summary>Removes all games that are WaitingForPlayers or InProgress. Returns number of games removed.</summary>
    Task<int> ClearOngoingGamesAsync();

    Task<GameResult> CreateGameAsync(Guid userId, CreateGameRequest request);
    Task<GameResult> JoinGameAsync(Guid gameId, Guid userId);
    Task<GameResult> LeaveGameAsync(Guid gameId, Guid userId);
    Task<GameResult> StartGameAsync(Guid gameId, Guid userId);
    Task<GameResult> CancelGameAsync(Guid gameId, Guid userId);
    
    Task<GameResult> SelectHeroAsync(Guid gameId, Guid userId, string heroId);
    Task<GameResult> SetReadyAsync(Guid gameId, Guid userId, bool isReady);

    // In-game operations
    Task<GameResult> MovePlayerAsync(Guid gameId, Guid userId, int q, int r);
    Task<GameResult> PlayCardAsync(Guid gameId, Guid userId, string cardId, bool powered, int? manaIndex = null);
    Task<GameResult> UseCardSidewaysAsync(Guid gameId, Guid userId, string cardId, string bonusType = "move");
    Task<GameResult> ResolveChoiceAsync(Guid gameId, Guid userId, string choiceId, string? discardCardId = null);
    Task<GameResult> CancelChoiceAsync(Guid gameId, Guid userId);
    Task<GameResult> UseManaAsync(Guid gameId, Guid userId, int dieIndex);
    Task<GameResult> UndoUseManaAsync(Guid gameId, Guid userId);
    Task<GameResult> UndoLastActionAsync(Guid gameId, Guid userId);
    Task<bool> CanUndoActionAsync(Guid gameId, Guid userId);
    Task<GameResult> UseCrystalAsync(Guid gameId, Guid userId, string color);
    Task<GameResult> UseManaTokenAsync(Guid gameId, Guid userId, string color);
    Task<GameResult> EndTurnAsync(Guid gameId, Guid userId);
    Task<GameResult> RestAsync(Guid gameId, Guid userId);
    Task<GameResult> SelectTacticAsync(Guid gameId, Guid userId, string tacticId);
    
    // Query methods
    Task<IReadOnlyList<(int Q, int R)>> GetValidMovesAsync(Guid gameId, Guid userId);
    Task<bool> CanExploreTileAsync(Guid gameId, Guid userId);
    
    // Exploration
    Task<GameResult> ExploreTileAsync(Guid gameId, Guid userId, int q, int r);

    // Combat operations
    Task<GameResult> InitiateCombatAsync(Guid gameId, Guid userId);
    Task<GameResult> RangedAttackAsync(Guid gameId, Guid userId, int enemyIndex, int attackValue);
    Task<GameResult> BlockEnemyAsync(Guid gameId, Guid userId, int enemyIndex, int blockValue);
    Task<GameResult> AttackEnemyAsync(Guid gameId, Guid userId, int enemyIndex, int attackValue);
    Task<GameResult> AssignDamageAsync(Guid gameId, Guid userId, int damage);
    Task<GameResult> EndCombatPhaseAsync(Guid gameId, Guid userId);
    Task<GameResult> FleeCombatAsync(Guid gameId, Guid userId);

    // Unit operations in combat
    Task<GameResult> ActivateUnitAsync(Guid gameId, Guid userId, int unitIndex, string abilityType, int? enemyIndex = null);
    Task<GameResult> AssignDamageToUnitAsync(Guid gameId, Guid userId, int unitIndex, int damage);
    Task<GameResult> HealUnitAsync(Guid gameId, Guid userId, int unitIndex);
    Task<GameResult> DisbandUnitAsync(Guid gameId, Guid userId, int unitIndex);

    // Site interaction operations
    Task<GameResult> InteractWithSiteAsync(Guid gameId, Guid userId, string interactionType);
    Task<GameResult> RecruitUnitAsync(Guid gameId, Guid userId, string unitId);
    Task<GameResult> HealAtSiteAsync(Guid gameId, Guid userId, int woundsToHeal);

    // Level up operations
    Task<GameResult> LevelUpAsync(Guid gameId, Guid userId, string? advancedActionId, string? skillId);
}

public class CreateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 4;
}

public class GameResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Game? Game { get; set; }

    public static GameResult Ok(Game game) => new() { Success = true, Game = game };
    public static GameResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
