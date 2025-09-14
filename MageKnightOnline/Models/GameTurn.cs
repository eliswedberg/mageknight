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
    
    // Action Point System
    public int ActionPoints { get; set; } = 0;
    public int MaxActionPoints { get; set; } = 0;
    public int UsedActionPoints { get; set; } = 0;
    
    public int MovementPoints { get; set; } = 0;
    public int MaxMovementPoints { get; set; } = 0;
    public int UsedMovementPoints { get; set; } = 0;
    
    public int InfluencePoints { get; set; } = 0;
    public int MaxInfluencePoints { get; set; } = 0;
    public int UsedInfluencePoints { get; set; } = 0;
    
    public int AttackPoints { get; set; } = 0;
    public int MaxAttackPoints { get; set; } = 0;
    public int UsedAttackPoints { get; set; } = 0;
    
    public int BlockPoints { get; set; } = 0;
    public int MaxBlockPoints { get; set; } = 0;
    public int UsedBlockPoints { get; set; } = 0;
    
    public bool HasPassed { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    
    public ICollection<TurnAction> Actions { get; set; } = new List<TurnAction>();
}

public enum TurnPhase
{
    Preparation,    // Draw cards, reset mana
    Main,          // Play cards, move, attack, explore
    Combat,        // Combat resolution
    EndOfTurn,     // End of turn cleanup
    Completed,     // Turn is fully completed
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
    
    public int ActionPointsCost { get; set; } = 0;
    public int MovementPointsCost { get; set; } = 0;
    public int InfluencePointsCost { get; set; } = 0;
    public int AttackPointsCost { get; set; } = 0;
    public int BlockPointsCost { get; set; } = 0;
    
    public int? CardId { get; set; }
    public MageKnightCard? Card { get; set; }
    
    public string? Data { get; set; } // JSON data for complex actions
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int ActionSequence { get; set; }
    
    public bool IsUndoable { get; set; } = true;
    public bool IsResolved { get; set; } = false;
    public string? Result { get; set; }
}

