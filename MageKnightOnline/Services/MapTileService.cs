using MageKnightOnline.Models;
using MageKnightOnline.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MageKnightOnline.Services;

/// <summary>
/// Service for managing map tile placement and exploration according to Map_tile_rules.md
/// </summary>
public class MapTileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MapTileService> _logger;
    private readonly SiteService _siteService;

    public MapTileService(ApplicationDbContext context, ILogger<MapTileService> logger, SiteService siteService)
    {
        _context = context;
        _logger = logger;
        _siteService = siteService;
    }

    /// <summary>
    /// Initialize the map with a starting tile
    /// </summary>
    public async Task<bool> InitializeMapAsync(int gameSessionId, string scenarioId, string startingLayout = "A")
    {
        try
        {
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);

            if (mapGraph == null)
            {
                mapGraph = new MapGraph
                {
                    GameSessionId = gameSessionId,
                    ScenarioId = scenarioId,
                    StartingLayout = startingLayout,
                    IsInitialized = false
                };
                _context.MapGraphs.Add(mapGraph);
            }

            if (mapGraph.IsInitialized)
            {
                _logger.LogWarning("Map already initialized for game session {GameSessionId}", gameSessionId);
                return false;
            }

            // Create starting tile
            var startingTile = await CreateStartingTileAsync(gameSessionId, startingLayout);
            if (startingTile == null)
            {
                _logger.LogError("Failed to create starting tile for game session {GameSessionId}", gameSessionId);
                return false;
            }

            // Place starting tile at origin (0,0)
            startingTile.CenterQ = 0;
            startingTile.CenterR = 0;
            startingTile.IsPlaced = true;
            startingTile.IsRevealed = true;
            startingTile.PlacementOrder = 1;

            // Add tile to map first
            mapGraph.Tiles.Add(startingTile);

            // Save the map graph to get the tile ID before creating hex spaces
            await _context.SaveChangesAsync();

            // Create 7 hex spaces for the starting tile (now that it has an ID)
            await CreateHexSpacesForTileAsync(startingTile);

            mapGraph.IsInitialized = true;
            mapGraph.CurrentPhase = ExplorationPhase.Countryside;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Map initialized for game session {GameSessionId} with starting tile {TileId}", 
                gameSessionId, startingTile.TileId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing map for game session {GameSessionId}", gameSessionId);
            return false;
        }
    }

    /// <summary>
    /// Check if a player can explore (reveal) a new tile
    /// </summary>
    public async Task<bool> CanExploreAsync(int gameSessionId, int playerId, int centerQ, int centerR)
    {
        try
        {
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);

            if (mapGraph == null || !mapGraph.IsInitialized)
            {
                return false;
            }

            // Check if position is valid for exploration
            if (!await IsValidExplorationPositionAsync(mapGraph, centerQ, centerR))
            {
                return false;
            }

            // Check if player has enough move points (this would need to be implemented in turn management)
            // For now, assume player can explore if they have 2+ move points
            // TODO: Integrate with turn management system

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking exploration possibility for game session {GameSessionId}", gameSessionId);
            return false;
        }
    }

    /// <summary>
    /// Explore (reveal) a new tile at the specified position
    /// </summary>
    public async Task<MapTileNew?> ExploreTileAsync(int gameSessionId, int playerId, int centerQ, int centerR)
    {
        try
        {
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);

            if (mapGraph == null || !mapGraph.IsInitialized)
            {
                _logger.LogError("Map not initialized for game session {GameSessionId}", gameSessionId);
                return null;
            }

            // Validate exploration
            if (!await CanExploreAsync(gameSessionId, playerId, centerQ, centerR))
            {
                _logger.LogWarning("Invalid exploration attempt for game session {GameSessionId} at ({Q}, {R})", 
                    gameSessionId, centerQ, centerR);
                return null;
            }

            // Draw tile from appropriate deck
            var newTile = await DrawTileFromDeckAsync(mapGraph);
            if (newTile == null)
            {
                _logger.LogError("No tiles available to draw for game session {GameSessionId}", gameSessionId);
                return null;
            }

            // Place the tile
            newTile.CenterQ = centerQ;
            newTile.CenterR = centerR;
            newTile.IsPlaced = true;
            newTile.IsRevealed = true;
            newTile.PlacementOrder = mapGraph.Tiles.Count + 1;

            // Validate placement according to rules
            if (!await ValidateTilePlacementAsync(newTile, mapGraph))
            {
                _logger.LogWarning("Tile placement validation failed for tile {TileId} at ({Q}, {R})", 
                    newTile.TileId, centerQ, centerR);
                return null;
            }

            // Add tile to map first
            mapGraph.Tiles.Add(newTile);

            // Create hex spaces for the new tile (now that it has an ID)
            await CreateHexSpacesForTileAsync(newTile);

            // Update adjacency relationships
            await UpdateTileAdjacencyAsync(newTile, mapGraph);

            // Trigger reveal effects
            await TriggerRevealEffectsAsync(newTile, mapGraph);

            // Update deck counts
            await UpdateDeckCountsAsync(mapGraph, newTile.TileType);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully explored tile {TileId} at ({Q}, {R}) for game session {GameSessionId}", 
                newTile.TileId, centerQ, centerR, gameSessionId);

            return newTile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exploring tile for game session {GameSessionId} at ({Q}, {R})", 
                gameSessionId, centerQ, centerR);
            return null;
        }
    }

    /// <summary>
    /// Get all valid exploration positions for a player
    /// </summary>
    public async Task<List<(int q, int r)>> GetValidExplorationPositionsAsync(int gameSessionId)
    {
        try
        {
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);

            if (mapGraph == null || !mapGraph.IsInitialized)
            {
                return new List<(int q, int r)>();
            }

            var validPositions = new List<(int q, int r)>();

            // Find all positions that border existing tiles
            foreach (var tile in mapGraph.Tiles.Where(t => t.IsPlaced))
            {
                // Check all 6 directions around this tile
                var directions = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
                
                foreach (var (dq, dr) in directions)
                {
                    var candidateQ = tile.CenterQ + dq;
                    var candidateR = tile.CenterR + dr;

                    if (await IsValidExplorationPositionAsync(mapGraph, candidateQ, candidateR))
                    {
                        validPositions.Add((candidateQ, candidateR));
                    }
                }
            }

            return validPositions.Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid exploration positions for game session {GameSessionId}", gameSessionId);
            return new List<(int q, int r)>();
        }
    }

    #region Private Helper Methods

    private async Task<MapTileNew?> CreateStartingTileAsync(int gameSessionId, string layout)
    {
        var startingTile = new MapTileNew
        {
            TileId = $"STARTING_{layout}",
            TileType = MapTileType.Starting,
            BackColor = TileBackColor.Neutral,
            GameSessionId = gameSessionId,
            ImageName = $"MK_starting_tile_{layout.ToLower()}",
            IsCity = false,
            IsUsed = true
        };

        _context.MapTileNews.Add(startingTile);
        
        // Save the tile immediately to get its ID
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created starting tile {TileId} with ID {Id}", startingTile.TileId, startingTile.Id);
        
        return startingTile;
    }

    private async Task<MapTileNew?> DrawTileFromDeckAsync(MapGraph mapGraph)
    {
        // Determine which deck to draw from based on current phase and remaining tiles
        MapTileType tileType;
        TileBackColor backColor;

        if (mapGraph.CurrentPhase == ExplorationPhase.Countryside && mapGraph.CountrysideTilesRemaining > 0)
        {
            tileType = MapTileType.Countryside;
            backColor = TileBackColor.Green;
        }
        else if (mapGraph.CoreNonCityTilesRemaining > 0)
        {
            tileType = MapTileType.CoreNonCity;
            backColor = TileBackColor.Brown;
        }
        else if (mapGraph.CoreCityTilesRemaining > 0)
        {
            tileType = MapTileType.CoreCity;
            backColor = TileBackColor.Brown;
        }
        else
        {
            _logger.LogWarning("No tiles available in any deck for game session {GameSessionId}", mapGraph.GameSessionId);
            return null;
        }

        // For now, create a placeholder tile - in a real implementation, this would draw from a shuffled deck
        var tileNumber = Random.Shared.Next(1, 100); // Placeholder
        var newTile = new MapTileNew
        {
            TileId = $"{tileType.ToString().ToUpper()}_{tileNumber:D2}",
            TileType = tileType,
            BackColor = backColor,
            GameSessionId = mapGraph.GameSessionId,
            ImageName = $"MK_map_tile_{tileNumber:D2}",
            IsCity = tileType == MapTileType.CoreCity,
            IsUsed = true
        };

        if (newTile.IsCity)
        {
            newTile.CityLevel = Random.Shared.Next(1, 4); // Placeholder
            newTile.CityColor = new[] { "red", "blue", "white", "green" }[Random.Shared.Next(4)];
        }

        _context.MapTileNews.Add(newTile);
        
        // Save the tile immediately to get its ID
        await _context.SaveChangesAsync();
        
        return newTile;
    }

    private async Task CreateHexSpacesForTileAsync(MapTileNew tile)
    {
        // Ensure the tile has been saved and has an ID
        _logger.LogInformation("Creating hex spaces for tile {TileId} with ID {Id}", tile.TileId, tile.Id);
        if (tile.Id == 0)
        {
            _logger.LogError("Cannot create hex spaces for tile {TileId} - tile has not been saved to database", tile.TileId);
            return;
        }

        // Load tile data from JSON if it's a starting tile
        var tileData = await LoadTileDataAsync(tile.TileId);
        
        // Create 7 hex spaces: 1 center + 6 surrounding
        var hexSpaces = new List<HexSpace>();

        // Center hex (position 0)
        var centerHexData = tileData?.HexSpaces?.FirstOrDefault(h => h.Position == 0);
        hexSpaces.Add(new HexSpace
        {
            HexId = $"{tile.TileId}_center",
            Q = tile.CenterQ,
            R = tile.CenterR,
            TerrainType = ParseTerrainType(centerHexData?.TerrainType) ?? TerrainType.Grassland,
            MapTileId = tile.Id,
            PositionInTile = 0,
            IsRevealed = tile.IsRevealed,
            GameSessionId = tile.GameSessionId,
            OccupantData = centerHexData?.IsPortal == true ? "{\"type\":\"portal\",\"portalType\":\"" + centerHexData.PortalType + "\"}" : null
        });

        // 6 surrounding hexes (positions 1-6)
        var directions = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
        for (int i = 0; i < 6; i++)
        {
            var (dq, dr) = directions[i];
            var hexData = tileData?.HexSpaces?.FirstOrDefault(h => h.Position == i + 1);
            hexSpaces.Add(new HexSpace
            {
                HexId = $"{tile.TileId}_hex_{i + 1}",
                Q = tile.CenterQ + dq,
                R = tile.CenterR + dr,
                TerrainType = ParseTerrainType(hexData?.TerrainType) ?? TerrainType.Grassland,
                MapTileId = tile.Id,
                PositionInTile = i + 1,
                IsRevealed = tile.IsRevealed,
                GameSessionId = tile.GameSessionId,
                OccupantData = hexData?.IsPortal == true ? "{\"type\":\"portal\",\"portalType\":\"" + hexData.PortalType + "\"}" : null
            });
        }

        _context.HexSpaces.AddRange(hexSpaces);
    }

    private async Task<bool> IsValidExplorationPositionAsync(MapGraph mapGraph, int centerQ, int centerR)
    {
        // Check if position is already occupied
        if (mapGraph.Tiles.Any(t => t.IsPlaced && t.CenterQ == centerQ && t.CenterR == centerR))
        {
            return false;
        }

        // Check coastline mask (if implemented)
        // TODO: Implement coastline mask validation

        return true;
    }

    private async Task<bool> ValidateTilePlacementAsync(MapTileNew tile, MapGraph mapGraph)
    {
        // Must be adjacent to at least 2 other placed tiles, OR
        // Adjacent to a tile that itself is adjacent to ≥2 others
        var adjacentTiles = mapGraph.Tiles.Where(t => t.IsPlaced && 
            IsAdjacent(tile.CenterQ, tile.CenterR, t.CenterQ, t.CenterR)).ToList();

        if (adjacentTiles.Count >= 2)
        {
            return true;
        }

        // Check if adjacent to a tile that touches ≥2 others
        foreach (var adjacentTile in adjacentTiles)
        {
            var adjacentToAdjacent = mapGraph.Tiles.Where(t => t.IsPlaced && t.Id != adjacentTile.Id &&
                IsAdjacent(adjacentTile.CenterQ, adjacentTile.CenterR, t.CenterQ, t.CenterR)).ToList();
            
            if (adjacentToAdjacent.Count >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAdjacent(int q1, int r1, int q2, int r2)
    {
        // Two tiles are adjacent if their centers are at distance 2 in hex coordinates
        var distance = (Math.Abs(q1 - q2) + Math.Abs(q1 + r1 - q2 - r2) + Math.Abs(r1 - r2)) / 2;
        return distance == 2;
    }

    private async Task UpdateTileAdjacencyAsync(MapTileNew newTile, MapGraph mapGraph)
    {
        var adjacentTileIds = new List<string>();
        
        foreach (var existingTile in mapGraph.Tiles.Where(t => t.IsPlaced))
        {
            if (IsAdjacent(newTile.CenterQ, newTile.CenterR, existingTile.CenterQ, existingTile.CenterR))
            {
                adjacentTileIds.Add(existingTile.TileId);
            }
        }

        newTile.AdjacentTileIds = JsonSerializer.Serialize(adjacentTileIds);
    }

    private async Task TriggerRevealEffectsAsync(MapTileNew tile, MapGraph mapGraph)
    {
        // Create sites for hex spaces that should have sites
        await CreateSitesForTileAsync(tile);

        // If City Tile, create City object
        if (tile.IsCity)
        {
            var city = new City
            {
                MapTileId = tile.Id,
                Level = tile.CityLevel ?? 1,
                Color = tile.CityColor ?? "red",
                GameSessionId = tile.GameSessionId,
                Defenders = "[]", // TODO: Set based on city level and color
                Rewards = "[]"    // TODO: Set based on city level and color
            };

            _context.Cities.Add(city);
            mapGraph.Cities.Add(city);
        }
    }

    private async Task CreateSitesForTileAsync(MapTileNew tile)
    {
        try
        {
            // Get hex spaces for this tile
            var hexSpaces = await _context.HexSpaces
                .Where(hs => hs.MapTileId == tile.Id)
                .ToListAsync();

            // For now, randomly assign sites to some hex spaces
            // In a real implementation, this would be based on tile data
            var siteTypes = new[] { "village", "monastery", "keep", "mage_tower", "ruins" };
            var random = new Random();

            foreach (var hexSpace in hexSpaces)
            {
                // 30% chance of having a site on each hex space
                if (random.NextDouble() < 0.3)
                {
                    var siteType = siteTypes[random.Next(siteTypes.Length)];
                    var site = await _siteService.CreateSiteAsync(tile.GameSessionId, siteType, hexSpace.Id);
                    
                    if (site != null)
                    {
                        // Reveal the site immediately when the tile is revealed
                        await _siteService.RevealSiteAsync(site.Id, 0, 1); // TODO: Get actual player and turn
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sites for tile {TileId}", tile.Id);
        }
    }

    private async Task UpdateDeckCountsAsync(MapGraph mapGraph, MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Countryside:
                mapGraph.CountrysideTilesRemaining = Math.Max(0, mapGraph.CountrysideTilesRemaining - 1);
                if (mapGraph.CountrysideTilesRemaining == 0)
                {
                    mapGraph.CurrentPhase = ExplorationPhase.Core;
                }
                break;
            case MapTileType.CoreNonCity:
                mapGraph.CoreNonCityTilesRemaining = Math.Max(0, mapGraph.CoreNonCityTilesRemaining - 1);
                break;
            case MapTileType.CoreCity:
                mapGraph.CoreCityTilesRemaining = Math.Max(0, mapGraph.CoreCityTilesRemaining - 1);
                break;
        }
    }

    private TerrainType? ParseTerrainType(string? terrainTypeString)
    {
        if (string.IsNullOrEmpty(terrainTypeString))
            return null;

        return terrainTypeString.ToLower() switch
        {
            "grassland" => TerrainType.Grassland,
            "forest" => TerrainType.Forest,
            "hills" => TerrainType.Mountain, // Map hills to mountain
            "plains" => TerrainType.Grassland, // Map plains to grassland for now
            "desert" => TerrainType.Desert,
            "mountain" => TerrainType.Mountain,
            "lake" => TerrainType.Lake,
            "swamp" => TerrainType.Barren, // Map swamp to barren for now
            _ => TerrainType.Grassland
        };
    }

    private async Task<TileData?> LoadTileDataAsync(string tileId)
    {
        try
        {
            var jsonPath = Path.Combine("Data", "starting_tiles.json");
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("Starting tiles JSON file not found at {Path}", jsonPath);
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var tileDataCollection = JsonSerializer.Deserialize<TileDataCollection>(jsonContent);
            
            return tileDataCollection?.StartingTiles?.FirstOrDefault(t => t.TileId == tileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tile data for {TileId}", tileId);
            return null;
        }
    }

    /// <summary>
    /// Get the portal position for a starting tile
    /// </summary>
    public async Task<(int q, int r)?> GetPortalPositionAsync(int gameSessionId)
    {
        try
        {
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);

            if (mapGraph == null) return null;

            var startingTile = mapGraph.Tiles.FirstOrDefault(t => t.TileType == MapTileType.Starting);
            if (startingTile == null) return null;

            var tileData = await LoadTileDataAsync(startingTile.TileId);
            var portalHex = tileData?.HexSpaces?.FirstOrDefault(h => h.IsPortal);
            
            if (portalHex == null)
            {
                // Default to center if no portal found
                return (startingTile.CenterQ, startingTile.CenterR);
            }

            // Calculate portal position based on hex position
            if (portalHex.Position == 0)
            {
                // Center hex
                return (startingTile.CenterQ, startingTile.CenterR);
            }
            else
            {
                // Surrounding hex (positions 1-6)
                var directions = new[] { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };
                var (dq, dr) = directions[portalHex.Position - 1];
                return (startingTile.CenterQ + dq, startingTile.CenterR + dr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portal position for game session {GameSessionId}", gameSessionId);
            return null;
        }
    }

    #endregion
}

// Data models for tile configuration
public class TileDataCollection
{
    public List<TileData>? StartingTiles { get; set; }
}

public class TileData
{
    public string TileId { get; set; } = string.Empty;
    public string Layout { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public List<HexSpaceData>? HexSpaces { get; set; }
}

public class HexSpaceData
{
    public int Position { get; set; }
    public string TerrainType { get; set; } = string.Empty;
    public bool IsPortal { get; set; }
    public string? PortalType { get; set; }
}
