using MageKnightOnline.Models;

namespace MageKnightOnline.Models;

public class SiteConquestResult
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site? Site { get; set; }
    public int PlayerId { get; set; }
    public int AttackPower { get; set; }
    public bool IsSuccessful { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int FameReward { get; set; }
    public int ReputationReward { get; set; }
    public int CrystalsReward { get; set; }
    public string? UnitReward { get; set; }
    public string? SpellReward { get; set; }
    public string? ArtifactReward { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string? Notes { get; set; }
}
