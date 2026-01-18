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
    public List<string>? CrystalColors { get; set; } // For mines (legacy)

    [JsonPropertyName("crystal_color")]
    public string? CrystalColor { get; set; } // For individual mine types

    [JsonPropertyName("icon")]
    public string? Icon { get; set; } // Emoji icon for display

    [JsonPropertyName("fame_bonus")]
    public int? FameBonus { get; set; } // Extra fame for defeating this site

    [JsonPropertyName("ruins_token_types")]
    public List<RuinsTokenInfo>? RuinsTokenTypes { get; set; } // For ruins sites

    // Computed properties
    public bool IsSafe => Type.Equals("safe", StringComparison.OrdinalIgnoreCase);
    public bool IsFortified => Type.Equals("fortified", StringComparison.OrdinalIgnoreCase) || Fortified;
    public bool IsAdventure => Type.Equals("adventure", StringComparison.OrdinalIgnoreCase);
    public bool IsRampaging => Type.Equals("rampaging", StringComparison.OrdinalIgnoreCase);
    public bool RequiresCombat => Combat != null || Assault != null;
    public bool IsMine => Id?.StartsWith("site_mine") == true;
}

/// <summary>
/// Info about a ruins token type for display purposes.
/// </summary>
public class RuinsTokenInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Loot or Combat

    [JsonPropertyName("reward")]
    public string? Reward { get; set; }

    [JsonPropertyName("enemies")]
    public string? Enemies { get; set; }
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
    public object? Reward { get; set; } // Can be string or SiteReward object

    [JsonPropertyName("fame")]
    public int? Fame { get; set; } // Fame gained from this interaction

    [JsonPropertyName("destroys_site")]
    public bool? DestroysSite { get; set; } // If true, site is destroyed after interaction

    [JsonPropertyName("ends_interaction")]
    public bool? EndsInteraction { get; set; } // If true, cannot do more interactions this turn

    [JsonPropertyName("repeatable")]
    public bool? Repeatable { get; set; } // If true, can be done multiple times per visit

    [JsonPropertyName("reputation_modifier")]
    public bool? ReputationModifier { get; set; } // If true, cost is modified by reputation

    [JsonPropertyName("color")]
    public string? Color { get; set; } // For crystal/mana interactions
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

    [JsonPropertyName("fortified")]
    public bool Fortified { get; set; } = true;

    [JsonPropertyName("reward")]
    public string? Reward { get; set; }

    [JsonPropertyName("fame_per_enemy")]
    public int? FamePerEnemy { get; set; }

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
    public List<string>? Rules { get; set; } // NightRules, Rampaging, RuinsToken, etc.

    [JsonPropertyName("enemies")]
    public object? Enemies { get; set; } // SiteDefenders or "Variable"

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; } = false; // If true, combat is required upon entering

    [JsonPropertyName("simultaneous")]
    public bool Simultaneous { get; set; } = false; // If true, fight all enemies at once

    [JsonPropertyName("provoke_movement")]
    public bool ProvokeMovement { get; set; } = false; // If true, triggers when moving nearby

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    // Computed properties
    public bool UsesNightRules => Rules?.Contains("NightRules") == true;
    public bool IsRampaging => Rules?.Contains("Rampaging") == true;
    public bool UsesRuinsTokens => Rules?.Contains("RuinsToken") == true;
}

/// <summary>
/// Reward for completing a site.
/// </summary>
public class SiteReward
{
    [JsonPropertyName("type")]
    public string? Type { get; set; } // Artifact, Spell, Crystal, Fame, RuinsToken

    [JsonPropertyName("types")]
    public List<SiteRewardItem>? Types { get; set; } // Multiple reward items

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("draw")]
    public int? Draw { get; set; } // Number of cards to draw

    [JsonPropertyName("keep")]
    public int? Keep { get; set; } // Number of cards to keep

    [JsonPropertyName("choice")]
    public bool? Choice { get; set; } // If true, player can choose color/type

    [JsonPropertyName("random")]
    public bool? Random { get; set; } // If true, random selection

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// A single reward item (for sites with multiple rewards).
/// </summary>
public class SiteRewardItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("draw")]
    public int? Draw { get; set; }

    [JsonPropertyName("keep")]
    public int? Keep { get; set; }
}
