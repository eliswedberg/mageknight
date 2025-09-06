using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class MageKnightGameService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MageKnightGameService> _logger;

    public MageKnightGameService(ApplicationDbContext context, ILogger<MageKnightGameService> logger)
    {
        _context = context;
        _logger = logger;
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

            // Create game board
            var gameBoard = new GameBoard
            {
                GameSessionId = gameSessionId,
                MapType = "Standard"
            };
            _context.GameBoards.Add(gameBoard);
            await _context.SaveChangesAsync();

            // Create board tiles
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
                AttackingPlayerId = playerId,
                DefendingSiteId = siteId,
                Type = CombatType.SiteAttack,
                Status = CombatStatus.Preparation,
                TurnNumber = await GetCurrentTurnNumberAsync(player.GameSessionId)
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
                .Include(s => s.GameBoard)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || site.IsExplored) return false;

            var player = await _context.GamePlayers
                .Include(gp => gp.GameSession)
                .FirstOrDefaultAsync(gp => gp.Id == playerId);

            if (player == null) return false;

            // Mark site as explored
            site.IsExplored = true;

            // Apply exploration rewards
            player.Fame += site.FameReward;
            player.Reputation += site.ReputationReward;
            player.Crystals += site.CrystalsReward;

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
    private Task CreateBoardTilesAsync(int gameBoardId)
    {
        var tiles = new List<BoardTile>();
        
        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 7; y++)
            {
                var tile = new BoardTile
                {
                    GameBoardId = gameBoardId,
                    X = x,
                    Y = y,
                    Type = TileType.Empty,
                    MovementCost = 1
                };
                tiles.Add(tile);
            }
        }
        
        _context.BoardTiles.AddRange(tiles);
        return Task.CompletedTask;
    }

    private Task CreateSitesAsync(int gameBoardId)
    {
        var sites = new List<Site>
        {
            new Site { GameBoardId = gameBoardId, X = 1, Y = 1, Type = SiteType.Ruins, Name = "Ancient Ruins", AttackCost = 2, FameReward = 1 },
            new Site { GameBoardId = gameBoardId, X = 5, Y = 1, Type = SiteType.Dungeon, Name = "Dark Dungeon", AttackCost = 3, FameReward = 2 },
            new Site { GameBoardId = gameBoardId, X = 1, Y = 5, Type = SiteType.Keep, Name = "Orc Keep", AttackCost = 4, FameReward = 3 },
            new Site { GameBoardId = gameBoardId, X = 5, Y = 5, Type = SiteType.MageTower, Name = "Mage Tower", AttackCost = 5, FameReward = 4 },
            new Site { GameBoardId = gameBoardId, X = 3, Y = 3, Type = SiteType.City, Name = "Capital City", AttackCost = 6, FameReward = 5 }
        };
        
        _context.Sites.AddRange(sites);
        return Task.CompletedTask;
    }

    private Task InitializePlayerPositionsAsync(int gameBoardId, List<GamePlayer> players)
    {
        var positions = new List<PlayerPosition>();
        
        for (int i = 0; i < players.Count; i++)
        {
            var position = new PlayerPosition
            {
                GameBoardId = gameBoardId,
                PlayerId = players[i].Id,
                X = 3, // Start in center
                Y = 3
            };
            positions.Add(position);
        }
        
        _context.PlayerPositions.AddRange(positions);
        return Task.CompletedTask;
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
    }

    private async Task ApplyCardEffectsAsync(GamePlayer player, MageKnightCard card, string? targetData)
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

    private async Task LogGameActionAsync(int gameSessionId, int playerId, ActionType actionType, string description)
    {
        var gameAction = new GameAction
        {
            GameSessionId = gameSessionId,
            PlayerId = playerId,
            Type = actionType,
            Description = description
        };
        _context.GameActions.Add(gameAction);
    }
}
