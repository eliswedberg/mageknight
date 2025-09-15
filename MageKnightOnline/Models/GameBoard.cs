using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Models;

public class GameBoard
{
    public int Id { get; set; }
    
    public int GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    
    public int Width { get; set; } = 7;
    public int Height { get; set; } = 7;
    
    public string MapType { get; set; } = "Standard"; // Standard, Volkaire, Krang, etc.
    
    public ICollection<BoardTile> Tiles { get; set; } = new List<BoardTile>();
    public ICollection<PlayerPosition> PlayerPositions { get; set; } = new List<PlayerPosition>();
    public ICollection<Site> Sites { get; set; } = new List<Site>();
}

public class BoardTile
{
    public int Id { get; set; }
    
    public int GameBoardId { get; set; }
    public GameBoard GameBoard { get; set; } = null!;
    
    public int X { get; set; }
    public int Y { get; set; }
    
    public TileType Type { get; set; }
    
    public bool IsExplored { get; set; } = false;
    
    public bool IsRevealed { get; set; } = false;
    
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    
    public string? Terrain { get; set; } // Forest, Mountain, Desert, etc.
    
    public int MovementCost { get; set; } = 1;
    
    public string? TileImageName { get; set; } // Name of the image file for this tile
    
    public TileEdgeData EdgeData { get; set; } = new TileEdgeData();
}

[Owned]
public class TileEdgeData
{
    // Each tile has 6 edges (hexagon), numbered clockwise from top
    // Edge 0 = Top, Edge 1 = Top-Right, Edge 2 = Bottom-Right, 
    // Edge 3 = Bottom, Edge 4 = Bottom-Left, Edge 5 = Top-Left
    public string[] EdgeSymbols { get; set; } = new string[6];
    public TerrainType[] EdgeTerrains { get; set; } = new TerrainType[6];
    
    public TileEdgeData()
    {
        // Initialize with empty symbols and unknown terrain
        for (int i = 0; i < 6; i++)
        {
            EdgeSymbols[i] = "";
            EdgeTerrains[i] = TerrainType.Unknown;
        }
    }
}

public enum TerrainType
{
    Unknown,
    Forest,
    Lake,
    Mountain,
    Desert,
    Village,
    Castle,
    Ruins,
    Mine,
    Grassland,
    Barren
}

public enum TileType
{
    Empty,
    Starting,       // Starting tile (A or B side)
    Countryside,    // Green back tiles (drawn randomly)
    Core,          // Brown back tiles (cities and non-cities)
    Forest,
    Mountain,
    Desert,
    Swamp,
    Water,
    Hills,
    Plains,
    Ruins,
    Dungeon,
    Keep,
    MageTower,
    Monastery,
    Village,
    City,
    Volkaire,
    Krang
}

public class PlayerPosition
{
    public int Id { get; set; }
    
    public int GameBoardId { get; set; }
    public GameBoard GameBoard { get; set; } = null!;
    
    public int PlayerId { get; set; }
    public GamePlayer Player { get; set; } = null!;
    
    public int X { get; set; }
    public int Y { get; set; }
    
    public DateTime MovedAt { get; set; } = DateTime.UtcNow;
    
    public int MovementPointsUsed { get; set; } = 0;
}
