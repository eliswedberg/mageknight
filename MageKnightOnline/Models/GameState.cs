using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class GameState
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int RoundNumber { get; set; } = 1;
    
    public int TurnNumber { get; set; } = 1;
    
    public int? CurrentPlayerId { get; set; }
    public GamePlayer? CurrentPlayer { get; set; }
    
    public TurnPhase CurrentPhase { get; set; } = TurnPhase.Preparation;
    
    public GamePhase GamePhase { get; set; } = GamePhase.Early;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public bool IsNightPhase { get; set; } = false;
    
    public int NightRounds { get; set; } = 0;
    
    public int DayRounds { get; set; } = 0;
    
    public string? CurrentWeather { get; set; }
    
    public int GlobalFame { get; set; } = 0;
    
    public int GlobalReputation { get; set; } = 0;
    
    public ICollection<GameEvent> Events { get; set; } = new List<GameEvent>();
}

public enum GamePhase
{
    Early,      // Rounds 1-3
    Mid,        // Rounds 4-6
    Late,       // Rounds 7-9
    End         // Final scoring
}

public class GameEvent
{
    public int Id { get; set; }
    
    public int GameStateId { get; set; }
    public GameState GameState { get; set; } = null!;
    
    public int? PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    
    public EventType Type { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON data for complex events
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int RoundNumber { get; set; }
    
    public int TurnNumber { get; set; }
}

public enum EventType
{
    GameStarted,
    GameEnded,
    RoundStarted,
    RoundEnded,
    TurnStarted,
    TurnEnded,
    PlayerJoined,
    PlayerLeft,
    SiteExplored,
    SiteConquered,
    CombatStarted,
    CombatEnded,
    CardPlayed,
    CardDrawn,
    SpellCast,
    ArtifactUsed,
    UnitRecruited,
    UnitDeployed,
    LevelUp,
    FameGained,
    ReputationChanged,
    ManaUsed,
    CrystalGained,
    WoundTaken,
    WoundHealed,
    WeatherChanged,
    SpecialEvent
}
