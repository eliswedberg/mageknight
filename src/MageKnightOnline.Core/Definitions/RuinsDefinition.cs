using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Definition for a ruins token that can be drawn when exploring Ancient Ruins.
/// </summary>
public class RuinsDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Resources, Spell, Artifact, Unit, Combat

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("effects")]
    public List<RuinsEffect>? Effects { get; set; }

    [JsonPropertyName("enemies")]
    public List<RuinsEnemy>? Enemies { get; set; }

    /// <summary>
    /// True if this is a combat token (requires fighting enemies).
    /// </summary>
    [JsonIgnore]
    public bool IsCombatToken => Type == "Combat" || Enemies?.Any() == true;

    /// <summary>
    /// True if this is a loot/reward token.
    /// </summary>
    [JsonIgnore]
    public bool IsLootToken => !IsCombatToken;
}

/// <summary>
/// An effect granted by a ruins token.
/// </summary>
public class RuinsEffect
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // GainCrystal, GainMana, GainCard, Recruit

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; } // SpellOffer, ArtifactDeck, etc.

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Enemy configuration for a ruins combat token.
/// </summary>
public class RuinsEnemy
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Green, Grey, Brown, Violet, Red

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
}

