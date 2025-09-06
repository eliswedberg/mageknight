using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Unit
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public UnitType Type { get; set; }
    
    public int Cost { get; set; } = 0;
    
    public int Attack { get; set; } = 0;
    
    public int Block { get; set; } = 0;
    
    public int Move { get; set; } = 0;
    
    public int Influence { get; set; } = 0;
    
    public int Fame { get; set; } = 0;
    
    public int Reputation { get; set; } = 0;
    
    public int Health { get; set; } = 1;
    
    public bool IsElite { get; set; } = false;
    
    public bool IsMercenary { get; set; } = false;
    
    public bool IsMage { get; set; } = false;
    
    public string? SpecialAbilities { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string Set { get; set; } = "Base";
    
    public Rarity Rarity { get; set; } = Rarity.Common;
}

public enum UnitType
{
    Infantry,
    Cavalry,
    Archer,
    Mage,
    Elite,
    Mercenary,
    Special
}

public class PlayerUnit
{
    public int Id { get; set; }
    
    public int GamePlayerId { get; set; }
    public GamePlayer GamePlayer { get; set; } = null!;
    
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    
    public bool IsRecruited { get; set; } = false;
    
    public DateTime RecruitedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeployed { get; set; } = false;
    
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    
    public int CurrentHealth { get; set; }
    
    public bool IsWounded { get; set; } = false;
    
    public bool IsDefeated { get; set; } = false;
}
