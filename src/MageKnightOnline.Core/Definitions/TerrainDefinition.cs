using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Definition of terrain movement costs.
/// </summary>
public class TerrainDefinition
{
    [JsonPropertyName("terrain")]
    public string Terrain { get; set; } = string.Empty;

    [JsonPropertyName("cost_day")]
    public int CostDay { get; set; }

    [JsonPropertyName("cost_night")]
    public int CostNight { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("special")]
    public string? Special { get; set; } // "impassable"

    /// <summary>
    /// Check if this terrain is impassable.
    /// </summary>
    public bool IsImpassable => Special?.Equals("impassable", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Get the movement cost for the current time of day.
    /// </summary>
    public int GetCost(bool isDay) => isDay ? CostDay : CostNight;
}
