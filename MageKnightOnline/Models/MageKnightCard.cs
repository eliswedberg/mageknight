using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class MageKnightCard
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public CardType Type { get; set; }
    
    public int Cost { get; set; }
    
    public int Attack { get; set; }
    
    public int Block { get; set; }
    
    public int Move { get; set; }
    
    public int Influence { get; set; }
    
    public int Fame { get; set; }
    
    public int Reputation { get; set; }
    
    public bool IsSpell { get; set; }
    
    public bool IsArtifact { get; set; }
    
    public bool IsUnit { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string? Set { get; set; } = "Base";
}

public enum CardType
{
    // Basic cards
    BasicMove,
    BasicAttack,
    BasicBlock,
    BasicInfluence,
    
    // Advanced cards
    AdvancedMove,
    AdvancedAttack,
    AdvancedBlock,
    AdvancedInfluence,
    
    // Spells
    Spell,
    
    // Artifacts
    Artifact,
    
    // Units
    Unit,
    
    // Special cards
    Special
}
