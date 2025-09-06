using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Combat
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int AttackingPlayerId { get; set; }
    public GamePlayer AttackingPlayer { get; set; } = null!;
    
    public int? DefendingSiteId { get; set; }
    public Site? DefendingSite { get; set; }
    
    public int? DefendingPlayerId { get; set; }
    public GamePlayer? DefendingPlayer { get; set; }
    
    public CombatType Type { get; set; }
    
    public CombatStatus Status { get; set; } = CombatStatus.Preparation;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndedAt { get; set; }
    
    public int TurnNumber { get; set; }
    
    public ICollection<CombatAction> Actions { get; set; } = new List<CombatAction>();
    public ICollection<CombatParticipant> Participants { get; set; } = new List<CombatParticipant>();
}

public enum CombatType
{
    SiteAttack,
    PlayerVsPlayer,
    MonsterHunt,
    Siege
}

public enum CombatStatus
{
    Preparation,
    InProgress,
    Resolved,
    Cancelled
}

public class CombatAction
{
    public int Id { get; set; }
    
    public int CombatId { get; set; }
    public Combat Combat { get; set; } = null!;
    
    public int PlayerId { get; set; }
    public GamePlayer Player { get; set; } = null!;
    
    public ActionType Type { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON data for complex actions
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int ActionSequence { get; set; }
    
    public int? CardId { get; set; }
    public MageKnightCard? Card { get; set; }
    
    public int AttackValue { get; set; } = 0;
    public int BlockValue { get; set; } = 0;
    public int RangeValue { get; set; } = 0;
}

public class CombatParticipant
{
    public int Id { get; set; }
    
    public int CombatId { get; set; }
    public Combat Combat { get; set; } = null!;
    
    public int? PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    
    public int? EnemyId { get; set; }
    public SiteEnemy? Enemy { get; set; }
    
    public int AttackValue { get; set; } = 0;
    public int BlockValue { get; set; } = 0;
    public int Health { get; set; } = 0;
    public int CurrentHealth { get; set; } = 0;
    
    public bool IsDefeated { get; set; } = false;
    
    public int Initiative { get; set; } = 0;
}
