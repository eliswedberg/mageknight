using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class TacticsDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty; // "Day" or "Night"

    [JsonPropertyName("position")]
    public int Position { get; set; } // Turn order position (1-6)

    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
