using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class PlayerHand
{
    public int Id { get; set; }
    
    public int GamePlayerId { get; set; }
    public GamePlayer GamePlayer { get; set; } = null!;
    
    public int CardId { get; set; }
    public MageKnightCard Card { get; set; } = null!;
    
    public bool IsPlayed { get; set; } = false;
    
    public DateTime AddedToHand { get; set; } = DateTime.UtcNow;
    
    public int HandPosition { get; set; }
}

public class PlayerDeck
{
    public int Id { get; set; }
    
    public int GamePlayerId { get; set; }
    public GamePlayer GamePlayer { get; set; } = null!;
    
    public int CardId { get; set; }
    public MageKnightCard Card { get; set; } = null!;
    
    public int DeckPosition { get; set; }
    
    public bool IsDrawn { get; set; } = false;
}

public class PlayerDiscard
{
    public int Id { get; set; }
    
    public int GamePlayerId { get; set; }
    public GamePlayer GamePlayer { get; set; } = null!;
    
    public int CardId { get; set; }
    public MageKnightCard Card { get; set; } = null!;
    
    public DateTime DiscardedAt { get; set; } = DateTime.UtcNow;
    
    public int DiscardPosition { get; set; }
}
