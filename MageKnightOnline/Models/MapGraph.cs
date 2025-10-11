using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MageKnightOnline.Models;

/// <summary>
/// The overall game map according to Map_tile_rules.md
/// Manages all placed tiles and their relationships
/// </summary>
public class MapGraph
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Game session this map belongs to
    /// </summary>
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    /// <summary>
    /// All placed tiles in the map
    /// </summary>
    public ICollection<MapTileNew> Tiles { get; set; } = new List<MapTileNew>();
    
    /// <summary>
    /// Adjacency links between tiles (stored as JSON for flexibility)
    /// Format: [["tileId1", "tileId2"], ["tileId2", "tileId3"], ...]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Edges { get; set; } = "[]";
    
    /// <summary>
    /// Coastline mask defining restricted placement zones (from scenario)
    /// Stored as JSON array of coordinates that are restricted
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string CoastlineMask { get; set; } = "[]";
    
    /// <summary>
    /// Scenario being played (determines starting tile and rules)
    /// </summary>
    [MaxLength(50)]
    public string ScenarioId { get; set; } = string.Empty;
    
    /// <summary>
    /// Starting tile layout (A or B)
    /// </summary>
    [MaxLength(1)]
    public string StartingLayout { get; set; } = "A";
    
    /// <summary>
    /// Current exploration phase
    /// </summary>
    public ExplorationPhase CurrentPhase { get; set; } = ExplorationPhase.Countryside;
    
    /// <summary>
    /// Number of countryside tiles remaining in deck
    /// </summary>
    public int CountrysideTilesRemaining { get; set; } = 0;
    
    /// <summary>
    /// Number of core non-city tiles remaining in deck
    /// </summary>
    public int CoreNonCityTilesRemaining { get; set; } = 0;
    
    /// <summary>
    /// Number of core city tiles remaining in deck
    /// </summary>
    public int CoreCityTilesRemaining { get; set; } = 0;
    
    /// <summary>
    /// All cities that have been revealed
    /// </summary>
    public ICollection<City> Cities { get; set; } = new List<City>();
    
    /// <summary>
    /// All sites on the map (villages, monasteries, etc.)
    /// TODO: Implement proper site management
    /// </summary>
    // public ICollection<Site> Sites { get; set; } = new List<Site>();
    
    /// <summary>
    /// Current turn number
    /// </summary>
    public int CurrentTurn { get; set; } = 1;
    
    /// <summary>
    /// Current round number
    /// </summary>
    public int CurrentRound { get; set; } = 1;
    
    /// <summary>
    /// Whether the map has been initialized with starting tile
    /// </summary>
    public bool IsInitialized { get; set; } = false;
    
    /// <summary>
    /// Map configuration data (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ConfigurationData { get; set; } = "{}";
}

/// <summary>
/// Exploration phases determining which deck to draw from
/// </summary>
public enum ExplorationPhase
{
    Countryside,  // Drawing from countryside deck
    Core          // Drawing from core deck (both city and non-city)
}

/// <summary>
/// City object spawned when a Core City tile is revealed
/// </summary>
public class City
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the MapTileNew that spawned this city
    /// </summary>
    public int MapTileId { get; set; }
    public MapTileNew MapTile { get; set; } = null!;
    
    /// <summary>
    /// City level from scenario definition
    /// </summary>
    public int Level { get; set; }
    
    /// <summary>
    /// City color (red, blue, white, green)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Color { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the city has been conquered
    /// </summary>
    public bool IsConquered { get; set; } = false;
    
    /// <summary>
    /// Player who conquered the city (if any)
    /// </summary>
    public int? ConqueredByPlayerId { get; set; }
    public GamePlayer? ConqueredByPlayer { get; set; }
    
    /// <summary>
    /// Turn when the city was conquered
    /// </summary>
    public int? ConqueredOnTurn { get; set; }
    
    /// <summary>
    /// City defenders (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Defenders { get; set; } = "[]";
    
    /// <summary>
    /// City rewards (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Rewards { get; set; } = "[]";
    
    /// <summary>
    /// Game session this city belongs to
    /// </summary>
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
}
