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
    public List<string> Abilities { get; set; } = new();

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    // Parsed ability values (computed from Abilities list)
    public UnitAbilities ParsedAbilities => _parsedAbilities ??= ParseAbilities();
    private UnitAbilities? _parsedAbilities;

    private UnitAbilities ParseAbilities()
    {
        var result = new UnitAbilities();
        
        foreach (var ability in Abilities)
        {
            // Parse "Attack X" or "Attack X (attributes)"
            var attackMatch = Regex.Match(ability, @"Attack\s+(\d+)(?:\s*\(([^)]+)\))?", RegexOptions.IgnoreCase);
            if (attackMatch.Success)
            {
                result.Attack = int.Parse(attackMatch.Groups[1].Value);
                if (attackMatch.Groups[2].Success)
                {
                    var attrs = attackMatch.Groups[2].Value.ToLower();
                    result.IsRanged = attrs.Contains("ranged");
                    result.IsSiege = attrs.Contains("siege");
                    if (attrs.Contains("fire")) result.AttackElement = "Fire";
                    else if (attrs.Contains("ice")) result.AttackElement = "Ice";
                }
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
                    if (attrs.Contains("fire")) result.BlockElement = "Fire";
                    else if (attrs.Contains("ice")) result.BlockElement = "Ice";
                    if (attrs.Contains("physical resistance")) result.HasPhysicalResistance = true;
                    if (attrs.Contains("fire resistance")) result.HasFireResistance = true;
                    if (attrs.Contains("ice resistance")) result.HasIceResistance = true;
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

            // Special abilities
            if (ability.Contains("Unstoppable", StringComparison.OrdinalIgnoreCase))
                result.IsUnstoppable = true;
            if (ability.Contains("Summon", StringComparison.OrdinalIgnoreCase))
                result.CanSummon = true;
        }

        return result;
    }
}

/// <summary>
/// Parsed unit abilities for easier access in game logic.
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
}
