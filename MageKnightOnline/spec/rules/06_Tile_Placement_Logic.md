# Tile Placement Logic

## Hexagonal Coordinate System

### Axial Coordinates (q, r)
The game uses **axial coordinates** (also known as "trapezoidal" or "offset" coordinates) for the hex grid. Each hex is identified by two coordinates: `q` (column) and `r` (row).

```
        (-1,0)  (0,0)  (1,0)
           (-1,1)  (0,1)  (1,1)
        (-1,2)  (0,2)  (1,2)
```

### Cube Coordinates (x, y, z)
For certain calculations, axial coordinates can be converted to cube coordinates:
- `x = q`
- `y = -q - r`
- `z = r`

The constraint `x + y + z = 0` always holds.

### Hex Directions
There are 6 directions from any hex to its neighbors (pointy-top orientation):

| Index | Direction  | Offset (q, r) |
|-------|------------|---------------|
| 0     | East       | (+1, 0)       |
| 1     | Northeast  | (+1, -1)      |
| 2     | Northwest  | (0, -1)       |
| 3     | West       | (-1, 0)       |
| 4     | Southwest  | (-1, +1)      |
| 5     | Southeast  | (0, +1)       |

## Map Tile Structure

### 7-Hex Tile Layout
Each Map Tile consists of **7 hexagons**: 1 center hex and 6 surrounding edge hexes.

```
        ⬡ (pos 1)
    ⬡       ⬡ (pos 2)
  (pos 6)  C
    ⬡       ⬡ (pos 3)
  (pos 5)  (pos 4)
```

### Tile Hex Positions (Relative to Center)
| Position | Name         | Relative Offset (q, r) |
|----------|--------------|------------------------|
| 0        | Center       | (0, 0)                 |
| 1        | Top          | (0, -1)                |
| 2        | Top-Right    | (+1, -1)               |
| 3        | Bottom-Right | (+1, 0)                |
| 4        | Bottom       | (0, +1)                |
| 5        | Bottom-Left  | (-1, +1)               |
| 6        | Top-Left     | (-1, 0)                |

## Tile Placement Rules

### Rule 1: No Overlap
None of the 7 hexes of the new tile can occupy a coordinate that already has a revealed hex.

### Rule 2: Adjacency
The new tile must be placed adjacent to the existing map - at least one hex of the new tile must be adjacent to an existing revealed hex.

### Rule 3: The "2-Hex" Rule (Core Tiles Only)
Core tiles (brown back) must share an edge with **at least two** existing hexes on the map.

### Rule 4: Edge-to-Edge Connection
Tiles must connect edge-to-edge, meaning one edge hex of the new tile becomes adjacent to an edge hex of an existing tile.

## Exploration Algorithm

### Step 1: Player Clicks on Unrevealed Hex
The player clicks on an unrevealed hex (`targetHex`) that is adjacent to their current position.

```
Player Position: (1, 0)
Target Hex: (2, 0)  ← Player clicks here
Direction: East (+1, 0)
```

### Step 2: Calculate Tile Center Position
The new tile's center must be placed **2 hexes away** from the player's position to avoid overlap:

```csharp
// Wrong: tileCenter = playerPos + direction  → Overlap!
// Correct:
tileCenter = targetHex + direction
```

**Example:**
```
Player at (1,0), clicks on (2,0), direction is (+1,0)
tileCenter = (2,0) + (+1,0) = (3,0)

New tile hexes:
  Center: (3, 0)
  Edges:  (3,-1), (4,-1), (4,0), (3,1), (2,1), (2,0)
                                              ↑
                                    Adjacent to player!
```

### Step 3: Calculate Direction Index
Find which of the 6 directions matches the exploration direction:

```csharp
var direction = new HexPosition 
{ 
    Q = targetHex.Q - playerPos.Q, 
    R = targetHex.R - playerPos.R 
};

for (int i = 0; i < HexDirections.Length; i++)
{
    if (HexDirections[i].Q == direction.Q && HexDirections[i].R == direction.R)
    {
        directionIndex = i;
        break;
    }
}
```

### Step 4: Calculate Tile Rotation
The tile must be rotated so that one of its edge hexes connects back to the player's position:

```csharp
// The edge that connects back is in the OPPOSITE direction
var oppositeDirectionIndex = (directionIndex + 3) % 6;

// Map direction index to tile edge position
var directionToTileEdge = new Dictionary<int, int>
{
    { 0, 1 }, // East → Position 1
    { 1, 3 }, // Northeast → Position 3
    { 2, 2 }, // Northwest → Position 2
    { 3, 4 }, // West → Position 4
    { 4, 6 }, // Southwest → Position 6
    { 5, 5 }  // Southeast → Position 5
};

var connectingEdgePosition = directionToTileEdge[oppositeDirectionIndex];

// Calculate rotation to align tile edge with connection point
var rotationOffset = CalculateRotationOffset(connectingEdgePosition, oppositeDirectionIndex);
```

### Step 5: Generate Tile Hexes with Rotation
Apply rotation and generate all 7 hex positions:

```csharp
var tileHexes = GenerateTileHexesWithRotation(tileCenter, tileDef, rotationOffset);
```

**Important:** The rotation algorithm must work on **direction indices**, not position indices.

#### Position to Direction Mapping
Tile positions (1-6) are NOT in circular order around the hex:
```
Position:  1    2    3    4    5    6
Direction: E    NW   NE   W    SE   SW
Index:     0    2    1    3    5    4
```

#### Correct Rotation Algorithm
```csharp
// Map tile position to HexDirections index
var positionToDirectionIndex = new[] { -1, 0, 2, 1, 3, 5, 4 };

foreach (var hexDef in tileDef.Hexes)
{
    if (hexDef.Position == 0)
    {
        // Center hex - no rotation needed
        hexPos = center;
    }
    else
    {
        // Get the direction index for this position
        var dirIndex = positionToDirectionIndex[hexDef.Position];
        
        // Rotate the direction
        var rotatedDirIndex = (dirIndex + rotationOffset) % 6;
        
        // Get the hex position using the rotated direction
        var direction = HexDirections[rotatedDirIndex];
        hexPos = center + direction;
    }
}
```

#### Why This Matters
Without proper direction-based rotation, newly placed tiles may have hexes at incorrect world coordinates, causing:
- Movement to fail across tile boundaries
- Sites appearing at wrong locations
- Visual misalignment on the map

## Visual Representation

### Before Exploration
```
    Existing Tile (center at 0,0)
    
         ⬡
       ⬡ C ⬡
         ⬡ [P]  ← Player at (1,0)
              🗺️  ← Explorable hex at (2,0)
```

### After Exploration
```
    Tile 1               Tile 2 (new, center at 3,0)
    
         ⬡                    ⬡
       ⬡ C ⬡              ⬡ C ⬡
         ⬡ [P] ⬡—————⬡ ⬡
                ↑       ↑
            (2,0)   (2,0) is now part of Tile 2!
```

## Implementation Classes

### HexCoords Struct
Located at: `src/MageKnightOnline.Core/GameEngine/HexCoords.cs`

Provides:
- Axial coordinate storage (Q, R)
- Cube coordinate conversion (X, Y, Z)
- Neighbor calculation
- Distance calculation
- World position conversion

### MapPlacementManager Class
Located at: `src/MageKnightOnline.Core/GameEngine/MapPlacementManager.cs`

Provides:
- `IsValidPlacement(HexCoords proposedTileCenter)` - Validates placement rules
- `PlaceTile(HexCoords tileCenter, MapTileDefinition tileDef)` - Places a new tile
- `GetValidPlacementPositions()` - Returns all valid positions for new tiles
- `GetMapEdgeHexes()` - Returns hexes on the edge of the current map

### GameEngine Methods
Located at: `src/MageKnightOnline.Core/GameEngine/GameEngine.cs`

- `ExploreTile(HexPosition targetHex)` - Main exploration entry point
- `PlaceNewTileAtEdge(HexPosition edgeHex, HexPosition unrevealedNeighbor)` - Places tile
- `GenerateTileHexesWithRotation(...)` - Generates hex data with rotation
- `CalculateRotationOffset(...)` - Calculates tile rotation

## Pixel Conversion (for Rendering)

### Hex to Pixel (Pointy-Top)
```csharp
// For pointy-top hexes with hex size (radius) = size
var x = size * Math.Sqrt(3) * (q + r / 2.0);
var y = size * 1.5 * r;
```

### Tile to Pixel
Tiles use the same coordinate system as hexes. The tile's visual center corresponds to the center hex position.

## Constants

```csharp
// Hex image dimensions (from Mage Knight tile images)
BaseHexHeight = 267.6   // Point-to-point height of one hex
BaseHexWidth = 232.0    // Flat-to-flat width of one hex
BaseTileWidth = 696.0   // Full tile image width
BaseTileHeight = 669.0  // Full tile image height
```

## Error Handling

### Invalid Exploration Attempts
- "This hex is already revealed" - Target hex is already part of the map
- "You must be adjacent to the hex you want to explore" - Target too far away
- "Not enough movement points to explore (need 1)" - Insufficient movement
- "No tiles available to explore" - Tile deck is empty
- "Invalid exploration direction" - Direction calculation failed

## References

- [Red Blob Games: Hexagonal Grids](https://www.redblobgames.com/grids/hexagons/)
- Mage Knight Board Game Ultimate Edition Rule Book (2018)
- `spec/definitions/map_tiles.json` - Tile definitions
- `spec/definitions/map_tiles.json.desc` - Tile structure description
