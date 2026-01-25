using System.Text.Json;
using MageKnightOnline.Core.Definitions;
using MageKnightOnline.Core.GameState;
using MageKnightOnline.Core.Services;

namespace MageKnightOnline.Core.GameEngine;

/// <summary>
/// Core game engine that handles all Mage Knight game logic.
/// </summary>
public class GameEngine : IGameEngine
{
    private readonly IGameDefinitionService _definitions;
    private GameStateModel _state = new();
    private readonly Random _random = new();
    
    // Undo system constants
    private const int MaxUndoHistory = 10;

    // Terrain costs (day, night)
    private static readonly Dictionary<string, (int Day, int Night)> TerrainCosts = new()
    {
        ["Plains"] = (2, 2),
        ["Hills"] = (3, 3),
        ["Forest"] = (3, 5),
        ["Wasteland"] = (4, 4),
        ["Desert"] = (5, 3),
        ["Swamp"] = (5, 5),
        ["Mountain"] = (99, 99), // Impassable
        ["Water"] = (99, 99),    // Impassable
        ["City"] = (2, 2),
    };

    // Hex directions for axial coordinates (pointy-top)
    private static readonly HexPosition[] HexDirections = new[]
    {
        new HexPosition { Q = 1, R = 0 },   // East
        new HexPosition { Q = 1, R = -1 },  // Northeast
        new HexPosition { Q = 0, R = -1 },  // Northwest
        new HexPosition { Q = -1, R = 0 },  // West
        new HexPosition { Q = -1, R = 1 },  // Southwest
        new HexPosition { Q = 0, R = 1 },   // Southeast
    };

    public GameEngine(IGameDefinitionService definitions)
    {
        _definitions = definitions;
    }

    public GameStateModel State => _state;

    public void LoadState(string? gameStateJson)
    {
        if (string.IsNullOrEmpty(gameStateJson))
        {
            _state = new GameStateModel();
            return;
        }

        _state = JsonSerializer.Deserialize<GameStateModel>(gameStateJson) ?? new GameStateModel();
        
        // Initialize offers if they are null (for games created before offers were added)
        if (_state.UnitOffers == null)
            _state.UnitOffers = new UnitOfferState();
        if (_state.SpellOffers == null)
            _state.SpellOffers = new SpellOfferState();
        if (_state.AdvancedActionOffers == null)
            _state.AdvancedActionOffers = new AdvancedActionOfferState();
        
        // Fill offers if they are empty (for existing games)
        // Always ensure offers are filled - this handles both new and existing games
        if (_state.UnitOffers.RegularUnits.Count < 3 || _state.UnitOffers.EliteUnits.Count < 2)
            RefillUnitOffers();
        if (_state.SpellOffers.Spells.Count < 3)
            RefillCardOffers();
    }

    public string SaveState()
    {
        return JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = false });
    }

    public PlayerState? GetCurrentPlayer()
    {
        if (_state.CurrentPlayerIndex < 0 || _state.CurrentPlayerIndex >= _state.Players.Count)
            return null;

        var turnOrder = _state.TurnOrder;
        if (turnOrder.Count == 0)
            return _state.Players.FirstOrDefault();

        var playerIndex = turnOrder[_state.CurrentPlayerIndex];
        return _state.Players.ElementAtOrDefault(playerIndex);
    }

    public PlayerState? GetPlayer(Guid userId)
    {
        return _state.Players.FirstOrDefault(p => p.UserId == userId);
    }

    public IEnumerable<HexPosition> GetValidMoves(int movementPoints)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            yield break;

        var visited = new Dictionary<string, int>(); // position -> remaining movement
        var toVisit = new Queue<(HexPosition pos, int remaining)>();

        visited[PosKey(player.Position)] = movementPoints;
        toVisit.Enqueue((player.Position, movementPoints));

        // Debug: Log revealed hexes count
        var totalRevealed = _state.Map.RevealedHexes.Count;
        var totalHexData = _state.Map.HexData.Count;

        while (toVisit.Count > 0)
        {
            var (current, remaining) = toVisit.Dequeue();

            foreach (var dir in HexDirections)
            {
                var neighbor = current + dir;
                var key = PosKey(neighbor);

                // Check if hex is revealed first
                if (!_state.Map.RevealedHexes.Contains(key))
                    continue; // Not revealed - can't move there

                // Get terrain cost
                var terrain = GetTerrainAt(neighbor);
                if (terrain == null) continue; // No terrain data (shouldn't happen if revealed)

                var cost = GetTerrainCost(terrain);
                if (cost >= 99) continue; // Impassable

                var newRemaining = remaining - cost;
                if (newRemaining < 0) continue; // Not enough movement

                // Check if we've found a better path
                if (visited.TryGetValue(key, out var existing) && existing >= newRemaining)
                    continue;

                visited[key] = newRemaining;
                toVisit.Enqueue((neighbor, newRemaining));
            }
        }

        // Return all reachable positions except the starting position
        var startKey = PosKey(player.Position);
        foreach (var kvp in visited)
        {
            if (kvp.Key != startKey)
            {
                var parts = kvp.Key.Split(',');
                yield return new HexPosition { Q = int.Parse(parts[0]), R = int.Parse(parts[1]) };
            }
        }
    }

    public GameActionResult MovePlayer(HexPosition destination)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.Phase != GamePhase.Movement)
            return GameActionResult.Fail("Can only move during movement phase");

        var terrain = GetTerrainAt(destination);
        if (terrain == null)
            return GameActionResult.Fail("Cannot move to unrevealed hex");

        var cost = GetTerrainCost(terrain);
        if (cost >= 99)
            return GameActionResult.Fail($"Cannot enter {terrain} - impassable terrain");

        // Save state for undo before moving
        SaveStateForUndo();

        if (player.MovementRemaining < cost)
            return GameActionResult.Fail($"Not enough movement points (need {cost}, have {player.MovementRemaining})");

        // Check if adjacent
        if (!IsAdjacent(player.Position, destination))
            return GameActionResult.Fail("Can only move to adjacent hexes");

        // Move the player
        var oldPosition = player.Position;
        player.Position = destination;
        player.MovementRemaining -= cost;

        // Check for enemies at destination
        var hexState = GetHexStateAt(destination);
        if (hexState?.Enemies.Any() == true)
        {
            _state.Phase = GamePhase.Combat;
            AddLogEntry("Combat", $"Encountered {hexState.Enemies.Count} enemies at ({destination.Q}, {destination.R})!");
            return GameActionResult.Ok($"Moved to ({destination.Q}, {destination.R}) - Combat initiated!");
        }

        // Check if there's a site to interact with
        if (!string.IsNullOrEmpty(hexState?.SiteType) && hexState.SiteType != "Portal")
        {
            AddLogEntry("Move", $"Moved to {hexState.SiteType} at ({destination.Q}, {destination.R}), cost {cost}");
            return GameActionResult.Ok($"Moved to {hexState.SiteType}");
        }

        AddLogEntry("Move", $"Moved from ({oldPosition.Q},{oldPosition.R}) to ({destination.Q},{destination.R}), cost {cost} ({terrain})");
        return GameActionResult.Ok($"Moved to ({destination.Q}, {destination.R})");
    }

    public IEnumerable<HexPosition> GetValidFlightMoves(int flightPoints)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            yield break;

        var visited = new Dictionary<string, int>();
        var toVisit = new Queue<(HexPosition pos, int remaining)>();

        visited[PosKey(player.Position)] = flightPoints;
        toVisit.Enqueue((player.Position, flightPoints));

        while (toVisit.Count > 0)
        {
            var (current, remaining) = toVisit.Dequeue();

            foreach (var dir in HexDirections)
            {
                var neighbor = current + dir;
                var key = PosKey(neighbor);

                // Flight ignores terrain but still needs revealed hex
                var terrain = GetTerrainAt(neighbor);
                if (terrain == null) continue;

                // Flight costs 1 per hex regardless of terrain
                var newRemaining = remaining - 1;
                if (newRemaining < 0) continue;

                if (visited.TryGetValue(key, out var existing) && existing >= newRemaining)
                    continue;

                visited[key] = newRemaining;
                toVisit.Enqueue((neighbor, newRemaining));
            }
        }

        var startKey = PosKey(player.Position);
        foreach (var kvp in visited)
        {
            if (kvp.Key != startKey)
            {
                var parts = kvp.Key.Split(',');
                yield return new HexPosition { Q = int.Parse(parts[0]), R = int.Parse(parts[1]) };
            }
        }
    }

    public GameActionResult MovePlayerWithFlight(HexPosition destination)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.Phase != GamePhase.Movement)
            return GameActionResult.Fail("Can only move during movement phase");

        var terrain = GetTerrainAt(destination);
        if (terrain == null)
            return GameActionResult.Fail("Cannot fly to unrevealed hex");

        // Flight costs 1 per hex
        if (player.FlightRemaining < 1)
            return GameActionResult.Fail("No flight points remaining");

        if (!IsAdjacent(player.Position, destination))
            return GameActionResult.Fail("Can only fly to adjacent hexes");

        // Move the player
        var oldPosition = player.Position;
        player.Position = destination;
        player.FlightRemaining -= 1;

        // Check for enemies at destination - flight doesn't avoid combat when landing
        var hexState = GetHexStateAt(destination);
        if (hexState?.Enemies.Any() == true)
        {
            _state.Phase = GamePhase.Combat;
            AddLogEntry("Combat", $"Flew to hex with {hexState.Enemies.Count} enemies - Combat initiated!");
            return GameActionResult.Ok($"Flew to ({destination.Q}, {destination.R}) - Combat initiated!");
        }

        if (!string.IsNullOrEmpty(hexState?.SiteType) && hexState.SiteType != "Portal")
        {
            AddLogEntry("Move", $"Flew to {hexState.SiteType} at ({destination.Q}, {destination.R})");
            return GameActionResult.Ok($"Flew to {hexState.SiteType}");
        }

        AddLogEntry("Move", $"Flew from ({oldPosition.Q},{oldPosition.R}) to ({destination.Q},{destination.R}) over {terrain}");
        return GameActionResult.Ok($"Flew to ({destination.Q}, {destination.R})");
    }

    public GameActionResult MovePlayerSafely(HexPosition destination)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.Phase != GamePhase.Movement)
            return GameActionResult.Fail("Can only move during movement phase");

        var terrain = GetTerrainAt(destination);
        if (terrain == null)
            return GameActionResult.Fail("Cannot move to unrevealed hex");

        var cost = GetTerrainCost(terrain);
        if (cost >= 99)
            return GameActionResult.Fail($"Cannot enter {terrain} - impassable terrain");

        // Safe movement uses safe movement pool first, then regular movement
        var totalSafeAvailable = player.SafeMovementRemaining;
        
        if (totalSafeAvailable < cost)
            return GameActionResult.Fail($"Not enough safe movement points (need {cost}, have {totalSafeAvailable})");

        if (!IsAdjacent(player.Position, destination))
            return GameActionResult.Fail("Can only move to adjacent hexes");

        // Move the player
        var oldPosition = player.Position;
        player.Position = destination;
        player.SafeMovementRemaining -= cost;

        // Safe movement does NOT provoke rampaging enemies - but entering hex with enemies still triggers combat
        var hexState = GetHexStateAt(destination);
        if (hexState?.Enemies.Any() == true)
        {
            _state.Phase = GamePhase.Combat;
            AddLogEntry("Combat", $"Entered hex with {hexState.Enemies.Count} enemies - Combat initiated!");
            return GameActionResult.Ok($"Safely moved to ({destination.Q}, {destination.R}) - Combat initiated!");
        }

        if (!string.IsNullOrEmpty(hexState?.SiteType) && hexState.SiteType != "Portal")
        {
            AddLogEntry("Move", $"Safely moved to {hexState.SiteType} at ({destination.Q}, {destination.R})");
            return GameActionResult.Ok($"Safely moved to {hexState.SiteType}");
        }

        AddLogEntry("Move", $"Safely moved from ({oldPosition.Q},{oldPosition.R}) to ({destination.Q},{destination.R}), cost {cost} ({terrain})");
        return GameActionResult.Ok($"Safely moved to ({destination.Q}, {destination.R})");
    }

    public IEnumerable<HexPosition> GetRampagingEnemyHexes()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            yield break;

        // Return all revealed hexes with enemies that are adjacent to the player's current position
        foreach (var dir in HexDirections)
        {
            var neighbor = player.Position + dir;
            var hexState = GetHexStateAt(neighbor);
            if (hexState?.Enemies.Any() == true && !hexState.IsConquered)
            {
                yield return neighbor;
            }
        }
    }

    /// <summary>
    /// Gets hexes at the edge of the revealed map where exploration is possible.
    /// </summary>
    public IEnumerable<HexPosition> GetExplorableEdges()
    {
        var player = GetCurrentPlayer();
        if (player == null) yield break;

        // Find revealed hexes that have at least one unrevealed neighbor
        foreach (var revealedKey in _state.Map.RevealedHexes)
        {
            var parts = revealedKey.Split(',');
            var hex = new HexPosition { Q = int.Parse(parts[0]), R = int.Parse(parts[1]) };
            
            // Check if this hex has an unrevealed neighbor (edge hex)
            foreach (var dir in HexDirections)
            {
                var neighbor = hex + dir;
                var neighborKey = PosKey(neighbor);
                
                if (!_state.Map.RevealedHexes.Contains(neighborKey))
                {
                    yield return hex; // This is an edge hex
                    break;
                }
            }
        }
    }

    public GameActionResult ExploreTile(HexPosition targetHex, int? edgePosition = null)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.Phase != GamePhase.Movement)
            return GameActionResult.Fail("Can only explore during movement phase");

        // Check if target hex is unrevealed
        var targetKey = PosKey(targetHex);
        if (_state.Map.RevealedHexes.Contains(targetKey))
            return GameActionResult.Fail("This hex is already revealed");

        // Mark as irreversible action - cannot undo after exploring
        MarkIrreversibleAction();

        // Check if player is adjacent to the target hex
        var playerPos = player.Position;
        bool isAdjacent = false;
        
        foreach (var dir in HexDirections)
        {
            var neighbor = playerPos + dir;
            if (neighbor.Q == targetHex.Q && neighbor.R == targetHex.R)
            {
                isAdjacent = true;
                break;
            }
        }

        if (!isAdjacent)
            return GameActionResult.Fail("You must be adjacent to the hex you want to explore");

        // Exploration costs 1 movement point
        if (player.MovementRemaining < 1)
            return GameActionResult.Fail("Not enough movement points to explore (need 1)");

        // Draw tile and place it edge-to-edge
        // playerPos is the player's current position, targetHex is the direction they're exploring
        var result = PlaceNewTileAtEdge(playerPos, targetHex, edgePosition);
        if (result.Success)
        {
            player.MovementRemaining -= 1;
            AddLogEntry("Explore", $"Explored new tile towards ({targetHex.Q}, {targetHex.R})");
        }

        return result;
    }

    private GameActionResult PlaceNewTileAtEdge(HexPosition edgeHex, HexPosition unrevealedNeighbor, int? preferredEdgePosition)
    {
        // Draw a tile from the appropriate deck
        string? tileId = null;
        
        if (_state.Decks.CountrysideTiles.Any())
        {
            tileId = _state.Decks.CountrysideTiles[0];
            _state.Decks.CountrysideTiles.RemoveAt(0);
        }
        else if (_state.Decks.CoreTiles.Any())
        {
            tileId = _state.Decks.CoreTiles[0];
            _state.Decks.CoreTiles.RemoveAt(0);
        }

        if (tileId == null)
            return GameActionResult.Fail("No tiles available to explore");

        // Get tile definition
        var tileDef = _definitions.GetMapTilesAsync().Result.FirstOrDefault(t => 
            t.Id == tileId || 
            (tileId.StartsWith("countryside_", StringComparison.OrdinalIgnoreCase) && 
             t.BackType == "Countryside") ||
            (tileId.StartsWith("core_", StringComparison.OrdinalIgnoreCase) && 
             t.BackType == "Core") ||
            (tileId.StartsWith("city_", StringComparison.OrdinalIgnoreCase) && 
             t.BackType == "City"));

        // Fallback to random tile of correct type if exact match not found
        if (tileDef == null)
        {
            var allTiles = _definitions.GetMapTilesAsync().Result.ToList();
            if (tileId.StartsWith("countryside_", StringComparison.OrdinalIgnoreCase))
            {
                var countrysideTiles = allTiles.Where(t => t.BackType == "Countryside" && !t.IsStartingTile).ToList();
                if (countrysideTiles.Any())
                    tileDef = countrysideTiles[_random.Next(countrysideTiles.Count)];
            }
            else if (tileId.StartsWith("core_", StringComparison.OrdinalIgnoreCase))
            {
                var coreTiles = allTiles.Where(t => t.BackType == "Core" && !t.IsStartingTile).ToList();
                if (coreTiles.Any())
                    tileDef = coreTiles[_random.Next(coreTiles.Count)];
            }
            else if (tileId.StartsWith("city_", StringComparison.OrdinalIgnoreCase))
            {
                var cityTiles = allTiles.Where(t => t.BackType == "City" && !t.IsStartingTile).ToList();
                if (cityTiles.Any())
                    tileDef = cityTiles[_random.Next(cityTiles.Count)];
            }
        }

        if (tileDef == null)
            return GameActionResult.Fail("Could not find tile definition");

        // Calculate direction from edgeHex to unrevealedNeighbor
        // This is the direction we want the new tile to extend into
        var direction = new HexPosition 
        { 
            Q = unrevealedNeighbor.Q - edgeHex.Q, 
            R = unrevealedNeighbor.R - edgeHex.R 
        };

        // Find which direction index this corresponds to
        int directionIndex = -1;
        for (int i = 0; i < HexDirections.Length; i++)
        {
            if (HexDirections[i].Q == direction.Q && HexDirections[i].R == direction.R)
            {
                directionIndex = i;
                break;
            }
        }

        if (directionIndex == -1)
            return GameActionResult.Fail("Invalid exploration direction");

        // Tile placement rule: The new tile's center must be placed far enough that 
        // the edge hexes don't overlap with existing hexes.
        // A 7-hex tile has center + 6 surrounding hexes. 
        // For edge-to-edge placement: tileCenter = unrevealedNeighbor + direction
        // This places the center 2 steps away from the player's edge hex
        var tileCenter = unrevealedNeighbor + direction;
        
        // The edge hex that connects to edgeHex is in the OPPOSITE direction from tile center
        // So we need to rotate the tile so that the edge hex in the opposite direction matches edgeHex
        var oppositeDirectionIndex = GetOppositeDirection(directionIndex);
        
        // Map HexDirections index to tile edge position
        // HexDirections: [East(0), Northeast(1), Northwest(2), West(3), Southwest(4), Southeast(5)]
        // Tile positions: 1=East, 2=NW, 3=NE, 4=West, 5=SE, 6=SW
        // 
        // Mapping: direction index -> tile edge position
        var directionToTileEdge = new Dictionary<int, int>
        {
            { 0, 1 }, // East → Position 1
            { 1, 3 }, // Northeast → Position 3
            { 2, 2 }, // Northwest → Position 2
            { 3, 4 }, // West → Position 4
            { 4, 6 }, // Southwest → Position 6
            { 5, 5 }  // Southeast → Position 5
        };
        
        // Find which tile edge should connect to edgeHex (opposite direction from tile center)
        var connectingEdgePosition = directionToTileEdge.GetValueOrDefault(oppositeDirectionIndex, 1);

        // Check if tile center position is already occupied
        var tileCenterKey = PosKey(tileCenter);
        if (_state.Map.HexData.ContainsKey(tileCenterKey) && _state.Map.RevealedHexes.Contains(tileCenterKey))
        {
            // Try alternative placement - rotate tile
            var alternativeRotation = _random.Next(6);
            // For now, just place it adjacent in a different direction
            var altDirection = (directionIndex + 2) % HexDirections.Length;
            tileCenter = edgeHex + HexDirections[altDirection];
            tileCenterKey = PosKey(tileCenter);
        }

        // Create the tile
        var tile = new MapTileState
        {
            TileId = tileDef.Id,
            Position = tileCenter,
            Rotation = 0, // We'll handle rotation by adjusting edge positions
            IsRevealed = true
        };
        _state.Map.Tiles.Add(tile);

        // Generate hex data for the new tile
        // We need to rotate the tile so that the correct edge hex connects to edgeHex
        // The edge hex at connectingEdgePosition should be at oppositeDirectionIndex from center
        var rotationOffset = CalculateRotationOffset(connectingEdgePosition, oppositeDirectionIndex);
        var tileHexes = GenerateTileHexesWithRotation(tileCenter, tileDef, rotationOffset);
        
        // Track which hexes we're adding for logging
        var addedHexes = new List<string>();
        
        foreach (var (hexPos, hexState) in tileHexes)
        {
            var key = PosKey(hexPos);
            // Don't overwrite existing revealed hexes
            if (!_state.Map.RevealedHexes.Contains(key))
            {
                _state.Map.RevealedHexes.Add(key);
                _state.Map.HexData[key] = hexState;
                addedHexes.Add($"({hexPos.Q},{hexPos.R}):{hexState.Terrain}");
            }
        }
        
        // IMPORTANT: Ensure the connecting hex (unrevealedNeighbor) is definitely revealed
        // This is the hex that bridges the old tile to the new tile
        var connectingKey = PosKey(unrevealedNeighbor);
        if (!_state.Map.RevealedHexes.Contains(connectingKey))
        {
            // The connecting hex wasn't part of the generated hexes - this is a bug!
            // Try to find the correct terrain from the tile
            var connectingHex = tileHexes.FirstOrDefault(h => PosKey(h.Position) == connectingKey);
            if (connectingHex.State != null)
            {
                _state.Map.RevealedHexes.Add(connectingKey);
                _state.Map.HexData[connectingKey] = connectingHex.State;
                addedHexes.Add($"(CONNECTING {unrevealedNeighbor.Q},{unrevealedNeighbor.R})");
            }
            else
            {
                // Fallback - add a default plains hex to ensure connectivity
                _state.Map.RevealedHexes.Add(connectingKey);
                _state.Map.HexData[connectingKey] = new HexState { Terrain = "Plains", SiteType = null, Enemies = new List<string>() };
                addedHexes.Add($"(FALLBACK CONNECTING {unrevealedNeighbor.Q},{unrevealedNeighbor.R}):Plains");
            }
        }

        AddLogEntry("Explore", $"Placed {tileDef.Id} at center ({tileCenter.Q},{tileCenter.R}). Hexes: {string.Join(", ", addedHexes.Take(7))}");
        return GameActionResult.Ok($"Explored new tile: {tileDef.Name ?? tileDef.Id}");
    }

    private int GetOppositeDirection(int directionIndex)
    {
        return (directionIndex + 3) % 6; // Opposite direction in hex grid
    }

    private int CalculateRotationOffset(int tileEdgePosition, int targetDirectionIndex)
    {
        // Calculate how much to rotate the tile so that tileEdgePosition aligns with targetDirectionIndex
        // 
        // Tile positions: 1=East, 2=NW, 3=NE, 4=West, 5=SE, 6=SW
        // Direction indices: 0=East, 1=NE, 2=NW, 3=West, 4=SW, 5=SE
        //
        // Convert tile position to direction index:
        var positionToDirectionIndex = new[] { -1, 0, 2, 1, 3, 5, 4 };
        var baseDirectionIndex = positionToDirectionIndex[tileEdgePosition];
        
        // Calculate rotation needed: how many steps to rotate from base direction to target direction
        int offset = targetDirectionIndex - baseDirectionIndex;
        if (offset < 0) offset += 6;
        return offset % 6;
    }

    private List<(HexPosition Position, HexState State)> GenerateTileHexesWithRotation(HexPosition center, MapTileDefinition tileDef, int rotationOffset)
    {
        var hexes = new List<(HexPosition, HexState)>();
        
        // Tile hex positions according to map_tiles.json.desc
        // Position 0: Center
        // Positions 1-6: Edge hexes in clockwise order starting from East
        //
        // Layout visualization:
        //        (2)   (3)
        //     (1)  (0)  (4)
        //        (6)   (5)
        
        // Position mapping: maps tile definition position (0-6) to base direction
        // Position 1 = East, Position 2 = NW, Position 3 = NE, Position 4 = West, Position 5 = SE, Position 6 = SW
        var positionToBaseDirection = new[]
        {
            new HexPosition { Q = 0, R = 0 },    // 0: Center
            new HexPosition { Q = 1, R = 0 },    // 1: East
            new HexPosition { Q = 0, R = -1 },   // 2: NW
            new HexPosition { Q = 1, R = -1 },   // 3: NE
            new HexPosition { Q = -1, R = 0 },   // 4: West
            new HexPosition { Q = 0, R = 1 },    // 5: SE
            new HexPosition { Q = -1, R = 1 }    // 6: SW
        };
        
        // HexDirections are in order: East(0), NE(1), NW(2), West(3), SW(4), SE(5)
        // We need to map position indices to HexDirections for rotation:
        // Position 1 (East) -> Direction 0
        // Position 3 (NE) -> Direction 1
        // Position 2 (NW) -> Direction 2
        // Position 4 (West) -> Direction 3
        // Position 6 (SW) -> Direction 4
        // Position 5 (SE) -> Direction 5
        var positionToDirectionIndex = new[] { -1, 0, 2, 1, 3, 5, 4 }; // position -> direction index
        var directionIndexToPosition = new[] { 1, 3, 2, 4, 6, 5 }; // direction index -> position

        foreach (var hexDef in tileDef.Hexes)
        {
            HexPosition hexPos;
            
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
            
            var key = PosKey(hexPos);
            
            // Don't overwrite existing hexes
            if (!_state.Map.HexData.ContainsKey(key) || !_state.Map.RevealedHexes.Contains(key))
            {
                hexes.Add((hexPos, new HexState
                {
                    Terrain = hexDef.Terrain,
                    SiteType = hexDef.Site,
                    Enemies = GenerateEnemiesForSite(hexDef.Site)
                }));
            }
        }

        return hexes;
    }

    private List<(HexPosition Position, HexState State)> GenerateTileHexes(HexPosition center, string tileId)
    {
        var hexes = new List<(HexPosition, HexState)>();
        
        // Try to get tile definition from JSON
        // Handle both formats: "tile_02_countryside" and "countryside_1"
        var tileDef = _definitions.GetMapTilesAsync().Result.FirstOrDefault(t => 
            t.Id == tileId || 
            (tileId.StartsWith("countryside_", StringComparison.OrdinalIgnoreCase) && 
             t.BackType == "Countryside") ||
            (tileId.StartsWith("core_", StringComparison.OrdinalIgnoreCase) && 
             t.BackType == "Core") ||
            (tileId.StartsWith("city_", StringComparison.OrdinalIgnoreCase) && 
             t.BackType == "City"));
        
        // If exact match not found but we have a type match, use a random tile of that type
        if (tileDef == null)
        {
            var allTiles = _definitions.GetMapTilesAsync().Result.ToList();
            if (tileId.StartsWith("countryside_", StringComparison.OrdinalIgnoreCase))
            {
                var countrysideTiles = allTiles.Where(t => t.BackType == "Countryside" && !t.IsStartingTile).ToList();
                if (countrysideTiles.Any())
                {
                    tileDef = countrysideTiles[_random.Next(countrysideTiles.Count)];
                }
            }
            else if (tileId.StartsWith("core_", StringComparison.OrdinalIgnoreCase))
            {
                var coreTiles = allTiles.Where(t => t.BackType == "Core" && !t.IsStartingTile).ToList();
                if (coreTiles.Any())
                {
                    tileDef = coreTiles[_random.Next(coreTiles.Count)];
                }
            }
            else if (tileId.StartsWith("city_", StringComparison.OrdinalIgnoreCase))
            {
                var cityTiles = allTiles.Where(t => t.BackType == "City" && !t.IsStartingTile).ToList();
                if (cityTiles.Any())
                {
                    tileDef = cityTiles[_random.Next(cityTiles.Count)];
                }
            }
        }
        
        if (tileDef != null && tileDef.Hexes.Any())
        {
            // Use actual tile definition
            foreach (var hexDef in tileDef.Hexes)
            {
                var hexPos = GetHexPositionFromTileIndex(center, hexDef.Position);
                var key = PosKey(hexPos);
                
                // Don't overwrite existing hexes
                if (!_state.Map.HexData.ContainsKey(key))
                {
                    hexes.Add((hexPos, new HexState
                    {
                        Terrain = hexDef.Terrain,
                        SiteType = hexDef.Site,
                        Enemies = GenerateEnemiesForSite(hexDef.Site)
                    }));
                }
            }
        }
        else
        {
            // Fallback: generate semi-random terrain
            var terrains = new[] { "Plains", "Forest", "Hills", "Swamp", "Wasteland" };
            var sites = new[] { "Village", "Monastery", "Keep", "MageTower", "AncientRuins", null, null, null, null, null };
            
            // Center hex
            var centerTerrain = terrains[_random.Next(terrains.Length)];
            var centerSite = sites[_random.Next(sites.Length)];
            hexes.Add((center, new HexState
            {
                Terrain = centerTerrain,
                SiteType = centerSite,
                Enemies = GenerateEnemiesForSite(centerSite)
            }));

            // Surrounding 6 hexes
            foreach (var dir in HexDirections)
            {
                var pos = center + dir;
                var terrain = terrains[_random.Next(terrains.Length)];
                var site = sites[_random.Next(sites.Length)];
                
                // Don't overwrite existing hexes
                if (!_state.Map.HexData.ContainsKey(PosKey(pos)))
                {
                    hexes.Add((pos, new HexState
                    {
                        Terrain = terrain,
                        SiteType = site,
                        Enemies = GenerateEnemiesForSite(site)
                    }));
                }
            }
        }

        return hexes;
    }

    private HexPosition GetHexPositionFromTileIndex(HexPosition center, int index)
    {
        // Convert tile hex index (0-6) to actual hex position
        // According to map_tiles.json.desc (rotated one step counter-clockwise to match images):
        // 0: Center
        // 1: Top (Kl 12) → maps to NW direction
        // 2: Top-Right (Kl 2) → maps to N direction
        // 3: Bottom-Right (Kl 4) → maps to NE direction
        // 4: Bottom (Kl 6) → maps to E direction
        // 5: Bottom-Left (Kl 8) → maps to S direction
        // 6: Top-Left (Kl 10) → maps to SW direction
        //
        // Layout visualization (after rotation):
        //        (2)   (3)
        //     (1)  (0)  (4)
        //        (6)   (5)
        
        return index switch
        {
            0 => center,                                        // Center
            1 => center + new HexPosition { Q = -1, R = 0 },    // Top → West/NW
            2 => center + new HexPosition { Q = 0, R = -1 },    // Top-Right → North
            3 => center + new HexPosition { Q = 1, R = -1 },    // Bottom-Right → NE
            4 => center + new HexPosition { Q = 1, R = 0 },     // Bottom → East
            5 => center + new HexPosition { Q = 0, R = 1 },     // Bottom-Left → South
            6 => center + new HexPosition { Q = -1, R = 1 },    // Top-Left → SW
            _ => center
        };
    }

    private List<string> GenerateEnemiesForSite(string? siteType, bool isCity = false)
    {
        if (string.IsNullOrEmpty(siteType)) return new List<string>();

        var enemies = new List<string>();
        
        // Special handling for cities - use city level
        if (siteType == "City" || isCity)
        {
            return GenerateCityDefenders();
        }
        
        // Determine enemy type and count based on site type
        var (enemyType, count) = siteType switch
        {
            // Friendly sites - no enemies
            "Village" => (null, 0),
            "Monastery" => (null, 0),
            "Portal" => (null, 0),
            "MagicalGlade" => (null, 0),
            "Mine_Red" => (null, 0),
            "Mine_Blue" => (null, 0),
            "Mine_Green" => (null, 0),
            "Mine_White" => (null, 0),
            
            // Fortified sites with defenders
            "Keep" => ("Grey", 1),
            "MageTower" => ("Violet", 1),
            
            // Adventure sites
            "Dungeon" => ("Brown", 1),
            "Tomb" => ("Red", 1),
            "MonsterDen" => ("Brown", 1),
            "SpawningGrounds" => ("Brown", 2),
            "Ruins" => (null, 0), // Ruins use tokens, handled separately
            "AncientRuins" => (null, 0), // Same as Ruins
            
            // Rampaging enemies (on map, blocking movement)
            "OrcMarauders" => ("Green", 1),
            "Draconum" => ("Red", 1),
            
            _ => (null, 0)
        };

        if (enemyType == null || count == 0)
            return enemies;

        // Get random enemies of the specified type
        var availableEnemies = _definitions.GetEnemiesByTypeAsync(enemyType).Result?.ToList();
        if (availableEnemies == null || !availableEnemies.Any())
            return enemies;

        for (int i = 0; i < count; i++)
        {
            var randomEnemy = availableEnemies[_random.Next(availableEnemies.Count)];
            enemies.Add(randomEnemy.Id);
        }

        return enemies;
    }

    /// <summary>
    /// Generates defenders for a city based on its level from the scenario.
    /// City level determines the number of defenders.
    /// </summary>
    private List<string> GenerateCityDefenders()
    {
        var enemies = new List<string>();
        
        // Get the next city level from the deck state
        var cityLevel = 3; // Default level if not specified
        if (_state.Decks.CityLevels.Any() && _state.Decks.NextCityIndex < _state.Decks.CityLevels.Count)
        {
            cityLevel = _state.Decks.CityLevels[_state.Decks.NextCityIndex];
            _state.Decks.NextCityIndex++;
        }
        
        AddLogEntry("CityRevealed", $"City revealed with level {cityLevel}!");
        
        // City defenders based on level:
        // Level 2-3: 2 White enemies
        // Level 4-5: 3 White enemies (1 in front, 2 behind)
        // Level 6-7: 4 enemies (2 White in front, 2 Grey behind)
        // Level 8+: 5 enemies (2 White + 1 Grey in front, 2 Grey behind)
        
        var whiteEnemies = _definitions.GetEnemiesByTypeAsync("White").Result?.ToList() ?? new List<EnemyDefinition>();
        var greyEnemies = _definitions.GetEnemiesByTypeAsync("Grey").Result?.ToList() ?? new List<EnemyDefinition>();
        
        int whiteCount = cityLevel switch
        {
            <= 3 => 2,
            <= 5 => 2,
            <= 7 => 2,
            _ => 3
        };
        
        int greyCount = cityLevel switch
        {
            <= 3 => 0,
            <= 5 => 1,
            <= 7 => 2,
            _ => 2
        };
        
        // Add white enemies
        for (int i = 0; i < whiteCount && whiteEnemies.Any(); i++)
        {
            var enemy = whiteEnemies[_random.Next(whiteEnemies.Count)];
            enemies.Add(enemy.Id);
        }
        
        // Add grey enemies
        for (int i = 0; i < greyCount && greyEnemies.Any(); i++)
        {
            var enemy = greyEnemies[_random.Next(greyEnemies.Count)];
            enemies.Add(enemy.Id);
        }
        
        return enemies;
    }

    #region Undo System

    /// <summary>
    /// Saves the current state to the undo stack before an action.
    /// </summary>
    private void SaveStateForUndo()
    {
        if (!_state.CanUndo) return;
        
        // Create a copy of state without the undo stack itself to avoid recursion
        var stateCopy = JsonSerializer.Deserialize<GameStateModel>(JsonSerializer.Serialize(_state))!;
        stateCopy.UndoStack = new List<string>(); // Don't include undo stack in saved state
        var stateJson = JsonSerializer.Serialize(stateCopy);
        
        _state.UndoStack.Insert(0, stateJson); // Add to front (most recent first)
        
        // Limit stack size
        while (_state.UndoStack.Count > MaxUndoHistory)
        {
            _state.UndoStack.RemoveAt(_state.UndoStack.Count - 1);
        }
    }

    /// <summary>
    /// Marks that an irreversible action has been taken (e.g., revealing tiles).
    /// </summary>
    private void MarkIrreversibleAction()
    {
        _state.CanUndo = false;
        _state.UndoStack.Clear();
    }

    /// <summary>
    /// Resets undo availability at the start of a new turn.
    /// </summary>
    private void ResetUndoForNewTurn()
    {
        _state.CanUndo = true;
        _state.UndoStack.Clear();
    }

    /// <summary>
    /// Checks if undo is currently available.
    /// </summary>
    public bool CanUndoAction()
    {
        return _state.CanUndo && _state.UndoStack.Count > 0;
    }

    /// <summary>
    /// Undoes the last action, restoring the previous state.
    /// </summary>
    public GameActionResult UndoLastAction()
    {
        if (!_state.CanUndo)
            return GameActionResult.Fail("Cannot undo - irreversible action has been taken (e.g., explored new tile)");
        
        if (_state.UndoStack.Count == 0)
            return GameActionResult.Fail("Nothing to undo");
        
        var previousStateJson = _state.UndoStack[0];
        _state.UndoStack.RemoveAt(0);
        
        var previousState = JsonSerializer.Deserialize<GameStateModel>(previousStateJson);
        
        if (previousState == null)
            return GameActionResult.Fail("Failed to restore previous state");
        
        // Preserve the current undo stack and canUndo status
        var currentUndoStack = _state.UndoStack;
        var currentCanUndo = _state.CanUndo;
        
        _state = previousState;
        _state.UndoStack = currentUndoStack;
        _state.CanUndo = currentCanUndo;
        
        AddLogEntry("Undo", "Undid last action");
        return GameActionResult.Ok("Action undone");
    }

    #endregion

    public GameActionResult PlayCard(string cardId, bool powered = false, ManaColor? manaUsed = null)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (!player.Hand.Contains(cardId))
            return GameActionResult.Fail("Card not in hand");

        // Save state for undo before playing card
        SaveStateForUndo();

        // Get card definition
        var card = _definitions.GetBasicActionsAsync().Result.FirstOrDefault(c => c.Id == cardId)
                   ?? _definitions.GetAdvancedActionsAsync().Result.FirstOrDefault(c => c.Id == cardId);

        if (card == null)
            return GameActionResult.Fail("Card not found");

        // Check if powered effect requires mana
        if (powered && card.EffectsPowered?.Any() == true)
        {
            // Check for mana availability: TemporaryMana first, then mana tokens
            ManaColor? manaToUse = null;
            bool usingTemporaryMana = false;
            bool usingManaToken = false;
            
            var requiredColor = !string.IsNullOrEmpty(card.ManaType) ? ParseManaColor(card.ManaType) : (ManaColor?)null;
            
            // 1. Check temporary mana (from Source die)
            if (player.TemporaryMana.HasValue)
            {
                var tempMana = player.TemporaryMana.Value;
                if (requiredColor == null || tempMana == requiredColor || tempMana == ManaColor.Gold)
                {
                    manaToUse = tempMana;
                    usingTemporaryMana = true;
                }
            }
            
            // 2. If no suitable temporary mana, check mana tokens
            if (manaToUse == null)
            {
                // Check if we have a matching mana token (or any if no color requirement)
                if (requiredColor.HasValue)
                {
                    // Need specific color - check gold first, then the required color
                    if (player.ManaTokens.Gold > 0)
                    {
                        manaToUse = ManaColor.Gold;
                        usingManaToken = true;
                    }
                    else if (HasManaToken(player, requiredColor.Value))
                    {
                        manaToUse = requiredColor.Value;
                        usingManaToken = true;
                    }
                }
                else
                {
                    // Any color works - use the first available token
                    if (player.ManaTokens.Red > 0) { manaToUse = ManaColor.Red; usingManaToken = true; }
                    else if (player.ManaTokens.Blue > 0) { manaToUse = ManaColor.Blue; usingManaToken = true; }
                    else if (player.ManaTokens.Green > 0) { manaToUse = ManaColor.Green; usingManaToken = true; }
                    else if (player.ManaTokens.White > 0) { manaToUse = ManaColor.White; usingManaToken = true; }
                    else if (player.ManaTokens.Black > 0) { manaToUse = ManaColor.Black; usingManaToken = true; }
                    else if (player.ManaTokens.Gold > 0) { manaToUse = ManaColor.Gold; usingManaToken = true; }
                }
            }
            
            if (manaToUse == null)
            {
                var manaRequirement = requiredColor.HasValue ? requiredColor.Value.ToString() : "any";
                return GameActionResult.Fail($"Powered effect requires {manaRequirement} mana. Take a mana die from Source or use a mana token.");
            }
            
            // Consume the mana
            if (usingTemporaryMana)
            {
                player.TemporaryMana = null;
            }
            else if (usingManaToken)
            {
                ConsumeManaToken(player, manaToUse.Value);
            }
        }

        // Remove card from hand, add to discard
        player.Hand.Remove(cardId);
        player.DiscardPile.Add(cardId);

        // Get the effects to apply (basic or powered)
        var effects = powered ? (card.EffectsPowered ?? card.EffectsBasic) : card.EffectsBasic;
        
        if (effects == null || effects.Count == 0)
        {
            AddLogEntry("PlayCard", $"Played {card.Name} (no effect)");
            return GameActionResult.Ok($"Played {card.Name}");
        }

        // Apply each effect
        var effectDescriptions = new List<string>();
        foreach (var effect in effects)
        {
            var value = effect.Value ?? 0;
            var effectType = effect.Type?.ToLower() ?? "";
            var desc = effect.Description ?? "";

            // Check for OR choices in description first
            if (desc.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
            {
                var orChoice = CreateOrChoicePendingChoice(card, effect, value, powered);
                if (orChoice != null)
                {
                    _state.PendingChoice = orChoice;
                    AddLogEntry("PlayCard", $"Played {card.Name} - choose effect");
                    return GameActionResult.Ok($"Played {card.Name} - choose effect");
                }
            }

            switch (effectType)
            {
                case "move":
                    player.MovementRemaining += value;
                    effectDescriptions.Add($"+{value} Move");
                    break;
                    
                case "flight":
                case "fly":
                    player.FlightRemaining += value;
                    effectDescriptions.Add($"+{value} Flight");
                    break;
                    
                case "safe_move":
                case "safe":
                    player.SafeMovementRemaining += value;
                    effectDescriptions.Add($"+{value} Safe Move");
                    break;
                    
                case "attack":
                    // Check for OR in description
                    if (desc.Contains("OR Block", StringComparison.OrdinalIgnoreCase))
                    {
                        var blockValue = ExtractValueFromOrDescription(desc, "Block");
                        var isRanged = effect.Attributes?.Contains("Ranged") == true;
                        _state.PendingChoice = new PendingChoice
                        {
                            Type = ChoiceType.EffectType,
                            CardId = cardId,
                            CardName = card.Name,
                            Description = $"Choose: {(isRanged ? "Ranged " : "")}Attack {value} OR Block {blockValue}",
                            EffectValue = value,
                            Options = new List<ChoiceOption>
                            {
                                new() { Id = isRanged ? "ranged_attack" : "attack", Name = isRanged ? "Ranged Attack" : "Attack", Description = $"+{value} {(isRanged ? "Ranged " : "")}Attack" },
                                new() { Id = "block", Name = "Block", Description = $"+{blockValue} Block" }
                            }
                        };
                        // Store the alternative value for the choice resolver
                        _state.PendingChoice.Options[1].Id = $"block_{blockValue}";
                        AddLogEntry("PlayCard", $"Played {card.Name} - choose Attack or Block");
                        return GameActionResult.Ok($"Played {card.Name} - choose Attack or Block");
                    }
                    player.AttackPool += value;
                    // Check for element attributes
                    if (effect.Attributes?.Contains("Fire") == true)
                        player.AttackElements.Add("Fire");
                    if (effect.Attributes?.Contains("Ice") == true)
                        player.AttackElements.Add("Ice");
                    if (effect.Attributes?.Contains("ColdFire") == true)
                        player.AttackElements.Add("ColdFire");
                    // Check for Ranged attribute
                    if (effect.Attributes?.Contains("Ranged") == true)
                        player.RangedAttack += value;
                    else
                        effectDescriptions.Add($"+{value} Attack");
                    break;
                    
                case "ranged_attack":
                case "ranged":
                    player.RangedAttack += value;
                    effectDescriptions.Add($"+{value} Ranged Attack");
                    break;
                    
                case "siege_attack":
                case "siege":
                    player.SiegeAttack += value;
                    effectDescriptions.Add($"+{value} Siege Attack");
                    break;
                    
                case "block":
                    // Check for OR in description
                    if (desc.Contains("OR Attack", StringComparison.OrdinalIgnoreCase))
                    {
                        var attackValue = ExtractValueFromOrDescription(desc, "Attack");
                        _state.PendingChoice = new PendingChoice
                        {
                            Type = ChoiceType.EffectType,
                            CardId = cardId,
                            CardName = card.Name,
                            Description = $"Choose: Block {value} OR Attack {attackValue}",
                            EffectValue = value,
                            Options = new List<ChoiceOption>
                            {
                                new() { Id = $"block_{value}", Name = "Block", Description = $"+{value} Block" },
                                new() { Id = $"attack_{attackValue}", Name = "Attack", Description = $"+{attackValue} Attack" }
                            }
                        };
                        AddLogEntry("PlayCard", $"Played {card.Name} - choose Block or Attack");
                        return GameActionResult.Ok($"Played {card.Name} - choose Block or Attack");
                    }
                    player.BlockPool += value;
                    effectDescriptions.Add($"+{value} Block");
                    break;
                    
                case "influence":
                    // Check for OR in description (Learning: "OR Block 2")
                    if (desc.Contains("OR Block", StringComparison.OrdinalIgnoreCase))
                    {
                        var blockValue = ExtractValueFromOrDescription(desc, "Block");
                        _state.PendingChoice = new PendingChoice
                        {
                            Type = ChoiceType.EffectType,
                            CardId = cardId,
                            CardName = card.Name,
                            Description = $"Choose: Influence {value} OR Block {blockValue}",
                            EffectValue = value,
                            Options = new List<ChoiceOption>
                            {
                                new() { Id = $"influence_{value}", Name = "Influence", Description = $"+{value} Influence" },
                                new() { Id = $"block_{blockValue}", Name = "Block", Description = $"+{blockValue} Block" }
                            }
                        };
                        AddLogEntry("PlayCard", $"Played {card.Name} - choose Influence or Block");
                        return GameActionResult.Ok($"Played {card.Name} - choose Influence or Block");
                    }
                    player.InfluencePool += value;
                    effectDescriptions.Add($"+{value} Influence");
                    break;
                    
                case "heal":
                    player.HealPool += value;
                    effectDescriptions.Add($"+{value} Heal");
                    break;
                    
                case "draw":
                case "drawcard":
                    // Check if this is Improvisation (requires discard)
                    if (desc.Contains("Discard a card", StringComparison.OrdinalIgnoreCase))
                    {
                        // Set up pending choice for Improvisation
                        _state.PendingChoice = new PendingChoice
                        {
                            Type = ChoiceType.DiscardForEffect,
                            CardId = cardId,
                            CardName = card.Name,
                            Description = $"Discard a card to gain Move/Attack/Block/Influence {value}",
                            RequiresDiscard = true,
                            EffectValue = value > 0 ? value : (powered ? 5 : 3), // Improvisation: basic 3, powered 5
                            Options = new List<ChoiceOption>
                            {
                                new() { Id = "move", Name = "Move", Description = $"+{(value > 0 ? value : (powered ? 5 : 3))} Move" },
                                new() { Id = "attack", Name = "Attack", Description = $"+{(value > 0 ? value : (powered ? 5 : 3))} Attack" },
                                new() { Id = "block", Name = "Block", Description = $"+{(value > 0 ? value : (powered ? 5 : 3))} Block" },
                                new() { Id = "influence", Name = "Influence", Description = $"+{(value > 0 ? value : (powered ? 5 : 3))} Influence" }
                            }
                        };
                        AddLogEntry("PlayCard", $"Played {card.Name} - choose a card to discard");
                        return GameActionResult.Ok($"Played {card.Name} - choose a card to discard and select effect type");
                    }
                    else
                    {
                        // Normal draw cards
                        for (int i = 0; i < value && player.DeedDeck.Count > 0; i++)
                        {
                            var drawnCard = player.DeedDeck[0];
                            player.DeedDeck.RemoveAt(0);
                            player.Hand.Add(drawnCard);
                        }
                        effectDescriptions.Add($"Draw {value} card(s)");
                    }
                    break;

                case "gainmana":
                    // Choose a mana color - handle multiple mana tokens
                    _state.PendingChoice = new PendingChoice
                    {
                        Type = ChoiceType.ManaColor,
                        CardId = cardId,
                        CardName = card.Name,
                        Description = value > 1 ? $"Choose {value} mana colors to gain" : "Choose a mana color to gain",
                        EffectValue = value,
                        Options = new List<ChoiceOption>
                        {
                            new() { Id = "red", Name = "Red Mana", Description = $"Gain {value} Red mana token(s)" },
                            new() { Id = "blue", Name = "Blue Mana", Description = $"Gain {value} Blue mana token(s)" },
                            new() { Id = "green", Name = "Green Mana", Description = $"Gain {value} Green mana token(s)" },
                            new() { Id = "white", Name = "White Mana", Description = $"Gain {value} White mana token(s)" }
                        }
                    };
                    AddLogEntry("PlayCard", $"Played {card.Name} - choose mana color");
                    return GameActionResult.Ok($"Played {card.Name} - choose mana color");

                case "gaincrystal":
                    // Choose a crystal color - handle Blood Ritual (Take 1 Wound) and multiple crystals
                    var crystalCount = value > 0 ? value : 1;
                    var extraEffect = desc.Contains("Take 1 Wound", StringComparison.OrdinalIgnoreCase) 
                        ? " (Take 1 Wound)" : "";
                    _state.PendingChoice = new PendingChoice
                    {
                        Type = ChoiceType.ManaColor,
                        CardId = cardId,
                        CardName = card.Name,
                        Description = $"Choose a crystal color to gain{extraEffect}",
                        EffectValue = crystalCount,
                        Options = new List<ChoiceOption>
                        {
                            new() { Id = "red_crystal", Name = "Red Crystal", Description = $"Gain {crystalCount} Red crystal(s){extraEffect}" },
                            new() { Id = "blue_crystal", Name = "Blue Crystal", Description = $"Gain {crystalCount} Blue crystal(s){extraEffect}" },
                            new() { Id = "green_crystal", Name = "Green Crystal", Description = $"Gain {crystalCount} Green crystal(s){extraEffect}" },
                            new() { Id = "white_crystal", Name = "White Crystal", Description = $"Gain {crystalCount} White crystal(s){extraEffect}" }
                        }
                    };
                    // Flag if this card causes a wound (Blood Ritual)
                    if (desc.Contains("Take 1 Wound", StringComparison.OrdinalIgnoreCase))
                    {
                        _state.PendingChoice.RequiresDiscard = false; // Reuse this flag to indicate wound
                        _state.PendingChoice.Description += "\n⚠️ This will cause 1 Wound!";
                    }
                    AddLogEntry("PlayCard", $"Played {card.Name} - choose crystal color");
                    return GameActionResult.Ok($"Played {card.Name} - choose crystal color");
                    
                default:
                    // Check for Tranquility-style OR effects
                    if (effect.Description?.Contains(" OR ") == true)
                    {
                        // Parse the OR options
                        _state.PendingChoice = new PendingChoice
                        {
                            Type = ChoiceType.HealOrDraw,
                            CardId = cardId,
                            CardName = card.Name,
                            Description = effect.Description,
                            EffectValue = value,
                            Options = new List<ChoiceOption>
                            {
                                new() { Id = "heal", Name = "Heal", Description = $"+{value} Heal" },
                                new() { Id = "draw", Name = "Draw Card", Description = "Draw 1 card" }
                            }
                        };
                        AddLogEntry("PlayCard", $"Played {card.Name} - choose effect");
                        return GameActionResult.Ok($"Played {card.Name} - choose effect");
                    }
                    if (!string.IsNullOrEmpty(effect.Description))
                        effectDescriptions.Add(effect.Description);
                    break;
            }
        }

        var description = string.Join(", ", effectDescriptions);
        AddLogEntry("PlayCard", $"Played {card.Name}{(powered ? " (powered)" : "")}: {description}");
        return GameActionResult.Ok($"Played {card.Name}: {description}");
    }

    /// <summary>
    /// Extracts a numeric value from an OR description like "OR Block 3" or "OR Attack 5"
    /// </summary>
    private int ExtractValueFromOrDescription(string description, string effectType)
    {
        // Pattern: "OR Block 3" or "OR Attack 5"
        var pattern = $@"OR\s+{effectType}\s+(\d+)";
        var match = System.Text.RegularExpressions.Regex.Match(description, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
        {
            return value;
        }
        return 0;
    }

    /// <summary>
    /// Creates a pending choice for OR-based effects from card description
    /// </summary>
    private PendingChoice? CreateOrChoicePendingChoice(CardDefinition card, CardEffect effect, int value, bool powered)
    {
        var desc = effect.Description ?? "";
        var effectType = effect.Type?.ToLower() ?? "";
        
        // Move X OR Block Y (Dodge and Weave)
        if (effectType == "move" && desc.Contains("OR Block", StringComparison.OrdinalIgnoreCase))
        {
            var blockValue = ExtractValueFromOrDescription(desc, "Block");
            return new PendingChoice
            {
                Type = ChoiceType.EffectType,
                CardId = card.Id,
                CardName = card.Name,
                Description = $"Choose: Move {value} OR Block {blockValue}",
                EffectValue = value,
                Options = new List<ChoiceOption>
                {
                    new() { Id = $"move_{value}", Name = "Move", Description = $"+{value} Move" },
                    new() { Id = $"block_{blockValue}", Name = "Block", Description = $"+{blockValue} Block" }
                }
            };
        }
        
        // Heal X OR Draw Y (Tranquility basic)
        if (effectType == "heal" && desc.Contains("OR Draw", StringComparison.OrdinalIgnoreCase))
        {
            var drawValue = ExtractValueFromOrDescription(desc, "Draw");
            if (drawValue == 0) drawValue = 1; // Default to 1 card
            return new PendingChoice
            {
                Type = ChoiceType.HealOrDraw,
                CardId = card.Id,
                CardName = card.Name,
                Description = $"Choose: Heal {value} OR Draw {drawValue} card(s)",
                EffectValue = value,
                Options = new List<ChoiceOption>
                {
                    new() { Id = $"heal_{value}", Name = "Heal", Description = $"+{value} Heal" },
                    new() { Id = $"draw_{drawValue}", Name = "Draw Cards", Description = $"Draw {drawValue} card(s)" }
                }
            };
        }
        
        return null;
    }

    private ManaColor ParseManaColor(string colorName)
    {
        return colorName?.ToLower() switch
        {
            "red" => ManaColor.Red,
            "blue" => ManaColor.Blue,
            "green" => ManaColor.Green,
            "white" => ManaColor.White,
            "black" => ManaColor.Black,
            "gold" => ManaColor.Gold,
            _ => ManaColor.Red
        };
    }

    private bool HasManaToken(PlayerState player, ManaColor color)
    {
        return color switch
        {
            ManaColor.Red => player.ManaTokens.Red > 0,
            ManaColor.Blue => player.ManaTokens.Blue > 0,
            ManaColor.Green => player.ManaTokens.Green > 0,
            ManaColor.White => player.ManaTokens.White > 0,
            ManaColor.Black => player.ManaTokens.Black > 0,
            ManaColor.Gold => player.ManaTokens.Gold > 0,
            _ => false
        };
    }

    private void ConsumeManaToken(PlayerState player, ManaColor color)
    {
        switch (color)
        {
            case ManaColor.Red: player.ManaTokens.Red--; break;
            case ManaColor.Blue: player.ManaTokens.Blue--; break;
            case ManaColor.Green: player.ManaTokens.Green--; break;
            case ManaColor.White: player.ManaTokens.White--; break;
            case ManaColor.Black: player.ManaTokens.Black--; break;
            case ManaColor.Gold: player.ManaTokens.Gold--; break;
        }
    }

    /// <summary>
    /// Uses a mana token from inventory as temporary mana for powering cards.
    /// </summary>
    public GameActionResult UseManaToken(ManaColor color)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Check if player already has temporary mana
        if (player.TemporaryMana.HasValue)
            return GameActionResult.Fail($"You already have {player.TemporaryMana.Value} mana active. Use it first or undo.");

        if (!HasManaToken(player, color))
            return GameActionResult.Fail($"You don't have any {color} mana tokens.");

        SaveStateForUndo();
        
        ConsumeManaToken(player, color);
        player.TemporaryMana = color;
        
        AddLogEntry("UseManaToken", $"Activated {color} mana token");
        return GameActionResult.Ok($"Activated {color} mana token - ready to power a card!");
    }

    public GameActionResult UseCardSideways(string cardId, string bonusType = "move")
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (!player.Hand.Contains(cardId))
            return GameActionResult.Fail("Card not in hand");

        // Get card definition
        var card = _definitions.GetBasicActionsAsync().Result.FirstOrDefault(c => c.Id == cardId)
                   ?? _definitions.GetAdvancedActionsAsync().Result.FirstOrDefault(c => c.Id == cardId);

        if (card == null)
            return GameActionResult.Fail("Card not found");

        // Save state for undo before using card
        SaveStateForUndo();

        // Remove card from hand, add to discard
        player.Hand.Remove(cardId);
        player.DiscardPile.Add(cardId);

        // Apply +1 based on player's choice
        var effectType = bonusType.ToLower() switch
        {
            "move" => ApplySidewaysBonus(player, "Move", () => player.MovementRemaining += 1),
            "attack" => ApplySidewaysBonus(player, "Attack", () => player.AttackPool += 1),
            "block" => ApplySidewaysBonus(player, "Block", () => player.BlockPool += 1),
            "influence" => ApplySidewaysBonus(player, "Influence", () => player.InfluencePool += 1),
            "heal" => ApplySidewaysBonus(player, "Heal", () => player.HealPool += 1),
            _ => ApplySidewaysBonus(player, "Move", () => player.MovementRemaining += 1)
        };

        AddLogEntry("Sideways", $"Used {card.Name} sideways for +1 {effectType}");
        return GameActionResult.Ok($"Used {card.Name} sideways for +1 {effectType}");
    }

    private string ApplySidewaysBonus(PlayerState player, string effectType, Action applyBonus)
    {
        applyBonus();
        return effectType;
    }

    /// <summary>
    /// Resolves a pending choice made by the player.
    /// </summary>
    public GameActionResult ResolveChoice(string choiceId, string? discardCardId = null)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.PendingChoice == null)
            return GameActionResult.Fail("No pending choice to resolve");

        var choice = _state.PendingChoice;
        var result = choice.Type switch
        {
            ChoiceType.ManaColor => ResolveManaColorChoice(player, choice, choiceId),
            ChoiceType.EffectType => ResolveEffectTypeChoice(player, choice, choiceId),
            ChoiceType.HealOrDraw => ResolveHealOrDrawChoice(player, choice, choiceId),
            ChoiceType.DiscardForEffect => ResolveDiscardForEffectChoice(player, choice, choiceId, discardCardId),
            ChoiceType.UnitFromOffer => RecruitUnit(choiceId),
            ChoiceType.SpellFromOffer => LearnSpell(choiceId),
            ChoiceType.AdvancedActionFromOffer => Training(choiceId),
            _ => GameActionResult.Fail("Unknown choice type")
        };

        if (result.Success)
        {
            _state.PendingChoice = null;
        }

        return result;
    }

    private GameActionResult ResolveManaColorChoice(PlayerState player, PendingChoice choice, string colorId)
    {
        // Parse colorId - could be "red", "blue_crystal", "green", etc.
        var parts = colorId.ToLower().Split('_');
        var colorName = parts[0];
        var isCrystal = parts.Length > 1 && parts[1] == "crystal" || 
                        choice.Description.Contains("crystal", StringComparison.OrdinalIgnoreCase);
        
        var color = colorName switch
        {
            "red" => ManaColor.Red,
            "blue" => ManaColor.Blue,
            "green" => ManaColor.Green,
            "white" => ManaColor.White,
            _ => ManaColor.Red
        };

        var count = choice.EffectValue > 0 ? choice.EffectValue : 1;

        if (isCrystal)
        {
            // Add crystals
            for (int i = 0; i < count; i++)
            {
                switch (color)
                {
                    case ManaColor.Red: player.Crystals.Red++; break;
                    case ManaColor.Blue: player.Crystals.Blue++; break;
                    case ManaColor.Green: player.Crystals.Green++; break;
                    case ManaColor.White: player.Crystals.White++; break;
                }
            }
            
            // Check for Blood Ritual - take 1 wound
            if (choice.Description.Contains("Wound", StringComparison.OrdinalIgnoreCase))
            {
                player.Hand.Add("wound");
                AddLogEntry("Choice", $"Gained {count} {color} crystal(s), took 1 wound");
                return GameActionResult.Ok($"Gained {count} {color} crystal(s), took 1 wound");
            }
            
            AddLogEntry("Choice", $"Gained {count} {color} crystal(s)");
            return GameActionResult.Ok($"Gained {count} {color} crystal(s)");
        }
        else
        {
            // Add mana tokens
            for (int i = 0; i < count; i++)
            {
                switch (color)
                {
                    case ManaColor.Red: player.ManaTokens.Red++; break;
                    case ManaColor.Blue: player.ManaTokens.Blue++; break;
                    case ManaColor.Green: player.ManaTokens.Green++; break;
                    case ManaColor.White: player.ManaTokens.White++; break;
                }
            }
            AddLogEntry("Choice", $"Gained {count} {color} mana token(s)");
            return GameActionResult.Ok($"Gained {count} {color} mana token(s)");
        }
    }

    private GameActionResult ResolveEffectTypeChoice(PlayerState player, PendingChoice choice, string effectId)
    {
        // Parse effectId - could be "move", "attack", "block_3", "attack_5", etc.
        var parts = effectId.ToLower().Split('_');
        var effectType = parts[0];
        var value = parts.Length > 1 && int.TryParse(parts[1], out var v) ? v : choice.EffectValue;
        
        switch (effectType)
        {
            case "move":
                player.MovementRemaining += value;
                AddLogEntry("Choice", $"+{value} Move from {choice.CardName}");
                return GameActionResult.Ok($"+{value} Move");
            case "attack":
                player.AttackPool += value;
                AddLogEntry("Choice", $"+{value} Attack from {choice.CardName}");
                return GameActionResult.Ok($"+{value} Attack");
            case "ranged":
                player.RangedAttack += value;
                AddLogEntry("Choice", $"+{value} Ranged Attack from {choice.CardName}");
                return GameActionResult.Ok($"+{value} Ranged Attack");
            case "block":
                player.BlockPool += value;
                AddLogEntry("Choice", $"+{value} Block from {choice.CardName}");
                return GameActionResult.Ok($"+{value} Block");
            case "influence":
                player.InfluencePool += value;
                AddLogEntry("Choice", $"+{value} Influence from {choice.CardName}");
                return GameActionResult.Ok($"+{value} Influence");
            default:
                return GameActionResult.Fail("Invalid effect type");
        }
    }

    private GameActionResult ResolveHealOrDrawChoice(PlayerState player, PendingChoice choice, string effectId)
    {
        // Parse effectId - could be "heal", "draw", "heal_2", "draw_3", etc.
        var parts = effectId.ToLower().Split('_');
        var effectType = parts[0];
        var value = parts.Length > 1 && int.TryParse(parts[1], out var v) ? v : choice.EffectValue;
        
        switch (effectType)
        {
            case "heal":
                var healValue = value > 0 ? value : choice.EffectValue;
                player.HealPool += healValue;
                AddLogEntry("Choice", $"+{healValue} Heal from {choice.CardName}");
                return GameActionResult.Ok($"+{healValue} Heal");
            case "draw":
                var drawCount = value > 0 ? value : 1;
                var cardsDrawn = 0;
                for (int i = 0; i < drawCount && player.DeedDeck.Count > 0; i++)
                {
                    var drawnCard = player.DeedDeck[0];
                    player.DeedDeck.RemoveAt(0);
                    player.Hand.Add(drawnCard);
                    cardsDrawn++;
                }
                if (cardsDrawn > 0)
                {
                    AddLogEntry("Choice", $"Drew {cardsDrawn} card(s) from {choice.CardName}");
                    return GameActionResult.Ok($"Drew {cardsDrawn} card(s)");
                }
                return GameActionResult.Fail("No cards to draw");
            default:
                return GameActionResult.Fail("Invalid choice");
        }
    }

    private GameActionResult ResolveDiscardForEffectChoice(PlayerState player, PendingChoice choice, string effectId, string? discardCardId)
    {
        if (string.IsNullOrEmpty(discardCardId))
            return GameActionResult.Fail("Must select a card to discard");

        if (!player.Hand.Contains(discardCardId))
            return GameActionResult.Fail("Card not in hand");

        // Discard the selected card
        player.Hand.Remove(discardCardId);
        player.DiscardPile.Add(discardCardId);

        // Get card name for logging
        var discardedCard = _definitions.GetBasicActionsAsync().Result.FirstOrDefault(c => c.Id == discardCardId)
                         ?? _definitions.GetAdvancedActionsAsync().Result.FirstOrDefault(c => c.Id == discardCardId);
        var cardName = discardedCard?.Name ?? discardCardId;

        // Apply the chosen effect
        var value = choice.EffectValue;
        switch (effectId.ToLower())
        {
            case "move":
                player.MovementRemaining += value;
                AddLogEntry("Choice", $"Discarded {cardName} for +{value} Move");
                return GameActionResult.Ok($"Discarded {cardName} for +{value} Move");
            case "attack":
                player.AttackPool += value;
                AddLogEntry("Choice", $"Discarded {cardName} for +{value} Attack");
                return GameActionResult.Ok($"Discarded {cardName} for +{value} Attack");
            case "block":
                player.BlockPool += value;
                AddLogEntry("Choice", $"Discarded {cardName} for +{value} Block");
                return GameActionResult.Ok($"Discarded {cardName} for +{value} Block");
            case "influence":
                player.InfluencePool += value;
                AddLogEntry("Choice", $"Discarded {cardName} for +{value} Influence");
                return GameActionResult.Ok($"Discarded {cardName} for +{value} Influence");
            default:
                return GameActionResult.Fail("Invalid effect type");
        }
    }

    /// <summary>
    /// Checks if there is a pending choice that needs to be resolved.
    /// </summary>
    public PendingChoice? GetPendingChoice() => _state.PendingChoice;

    /// <summary>
    /// Cancels the pending choice and returns the card to hand.
    /// </summary>
    public GameActionResult CancelChoice()
    {
        if (_state.PendingChoice == null)
            return GameActionResult.Fail("No pending choice to cancel");

        // Note: The card was already moved to discard in PlayCard
        // We would need to track this to undo it properly
        // For now, just clear the choice
        _state.PendingChoice = null;
        return GameActionResult.Ok("Choice cancelled");
    }

    public GameActionResult EndTurn()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Cannot end turn during combat
        if (_state.Combat != null)
            return GameActionResult.Fail("Cannot end turn during combat. Finish or flee from combat first.");

        // Cannot end turn during tactics selection
        if (_state.Phase == GamePhase.TacticsSelection)
            return GameActionResult.Fail("Cannot end turn during tactics selection. Select a tactic first.");

        // Reset all player pools and state for next turn
        ResetPlayerTurnState(player);

        // Reset undo for the next player's turn
        ResetUndoForNewTurn();

        AddLogEntry("EndTurn", $"Player ended their turn");

        // Check victory conditions after each turn
        if (CheckVictoryConditions())
        {
            AddLogEntry("Victory", _state.Victory?.EndReason ?? "Game over!");
            return GameActionResult.Ok($"Game Over! {_state.Victory?.EndReason}");
        }

        // Move to next player in turn order
        _state.CurrentPlayerIndex++;
        if (_state.CurrentPlayerIndex >= _state.TurnOrder.Count)
        {
            // End of round
            _state.CurrentPlayerIndex = 0;
            EndRound();

            // Check again after round ends
            if (CheckVictoryConditions())
            {
                AddLogEntry("Victory", _state.Victory?.EndReason ?? "Game over!");
                return GameActionResult.Ok($"Game Over! {_state.Victory?.EndReason}");
            }
        }
        else
        {
            // Draw cards for next player
            var nextPlayerIndex = _state.TurnOrder[_state.CurrentPlayerIndex];
            var nextPlayer = _state.Players[nextPlayerIndex];
            DrawCardsToHandLimit(nextPlayer);
        }

        return GameActionResult.Ok("Turn ended");
    }

    private void ResetPlayerTurnState(PlayerState player)
    {
        player.MovementRemaining = 0;
        player.FlightRemaining = 0;
        player.SafeMovementRemaining = 0;
        player.AttackPool = 0;
        player.BlockPool = 0;
        player.InfluencePool = 0;
        player.HealPool = 0;
        player.RangedAttack = 0;
        player.SiegeAttack = 0;
        player.AttackElements.Clear();
        player.HasRested = false;
        player.UsedSiteInteractions.Clear(); // Reset site interactions for new turn
        player.TurnStartPosition = null;
    }

    public GameActionResult Rest()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (player.HasRested)
            return GameActionResult.Fail("Already rested this turn");

        // Discard any non-wound cards from hand
        var nonWoundCards = player.Hand.Where(c => !c.StartsWith("wound")).ToList();
        foreach (var card in nonWoundCards)
        {
            player.Hand.Remove(card);
            player.DiscardPile.Add(card);
        }

        // Draw up to hand limit
        DrawCardsToHandLimit(player);

        player.HasRested = true;
        AddLogEntry("Rest", "Player chose to rest");

        return GameActionResult.Ok("Rested and drew new cards");
    }

    public GameActionResult UseMana(int dieIndex)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (dieIndex < 0 || dieIndex >= _state.ManaPool.Count)
            return GameActionResult.Fail("Invalid mana die index");

        // Check if player already has temporary mana (can only take one)
        if (player.TemporaryMana.HasValue)
            return GameActionResult.Fail("You can only take one mana die per round. You already have temporary mana.");

        // Save state for undo before taking mana
        SaveStateForUndo();

        var color = _state.ManaPool[dieIndex];
        
        // Give player temporary mana (don't remove from pool yet - it stays until end of round)
        player.TemporaryMana = color;
        player.UsedManaDieIndex = dieIndex;

        AddLogEntry("UseMana", $"Took {color} mana from Source (temporary until end of round)");
        return GameActionResult.Ok($"Took {color} mana from Source");
    }

    public GameActionResult UseCrystal(ManaColor color)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var count = GetCrystalCount(player.Crystals, color);
        if (count <= 0)
            return GameActionResult.Fail($"No {color} crystals available");

        DecrementCrystal(player.Crystals, color);

        AddLogEntry("UseCrystal", $"Used {color} crystal");
        return GameActionResult.Ok($"Used {color} crystal");
    }

    private int GetCrystalCount(CrystalInventory crystals, ManaColor color)
    {
        return color switch
        {
            ManaColor.Red => crystals.Red,
            ManaColor.Blue => crystals.Blue,
            ManaColor.Green => crystals.Green,
            ManaColor.White => crystals.White,
            ManaColor.Gold => crystals.Gold,
            _ => 0
        };
    }

    private void DecrementCrystal(CrystalInventory crystals, ManaColor color)
    {
        switch (color)
        {
            case ManaColor.Red: crystals.Red--; break;
            case ManaColor.Blue: crystals.Blue--; break;
            case ManaColor.Green: crystals.Green--; break;
            case ManaColor.White: crystals.White--; break;
            case ManaColor.Gold: crystals.Gold--; break;
        }
    }

    public GameActionResult UndoUseMana()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (!player.TemporaryMana.HasValue)
            return GameActionResult.Fail("No temporary mana to undo");

        var color = player.TemporaryMana.Value;
        player.TemporaryMana = null;
        player.UsedManaDieIndex = null;

        AddLogEntry("UndoMana", $"Returned {color} mana - die selection undone");
        return GameActionResult.Ok($"Returned {color} mana to Source");
    }

    public GameActionResult DrawCards()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var drawn = DrawCardsToHandLimit(player);
        AddLogEntry("DrawCards", $"Drew {drawn} cards");
        return GameActionResult.Ok($"Drew {drawn} cards");
    }

    public GameActionResult SelectTactic(string tacticId)
    {
        if (_state.Phase != GamePhase.TacticsSelection)
            return GameActionResult.Fail("Not in tactics selection phase");

        // Find the player index for the current player
        var playerIndex = _state.CurrentPlayerIndex;
        if (playerIndex < 0 || playerIndex >= _state.Players.Count)
            return GameActionResult.Fail("Invalid player index");

        // Check if player already selected a tactic
        if (_state.SelectedTactics.ContainsKey(playerIndex))
            return GameActionResult.Fail("You have already selected a tactic");

        // Check if tactic is available
        if (!_state.AvailableTactics.Contains(tacticId))
            return GameActionResult.Fail("This tactic is not available");

        // Check if tactic was already taken by another player
        if (_state.SelectedTactics.ContainsValue(tacticId))
            return GameActionResult.Fail("This tactic has already been selected by another player");

        // Select the tactic
        _state.SelectedTactics[playerIndex] = tacticId;
        _state.AvailableTactics.Remove(tacticId);

        AddLogEntry("SelectTactic", $"Player selected tactic: {tacticId}");

        // Move to next player for tactic selection
        _state.CurrentPlayerIndex++;
        
        // If all players have selected, determine turn order and start the round
        if (AllPlayersSelectedTactics())
        {
            DetermineTurnOrder();
            _state.Phase = GamePhase.Movement;
            _state.CurrentPlayerIndex = 0; // First player in turn order
            
            // Draw cards up to hand limit for all players (if not already at limit)
            // This ensures players have cards at the start of the round
            foreach (var player in _state.Players)
            {
                if (player.Hand.Count < player.HandLimit && player.DeedDeck.Count > 0)
                {
                    DrawCardsToHandLimit(player);
                }
            }
            
            // Apply tactic effects (draw cards, etc.) - this may add extra cards
            ApplyTacticEffects();
            
            AddLogEntry("RoundStart", $"Round {_state.Round} begins - Turn order determined");
        }

        return GameActionResult.Ok($"Selected tactic: {tacticId}");
    }

    public IEnumerable<string> GetAvailableTactics()
    {
        return _state.AvailableTactics;
    }

    public bool AllPlayersSelectedTactics()
    {
        return _state.SelectedTactics.Count >= _state.Players.Count;
    }

    private void DetermineTurnOrder()
    {
        // Sort players by their tactic's position (lower position = earlier turn)
        var playerOrder = _state.SelectedTactics
            .OrderBy(kvp => GetTacticPosition(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        _state.TurnOrder = playerOrder;
    }

    private int GetTacticPosition(string tacticId)
    {
        // Extract position from tactic ID (e.g., "tact_day_01" -> position 1)
        // This is a simplified version - real implementation would look up from definitions
        var parts = tacticId.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var position))
            return position;
        return 6; // Default to last position
    }

    private void ApplyTacticEffects()
    {
        foreach (var kvp in _state.SelectedTactics)
        {
            var playerIndex = kvp.Key;
            var tacticId = kvp.Value;
            var player = _state.Players[playerIndex];

            // Apply tactic effect based on ID
            // This is simplified - real implementation would look up effects from definitions
            if (tacticId.Contains("01")) // "The Right Moment" or "Mana Search"
            {
                DrawCardsToHandLimit(player, 1); // Draw 1 extra card
            }
            else if (tacticId.Contains("05")) // "Great Start" or "Long Night"
            {
                DrawCardsToHandLimit(player, 2); // Draw 2 extra cards
            }
            else if (tacticId.Contains("02") && tacticId.Contains("night")) // "Sparing Power"
            {
                DrawCardsToHandLimit(player, 2); // Draw 2 extra cards
            }
        }
    }

    private int DrawCardsToHandLimit(PlayerState player, int extra = 0)
    {
        var targetCount = player.HandLimit + extra;
        var drawn = 0;
        while (player.Hand.Count < targetCount && player.DeedDeck.Count > 0)
        {
            var card = player.DeedDeck[0];
            player.DeedDeck.RemoveAt(0);
            player.Hand.Add(card);
            drawn++;
        }
        return drawn;
    }

    public void AddLogEntry(string action, string? details = null)
    {
        _state.GameLog.Add(new GameLogEntry
        {
            Timestamp = DateTime.UtcNow,
            PlayerIndex = _state.CurrentPlayerIndex,
            Action = action,
            Details = details
        });
    }

    // Private helper methods

    private void EndRound()
    {
        _state.Round++;

        // Toggle day/night
        _state.IsDay = !_state.IsDay;

        // Shuffle discard into deed deck for all players
        foreach (var player in _state.Players)
        {
            // Return used mana die to pool and reroll it
            if (player.UsedManaDieIndex.HasValue && player.UsedManaDieIndex.Value >= 0 && player.UsedManaDieIndex.Value < _state.ManaPool.Count)
            {
                // Reroll the die that was used
                var oldColor = _state.ManaPool[player.UsedManaDieIndex.Value];
                var newColor = RollManaDie();
                _state.ManaPool[player.UsedManaDieIndex.Value] = newColor;
                AddLogEntry("EndRound", $"Rerolled {oldColor} mana die to {newColor}");
            }
            
            // Clear temporary mana (it's consumed or expires at end of round)
            player.TemporaryMana = null;
            player.UsedManaDieIndex = null;
            
            // Put hand cards (except wounds) into discard first
            var handCards = player.Hand.Where(c => !c.StartsWith("wound")).ToList();
            foreach (var card in handCards)
            {
                player.Hand.Remove(card);
                player.DiscardPile.Add(card);
            }
            
            // Shuffle discard back into deck
            player.DeedDeck.AddRange(player.DiscardPile);
            player.DiscardPile.Clear();
            ShuffleList(player.DeedDeck);
            
            // Draw new hand for all players
            DrawCardsToHandLimit(player);
            
            // Ready all units
            foreach (var unit in player.Units)
            {
                unit.IsReady = true;
                unit.UsedThisCombat = false;
            }
        }

        // Reroll mana pool (for any unused dice)
        RollManaPool();
        
        // Reset to tactics selection for new round
        _state.Phase = GamePhase.TacticsSelection;
        _state.SelectedTactics.Clear();
        
        // Set available tactics based on day/night
        var tactics = _state.IsDay 
            ? _definitions.GetDayTacticsAsync().Result 
            : _definitions.GetNightTacticsAsync().Result;
        _state.AvailableTactics = tactics.Select(t => t.Id).ToList();
        
        // Refill unit offers
        RefillUnitOffers();
        
        // Refill spell/advanced action offers
        RefillCardOffers();

        AddLogEntry("RoundEnd", $"Round {_state.Round} begins - {(_state.IsDay ? "Day" : "Night")}. Select tactics!");
    }
    
    private void RefillUnitOffers()
    {
        // Ensure there are enough units in the offer
        // Regular units: 3 in offer
        // Elite units: 2 in offer (or based on player count)
        
        var regularUnits = _definitions.GetRegularUnitsAsync().Result.ToList();
        var eliteUnits = _definitions.GetEliteUnitsAsync().Result.ToList();
        
        // Remove units that are already in the offer
        var availableRegular = regularUnits
            .Where(u => !_state.UnitOffers.RegularUnits.Contains(u.Id))
            .ToList();
        var availableElite = eliteUnits
            .Where(u => !_state.UnitOffers.EliteUnits.Contains(u.Id))
            .ToList();
        
        // Fill regular units to 3
        while (_state.UnitOffers.RegularUnits.Count < 3 && availableRegular.Any())
        {
            var unit = availableRegular[_random.Next(availableRegular.Count)];
            _state.UnitOffers.RegularUnits.Add(unit.Id);
            availableRegular.Remove(unit);
        }
        
        // Fill elite units to 2
        while (_state.UnitOffers.EliteUnits.Count < 2 && availableElite.Any())
        {
            var unit = availableElite[_random.Next(availableElite.Count)];
            _state.UnitOffers.EliteUnits.Add(unit.Id);
            availableElite.Remove(unit);
        }
    }
    
    private void RefillCardOffers()
    {
        // Ensure spell and advanced action offers are full
        // 3 spells in offer, 3 advanced actions in offer
        
        var spells = _definitions.GetSpellsAsync().Result.ToList();
        var advancedActions = _definitions.GetAdvancedActionsAsync().Result.ToList();
        
        // Remove cards that are already in the offer
        var availableSpells = spells
            .Where(s => !_state.SpellOffers.Spells.Contains(s.Id))
            .ToList();
        var availableAdvancedActions = advancedActions
            .Where(a => !_state.AdvancedActionOffers.AdvancedActions.Contains(a.Id))
            .ToList();
        
        // Fill spells to 3
        while (_state.SpellOffers.Spells.Count < 3 && availableSpells.Any())
        {
            var spell = availableSpells[_random.Next(availableSpells.Count)];
            _state.SpellOffers.Spells.Add(spell.Id);
            availableSpells.Remove(spell);
        }
        
        // Fill advanced actions to 3
        while (_state.AdvancedActionOffers.AdvancedActions.Count < 3 && availableAdvancedActions.Any())
        {
            var action = availableAdvancedActions[_random.Next(availableAdvancedActions.Count)];
            _state.AdvancedActionOffers.AdvancedActions.Add(action.Id);
            availableAdvancedActions.Remove(action);
        }
    }

    private ManaColor RollManaDie()
    {
        var colors = new[] { ManaColor.Red, ManaColor.Blue, ManaColor.Green, ManaColor.White };
        // Gold has 1/6 chance, each color has equal remaining chance
        if (_random.Next(6) == 0)
            return ManaColor.Gold;
        else
            return colors[_random.Next(colors.Length)];
    }

    private void RollManaPool()
    {
        var colors = new[] { ManaColor.Red, ManaColor.Blue, ManaColor.Green, ManaColor.White };
        var diceCount = _state.Players.Count + 2; // Base dice count

        _state.ManaPool.Clear();
        for (int i = 0; i < diceCount; i++)
        {
            _state.ManaPool.Add(RollManaDie());
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private string? GetTerrainAt(HexPosition pos)
    {
        var hexKey = PosKey(pos);
        
        // Check if hex is revealed and has data
        if (!_state.Map.RevealedHexes.Contains(hexKey))
            return null; // Not revealed = can't move there yet
        
        if (_state.Map.HexData.TryGetValue(hexKey, out var hexState))
        {
            return hexState.Terrain;
        }
        
        return null; // No data for this hex
    }

    private int GetTerrainCost(string terrain)
    {
        if (TerrainCosts.TryGetValue(terrain, out var costs))
            return _state.IsDay ? costs.Day : costs.Night;
        return 2; // Default plains cost
    }

    private HexState? GetHexStateAt(HexPosition pos)
    {
        var hexKey = PosKey(pos);
        if (_state.Map.HexData.TryGetValue(hexKey, out var hexState))
            return hexState;
        return null;
    }

    private SiteState? GetSiteStateAt(HexPosition pos)
    {
        foreach (var tile in _state.Map.Tiles)
        {
            var hexKey = $"{pos.Q},{pos.R}";
            if (tile.SiteStates.TryGetValue(hexKey, out var siteState))
                return siteState;
        }
        return null;
    }

    private bool IsAdjacent(HexPosition a, HexPosition b)
    {
        foreach (var dir in HexDirections)
        {
            if ((a + dir).Equals(b))
                return true;
        }
        return false;
    }

    private static string PosKey(HexPosition pos) => $"{pos.Q},{pos.R}";

    // Combat methods

    public GameActionResult InitiateCombat()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var hexState = GetHexStateAt(player.Position);
        if (hexState == null || !hexState.Enemies.Any())
            return GameActionResult.Fail("No enemies at current position");

        // Check if site uses night rules (dungeons, tombs)
        var siteType = hexState.SiteType?.ToLower() ?? "";
        var isNightRules = siteType.Contains("dungeon") || siteType.Contains("tomb");
        var isFortifiedSite = siteType.Contains("keep") || siteType.Contains("city");

        // Create combat state
        var combat = new CombatState
        {
            Position = player.Position,
            SiteType = hexState.SiteType,
            IsNightRules = isNightRules
        };

        // Load enemy definitions
        var hasSwiftEnemies = false;
        foreach (var enemyId in hexState.Enemies)
        {
            var enemyDef = _definitions.GetEnemiesAsync().Result.FirstOrDefault(e => e.Id == enemyId);
            if (enemyDef != null)
            {
                var combatEnemy = CreateCombatEnemy(enemyDef);

                // Site-based fortified status
                if (isFortifiedSite || combatEnemy.Abilities.Contains("Fortified"))
                {
                    if (!combatEnemy.Abilities.Contains("Fortified"))
                    {
                        combatEnemy.Abilities.Add("Fortified");
                    }
                }

                combat.Enemies.Add(combatEnemy);
                
                if (combatEnemy.IsSwift)
                    hasSwiftEnemies = true;
                
                // Handle Summon ability - add summoned enemies
                if (combatEnemy.CanSummon)
                {
                    var summonedEnemies = ProcessSummonAbility(combatEnemy, combat);
                    foreach (var summoned in summonedEnemies)
                    {
                        combat.Enemies.Add(summoned);
                        combat.SummonedEnemies.Add(summoned.EnemyId);
                        if (summoned.IsSwift)
                            hasSwiftEnemies = true;
                    }
                }
            }
        }

        // If there are swift enemies, start with swift attack phase
        combat.Phase = hasSwiftEnemies ? CombatPhase.SwiftAttack : CombatPhase.RangedAttack;

        _state.Combat = combat;
        _state.Phase = GamePhase.Combat;

        var summonMsg = combat.SummonedEnemies.Any() ? $" (+{combat.SummonedEnemies.Count} summoned)" : "";
        var phaseMsg = hasSwiftEnemies ? "Swift enemies attack first!" : "Ranged attack phase";
        AddLogEntry("Combat", $"Combat initiated with {combat.Enemies.Count} enemies{summonMsg}. {phaseMsg}");
        return GameActionResult.Ok($"Combat started with {combat.Enemies.Count} enemies{summonMsg}. {phaseMsg}");
    }

    private List<CombatEnemy> ProcessSummonAbility(CombatEnemy summoner, CombatState combat)
    {
        var summonedEnemies = new List<CombatEnemy>();
        
        // Parse summon ability - format: "Summon_EnemyType" (e.g., "Summon_Brown")
        var summonAbilities = summoner.Abilities.Where(a => a.StartsWith("Summon_", StringComparison.OrdinalIgnoreCase));
        
        foreach (var summonAbility in summonAbilities)
        {
            var enemyType = summonAbility.Substring(7); // Remove "Summon_" prefix
            
            // Try to draw from enemy deck
            if (_state.Decks.EnemyDecks.TryGetValue(enemyType, out var deck) && deck.Any())
            {
                var enemyId = deck[0];
                deck.RemoveAt(0);
                
                var enemyDef = _definitions.GetEnemiesAsync().Result.FirstOrDefault(e => e.Id == enemyId);
                if (enemyDef != null)
                {
                    var summonedEnemy = CreateCombatEnemy(enemyDef);
                    summonedEnemy.Name = $"{enemyDef.Name} (Summoned)";
                    summonedEnemy.Fame = 0; // Summoned enemies don't give fame
                    
                    summonedEnemies.Add(summonedEnemy);
                    AddLogEntry("Combat", $"{summoner.Name} summoned a {enemyDef.Name}!");
                }
            }
            else
            {
                // No enemies of that type available - summon fails
                AddLogEntry("Combat", $"{summoner.Name}'s summon ability failed - no {enemyType} enemies available");
            }
        }
        
        return summonedEnemies;
    }

    public GameActionResult RangedAttack(int enemyIndex, int attackValue)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.RangedAttack)
            return GameActionResult.Fail("Not in ranged attack phase");

        if (enemyIndex < 0 || enemyIndex >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex];
        if (enemy.IsDefeated)
            return GameActionResult.Fail("Enemy already defeated");

        var player = GetCurrentPlayer();
        if (player == null || player.RangedAttack < attackValue)
            return GameActionResult.Fail("Not enough ranged attack");

        // Fortified enemies require Siege attack for ranged
        if (enemy.IsFortified && player.SiegeAttack < attackValue)
        {
            return GameActionResult.Fail("Fortified enemies require Siege attack for ranged attacks");
        }

        // Calculate effective armor with resistances
        var effectiveArmor = CalculateEffectiveArmor(enemy, "Physical", false);
        
        if (attackValue >= effectiveArmor)
        {
            DefeatEnemy(enemy, player, "ranged attack");
            player.RangedAttack -= attackValue;
            return GameActionResult.Ok($"Defeated {enemy.Name} with ranged attack!");
        }
        else
        {
            player.RangedAttack -= attackValue;
            // Paralyze: ineffective attack causes wound
            if (enemy.IsParalyze)
            {
                player.Hand.Add("wound");
                AddLogEntry("Combat", $"Paralyze! Ineffective attack against {enemy.Name} caused a wound!");
                return GameActionResult.Fail($"Attack ineffective - Paralyze caused a wound!");
            }
            AddLogEntry("Combat", $"Ranged attack failed - need {effectiveArmor} attack, had {attackValue}");
            return GameActionResult.Fail($"Attack too weak - need {effectiveArmor} to defeat");
        }
    }

    public GameActionResult SiegeAttack(int enemyIndex, int attackValue)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.RangedAttack)
            return GameActionResult.Fail("Siege attacks are done in ranged phase");

        if (enemyIndex < 0 || enemyIndex >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex];
        if (enemy.IsDefeated)
            return GameActionResult.Fail("Enemy already defeated");

        var player = GetCurrentPlayer();
        if (player == null || player.SiegeAttack < attackValue)
            return GameActionResult.Fail("Not enough siege attack");

        var effectiveArmor = CalculateEffectiveArmor(enemy, "Physical", false);
        
        if (attackValue >= effectiveArmor)
        {
            DefeatEnemy(enemy, player, "siege attack");
            player.SiegeAttack -= attackValue;
            return GameActionResult.Ok($"Defeated {enemy.Name} with siege attack!");
        }
        else
        {
            player.SiegeAttack -= attackValue;
            if (enemy.IsParalyze)
            {
                player.Hand.Add("wound");
                return GameActionResult.Fail($"Attack ineffective - Paralyze caused a wound!");
            }
            return GameActionResult.Fail($"Attack too weak - need {effectiveArmor} to defeat");
        }
    }

    public GameActionResult BlockEnemy(int enemyIndex, int blockValue)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.Block && _state.Combat.Phase != CombatPhase.SwiftAttack)
            return GameActionResult.Fail("Not in block phase");

        if (enemyIndex < 0 || enemyIndex >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex];
        if (enemy.IsDefeated || enemy.IsBlocked)
            return GameActionResult.Fail("Enemy already defeated or blocked");

        // In swift phase, can only block swift enemies
        if (_state.Combat.Phase == CombatPhase.SwiftAttack && !enemy.IsSwift)
            return GameActionResult.Fail("Can only block Swift enemies in this phase");

        var player = GetCurrentPlayer();
        if (player == null || player.BlockPool < blockValue)
            return GameActionResult.Fail("Not enough block");

        // Determine required block type based on attack type
        var requiredBlock = GetRequiredBlockForAttack(enemy.AttackType);
        
        // Check if block is sufficient
        if (blockValue >= enemy.Attack)
        {
            enemy.IsBlocked = true;
            player.BlockPool -= blockValue;
            AddLogEntry("Combat", $"Fully blocked {enemy.Name}'s {enemy.AttackType} attack ({blockValue} vs {enemy.Attack})");
            return GameActionResult.Ok($"Blocked {enemy.Name}'s attack");
        }
        else
        {
            player.BlockPool -= blockValue;
            var remainingDamage = enemy.Attack - blockValue;
            _state.Combat.TotalUnblockedDamage += remainingDamage;
            AddLogEntry("Combat", $"Partially blocked {enemy.Name}'s attack - {remainingDamage} damage unblocked");
            return GameActionResult.Ok($"Partial block - {remainingDamage} damage unblocked");
        }
    }

    public GameActionResult AttackEnemy(int enemyIndex, int attackValue)
    {
        return AttackEnemyWithElement(enemyIndex, attackValue, "Physical");
    }

    public GameActionResult AttackEnemyWithElement(int enemyIndex, int attackValue, string element)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.Attack)
            return GameActionResult.Fail("Not in attack phase");

        if (enemyIndex < 0 || enemyIndex >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex];
        if (enemy.IsDefeated)
            return GameActionResult.Fail("Enemy already defeated");

        var player = GetCurrentPlayer();
        if (player == null || player.AttackPool < attackValue)
            return GameActionResult.Fail("Not enough attack");

        // Calculate effective armor considering resistances and element
        var effectiveArmor = CalculateEffectiveArmor(enemy, element, true);
        
        if (attackValue >= effectiveArmor)
        {
            DefeatEnemy(enemy, player, $"{element} attack");
            player.AttackPool -= attackValue;
            return GameActionResult.Ok($"Defeated {enemy.Name}!");
        }
        else
        {
            player.AttackPool -= attackValue;
            // Paralyze: ineffective attack causes wound
            if (enemy.IsParalyze)
            {
                player.Hand.Add("wound");
                AddLogEntry("Combat", $"Paralyze! Ineffective attack against {enemy.Name} caused a wound!");
                return GameActionResult.Fail($"Attack ineffective - Paralyze caused a wound!");
            }
            AddLogEntry("Combat", $"Attack failed - need {effectiveArmor} attack, had {attackValue}");
            return GameActionResult.Fail($"Attack too weak - need {effectiveArmor} to defeat");
        }
    }

    private int CalculateEffectiveArmor(CombatEnemy enemy, string attackElement, bool isMelee)
    {
        var baseArmor = enemy.Armor;
        
        // Check resistances
        if (enemy.Resistances.Contains(attackElement))
        {
            // Resistant: armor is doubled
            baseArmor *= 2;
        }
        
        // Ice attack: halve armor (rounded up), but not if Ice resistant
        if (attackElement == "Ice" && !enemy.Resistances.Contains("Ice"))
        {
            baseArmor = (baseArmor + 1) / 2;
        }
        
        // ColdFire: combines Ice and Fire effects
        if (attackElement == "ColdFire")
        {
            if (!enemy.Resistances.Contains("Ice") && !enemy.Resistances.Contains("Fire"))
            {
                baseArmor = (baseArmor + 1) / 2; // Ice effect
            }
        }
        
        return baseArmor;
    }

    private void DefeatEnemy(CombatEnemy enemy, PlayerState player, string attackType)
    {
        enemy.IsDefeated = true;
        player.Fame += enemy.Fame;
        
        // Check for Summon ability - summoned enemies flee when summoner is killed
        if (enemy.CanSummon && _state.Combat != null)
        {
            // Mark all summoned enemies as defeated (they flee when summoner dies)
            var summonedIds = _state.Combat.SummonedEnemies.ToList();
            foreach (var summonedEnemy in _state.Combat.Enemies.Where(e => summonedIds.Contains(e.EnemyId) && !e.IsDefeated))
            {
                summonedEnemy.IsDefeated = true;
                AddLogEntry("Combat", $"{summonedEnemy.Name} fled when their summoner was defeated!");
            }
            AddLogEntry("Combat", $"Summoner {enemy.Name} defeated - summoned creatures dispersed!");
        }
        
        var fameMsg = enemy.Fame > 0 ? $"! +{enemy.Fame} fame" : " (no fame - summoned)";
        AddLogEntry("Combat", $"Defeated {enemy.Name} with {attackType}{fameMsg}");
    }

    private string GetRequiredBlockForAttack(string attackType)
    {
        return attackType switch
        {
            "Fire" => "Fire", // Fire attacks can be blocked with Fire block or Ice block
            "Ice" => "Ice",
            "Cold" => "Cold",
            "Physical" => "Physical",
            _ => "Physical"
        };
    }

    // Keep the old method signature for compatibility
    private GameActionResult AttackEnemyOld(int enemyIndex, int attackValue)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.Attack)
            return GameActionResult.Fail("Not in attack phase");

        if (enemyIndex < 0 || enemyIndex >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex];
        if (enemy.IsDefeated)
            return GameActionResult.Fail("Enemy already defeated");

        var player = GetCurrentPlayer();
        if (player == null || player.AttackPool < attackValue)
            return GameActionResult.Fail("Not enough attack");

        // Apply damage
        if (attackValue >= enemy.Armor)
        {
            enemy.IsDefeated = true;
            player.AttackPool -= attackValue;
            
            // Award fame
            var fame = GetEnemyFame(enemy.EnemyId);
            player.Fame += fame;
            
            AddLogEntry("Combat", $"Defeated {enemy.EnemyId}! Gained {fame} fame.");
            return GameActionResult.Ok($"Defeated {enemy.EnemyId}! +{fame} fame");
        }
        else
        {
            player.AttackPool -= attackValue;
            AddLogEntry("Combat", $"Attack failed - need {enemy.Armor} attack, had {attackValue}");
            return GameActionResult.Fail($"Attack too weak - need {enemy.Armor} to defeat");
        }
    }

    public GameActionResult AssignDamage(int damage)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Add wound cards to hand
        for (int i = 0; i < damage; i++)
        {
            player.Hand.Add("wound");
        }

        AddLogEntry("Combat", $"Took {damage} wounds");
        return GameActionResult.Ok($"Took {damage} wounds");
    }

    public GameActionResult EndCombatPhase()
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        switch (_state.Combat.Phase)
        {
            case CombatPhase.SwiftAttack:
                // Swift enemies have attacked, now player can do ranged
                _state.Combat.Phase = CombatPhase.RangedAttack;
                AddLogEntry("Combat", "Ranged/Siege attack phase begins");
                return GameActionResult.Ok("Ranged attack phase begins");

            case CombatPhase.RangedAttack:
                // Check if there are swift enemies that need to attack
                var hasSwiftEnemies = _state.Combat.Enemies.Any(e => !e.IsDefeated && e.IsSwift);
                if (hasSwiftEnemies && !_state.Combat.SwiftEnemiesAttacked)
                {
                    // Swift enemies attack before block phase
                    _state.Combat.SwiftEnemiesAttacked = true;
                    var swiftDamage = CalculateSwiftDamage();
                    if (swiftDamage > 0)
                    {
                        _state.Combat.TotalUnblockedDamage = swiftDamage;
                        _state.Combat.Phase = CombatPhase.Block;
                        AddLogEntry("Combat", $"Swift enemies attack! Block {swiftDamage} damage or take wounds");
                        return GameActionResult.Ok($"Swift enemies attack for {swiftDamage} damage!");
                    }
                }
                
                _state.Combat.Phase = CombatPhase.Block;
                _state.Combat.TotalUnblockedDamage = 0; // Reset for regular block phase
                AddLogEntry("Combat", "Block phase begins");
                return GameActionResult.Ok("Block phase begins");

            case CombatPhase.Block:
                // Calculate unblocked damage from non-swift enemies
                var unblockedDamage = _state.Combat.Enemies
                    .Where(e => !e.IsDefeated && !e.IsBlocked && !e.IsSwift)
                    .Sum(e => e.Attack);
                
                // Add any previously unblocked damage
                unblockedDamage += _state.Combat.TotalUnblockedDamage;
                
                // Apply Brutal ability (double damage if not blocked at all)
                foreach (var enemy in _state.Combat.Enemies.Where(e => !e.IsDefeated && !e.IsBlocked && e.IsBrutal))
                {
                    unblockedDamage += enemy.Attack; // Double the damage
                    AddLogEntry("Combat", $"Brutal! {enemy.Name}'s damage is doubled!");
                }
                
                _state.Combat.TotalUnblockedDamage = unblockedDamage;
                
                if (unblockedDamage > 0)
                {
                    _state.Combat.Phase = CombatPhase.AssignDamage;
                    AddLogEntry("Combat", $"Assign damage phase - {unblockedDamage} incoming damage");
                    return GameActionResult.Ok($"Assign {unblockedDamage} damage as wounds");
                }
                else
                {
                    // No damage to assign, skip to attack
                    _state.Combat.Phase = CombatPhase.Attack;
                    AddLogEntry("Combat", "All attacks blocked! Attack phase begins");
                    return GameActionResult.Ok("All attacks blocked! Attack phase begins");
                }

            case CombatPhase.AssignDamage:
                // Auto-assign remaining damage as wounds
                var damageToAssign = _state.Combat.TotalUnblockedDamage;
                if (damageToAssign > 0)
                {
                    // Check for Poison - poison wounds go to discard, not hand
                    var poisonEnemies = _state.Combat.Enemies.Where(e => !e.IsDefeated && e.IsPoison).ToList();
                    
                    // Check for Vampiric enemies - they heal when dealing damage
                    var vampiricEnemies = _state.Combat.Enemies.Where(e => !e.IsDefeated && !e.IsBlocked && e.IsVampiric).ToList();
                    if (vampiricEnemies.Any())
                    {
                        // Vampiric enemies heal damage equal to the wounds they cause
                        foreach (var vampEnemy in vampiricEnemies)
                        {
                            var healAmount = Math.Min(vampEnemy.Attack, vampEnemy.CurrentDamage);
                            if (healAmount > 0)
                            {
                                vampEnemy.CurrentDamage -= healAmount;
                                vampEnemy.VampiricArmorBonus += healAmount;
                                AddLogEntry("Combat", $"Vampiric! {vampEnemy.Name} healed {healAmount} damage!");
                            }
                        }
                    }
                    
                    for (int i = 0; i < damageToAssign; i++)
                    {
                        if (poisonEnemies.Any())
                        {
                            // Poison wounds are harder to heal
                            player.DiscardPile.Add("wound_poison");
                            AddLogEntry("Combat", "Poison wound! (Goes to discard pile)");
                        }
                        else
                        {
                            player.Hand.Add("wound");
                        }
                    }
                    AddLogEntry("Combat", $"Took {damageToAssign} wounds");
                }
                
                _state.Combat.TotalUnblockedDamage = 0;
                _state.Combat.Phase = CombatPhase.Attack;
                AddLogEntry("Combat", "Attack phase begins");
                return GameActionResult.Ok("Attack phase begins");

            case CombatPhase.Attack:
                return ResolveCombat();

            case CombatPhase.Resolution:
                return ResolveCombat();

            default:
                return GameActionResult.Fail("Unknown combat phase");
        }
    }

    private int CalculateSwiftDamage()
    {
        if (_state.Combat == null) return 0;
        return _state.Combat.Enemies
            .Where(e => !e.IsDefeated && e.IsSwift)
            .Sum(e => e.Attack);
    }

    public GameActionResult FleeCombat()
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Take damage from all enemies when fleeing (Brutal enemies do double)
        var totalDamage = 0;
        var messages = new List<string>();
        foreach (var enemy in _state.Combat.Enemies.Where(e => !e.IsDefeated))
        {
            var enemyDamage = enemy.Attack;
            if (enemy.IsBrutal)
            {
                enemyDamage *= 2; // Double for Brutal
                messages.Add($"Brutal! {enemy.Name}'s damage doubled");
            }
            totalDamage += enemyDamage;
            
            // Vampiric enemies heal when dealing damage
            if (enemy.IsVampiric)
            {
                var healAmount = Math.Min(enemyDamage, enemy.CurrentDamage);
                if (healAmount > 0)
                {
                    enemy.CurrentDamage -= healAmount;
                    enemy.VampiricArmorBonus += healAmount;
                    messages.Add($"Vampiric! {enemy.Name} healed {healAmount} damage");
                }
            }
        }

        var poisonEnemies = _state.Combat.Enemies.Where(e => !e.IsDefeated && e.IsPoison).ToList();
        for (int i = 0; i < totalDamage; i++)
        {
            if (poisonEnemies.Any())
            {
                player.DiscardPile.Add("wound_poison");
            }
            else
            {
                player.Hand.Add("wound");
            }
        }

        _state.Combat = null;
        _state.Phase = GamePhase.Movement;

        var messageText = messages.Any() ? $" ({string.Join(", ", messages)})" : "";
        AddLogEntry("Combat", $"Fled combat! Took {totalDamage} wounds.{messageText}");
        return GameActionResult.Ok($"Fled combat - took {totalDamage} wounds{messageText}");
    }

    private GameActionResult ResolveCombat()
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var allDefeated = _state.Combat.Enemies.All(e => e.IsDefeated);

        if (allDefeated)
        {
            // Mark hex as conquered
            var hexState = GetHexStateAt(_state.Combat.Position);
            if (hexState != null)
            {
                hexState.IsConquered = true;
                hexState.OwnerUserId = player.UserId;
                hexState.Enemies.Clear();

                // Track city conquest
                if (hexState.SiteType == "City")
                {
                    _state.CitiesConquered++;
                    AddLogEntry("Conquest", $"City conquered! ({_state.CitiesConquered}/{_state.TotalCities})");
                }
            }

            // Award site rewards
            var reward = GetSiteReward(_state.Combat.SiteType);
            if (!string.IsNullOrEmpty(reward))
            {
                AddLogEntry("Combat", $"Victory reward: {reward}");
            }

            // Reset unit combat state
            ResetUnitsCombatState(player);

            _state.Combat = null;
            _state.Phase = GamePhase.Movement;

            // Check victory after conquering a site
            if (CheckVictoryConditions())
            {
                AddLogEntry("Victory", _state.Victory?.EndReason ?? "Game over!");
                return GameActionResult.Ok($"Victory! {_state.Victory?.EndReason}");
            }

            AddLogEntry("Combat", "Victory! All enemies defeated.");
            return GameActionResult.Ok($"Victory! All enemies defeated. {reward}");
        }
        else
        {
            // Some enemies remain - combat continues from ranged phase
            _state.Combat.Phase = CombatPhase.RangedAttack;
            _state.Combat.SwiftEnemiesAttacked = false; // Reset for next round
            
            // Reset blocked status for next round
            foreach (var enemy in _state.Combat.Enemies)
            {
                enemy.IsBlocked = false;
            }
            
            // Reset unit UsedThisCombat for next combat round (but not IsReady)
            foreach (var unit in player.Units)
            {
                unit.UsedThisCombat = false;
            }
            
            AddLogEntry("Combat", "Combat continues - enemies remain");
            return GameActionResult.Ok("Combat continues - some enemies remain");
        }
    }

    private string GetSiteReward(string? siteType)
    {
        if (string.IsNullOrEmpty(siteType)) return "";
        
        return siteType.ToLower() switch
        {
            "dungeon" => "Gained an Artifact!",
            "tomb" => "Gained an Artifact and a Spell!",
            "monsterden" or "monster_den" => "Gained 2 Crystals!",
            "spawninggrounds" or "spawning_grounds" => "Gained an Artifact!",
            "draconum" => "Gained 2 Artifacts!",
            "ancientruins" or "ancient_ruins" or "ruins" => "Gained a Ruins Token!",
            _ => ""
        };
    }

    private int GetEnemyFame(string enemyId)
    {
        var enemy = _definitions.GetEnemiesAsync().Result.FirstOrDefault(e => e.Id == enemyId);
        return enemy?.Fame ?? 2; // Default 2 fame
    }

    // ==================== SITE INTERACTIONS ====================

    public IEnumerable<SiteInteractionOption> GetAvailableSiteInteractions()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            yield break;

        var hexState = GetHexStateAt(player.Position);
        if (hexState == null || string.IsNullOrEmpty(hexState.SiteType))
            yield break;

        // Check if site has enemies (must be conquered first)
        if (hexState.Enemies.Any() && !hexState.IsConquered)
        {
            yield return new SiteInteractionOption
            {
                Type = "Combat",
                Description = $"Fight {hexState.Enemies.Count} enemies to conquer this site",
                IsAvailable = true
            };
            yield break;
        }

        var siteType = hexState.SiteType.ToLower();

        // Village interactions
        if (siteType.Contains("village"))
        {
            yield return new SiteInteractionOption
            {
                Type = "Recruit",
                Description = "Recruit a unit (requires Influence)",
                InfluenceCost = GetRecruitCost(),
                IsAvailable = player.InfluencePool >= GetRecruitCost()
            };
            yield return new SiteInteractionOption
            {
                Type = "Heal",
                Description = "Heal 1 wound (3 Influence)",
                InfluenceCost = 3,
                IsAvailable = player.InfluencePool >= 3 && player.Hand.Contains("wound")
            };
            yield return new SiteInteractionOption
            {
                Type = "Plunder",
                Description = "Draw 2 cards, lose 1 Reputation",
                IsAvailable = true
            };
        }
        // Monastery interactions
        else if (siteType.Contains("monastery"))
        {
            yield return new SiteInteractionOption
            {
                Type = "Recruit",
                Description = "Recruit a unit (requires Influence)",
                InfluenceCost = GetRecruitCost(),
                IsAvailable = player.InfluencePool >= GetRecruitCost()
            };
            yield return new SiteInteractionOption
            {
                Type = "Heal",
                Description = "Heal 1 wound (2 Influence)",
                InfluenceCost = 2,
                IsAvailable = player.InfluencePool >= 2 && player.Hand.Contains("wound")
            };
            yield return new SiteInteractionOption
            {
                Type = "Training",
                Description = "Gain Advanced Action (6 Influence)",
                InfluenceCost = 6,
                IsAvailable = player.InfluencePool >= 6
            };
            yield return new SiteInteractionOption
            {
                Type = "Burn",
                Description = "🔥 Burn the Monastery: Gain 4 Fame, -3 Reputation, draw 1 Artifact",
                IsAvailable = !hexState.IsBurned
            };
        }
        // Mage Tower interactions
        else if (siteType.Contains("magetower") || siteType.Contains("mage_tower"))
        {
            yield return new SiteInteractionOption
            {
                Type = "Recruit",
                Description = "Recruit a spellcaster (requires Influence)",
                InfluenceCost = GetRecruitCost(),
                IsAvailable = player.InfluencePool >= GetRecruitCost()
            };
            yield return new SiteInteractionOption
            {
                Type = "LearnSpell",
                Description = "Learn a spell (7 Influence + 1 Mana)",
                InfluenceCost = 7,
                IsAvailable = player.InfluencePool >= 7
            };
        }
        // Magical Glade interactions
        else if (siteType.Contains("glade") || siteType.Contains("magicalglade"))
        {
            if (hexState.IsCorrupted)
            {
                yield return new SiteInteractionOption
                {
                    Type = "Cleanse",
                    Description = "✨ Cleanse the corrupted Glade: Requires 5 total Heal points",
                    IsAvailable = player.HealPool >= 5
                };
            }
            else
            {
                yield return new SiteInteractionOption
                {
                    Type = "Heal",
                    Description = "Heal 1 wound (Free)",
                    InfluenceCost = 0,
                    IsAvailable = player.Hand.Contains("wound")
                };
                yield return new SiteInteractionOption
                {
                    Type = "Empower",
                    Description = _state.IsDay ? "Gain 1 Gold Crystal" : "Gain 1 Black Mana Token",
                    IsAvailable = true
                };
            }
        }
        // Crystal Mine interactions
        else if (siteType.Contains("mine"))
        {
            var mineColor = GetMineColor(siteType);
            yield return new SiteInteractionOption
            {
                Type = "Harvest",
                Description = $"Gain 1 {mineColor} Crystal",
                IsAvailable = true
            };
        }
        // Keep interactions (if conquered)
        else if (siteType.Contains("keep"))
        {
            if (hexState.IsConquered && hexState.OwnerUserId == player.UserId)
            {
                yield return new SiteInteractionOption
                {
                    Type = "Recruit",
                    Description = "Recruit a unit from your keep",
                    InfluenceCost = GetRecruitCost(),
                    IsAvailable = player.InfluencePool >= GetRecruitCost()
                };
            }
        }
        // Ancient Ruins interactions (if conquered/cleared)
        else if (siteType.Contains("ruins") || siteType.Contains("ancientruins"))
        {
            if (hexState.IsConquered && _state.Decks.RuinsTokens.Any())
            {
                yield return new SiteInteractionOption
                {
                    Type = "DrawRuins",
                    Description = "Draw a Ruins Token (may be loot or enemies!)",
                    IsAvailable = _state.ActiveRuinsToken == null
                };
            }
            else if (!hexState.IsConquered)
            {
                yield return new SiteInteractionOption
                {
                    Type = "DrawRuins",
                    Description = "Draw a Ruins Token (may be loot or enemies!)",
                    IsAvailable = _state.ActiveRuinsToken == null && _state.Decks.RuinsTokens.Any()
                };
            }
        }
        // City interactions (if conquered)
        else if (siteType.Contains("city"))
        {
            if (hexState.IsConquered)
            {
                yield return new SiteInteractionOption
                {
                    Type = "Recruit",
                    Description = "Recruit any unit (city has all unit types)",
                    InfluenceCost = GetRecruitCost(),
                    IsAvailable = player.InfluencePool >= GetRecruitCost()
                };
                yield return new SiteInteractionOption
                {
                    Type = "Heal",
                    Description = "Heal 1 wound (3 Influence)",
                    InfluenceCost = 3,
                    IsAvailable = player.InfluencePool >= 3 && player.Hand.Contains("wound")
                };
                yield return new SiteInteractionOption
                {
                    Type = "Training",
                    Description = "Gain Advanced Action (6 Influence)",
                    InfluenceCost = 6,
                    IsAvailable = player.InfluencePool >= 6
                };
                yield return new SiteInteractionOption
                {
                    Type = "LearnSpell",
                    Description = "Learn a spell (7 Influence)",
                    InfluenceCost = 7,
                    IsAvailable = player.InfluencePool >= 7
                };
                yield return new SiteInteractionOption
                {
                    Type = "BuyFame",
                    Description = "Buy 1 Fame (2 Influence) - repeatable",
                    InfluenceCost = 2,
                    IsAvailable = player.InfluencePool >= 2
                };
            }
        }
    }

    private int GetRecruitCost()
    {
        // Base cost modified by reputation
        var player = GetCurrentPlayer();
        if (player == null) return 5;

        // Reputation affects influence cost
        // Positive reputation = discount, negative = premium
        var baseCost = 5;
        var modifier = player.Reputation switch
        {
            >= 5 => -2,
            >= 3 => -1,
            >= 1 => 0,
            >= -2 => 1,
            >= -4 => 2,
            _ => 3
        };

        return Math.Max(1, baseCost + modifier);
    }

    private string GetMineColor(string siteType)
    {
        if (siteType.Contains("green")) return "Green";
        if (siteType.Contains("blue")) return "Blue";
        if (siteType.Contains("red")) return "Red";
        if (siteType.Contains("white")) return "White";
        return "Gold";
    }

    public GameActionResult InteractWithSite(string interactionType, Dictionary<string, object>? parameters = null)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var hexState = GetHexStateAt(player.Position);
        if (hexState == null || string.IsNullOrEmpty(hexState.SiteType))
            return GameActionResult.Fail("No site at current position");

        var hexKey = PosKey(player.Position);
        var interactionKey = $"{hexKey}:{interactionType.ToLower()}";

        // Check if this is a repeatable interaction
        var isRepeatable = IsRepeatableInteraction(interactionType, hexState.SiteType);
        
        // Check if this non-repeatable interaction was already used at this hex this turn
        if (!isRepeatable && player.UsedSiteInteractions.Contains(interactionKey))
        {
            return GameActionResult.Fail($"You have already used {interactionType} at this site this turn");
        }

        // Execute the interaction
        GameActionResult result;
        switch (interactionType.ToLower())
        {
            case "heal":
                result = HealAtSite(1);
                break;
            case "plunder":
                result = Plunder();
                break;
            case "empower":
                result = Empower();
                break;
            case "harvest":
                result = Harvest(hexState.SiteType);
                break;
            case "training":
                result = ShowAdvancedActionOfferChoice();
                break;
            case "drawruins":
                result = DrawRuinsToken();
                break;
            case "buyfame":
                result = BuyFame();
                break;
            case "learnspell":
                result = ShowSpellOfferChoice();
                break;
            case "recruit":
                result = ShowUnitOfferChoice();
                break;
            case "burn":
                result = BurnMonastery();
                break;
            case "cleanse":
                result = CleanseGlade();
                break;
            default:
                return GameActionResult.Fail($"Unknown interaction type: {interactionType}");
        }

        // If successful and not repeatable, mark as used
        if (result.Success && !isRepeatable)
        {
            player.UsedSiteInteractions.Add(interactionKey);
        }

        return result;
    }

    /// <summary>
    /// Determines if a site interaction can be used multiple times per turn.
    /// </summary>
    private bool IsRepeatableInteraction(string interactionType, string siteType)
    {
        var type = interactionType.ToLower();
        var site = siteType.ToLower();

        // Heal is repeatable at villages, monasteries, cities, glades
        if (type == "heal")
            return true;

        // BuyFame is repeatable at conquered cities
        if (type == "buyfame" && site.Contains("city"))
            return true;

        // Everything else is NOT repeatable:
        // - Harvest (once per mine per turn)
        // - Empower (once per glade per turn)
        // - Training (once per site per turn)
        // - LearnSpell (once per site per turn)
        // - Plunder (once, and ends interaction)
        // - Burn (destroys site)
        // - Cleanse (once per corrupted glade)
        // - DrawRuins (once per ruins)
        return false;
    }

    public GameActionResult RecruitUnit(string unitId)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var cost = GetRecruitCost();
        if (player.InfluencePool < cost)
            return GameActionResult.Fail($"Not enough influence (need {cost}, have {player.InfluencePool})");

        // Check unit limit
        if (player.Units.Count >= player.CommandTokens)
            return GameActionResult.Fail($"Unit limit reached ({player.CommandTokens})");

        // Check if unit is in offer
        var isRegular = _state.UnitOffers.RegularUnits.Contains(unitId);
        var isElite = _state.UnitOffers.EliteUnits.Contains(unitId);
        if (!isRegular && !isElite)
            return GameActionResult.Fail("Unit not available in offer");

        // Get unit definition
        var unitDef = _definitions.GetUnitsAsync().Result.FirstOrDefault(u => u.Id == unitId);
        if (unitDef == null)
            return GameActionResult.Fail("Invalid unit");

        SaveStateForUndo();

        // Remove unit from offer
        if (isRegular)
            _state.UnitOffers.RegularUnits.Remove(unitId);
        else
            _state.UnitOffers.EliteUnits.Remove(unitId);

        // Add unit to player
        player.Units.Add(new UnitState
        {
            UnitId = unitId,
            Name = unitDef.Name,
            Armor = unitDef.Armor,
            IsReady = true,
            IsWounded = false,
            UsedThisCombat = false
        });
        player.InfluencePool -= cost;

        // Refill offer
        RefillUnitOffers();

        // Clear pending choice
        _state.PendingChoice = null;

        AddLogEntry("Recruit", $"Recruited {unitDef.Name} for {cost} influence");
        return GameActionResult.Ok($"Recruited {unitDef.Name}!");
    }

    public GameActionResult HealAtSite(int woundsToHeal)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var hexState = GetHexStateAt(player.Position);
        var siteType = hexState?.SiteType?.ToLower() ?? "";

        // Determine heal cost based on site
        int healCost = siteType switch
        {
            var s when s.Contains("glade") => 0,
            var s when s.Contains("monastery") => 2,
            var s when s.Contains("village") => 3,
            _ => 3
        };

        if (player.InfluencePool < healCost)
            return GameActionResult.Fail($"Not enough influence (need {healCost})");

        // Find wound in hand
        var woundIndex = player.Hand.IndexOf("wound");
        if (woundIndex < 0)
            return GameActionResult.Fail("No wounds to heal");

        // Remove wound and pay cost
        player.Hand.RemoveAt(woundIndex);
        player.InfluencePool -= healCost;

        AddLogEntry("Heal", $"Healed 1 wound for {healCost} influence");
        return GameActionResult.Ok($"Healed 1 wound");
    }

    public GameActionResult Plunder()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Draw 2 cards
        for (int i = 0; i < 2; i++)
        {
            if (player.Deck.Any())
            {
                var card = player.Deck[0];
                player.Deck.RemoveAt(0);
                player.Hand.Add(card);
            }
        }

        // Lose reputation
        player.Reputation--;

        AddLogEntry("Plunder", "Plundered village: Drew 2 cards, -1 Reputation");
        return GameActionResult.Ok("Plundered! Drew 2 cards, -1 Reputation");
    }

    private GameActionResult Empower()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.IsDay)
        {
            // Gain gold crystal
            player.Crystals.Gold++;
            AddLogEntry("Empower", "Gained 1 Gold Crystal from Magical Glade");
            return GameActionResult.Ok("Gained 1 Gold Crystal!");
        }
        else
        {
            // Gain black mana token
            player.ManaTokens.Black++;
            AddLogEntry("Empower", "Gained 1 Black Mana Token from Magical Glade");
            return GameActionResult.Ok("Gained 1 Black Mana Token!");
        }
    }

    private GameActionResult BurnMonastery()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var hexState = GetHexStateAt(player.Position);
        if (hexState == null || !hexState.SiteType?.ToLower().Contains("monastery") == true)
            return GameActionResult.Fail("Not at a monastery");

        if (hexState.IsBurned)
            return GameActionResult.Fail("This monastery has already been burned");

        // Mark as irreversible - destroying a site permanently changes the game state
        MarkIrreversibleAction();

        // Mark as burned
        hexState.IsBurned = true;

        // Gain 4 Fame
        player.Fame += 4;

        // Lose 3 Reputation
        player.Reputation -= 3;

        // Draw an Artifact from deck
        if (_state.Decks.Artifacts.Any())
        {
            var artifactId = _state.Decks.Artifacts[0];
            _state.Decks.Artifacts.RemoveAt(0);
            player.Artifacts.Add(artifactId);
            var artifactDef = _definitions.GetArtifactsAsync().Result.FirstOrDefault(a => a.Id == artifactId);
            var artifactName = artifactDef?.Name ?? artifactId;
            AddLogEntry("Burn", $"🔥 Burned the Monastery! Gained 4 Fame, -3 Reputation, gained artifact: {artifactName}");
            return GameActionResult.Ok($"🔥 Burned the Monastery! Gained 4 Fame, lost 3 Reputation, and found {artifactName}!");
        }

        AddLogEntry("Burn", "🔥 Burned the Monastery! Gained 4 Fame, -3 Reputation");
        return GameActionResult.Ok("🔥 Burned the Monastery! Gained 4 Fame, lost 3 Reputation.");
    }

    private GameActionResult CleanseGlade()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var hexState = GetHexStateAt(player.Position);
        if (hexState == null || !hexState.SiteType?.ToLower().Contains("glade") == true)
            return GameActionResult.Fail("Not at a magical glade");

        if (!hexState.IsCorrupted)
            return GameActionResult.Fail("This glade is not corrupted");

        if (player.HealPool < 5)
            return GameActionResult.Fail("Need 5 Heal points to cleanse the glade");

        // Spend heal points
        player.HealPool -= 5;

        // Cleanse the glade
        hexState.IsCorrupted = false;

        // Reward: Gain 2 Fame, 1 Reputation, and restore the glade's power
        player.Fame += 2;
        player.Reputation++;

        // Bonus reward: A crystal of your choice
        var colors = new[] { "green", "red", "blue", "white" };
        var color = colors[_random.Next(colors.Length)];
        switch (color)
        {
            case "green": player.Crystals.Green++; break;
            case "red": player.Crystals.Red++; break;
            case "blue": player.Crystals.Blue++; break;
            case "white": player.Crystals.White++; break;
        }

        AddLogEntry("Cleanse", $"✨ Cleansed the corrupted Glade! Gained 2 Fame, +1 Reputation, and a {color} crystal!");
        return GameActionResult.Ok($"✨ Cleansed the Glade! Gained 2 Fame, +1 Reputation, and a {color} crystal!");
    }

    private GameActionResult Harvest(string siteType)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        SaveStateForUndo();

        var color = GetMineColor(siteType.ToLower());
        
        switch (color)
        {
            case "Green":
                player.Crystals.Green++;
                break;
            case "Blue":
                player.Crystals.Blue++;
                break;
            case "Red":
                player.Crystals.Red++;
                break;
            case "White":
                player.Crystals.White++;
                break;
            default:
                player.Crystals.Gold++;
                break;
        }

        AddLogEntry("Harvest", $"Harvested 1 {color} Crystal from mine");
        return GameActionResult.Ok($"Harvested 1 {color} Crystal!");
    }

    private GameActionResult ShowAdvancedActionOfferChoice()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (player.InfluencePool < 6)
            return GameActionResult.Fail("Not enough influence (need 6)");

        // Ensure offers are filled
        RefillCardOffers();

        if (!_state.AdvancedActionOffers.AdvancedActions.Any())
            return GameActionResult.Fail("No Advanced Actions available in offer");

        // Create choice for player to select from advanced action offer
        var advancedActions = _definitions.GetAdvancedActionsAsync().Result;
        var options = _state.AdvancedActionOffers.AdvancedActions
            .Select(actionId =>
            {
                var action = advancedActions.FirstOrDefault(a => a.Id == actionId);
                return new ChoiceOption
                {
                    Id = actionId,
                    Name = action?.Name ?? actionId,
                    Description = ""
                };
            })
            .ToList();

        _state.PendingChoice = new PendingChoice
        {
            Type = ChoiceType.AdvancedActionFromOffer,
            Description = "Choose an Advanced Action from the offer (6 Influence)",
            Options = options
        };

        return GameActionResult.Ok("Select an Advanced Action from the offer");
    }

    private GameActionResult Training(string advancedActionId)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (player.InfluencePool < 6)
            return GameActionResult.Fail("Not enough influence (need 6)");

        if (!_state.AdvancedActionOffers.AdvancedActions.Contains(advancedActionId))
            return GameActionResult.Fail("Advanced Action not available in offer");

        SaveStateForUndo();

        // Mark as irreversible - drawing from shared deck reveals new information
        MarkIrreversibleAction();

        // Remove advanced action from offer and add to player's discard
        _state.AdvancedActionOffers.AdvancedActions.Remove(advancedActionId);
        player.DiscardPile.Add(advancedActionId); // Goes to discard, will be shuffled in at end of round
        player.InfluencePool -= 6;

        // Refill offer
        RefillCardOffers();

        // Clear pending choice
        _state.PendingChoice = null;

        var cardDef = _definitions.GetAdvancedActionsAsync().Result.FirstOrDefault(c => c.Id == advancedActionId);
        var cardName = cardDef?.Name ?? advancedActionId;

        AddLogEntry("Training", $"Trained at monastery - gained Advanced Action: {cardName}");
        return GameActionResult.Ok($"Trained! Gained {cardName}.");
    }

    private GameActionResult BuyFame()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Check if at a conquered city
        var hexState = GetHexStateAt(player.Position);
        if (hexState?.SiteType?.ToLower().Contains("city") != true)
            return GameActionResult.Fail("Must be at a city to buy fame");

        // Cost: 2 Influence per 1 Fame (as per rules)
        if (player.InfluencePool < 2)
            return GameActionResult.Fail("Not enough influence (need 2)");

        SaveStateForUndo();

        player.InfluencePool -= 2;
        player.Fame += 1;

        CheckForLevelUp();

        AddLogEntry("BuyFame", "Bought 1 Fame at city for 2 Influence");
        return GameActionResult.Ok("Gained 1 Fame!");
    }

    private GameActionResult ShowSpellOfferChoice()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (player.InfluencePool < 7)
            return GameActionResult.Fail("Not enough influence (need 7)");

        // Check if player has mana (temporary or crystal)
        var hasMana = player.TemporaryMana.HasValue || 
                      player.Crystals.Red > 0 || player.Crystals.Blue > 0 || 
                      player.Crystals.Green > 0 || player.Crystals.White > 0 ||
                      player.Crystals.Gold > 0;
        
        if (!hasMana)
            return GameActionResult.Fail("Learning a spell requires 1 mana (temporary or crystal)");

        // Ensure offers are filled
        RefillCardOffers();

        if (!_state.SpellOffers.Spells.Any())
            return GameActionResult.Fail("No spells available in offer");

        // Create choice for player to select from spell offer
        var spells = _definitions.GetSpellsAsync().Result;
        var options = _state.SpellOffers.Spells
            .Select(spellId =>
            {
                var spell = spells.FirstOrDefault(s => s.Id == spellId);
                return new ChoiceOption
                {
                    Id = spellId,
                    Name = spell?.Name ?? spellId,
                    Description = ""
                };
            })
            .ToList();

        _state.PendingChoice = new PendingChoice
        {
            Type = ChoiceType.SpellFromOffer,
            Description = "Choose a spell from the offer (7 Influence + 1 Mana)",
            Options = options
        };

        return GameActionResult.Ok("Select a spell from the offer");
    }

    public GameActionResult LearnSpell(string spellId)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (player.InfluencePool < 7)
            return GameActionResult.Fail("Not enough influence (need 7)");

        // Check if player has mana (temporary or crystal)
        var hasMana = player.TemporaryMana.HasValue || 
                      player.Crystals.Red > 0 || player.Crystals.Blue > 0 || 
                      player.Crystals.Green > 0 || player.Crystals.White > 0 ||
                      player.Crystals.Gold > 0;
        
        if (!hasMana)
            return GameActionResult.Fail("Learning a spell requires 1 mana (temporary or crystal)");

        if (!_state.SpellOffers.Spells.Contains(spellId))
            return GameActionResult.Fail("Spell not available in offer");

        SaveStateForUndo();

        // Consume temporary mana first, otherwise use a crystal
        if (player.TemporaryMana.HasValue)
        {
            player.TemporaryMana = null;
            player.UsedManaDieIndex = null;
        }
        else
        {
            // Use first available crystal
            if (player.Crystals.Gold > 0) player.Crystals.Gold--;
            else if (player.Crystals.Red > 0) player.Crystals.Red--;
            else if (player.Crystals.Blue > 0) player.Crystals.Blue--;
            else if (player.Crystals.Green > 0) player.Crystals.Green--;
            else if (player.Crystals.White > 0) player.Crystals.White--;
        }

        // Mark as irreversible - drawing from shared deck reveals new information
        MarkIrreversibleAction();

        // Remove spell from offer and add to player
        _state.SpellOffers.Spells.Remove(spellId);
        player.Spells.Add(spellId);
        player.InfluencePool -= 7;

        // Refill offer
        RefillCardOffers();

        // Clear pending choice
        _state.PendingChoice = null;

        var spellDef = _definitions.GetSpellsAsync().Result.FirstOrDefault(s => s.Id == spellId);
        var spellName = spellDef?.Name ?? spellId;

        AddLogEntry("LearnSpell", $"Learned spell: {spellName}");
        return GameActionResult.Ok($"Learned {spellName}!");
    }

    private GameActionResult ShowUnitOfferChoice()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var cost = GetRecruitCost();
        if (player.InfluencePool < cost)
            return GameActionResult.Fail($"Not enough influence (need {cost})");

        // Check unit limit
        if (player.Units.Count >= player.CommandTokens)
            return GameActionResult.Fail($"Unit limit reached ({player.CommandTokens})");

        // Ensure offers are filled
        RefillUnitOffers();

        // Get all available units (regular + elite)
        var allUnits = _state.UnitOffers.RegularUnits.Concat(_state.UnitOffers.EliteUnits).ToList();
        if (!allUnits.Any())
            return GameActionResult.Fail("No units available in offer");

        // Create choice for player to select from unit offer
        var unitDefs = _definitions.GetUnitsAsync().Result;
        var options = allUnits
            .Select(unitId =>
            {
                var unit = unitDefs.FirstOrDefault(u => u.Id == unitId);
                return new ChoiceOption
                {
                    Id = unitId,
                    Name = unit?.Name ?? unitId,
                    Description = $"Armor: {unit?.Armor ?? 0}, Cost: {cost} Influence"
                };
            })
            .ToList();

        _state.PendingChoice = new PendingChoice
        {
            Type = ChoiceType.UnitFromOffer,
            Description = $"Choose a unit from the offer ({cost} Influence)",
            Options = options
        };

        return GameActionResult.Ok("Select a unit from the offer");
    }

    // ==================== RUINS TOKEN SYSTEM ====================

    private GameActionResult DrawRuinsToken()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.ActiveRuinsToken != null)
            return GameActionResult.Fail("Already resolving a ruins token");

        if (!_state.Decks.RuinsTokens.Any())
            return GameActionResult.Fail("No ruins tokens remaining");

        // Mark as irreversible - drawing from shared deck reveals new information
        MarkIrreversibleAction();

        // Draw top token
        var tokenId = _state.Decks.RuinsTokens[0];
        _state.Decks.RuinsTokens.RemoveAt(0);

        // Get token definition
        var tokenDef = _definitions.GetRuinsTokensAsync().Result.FirstOrDefault(t => t.Id == tokenId);
        if (tokenDef == null)
            return GameActionResult.Fail("Invalid ruins token");

        // Create active token state
        _state.ActiveRuinsToken = new ActiveRuinsToken
        {
            TokenId = tokenId,
            Name = tokenDef.Name,
            Type = tokenDef.Type,
            Description = tokenDef.Description,
            IsResolved = false,
            PendingChoices = new List<RuinsChoice>()
        };

        AddLogEntry("Ruins", $"Drew ruins token: {tokenDef.Name}");

        // Handle different token types
        if (tokenDef.IsCombatToken && tokenDef.Enemies != null)
        {
            // Combat token - initiate combat with enemies
            return InitiateRuinsCombat(tokenDef);
        }
        else if (tokenDef.Effects != null && tokenDef.Effects.Any())
        {
            // Loot token - apply effects or queue choices
            return ApplyRuinsTokenEffects(tokenDef);
        }

        return GameActionResult.Ok($"Drew ruins token: {tokenDef.Name}");
    }

    private GameActionResult InitiateRuinsCombat(Definitions.RuinsDefinition tokenDef)
    {
        if (tokenDef.Enemies == null || !tokenDef.Enemies.Any())
            return GameActionResult.Fail("No enemies on this ruins token");

        // Create combat state with enemies from the token
        var combatEnemies = new List<CombatEnemy>();

        foreach (var enemyConfig in tokenDef.Enemies)
        {
            // Draw enemies of the specified type
            var enemyType = enemyConfig.Type;
            if (!_state.Decks.EnemyDecks.TryGetValue(enemyType, out var enemyDeck) || !enemyDeck.Any())
            {
                // Try to find enemies of this type
                var availableEnemies = _definitions.GetEnemiesByTypeAsync(enemyType).Result.ToList();
                if (!availableEnemies.Any())
                {
                    AddLogEntry("Ruins", $"No {enemyType} enemies available");
                    continue;
                }

                // Use a random enemy of this type
                var enemyDef = availableEnemies[_random.Next(availableEnemies.Count)];
                for (int i = 0; i < enemyConfig.Count; i++)
                {
                    combatEnemies.Add(CreateCombatEnemy(enemyDef));
                }
            }
            else
            {
                // Draw from the deck
                for (int i = 0; i < enemyConfig.Count && enemyDeck.Any(); i++)
                {
                    var enemyId = enemyDeck[0];
                    enemyDeck.RemoveAt(0);
                    var enemyDef = _definitions.GetEnemiesAsync().Result.FirstOrDefault(e => e.Id == enemyId);
                    if (enemyDef != null)
                    {
                        combatEnemies.Add(CreateCombatEnemy(enemyDef));
                    }
                }
            }
        }

        if (!combatEnemies.Any())
        {
            // No enemies to fight - resolve token as empty
            _state.ActiveRuinsToken!.IsResolved = true;
            _state.ActiveRuinsToken = null;
            AddLogEntry("Ruins", "Ruins token had no enemies - resolved");
            return GameActionResult.Ok("No enemies found in the ruins!");
        }

        var player = GetCurrentPlayer();

        _state.Combat = new CombatState
        {
            Position = player!.Position,
            Enemies = combatEnemies,
            Phase = combatEnemies.Any(e => e.IsSwift) ? CombatPhase.SwiftAttack : CombatPhase.RangedAttack,
            SiteType = "Ruins",
            IsNightRules = false
        };

        _state.Phase = GamePhase.Combat;

        AddLogEntry("Combat", $"Ruins combat! Fighting {combatEnemies.Count} enemies from {tokenDef.Name}");
        return GameActionResult.Ok($"Ruins combat! Fight {combatEnemies.Count} enemies!");
    }

    private CombatEnemy CreateCombatEnemy(Definitions.EnemyDefinition enemyDef)
    {
        return new CombatEnemy
        {
            EnemyId = enemyDef.Id,
            Name = enemyDef.Name,
            Armor = enemyDef.Armor.Value,
            Attack = enemyDef.Attack.Value,
            AttackType = enemyDef.Attack.GetElement(),
            IsRangedAttack = enemyDef.Attack.IsRanged,
            Resistances = enemyDef.Armor.Resistances,
            Abilities = enemyDef.Abilities,
            Fame = enemyDef.Fame,
            CurrentDamage = 0,
            IsDefeated = false,
            IsBlocked = false,
            SummonType = enemyDef.SummonType
        };
    }

    private GameActionResult ApplyRuinsTokenEffects(Definitions.RuinsDefinition tokenDef)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        var messages = new List<string>();
        var hasChoices = false;

        foreach (var effect in tokenDef.Effects!)
        {
            switch (effect.Type.ToLower())
            {
                case "gaincrystal":
                    // Add choice for crystal colors
                    var crystalAmount = effect.Value ?? 1;
                    _state.ActiveRuinsToken!.PendingChoices!.Add(new RuinsChoice
                    {
                        ChoiceType = "CrystalColor",
                        Description = $"Choose {crystalAmount} crystal color(s)",
                        Amount = crystalAmount,
                        Options = new List<string> { "Red", "Blue", "Green", "White" },
                        IsResolved = false
                    });
                    hasChoices = true;
                    break;

                case "gainmana":
                    // Add choice for mana color
                    var manaAmount = effect.Value ?? 1;
                    _state.ActiveRuinsToken!.PendingChoices!.Add(new RuinsChoice
                    {
                        ChoiceType = "ManaColor",
                        Description = $"Choose {manaAmount} mana token color(s)",
                        Amount = manaAmount,
                        Options = new List<string> { "Red", "Blue", "Green", "White", "Black", "Gold" },
                        IsResolved = false
                    });
                    hasChoices = true;
                    break;

                case "gaincard":
                    if (effect.Target == "SpellOffer")
                    {
                        // Add spell from offer
                        if (_state.Decks.Spells.Any())
                        {
                            var spellId = _state.Decks.Spells[0];
                            _state.Decks.Spells.RemoveAt(0);
                            player.Spells.Add(spellId);
                            var spellDef = _definitions.GetSpellsAsync().Result.FirstOrDefault(s => s.Id == spellId);
                            messages.Add($"Gained spell: {spellDef?.Name ?? spellId}");
                        }
                    }
                    else if (effect.Target == "ArtifactDeck")
                    {
                        // Draw artifact
                        if (_state.Decks.Artifacts.Any())
                        {
                            var artifactId = _state.Decks.Artifacts[0];
                            _state.Decks.Artifacts.RemoveAt(0);
                            player.Artifacts.Add(artifactId);
                            var artifactDef = _definitions.GetArtifactsAsync().Result.FirstOrDefault(a => a.Id == artifactId);
                            messages.Add($"Gained artifact: {artifactDef?.Name ?? artifactId}");
                        }
                    }
                    break;

                case "recruit":
                    // Free recruit - add choice for unit
                    _state.ActiveRuinsToken!.PendingChoices!.Add(new RuinsChoice
                    {
                        ChoiceType = "UnitFromOffer",
                        Description = "Choose a unit to recruit for free",
                        Amount = 1,
                        Options = _state.Decks.RegularUnits.Take(3).Concat(_state.Decks.EliteUnits.Take(2)).ToList(),
                        IsResolved = false
                    });
                    hasChoices = true;
                    break;
            }
        }

        if (!hasChoices)
        {
            // All effects applied immediately - resolve token
            _state.ActiveRuinsToken!.IsResolved = true;
            _state.ActiveRuinsToken = null;
        }

        var resultMessage = string.Join("; ", messages);
        if (hasChoices)
        {
            resultMessage = $"{tokenDef.Name}: Make your choices!";
        }

        return GameActionResult.Ok(string.IsNullOrEmpty(resultMessage) ? tokenDef.Description : resultMessage);
    }

    /// <summary>
    /// Resolves a pending choice for the active ruins token.
    /// </summary>
    public GameActionResult ResolveRuinsChoice(int choiceIndex, string selection)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (_state.ActiveRuinsToken == null)
            return GameActionResult.Fail("No active ruins token");

        if (_state.ActiveRuinsToken.PendingChoices == null ||
            choiceIndex < 0 ||
            choiceIndex >= _state.ActiveRuinsToken.PendingChoices.Count)
            return GameActionResult.Fail("Invalid choice index");

        var choice = _state.ActiveRuinsToken.PendingChoices[choiceIndex];
        if (choice.IsResolved)
            return GameActionResult.Fail("Choice already resolved");

        switch (choice.ChoiceType)
        {
            case "CrystalColor":
                return ApplyCrystalChoice(player, choice, selection);

            case "ManaColor":
                return ApplyManaChoice(player, choice, selection);

            case "UnitFromOffer":
                return ApplyUnitChoice(player, choice, selection);

            default:
                return GameActionResult.Fail($"Unknown choice type: {choice.ChoiceType}");
        }
    }

    private GameActionResult ApplyCrystalChoice(PlayerState player, RuinsChoice choice, string color)
    {
        switch (color.ToLower())
        {
            case "red":
                player.Crystals.Red++;
                break;
            case "blue":
                player.Crystals.Blue++;
                break;
            case "green":
                player.Crystals.Green++;
                break;
            case "white":
                player.Crystals.White++;
                break;
            default:
                return GameActionResult.Fail("Invalid crystal color");
        }

        choice.Amount--;
        if (choice.Amount <= 0)
        {
            choice.IsResolved = true;
            CheckAllRuinsChoicesResolved();
        }

        AddLogEntry("Ruins", $"Gained 1 {color} Crystal");
        return GameActionResult.Ok($"Gained 1 {color} Crystal!");
    }

    private GameActionResult ApplyManaChoice(PlayerState player, RuinsChoice choice, string color)
    {
        switch (color.ToLower())
        {
            case "red":
                player.ManaTokens.Red++;
                break;
            case "blue":
                player.ManaTokens.Blue++;
                break;
            case "green":
                player.ManaTokens.Green++;
                break;
            case "white":
                player.ManaTokens.White++;
                break;
            case "black":
                player.ManaTokens.Black++;
                break;
            case "gold":
                player.ManaTokens.Gold++;
                break;
            default:
                return GameActionResult.Fail("Invalid mana color");
        }

        choice.Amount--;
        if (choice.Amount <= 0)
        {
            choice.IsResolved = true;
            CheckAllRuinsChoicesResolved();
        }

        AddLogEntry("Ruins", $"Gained 1 {color} Mana Token");
        return GameActionResult.Ok($"Gained 1 {color} Mana Token!");
    }

    private GameActionResult ApplyUnitChoice(PlayerState player, RuinsChoice choice, string unitId)
    {
        if (player.Units.Count >= player.CommandTokens)
            return GameActionResult.Fail($"Unit limit reached ({player.CommandTokens})");

        var unitDef = _definitions.GetUnitsAsync().Result.FirstOrDefault(u => u.Id == unitId);
        if (unitDef == null)
            return GameActionResult.Fail("Invalid unit");

        // Remove from offer
        if (_state.Decks.RegularUnits.Contains(unitId))
            _state.Decks.RegularUnits.Remove(unitId);
        else if (_state.Decks.EliteUnits.Contains(unitId))
            _state.Decks.EliteUnits.Remove(unitId);

        // Add to player
        player.Units.Add(new UnitState
        {
            UnitId = unitId,
            Name = unitDef.Name,
            Armor = unitDef.Armor,
            IsReady = true,
            IsWounded = false,
            UsedThisCombat = false
        });

        choice.IsResolved = true;
        CheckAllRuinsChoicesResolved();

        AddLogEntry("Ruins", $"Recruited {unitDef.Name} for free!");
        return GameActionResult.Ok($"Recruited {unitDef.Name}!");
    }

    private void CheckAllRuinsChoicesResolved()
    {
        if (_state.ActiveRuinsToken == null) return;

        var allResolved = _state.ActiveRuinsToken.PendingChoices?.All(c => c.IsResolved) ?? true;
        if (allResolved)
        {
            _state.ActiveRuinsToken.IsResolved = true;
            _state.ActiveRuinsToken = null;
            AddLogEntry("Ruins", "Ruins token fully resolved");
        }
    }

    /// <summary>
    /// Gets the currently active ruins token being resolved, if any.
    /// </summary>
    public ActiveRuinsToken? GetActiveRuinsToken()
    {
        return _state.ActiveRuinsToken;
    }

    // ==================== LEVEL UP SYSTEM ====================

    // Fame thresholds for each level (from leveling.json)
    private static readonly int[] LevelThresholds = { 0, 3, 8, 15, 24, 35, 48, 64, 82, 104 };
    private static readonly int[] CommandTokensByLevel = { 2, 3, 3, 3, 4, 4, 4, 4, 5, 5 };
    private static readonly int[] ArmorByLevel = { 2, 2, 2, 3, 3, 3, 3, 4, 4, 4 };
    private static readonly int[] HandSizeByLevel = { 5, 5, 5, 5, 5, 5, 6, 6, 6, 6 };

    public bool CanLevelUp()
    {
        var player = GetCurrentPlayer();
        if (player == null) return false;

        var nextLevel = player.Level + 1;
        if (nextLevel > 10) return false; // Max level

        var fameRequired = GetFameForNextLevel();
        return player.Fame >= fameRequired;
    }

    public int GetFameForNextLevel()
    {
        var player = GetCurrentPlayer();
        if (player == null) return int.MaxValue;

        var nextLevel = player.Level + 1;
        if (nextLevel > 10) return int.MaxValue;

        return LevelThresholds[nextLevel - 1];
    }

    public IEnumerable<string> GetAvailableAdvancedActions()
    {
        // Get advanced actions from the offer (dummy deck)
        // In a full implementation, this would be from the advanced action offer
        var allActions = _definitions.GetAdvancedActionsAsync().Result;
        var player = GetCurrentPlayer();
        if (player == null) return Enumerable.Empty<string>();

        // Return actions not already in player's deck
        var playerCards = player.DeedDeck.Concat(player.Hand).Concat(player.DiscardPile).ToHashSet();
        return allActions
            .Where(a => !playerCards.Contains(a.Id))
            .Take(3) // Offer 3 choices
            .Select(a => a.Id);
    }

    public IEnumerable<string> GetAvailableSkills()
    {
        var player = GetCurrentPlayer();
        if (player == null) return Enumerable.Empty<string>();

        var allSkills = _definitions.GetSkillsAsync().Result;
        var playerSkills = player.Skills.ToHashSet();

        // Get hero-specific skills first
        var heroSkills = allSkills
            .Where(s => s.Hero == player.HeroId && !playerSkills.Contains(s.Id))
            .ToList();

        // If no hero-specific skills available, offer common skills
        if (!heroSkills.Any())
        {
            heroSkills = allSkills
                .Where(s => string.IsNullOrEmpty(s.Hero) && !playerSkills.Contains(s.Id))
                .Take(3)
                .ToList();
        }

        return heroSkills.Select(s => s.Id);
    }

    public GameActionResult LevelUp(string? advancedActionId, string? skillId)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (!CanLevelUp())
            return GameActionResult.Fail("Not enough fame to level up");

        var newLevel = player.Level + 1;
        var oldLevel = player.Level;

        // Update player stats
        player.Level = newLevel;
        player.CommandTokens = CommandTokensByLevel[newLevel - 1];
        player.Armor = ArmorByLevel[newLevel - 1];
        player.HandLimit = HandSizeByLevel[newLevel - 1];

        // Determine reward type based on level
        var rewardType = GetLevelReward(newLevel);
        var rewardMessage = "";

        if (rewardType == "AdvancedAction+Skill")
        {
            // Add advanced action to deck
            if (!string.IsNullOrEmpty(advancedActionId))
            {
                var actionDef = _definitions.GetAdvancedActionsAsync().Result
                    .FirstOrDefault(a => a.Id == advancedActionId);
                if (actionDef != null)
                {
                    player.DeedDeck.Add(advancedActionId);
                    rewardMessage += $"Gained {actionDef.Name}. ";
                }
            }

            // Add skill
            if (!string.IsNullOrEmpty(skillId))
            {
                var skillDef = _definitions.GetSkillsAsync().Result
                    .FirstOrDefault(s => s.Id == skillId);
                if (skillDef != null)
                {
                    player.Skills.Add(skillId);
                    rewardMessage += $"Learned {skillDef.Name}. ";
                }
            }
        }
        else if (rewardType == "CommandToken")
        {
            rewardMessage = "Gained Command Token. ";
        }

        // Update armor and hand size message
        if (ArmorByLevel[newLevel - 1] > ArmorByLevel[oldLevel - 1])
        {
            rewardMessage += $"Armor increased to {player.Armor}. ";
        }
        if (HandSizeByLevel[newLevel - 1] > HandSizeByLevel[oldLevel - 1])
        {
            rewardMessage += $"Hand size increased to {player.HandLimit}. ";
        }

        AddLogEntry("LevelUp", $"Leveled up to {newLevel}! {rewardMessage}");
        return GameActionResult.Ok($"Leveled up to {newLevel}! {rewardMessage}");
    }

    private string GetLevelReward(int level)
    {
        // Even levels (2, 4, 6, 8, 10) give Advanced Action + Skill
        // Odd levels (3, 5, 7, 9) give Command Token
        if (level % 2 == 0)
            return "AdvancedAction+Skill";
        else
            return "CommandToken";
    }

    /// <summary>
    /// Checks and applies level up if player has enough fame.
    /// Called automatically after gaining fame.
    /// </summary>
    private void CheckForLevelUp()
    {
        var player = GetCurrentPlayer();
        if (player == null) return;

        while (CanLevelUp())
        {
            // Auto-level up for command token levels (3, 5, 7, 9)
            var nextLevel = player.Level + 1;
            if (GetLevelReward(nextLevel) == "CommandToken")
            {
                LevelUp(null, null);
            }
            else
            {
                // For skill/action levels, notify player to choose
                AddLogEntry("LevelUp", $"Ready to level up to {nextLevel}! Choose an Advanced Action and Skill.");
                break;
            }
        }
    }

    // ==================== UNIT COMBAT OPERATIONS ====================

    public GameActionResult ActivateUnit(int unitIndex, string abilityType, int? enemyIndex = null)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (unitIndex < 0 || unitIndex >= player.Units.Count)
            return GameActionResult.Fail("Invalid unit index");

        var unit = player.Units[unitIndex];
        
        // Check if unit can be activated
        if (!unit.IsReady)
            return GameActionResult.Fail($"{unit.Name} is exhausted and cannot be activated");

        if (unit.UsedThisCombat)
            return GameActionResult.Fail($"{unit.Name} has already been used this combat");

        // Get unit definition for abilities
        var unitDef = _definitions.GetUnitsAsync().Result.FirstOrDefault(u => u.Id == unit.UnitId);
        if (unitDef == null)
            return GameActionResult.Fail("Unit definition not found");

        var abilities = Definitions.UnitAbilities.FromUnitDefinition(unitDef);
        var abilityTypeLower = abilityType.ToLower();

        // Apply the ability based on type and current combat phase
        switch (abilityTypeLower)
        {
            case "attack":
                return UseUnitAttack(player, unit, unitDef, abilities, enemyIndex);

            case "ranged":
            case "ranged_attack":
                return UseUnitRangedAttack(player, unit, unitDef, abilities, enemyIndex);

            case "block":
                return UseUnitBlock(player, unit, unitDef, abilities, enemyIndex);

            case "influence":
                return UseUnitInfluence(player, unit, unitDef, abilities);

            case "move":
                return UseUnitMove(player, unit, unitDef, abilities);

            case "heal":
                return UseUnitHeal(player, unit, unitDef, abilities);

            default:
                return GameActionResult.Fail($"Unknown ability type: {abilityType}");
        }
    }

    private GameActionResult UseUnitAttack(PlayerState player, UnitState unit, Definitions.UnitDefinition unitDef, Definitions.UnitAbilities abilities, int? enemyIndex)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.Attack)
            return GameActionResult.Fail("Can only use attack ability during attack phase");

        if (abilities.Attack <= 0)
            return GameActionResult.Fail($"{unit.Name} has no attack ability");

        if (!enemyIndex.HasValue)
            return GameActionResult.Fail("Must specify an enemy to attack");

        if (enemyIndex.Value < 0 || enemyIndex.Value >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex.Value];
        if (enemy.IsDefeated)
            return GameActionResult.Fail("Enemy already defeated");

        // Mark unit as used
        unit.UsedThisCombat = true;
        unit.IsReady = false; // Exhaust the unit

        // Calculate effective armor
        var element = abilities.AttackElement ?? "Physical";
        var effectiveArmor = CalculateEffectiveArmor(enemy, element, true);

        if (abilities.Attack >= effectiveArmor)
        {
            DefeatEnemy(enemy, player, $"{unit.Name}'s {element} attack");
            return GameActionResult.Ok($"{unit.Name} defeated {enemy.Name} with {abilities.Attack} {element} attack!");
        }
        else
        {
            // Wounded units that attack ineffectively are destroyed
            if (unit.IsWounded)
            {
                player.Units.Remove(unit);
                AddLogEntry("Combat", $"{unit.Name} was destroyed after ineffective attack while wounded!");
                return GameActionResult.Fail($"Attack failed and {unit.Name} was destroyed!");
            }

            // Paralyze effect
            if (enemy.IsParalyze)
            {
                unit.IsWounded = true;
                AddLogEntry("Combat", $"Paralyze! {unit.Name} was wounded by ineffective attack!");
                return GameActionResult.Fail($"Attack ineffective - {unit.Name} was wounded by Paralyze!");
            }

            AddLogEntry("Combat", $"{unit.Name}'s attack failed - need {effectiveArmor} attack, had {abilities.Attack}");
            return GameActionResult.Fail($"Attack too weak - need {effectiveArmor} to defeat");
        }
    }

    private GameActionResult UseUnitRangedAttack(PlayerState player, UnitState unit, Definitions.UnitDefinition unitDef, Definitions.UnitAbilities abilities, int? enemyIndex)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.RangedAttack)
            return GameActionResult.Fail("Can only use ranged attack during ranged attack phase");

        if (abilities.Attack <= 0 || !abilities.IsRanged)
            return GameActionResult.Fail($"{unit.Name} has no ranged attack ability");

        if (!enemyIndex.HasValue)
            return GameActionResult.Fail("Must specify an enemy to attack");

        if (enemyIndex.Value < 0 || enemyIndex.Value >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex.Value];
        if (enemy.IsDefeated)
            return GameActionResult.Fail("Enemy already defeated");

        // Fortified enemies require siege
        if (enemy.IsFortified && !abilities.IsSiege)
            return GameActionResult.Fail("Fortified enemies require Siege attack for ranged attacks");

        // Mark unit as used
        unit.UsedThisCombat = true;
        unit.IsReady = false;

        var element = abilities.AttackElement ?? "Physical";
        var effectiveArmor = CalculateEffectiveArmor(enemy, element, false);

        if (abilities.Attack >= effectiveArmor)
        {
            DefeatEnemy(enemy, player, $"{unit.Name}'s ranged {element} attack");
            return GameActionResult.Ok($"{unit.Name} defeated {enemy.Name} with ranged attack!");
        }
        else
        {
            if (unit.IsWounded)
            {
                player.Units.Remove(unit);
                return GameActionResult.Fail($"Attack failed and {unit.Name} was destroyed!");
            }

            if (enemy.IsParalyze)
            {
                unit.IsWounded = true;
                return GameActionResult.Fail($"Attack ineffective - {unit.Name} was wounded by Paralyze!");
            }

            return GameActionResult.Fail($"Attack too weak - need {effectiveArmor} to defeat");
        }
    }

    private GameActionResult UseUnitBlock(PlayerState player, UnitState unit, Definitions.UnitDefinition unitDef, Definitions.UnitAbilities abilities, int? enemyIndex)
    {
        if (_state.Combat == null)
            return GameActionResult.Fail("Not in combat");

        if (_state.Combat.Phase != CombatPhase.Block && _state.Combat.Phase != CombatPhase.SwiftAttack)
            return GameActionResult.Fail("Can only use block ability during block phase");

        if (abilities.Block <= 0)
            return GameActionResult.Fail($"{unit.Name} has no block ability");

        if (!enemyIndex.HasValue)
            return GameActionResult.Fail("Must specify an enemy to block");

        if (enemyIndex.Value < 0 || enemyIndex.Value >= _state.Combat.Enemies.Count)
            return GameActionResult.Fail("Invalid enemy index");

        var enemy = _state.Combat.Enemies[enemyIndex.Value];
        if (enemy.IsDefeated || enemy.IsBlocked)
            return GameActionResult.Fail("Enemy already defeated or blocked");

        // In swift phase, can only block swift enemies
        if (_state.Combat.Phase == CombatPhase.SwiftAttack && !enemy.IsSwift)
            return GameActionResult.Fail("Can only block Swift enemies in this phase");

        // Mark unit as used
        unit.UsedThisCombat = true;
        unit.IsReady = false;

        // Check block element vs attack type
        var canBlockElement = CanBlockAttackType(abilities, enemy.AttackType);
        if (!canBlockElement)
        {
            // Physical block can always be used but may be less effective
            AddLogEntry("Combat", $"{unit.Name}'s block may be less effective against {enemy.AttackType} attacks");
        }

        if (abilities.Block >= enemy.Attack)
        {
            enemy.IsBlocked = true;
            AddLogEntry("Combat", $"{unit.Name} fully blocked {enemy.Name}'s attack ({abilities.Block} vs {enemy.Attack})");
            return GameActionResult.Ok($"{unit.Name} blocked {enemy.Name}'s attack!");
        }
        else
        {
            var remainingDamage = enemy.Attack - abilities.Block;
            _state.Combat.TotalUnblockedDamage += remainingDamage;

            // Unit takes the unblocked damage as wound
            if (!unit.IsWounded)
            {
                unit.IsWounded = true;
                AddLogEntry("Combat", $"{unit.Name} partially blocked {enemy.Name} and was wounded!");
                return GameActionResult.Ok($"Partial block - {unit.Name} was wounded, {remainingDamage} damage unblocked");
            }
            else
            {
                // Already wounded unit is destroyed
                player.Units.Remove(unit);
                AddLogEntry("Combat", $"{unit.Name} was destroyed blocking {enemy.Name}!");
                return GameActionResult.Ok($"Partial block - {unit.Name} was destroyed, {remainingDamage} damage unblocked");
            }
        }
    }

    private bool CanBlockAttackType(Definitions.UnitAbilities abilities, string attackType)
    {
        if (string.IsNullOrEmpty(abilities.BlockElement))
            return true; // Physical block works against everything

        var blockElement = abilities.BlockElement.ToLower();
        var attackTypeLower = attackType.ToLower();

        // Ice block can block Fire attacks
        if (blockElement == "ice" && attackTypeLower == "fire")
            return true;
        // Fire block can block Ice attacks
        if (blockElement == "fire" && attackTypeLower == "ice")
            return true;
        // Matching elements
        if (blockElement == attackTypeLower)
            return true;

        return false;
    }

    private GameActionResult UseUnitInfluence(PlayerState player, UnitState unit, Definitions.UnitDefinition unitDef, Definitions.UnitAbilities abilities)
    {
        if (abilities.Influence <= 0)
            return GameActionResult.Fail($"{unit.Name} has no influence ability");

        // Mark unit as used (but not exhausted for influence)
        unit.UsedThisCombat = true;
        unit.IsReady = false;

        player.InfluencePool += abilities.Influence;
        AddLogEntry("Unit", $"{unit.Name} provided +{abilities.Influence} influence");
        return GameActionResult.Ok($"{unit.Name} provided +{abilities.Influence} influence!");
    }

    private GameActionResult UseUnitMove(PlayerState player, UnitState unit, Definitions.UnitDefinition unitDef, Definitions.UnitAbilities abilities)
    {
        if (abilities.Move <= 0)
            return GameActionResult.Fail($"{unit.Name} has no move ability");

        // Mark unit as used
        unit.UsedThisCombat = true;
        unit.IsReady = false;

        player.MovementRemaining += abilities.Move;
        AddLogEntry("Unit", $"{unit.Name} provided +{abilities.Move} movement");
        return GameActionResult.Ok($"{unit.Name} provided +{abilities.Move} movement!");
    }

    private GameActionResult UseUnitHeal(PlayerState player, UnitState unit, Definitions.UnitDefinition unitDef, Definitions.UnitAbilities abilities)
    {
        if (abilities.Heal <= 0)
            return GameActionResult.Fail($"{unit.Name} has no heal ability");

        // Mark unit as used
        unit.UsedThisCombat = true;
        unit.IsReady = false;

        player.HealPool += abilities.Heal;
        AddLogEntry("Unit", $"{unit.Name} provided +{abilities.Heal} healing");
        return GameActionResult.Ok($"{unit.Name} provided +{abilities.Heal} healing!");
    }

    public GameActionResult AssignDamageToUnit(int unitIndex, int damage)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (unitIndex < 0 || unitIndex >= player.Units.Count)
            return GameActionResult.Fail("Invalid unit index");

        var unit = player.Units[unitIndex];

        if (!unit.IsWounded)
        {
            // Unit takes 1 wound
            unit.IsWounded = true;
            AddLogEntry("Combat", $"{unit.Name} was wounded absorbing {damage} damage");
            return GameActionResult.Ok($"{unit.Name} was wounded!");
        }
        else
        {
            // Already wounded unit is destroyed
            player.Units.Remove(unit);
            AddLogEntry("Combat", $"{unit.Name} was destroyed absorbing {damage} damage");
            return GameActionResult.Ok($"{unit.Name} was destroyed!");
        }
    }

    public IEnumerable<UnitCombatOption> GetAvailableUnitActions()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            yield break;

        for (int i = 0; i < player.Units.Count; i++)
        {
            var unit = player.Units[i];
            var unitDef = _definitions.GetUnitsAsync().Result.FirstOrDefault(u => u.Id == unit.UnitId);
            if (unitDef == null) continue;

            var abilities = Definitions.UnitAbilities.FromUnitDefinition(unitDef);
            var option = new UnitCombatOption
            {
                UnitIndex = i,
                UnitId = unit.UnitId,
                UnitName = unit.Name,
                Armor = unit.Armor,
                IsWounded = unit.IsWounded,
                IsReady = unit.IsReady,
                UsedThisCombat = unit.UsedThisCombat,
                AvailableAbilities = new List<UnitAbilityOption>()
            };

            // Add available abilities based on current phase
            if (_state.Combat != null)
            {
                // Attack abilities
                if (abilities.Attack > 0)
                {
                    if (_state.Combat.Phase == CombatPhase.Attack)
                    {
                        option.AvailableAbilities.Add(new UnitAbilityOption
                        {
                            AbilityType = "Attack",
                            Value = abilities.Attack,
                            Element = abilities.AttackElement,
                            Description = $"Attack {abilities.Attack}{(abilities.AttackElement != null ? $" ({abilities.AttackElement})" : "")}"
                        });
                    }

                    if (abilities.IsRanged && _state.Combat.Phase == CombatPhase.RangedAttack)
                    {
                        option.AvailableAbilities.Add(new UnitAbilityOption
                        {
                            AbilityType = "Ranged",
                            Value = abilities.Attack,
                            Element = abilities.AttackElement,
                            IsRanged = true,
                            IsSiege = abilities.IsSiege,
                            Description = $"Ranged Attack {abilities.Attack}{(abilities.IsSiege ? " (Siege)" : "")}{(abilities.AttackElement != null ? $" ({abilities.AttackElement})" : "")}"
                        });
                    }
                }

                // Block abilities
                if (abilities.Block > 0 && (_state.Combat.Phase == CombatPhase.Block || _state.Combat.Phase == CombatPhase.SwiftAttack))
                {
                    option.AvailableAbilities.Add(new UnitAbilityOption
                    {
                        AbilityType = "Block",
                        Value = abilities.Block,
                        Element = abilities.BlockElement,
                        Description = $"Block {abilities.Block}{(abilities.BlockElement != null ? $" ({abilities.BlockElement})" : "")}"
                    });
                }
            }

            // Non-combat abilities (always available when unit is ready)
            if (abilities.Influence > 0 && unit.IsReady && !unit.UsedThisCombat)
            {
                option.AvailableAbilities.Add(new UnitAbilityOption
                {
                    AbilityType = "Influence",
                    Value = abilities.Influence,
                    Description = $"Influence {abilities.Influence}"
                });
            }

            if (abilities.Move > 0 && unit.IsReady && !unit.UsedThisCombat && _state.Combat == null)
            {
                option.AvailableAbilities.Add(new UnitAbilityOption
                {
                    AbilityType = "Move",
                    Value = abilities.Move,
                    Description = $"Move {abilities.Move}"
                });
            }

            if (abilities.Heal > 0 && unit.IsReady && !unit.UsedThisCombat)
            {
                option.AvailableAbilities.Add(new UnitAbilityOption
                {
                    AbilityType = "Heal",
                    Value = abilities.Heal,
                    Description = $"Heal {abilities.Heal}"
                });
            }

            yield return option;
        }
    }

    public GameActionResult HealUnit(int unitIndex)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (unitIndex < 0 || unitIndex >= player.Units.Count)
            return GameActionResult.Fail("Invalid unit index");

        var unit = player.Units[unitIndex];

        if (!unit.IsWounded)
            return GameActionResult.Fail($"{unit.Name} is not wounded");

        if (player.HealPool < 2) // Units require 2 heal to recover
            return GameActionResult.Fail("Not enough healing (need 2 heal to heal a unit)");

        unit.IsWounded = false;
        player.HealPool -= 2;

        AddLogEntry("Heal", $"Healed {unit.Name}");
        return GameActionResult.Ok($"Healed {unit.Name}!");
    }

    public GameActionResult DisbandUnit(int unitIndex)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (unitIndex < 0 || unitIndex >= player.Units.Count)
            return GameActionResult.Fail("Invalid unit index");

        var unit = player.Units[unitIndex];
        var unitName = unit.Name;
        player.Units.RemoveAt(unitIndex);

        AddLogEntry("Disband", $"Disbanded {unitName}");
        return GameActionResult.Ok($"Disbanded {unitName}");
    }

    /// <summary>
    /// Resets unit combat state at the end of combat.
    /// </summary>
    private void ResetUnitsCombatState(PlayerState player)
    {
        foreach (var unit in player.Units)
        {
            unit.UsedThisCombat = false;
        }
    }

    /// <summary>
    /// Readies all units at the start of a new round.
    /// </summary>
    private void ReadyAllUnits(PlayerState player)
    {
        foreach (var unit in player.Units)
        {
            unit.IsReady = true;
            unit.UsedThisCombat = false;
        }
    }

    #region Victory Conditions

    public bool CheckVictoryConditions()
    {
        // Check if game is already over
        if (_state.Victory?.IsGameOver == true)
            return true;

        // Get scenario
        var scenarios = _definitions.GetScenariosAsync().Result;
        var scenario = scenarios.FirstOrDefault(s => s.Id == _state.ScenarioId);

        // Get total rounds from scenario
        var totalRounds = _definitions.GetScenariosAsync().Result
            .FirstOrDefault(s => s.Id == _state.ScenarioId)?.Rounds ?? 6;

        // Check if all rounds are complete
        if (_state.Round > totalRounds)
        {
            var victory = CalculateFinalScores();
            victory.VictoryType = VictoryType.TimeOut;
            victory.EndReason = "All rounds completed";
            _state.Victory = victory;
            return true;
        }

        // Check scenario-specific victory conditions
        if (scenario != null)
        {
            // Check city conquest
            if (scenario.Goal?.Contains("Conquer all cities") == true || 
                scenario.Goal?.Contains("Conquer") == true && _state.TotalCities > 0)
            {
                if (_state.CitiesConquered >= _state.TotalCities && _state.TotalCities > 0)
                {
                    var victory = CalculateFinalScores();
                    victory.VictoryType = VictoryType.CityConquest;
                    victory.EndReason = "All cities conquered!";
                    _state.Victory = victory;
                    return true;
                }
            }

            // Check "Reveal the City" goal (training scenario)
            if (scenario.Goal?.Contains("Reveal the City") == true)
            {
                if (_state.CityRevealed)
                {
                    var victory = CalculateFinalScores();
                    victory.VictoryType = VictoryType.ScenarioGoal;
                    victory.EndReason = "City tile revealed!";
                    _state.Victory = victory;
                    return true;
                }
            }

            // Check "Conquer Dungeon/Tomb/MonsterDen/SpawningGrounds" goal (Druid Nights)
            if (scenario.Goal?.Contains("Dungeon/Tomb/MonsterDen/SpawningGrounds") == true)
            {
                var adventureSitesConquered = CountAllAdventureSitesConquered();
                if (adventureSitesConquered >= 4) // Requires at least 4 adventure sites
                {
                    var victory = CalculateFinalScores();
                    victory.VictoryType = VictoryType.ScenarioGoal;
                    victory.EndReason = "All required adventure sites conquered!";
                    _state.Victory = victory;
                    return true;
                }
            }

            // Check "Hold adventure sites and cities" goal (Conquer and Hold)
            if (scenario.Goal?.Contains("Hold adventure sites") == true)
            {
                // This scenario uses end-of-game scoring, not early victory
                // Victory is determined when rounds end
            }

            // Check "Conquer mines and the city" goal (Mines Liberation)
            if (scenario.Goal?.Contains("Conquer mines") == true)
            {
                var minesConquered = CountMinesConquered();
                var cityConquered = _state.CitiesConquered >= 1;
                if (minesConquered >= 3 && cityConquered)
                {
                    var victory = CalculateFinalScores();
                    victory.VictoryType = VictoryType.ScenarioGoal;
                    victory.EndReason = "All mines and the city conquered!";
                    _state.Victory = victory;
                    return true;
                }
            }
        }

        return false;
    }

    private int CountAllAdventureSitesConquered()
    {
        var adventureSiteTypes = new[] { "Dungeon", "Tomb", "MonsterDen", "SpawningGrounds" };
        return _state.Map.HexData.Values
            .Where(h => h.IsConquered && 
                       adventureSiteTypes.Any(t => h.SiteType?.Contains(t) == true))
            .Count();
    }

    private int CountMinesConquered()
    {
        return _state.Map.HexData.Values
            .Where(h => h.IsConquered && h.SiteType?.Contains("Mine") == true)
            .Count();
    }

    public VictoryState CalculateFinalScores()
    {
        var victory = new VictoryState
        {
            IsGameOver = true,
            FinalScores = new List<PlayerScore>()
        };

        foreach (var player in _state.Players)
        {
            var score = new PlayerScore
            {
                UserId = player.UserId,
                HeroName = player.HeroId,
                Fame = player.Fame,
                ReputationBonus = CalculateReputationBonus(player.Reputation),
                CitiesConquered = CountPlayerCitiesConquered(player),
                AdventureSitesConquered = CountPlayerAdventureSitesConquered(player),
                ArtifactsCount = player.Artifacts.Count,
                SpellsCount = player.Spells.Count,
                AdvancedActionsCount = player.AdvancedActions.Count
            };

            // Calculate total score
            score.TotalScore = score.Fame + score.ReputationBonus +
                               (score.CitiesConquered * 10) +
                               (score.AdventureSitesConquered * 2) +
                               (score.ArtifactsCount * 2) +
                               (score.SpellsCount * 1) +
                               (score.AdvancedActionsCount * 1);

            victory.FinalScores.Add(score);
        }

        // Sort by total score and assign ranks
        var sortedScores = victory.FinalScores.OrderByDescending(s => s.TotalScore).ToList();
        for (int i = 0; i < sortedScores.Count; i++)
        {
            sortedScores[i].Rank = i + 1;
        }
        victory.FinalScores = sortedScores;

        // Set winners (could be multiple in case of tie)
        var highestScore = sortedScores.FirstOrDefault()?.TotalScore ?? 0;
        victory.WinnerUserIds = sortedScores
            .Where(s => s.TotalScore == highestScore)
            .Select(s => s.UserId)
            .ToList();

        return victory;
    }

    public GameActionResult EndGame(string reason)
    {
        if (_state.Victory?.IsGameOver == true)
            return GameActionResult.Fail("Game is already over");

        var victory = CalculateFinalScores();
        victory.EndReason = reason;
        victory.VictoryType = VictoryType.TimeOut;
        _state.Victory = victory;

        AddLogEntry("Game Over", reason);
        return GameActionResult.Ok($"Game ended: {reason}");
    }

    public VictoryState? GetVictoryState()
    {
        return _state.Victory;
    }

    private int CalculateReputationBonus(int reputation)
    {
        // Reputation ranges from -7 to +7
        // Negative reputation gives negative points
        // Positive reputation gives bonus points
        return reputation switch
        {
            >= 7 => 10,
            >= 5 => 7,
            >= 3 => 5,
            >= 1 => 3,
            0 => 0,
            >= -2 => -2,
            >= -4 => -5,
            >= -6 => -8,
            _ => -12
        };
    }

    private int CountPlayerCitiesConquered(PlayerState player)
    {
        // Count cities that were conquered by this player
        int count = 0;
        foreach (var hex in _state.Map.HexData.Values)
        {
            if (hex.SiteType == "City" && hex.IsConquered && hex.OwnerUserId == player.UserId)
            {
                count++;
            }
        }
        return count;
    }

    private int CountPlayerAdventureSitesConquered(PlayerState player)
    {
        // Count adventure sites conquered by this player
        int count = 0;
        var adventureSites = new[] { "Dungeon", "Tomb", "MonsterDen", "SpawningGrounds", "Keep", "MageTower", "Mage_Tower" };
        
        foreach (var hex in _state.Map.HexData.Values)
        {
            if (adventureSites.Contains(hex.SiteType) && hex.IsConquered && hex.OwnerUserId == player.UserId)
            {
                count++;
            }
        }
        return count;
    }

    #endregion
}
