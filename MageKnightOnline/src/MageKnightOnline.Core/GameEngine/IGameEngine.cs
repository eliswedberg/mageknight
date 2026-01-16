using MageKnightOnline.Core.GameState;

namespace MageKnightOnline.Core.GameEngine;

/// <summary>
/// Interface for the game engine that handles all game logic.
/// </summary>
public interface IGameEngine
{
    /// <summary>
    /// Gets the current game state.
    /// </summary>
    GameStateModel State { get; }

    /// <summary>
    /// Loads a game state from JSON.
    /// </summary>
    void LoadState(string? gameStateJson);

    /// <summary>
    /// Serializes the current game state to JSON.
    /// </summary>
    string SaveState();

    /// <summary>
    /// Gets the current player state.
    /// </summary>
    PlayerState? GetCurrentPlayer();

    /// <summary>
    /// Gets a player state by user ID.
    /// </summary>
    PlayerState? GetPlayer(Guid userId);

    /// <summary>
    /// Calculates valid movement destinations for the current player.
    /// </summary>
    IEnumerable<HexPosition> GetValidMoves(int movementPoints);

    /// <summary>
    /// Moves the current player to a hex.
    /// </summary>
    GameActionResult MovePlayer(HexPosition destination);

    /// <summary>
    /// Plays a card from the current player's hand.
    /// </summary>
    GameActionResult PlayCard(string cardId, bool powered = false, ManaColor? manaUsed = null);

    /// <summary>
    /// Uses a card sideways for +1 of its type.
    /// </summary>
    GameActionResult UseCardSideways(string cardId);

    /// <summary>
    /// Ends the current player's turn.
    /// </summary>
    GameActionResult EndTurn();

    /// <summary>
    /// Current player chooses to rest.
    /// </summary>
    GameActionResult Rest();

    /// <summary>
    /// Uses a mana die from the pool.
    /// </summary>
    GameActionResult UseMana(int dieIndex);

    /// <summary>
    /// Uses a crystal from the player's inventory.
    /// </summary>
    GameActionResult UseCrystal(ManaColor color);

    /// <summary>
    /// Rerolls the mana pool (start of turn).
    /// </summary>
    GameActionResult RerollManaPool();

    /// <summary>
    /// Draws cards up to hand limit.
    /// </summary>
    GameActionResult DrawCards();

    /// <summary>
    /// Selects a tactic card for the current player.
    /// </summary>
    GameActionResult SelectTactic(string tacticId);

    /// <summary>
    /// Gets the available tactics for this round.
    /// </summary>
    IEnumerable<string> GetAvailableTactics();

    /// <summary>
    /// Checks if all players have selected tactics.
    /// </summary>
    bool AllPlayersSelectedTactics();

    /// <summary>
    /// Adds a log entry.
    /// </summary>
    void AddLogEntry(string action, string? details = null);

    // Combat operations
    
    /// <summary>
    /// Initiates combat at the current player's position.
    /// </summary>
    GameActionResult InitiateCombat();

    /// <summary>
    /// Uses ranged attack against an enemy.
    /// </summary>
    GameActionResult RangedAttack(int enemyIndex, int attackValue);

    /// <summary>
    /// Blocks an enemy's attack.
    /// </summary>
    GameActionResult BlockEnemy(int enemyIndex, int blockValue);

    /// <summary>
    /// Attacks an enemy in melee.
    /// </summary>
    GameActionResult AttackEnemy(int enemyIndex, int attackValue);

    /// <summary>
    /// Assigns damage to the player (wounds).
    /// </summary>
    GameActionResult AssignDamage(int damage);

    /// <summary>
    /// Ends the current combat phase.
    /// </summary>
    GameActionResult EndCombatPhase();

    /// <summary>
    /// Flees from combat.
    /// </summary>
    GameActionResult FleeCombat();

    // Site interactions

    /// <summary>
    /// Gets available interactions at the current player's position.
    /// </summary>
    IEnumerable<SiteInteractionOption> GetAvailableSiteInteractions();

    /// <summary>
    /// Performs a site interaction.
    /// </summary>
    GameActionResult InteractWithSite(string interactionType, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Recruits a unit from a site.
    /// </summary>
    GameActionResult RecruitUnit(string unitId);

    /// <summary>
    /// Heals wounds at a site.
    /// </summary>
    GameActionResult HealAtSite(int woundsToHeal);

    /// <summary>
    /// Plunders a village for cards.
    /// </summary>
    GameActionResult Plunder();

    // Level Up

    /// <summary>
    /// Checks if the current player can level up.
    /// </summary>
    bool CanLevelUp();

    /// <summary>
    /// Gets the available advanced actions for level up selection.
    /// </summary>
    IEnumerable<string> GetAvailableAdvancedActions();

    /// <summary>
    /// Gets the available skills for level up selection.
    /// </summary>
    IEnumerable<string> GetAvailableSkills();

    /// <summary>
    /// Performs a level up, selecting an advanced action and skill.
    /// </summary>
    GameActionResult LevelUp(string? advancedActionId, string? skillId);

    /// <summary>
    /// Gets the fame required for next level.
    /// </summary>
    int GetFameForNextLevel();
}

/// <summary>
/// Represents an available site interaction option.
/// </summary>
public class SiteInteractionOption
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? InfluenceCost { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? UnavailableReason { get; set; }
}

/// <summary>
/// Result of a game action.
/// </summary>
public class GameActionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ActionDescription { get; set; }

    public static GameActionResult Ok(string description) => new() { Success = true, ActionDescription = description };
    public static GameActionResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
