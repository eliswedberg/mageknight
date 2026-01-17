using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Definition of a map site (village, keep, dungeon, etc.).
/// </summary>
public class SiteDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // safe, fortified, adventure, rampaging

    [JsonPropertyName("fortified")]
    public bool Fortified { get; set; } = false;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("interactions")]
    public List<SiteInteraction>? Interactions { get; set; }

    [JsonPropertyName("interactions_when_conquered")]
    public List<SiteInteraction>? InteractionsWhenConquered { get; set; }

    [JsonPropertyName("assault")]
    public SiteAssault? Assault { get; set; }

    [JsonPropertyName("combat")]
    public SiteCombat? Combat { get; set; }

    [JsonPropertyName("reward")]
    public SiteReward? Reward { get; set; }

    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; }

    [JsonPropertyName("crystal_colors")]
    public List<string>? CrystalColors { get; set; } // For mines

    // Computed properties
    public bool IsSafe => Type.Equals("safe", StringComparison.OrdinalIgnoreCase);
    public bool IsFortified => Type.Equals("fortified", StringComparison.OrdinalIgnoreCase) || Fortified;
    public bool IsAdventure => Type.Equals("adventure", StringComparison.OrdinalIgnoreCase);
    public bool IsRampaging => Type.Equals("rampaging", StringComparison.OrdinalIgnoreCase);
    public bool RequiresCombat => Combat != null || Assault != null;
}

/// <summary>
/// An interaction available at a site.
/// </summary>
public class SiteInteraction
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Recruit, Heal, GainCard, Special, Combat, etc.

    [JsonPropertyName("target")]
    public string? Target { get; set; } // Unit type, card type, etc.

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("cost")]
    public SiteInteractionCost? Cost { get; set; }

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("effect")]
    public string? Effect { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("reputation_change")]
    public int? ReputationChange { get; set; }

    [JsonPropertyName("day_effect")]
    public string? DayEffect { get; set; }

    [JsonPropertyName("night_effect")]
    public string? NightEffect { get; set; }

    [JsonPropertyName("enemy_type")]
    public string? EnemyType { get; set; }

    [JsonPropertyName("enemy_count")]
    public int? EnemyCount { get; set; }

    [JsonPropertyName("reward")]
    public string? Reward { get; set; }
}

/// <summary>
/// Cost for a site interaction.
/// </summary>
public class SiteInteractionCost
{
    [JsonPropertyName("influence")]
    public int? Influence { get; set; }

    [JsonPropertyName("mana")]
    public int? Mana { get; set; }
}

/// <summary>
/// Assault information for fortified sites.
/// </summary>
public class SiteAssault
{
    [JsonPropertyName("defenders")]
    public SiteDefenders? Defenders { get; set; }

    [JsonPropertyName("defender_count_by_level")]
    public Dictionary<string, int>? DefenderCountByLevel { get; set; }

    [JsonPropertyName("reward")]
    public string? Reward { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Defender configuration for a site.
/// </summary>
public class SiteDefenders
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Enemy type (Grey, Violet, White)

    [JsonPropertyName("count")]
    public object? Count { get; set; } // int or "variable"
}

/// <summary>
/// Combat information for adventure sites.
/// </summary>
public class SiteCombat
{
    [JsonPropertyName("rules")]
    public List<string>? Rules { get; set; } // NightRules, etc.

    [JsonPropertyName("enemies")]
    public object? Enemies { get; set; } // SiteDefenders or "Variable"

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Reward for completing a site.
/// </summary>
public class SiteReward
{
    [JsonPropertyName("type")]
    public string? Type { get; set; } // Artifact, Spell, Crystal, Fame, RuinsToken

    [JsonPropertyName("types")]
    public List<string>? Types { get; set; } // Multiple reward types

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
