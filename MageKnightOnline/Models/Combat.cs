using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Combat
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    
    public int? AttackingPlayerId { get; set; }
    public GamePlayer? AttackingPlayer { get; set; }
    
    public int? DefendingPlayerId { get; set; }
    public GamePlayer? DefendingPlayer { get; set; }
    
    public CombatType Type { get; set; }
    public CombatStatus Status { get; set; } = CombatStatus.Preparing;
    
    // Combat flow
    public int CurrentTurn { get; set; } = 1;
    public int? CurrentParticipantId { get; set; }
    public CombatParticipant? CurrentParticipant { get; set; }
    
    // Combat phases
    public CombatPhase CurrentPhase { get; set; } = CombatPhase.Preparation;
    public bool IsInitiativePhase { get; set; } = false;
    public bool IsAttackPhase { get; set; } = false;
    public bool IsBlockPhase { get; set; } = false;
    public bool IsResolutionPhase { get; set; } = false;
    
    // Combat state
    public string CombatState { get; set; } = "{}"; // JSON object for complex state
    public string Modifiers { get; set; } = "{}"; // JSON object for combat modifiers
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    
    public int TurnNumber { get; set; }
    
    // Participants and actions
    public ICollection<CombatParticipant> Participants { get; set; } = new List<CombatParticipant>();
    public ICollection<CombatAction> Actions { get; set; } = new List<CombatAction>();
    public ICollection<CombatResult> Results { get; set; } = new List<CombatResult>();
}

public enum CombatType
{
    SiteConquest,
    PlayerVsPlayer,
    RampagingEnemy,
    Siege,
    Assault,
    Exploration,
    Rampage
}

public enum CombatStatus
{
    Preparing,
    InProgress,
    Resolved,
    Cancelled,
    Paused
}

public enum CombatPhase
{
    Preparation,
    Initiative,
    Attack,
    Block,
    Resolution,
    Cleanup
}

public class CombatAction
{
    public int Id { get; set; }
    
    public int CombatId { get; set; }
    public Combat Combat { get; set; } = null!;
    
    public int ParticipantId { get; set; }
    public CombatParticipant Participant { get; set; } = null!;
    
    public CombatActionType ActionType { get; set; }
    public string Description { get; set; } = string.Empty;
    
    // Target information
    public int? TargetId { get; set; }
    public CombatParticipant? Target { get; set; }
    
    // Action values
    public int Value { get; set; } = 0; // Attack value, block value, damage dealt, etc.
    public int DamageDealt { get; set; } = 0;
    public int DamageReceived { get; set; } = 0;
    
    // Timing
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TurnNumber { get; set; } = 0;
    public int ActionSequence { get; set; } = 0;
    
    // Special effects
    public string SpecialEffects { get; set; } = "[]"; // JSON array
    public string Modifiers { get; set; } = "{}"; // JSON object
    
    // Result
    public bool IsResolved { get; set; } = false;
    public string Result { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CombatParticipant
{
    public int Id { get; set; }
    
    public int CombatId { get; set; }
    public Combat Combat { get; set; } = null!;
    
    // Player participant
    public int? PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    
    // Enemy participant
    public int? EnemyId { get; set; }
    public Enemy? Enemy { get; set; }
    
    // Combat stats
    public int AttackValue { get; set; } = 0;
    public int BlockValue { get; set; } = 0;
    public int Health { get; set; } = 0;
    public int CurrentHealth { get; set; } = 0;
    public int Initiative { get; set; } = 0;
    
    // Status
    public bool IsDefeated { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    // Combat order
    public int CombatOrder { get; set; } = 0;
    
    // Special abilities and resistances
    public string SpecialAbilities { get; set; } = "[]"; // JSON array
    public string Resistances { get; set; } = "[]"; // JSON array
    
    // Combat modifiers
    public int AttackModifier { get; set; } = 0;
    public int BlockModifier { get; set; } = 0;
    public int DamageModifier { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<CombatAction> Actions { get; set; } = new List<CombatAction>();
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
    public EnemyColor Color { get; set; }
    
    // Base stats
    public int Armor { get; set; } = 0;
    public int Attack { get; set; } = 0;
    public int Health { get; set; } = 0;
    public int Initiative { get; set; } = 0;
    
    // Special properties
    public string Abilities { get; set; } = "[]"; // JSON array
    public string Resistances { get; set; } = "[]"; // JSON array
    public int FameValue { get; set; } = 0;
    
    // Combat modifiers
    public bool IsFortified { get; set; } = false;
    public bool IsRanged { get; set; } = false;
    public bool IsSiege { get; set; } = false;
    public bool IsElite { get; set; } = false;
    
    public string? ImageUrl { get; set; }
    
    public ICollection<CombatParticipant> CombatParticipants { get; set; } = new List<CombatParticipant>();
}

public enum CombatActionType
{
    Attack,
    Block,
    RangedAttack,
    SiegeAttack,
    SpecialAbility,
    Spell,
    Artifact,
    UnitAbility,
    Resistance,
    Vulnerability,
    Damage,
    Heal,
    Buff,
    Debuff,
    Initiative,
    TurnStart,
    TurnEnd,
    CombatStart,
    CombatEnd,
    Victory,
    Defeat
}

public enum EnemyType
{
    Orc,
    Draconum,
    Undead,
    Demon,
    Beast,
    Elemental,
    Construct,
    Humanoid,
    Dragon,
    Guardian
}

public enum EnemyColor
{
    Brown,
    Gray,
    Violet,
    Draconum,
    Orc,
    Red,
    Blue,
    White,
    Green,
    Black
}
