using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

public class MapTileDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("back_type")]
    public string BackType { get; set; } = "Countryside";

    [JsonPropertyName("is_starting_tile")]
    public bool IsStartingTile { get; set; } = false;

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("back_image")]
    public string? BackImage { get; set; }

    [JsonPropertyName("city_image")]
    public string? CityImage { get; set; }

    [JsonPropertyName("hexes")]
    public List<TileHexDefinition> Hexes { get; set; } = new();
}

public class TileHexDefinition
{
    [JsonPropertyName("position")]
    public int Position { get; set; } // 0 = center, 1-6 = surrounding hexes

    [JsonPropertyName("terrain")]
    public string Terrain { get; set; } = "Plains";

    [JsonPropertyName("site")]
    public string? Site { get; set; }
}
