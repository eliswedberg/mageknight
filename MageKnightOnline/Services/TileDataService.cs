using MageKnightOnline.Models;

namespace MageKnightOnline.Services;

public class TileDataService
{
    private readonly Dictionary<string, TileEdgeData> _tileEdgeData = new();

    public TileDataService()
    {
        InitializeTileData();
    }

    public TileEdgeData GetTileEdgeData(string tileId)
    {
        return _tileEdgeData.TryGetValue(tileId, out var data) ? data : new TileEdgeData();
    }

    private void InitializeTileData()
    {
        // Based on the images provided, mapping tile IDs to their edge symbols and terrains
        // Edge numbering: 0=Top, 1=Top-Right, 2=Bottom-Right, 3=Bottom, 4=Bottom-Left, 5=Top-Left

        // Starting tiles (A and B sides)
        _tileEdgeData["01-A"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland }
        };

        _tileEdgeData["01-B"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland }
        };

        // Countryside tiles (01-1 to 01-11)
        _tileEdgeData["01-1"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland }
        };

        _tileEdgeData["01-2"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain }
        };

        _tileEdgeData["01-3"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Forest, TerrainType.Forest, TerrainType.Forest, TerrainType.Forest, TerrainType.Forest, TerrainType.Forest }
        };

        _tileEdgeData["01-4"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Lake, TerrainType.Lake, TerrainType.Lake, TerrainType.Lake, TerrainType.Lake, TerrainType.Lake }
        };

        _tileEdgeData["01-5"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Village, TerrainType.Village, TerrainType.Village, TerrainType.Village, TerrainType.Village, TerrainType.Village }
        };

        _tileEdgeData["01-6"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Castle, TerrainType.Castle, TerrainType.Castle, TerrainType.Castle, TerrainType.Castle, TerrainType.Castle }
        };

        _tileEdgeData["01-7"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins }
        };

        _tileEdgeData["01-8"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Mine, TerrainType.Mine, TerrainType.Mine, TerrainType.Mine, TerrainType.Mine, TerrainType.Mine }
        };

        _tileEdgeData["01-9"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Desert, TerrainType.Desert, TerrainType.Desert, TerrainType.Desert, TerrainType.Desert, TerrainType.Desert }
        };

        _tileEdgeData["01-10"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Barren, TerrainType.Barren, TerrainType.Barren, TerrainType.Barren, TerrainType.Barren, TerrainType.Barren }
        };

        _tileEdgeData["01-11"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "triangle", "triangle", "triangle", "triangle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland, TerrainType.Grassland }
        };

        // Core tiles (02-1 to 02-8) - these have more complex edge patterns
        _tileEdgeData["02-1"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "star", "triangle", "circle", "triangle", "star", "triangle" },
            EdgeTerrains = new[] { TerrainType.Castle, TerrainType.Castle, TerrainType.Castle, TerrainType.Castle, TerrainType.Castle, TerrainType.Castle }
        };

        _tileEdgeData["02-2"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "star", "triangle", "circle", "triangle", "star" },
            EdgeTerrains = new[] { TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain, TerrainType.Mountain }
        };

        _tileEdgeData["02-3"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "circle", "triangle", "star", "triangle", "circle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Forest, TerrainType.Forest, TerrainType.Forest, TerrainType.Forest, TerrainType.Forest, TerrainType.Forest }
        };

        _tileEdgeData["02-4"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "circle", "triangle", "star", "triangle", "circle" },
            EdgeTerrains = new[] { TerrainType.Lake, TerrainType.Lake, TerrainType.Lake, TerrainType.Lake, TerrainType.Lake, TerrainType.Lake }
        };

        _tileEdgeData["02-5"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "star", "triangle", "circle", "triangle", "star", "triangle" },
            EdgeTerrains = new[] { TerrainType.Village, TerrainType.Village, TerrainType.Village, TerrainType.Village, TerrainType.Village, TerrainType.Village }
        };

        _tileEdgeData["02-6"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "star", "triangle", "circle", "triangle", "star" },
            EdgeTerrains = new[] { TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins, TerrainType.Ruins }
        };

        _tileEdgeData["02-7"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "circle", "triangle", "star", "triangle", "circle", "triangle" },
            EdgeTerrains = new[] { TerrainType.Mine, TerrainType.Mine, TerrainType.Mine, TerrainType.Mine, TerrainType.Mine, TerrainType.Mine }
        };

        _tileEdgeData["02-8"] = new TileEdgeData
        {
            EdgeSymbols = new[] { "triangle", "circle", "triangle", "star", "triangle", "circle" },
            EdgeTerrains = new[] { TerrainType.Desert, TerrainType.Desert, TerrainType.Desert, TerrainType.Desert, TerrainType.Desert, TerrainType.Desert }
        };
    }

    public bool DoTilesMatchAtContact(string tileId1, int edge1, string tileId2, int edge2)
    {
        var data1 = GetTileEdgeData(tileId1);
        var data2 = GetTileEdgeData(tileId2);

        // Check if symbols match
        var symbolMatch = data1.EdgeSymbols[edge1] == data2.EdgeSymbols[edge2];
        
        // Check if terrain types match
        var terrainMatch = data1.EdgeTerrains[edge1] == data2.EdgeTerrains[edge2];

        return symbolMatch && terrainMatch;
    }

    public int GetOppositeEdge(int edge)
    {
        // In a hexagon, opposite edges are 3 positions away (mod 6)
        return (edge + 3) % 6;
    }
}

