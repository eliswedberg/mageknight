using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class GameTurn
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int TurnNumber { get; set; }
    
    public TurnPhase Phase { get; set; } = TurnPhase.Preparation;
    
    public int CurrentPlayerId { get; set; }
    public GamePlayer CurrentPlayer { get; set; } = null!;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndedAt { get; set; }
    
    public int ActionsRemaining { get; set; } = 0;
    
    public int ManaAvailable { get; set; } = 0;
    
    public int CrystalsAvailable { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<TurnAction> Actions { get; set; } = new List<TurnAction>();
}

public enum TurnPhase
{
    Preparation,    // Draw cards, reset mana
    Main,          // Play cards, move, attack, explore
    End,           // Discard hand, check victory conditions
    Night          // Night phase (if applicable)
}

public class TurnAction
{
    public int Id { get; set; }
    
    public int GameTurnId { get; set; }
    public GameTurn GameTurn { get; set; } = null!;
    
    public int PlayerId { get; set; }
    public GamePlayer Player { get; set; } = null!;
    
    public ActionType Type { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON data for complex actions
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int ActionSequence { get; set; }
    
    public bool IsUndoable { get; set; } = true;
}
