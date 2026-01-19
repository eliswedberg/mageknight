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

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty; // Marauding Orcs, Keep Defenders, etc.

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("fame")]
    public int Fame { get; set; }

    [JsonPropertyName("attack")]
    public EnemyAttack Attack { get; set; } = new();

    [JsonPropertyName("attacks")]
    public List<EnemyAttack>? Attacks { get; set; } // For enemies with multiple attacks (Altem Mages)

    [JsonPropertyName("armor")]
    public EnemyArmor Armor { get; set; } = new();

    [JsonPropertyName("abilities")]
    public List<string> Abilities { get; set; } = new();

    [JsonPropertyName("ability_descriptions")]
    public Dictionary<string, string>? AbilityDescriptions { get; set; }

    [JsonPropertyName("summon_type")]
    public string? SummonType { get; set; } // For Summon ability

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("back_image")]
    public string? BackImage { get; set; }

    // Computed properties for abilities
    public bool IsSwift => Abilities.Contains("Swift", StringComparer.OrdinalIgnoreCase);
    public bool IsBrutal => Abilities.Contains("Brutal", StringComparer.OrdinalIgnoreCase);
    public bool IsFortified => Abilities.Contains("Fortified", StringComparer.OrdinalIgnoreCase);
    public bool IsPoison => Abilities.Contains("Poison", StringComparer.OrdinalIgnoreCase);
    public bool IsParalyze => Abilities.Contains("Paralyze", StringComparer.OrdinalIgnoreCase);
    public bool IsVampiric => Abilities.Contains("Vampiric", StringComparer.OrdinalIgnoreCase);
    public bool HasSummon => Abilities.Any(a => a.StartsWith("Summon", StringComparison.OrdinalIgnoreCase));
    public bool IsArcaneImmune => Abilities.Contains("Arcane_Immunity", StringComparer.OrdinalIgnoreCase);
    public bool IsAssassination => Abilities.Contains("Assassination", StringComparer.OrdinalIgnoreCase);
    public bool IsCumbersome => Abilities.Contains("Cumbersome", StringComparer.OrdinalIgnoreCase);

    // Computed properties for resistances (from Armor object)
    public bool HasPhysicalResistance => Armor.Resistances.Contains("Physical", StringComparer.OrdinalIgnoreCase);
    public bool HasFireResistance => Armor.Resistances.Contains("Fire", StringComparer.OrdinalIgnoreCase);
    public bool HasIceResistance => Armor.Resistances.Contains("Ice", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get the effective block requirement (doubled for Swift enemies).
    /// </summary>
    public int GetBlockRequirement() => IsSwift ? Attack.Value * 2 : Attack.Value;

    /// <summary>
    /// Get the effective damage dealt (doubled for Brutal if unblocked).
    /// </summary>
    public int GetDamageDealt(bool isFullyBlocked) => !isFullyBlocked && IsBrutal ? Attack.Value * 2 : Attack.Value;
}

public class EnemyAttack
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("element")]
    public string Element { get; set; } = "Physical"; // Physical, Fire, Ice, ColdFire

    [JsonPropertyName("is_ranged")]
    public bool IsRanged { get; set; } = false;

    // Legacy support for old format - attributes array
    [JsonPropertyName("attributes")]
    public List<string>? Attributes { get; set; }

    // Get element from either new Element field or legacy Attributes
    public string GetElement()
    {
        if (!string.IsNullOrEmpty(Element) && Element != "Physical")
            return Element;
        
        if (Attributes != null && Attributes.Count > 0)
        {
            var attr = Attributes.FirstOrDefault(a => 
                a.Equals("Fire", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("Ice", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("ColdFire", StringComparison.OrdinalIgnoreCase));
            if (attr != null) return attr;
        }
        
        return "Physical";
    }

    // Computed properties
    public bool IsFire => GetElement().Equals("Fire", StringComparison.OrdinalIgnoreCase);
    public bool IsIce => GetElement().Equals("Ice", StringComparison.OrdinalIgnoreCase);
    public bool IsColdFire => GetElement().Equals("ColdFire", StringComparison.OrdinalIgnoreCase);
    public bool IsPhysical => GetElement().Equals("Physical", StringComparison.OrdinalIgnoreCase);
}

public class EnemyArmor
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("resistances")]
    public List<string> Resistances { get; set; } = new();
}
