using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Combat
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int SiteId { get; set; }
    public Site? Site { get; set; }
    
    public CombatType Type { get; set; }
    
    public CombatStatus Status { get; set; } = CombatStatus.Preparing;
    
    public int CurrentTurn { get; set; }
    public int? CurrentParticipantId { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndedAt { get; set; }
    
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
    Preparing,
    InProgress,
    Completed,
    Abandoned
}

public class CombatAction
{
    public int Id { get; set; }
    
    public int CombatId { get; set; }
    public Combat Combat { get; set; } = null!;
    
    public int ParticipantId { get; set; }
    public CombatParticipant Participant { get; set; } = null!;
    
    public CombatActionType ActionType { get; set; }
    public int Value { get; set; }
    public int? TargetId { get; set; }
    public string? Description { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CombatParticipant
{
    public int Id { get; set; }
    
    public int CombatId { get; set; }
    public Combat Combat { get; set; } = null!;
    
    public int? PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    
    public int? EnemyId { get; set; }
    public Enemy? Enemy { get; set; }
    
    public int AttackValue { get; set; }
    public int BlockValue { get; set; }
    public int Health { get; set; }
    public int CurrentHealth { get; set; }
    public int Initiative { get; set; }
    public bool IsDefeated { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Enemy
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public EnemyType Type { get; set; }
    public int Level { get; set; }
    public int AttackValue { get; set; }
    public int BlockValue { get; set; }
    public int Health { get; set; }
    public int Initiative { get; set; }
    
    public string? SpecialAbilities { get; set; } // JSON string
    public string? LootTable { get; set; } // JSON string
    
    public string? ImageUrl { get; set; }
}

public enum CombatActionType
{
    Attack,
    Block,
    Special,
    Defend,
    Retreat
}

public enum EnemyType
{
    Orc,
    Goblin,
    Skeleton,
    Dragon,
    Demon,
    Undead,
    Beast,
    Humanoid,
    Elemental,
    Construct
}
