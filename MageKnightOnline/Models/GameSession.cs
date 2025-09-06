using System.ComponentModel.DataAnnotations;
using MageKnightOnline.Data;

namespace MageKnightOnline.Models;

public class GameSession
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    
    public int MaxPlayers { get; set; } = 4;
    
    public int CurrentPlayers { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    [Required]
    public string HostUserId { get; set; } = string.Empty;
    
    public ApplicationUser HostUser { get; set; } = null!;
    
    public ICollection<GamePlayer> Players { get; set; } = new List<GamePlayer>();
    
    public ICollection<GameAction> Actions { get; set; } = new List<GameAction>();
}

public enum GameStatus
{
    WaitingForPlayers,
    InProgress,
    Paused,
    Completed,
    Cancelled
}
