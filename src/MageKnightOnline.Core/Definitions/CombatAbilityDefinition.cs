using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Root object for combat_abilities.json.
/// </summary>
public class CombatAbilitiesRoot
{
    [JsonPropertyName("enemy_offensive_abilities")]
    public List<CombatAbilityDefinition> EnemyOffensiveAbilities { get; set; } = new();

    [JsonPropertyName("enemy_defensive_abilities")]
    public List<CombatAbilityDefinition> EnemyDefensiveAbilities { get; set; } = new();

    [JsonPropertyName("unit_resistances")]
    public List<UnitResistanceDefinition> UnitResistances { get; set; } = new();

    [JsonPropertyName("attack_elements")]
    public List<AttackElementDefinition> AttackElements { get; set; } = new();

    [JsonPropertyName("attack_types")]
    public List<AttackTypeDefinition> AttackTypes { get; set; } = new();

    [JsonPropertyName("block_efficiency")]
    public BlockEfficiencyDefinition? BlockEfficiency { get; set; }
}

/// <summary>
/// Definition of a combat ability (enemy offensive or defensive).
/// </summary>
public class CombatAbilityDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("element")]
    public string? Element { get; set; }

    [JsonPropertyName("resistance")]
    public string? Resistance { get; set; }

    [JsonPropertyName("effect")]
    public AbilityEffect? Effect { get; set; }
}

/// <summary>
/// Effect of a combat ability.
/// </summary>
public class AbilityEffect
{
    // Offensive effects
    [JsonPropertyName("block_multiplier")]
    public int? BlockMultiplier { get; set; } // Swift: 2

    [JsonPropertyName("damage_multiplier_if_unblocked")]
    public int? DamageMultiplierIfUnblocked { get; set; } // Brutal: 2

    [JsonPropertyName("extra_wounds_to_unit")]
    public int? ExtraWoundsToUnit { get; set; } // Poison: 1

    [JsonPropertyName("wounds_to_discard_pile")]
    public bool? WoundsToDiscardPile { get; set; } // Poison

    [JsonPropertyName("destroys_wounded_units")]
    public bool? DestroysWoundedUnits { get; set; } // Paralyze

    [JsonPropertyName("hero_discards_non_wounds")]
    public bool? HeroDiscardsNonWounds { get; set; } // Paralyze

    [JsonPropertyName("cannot_assign_to_units")]
    public bool? CannotAssignToUnits { get; set; } // Assassination

    [JsonPropertyName("summons_enemy")]
    public bool? SummonsEnemy { get; set; } // Summon

    [JsonPropertyName("requires_element_block")]
    public List<string>? RequiresElementBlock { get; set; } // Fire Attack, Ice Attack, etc.

    [JsonPropertyName("can_reduce_with_move")]
    public bool? CanReduceWithMove { get; set; } // Cumbersome

    [JsonPropertyName("armor_increase_per_wound")]
    public int? ArmorIncreasePerWound { get; set; } // Vampiric

    // Defensive effects
    [JsonPropertyName("requires_siege_in_ranged_phase")]
    public bool? RequiresSiegeInRangedPhase { get; set; } // Fortified

    [JsonPropertyName("halves_physical_attacks")]
    public bool? HalvesPhysicalAttacks { get; set; }

    [JsonPropertyName("halves_fire_attacks")]
    public bool? HalvesFireAttacks { get; set; }

    [JsonPropertyName("halves_ice_attacks")]
    public bool? HalvesIceAttacks { get; set; }

    [JsonPropertyName("ignores_red_effects")]
    public bool? IgnoresRedEffects { get; set; }

    [JsonPropertyName("ignores_blue_effects")]
    public bool? IgnoresBlueEffects { get; set; }

    [JsonPropertyName("has_two_armor_values")]
    public bool? HasTwoArmorValues { get; set; } // Elusive

    [JsonPropertyName("lower_armor_if_all_blocked")]
    public bool? LowerArmorIfAllBlocked { get; set; } // Elusive

    [JsonPropertyName("ignores_site_fortification")]
    public bool? IgnoresSiteFortification { get; set; } // Unfortified

    [JsonPropertyName("immune_to_non_combat_effects")]
    public bool? ImmuneToNonCombatEffects { get; set; } // Arcane Immunity

    [JsonPropertyName("must_be_targeted_first")]
    public bool? MustBeTargetedFirst { get; set; } // Defend
}

/// <summary>
/// Definition of a unit resistance.
/// </summary>
public class UnitResistanceDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("applies_to")]
    public string AppliesTo { get; set; } = string.Empty;
}

/// <summary>
/// Definition of an attack element.
/// </summary>
public class AttackElementDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("efficient_blocks")]
    public List<string> EfficientBlocks { get; set; } = new();
}

/// <summary>
/// Definition of an attack type.
/// </summary>
public class AttackTypeDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Block efficiency rules.
/// </summary>
public class BlockEfficiencyDefinition
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("rules")]
    public List<BlockEfficiencyRule> Rules { get; set; } = new();
}

/// <summary>
/// A single block efficiency rule.
/// </summary>
public class BlockEfficiencyRule
{
    [JsonPropertyName("attack_type")]
    public string AttackType { get; set; } = string.Empty;

    [JsonPropertyName("efficient_blocks")]
    public List<string> EfficientBlocks { get; set; } = new();

    [JsonPropertyName("inefficient_blocks")]
    public List<string> InefficientBlocks { get; set; } = new();
}
