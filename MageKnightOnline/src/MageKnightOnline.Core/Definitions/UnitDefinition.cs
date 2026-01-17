using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
    public List<string> Abilities { get; set; } = new(); // String format: "Attack 3 (Ranged, Fire)"

    [JsonPropertyName("resistances")]
    public List<string>? Resistances { get; set; }

    [JsonPropertyName("special_abilities")]
    public List<string>? SpecialAbilities { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    // Computed properties
    public bool IsRegular => Rank.Equals("Regular", StringComparison.OrdinalIgnoreCase);
    public bool IsElite => Rank.Equals("Elite", StringComparison.OrdinalIgnoreCase);
    
    // Check resistances from both dedicated field and ability strings
    public bool HasPhysicalResistance => 
        (Resistances?.Contains("Physical", StringComparer.OrdinalIgnoreCase) ?? false) ||
        Abilities.Any(a => a.Contains("Physical Resistance", StringComparison.OrdinalIgnoreCase));
    public bool HasFireResistance => 
        (Resistances?.Contains("Fire", StringComparer.OrdinalIgnoreCase) ?? false) ||
        Abilities.Any(a => a.Contains("Fire Resistance", StringComparison.OrdinalIgnoreCase) || 
                          a.Contains("Fire/Ice Resistance", StringComparison.OrdinalIgnoreCase));
    public bool HasIceResistance => 
        (Resistances?.Contains("Ice", StringComparer.OrdinalIgnoreCase) ?? false) ||
        Abilities.Any(a => a.Contains("Ice Resistance", StringComparison.OrdinalIgnoreCase) ||
                          a.Contains("Fire/Ice Resistance", StringComparison.OrdinalIgnoreCase));
    public bool IsUnstoppable => 
        (SpecialAbilities?.Contains("Unstoppable", StringComparer.OrdinalIgnoreCase) ?? false) ||
        Abilities.Any(a => a.Contains("Unstoppable", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Helper class for parsed unit abilities - provides easy access to unit stats.
/// Used by GameEngine for combat calculations.
/// </summary>
public class UnitAbilities
{
    public int Attack { get; set; } = 0;
    public int Block { get; set; } = 0;
    public int Influence { get; set; } = 0;
    public int Move { get; set; } = 0;
    public int Heal { get; set; } = 0;
    
    public bool IsRanged { get; set; } = false;
    public bool IsSiege { get; set; } = false;
    public string? AttackElement { get; set; }
    public string? BlockElement { get; set; }
    
    public bool HasPhysicalResistance { get; set; } = false;
    public bool HasFireResistance { get; set; } = false;
    public bool HasIceResistance { get; set; } = false;
    
    public bool IsUnstoppable { get; set; } = false;
    public bool CanSummon { get; set; } = false;

    /// <summary>
    /// Create UnitAbilities from a UnitDefinition by parsing ability strings.
    /// </summary>
    public static UnitAbilities FromUnitDefinition(UnitDefinition unitDef)
    {
        var result = new UnitAbilities
        {
            HasPhysicalResistance = unitDef.HasPhysicalResistance,
            HasFireResistance = unitDef.HasFireResistance,
            HasIceResistance = unitDef.HasIceResistance,
            IsUnstoppable = unitDef.IsUnstoppable
        };

        foreach (var ability in unitDef.Abilities)
        {
            // Parse "Attack X" or "Attack X (attributes)"
            var attackMatch = Regex.Match(ability, @"(?:Summon\s+)?Attack\s+(\d+)(?:\s*\(([^)]+)\))?", RegexOptions.IgnoreCase);
            if (attackMatch.Success)
            {
                result.Attack = int.Parse(attackMatch.Groups[1].Value);
                if (attackMatch.Groups[2].Success)
                {
                    var attrs = attackMatch.Groups[2].Value.ToLower();
                    result.IsRanged = attrs.Contains("ranged");
                    result.IsSiege = attrs.Contains("siege");
                    if (attrs.Contains("fire") && attrs.Contains("ice"))
                        result.AttackElement = "ColdFire";
                    else if (attrs.Contains("fire"))
                        result.AttackElement = "Fire";
                    else if (attrs.Contains("ice"))
                        result.AttackElement = "Ice";
                }
                if (ability.Contains("Summon", StringComparison.OrdinalIgnoreCase))
                    result.CanSummon = true;
                continue;
            }

            // Parse "Block X" or "Block X (attributes)"
            var blockMatch = Regex.Match(ability, @"Block\s+(\d+)(?:\s*\(([^)]+)\))?", RegexOptions.IgnoreCase);
            if (blockMatch.Success)
            {
                result.Block = int.Parse(blockMatch.Groups[1].Value);
                if (blockMatch.Groups[2].Success)
                {
                    var attrs = blockMatch.Groups[2].Value.ToLower();
                    // Check for element (not resistance)
                    if (attrs.Contains("fire") && !attrs.Contains("resistance"))
                        result.BlockElement = "Fire";
                    else if (attrs.Contains("ice") && !attrs.Contains("resistance"))
                        result.BlockElement = "Ice";
                }
                continue;
            }

            // Parse "Influence X"
            var influenceMatch = Regex.Match(ability, @"Influence\s+(\d+)", RegexOptions.IgnoreCase);
            if (influenceMatch.Success)
            {
                result.Influence = int.Parse(influenceMatch.Groups[1].Value);
                continue;
            }

            // Parse "Move X"
            var moveMatch = Regex.Match(ability, @"Move\s+(\d+)", RegexOptions.IgnoreCase);
            if (moveMatch.Success)
            {
                result.Move = int.Parse(moveMatch.Groups[1].Value);
                continue;
            }

            // Parse "Heal X"
            var healMatch = Regex.Match(ability, @"Heal\s+(\d+)", RegexOptions.IgnoreCase);
            if (healMatch.Success)
            {
                result.Heal = int.Parse(healMatch.Groups[1].Value);
                continue;
            }
        }

        return result;
    }
}
