using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.GameState;

/// <summary>
/// Represents the complete state of a Mage Knight game.
/// This is serialized to JSON and stored in the Game.GameState column.
/// </summary>
public class GameStateModel
{
    [JsonPropertyName("round")]
    public int Round { get; set; } = 1;

    [JsonPropertyName("is_day")]
    public bool IsDay { get; set; } = true;

    [JsonPropertyName("current_player_index")]
    public int CurrentPlayerIndex { get; set; } = 0;

    [JsonPropertyName("phase")]
    public GamePhase Phase { get; set; } = GamePhase.Movement;

    [JsonPropertyName("players")]
    public List<PlayerState> Players { get; set; } = new();

    [JsonPropertyName("map")]
    public MapState Map { get; set; } = new();

    [JsonPropertyName("decks")]
    public DeckState Decks { get; set; } = new();

    [JsonPropertyName("mana_pool")]
    public List<ManaColor> ManaPool { get; set; } = new();

    [JsonPropertyName("turn_order")]
    public List<int> TurnOrder { get; set; } = new();

    [JsonPropertyName("game_log")]
    public List<GameLogEntry> GameLog { get; set; } = new();

    // Tactics selection state
    [JsonPropertyName("available_tactics")]
    public List<string> AvailableTactics { get; set; } = new(); // Tactic IDs available this round

    [JsonPropertyName("selected_tactics")]
    public Dictionary<int, string> SelectedTactics { get; set; } = new(); // PlayerIndex -> TacticId

    // Combat state
    [JsonPropertyName("combat")]
    public CombatState? Combat { get; set; }

    // Victory state
    [JsonPropertyName("victory")]
    public VictoryState? Victory { get; set; }

    // Scenario tracking
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; set; } = string.Empty;

    [JsonPropertyName("cities_conquered")]
    public int CitiesConquered { get; set; } = 0;

    [JsonPropertyName("total_cities")]
    public int TotalCities { get; set; } = 0;

    [JsonPropertyName("city_revealed")]
    public bool CityRevealed { get; set; } = false;
}

/// <summary>
/// State for tracking victory conditions.
/// </summary>
public class VictoryState
{
    [JsonPropertyName("is_game_over")]
    public bool IsGameOver { get; set; } = false;

    [JsonPropertyName("victory_type")]
    public VictoryType VictoryType { get; set; } = VictoryType.None;

    [JsonPropertyName("winner_user_ids")]
    public List<Guid> WinnerUserIds { get; set; } = new();

    [JsonPropertyName("final_scores")]
    public List<PlayerScore> FinalScores { get; set; } = new();

    [JsonPropertyName("end_reason")]
    public string EndReason { get; set; } = string.Empty;
}

public enum VictoryType
{
    None,
    CityConquest,       // All cities conquered
    ScenarioGoal,       // Scenario-specific goal achieved
    TimeOut,            // All rounds completed
    Cooperative,        // Co-op victory
    Defeat              // All players eliminated or failed objective
}

/// <summary>
/// Final score for a player at end of game.
/// </summary>
public class PlayerScore
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("hero_name")]
    public string HeroName { get; set; } = string.Empty;

    [JsonPropertyName("fame")]
    public int Fame { get; set; }

    [JsonPropertyName("reputation_bonus")]
    public int ReputationBonus { get; set; }

    [JsonPropertyName("cities_conquered")]
    public int CitiesConquered { get; set; }

    [JsonPropertyName("adventure_sites_conquered")]
    public int AdventureSitesConquered { get; set; }

    [JsonPropertyName("artifacts_count")]
    public int ArtifactsCount { get; set; }

    [JsonPropertyName("spells_count")]
    public int SpellsCount { get; set; }

    [JsonPropertyName("advanced_actions_count")]
    public int AdvancedActionsCount { get; set; }

    [JsonPropertyName("total_score")]
    public int TotalScore { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }
}

/// <summary>
/// State of an active combat encounter.
/// </summary>
public class CombatState
{
    [JsonPropertyName("phase")]
    public CombatPhase Phase { get; set; } = CombatPhase.RangedAttack;

    [JsonPropertyName("enemies")]
    public List<CombatEnemy> Enemies { get; set; } = new();

    [JsonPropertyName("position")]
    public HexPosition Position { get; set; } = new();

    [JsonPropertyName("site_type")]
    public string? SiteType { get; set; }

    [JsonPropertyName("is_night_rules")]
    public bool IsNightRules { get; set; } = false; // Dungeons/Tombs use night rules

    [JsonPropertyName("summoned_enemies")]
    public List<string> SummonedEnemies { get; set; } = new(); // Enemies summoned during combat

    [JsonPropertyName("swift_enemies_attacked")]
    public bool SwiftEnemiesAttacked { get; set; } = false; // Track if swift enemies already attacked

    [JsonPropertyName("total_unblocked_damage")]
    public int TotalUnblockedDamage { get; set; } = 0; // Track damage to assign
}

public enum CombatPhase
{
    SwiftAttack,    // Swift enemies attack first (before ranged)
    RangedAttack,   // Player can use ranged attacks (and siege vs fortified)
    Block,          // Player must block enemy attacks
    AssignDamage,   // Player assigns unblocked damage as wounds
    Attack,         // Player attacks enemies in melee
    Resolution      // Combat ends, rewards given
}

/// <summary>
/// An enemy in combat.
/// </summary>
public class CombatEnemy
{
    [JsonPropertyName("enemy_id")]
    public string EnemyId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("armor")]
    public int Armor { get; set; }

    [JsonPropertyName("attack")]
    public int Attack { get; set; }

    [JsonPropertyName("attack_type")]
    public string AttackType { get; set; } = "Physical"; // Physical, Fire, Ice, Cold

    [JsonPropertyName("resistances")]
    public List<string> Resistances { get; set; } = new(); // Physical, Fire, Ice

    [JsonPropertyName("current_damage")]
    public int CurrentDamage { get; set; } = 0;

    [JsonPropertyName("is_defeated")]
    public bool IsDefeated { get; set; } = false;

    [JsonPropertyName("is_blocked")]
    public bool IsBlocked { get; set; } = false; // Was this enemy's attack fully blocked?

    [JsonPropertyName("abilities")]
    public List<string> Abilities { get; set; } = new();

    [JsonPropertyName("fame")]
    public int Fame { get; set; } = 2;

    // Computed properties
    public bool IsSwift => Abilities.Contains("Swift");
    public bool IsFortified => Abilities.Contains("Fortified");
    public bool IsBrutal => Abilities.Contains("Brutal");
    public bool IsPoison => Abilities.Contains("Poison");
    public bool IsParalyze => Abilities.Contains("Paralyze");
    public bool CanSummon => Abilities.Any(a => a.StartsWith("Summon"));
}

public enum GamePhase
{
    Setup,
    TacticsSelection, // New phase for selecting tactics at start of round
    Movement,
    Interaction,
    Combat,
    Rest,
    RoundEnd
}

public enum ManaColor
{
    Red,
    Blue,
    Green,
    White,
    Black,
    Gold
}

/// <summary>
/// State for a single player in the game.
/// </summary>
public class PlayerState
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("hero_id")]
    public string HeroId { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public HexPosition Position { get; set; } = new();

    [JsonPropertyName("fame")]
    public int Fame { get; set; } = 0;

    [JsonPropertyName("reputation")]
    public int Reputation { get; set; } = 0;

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("armor")]
    public int Armor { get; set; } = 2;

    [JsonPropertyName("hand_limit")]
    public int HandLimit { get; set; } = 5;

    [JsonPropertyName("crystals")]
    public CrystalInventory Crystals { get; set; } = new();

    [JsonPropertyName("mana_tokens")]
    public ManaTokenInventory ManaTokens { get; set; } = new();

    [JsonPropertyName("command_tokens")]
    public int CommandTokens { get; set; } = 1; // Unit limit

    [JsonPropertyName("deck")]
    public List<string> Deck { get; set; } = new(); // Main deck for drawing

    [JsonPropertyName("hand")]
    public List<string> Hand { get; set; } = new(); // Card IDs

    [JsonPropertyName("deed_deck")]
    public List<string> DeedDeck { get; set; } = new(); // Card IDs

    [JsonPropertyName("discard_pile")]
    public List<string> DiscardPile { get; set; } = new(); // Card IDs

    [JsonPropertyName("units")]
    public List<UnitState> Units { get; set; } = new();

    [JsonPropertyName("skills")]
    public List<string> Skills { get; set; } = new(); // Skill IDs

    [JsonPropertyName("movement_remaining")]
    public int MovementRemaining { get; set; } = 0;

    [JsonPropertyName("has_rested")]
    public bool HasRested { get; set; } = false;

    // Accumulated effects for current turn/action
    [JsonPropertyName("attack_pool")]
    public int AttackPool { get; set; } = 0;

    [JsonPropertyName("block_pool")]
    public int BlockPool { get; set; } = 0;

    [JsonPropertyName("influence_pool")]
    public int InfluencePool { get; set; } = 0;

    [JsonPropertyName("heal_pool")]
    public int HealPool { get; set; } = 0;

    // Attack modifiers
    [JsonPropertyName("ranged_attack")]
    public int RangedAttack { get; set; } = 0;

    [JsonPropertyName("siege_attack")]
    public int SiegeAttack { get; set; } = 0;

    // Element types for current attacks
    [JsonPropertyName("attack_elements")]
    public List<string> AttackElements { get; set; } = new(); // "Fire", "Ice", "ColdFire"

    // Collected items
    [JsonPropertyName("artifacts")]
    public List<string> Artifacts { get; set; } = new(); // Artifact IDs

    [JsonPropertyName("spells")]
    public List<string> Spells { get; set; } = new(); // Spell IDs

    [JsonPropertyName("advanced_actions")]
    public List<string> AdvancedActions { get; set; } = new(); // Advanced Action IDs
}

/// <summary>
/// State of a unit owned by a player.
/// </summary>
public class UnitState
{
    [JsonPropertyName("unit_id")]
    public string UnitId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("armor")]
    public int Armor { get; set; } = 3;

    [JsonPropertyName("is_wounded")]
    public bool IsWounded { get; set; } = false;

    [JsonPropertyName("is_ready")]
    public bool IsReady { get; set; } = true;

    [JsonPropertyName("used_this_combat")]
    public bool UsedThisCombat { get; set; } = false;

    /// <summary>
    /// Can this unit be activated? Must be ready and not wounded.
    /// </summary>
    public bool CanActivate => IsReady && !IsWounded;

    /// <summary>
    /// Can this unit be used for blocking? Wounded units can still block but take damage.
    /// </summary>
    public bool CanBlock => IsReady;
}

/// <summary>
/// Hexagonal position on the map.
/// </summary>
public class HexPosition
{
    [JsonPropertyName("q")]
    public int Q { get; set; } = 0; // Column

    [JsonPropertyName("r")]
    public int R { get; set; } = 0; // Row

    public override bool Equals(object? obj)
    {
        if (obj is HexPosition other)
            return Q == other.Q && R == other.R;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Q, R);

    public static HexPosition operator +(HexPosition a, HexPosition b)
        => new() { Q = a.Q + b.Q, R = a.R + b.R };
}

/// <summary>
/// State of the game map.
/// </summary>
public class MapState
{
    [JsonPropertyName("tiles")]
    public List<MapTileState> Tiles { get; set; } = new();

    [JsonPropertyName("revealed_hexes")]
    public HashSet<string> RevealedHexes { get; set; } = new(); // "q,r" format

    [JsonPropertyName("hex_data")]
    public Dictionary<string, HexState> HexData { get; set; } = new(); // "q,r" -> HexState
}

/// <summary>
/// State of a single hex on the map.
/// </summary>
public class HexState
{
    [JsonPropertyName("terrain")]
    public string Terrain { get; set; } = "Plains";

    [JsonPropertyName("site_type")]
    public string? SiteType { get; set; }

    [JsonPropertyName("enemies")]
    public List<string> Enemies { get; set; } = new();

    [JsonPropertyName("is_conquered")]
    public bool IsConquered { get; set; } = false;

    [JsonPropertyName("owner_user_id")]
    public Guid? OwnerUserId { get; set; }
}

/// <summary>
/// State of a single map tile.
/// </summary>
public class MapTileState
{
    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public HexPosition Position { get; set; } = new();

    [JsonPropertyName("rotation")]
    public int Rotation { get; set; } = 0; // 0-5 for hex rotations

    [JsonPropertyName("is_revealed")]
    public bool IsRevealed { get; set; } = false;

    [JsonPropertyName("site_states")]
    public Dictionary<string, SiteState> SiteStates { get; set; } = new();
}

/// <summary>
/// State of a site on the map (village, keep, etc.)
/// </summary>
public class SiteState
{
    [JsonPropertyName("is_conquered")]
    public bool IsConquered { get; set; } = false;

    [JsonPropertyName("enemies")]
    public List<string> Enemies { get; set; } = new(); // Enemy IDs

    [JsonPropertyName("owner_user_id")]
    public Guid? OwnerUserId { get; set; }
}

/// <summary>
/// State of all shared decks.
/// </summary>
public class DeckState
{
    [JsonPropertyName("advanced_actions")]
    public List<string> AdvancedActions { get; set; } = new();

    [JsonPropertyName("spells")]
    public List<string> Spells { get; set; } = new();

    [JsonPropertyName("artifacts")]
    public List<string> Artifacts { get; set; } = new();

    [JsonPropertyName("regular_units")]
    public List<string> RegularUnits { get; set; } = new();

    [JsonPropertyName("elite_units")]
    public List<string> EliteUnits { get; set; } = new();

    [JsonPropertyName("countryside_tiles")]
    public List<string> CountrysideTiles { get; set; } = new();

    [JsonPropertyName("core_tiles")]
    public List<string> CoreTiles { get; set; } = new();

    [JsonPropertyName("city_tiles")]
    public List<string> CityTiles { get; set; } = new();

    // Enemy decks by type
    [JsonPropertyName("enemy_decks")]
    public Dictionary<string, List<string>> EnemyDecks { get; set; } = new();
}

/// <summary>
/// A log entry for game history.
/// </summary>
public class GameLogEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("player_index")]
    public int? PlayerIndex { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Inventory of mana crystals.
/// </summary>
public class CrystalInventory
{
    [JsonPropertyName("red")]
    public int Red { get; set; } = 0;

    [JsonPropertyName("blue")]
    public int Blue { get; set; } = 0;

    [JsonPropertyName("green")]
    public int Green { get; set; } = 0;

    [JsonPropertyName("white")]
    public int White { get; set; } = 0;

    [JsonPropertyName("gold")]
    public int Gold { get; set; } = 0;
}

/// <summary>
/// Inventory of mana tokens (temporary mana).
/// </summary>
public class ManaTokenInventory
{
    [JsonPropertyName("red")]
    public int Red { get; set; } = 0;

    [JsonPropertyName("blue")]
    public int Blue { get; set; } = 0;

    [JsonPropertyName("green")]
    public int Green { get; set; } = 0;

    [JsonPropertyName("white")]
    public int White { get; set; } = 0;

    [JsonPropertyName("black")]
    public int Black { get; set; } = 0;

    [JsonPropertyName("gold")]
    public int Gold { get; set; } = 0;
}
