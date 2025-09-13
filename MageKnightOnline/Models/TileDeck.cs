using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class TileDeck
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public ICollection<MapTile> Tiles { get; set; } = new List<MapTile>();
}

public class MapTile
{
    public int Id { get; set; }
    
    public int? TileDeckId { get; set; }
    public TileDeck? TileDeck { get; set; }
    
    public string TileNumber { get; set; } = string.Empty; // e.g., "01-1", "02-3"
    public TileCategory Category { get; set; } // Countryside or Core
    public bool IsCity { get; set; } = false; // For Core tiles only
    public string ImageName { get; set; } = string.Empty; // e.g., "MK_map_tiles_01-1"
    
    public ICollection<TileTerrainSection> TerrainSections { get; set; } = new List<TileTerrainSection>();
    public ICollection<TileSite> Sites { get; set; } = new List<TileSite>();
    
    public bool IsUsed { get; set; } = false;
    public int? PlacedAtX { get; set; }
    public int? PlacedAtY { get; set; }
    public int Rotation { get; set; } = 0; // 0, 60, 120, 180, 240, 300 degrees
}

public enum TileCategory
{
    Countryside, // Green back
    Core        // Brown back
}

public class TileTerrainSection
{
    public int Id { get; set; }
    
    public int MapTileId { get; set; }
    public MapTile MapTile { get; set; } = null!;
    
    public int Section { get; set; } // 0-5 for hex sections
    public string TerrainType { get; set; } = string.Empty; // Forest, Mountain, Desert, etc.
    public int MovementCost { get; set; } = 1;
}

public class TileSite
{
    public int Id { get; set; }
    
    public int MapTileId { get; set; }
    public MapTile MapTile { get; set; } = null!;
    
    public int Section { get; set; } // Which hex section this site is in
    public SiteType SiteType { get; set; }
    public string Name { get; set; } = string.Empty;
}
