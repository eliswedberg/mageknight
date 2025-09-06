using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Artifact
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public ArtifactType Type { get; set; }
    
    public int Cost { get; set; } = 0;
    
    public int Attack { get; set; } = 0;
    
    public int Block { get; set; } = 0;
    
    public int Move { get; set; } = 0;
    
    public int Influence { get; set; } = 0;
    
    public int Fame { get; set; } = 0;
    
    public int Reputation { get; set; } = 0;
    
    public bool IsPermanent { get; set; } = true;
    
    public bool IsConsumable { get; set; } = false;
    
    public int Uses { get; set; } = 1;
    
    public string? SpecialEffect { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string Set { get; set; } = "Base";
    
    public Rarity Rarity { get; set; } = Rarity.Common;
}

public enum ArtifactType
{
    Weapon,
    Armor,
    Ring,
    Amulet,
    Boots,
    Cloak,
    Staff,
    Orb,
    Scroll,
    Potion,
    Special
}

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public class PlayerArtifact
{
    public int Id { get; set; }
    
    public int GamePlayerId { get; set; }
    public GamePlayer GamePlayer { get; set; } = null!;
    
    public int ArtifactId { get; set; }
    public Artifact Artifact { get; set; } = null!;
    
    public bool IsEquipped { get; set; } = false;
    
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    
    public int UsesRemaining { get; set; } = 1;
    
    public bool IsActive { get; set; } = false;
}
