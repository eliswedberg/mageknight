using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class HeroDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("color_theme")]
    public string ColorTheme { get; set; } = string.Empty;

    [JsonPropertyName("starting_cards_ref")]
    public string StartingCardsRef { get; set; } = string.Empty;

    [JsonPropertyName("skill_pool_ref")]
    public string SkillPoolRef { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("token_image")]
    public string? TokenImage { get; set; }

    [JsonPropertyName("mat_image")]
    public string? MatImage { get; set; }
}
