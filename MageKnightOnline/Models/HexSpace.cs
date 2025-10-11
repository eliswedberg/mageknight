using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MageKnightOnline.Models;

/// <summary>
/// A single playable space on the board according to Map_tile_rules.md
/// Each HexSpace represents one hex in the hex-based world builder
/// </summary>
public class HexSpace
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier (e.g. hex_3_5_A)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string HexId { get; set; } = string.Empty;
    
    /// <summary>
    /// Axial hex coordinates (map-level) - q coordinate
    /// </summary>
    public int Q { get; set; }
    
    /// <summary>
    /// Axial hex coordinates (map-level) - r coordinate
    /// </summary>
    public int R { get; set; }
    
    /// <summary>
    /// Terrain type of this hex space
    /// </summary>
    public TerrainType TerrainType { get; set; }
    
    /// <summary>
    /// Optional reference to a map site (village, monastery, ruin, etc.)
    /// </summary>
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    
    /// <summary>
    /// Can hold hero, unit, rampaging enemy, etc.
    /// This is a JSON field to store occupant data flexibly
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? OccupantData { get; set; }
    
    /// <summary>
    /// Derived from terrain + day/night conditions
    /// This is calculated, not stored directly
    /// </summary>
    [NotMapped]
    public bool IsAccessible { get; set; }
    
    /// <summary>
    /// Reference to the MapTileNew this HexSpace belongs to
    /// </summary>
    public int MapTileId { get; set; }
    public MapTileNew MapTile { get; set; } = null!;
    
    /// <summary>
    /// Position within the tile (0-6, where 0 is center and 1-6 are the surrounding hexes)
    /// </summary>
    public int PositionInTile { get; set; }
    
    /// <summary>
    /// Movement cost for entering this hex (derived from terrain)
    /// </summary>
    public int MovementCost { get; set; } = 1;
    
    /// <summary>
    /// Whether this hex space is currently revealed/visible
    /// </summary>
    public bool IsRevealed { get; set; } = false;
    
    /// <summary>
    /// Whether this hex space has been explored by a player
    /// </summary>
    public bool IsExplored { get; set; } = false;
    
    /// <summary>
    /// Game session this hex space belongs to
    /// </summary>
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
}

