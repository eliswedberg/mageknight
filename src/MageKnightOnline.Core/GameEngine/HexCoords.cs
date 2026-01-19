using System;

namespace MageKnightOnline.Core.GameEngine;

/// <summary>
/// Immutable struct representing hexagonal coordinates using the Axial coordinate system.
/// Uses pointy-top hexagon orientation (standard for Mage Knight).
/// 
/// Axial coordinates (q, r) can be converted to/from Cube coordinates (x, y, z) where:
///   x = q
///   z = r  
///   y = -x - z (derived, since x + y + z = 0)
/// </summary>
public readonly struct HexCoords : IEquatable<HexCoords>
{
    /// <summary>Column coordinate (x in cube coords)</summary>
    public int Q { get; }
    
    /// <summary>Row coordinate (z in cube coords)</summary>
    public int R { get; }

    // Cube coordinate accessors (derived)
    public int X => Q;
    public int Y => -Q - R;
    public int Z => R;

    public HexCoords(int q, int r)
    {
        Q = q;
        R = r;
    }

    /// <summary>
    /// Create HexCoords from cube coordinates.
    /// </summary>
    public static HexCoords FromCube(int x, int y, int z)
    {
        if (x + y + z != 0)
            throw new ArgumentException("Cube coordinates must sum to 0");
        return new HexCoords(x, z);
    }

    #region Neighbor Directions (Pointy-Top Layout)
    
    /// <summary>
    /// The 6 neighbor directions for pointy-top hexagons.
    /// Ordered clockwise starting from East.
    /// </summary>
    public static readonly HexCoords[] Directions = new HexCoords[]
    {
        new(+1,  0), // East
        new(+1, -1), // Northeast
        new( 0, -1), // Northwest
        new(-1,  0), // West
        new(-1, +1), // Southwest
        new( 0, +1), // Southeast
    };

    /// <summary>Direction indices for readability</summary>
    public const int DIR_E = 0;
    public const int DIR_NE = 1;
    public const int DIR_NW = 2;
    public const int DIR_W = 3;
    public const int DIR_SW = 4;
    public const int DIR_SE = 5;

    #endregion

    #region Neighbor Methods

    /// <summary>
    /// Get the neighbor in the specified direction (0-5).
    /// </summary>
    public HexCoords GetNeighbor(int direction)
    {
        if (direction < 0 || direction >= 6)
            throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be 0-5");
        return this + Directions[direction];
    }

    /// <summary>
    /// Get all 6 neighboring hex coordinates.
    /// </summary>
    public HexCoords[] GetAllNeighbors()
    {
        var neighbors = new HexCoords[6];
        for (int i = 0; i < 6; i++)
            neighbors[i] = this + Directions[i];
        return neighbors;
    }

    /// <summary>
    /// Check if this hex is adjacent to another hex.
    /// </summary>
    public bool IsAdjacentTo(HexCoords other)
    {
        return DistanceTo(other) == 1;
    }

    #endregion

    #region Distance Calculation

    /// <summary>
    /// Calculate the hex distance (number of steps) to another coordinate.
    /// Uses the cube coordinate formula: max(|dx|, |dy|, |dz|)
    /// </summary>
    public int DistanceTo(HexCoords other)
    {
        int dx = Math.Abs(X - other.X);
        int dy = Math.Abs(Y - other.Y);
        int dz = Math.Abs(Z - other.Z);
        return Math.Max(dx, Math.Max(dy, dz));
    }

    #endregion

    #region World Position Conversion

    /// <summary>
    /// Convert hex coordinates to world position (for rendering).
    /// Uses pointy-top layout with specified hex size.
    /// </summary>
    /// <param name="hexSize">The distance from center to corner (outer radius)</param>
    /// <returns>World position as (x, y) tuple</returns>
    public (float x, float y) ToWorldPosition(float hexSize = 1.0f)
    {
        // Pointy-top hex layout formulas:
        // x = size * sqrt(3) * (q + r/2)
        // y = size * 3/2 * r
        float sqrt3 = MathF.Sqrt(3);
        float x = hexSize * sqrt3 * (Q + R / 2.0f);
        float y = hexSize * 1.5f * R;
        return (x, y);
    }

    /// <summary>
    /// Convert world position to nearest hex coordinate.
    /// Uses pointy-top layout.
    /// </summary>
    public static HexCoords FromWorldPosition(float worldX, float worldY, float hexSize = 1.0f)
    {
        float sqrt3 = MathF.Sqrt(3);
        
        // Reverse the pointy-top formulas to get fractional q, r
        float q = (sqrt3 / 3 * worldX - 1.0f / 3 * worldY) / hexSize;
        float r = (2.0f / 3 * worldY) / hexSize;
        
        return RoundToNearest(q, r);
    }

    /// <summary>
    /// Round fractional axial coordinates to nearest hex.
    /// </summary>
    private static HexCoords RoundToNearest(float q, float r)
    {
        // Convert to cube, round, convert back
        float x = q;
        float z = r;
        float y = -x - z;

        int rx = (int)MathF.Round(x);
        int ry = (int)MathF.Round(y);
        int rz = (int)MathF.Round(z);

        float xDiff = MathF.Abs(rx - x);
        float yDiff = MathF.Abs(ry - y);
        float zDiff = MathF.Abs(rz - z);

        // Reset the coordinate with largest diff to maintain x+y+z=0
        if (xDiff > yDiff && xDiff > zDiff)
            rx = -ry - rz;
        else if (yDiff > zDiff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new HexCoords(rx, rz);
    }

    #endregion

    #region Equality and Operators

    public bool Equals(HexCoords other) => Q == other.Q && R == other.R;
    
    public override bool Equals(object? obj) => obj is HexCoords other && Equals(other);
    
    public override int GetHashCode() => HashCode.Combine(Q, R);

    public static bool operator ==(HexCoords a, HexCoords b) => a.Equals(b);
    public static bool operator !=(HexCoords a, HexCoords b) => !a.Equals(b);

    public static HexCoords operator +(HexCoords a, HexCoords b) => new(a.Q + b.Q, a.R + b.R);
    public static HexCoords operator -(HexCoords a, HexCoords b) => new(a.Q - b.Q, a.R - b.R);
    public static HexCoords operator *(HexCoords a, int scalar) => new(a.Q * scalar, a.R * scalar);

    #endregion

    #region Utility

    public override string ToString() => $"Hex({Q}, {R})";

    /// <summary>
    /// The origin hex (0, 0).
    /// </summary>
    public static readonly HexCoords Origin = new(0, 0);

    #endregion
}
