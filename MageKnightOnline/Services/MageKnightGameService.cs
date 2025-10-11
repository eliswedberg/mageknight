using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class MageKnightGameService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MageKnightGameService> _logger;
    private readonly MapTileService _mapTileService;
    private readonly ActionCardService _actionCardService;

    public MageKnightGameService(ApplicationDbContext context, ILogger<MageKnightGameService> logger, 
        MapTileService mapTileService, ActionCardService actionCardService)
    {
        _context = context;
        _logger = logger;
        _mapTileService = mapTileService;
        _actionCardService = actionCardService;
    }

    // Game Initialization
    public async Task<bool> InitializeGameAsync(int gameSessionId)
    {
        try
        {
            var gameSession = await _context.GameSessions
                .Include(gs => gs.Players)
                .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

            if (gameSession == null) return false;

            // Initialize the new map system
            var mapInitialized = await _mapTileService.InitializeMapAsync(gameSessionId, "standard", "A");
            if (!mapInitialized)
            {
                _logger.LogError("Failed to initialize map for game session {GameSessionId}", gameSessionId);
                return false;
            }

            // Create legacy game board for backward compatibility
            var gameBoard = new GameBoard
            {
                GameSessionId = gameSessionId,
                MapType = "Standard"
            };
            _context.GameBoards.Add(gameBoard);
            await _context.SaveChangesAsync();

            // Create board tiles (legacy system)
            await CreateBoardTilesAsync(gameBoard.Id);

            // Create sites
            await CreateSitesAsync(gameBoard.Id);

            // Initialize player positions
            await InitializePlayerPositionsAsync(gameBoard.Id, gameSession.Players.ToList());

            // Create game state
            var gameState = new GameState
            {
                GameSessionId = gameSessionId,
                CurrentPlayerId = gameSession.Players.OrderBy(p => p.PlayerNumber).First().Id,
                GamePhase = GamePhase.Early
            };
            _context.GameStates.Add(gameState);

            // Initialize player decks
            foreach (var player in gameSession.Players)
            {
                await InitializePlayerDeckAsync(player.Id);
                await DrawInitialHandAsync(player.Id);
                
                // Create standard action card deck
                await _actionCardService.CreateStandardDeckAsync(player.Id);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing game {GameSessionId}", gameSessionId);
            return false;
        }
    }

    // Turn Management
    public async Task<bool> StartNewTurnAsync(int gameSessionId, int playerId)
    {
        try
        {
            var gameState = await _context.GameStates
                .FirstOrDefaultAsync(gs => gs.GameSessionId == gameSessionId);

            if (gameState == null) return false;

            var player = await _context.GamePlayers
                .FirstOrDefaultAsync(gp => gp.Id == playerId);

            if (player == null) return false;

            // Create new turn
            var gameTurn = new GameTurn
            {
                GameSessionId = gameSessionId,
                TurnNumber = gameState.TurnNumber,
                CurrentPlayerId = playerId,
                Phase = TurnPhase.Preparation,
                ActionsRemaining = 0,
                ManaAvailable = 0,
                CrystalsAvailable = 0
            };
            _context.GameTurns.Add(gameTurn);

            // Update game state
            gameState.CurrentPlayerId = playerId;
            gameState.TurnNumber++;
            gameState.CurrentPhase = TurnPhase.Preparation;
            gameState.LastUpdated = DateTime.UtcNow;

            // Draw cards for new turn
            await DrawCardsForTurnAsync(playerId, 5);

            // Reset mana and crystals
            player.Mana = 0;
            player.Crystals = 0;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new turn for player {PlayerId}", playerId);
            return false;
        }
    }

    // Card Management
    public async Task<bool> PlayCardAsync(int playerId, int cardId, string? targetData = null)
    {
        try
        {
            var playerHand = await _context.PlayerHands
                .Include(ph => ph.Card)
                .FirstOrDefaultAsync(ph => ph.GamePlayerId == playerId && ph.CardId == cardId && !ph.IsPlayed);

            if (playerHand == null) return false;

            var player = await _context.GamePlayers.FindAsync(playerId);
            if (player == null) return false;

            // Check if player has enough mana/crystals
            if (playerHand.Card.Cost > player.Mana + player.Crystals) return false;

            // Mark card as played
            playerHand.IsPlayed = true;

            // Apply card effects
            await ApplyCardEffectsAsync(player, playerHand.Card, targetData);

            // Move card to discard pile
            var playerDiscard = new PlayerDiscard
            {
                GamePlayerId = playerId,
                CardId = cardId,
                DiscardPosition = await GetNextDiscardPositionAsync(playerId)
            };
            _context.PlayerDiscards.Add(playerDiscard);

            // Log the action
            await LogGameActionAsync(player.GameSessionId, playerId, ActionType.CardPlayed, 
                $"Played {playerHand.Card.Name}");

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing card {CardId} for player {PlayerId}", cardId, playerId);
            return false;
        }
    }

    // Movement
    public async Task<bool> MovePlayerAsync(int playerId, int targetX, int targetY)
    {
        try
        {
            var player = await _context.GamePlayers
                .Include(gp => gp.GameSession)
                .FirstOrDefaultAsync(gp => gp.Id == playerId);

            if (player == null) return false;

            var gameBoard = await _context.GameBoards
                .Include(gb => gb.PlayerPositions)
                .FirstOrDefaultAsync(gb => gb.GameSessionId == player.GameSessionId);

            if (gameBoard == null) return false;

            var currentPosition = gameBoard.PlayerPositions
                .FirstOrDefault(pp => pp.PlayerId == playerId);

            if (currentPosition == null) return false;

            // Calculate movement cost
            var movementCost = CalculateMovementCost(currentPosition.X, currentPosition.Y, targetX, targetY);
            
            if (movementCost > player.Mana) return false;

            // Update position
            currentPosition.X = targetX;
            currentPosition.Y = targetY;
            currentPosition.MovedAt = DateTime.UtcNow;
            currentPosition.MovementPointsUsed += movementCost;

            // Deduct mana
            player.Mana -= movementCost;

            // Log the action
            await LogGameActionAsync(player.GameSessionId, playerId, ActionType.Movement, 
                $"Moved to ({targetX}, {targetY})");

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving player {PlayerId}", playerId);
            return false;
        }
    }

    // Combat
    public async Task<bool> StartCombatAsync(int playerId, int siteId)
    {
        try
        {
            var player = await _context.GamePlayers
                .Include(gp => gp.GameSession)
                .FirstOrDefaultAsync(gp => gp.Id == playerId);

            if (player == null) return false;

            var site = await _context.Sites
                .Include(s => s.Enemies)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || site.IsConquered) return false;

            // Create combat
            var combat = new Combat
            {
                GameSessionId = player.GameSessionId,
                SiteId = siteId,
                Type = CombatType.SiteConquest,
                Status = CombatStatus.Preparing,
                CurrentTurn = await GetCurrentTurnNumberAsync(player.GameSessionId)
            };
            _context.Combats.Add(combat);

            // Add combat participants
            var attacker = new CombatParticipant
            {
                CombatId = combat.Id,
                PlayerId = playerId,
                AttackValue = CalculatePlayerAttack(playerId),
                BlockValue = CalculatePlayerBlock(playerId),
                Health = player.Level + 2,
                CurrentHealth = player.Level + 2,
                Initiative = 1
            };
            _context.CombatParticipants.Add(attacker);

            foreach (var enemy in site.Enemies.Where(e => !e.IsDefeated))
            {
                var enemyParticipant = new CombatParticipant
                {
                    CombatId = combat.Id,
                    EnemyId = enemy.Id,
                    AttackValue = enemy.Attack,
                    BlockValue = enemy.Block,
                    Health = enemy.Health,
                    CurrentHealth = enemy.CurrentHealth,
                    Initiative = 2
                };
                _context.CombatParticipants.Add(enemyParticipant);
            }

            // Log the action
            await LogGameActionAsync(player.GameSessionId, playerId, ActionType.CombatStarted, 
                $"Started combat at {site.Name}");

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting combat for player {PlayerId} at site {SiteId}", playerId, siteId);
            return false;
        }
    }

    // Exploration
    public async Task<bool> ExploreSiteAsync(int playerId, int siteId)
    {
        try
        {
            var site = await _context.Sites
                .Include(s => s.GameSession)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || site.IsRevealed) return false;

            var player = await _context.GamePlayers
                .Include(gp => gp.GameSession)
                .FirstOrDefaultAsync(gp => gp.Id == playerId);

            if (player == null) return false;

            // Mark site as revealed
            site.IsRevealed = true;

            // Apply exploration rewards (these would come from the Rewards JSON field)
            // For now, we'll use placeholder values
            player.Fame += 1; // TODO: Parse from site.Rewards JSON
            player.Reputation += 0; // TODO: Parse from site.Rewards JSON
            player.Crystals += 0; // TODO: Parse from site.Rewards JSON

            // Log the action
            await LogGameActionAsync(player.GameSessionId, playerId, ActionType.SiteExplored, 
                $"Explored {site.Name}");

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exploring site {SiteId} for player {PlayerId}", siteId, playerId);
            return false;
        }
    }

    // Helper Methods
    private async Task CreateBoardTilesAsync(int gameBoardId)
    {
        var tiles = new List<BoardTile>();
        
        // According to Mage Knight rules, start with only the starting tile revealed
        // Create the starting tile (center position)
        var startingTile = new BoardTile
        {
            GameBoardId = gameBoardId,
            X = 0, // Use 0,0 as center in hex coordinate system
            Y = 0,
            Type = TileType.Starting,
            IsExplored = true,
            IsRevealed = true,
            Terrain = "Mixed", // Starting tile has multiple terrains
            MovementCost = 1,
            TileImageName = "MK_map_tiles_01-A" // Starting tile A side
        };
        tiles.Add(startingTile);
        
        // Only create the starting tile for now
        // Additional tiles will be placed through exploration
        
        _context.BoardTiles.AddRange(tiles);
        await _context.SaveChangesAsync();
    }

    private async Task CreateSitesAsync(int gameBoardId)
    {
        // Sites are now created by the MapTileService when tiles are revealed
        // This method is kept for backward compatibility but doesn't create sites anymore
        // Sites will be created automatically when tiles are explored
    }

    public async Task<GameBoard?> GetGameBoardAsync(int gameSessionId)
    {
        var gameBoard = await _context.GameBoards
            .Include(gb => gb.Tiles)
            .Include(gb => gb.PlayerPositions)
            .FirstOrDefaultAsync(gb => gb.GameSessionId == gameSessionId);

        // If no legacy game board exists, create one with data from the new system
        if (gameBoard == null)
        {
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);
            
            if (mapGraph != null)
            {
                // Create a legacy game board from the new system data
                gameBoard = new GameBoard
                {
                    GameSessionId = gameSessionId,
                    MapType = "Standard"
                };
                _context.GameBoards.Add(gameBoard);
                await _context.SaveChangesAsync();

                // Convert new system tiles to legacy tiles
                foreach (var newTile in mapGraph.Tiles.Where(t => t.IsPlaced))
                {
                    var legacyTile = new BoardTile
                    {
                        GameBoardId = gameBoard.Id,
                        X = newTile.CenterQ,
                        Y = newTile.CenterR,
                        Type = ConvertTileType(newTile.TileType),
                        IsRevealed = newTile.IsRevealed,
                        IsExplored = newTile.IsRevealed,
                        TileImageName = newTile.ImageName
                    };
                    _context.BoardTiles.Add(legacyTile);
                }
                await _context.SaveChangesAsync();
            }
        }

        return gameBoard;
    }

    private TileType ConvertTileType(MapTileType newType)
    {
        return newType switch
        {
            MapTileType.Starting => TileType.Starting,
            MapTileType.Countryside => TileType.Countryside,
            MapTileType.CoreNonCity => TileType.Core,
            MapTileType.CoreCity => TileType.City,
            _ => TileType.Starting
        };
    }

    public async Task<bool> ExploreTileAsync(int gameSessionId, int x, int y)
    {
        try
        {
            // Use the new map tile service for exploration
            // Convert coordinates from the UI to axial coordinates
            var (q, r) = ConvertToAxialCoordinates(x, y);
            
            var newTile = await _mapTileService.ExploreTileAsync(gameSessionId, 1, q, r); // TODO: Get actual player ID
            if (newTile == null)
            {
                _logger.LogInformation("Failed to explore tile at ({X}, {Y}) for game session {GameSessionId}", x, y, gameSessionId);
                return false;
            }

            // Also update the legacy system for backward compatibility
            var gameBoard = await GetGameBoardAsync(gameSessionId);
            if (gameBoard != null)
            {
                var legacyTile = await DrawRandomTileAsync(gameBoard.Id, x, y);
                if (legacyTile != null)
                {
                    _context.BoardTiles.Add(legacyTile);
                    await _context.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Explored new tile at ({X}, {Y}) for game session {GameSessionId}", x, y, gameSessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exploring tile at ({X}, {Y}) for game session {GameSessionId}", x, y, gameSessionId);
            return false;
        }
    }

    // Convert UI coordinates to axial coordinates for the new map system
    private (int q, int r) ConvertToAxialCoordinates(int x, int y)
    {
        // This is a simple conversion - in a real implementation, this would be more sophisticated
        // For now, we'll use the same coordinates as the UI
        return (x, y);
    }

    private async Task<BoardTile?> DrawRandomTileAsync(int gameBoardId, int x, int y)
    {
        // Get the game board to find adjacent tiles for edge matching
        var gameBoard = await _context.GameBoards
            .Include(gb => gb.Tiles)
            .FirstOrDefaultAsync(gb => gb.Id == gameBoardId);
        
        if (gameBoard == null) return null;
        
        // Find adjacent tiles to match edges
        var adjacentTiles = gameBoard.Tiles.Where(t => t.IsRevealed && IsAdjacentToPosition(t.X, t.Y, x, y)).ToList();
        
        // For now, create a simple random tile
        // In a full implementation, this would draw from the actual tile deck and match edges
        var random = new Random();
        var tileTypes = new[] { "01-1", "01-2", "01-3", "01-4", "01-5", "01-6", "01-7", "01-8", "01-9", "01-10", "01-11" };
        var terrainTypes = new[] { "Forest", "Mountain", "Desert", "Plains", "Hills" };
        
        var selectedTile = tileTypes[random.Next(tileTypes.Length)];
        var selectedTerrain = terrainTypes[random.Next(terrainTypes.Length)];
        
        // TODO: Implement proper edge matching logic
        // This would check the adjacent tiles' edge data and select a tile that matches
        
        return new BoardTile
        {
            GameBoardId = gameBoardId,
            X = x,
            Y = y,
            Type = TileType.Countryside,
            IsExplored = false,
            IsRevealed = true,
            Terrain = selectedTerrain,
            MovementCost = selectedTerrain == "Mountain" ? 3 : selectedTerrain == "Forest" ? 2 : 1,
            TileImageName = $"MK_map_tiles_{selectedTile}"
        };
    }
    
    private bool IsAdjacentToPosition(int tileX, int tileY, int targetX, int targetY)
    {
        // Check if tile is adjacent to target position in hex grid
        var adjacentPositions = GetHexAdjacentPositions(tileX, tileY);
        return adjacentPositions.Contains((targetX, targetY));
    }

    public async Task<bool> RemoveAllTilesExceptOneAsync(int gameSessionId)
    {
        try
        {
            // Reset the new map system to only have the starting tile
            var mapGraph = await _context.MapGraphs
                .Include(mg => mg.Tiles)
                .FirstOrDefaultAsync(mg => mg.GameSessionId == gameSessionId);
            
            if (mapGraph != null)
            {
                // Remove all tiles except the starting tile
                var tilesToRemove = mapGraph.Tiles.Where(t => t.TileType != MapTileType.Starting).ToList();
                foreach (var tile in tilesToRemove)
                {
                    // Remove associated hex spaces
                    var hexSpaces = await _context.HexSpaces.Where(hs => hs.MapTileId == tile.Id).ToListAsync();
                    _context.HexSpaces.RemoveRange(hexSpaces);
                    
                    // Remove the tile
                    _context.MapTileNews.Remove(tile);
                }
                
                // Reset map state
                mapGraph.IsInitialized = true;
                mapGraph.CurrentPhase = ExplorationPhase.Countryside;
                mapGraph.CountrysideTilesRemaining = 10; // Reset deck counts
                mapGraph.CoreNonCityTilesRemaining = 8;
                mapGraph.CoreCityTilesRemaining = 4;
                
                await _context.SaveChangesAsync();
            }

            // Also clean up legacy system
            var gameBoard = await GetGameBoardAsync(gameSessionId);
            if (gameBoard != null)
            {
                // Get all tiles except the starting tile (0,0)
                var tilesToRemove = gameBoard.Tiles.Where(t => !(t.X == 0 && t.Y == 0)).ToList();
                
                if (tilesToRemove.Any())
                {
                    _context.BoardTiles.RemoveRange(tilesToRemove);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Removed {Count} legacy tiles from game session {GameSessionId}, keeping only starting tile", 
                        tilesToRemove.Count, gameSessionId);
                }
            }
            
            _logger.LogInformation("Reset map to starting tile only for game session {GameSessionId}", gameSessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tiles for game session {GameSessionId}", gameSessionId);
            return false;
        }
    }

    private async Task InitializePlayerPositionsAsync(int gameBoardId, List<GamePlayer> players)
    {
        var positions = new List<PlayerPosition>();
        
        for (int i = 0; i < players.Count; i++)
        {
            var position = new PlayerPosition
            {
                GameBoardId = gameBoardId,
                PlayerId = players[i].Id,
                X = 0, // Start in center (starting tile)
                Y = 0
            };
            positions.Add(position);
        }
        
        _context.PlayerPositions.AddRange(positions);
        await _context.SaveChangesAsync();
    }

    private async Task InitializePlayerDeckAsync(int playerId)
    {
        // Get basic cards for starting deck
        var basicCards = await _context.MageKnightCards
            .Where(c => c.Type == CardType.BasicMove || c.Type == CardType.BasicAttack || 
                       c.Type == CardType.BasicBlock || c.Type == CardType.BasicInfluence)
            .Take(16)
            .ToListAsync();

        var deck = new List<PlayerDeck>();
        
        for (int i = 0; i < basicCards.Count; i++)
        {
            deck.Add(new PlayerDeck
            {
                GamePlayerId = playerId,
                CardId = basicCards[i].Id,
                DeckPosition = i
            });
        }
        
        _context.PlayerDecks.AddRange(deck);
        await _context.SaveChangesAsync();
    }

    private async Task DrawInitialHandAsync(int playerId)
    {
        var deck = await _context.PlayerDecks
            .Where(pd => pd.GamePlayerId == playerId && !pd.IsDrawn)
            .OrderBy(pd => pd.DeckPosition)
            .Take(5)
            .ToListAsync();

        var hand = new List<PlayerHand>();
        
        for (int i = 0; i < deck.Count; i++)
        {
            deck[i].IsDrawn = true;
            hand.Add(new PlayerHand
            {
                GamePlayerId = playerId,
                CardId = deck[i].CardId,
                HandPosition = i
            });
        }
        
        _context.PlayerHands.AddRange(hand);
        await _context.SaveChangesAsync();
    }

    private async Task DrawCardsForTurnAsync(int playerId, int count)
    {
        var deck = await _context.PlayerDecks
            .Where(pd => pd.GamePlayerId == playerId && !pd.IsDrawn)
            .OrderBy(pd => pd.DeckPosition)
            .Take(count)
            .ToListAsync();

        var hand = new List<PlayerHand>();
        
        for (int i = 0; i < deck.Count; i++)
        {
            deck[i].IsDrawn = true;
            hand.Add(new PlayerHand
            {
                GamePlayerId = playerId,
                CardId = deck[i].CardId,
                HandPosition = await GetNextHandPositionAsync(playerId)
            });
        }
        
        _context.PlayerHands.AddRange(hand);
        await _context.SaveChangesAsync();
    }

    private Task ApplyCardEffectsAsync(GamePlayer player, MageKnightCard card, string? targetData)
    {
        // Apply card effects based on card type
        player.Mana += card.Move;
        player.Crystals += card.Influence;
        player.Fame += card.Fame;
        player.Reputation += card.Reputation;
        
        // Additional effects based on card type
        if (card.IsSpell)
        {
            // Handle spell effects
        }
        else if (card.IsArtifact)
        {
            // Handle artifact effects
        }
        else if (card.IsUnit)
        {
            // Handle unit effects
        }
        
        return Task.CompletedTask;
    }

    private int CalculateMovementCost(int fromX, int fromY, int toX, int toY)
    {
        return Math.Abs(toX - fromX) + Math.Abs(toY - fromY);
    }

    private async Task<int> GetCurrentTurnNumberAsync(int gameSessionId)
    {
        var gameState = await _context.GameStates
            .FirstOrDefaultAsync(gs => gs.GameSessionId == gameSessionId);
        return gameState?.TurnNumber ?? 1;
    }

    private int CalculatePlayerAttack(int playerId)
    {
        // Calculate total attack from cards, artifacts, units, etc.
        return 1; // Placeholder
    }

    private int CalculatePlayerBlock(int playerId)
    {
        // Calculate total block from cards, artifacts, units, etc.
        return 1; // Placeholder
    }

    private async Task<int> GetNextHandPositionAsync(int playerId)
    {
        var maxPosition = await _context.PlayerHands
            .Where(ph => ph.GamePlayerId == playerId)
            .MaxAsync(ph => (int?)ph.HandPosition) ?? -1;
        return maxPosition + 1;
    }

    private async Task<int> GetNextDiscardPositionAsync(int playerId)
    {
        var maxPosition = await _context.PlayerDiscards
            .Where(pd => pd.GamePlayerId == playerId)
            .MaxAsync(pd => (int?)pd.DiscardPosition) ?? -1;
        return maxPosition + 1;
    }

    private Task LogGameActionAsync(int gameSessionId, int playerId, ActionType actionType, string description)
    {
        var gameAction = new GameAction
        {
            GameSessionId = gameSessionId,
            PlayerId = playerId,
            Type = actionType,
            Description = description
        };
        _context.GameActions.Add(gameAction);
        
        return Task.CompletedTask;
    }

    private bool IsAdjacentToRevealedTile(GameBoard gameBoard, int x, int y)
    {
        // Check if the position is adjacent to any revealed tile
        var revealedTiles = gameBoard.Tiles.Where(t => t.IsRevealed).ToList();
        
        foreach (var tile in revealedTiles)
        {
            // Check hex adjacency (6 directions)
            var adjacentPositions = GetHexAdjacentPositions(tile.X, tile.Y);
            if (adjacentPositions.Contains((x, y)))
            {
                return true;
            }
        }
        
        return false;
    }

    private List<(int X, int Y)> GetHexAdjacentPositions(int x, int y)
    {
        // Hex grid adjacent positions (6 directions)
        return new List<(int, int)>
        {
            (x + 1, y),     // East
            (x - 1, y),     // West
            (x, y + 1),     // North East
            (x, y - 1),     // South West
            (x + 1, y - 1), // North West
            (x - 1, y + 1)  // South East
        };
    }
}
