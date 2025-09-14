using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class EnhancedPlayerHand
{
    public int Id { get; set; }
    
    public int PlayerId { get; set; }
    public GamePlayer Player { get; set; } = null!;
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public List<PlayerCardAcquisition> Cards { get; set; } = new();
    public int MaxHandSize { get; set; } = 5;
    public int CurrentHandSize => Cards.Count(c => c.IsInHand);
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
