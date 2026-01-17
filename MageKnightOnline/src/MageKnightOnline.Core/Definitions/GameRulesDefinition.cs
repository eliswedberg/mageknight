using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Root object for game_rules.json containing all game rules.
/// </summary>
public class GameRulesDefinition
{
    [JsonPropertyName("game_flow")]
    public GameFlowRules? GameFlow { get; set; }

    [JsonPropertyName("player_turn")]
    public PlayerTurnRules? PlayerTurn { get; set; }

    [JsonPropertyName("combat")]
    public CombatRules? Combat { get; set; }

    [JsonPropertyName("movement")]
    public MovementRules? Movement { get; set; }

    [JsonPropertyName("mana")]
    public ManaRules? Mana { get; set; }

    [JsonPropertyName("leveling")]
    public LevelingRules? Leveling { get; set; }

    [JsonPropertyName("reputation")]
    public ReputationRules? Reputation { get; set; }

    [JsonPropertyName("units")]
    public UnitRules? Units { get; set; }

    [JsonPropertyName("sites")]
    public SiteRules? Sites { get; set; }

    [JsonPropertyName("cards")]
    public CardRules? Cards { get; set; }
}

public class GameFlowRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("phases")]
    public List<GamePhaseRule>? Phases { get; set; }
}

public class GamePhaseRule
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<string>? Steps { get; set; }

    [JsonPropertyName("conditions")]
    public List<string>? Conditions { get; set; }

    [JsonPropertyName("cleanup")]
    public List<string>? Cleanup { get; set; }
}

public class PlayerTurnRules
{
    [JsonPropertyName("types")]
    public Dictionary<string, TurnTypeRule>? Types { get; set; }

    [JsonPropertyName("any_turn_actions")]
    public AnyTurnActions? AnyTurnActions { get; set; }

    [JsonPropertyName("end_of_turn")]
    public EndOfTurnRule? EndOfTurn { get; set; }
}

public class TurnTypeRule
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("phases")]
    public List<TurnPhaseRule>? Phases { get; set; }

    [JsonPropertyName("options")]
    public List<RestOption>? Options { get; set; }
}

public class TurnPhaseRule
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }

    [JsonPropertyName("action_types")]
    public List<string>? ActionTypes { get; set; }

    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }
}

public class RestOption
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}

public class AnyTurnActions
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("actions")]
    public List<string>? Actions { get; set; }
}

public class EndOfTurnRule
{
    [JsonPropertyName("steps")]
    public List<string>? Steps { get; set; }
}

public class CombatRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("phases")]
    public List<CombatPhaseRule>? Phases { get; set; }

    [JsonPropertyName("fleeing")]
    public FleeingRules? Fleeing { get; set; }

    [JsonPropertyName("rewards")]
    public CombatRewardsRules? Rewards { get; set; }
}

public class CombatPhaseRule
{
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class FleeingRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class CombatRewardsRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class MovementRules
{
    [JsonPropertyName("terrain_rules")]
    public TerrainRulesSection? TerrainRules { get; set; }

    [JsonPropertyName("exploration")]
    public ExplorationRules? Exploration { get; set; }

    [JsonPropertyName("provoking_enemies")]
    public ProvokingRules? ProvokingEnemies { get; set; }
}

public class TerrainRulesSection
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("day_night_variation")]
    public string? DayNightVariation { get; set; }

    [JsonPropertyName("modifiers")]
    public List<string>? Modifiers { get; set; }
}

public class ExplorationRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class ProvokingRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class ManaRules
{
    [JsonPropertyName("source")]
    public ManaSourceRules? Source { get; set; }

    [JsonPropertyName("crystals")]
    public CrystalRules? Crystals { get; set; }

    [JsonPropertyName("colors")]
    public ManaColorsSection? Colors { get; set; }

    [JsonPropertyName("powered_effects")]
    public PoweredEffectsRules? PoweredEffects { get; set; }
}

public class ManaSourceRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class CrystalRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class ManaColorsSection
{
    [JsonPropertyName("basic")]
    public List<string>? Basic { get; set; }

    [JsonPropertyName("special")]
    public Dictionary<string, string>? Special { get; set; }
}

public class PoweredEffectsRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class LevelingRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("process")]
    public List<string>? Process { get; set; }

    [JsonPropertyName("fame_thresholds")]
    public List<int>? FameThresholds { get; set; }
}

public class ReputationRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("effects")]
    public List<string>? Effects { get; set; }

    [JsonPropertyName("changes")]
    public ReputationChanges? Changes { get; set; }
}

public class ReputationChanges
{
    [JsonPropertyName("positive")]
    public List<string>? Positive { get; set; }

    [JsonPropertyName("negative")]
    public List<string>? Negative { get; set; }
}

public class UnitRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("recruiting")]
    public RecruitingRules? Recruiting { get; set; }

    [JsonPropertyName("using_units")]
    public UsingUnitsRules? UsingUnits { get; set; }

    [JsonPropertyName("wounding_units")]
    public WoundingUnitsRules? WoundingUnits { get; set; }
}

public class RecruitingRules
{
    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }

    [JsonPropertyName("locations")]
    public Dictionary<string, string>? Locations { get; set; }
}

public class UsingUnitsRules
{
    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class WoundingUnitsRules
{
    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class SiteRules
{
    [JsonPropertyName("fortified_sites")]
    public SiteTypeRules? FortifiedSites { get; set; }

    [JsonPropertyName("adventure_sites")]
    public SiteTypeRules? AdventureSites { get; set; }

    [JsonPropertyName("rampaging_enemies")]
    public SiteTypeRules? RampagingEnemies { get; set; }
}

public class SiteTypeRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class CardRules
{
    [JsonPropertyName("playing_sideways")]
    public PlayingSidewaysRules? PlayingSideways { get; set; }

    [JsonPropertyName("wounds")]
    public WoundCardRules? Wounds { get; set; }
}

public class PlayingSidewaysRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("effects")]
    public Dictionary<string, string>? Effects { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}

public class WoundCardRules
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }
}
