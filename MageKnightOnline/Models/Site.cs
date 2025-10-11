using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MageKnightOnline.Models;

/// <summary>
/// Site model based on sites.json schema
/// </summary>
public class Site
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Site identifier from sites.json (e.g., "village", "monastery", "city_red")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SiteId { get; set; } = string.Empty;
    
    /// <summary>
    /// Site type from sites.json schema
    /// </summary>
    public SiteType Type { get; set; }
    
    /// <summary>
    /// Site color (for cities and some other sites)
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; }
    
    /// <summary>
    /// Whether this site is fortified
    /// </summary>
    public bool IsFortified { get; set; } = false;
    
    /// <summary>
    /// Whether entering this site triggers assaults
    /// </summary>
    public bool EnteringAssaults { get; set; } = false;
    
    /// <summary>
    /// Effects triggered when site is revealed (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string WhenRevealed { get; set; } = "[]";
    
    /// <summary>
    /// Interaction options available at this site (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string InteractOptions { get; set; } = "[]";
    
    /// <summary>
    /// Interaction options when site is conquered (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string InteractConquered { get; set; } = "[]";
    
    /// <summary>
    /// Site defenders (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Defenders { get; set; } = "[]";
    
    /// <summary>
    /// Site rewards (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Rewards { get; set; } = "[]";
    
    /// <summary>
    /// Burn effects (for monasteries, etc.) (JSON object)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Burn { get; set; }
    
    /// <summary>
    /// Game session this site belongs to
    /// </summary>
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    /// <summary>
    /// Hex space this site is located on
    /// </summary>
    public int? HexSpaceId { get; set; }
    public HexSpace? HexSpace { get; set; }
    
    /// <summary>
    /// Whether the site has been revealed
    /// </summary>
    public bool IsRevealed { get; set; } = false;
    
    /// <summary>
    /// Whether the site has been conquered
    /// </summary>
    public bool IsConquered { get; set; } = false;
    
    /// <summary>
    /// Player who conquered the site
    /// </summary>
    public int? ConqueredByPlayerId { get; set; }
    public GamePlayer? ConqueredByPlayer { get; set; }
    
    /// <summary>
    /// When the site was conquered
    /// </summary>
    public DateTime? ConqueredAt { get; set; }
    
    /// <summary>
    /// Turn when the site was conquered
    /// </summary>
    public int? ConqueredOnTurn { get; set; }
    
    /// <summary>
    /// Whether the site has been burned/destroyed
    /// </summary>
    public bool IsBurned { get; set; } = false;
    
    /// <summary>
    /// Player who burned the site
    /// </summary>
    public int? BurnedByPlayerId { get; set; }
    public GamePlayer? BurnedByPlayer { get; set; }
    
    /// <summary>
    /// When the site was burned
    /// </summary>
    public DateTime? BurnedAt { get; set; }
    
    /// <summary>
    /// Site name for display
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Site description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Site enemies (legacy support)
    /// </summary>
    public ICollection<SiteEnemy> Enemies { get; set; } = new List<SiteEnemy>();
}

/// <summary>
/// Site types from sites.json schema
/// </summary>
public enum SiteType
{
    Village,
    Monastery,
    Keep,
    MageTower,
    City,
    Ruins,
    Dungeon,
    Tomb,
    MonsterDen,
    SpawningGrounds,
    Portal
}

public class SiteEnemy
{
    public int Id { get; set; }
    
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    
    public int Attack { get; set; }
    
    public int Block { get; set; }
    
    public int Health { get; set; }
    
    public int CurrentHealth { get; set; }
    
    public bool IsDefeated { get; set; } = false;
    
    public string? SpecialAbilities { get; set; }
    
    public int FameReward { get; set; } = 0;
    
    public int ReputationReward { get; set; } = 0;
}
