using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class PlayerCardAcquisition
{
    public int Id { get; set; }
    
    public int PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    
    public int CardId { get; set; }
    public MageKnightCard? Card { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession? GameSession { get; set; }
    
    public string AcquisitionMethod { get; set; } = string.Empty; // "Purchase", "Reward", "Conquest", "LevelUp"
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    
    public bool IsInHand { get; set; } = false;
    public bool IsInDeck { get; set; } = true;
    public bool IsInDiscard { get; set; } = false;
    public bool IsPermanent { get; set; } = false;
}

