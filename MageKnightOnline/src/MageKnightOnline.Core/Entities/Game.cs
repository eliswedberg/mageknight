namespace MageKnightOnline.Core.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    
    // Scenario configuration
    public string ScenarioId { get; set; } = string.Empty;
    public int MinPlayers { get; set; } = 1;
    public int MaxPlayers { get; set; } = 4;
    
    // JSON columns for dynamic data
    public string? Settings { get; set; }  // Scenario-specific settings (JSON)
    public string? GameState { get; set; } // Full game state (JSON)
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    // Foreign keys
    public Guid CreatedByUserId { get; set; }
    
    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<GamePlayer> Players { get; set; } = new List<GamePlayer>();
}

public enum GameStatus
{
    WaitingForPlayers,
    InProgress,
    Completed,
    Cancelled
}
