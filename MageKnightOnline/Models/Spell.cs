using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Spell
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public SpellType Type { get; set; }
    
    public int ManaCost { get; set; }
    
    public int CrystalCost { get; set; } = 0;
    
    public int Range { get; set; } = 0;
    
    public int Attack { get; set; } = 0;
    
    public int Block { get; set; } = 0;
    
    public int Move { get; set; } = 0;
    
    public int Influence { get; set; } = 0;
    
    public int Fame { get; set; } = 0;
    
    public int Reputation { get; set; } = 0;
    
    public bool IsInstant { get; set; } = false;
    
    public bool IsPersistent { get; set; } = false;
    
    public string? SpecialEffect { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string Set { get; set; } = "Base";
}

public enum SpellType
{
    Attack,
    Defense,
    Movement,
    Influence,
    Healing,
    Utility,
    Summoning,
    Transformation,
    Special
}

public class PlayerSpell
{
    public int Id { get; set; }
    
    public int GamePlayerId { get; set; }
    public GamePlayer GamePlayer { get; set; } = null!;
    
    public int SpellId { get; set; }
    public Spell Spell { get; set; } = null!;
    
    public bool IsLearned { get; set; } = false;
    
    public DateTime LearnedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = false;
    
    public DateTime? ActivatedAt { get; set; }
    
    public int UsesRemaining { get; set; } = 1;
}
