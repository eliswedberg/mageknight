using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class EnemyDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Green, Grey, Violet, Brown, Red, White

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("fame")]
    public int Fame { get; set; }

    [JsonPropertyName("attack")]
    public EnemyAttack Attack { get; set; } = new();

    [JsonPropertyName("armor")]
    public EnemyArmor Armor { get; set; } = new();

    [JsonPropertyName("abilities")]
    public List<string> Abilities { get; set; } = new();

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("back_image")]
    public string? BackImage { get; set; }
}

public class EnemyAttack
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("attributes")]
    public List<string> Attributes { get; set; } = new();
}

public class EnemyArmor
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("resistances")]
    public List<string> Resistances { get; set; } = new();
}
