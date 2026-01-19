using System;
using System.Collections.Generic;
using System.Linq;

namespace MageKnightOnline.Core.GameEngine;

/// <summary>
/// Represents terrain data for a single hex on the map.
/// </summary>
public class TerrainHex
{
    /// <summary>The hex's position on the map</summary>
    public HexCoords Coords { get; set; }
    
    /// <summary>Terrain type (Plains, Forest, Hill, etc.)</summary>
    public string TerrainType { get; set; } = "Plains";
    
    /// <summary>ID of the Map Tile this hex belongs to</summary>
    public string TileId { get; set; } = string.Empty;
    
    /// <summary>Site on this hex (Village, Keep, etc.), null if none</summary>
    public string? SiteType { get; set; }
    
    /// <summary>Is this an edge hex of the map tile?</summary>
    public bool IsEdgeHex { get; set; }
    
    /// <summary>Local position within the tile (0 = center, 1-6 = edges)</summary>
    public int LocalIndex { get; set; }
}

/// <summary>
/// Manages Map Tile placement following Mage Knight's official rules.
/// 
/// A Map Tile consists of 7 hexes: 1 center hex + 6 surrounding hexes.
/// 
/// Placement Rules:
/// - Rule A: No overlap with existing hexes
/// - Rule B: Must be adjacent to existing map
/// - Rule C: Must share edges with AT LEAST 2 existing hexes (the "2-hex rule")
/// </summary>
public class MapPlacementManager
{
    #region Fields and Properties

    /// <summary>
    /// All hexes currently on the map, indexed by their coordinates.
    /// </summary>
    private readonly Dictionary<HexCoords, TerrainHex> _hexMap = new();

    /// <summary>
    /// Read-only access to the hex map.
    /// </summary>
    public IReadOnlyDictionary<HexCoords, TerrainHex> HexMap => _hexMap;

    /// <summary>
    /// Number of tiles currently placed.
    /// </summary>
    public int TileCount { get; private set; } = 0;

    /// <summary>
    /// Whether the map is empty (no tiles placed).
    /// </summary>
    public bool IsEmpty => _hexMap.Count == 0;

    #endregion

    #region Tile Shape Definition

    /// <summary>
    /// Returns the 7 local coordinates that make up a single Map Tile,
    /// relative to the tile's center hex (which is at 0,0).
    /// 
    /// Layout (pointy-top):
    ///       [1]   [2]
    ///     [6] [0] [3]
    ///       [5]   [4]
    /// 
    /// Index 0 = Center
    /// Index 1-6 = Edge hexes (clockwise from NE)
    /// </summary>
    public static HexCoords[] GetTileLocalCoords()
    {
        return new HexCoords[]
        {
            new(0, 0),   // [0] Center
            new(0, -1),  // [1] North (NW in pointy-top)
            new(1, -1),  // [2] Northeast
            new(1, 0),   // [3] East (SE in pointy-top)
            new(0, 1),   // [4] South (SE in pointy-top)
            new(-1, 1),  // [5] Southwest
            new(-1, 0),  // [6] West (NW in pointy-top)
        };
    }

    /// <summary>
    /// Get the absolute world coordinates for a tile's 7 hexes
    /// given the tile's center position.
    /// </summary>
    public static HexCoords[] GetTileWorldCoords(HexCoords tileCenter)
    {
        var localCoords = GetTileLocalCoords();
        var worldCoords = new HexCoords[7];
        
        for (int i = 0; i < 7; i++)
        {
            worldCoords[i] = tileCenter + localCoords[i];
        }
        
        return worldCoords;
    }

    /// <summary>
    /// Get only the 6 edge hexes of a tile (excludes center).
    /// </summary>
    public static HexCoords[] GetTileEdgeCoords(HexCoords tileCenter)
    {
        var localCoords = GetTileLocalCoords();
        var edgeCoords = new HexCoords[6];
        
        for (int i = 0; i < 6; i++)
        {
            edgeCoords[i] = tileCenter + localCoords[i + 1];
        }
        
        return edgeCoords;
    }

    #endregion

    #region Placement Validation

    /// <summary>
    /// Result of a placement validation check.
    /// </summary>
    public class PlacementValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public bool PassesOverlapRule { get; set; }
        public bool PassesAdjacencyRule { get; set; }
        public bool PassesTwoHexRule { get; set; }
        public int AdjacentExistingHexCount { get; set; }
        public List<HexCoords> OverlappingHexes { get; set; } = new();
        public List<HexCoords> AdjacentExistingHexes { get; set; } = new();

        public static PlacementValidationResult Valid(int adjacentCount, List<HexCoords> adjacentHexes) => new()
        {
            IsValid = true,
            PassesOverlapRule = true,
            PassesAdjacencyRule = true,
            PassesTwoHexRule = true,
            AdjacentExistingHexCount = adjacentCount,
            AdjacentExistingHexes = adjacentHexes
        };

        public static PlacementValidationResult Invalid(string message) => new()
        {
            IsValid = false,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// Validates if a Map Tile can be placed at the proposed center position.
    /// 
    /// Checks three rules:
    /// - Rule A (No Overlap): None of the 7 hexes can overlap existing hexes
    /// - Rule B (Adjacency): New tile must be adjacent to existing map
    /// - Rule C (2-Hex Rule): Must share edges with at least 2 existing hexes
    /// </summary>
    /// <param name="proposedTileCenter">The center hex coordinate for the new tile</param>
    /// <returns>Detailed validation result</returns>
    public PlacementValidationResult ValidatePlacement(HexCoords proposedTileCenter)
    {
        // Special case: First tile can be placed anywhere
        if (IsEmpty)
        {
            return PlacementValidationResult.Valid(0, new List<HexCoords>());
        }

        var tileCoords = GetTileWorldCoords(proposedTileCenter);
        var result = new PlacementValidationResult();

        // ============================================
        // RULE A: No Overlap Check
        // ============================================
        var overlappingHexes = new List<HexCoords>();
        foreach (var coord in tileCoords)
        {
            if (_hexMap.ContainsKey(coord))
            {
                overlappingHexes.Add(coord);
            }
        }

        result.PassesOverlapRule = overlappingHexes.Count == 0;
        result.OverlappingHexes = overlappingHexes;

        if (!result.PassesOverlapRule)
        {
            result.ErrorMessage = $"Tile overlaps {overlappingHexes.Count} existing hex(es) at: {string.Join(", ", overlappingHexes)}";
            return result;
        }

        // ============================================
        // RULE B & C: Adjacency and 2-Hex Rule
        // ============================================
        // Count how many UNIQUE existing hexes the new tile's hexes are adjacent to
        var adjacentExistingHexes = new HashSet<HexCoords>();

        foreach (var newHexCoord in tileCoords)
        {
            var neighbors = newHexCoord.GetAllNeighbors();
            foreach (var neighbor in neighbors)
            {
                // Check if this neighbor exists on the map AND is not part of the new tile
                if (_hexMap.ContainsKey(neighbor) && !tileCoords.Contains(neighbor))
                {
                    adjacentExistingHexes.Add(neighbor);
                }
            }
        }

        result.AdjacentExistingHexCount = adjacentExistingHexes.Count;
        result.AdjacentExistingHexes = adjacentExistingHexes.ToList();

        // Rule B: Must be adjacent to at least one existing hex
        result.PassesAdjacencyRule = adjacentExistingHexes.Count >= 1;
        
        // Rule C: Must be adjacent to at least TWO existing hexes
        result.PassesTwoHexRule = adjacentExistingHexes.Count >= 2;

        if (!result.PassesAdjacencyRule)
        {
            result.ErrorMessage = "Tile is not adjacent to the existing map (must touch at least 1 hex)";
            return result;
        }

        if (!result.PassesTwoHexRule)
        {
            result.ErrorMessage = $"Tile only touches {adjacentExistingHexes.Count} existing hex(es). The 2-hex rule requires touching at least 2 different hexes.";
            return result;
        }

        // All rules pass!
        result.IsValid = true;
        return result;
    }

    /// <summary>
    /// Simple boolean check for placement validity.
    /// </summary>
    public bool IsValidPlacement(HexCoords proposedTileCenter)
    {
        return ValidatePlacement(proposedTileCenter).IsValid;
    }

    #endregion

    #region Tile Placement

    /// <summary>
    /// Places a Map Tile at the specified center position.
    /// </summary>
    /// <param name="tileCenter">Center hex coordinate for the tile</param>
    /// <param name="tileId">Unique ID for this tile</param>
    /// <param name="terrainData">Array of 7 terrain types (center first, then edges)</param>
    /// <param name="siteData">Optional array of 7 site types (null for no site)</param>
    /// <param name="validateFirst">Whether to validate before placing (default true)</param>
    /// <returns>True if tile was placed successfully</returns>
    public bool PlaceTile(
        HexCoords tileCenter, 
        string tileId, 
        string[] terrainData, 
        string?[]? siteData = null,
        bool validateFirst = true)
    {
        if (terrainData.Length != 7)
            throw new ArgumentException("Terrain data must have exactly 7 elements", nameof(terrainData));

        if (validateFirst && !IsEmpty)
        {
            var validation = ValidatePlacement(tileCenter);
            if (!validation.IsValid)
                return false;
        }

        var tileCoords = GetTileWorldCoords(tileCenter);
        var localCoords = GetTileLocalCoords();

        for (int i = 0; i < 7; i++)
        {
            var hex = new TerrainHex
            {
                Coords = tileCoords[i],
                TerrainType = terrainData[i],
                TileId = tileId,
                SiteType = siteData?[i],
                IsEdgeHex = i > 0, // Index 0 is center
                LocalIndex = i
            };

            _hexMap[tileCoords[i]] = hex;
        }

        TileCount++;
        return true;
    }

    /// <summary>
    /// Removes a tile from the map by its tile ID.
    /// </summary>
    public bool RemoveTile(string tileId)
    {
        var hexesToRemove = _hexMap
            .Where(kvp => kvp.Value.TileId == tileId)
            .Select(kvp => kvp.Key)
            .ToList();

        if (hexesToRemove.Count == 0)
            return false;

        foreach (var coord in hexesToRemove)
        {
            _hexMap.Remove(coord);
        }

        TileCount--;
        return true;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get a hex at the specified coordinates.
    /// </summary>
    public TerrainHex? GetHex(HexCoords coords)
    {
        return _hexMap.TryGetValue(coords, out var hex) ? hex : null;
    }

    /// <summary>
    /// Check if a hex exists at the specified coordinates.
    /// </summary>
    public bool HasHex(HexCoords coords)
    {
        return _hexMap.ContainsKey(coords);
    }

    /// <summary>
    /// Get all valid placement positions for a new tile.
    /// This finds all positions where a new 7-hex tile could legally be placed.
    /// </summary>
    public List<HexCoords> GetValidPlacementPositions()
    {
        if (IsEmpty)
        {
            // If map is empty, any position is valid (return origin as suggestion)
            return new List<HexCoords> { HexCoords.Origin };
        }

        var validPositions = new List<HexCoords>();
        var checkedPositions = new HashSet<HexCoords>();

        // For each existing hex, check potential tile placements nearby
        foreach (var existingHex in _hexMap.Keys)
        {
            // A new tile center could be within distance 2 of existing hexes
            // (since tile radius is 1, and we need adjacency)
            for (int q = -3; q <= 3; q++)
            {
                for (int r = -3; r <= 3; r++)
                {
                    var candidate = new HexCoords(existingHex.Q + q, existingHex.R + r);
                    
                    if (checkedPositions.Contains(candidate))
                        continue;
                    
                    checkedPositions.Add(candidate);

                    if (IsValidPlacement(candidate))
                    {
                        validPositions.Add(candidate);
                    }
                }
            }
        }

        return validPositions;
    }

    /// <summary>
    /// Get all edge hexes of the current map (hexes with at least one empty neighbor).
    /// These are the hexes where exploration could occur.
    /// </summary>
    public List<HexCoords> GetMapEdgeHexes()
    {
        var edgeHexes = new List<HexCoords>();

        foreach (var hex in _hexMap.Keys)
        {
            var neighbors = hex.GetAllNeighbors();
            bool hasEmptyNeighbor = neighbors.Any(n => !_hexMap.ContainsKey(n));
            
            if (hasEmptyNeighbor)
            {
                edgeHexes.Add(hex);
            }
        }

        return edgeHexes;
    }

    /// <summary>
    /// Get all hexes belonging to a specific tile.
    /// </summary>
    public List<TerrainHex> GetTileHexes(string tileId)
    {
        return _hexMap.Values
            .Where(h => h.TileId == tileId)
            .ToList();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Clear the entire map.
    /// </summary>
    public void Clear()
    {
        _hexMap.Clear();
        TileCount = 0;
    }

    /// <summary>
    /// Get world position for a hex coordinate.
    /// </summary>
    public static (float x, float y) GetWorldPosition(HexCoords coords, float hexSize = 1.0f)
    {
        return coords.ToWorldPosition(hexSize);
    }

    #endregion
}
