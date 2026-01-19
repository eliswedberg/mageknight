namespace MageKnightOnline.Core.Entities;

public class GamePlayer
{
    public Guid Id { get; set; }
    
    // Foreign keys
    public Guid GameId { get; set; }
    public Guid UserId { get; set; }
    
    // Player configuration
    public string? HeroId { get; set; }  // Selected hero (null until chosen)
    public bool IsReady { get; set; } = false;
    public int TurnOrder { get; set; } = 0;
    
    // Timestamps
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Game Game { get; set; } = null!;
    public User User { get; set; } = null!;
}
