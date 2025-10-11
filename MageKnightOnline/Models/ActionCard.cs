using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MageKnightOnline.Models;

/// <summary>
/// Action Card model enligt actions.json schema
/// </summary>
public class ActionCard
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique card identifier
    /// </summary>
    [MaxLength(50)]
    public string CardId { get; set; } = string.Empty;

    /// <summary>
    /// Card name
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Card type: basic or advanced
    /// </summary>
    public ActionCardType Type { get; set; }

    /// <summary>
    /// Card color: red, blue, white, green, or none
    /// </summary>
    public ActionCardColor Color { get; set; }

    /// <summary>
    /// Card effects stored as JSON array
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Effects { get; set; } = "[]";

    /// <summary>
    /// Whether strong effect requires mana
    /// </summary>
    public bool StrongRequiresMana { get; set; }

    /// <summary>
    /// Game session this card belongs to (for player decks)
    /// </summary>
    public int? GameSessionId { get; set; }
    public GameSession? GameSession { get; set; }

    /// <summary>
    /// Player who owns this card (for player decks)
    /// </summary>
    public int? PlayerId { get; set; }
    public GamePlayer? Player { get; set; }

    /// <summary>
    /// Card location: deck, hand, discard, offer
    /// </summary>
    public CardLocation Location { get; set; } = CardLocation.Deck;

    /// <summary>
    /// Position in deck/hand/discard
    /// </summary>
    public int Position { get; set; } = 0;

    /// <summary>
    /// Whether card is currently played
    /// </summary>
    public bool IsPlayed { get; set; } = false;

    /// <summary>
    /// Whether card is played sideways (for effects)
    /// </summary>
    public bool IsPlayedSideways { get; set; } = false;

    /// <summary>
    /// Whether strong effect is being used
    /// </summary>
    public bool UsingStrongEffect { get; set; } = false;

    /// <summary>
    /// Mana used for strong effect
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ManaUsed { get; set; } = "{}"; // JSON object with mana colors and amounts
}

/// <summary>
/// Action card types
/// </summary>
public enum ActionCardType
{
    Basic,
    Advanced
}

/// <summary>
/// Action card colors
/// </summary>
public enum ActionCardColor
{
    None,
    Red,
    Blue,
    White,
    Green
}

/// <summary>
/// Card locations in the game
/// </summary>
public enum CardLocation
{
    Deck,
    Hand,
    Discard,
    Offer,
    Played
}

/// <summary>
/// Card effect model
/// </summary>
public class CardEffect
{
    /// <summary>
    /// Phase when effect applies
    /// </summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Effect description
    /// </summary>
    public string Effect { get; set; } = string.Empty;

    /// <summary>
    /// Effect values
    /// </summary>
    public EffectValues Values { get; set; } = new();

    /// <summary>
    /// Element type
    /// </summary>
    public ElementType Element { get; set; } = ElementType.Physical;
}

/// <summary>
/// Effect values
/// </summary>
public class EffectValues
{
    public int Move { get; set; } = 0;
    public int Influence { get; set; } = 0;
    public int Attack { get; set; } = 0;
    public int Block { get; set; } = 0;
}

/// <summary>
/// Element types
/// </summary>
public enum ElementType
{
    Physical,
    Fire,
    Ice,
    ColdFire,
    Null
}
