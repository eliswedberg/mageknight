using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class ScenarioDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("rounds")]
    public int Rounds { get; set; }

    [JsonPropertyName("map_shape")]
    public string MapShape { get; set; } = string.Empty;

    [JsonPropertyName("tiles_deck")]
    public TilesDeck TilesDeck { get; set; } = new();

    [JsonPropertyName("city_levels")]
    public List<int> CityLevels { get; set; } = new();

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("special_rules")]
    public List<string> SpecialRules { get; set; } = new();

    [JsonPropertyName("min_players")]
    public int MinPlayers { get; set; } = 1;

    [JsonPropertyName("max_players")]
    public int MaxPlayers { get; set; } = 4;

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}

public class TilesDeck
{
    [JsonPropertyName("countryside")]
    public int Countryside { get; set; }

    [JsonPropertyName("core")]
    public int Core { get; set; }

    [JsonPropertyName("cities")]
    public int Cities { get; set; }
}
