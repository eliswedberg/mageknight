using MageKnightOnline.Models;

namespace MageKnightOnline.Models;

public class CombatResult
{
    public int Id { get; set; }
    public int CombatId { get; set; }
    public Combat? Combat { get; set; }
    
    public int AttackerId { get; set; }
    public int DefenderId { get; set; }
    
    public int AttackerTotal { get; set; }
    public int DefenderTotal { get; set; }
    
    public int? Winner { get; set; }
    public int DamageDealt { get; set; }
    public bool IsVictory { get; set; }
    
    public DateTime ResolvedAt { get; set; }
    public string? Notes { get; set; }
}
