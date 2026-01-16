using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class SkillDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("hero")]
    public string Hero { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("back_image")]
    public string? BackImage { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("effects")]
    public List<CardEffect> Effects { get; set; } = new();
}
