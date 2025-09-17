namespace MageKnightOnline.Models;

public class HexCell
{
    // Axial coordinates (q, r). s = -q - r
    public int Q { get; set; }
    public int R { get; set; }

    // State flags
    public bool IsTile { get; set; }
    public bool IsExploration { get; set; }

    public bool IsEmpty => !IsTile && !IsExploration;

    public (int q, int r) Key => (Q, R);
}


