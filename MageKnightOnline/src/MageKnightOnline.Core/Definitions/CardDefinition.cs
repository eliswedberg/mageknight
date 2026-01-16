using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class CardDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("heroes")]
    public List<string>? Heroes { get; set; }

    [JsonPropertyName("count_per_hero")]
    public int? CountPerHero { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("effects_basic")]
    public List<CardEffect>? EffectsBasic { get; set; }

    [JsonPropertyName("effects_powered")]
    public List<CardEffect>? EffectsPowered { get; set; }

    [JsonPropertyName("effects_strong")]
    public List<CardEffect>? EffectsStrong { get; set; }

    [JsonPropertyName("effects_destroy")]
    public List<CardEffect>? EffectsDestroy { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    // Helper properties for UI
    [JsonIgnore]
    public string? Type => EffectsBasic?.FirstOrDefault()?.Type;

    [JsonIgnore]
    public string? ManaType => Color;

    [JsonIgnore]
    public string BasicEffect => string.Join(", ", EffectsBasic?.Select(e => $"{e.Type} {e.Value}") ?? Array.Empty<string>());

    [JsonIgnore]
    public string? PoweredEffect => EffectsPowered?.Any() == true 
        ? string.Join(", ", EffectsPowered.Select(e => $"{e.Type} {e.Value}")) 
        : null;

    [JsonIgnore]
    public int BasicValue => EffectsBasic?.FirstOrDefault()?.Value ?? 0;

    [JsonIgnore]
    public int? PoweredValue => EffectsPowered?.FirstOrDefault()?.Value;
}

public class CardEffect
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("attributes")]
    public List<string>? Attributes { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; }
}
