using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class UnitDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("rank")]
    public string Rank { get; set; } = string.Empty; // Regular or Elite

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("recruit_cost")]
    public int RecruitCost { get; set; }

    [JsonPropertyName("armor")]
    public int Armor { get; set; }

    [JsonPropertyName("abilities")]
    public List<string> Abilities { get; set; } = new();

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
