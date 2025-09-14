using System.ComponentModel.DataAnnotations;
using MageKnightOnline.Data;

namespace MageKnightOnline.Models;

public class GamePlayer
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public int PlayerNumber { get; set; }
    
    public PlayerStatus Status { get; set; } = PlayerStatus.Joined;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LeftAt { get; set; }
    
    // Mage Knight specific properties
    public string? KnightName { get; set; }
    public int Level { get; set; } = 1;
    public int Fame { get; set; } = 0;
    public int Reputation { get; set; } = 0;
    public int Crystals { get; set; } = 0;
    public int Mana { get; set; } = 0;
    public int HandSize { get; set; } = 5;
    public int DeckSize { get; set; } = 16;
    public int DiscardSize { get; set; } = 0;
    public int Wounds { get; set; } = 0;
    
    // Enhanced leveling properties
    public int Experience { get; set; } = 0;
    public int ExperienceToNextLevel { get; set; } = 100;
    public int MaxHealth { get; set; } = 10;
    public int CurrentHealth { get; set; } = 10;
    public int MaxMana { get; set; } = 5;
    public int CurrentMana { get; set; } = 5;
    public int MaxCrystals { get; set; } = 10;
    public int CurrentCrystals { get; set; } = 0;
    
    // Character progression
    public string? UnlockedAbilities { get; set; } // JSON string
    public string? EquippedArtifacts { get; set; } // JSON string
    public int TotalFameEarned { get; set; } = 0;
    public int TotalReputationEarned { get; set; } = 0;
    public int SitesConquered { get; set; } = 0;
    public int EnemiesDefeated { get; set; } = 0;
    
    public bool IsCurrentPlayer { get; set; } = false;
    public bool HasPassed { get; set; } = false;
    
    public ICollection<GameAction> Actions { get; set; } = new List<GameAction>();
}

public enum PlayerStatus
{
    Joined,
    Ready,
    Playing,
    Left,
    Eliminated
}
