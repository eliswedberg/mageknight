using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class GameAction
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int? PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    
    public ActionType Type { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON data for complex actions
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int TurnNumber { get; set; } = 1;
    
    public int ActionSequence { get; set; } = 1;
}

public enum ActionType
{
    // Game management
    GameStarted,
    GameEnded,
    PlayerJoined,
    PlayerLeft,
    TurnStarted,
    TurnEnded,
    
    // Mage Knight actions
    CardPlayed,
    CardDrawn,
    CardDiscarded,
    Movement,
    Attack,
    Block,
    SpellCast,
    ArtifactUsed,
    SkillLearned,
    LevelUp,
    FameGained,
    ReputationChanged,
    ManaUsed,
    CrystalGained,
    WoundTaken,
    WoundHealed,
    Pass,
    
    // Combat actions
    CombatStarted,
    CombatEnded,
    UnitRecruited,
    UnitDeployed,
    UnitDefeated,
    
    // Exploration actions
    SiteExplored,
    RuinExplored,
    DungeonExplored,
    KeepExplored,
    MageTowerExplored,
    MonasteryExplored,
    VillageExplored,
    CityExplored
}
