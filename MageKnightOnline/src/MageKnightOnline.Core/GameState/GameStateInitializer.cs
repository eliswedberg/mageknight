using MageKnightOnline.Core.Definitions;
using MageKnightOnline.Core.Entities;
using MageKnightOnline.Core.Services;

namespace MageKnightOnline.Core.GameState;

/// <summary>
/// Initializes a new game state based on scenario and player configuration.
/// </summary>
public class GameStateInitializer
{
    private readonly IGameDefinitionService _definitions;
    private readonly Random _random = new();

    public GameStateInitializer(IGameDefinitionService definitions)
    {
        _definitions = definitions;
    }

    /// <summary>
    /// Creates initial game state for a new game.
    /// </summary>
    public async Task<GameStateModel> InitializeAsync(Game game, ScenarioDefinition scenario)
    {
        var state = new GameStateModel
        {
            Round = 1,
            IsDay = true,
            Phase = GamePhase.TacticsSelection, // Start with tactics selection
            CurrentPlayerIndex = 0
        };

        // Initialize available tactics for first round (day tactics)
        var tactics = await _definitions.GetDayTacticsAsync();
        state.AvailableTactics = tactics.Select(t => t.Id).ToList();

        // Initialize players
        foreach (var player in game.Players.OrderBy(p => p.TurnOrder))
        {
            var playerState = await CreatePlayerStateAsync(player);
            state.Players.Add(playerState);
            state.TurnOrder.Add(state.Players.Count - 1);
        }

        // Initialize decks
        state.Decks = await CreateDeckStateAsync(scenario);

        // Initialize map with starting tile
        state.Map = await CreateInitialMapAsync(scenario, state.Players.Count);

        // Roll initial mana pool
        state.ManaPool = RollManaPool(state.Players.Count);

        // Add game start log
        state.GameLog.Add(new GameLogEntry
        {
            Action = "GameStarted",
            Details = $"Game started with {state.Players.Count} players"
        });

        return state;
    }

    private async Task<PlayerState> CreatePlayerStateAsync(GamePlayer player)
    {
        var heroId = player.HeroId ?? "tovak";
        var basicActions = await _definitions.GetBasicActionsAsync();

        // Get hero-specific starting cards
        var startingCards = basicActions
            .Where(c => c.Heroes == null || c.Heroes.Contains(heroId) || c.Heroes.Count == 0)
            .SelectMany(c => Enumerable.Repeat(c.Id, c.CountPerHero ?? 1))
            .ToList();

        // Shuffle the deed deck
        Shuffle(startingCards);

        // Draw initial hand (5 cards)
        var hand = startingCards.Take(5).ToList();
        var deck = startingCards.Skip(5).ToList();

        return new PlayerState
        {
            UserId = player.UserId,
            HeroId = heroId,
            Position = new HexPosition { Q = 0, R = 0 }, // Starting position
            Fame = 0,
            Reputation = 0,
            Level = 1,
            Armor = 2,
            HandLimit = 5,
            Hand = hand,
            Deck = deck, // Main deck for drawing
            DeedDeck = deck.ToList(), // Copy for reference
            DiscardPile = new List<string>(),
            Units = new List<UnitState>(),
            Skills = new List<string>(),
            Crystals = new CrystalInventory(),
            ManaTokens = new ManaTokenInventory(),
            CommandTokens = 1, // Start with 1 unit slot
            MovementRemaining = 0,
            HasRested = false
        };
    }

    private async Task<DeckState> CreateDeckStateAsync(ScenarioDefinition scenario)
    {
        var advancedActions = (await _definitions.GetAdvancedActionsAsync()).Select(c => c.Id).ToList();
        var spells = (await _definitions.GetSpellsAsync()).Select(c => c.Id).ToList();
        var artifacts = (await _definitions.GetArtifactsAsync()).Select(c => c.Id).ToList();
        var units = await _definitions.GetUnitsAsync();
        var enemies = await _definitions.GetEnemiesAsync();

        Shuffle(advancedActions);
        Shuffle(spells);
        Shuffle(artifacts);

        var regularUnits = units.Where(u => u.Rank == "Regular").Select(u => u.Id).ToList();
        var eliteUnits = units.Where(u => u.Rank == "Elite").Select(u => u.Id).ToList();
        Shuffle(regularUnits);
        Shuffle(eliteUnits);

        // Group enemies by type
        var enemyDecks = enemies
            .GroupBy(e => e.Type)
            .ToDictionary(
                g => g.Key,
                g => {
                    var deck = g.Select(e => e.Id).ToList();
                    Shuffle(deck);
                    return deck;
                }
            );

        return new DeckState
        {
            AdvancedActions = advancedActions,
            Spells = spells,
            Artifacts = artifacts,
            RegularUnits = regularUnits,
            EliteUnits = eliteUnits,
            CountrysideTiles = CreateTileDeck("countryside", scenario.TilesDeck.Countryside),
            CoreTiles = CreateTileDeck("core", scenario.TilesDeck.Core),
            CityTiles = CreateTileDeck("city", scenario.TilesDeck.Cities),
            EnemyDecks = enemyDecks
        };
    }

    private List<string> CreateTileDeck(string type, int count)
    {
        // Generate tile IDs based on type
        var tiles = Enumerable.Range(1, count).Select(i => $"{type}_{i}").ToList();
        Shuffle(tiles);
        return tiles;
    }

    private async Task<MapState> CreateInitialMapAsync(ScenarioDefinition scenario, int playerCount)
    {
        var map = new MapState();

        // Starting tile layout (The Portal - tile_01_start)
        // Center hex (0,0) is the Portal, surrounded by 6 hexes
        var startingTileHexes = new (int q, int r, string terrain, string? site)[]
        {
            (0, 0, "Plains", "Portal"),      // Center - Portal (starting position)
            (1, 0, "Plains", null),          // East
            (1, -1, "Forest", null),         // Northeast
            (0, -1, "Water", null),          // Northwest
            (-1, 0, "Water", null),          // West
            (-1, 1, "Water", null),          // Southwest
            (0, 1, "Plains", null),          // Southeast
        };

        // Add starting tile
        var startingTile = new MapTileState
        {
            TileId = "tile_01_start",
            Position = new HexPosition { Q = 0, R = 0 },
            Rotation = 0,
            IsRevealed = true
        };
        map.Tiles.Add(startingTile);

        // Add hex data for starting tile
        foreach (var (q, r, terrain, site) in startingTileHexes)
        {
            var key = $"{q},{r}";
            map.RevealedHexes.Add(key);
            map.HexData[key] = new HexState
            {
                Terrain = terrain,
                SiteType = site,
                Enemies = new List<string>(),
                IsConquered = site == "Portal" // Portal is always "conquered"
            };
        }

        // Add a few more tiles around the starting area for exploration
        // These are face-down (unrevealed) but we know their edge positions
        var adjacentTilePositions = new (int q, int r)[]
        {
            (2, -1),   // Northeast of starting tile
            (2, 0),    // East
            (1, 1),    // Southeast
            (-1, 2),   // South
            (-2, 1),   // Southwest
            (-2, 0),   // West
            (-1, -1),  // Northwest
        };

        // Add placeholder hexes at tile edges (unrevealed)
        foreach (var (tq, tr) in adjacentTilePositions)
        {
            var key = $"{tq},{tr}";
            if (!map.HexData.ContainsKey(key))
            {
                // Don't add to RevealedHexes - these are face-down
                map.HexData[key] = new HexState
                {
                    Terrain = "Unknown",
                    SiteType = null
                };
            }
        }

        return map;
    }

    private List<ManaColor> RollManaPool(int playerCount)
    {
        var diceCount = playerCount + 2; // Base rule: players + 2 dice
        var pool = new List<ManaColor>();
        var colors = new[] { ManaColor.Red, ManaColor.Blue, ManaColor.Green, ManaColor.White, ManaColor.Black, ManaColor.Gold };

        for (int i = 0; i < diceCount; i++)
        {
            // Each die has equal chance of each color
            pool.Add(colors[_random.Next(colors.Length)]);
        }

        return pool;
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
