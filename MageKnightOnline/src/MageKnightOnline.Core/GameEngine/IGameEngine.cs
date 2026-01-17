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
    /// Moves the current player using flight (ignores terrain cost, costs 1 per hex).
    /// </summary>
    GameActionResult MovePlayerWithFlight(HexPosition destination);

    /// <summary>
    /// Explores a new tile at the edge of the map. Costs movement and requires being at an edge hex.
    /// </summary>
    GameActionResult ExploreTile(HexPosition edgeHex, int? edgePosition = null);

    /// <summary>
    /// Moves the current player using safe movement (avoids provoking enemies).
    /// </summary>
    GameActionResult MovePlayerSafely(HexPosition destination);

    /// <summary>
    /// Gets valid flight destinations for the current player.
    /// </summary>
    IEnumerable<HexPosition> GetValidFlightMoves(int flightPoints);

    /// <summary>
    /// Gets hexes with rampaging enemies that the player would provoke.
    /// </summary>
    IEnumerable<HexPosition> GetRampagingEnemyHexes();

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
    /// Uses a mana die from the pool (takes temporary mana).
    /// </summary>
    GameActionResult UseMana(int dieIndex);

    /// <summary>
    /// Undoes the mana die selection (returns temporary mana).
    /// Can only be done if no irreversible actions have been taken.
    /// </summary>
    GameActionResult UndoUseMana();

    /// <summary>
    /// Uses a crystal from the player's inventory.
    /// </summary>
    GameActionResult UseCrystal(ManaColor color);

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

    // Unit operations in combat

    /// <summary>
    /// Activates a unit to use its ability in combat.
    /// </summary>
    GameActionResult ActivateUnit(int unitIndex, string abilityType, int? enemyIndex = null);

    /// <summary>
    /// Assigns damage to a unit instead of taking wounds.
    /// </summary>
    GameActionResult AssignDamageToUnit(int unitIndex, int damage);

    /// <summary>
    /// Gets the available units that can be activated in the current combat phase.
    /// </summary>
    IEnumerable<UnitCombatOption> GetAvailableUnitActions();

    /// <summary>
    /// Heals a wounded unit.
    /// </summary>
    GameActionResult HealUnit(int unitIndex);

    /// <summary>
    /// Disbands a unit (removes it from the player's command).
    /// </summary>
    GameActionResult DisbandUnit(int unitIndex);

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

    // Ruins token operations

    /// <summary>
    /// Gets the currently active ruins token being resolved, if any.
    /// </summary>
    ActiveRuinsToken? GetActiveRuinsToken();

    /// <summary>
    /// Resolves a pending choice for the active ruins token.
    /// </summary>
    GameActionResult ResolveRuinsChoice(int choiceIndex, string selection);

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

    // Victory conditions

    /// <summary>
    /// Checks if victory conditions have been met.
    /// </summary>
    bool CheckVictoryConditions();

    /// <summary>
    /// Calculates final scores for all players.
    /// </summary>
    VictoryState CalculateFinalScores();

    /// <summary>
    /// Ends the game and calculates final results.
    /// </summary>
    GameActionResult EndGame(string reason);

    /// <summary>
    /// Gets the current victory state (null if game not over).
    /// </summary>
    VictoryState? GetVictoryState();
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
/// Represents an available unit action in combat.
/// </summary>
public class UnitCombatOption
{
    public int UnitIndex { get; set; }
    public string UnitId { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public int Armor { get; set; } = 3;
    public bool IsWounded { get; set; }
    public bool IsReady { get; set; }
    public bool UsedThisCombat { get; set; }
    public List<UnitAbilityOption> AvailableAbilities { get; set; } = new();
}

/// <summary>
/// Represents a specific ability a unit can use.
/// </summary>
public class UnitAbilityOption
{
    public string AbilityType { get; set; } = string.Empty; // Attack, Block, Influence, Move, Heal
    public int Value { get; set; }
    public string? Element { get; set; } // Fire, Ice, etc.
    public bool IsRanged { get; set; }
    public bool IsSiege { get; set; }
    public string Description { get; set; } = string.Empty;
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
