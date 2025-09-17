using MageKnightOnline.Models;

namespace MageKnightOnline.Services;

public class HexGridManager
{
    // Axial coordinates dictionary
    public Dictionary<(int q, int r), HexCell> Cells { get; } = new();

    // Hex geometry
    public int HexRadiusPixels { get; set; } = 30; // same visual size as map-hex

    public HexGridManager()
    {
        // Initialize with one starting tile at (0,0) with inner 7 hexes (radius 1)
        AddTileAt(0, 0);
        UpdateExploration();
    }

    public void AddTileAt(int centerQ, int centerR)
    {
        // Create inner 7 cells (radius 1) in axial coordinates
        var inner = GetRadiusCells(centerQ, centerR, 1);
        foreach (var (q, r) in inner)
        {
            var key = (q, r);
            if (!Cells.TryGetValue(key, out var cell))
            {
                cell = new HexCell { Q = q, R = r };
                Cells[key] = cell;
            }
            cell.IsTile = true;
            cell.IsExploration = false;
        }
    }

    public bool CanPlaceTileAt(int centerQ, int centerR)
    {
        // Cannot overlap existing tiles
        foreach (var (q, r) in GetRadiusCells(centerQ, centerR, 1))
        {
            if (Cells.TryGetValue((q, r), out var c) && c.IsTile)
                return false;
        }
        return true;
    }

    public void UpdateExploration()
    {
        // Mark all radius-2 ring cells around any existing tile as exploration if empty
        var toMark = new HashSet<(int q, int r)>();
        var tileCenters = GetTileCenters();
        foreach (var (cq, cr) in tileCenters)
        {
            foreach (var (q, r) in GetRingCells(cq, cr, 2))
            {
                toMark.Add((q, r));
            }
        }

        foreach (var (q, r) in toMark)
        {
            if (!Cells.TryGetValue((q, r), out var cell))
            {
                cell = new HexCell { Q = q, R = r };
                Cells[(q, r)] = cell;
            }
            if (!cell.IsTile)
            {
                cell.IsExploration = true;
            }
        }
    }

    public bool TryPlaceTile(int centerQ, int centerR)
    {
        if (!CanPlaceTileAt(centerQ, centerR)) return false;
        AddTileAt(centerQ, centerR);
        UpdateExploration();
        return true;
    }

    public List<(int q, int r)> GetTileCenters()
    {
        // Tile centers are the axial centers such that their radius-1 set are tiles.
        // We approximate: choose any cell that is the exact center (q,r) that itself is tile center if its 6 neighbors at distance 1 include the pattern 2-3-2 around it.
        // For simplicity, track centers by discovering any tile cell at center (0,0) initially and when adding.
        // Here, infer by checking if (q,r) is tile and all 6 neighbors at distance 1 are tile except two missing corners wouldn't fit 2-3-2.
        // Simpler: Keep centers explicitly by scanning existing tiles and picking those (q,r) where all radius-1 cells exist as tiles.
        var centers = new List<(int q, int r)>();
        var candidates = Cells.Values.Where(c => c.IsTile).Select(c => (c.Q, c.R)).ToHashSet();
        foreach (var (q, r) in candidates)
        {
            var ring = GetRadiusCells(q, r, 1);
            if (ring.All(p => Cells.TryGetValue(p, out var c) && c.IsTile))
            {
                centers.Add((q, r));
            }
        }
        // Ensure at least the initial center
        if (!centers.Contains((0, 0))) centers.Add((0, 0));
        return centers.Distinct().ToList();
    }

    // Axial helpers
    private static readonly (int dq, int dr)[] Directions = new[]
    {
        (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
    };

    public static IEnumerable<(int q, int r)> GetNeighbors(int q, int r)
    {
        foreach (var (dq, dr) in Directions)
            yield return (q + dq, r + dr);
    }

    public static IEnumerable<(int q, int r)> GetRadiusCells(int cq, int cr, int radius)
    {
        // All cells with hex distance <= radius
        var list = new List<(int, int)>();
        for (int dq = -radius; dq <= radius; dq++)
        {
            for (int dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
            {
                int q = cq + dq;
                int r = cr + dr;
                list.Add((q, r));
            }
        }
        return list;
    }

    public static IEnumerable<(int q, int r)> GetRingCells(int cq, int cr, int radius)
    {
        // Cells with exact hex distance = radius
        var results = new List<(int, int)>();
        int q = cq + Directions[4].dq * radius;
        int r = cr + Directions[4].dr * radius;
        for (int i = 0; i < 6; i++)
        {
            for (int step = 0; step < radius; step++)
            {
                results.Add((q, r));
                q += Directions[i].dq;
                r += Directions[i].dr;
            }
        }
        return results;
    }
}


