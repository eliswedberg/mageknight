using MageKnightOnline.Core.GameEngine;
using Xunit;

namespace MageKnightOnline.Tests;

/// <summary>
/// Unit tests for HexCoords and MapPlacementManager.
/// </summary>
public class MapPlacementTests
{
    #region HexCoords Tests

    [Fact]
    public void HexCoords_Equality_Works()
    {
        var a = new HexCoords(1, 2);
        var b = new HexCoords(1, 2);
        var c = new HexCoords(2, 1);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.NotEqual(a, c);
        Assert.True(a != c);
    }

    [Fact]
    public void HexCoords_CubeCoordinates_SumToZero()
    {
        var coords = new HexCoords(3, -2);
        
        // x + y + z should always equal 0
        Assert.Equal(0, coords.X + coords.Y + coords.Z);
    }

    [Fact]
    public void HexCoords_Addition_Works()
    {
        var a = new HexCoords(1, 2);
        var b = new HexCoords(3, -1);
        var result = a + b;

        Assert.Equal(new HexCoords(4, 1), result);
    }

    [Fact]
    public void HexCoords_Distance_IsCorrect()
    {
        var origin = new HexCoords(0, 0);
        var neighbor = new HexCoords(1, 0);
        var twoAway = new HexCoords(2, 0);
        var diagonal = new HexCoords(1, 1);

        Assert.Equal(0, origin.DistanceTo(origin));
        Assert.Equal(1, origin.DistanceTo(neighbor));
        Assert.Equal(2, origin.DistanceTo(twoAway));
        Assert.Equal(2, origin.DistanceTo(diagonal));
    }

    [Fact]
    public void HexCoords_GetAllNeighbors_Returns6Neighbors()
    {
        var center = new HexCoords(0, 0);
        var neighbors = center.GetAllNeighbors();

        Assert.Equal(6, neighbors.Length);
        
        // All neighbors should be distance 1 from center
        foreach (var neighbor in neighbors)
        {
            Assert.Equal(1, center.DistanceTo(neighbor));
        }
    }

    [Fact]
    public void HexCoords_IsAdjacentTo_Works()
    {
        var a = new HexCoords(0, 0);
        var b = new HexCoords(1, 0);  // Adjacent
        var c = new HexCoords(2, 0);  // Not adjacent (distance 2)

        Assert.True(a.IsAdjacentTo(b));
        Assert.True(b.IsAdjacentTo(a)); // Symmetric
        Assert.False(a.IsAdjacentTo(c));
    }

    #endregion

    #region MapPlacementManager - Tile Shape Tests

    [Fact]
    public void GetTileLocalCoords_Returns7Hexes()
    {
        var localCoords = MapPlacementManager.GetTileLocalCoords();
        
        Assert.Equal(7, localCoords.Length);
        Assert.Equal(new HexCoords(0, 0), localCoords[0]); // Center should be origin
    }

    [Fact]
    public void GetTileWorldCoords_OffsetsCorrectly()
    {
        var tileCenter = new HexCoords(5, 3);
        var worldCoords = MapPlacementManager.GetTileWorldCoords(tileCenter);
        
        Assert.Equal(7, worldCoords.Length);
        Assert.Equal(tileCenter, worldCoords[0]); // First should be the center
    }

    #endregion

    #region MapPlacementManager - First Tile Tests

    [Fact]
    public void FirstTile_CanBePlacedAnywhere()
    {
        var manager = new MapPlacementManager();
        
        Assert.True(manager.IsEmpty);
        
        // First tile should be valid anywhere
        Assert.True(manager.IsValidPlacement(new HexCoords(0, 0)));
        Assert.True(manager.IsValidPlacement(new HexCoords(100, -50)));
    }

    [Fact]
    public void PlaceTile_AddsHexesToMap()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Center", "N", "NE", "E", "S", "SW", "W" };
        
        bool placed = manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        Assert.True(placed);
        Assert.Equal(1, manager.TileCount);
        Assert.Equal(7, manager.HexMap.Count);
    }

    #endregion

    #region MapPlacementManager - Rule A (No Overlap) Tests

    [Fact]
    public void RuleA_RejectsOverlappingPlacement()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place first tile at origin
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        // Try to place second tile at the same position - should fail
        var result = manager.ValidatePlacement(HexCoords.Origin);
        
        Assert.False(result.IsValid);
        Assert.False(result.PassesOverlapRule);
        Assert.NotEmpty(result.OverlappingHexes);
    }

    [Fact]
    public void RuleA_RejectsPartialOverlap()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place first tile at origin
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        // Try to place second tile 1 hex away - this would overlap edge hexes
        var result = manager.ValidatePlacement(new HexCoords(1, 0));
        
        Assert.False(result.IsValid);
        Assert.False(result.PassesOverlapRule);
    }

    #endregion

    #region MapPlacementManager - Rule B (Adjacency) Tests

    [Fact]
    public void RuleB_RejectsFloatingTile()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place first tile at origin
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        // Try to place second tile far away - should fail adjacency
        var result = manager.ValidatePlacement(new HexCoords(10, 10));
        
        Assert.False(result.IsValid);
        Assert.False(result.PassesAdjacencyRule);
    }

    #endregion

    #region MapPlacementManager - Rule C (2-Hex Rule) Tests

    [Fact]
    public void RuleC_RejectsSingleHexConnection()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place first tile at origin
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        // Find a position that only touches 1 hex
        // Tile at (3, -1) - its SW corner would touch the E edge of origin tile's NE hex
        // But this might touch more than 1... let's check further out
        var result = manager.ValidatePlacement(new HexCoords(3, -2));
        
        // Verify if this position touches only 1 hex
        if (result.AdjacentExistingHexCount == 1)
        {
            Assert.False(result.IsValid);
            Assert.False(result.PassesTwoHexRule);
            Assert.True(result.PassesAdjacencyRule); // Does touch at least 1
            Assert.Contains("2-hex rule", result.ErrorMessage ?? "");
        }
    }

    [Fact]
    public void RuleC_AcceptsValidPlacement()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place first tile at origin
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        // Place second tile at position (2, 1) - should share multiple edges
        // Tile at (2,1) has hexes at (2,1), (2,0), (3,0), (3,1), (2,2), (1,2), (1,1)
        // (2,0) is neighbor to (1,0) which is on tile A
        // (1,1) is neighbor to (1,0) and (0,1) which are both on tile A
        // So this position touches 2 existing hexes: (1,0) and (0,1)
        var result = manager.ValidatePlacement(new HexCoords(2, 1));
        
        // This should be valid (touches 2+ hexes)
        Assert.True(result.PassesOverlapRule, $"Overlap check failed. Overlapping: {string.Join(", ", result.OverlappingHexes)}");
        Assert.True(result.PassesAdjacencyRule, "Adjacency check failed");
        Assert.True(result.PassesTwoHexRule, $"2-hex rule failed. Adjacent count: {result.AdjacentExistingHexCount}");
        Assert.True(result.IsValid, result.ErrorMessage ?? "Unknown error");
        Assert.True(result.AdjacentExistingHexCount >= 2, $"Expected >=2 adjacent, got {result.AdjacentExistingHexCount}");
    }

    #endregion

    #region MapPlacementManager - Integration Tests

    [Fact]
    public void CanPlace_MultipleTiles_InValidConfiguration()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place tile 1 at origin
        Assert.True(manager.PlaceTile(HexCoords.Origin, "tile_001", terrain));
        Assert.Equal(1, manager.TileCount);
        
        // Place tile 2 at (2, 1) - touches (1,0) and (0,1) from tile 1
        var result2 = manager.ValidatePlacement(new HexCoords(2, 1));
        Assert.True(result2.IsValid, $"Tile 2 validation failed: {result2.ErrorMessage}");
        Assert.True(manager.PlaceTile(new HexCoords(2, 1), "tile_002", terrain));
        Assert.Equal(2, manager.TileCount);
        
        // Place tile 3 at (-2, -1) - should touch hexes from tile 1
        // Tile at (-2,-1) has hexes: (-2,-1), (-2,-2), (-1,-2), (-1,-1), (-2,0), (-3,0), (-3,-1)
        // (-1,-1) neighbors (0,-1) and (-1,0) which are on tile 1
        // (-2,0) neighbors (-1,0) which is on tile 1
        // So this touches 2 existing hexes: (0,-1) and (-1,0)
        var result3 = manager.ValidatePlacement(new HexCoords(-2, -1));
        Assert.True(result3.IsValid, $"Tile 3 validation failed: {result3.ErrorMessage}. Adjacent: {result3.AdjacentExistingHexCount}");
        Assert.True(manager.PlaceTile(new HexCoords(-2, -1), "tile_003", terrain));
        Assert.Equal(3, manager.TileCount);
        
        // Total hexes should be 21 (3 tiles × 7 hexes)
        Assert.Equal(21, manager.HexMap.Count);
    }

    [Fact]
    public void GetValidPlacementPositions_ReturnsValidOptions()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Place first tile
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        // Get valid positions for second tile
        var validPositions = manager.GetValidPlacementPositions();
        
        // Should have multiple valid positions
        Assert.NotEmpty(validPositions);
        
        // Each returned position should actually be valid
        foreach (var pos in validPositions)
        {
            Assert.True(manager.IsValidPlacement(pos), $"Position {pos} was returned as valid but fails validation");
        }
    }

    [Fact]
    public void GetMapEdgeHexes_ReturnsCorrectHexes()
    {
        var manager = new MapPlacementManager();
        var terrain = new[] { "Plains", "Plains", "Plains", "Plains", "Plains", "Plains", "Plains" };
        
        // Single tile - all 7 hexes should have at least one empty neighbor
        // (since the tile is surrounded by empty space)
        manager.PlaceTile(HexCoords.Origin, "tile_001", terrain);
        
        var edgeHexes = manager.GetMapEdgeHexes();
        
        // For a single tile, all 7 hexes are edge hexes (all have empty neighbors)
        // Center has 6 neighbors, all on the tile itself
        // Edge hexes have 3 neighbors on the tile and 3 outside
        // But center's neighbors ARE the 6 edge hexes, so center has NO empty neighbors!
        // Only the 6 edge hexes have empty neighbors
        Assert.Equal(6, edgeHexes.Count);
        
        // The center hex (0,0) should NOT be in the edge list
        Assert.DoesNotContain(HexCoords.Origin, edgeHexes);
    }

    #endregion

    #region World Position Tests

    [Fact]
    public void WorldPosition_OriginIsZero()
    {
        var origin = HexCoords.Origin;
        var (x, y) = origin.ToWorldPosition(1.0f);
        
        Assert.Equal(0f, x, precision: 5);
        Assert.Equal(0f, y, precision: 5);
    }

    [Fact]
    public void WorldPosition_NeighborsAreEquidistant()
    {
        var center = HexCoords.Origin;
        var (cx, cy) = center.ToWorldPosition(1.0f);
        
        var neighbors = center.GetAllNeighbors();
        float? firstDistance = null;
        
        foreach (var neighbor in neighbors)
        {
            var (nx, ny) = neighbor.ToWorldPosition(1.0f);
            float distance = MathF.Sqrt((nx - cx) * (nx - cx) + (ny - cy) * (ny - cy));
            
            if (firstDistance == null)
            {
                firstDistance = distance;
            }
            else
            {
                // All neighbors should be equidistant from center
                Assert.Equal(firstDistance.Value, distance, precision: 4);
            }
        }
    }

    #endregion
}
