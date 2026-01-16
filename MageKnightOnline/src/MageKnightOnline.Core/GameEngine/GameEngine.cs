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

        while (toVisit.Count > 0)
        {
            var (current, remaining) = toVisit.Dequeue();

            foreach (var dir in HexDirections)
            {
                var neighbor = current + dir;
                var key = PosKey(neighbor);

                // Get terrain cost
                var terrain = GetTerrainAt(neighbor);
                if (terrain == null) continue; // Off map

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

        if (player.MovementRemaining < cost)
            return GameActionResult.Fail($"Not enough movement points (need {cost}, have {player.MovementRemaining})");

        // Check if adjacent
        if (!IsAdjacent(player.Position, destination))
            return GameActionResult.Fail("Can only move to adjacent hexes");

        // Move the player
        var oldPosition = player.Position;
        player.Position = destination;
        player.MovementRemaining -= cost;

        // Check if we're at edge of revealed area - trigger exploration
        CheckForExploration(destination);

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

    private void CheckForExploration(HexPosition position)
    {
        // Check each adjacent hex - if any are unrevealed, we might be able to explore
        foreach (var dir in HexDirections)
        {
            var neighbor = position + dir;
            var key = PosKey(neighbor);
            
            // If this hex is not revealed, check if we should reveal a new tile
            if (!_state.Map.RevealedHexes.Contains(key))
            {
                // Check if there's a tile deck to draw from
                if (_state.Decks.CountrysideTiles.Any() || _state.Decks.CoreTiles.Any())
                {
                    // For now, auto-reveal adjacent unrevealed hexes when exploring
                    // In a full implementation, this would place entire tiles
                    RevealNewTile(neighbor);
                    break; // Only reveal one tile at a time
                }
            }
        }
    }

    private void RevealNewTile(HexPosition centerPosition)
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

        if (tileId == null) return;

        // Create the tile
        var tile = new MapTileState
        {
            TileId = tileId,
            Position = centerPosition,
            Rotation = _random.Next(6), // Random rotation
            IsRevealed = true
        };
        _state.Map.Tiles.Add(tile);

        // Generate hex data for the new tile (7 hexes in a hex pattern)
        var tileHexes = GenerateTileHexes(centerPosition, tileId);
        foreach (var (hexPos, hexState) in tileHexes)
        {
            var key = PosKey(hexPos);
            _state.Map.RevealedHexes.Add(key);
            _state.Map.HexData[key] = hexState;
        }

        AddLogEntry("Explore", $"Revealed new tile: {tileId} at ({centerPosition.Q}, {centerPosition.R})");
    }

    private List<(HexPosition Position, HexState State)> GenerateTileHexes(HexPosition center, string tileId)
    {
        var hexes = new List<(HexPosition, HexState)>();
        
        // Try to get tile definition from JSON
        var tileDef = _definitions.GetMapTilesAsync().Result.FirstOrDefault(t => t.Id == tileId);
        
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
        // Index 0 = center, 1-6 = surrounding hexes (clockwise from East)
        return index switch
        {
            0 => center, // Center
            1 => center + new HexPosition { Q = 1, R = 0 },   // East
            2 => center + new HexPosition { Q = 0, R = -1 },  // Northeast
            3 => center + new HexPosition { Q = -1, R = -1 }, // Northwest  
            4 => center + new HexPosition { Q = -1, R = 0 },  // West
            5 => center + new HexPosition { Q = 0, R = 1 },   // Southwest
            6 => center + new HexPosition { Q = 1, R = 1 },   // Southeast
            _ => center
        };
    }

    private List<string> GenerateEnemiesForSite(string? siteType)
    {
        if (string.IsNullOrEmpty(siteType)) return new List<string>();

        // Generate enemies based on site type
        return siteType switch
        {
            "Village" => new List<string>(), // Villages are friendly
            "Monastery" => new List<string>(), // Monasteries are friendly
            "Keep" => new List<string> { "enemy_keep_guardian" },
            "MageTower" => new List<string> { "enemy_mage" },
            "AncientRuins" => new List<string> { "enemy_orc_marauder" },
            "Dungeon" => new List<string> { "enemy_orc_marauder", "enemy_orc_marauder" },
            _ => new List<string>()
        };
    }

    public GameActionResult PlayCard(string cardId, bool powered = false, ManaColor? manaUsed = null)
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

        // Check if powered effect requires mana
        if (powered && !string.IsNullOrEmpty(card.ManaType))
        {
            // Verify mana is available
            if (manaUsed == null)
                return GameActionResult.Fail($"Powered effect requires {card.ManaType} mana");
            
            // Check if the mana color matches
            var requiredColor = ParseManaColor(card.ManaType);
            if (manaUsed != requiredColor && manaUsed != ManaColor.Gold)
                return GameActionResult.Fail($"Wrong mana color - need {card.ManaType}");
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

            switch (effectType)
            {
                case "move":
                    player.MovementRemaining += value;
                    effectDescriptions.Add($"+{value} Move");
                    break;
                    
                case "attack":
                    player.AttackPool += value;
                    // Check for element attributes
                    if (effect.Attributes?.Contains("Fire") == true)
                        player.AttackElements.Add("Fire");
                    if (effect.Attributes?.Contains("Ice") == true)
                        player.AttackElements.Add("Ice");
                    if (effect.Attributes?.Contains("ColdFire") == true)
                        player.AttackElements.Add("ColdFire");
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
                    player.BlockPool += value;
                    effectDescriptions.Add($"+{value} Block");
                    break;
                    
                case "influence":
                    player.InfluencePool += value;
                    effectDescriptions.Add($"+{value} Influence");
                    break;
                    
                case "heal":
                    player.HealPool += value;
                    effectDescriptions.Add($"+{value} Heal");
                    break;
                    
                case "draw":
                    // Draw cards
                    for (int i = 0; i < value && player.DeedDeck.Count > 0; i++)
                    {
                        var drawnCard = player.DeedDeck[0];
                        player.DeedDeck.RemoveAt(0);
                        player.Hand.Add(drawnCard);
                    }
                    effectDescriptions.Add($"Draw {value} card(s)");
                    break;
                    
                default:
                    if (!string.IsNullOrEmpty(effect.Description))
                        effectDescriptions.Add(effect.Description);
                    break;
            }
        }

        var description = string.Join(", ", effectDescriptions);
        AddLogEntry("PlayCard", $"Played {card.Name}{(powered ? " (powered)" : "")}: {description}");
        return GameActionResult.Ok($"Played {card.Name}: {description}");
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

    public GameActionResult UseCardSideways(string cardId)
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

        // Remove card from hand, add to discard
        player.Hand.Remove(cardId);
        player.DiscardPile.Add(cardId);

        // Sideways gives +1 of the card's primary type
        var cardType = card.Type?.ToLower() ?? "";
        var effectType = "";
        
        switch (cardType)
        {
            case "move":
                player.MovementRemaining += 1;
                effectType = "Move";
                break;
            case "attack":
                player.AttackPool += 1;
                effectType = "Attack";
                break;
            case "block":
                player.BlockPool += 1;
                effectType = "Block";
                break;
            case "influence":
                player.InfluencePool += 1;
                effectType = "Influence";
                break;
            case "heal":
                player.HealPool += 1;
                effectType = "Heal";
                break;
            default:
                // For special cards, default to +1 Move
                player.MovementRemaining += 1;
                effectType = "Move";
                break;
        }

        AddLogEntry("Sideways", $"Used {card.Name} sideways for +1 {effectType}");
        return GameActionResult.Ok($"Used {card.Name} sideways for +1 {effectType}");
    }

    public GameActionResult EndTurn()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        // Reset all player pools and state for next turn
        ResetPlayerTurnState(player);

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
        player.AttackPool = 0;
        player.BlockPool = 0;
        player.InfluencePool = 0;
        player.HealPool = 0;
        player.RangedAttack = 0;
        player.SiegeAttack = 0;
        player.AttackElements.Clear();
        player.HasRested = false;
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
        if (dieIndex < 0 || dieIndex >= _state.ManaPool.Count)
            return GameActionResult.Fail("Invalid mana die index");

        var color = _state.ManaPool[dieIndex];
        _state.ManaPool.RemoveAt(dieIndex);

        AddLogEntry("UseMana", $"Used {color} mana from pool");
        return GameActionResult.Ok($"Used {color} mana");
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

    public GameActionResult RerollManaPool()
    {
        RollManaPool();
        AddLogEntry("RerollMana", "Rerolled mana pool");
        return GameActionResult.Ok("Mana pool rerolled");
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
            
            // Apply tactic effects (draw cards, etc.)
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
            player.DeedDeck.AddRange(player.DiscardPile);
            player.DiscardPile.Clear();
            ShuffleList(player.DeedDeck);
        }

        // Reroll mana pool
        RollManaPool();

        AddLogEntry("RoundEnd", $"Round {_state.Round} begins - {(_state.IsDay ? "Day" : "Night")}");
    }

    private void RollManaPool()
    {
        var colors = new[] { ManaColor.Red, ManaColor.Blue, ManaColor.Green, ManaColor.White };
        var diceCount = _state.Players.Count + 2; // Base dice count

        _state.ManaPool.Clear();
        for (int i = 0; i < diceCount; i++)
        {
            // Gold has 1/6 chance, each color has equal remaining chance
            if (_random.Next(6) == 0)
                _state.ManaPool.Add(ManaColor.Gold);
            else
                _state.ManaPool.Add(colors[_random.Next(colors.Length)]);
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
                var combatEnemy = new CombatEnemy
                {
                    EnemyId = enemyId,
                    Name = enemyDef.Name,
                    Armor = enemyDef.Armor?.Value ?? 3,
                    Attack = enemyDef.Attack?.Value ?? 3,
                    AttackType = enemyDef.Attack?.Attributes?.FirstOrDefault() ?? "Physical",
                    Resistances = enemyDef.Armor?.Resistances ?? new List<string>(),
                    Abilities = enemyDef.Abilities ?? new List<string>(),
                    Fame = enemyDef.Fame
                };

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
            }
        }

        // If there are swift enemies, start with swift attack phase
        combat.Phase = hasSwiftEnemies ? CombatPhase.SwiftAttack : CombatPhase.RangedAttack;

        _state.Combat = combat;
        _state.Phase = GamePhase.Combat;

        var phaseMsg = hasSwiftEnemies ? "Swift enemies attack first!" : "Ranged attack phase";
        AddLogEntry("Combat", $"Combat initiated with {combat.Enemies.Count} enemies. {phaseMsg}");
        return GameActionResult.Ok($"Combat started with {combat.Enemies.Count} enemies. {phaseMsg}");
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
        
        // Check for Summon ability - summoned enemies also give fame when parent is killed
        if (enemy.CanSummon)
        {
            AddLogEntry("Combat", $"Summoner {enemy.Name} defeated - summoned creatures dispersed");
        }
        
        AddLogEntry("Combat", $"Defeated {enemy.Name} with {attackType}! +{enemy.Fame} fame");
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
        foreach (var enemy in _state.Combat.Enemies.Where(e => !e.IsDefeated))
        {
            totalDamage += enemy.Attack;
            if (enemy.IsBrutal)
            {
                totalDamage += enemy.Attack; // Double for Brutal
            }
        }

        for (int i = 0; i < totalDamage; i++)
        {
            player.Hand.Add("wound");
        }

        _state.Combat = null;
        _state.Phase = GamePhase.Movement;

        AddLogEntry("Combat", $"Fled combat! Took {totalDamage} wounds.");
        return GameActionResult.Ok($"Fled combat - took {totalDamage} wounds");
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

        switch (interactionType.ToLower())
        {
            case "heal":
                return HealAtSite(1);
            case "plunder":
                return Plunder();
            case "empower":
                return Empower();
            case "harvest":
                return Harvest(hexState.SiteType);
            case "training":
                return Training();
            default:
                return GameActionResult.Fail($"Unknown interaction type: {interactionType}");
        }
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

        // Get unit definition
        var unitDef = _definitions.GetUnitsAsync().Result.FirstOrDefault(u => u.Id == unitId);
        if (unitDef == null)
            return GameActionResult.Fail("Invalid unit");

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

    private GameActionResult Harvest(string siteType)
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

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

    private GameActionResult Training()
    {
        var player = GetCurrentPlayer();
        if (player == null)
            return GameActionResult.Fail("No current player");

        if (player.InfluencePool < 6)
            return GameActionResult.Fail("Not enough influence (need 6)");

        // In a full implementation, this would let the player choose an advanced action
        // For now, just deduct the cost and add a placeholder
        player.InfluencePool -= 6;

        AddLogEntry("Training", "Trained at monastery - gained Advanced Action");
        return GameActionResult.Ok("Trained! Choose an Advanced Action.");
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

        var abilities = unitDef.ParsedAbilities;
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

            var abilities = unitDef.ParsedAbilities;
            var option = new UnitCombatOption
            {
                UnitIndex = i,
                UnitId = unit.UnitId,
                UnitName = unit.Name,
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
        }

        return false;
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
