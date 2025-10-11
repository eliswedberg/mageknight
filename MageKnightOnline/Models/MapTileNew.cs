using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MageKnightOnline.Models;

/// <summary>
/// A composite of 7 HexSpaces arranged in a fixed pattern according to Map_tile_rules.md
/// Each MapTile contains exactly 7 interactive hexes (hex spaces)
/// </summary>
public class MapTileNew
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Unique tile reference (e.g. COUNTRYSIDE_04, STARTING_A, CORE_CITY_01)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TileId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of tile determining placement rules and deck
    /// </summary>
    public MapTileType TileType { get; set; }
    
    /// <summary>
    /// The 7 internal hex spaces of the tile
    /// Position 0 is center, positions 1-6 are surrounding hexes
    /// </summary>
    public ICollection<HexSpace> Hexes { get; set; } = new List<HexSpace>();
    
    /// <summary>
    /// Tile center coordinates in map space (axial coordinates)
    /// </summary>
    public int CenterQ { get; set; }
    public int CenterR { get; set; }
    
    /// <summary>
    /// Always the same as the starting tile (rotation is locked)
    /// 0 = no rotation, 60 = 60 degrees, etc.
    /// </summary>
    public int Orientation { get; set; } = 0;
    
    /// <summary>
    /// List of neighboring tile IDs (stored as JSON for flexibility)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string AdjacentTileIds { get; set; } = "[]";
    
    /// <summary>
    /// Whether the tile is currently visible/revealed
    /// </summary>
    public bool IsRevealed { get; set; } = false;
    
    /// <summary>
    /// Whether the tile has been placed on the map
    /// </summary>
    public bool IsPlaced { get; set; } = false;
    
    /// <summary>
    /// Game session this tile belongs to
    /// </summary>
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    /// <summary>
    /// Image name for rendering this tile
    /// </summary>
    [MaxLength(100)]
    public string ImageName { get; set; } = string.Empty;
    
    /// <summary>
    /// Back color of the tile (determines which deck it comes from)
    /// </summary>
    public TileBackColor BackColor { get; set; }
    
    /// <summary>
    /// Whether this is a city tile (spawns City object when revealed)
    /// </summary>
    public bool IsCity { get; set; } = false;
    
    /// <summary>
    /// City level if this is a city tile
    /// </summary>
    public int? CityLevel { get; set; }
    
    /// <summary>
    /// City color if this is a city tile
    /// </summary>
    [MaxLength(20)]
    public string? CityColor { get; set; }
    
    /// <summary>
    /// Whether this tile has been used/placed in the current game
    /// </summary>
    public bool IsUsed { get; set; } = false;
    
    /// <summary>
    /// Order in which this tile was placed (for tracking placement sequence)
    /// </summary>
    public int? PlacementOrder { get; set; }
    
    /// <summary>
    /// Validation data for tile placement rules
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string PlacementValidationData { get; set; } = "{}";
}

/// <summary>
/// Types of map tiles determining placement rules and deck
/// </summary>
public enum MapTileType
{
    Starting,      // Always in play, defines base orientation (A or B layout)
    Countryside,   // Green back, early exploration
    CoreNonCity,   // Brown back, late exploration
    CoreCity       // Brown back + City symbol, endgame zones
}

/// <summary>
/// Back colors of tiles determining which deck they come from
/// </summary>
public enum TileBackColor
{
    Neutral,  // Starting tiles
    Green,    // Countryside tiles
    Brown     // Core tiles (both city and non-city)
}
