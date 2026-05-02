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
            CurrentPlayerIndex = 0,
            ScenarioId = scenario.Id,
            TotalCities = scenario.CityLevels.Count
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
        state.Offers = CreateInitialOffers(state.Decks);

        // Initialize map with starting tile
        state.Map = await CreateInitialMapAsync(scenario, state.Players.Count);

        // Roll initial mana pool
        state.ManaSource = RollManaSource(state.Players.Count, state.IsDay);
        state.ManaPool = state.ManaSource.Select(d => d.Color).ToList();

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
        var heroId = player.HeroId ?? "hero_tovak";
        
        // Extract hero name from heroId (e.g., "hero_tovak" -> "Tovak")
        // First try to get hero definition to get the proper name
        var hero = await _definitions.GetHeroAsync(heroId);
        var heroName = hero?.Name ?? ExtractHeroName(heroId);
        
        var basicActions = await _definitions.GetBasicActionsAsync();

        // Get hero-specific starting cards
        // A card is available if:
        // 1. Heroes list is null/empty (available to all), OR
        // 2. Hero name is in the Heroes list (case-insensitive match)
        var startingCards = basicActions
            .Where(c => 
            {
                if (c.Heroes == null || c.Heroes.Count == 0)
                    return true; // Available to all heroes
                
                // Case-insensitive match
                return c.Heroes.Any(h => 
                    string.Equals(h, heroName, StringComparison.OrdinalIgnoreCase));
            })
            .SelectMany(c => Enumerable.Repeat(c.Id, c.CountPerHero ?? 1))
            .ToList();

        // Validate we got cards
        if (startingCards.Count == 0)
        {
            var availableHeroes = basicActions
                .SelectMany(c => c.Heroes ?? new List<string>())
                .Distinct()
                .ToList();
            throw new InvalidOperationException(
                $"No starting cards found for hero '{heroId}' (name: '{heroName}'). " +
                $"Available heroes in cards: {string.Join(", ", availableHeroes)}. " +
                $"Total basic actions: {basicActions.Count}");
        }

        // Shuffle the deed deck
        Shuffle(startingCards);

        // Draw initial hand (5 cards) - according to Mage Knight rules
        var hand = startingCards.Take(5).ToList();
        var deck = startingCards.Skip(5).ToList();

        // Log for debugging (can be removed in production)
        System.Diagnostics.Debug.WriteLine(
            $"Player {player.UserId}: Hero={heroId}, Name={heroName}, " +
            $"Total cards={startingCards.Count}, Hand={hand.Count}, Deck={deck.Count}");

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
            Deck = new List<string>(), // Deck is for drawing during game (starts empty after initial draw)
            DeedDeck = deck, // DeedDeck is the main deck - remaining cards after initial hand
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

    /// <summary>
    /// Extracts hero name from heroId (e.g., "hero_tovak" -> "Tovak").
    /// </summary>
    private string ExtractHeroName(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
            return "Tovak"; // Default
        
        // Remove "hero_" prefix if present
        var name = heroId.StartsWith("hero_", StringComparison.OrdinalIgnoreCase)
            ? heroId.Substring(5)
            : heroId;
        
        // Capitalize first letter (Tovak, Arythea, etc.)
        if (name.Length > 0)
        {
            return char.ToUpperInvariant(name[0]) + (name.Length > 1 ? name.Substring(1).ToLowerInvariant() : "");
        }
        
        return name;
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

        // Create ruins token deck - each token appears 'count' times
        var ruinsTokens = await _definitions.GetRuinsTokensAsync();
        var ruinsTokenDeck = ruinsTokens
            .SelectMany(r => Enumerable.Repeat(r.Id, r.Count))
            .ToList();
        Shuffle(ruinsTokenDeck);

        return new DeckState
        {
            AdvancedActions = advancedActions,
            Spells = spells,
            Artifacts = artifacts,
            RegularUnits = regularUnits,
            EliteUnits = eliteUnits,
            CountrysideTiles = await CreateTileDeck("Countryside", scenario.TilesDeck.Countryside),
            CoreTiles = await CreateTileDeck("Core", scenario.TilesDeck.Core),
            CityTiles = await CreateTileDeck("City", scenario.TilesDeck.Cities),
            EnemyDecks = enemyDecks,
            RuinsTokens = ruinsTokenDeck,
            CityLevels = scenario.CityLevels.ToList()
        };
    }

    private async Task<List<string>> CreateTileDeck(string type, int count)
    {
        // Get actual tile definitions from JSON
        var allTiles = await _definitions.GetMapTilesAsync();
        
        // Filter tiles by back_type (Countryside, Core, City)
        var matchingTiles = allTiles
            .Where(t => string.Equals(t.BackType, type, StringComparison.OrdinalIgnoreCase) && 
                       !t.IsStartingTile)
            .Select(t => t.Id)
            .ToList();
        
        // If we have enough tiles, use them; otherwise generate placeholders
        if (matchingTiles.Count >= count)
        {
            // Take the number we need and shuffle
            var selected = matchingTiles.Take(count).ToList();
            Shuffle(selected);
            return selected;
        }
        else
        {
            // Use available tiles and generate placeholders for the rest
            var result = matchingTiles.ToList();
            var needed = count - result.Count;
            for (int i = 1; i <= needed; i++)
            {
                result.Add($"{type.ToLower()}_{i}");
            }
            Shuffle(result);
            return result;
        }
    }

    private async Task<MapState> CreateInitialMapAsync(ScenarioDefinition scenario, int playerCount)
    {
        var map = new MapState();

        // Load starting tile from JSON definition
        var startingTileDef = (await _definitions.GetMapTilesAsync())
            .FirstOrDefault(t => t.Id == "tile_01_start" || t.IsStartingTile);

        // Position mapping: tile position index -> axial coordinates (Q, R)
        // Based on tile images orientation:
        // Position 1 (Kl 12/Top) maps to East (1,0) in game coordinates
        // Position 4 (Kl 6/Bottom) maps to West (-1,0) in game coordinates
        //        (2)   (3)
        //     (4)  (0)  (1)
        //        (5)   (6)
        var positionToCoord = new (int q, int r)[]
        {
            (0, 0),    // Position 0: Center
            (1, 0),    // Position 1: Top (Kl 12) → East
            (0, -1),   // Position 2: Top-Right (Kl 2) → NW
            (1, -1),   // Position 3: Bottom-Right (Kl 4) → NE
            (-1, 0),   // Position 4: Bottom (Kl 6) → West
            (0, 1),    // Position 5: Bottom-Left (Kl 8) → SE
            (-1, 1)    // Position 6: Top-Left (Kl 10) → SW
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

        // Generate hex data from tile definition
        if (startingTileDef != null)
        {
            foreach (var hexDef in startingTileDef.Hexes)
            {
                var (q, r) = positionToCoord[hexDef.Position];
                var key = $"{q},{r}";
                map.RevealedHexes.Add(key);
                map.HexData[key] = new HexState
                {
                    Terrain = hexDef.Terrain,
                    SiteType = hexDef.Site,
                    Enemies = new List<string>(),
                    IsConquered = hexDef.Site == "Portal" // Portal is always "conquered"
                };
            }
        }
        else
        {
            // Fallback: hardcoded values matching tile_01_start in JSON
            var startingTileHexes = new (int q, int r, string terrain, string? site)[]
            {
                (0, 0, "Plains", "Portal"),      // Position 0: Center - Portal
                (-1, 0, "Plains", null),         // Position 1: West - Plains
                (0, -1, "Forest", null),         // Position 2: North - Forest
                (1, -1, "Plains", null),         // Position 3: NE - Plains
                (1, 0, "Water", null),           // Position 4: East - Water
                (0, 1, "Water", null),           // Position 5: South - Water
                (-1, 1, "Water", null),          // Position 6: SW - Water
            };

            foreach (var (q, r, terrain, site) in startingTileHexes)
            {
                var key = $"{q},{r}";
                map.RevealedHexes.Add(key);
                map.HexData[key] = new HexState
                {
                    Terrain = terrain,
                    SiteType = site,
                    Enemies = new List<string>(),
                    IsConquered = site == "Portal"
                };
            }
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
        return RollManaSource(playerCount, isDay: true).Select(d => d.Color).ToList();
    }

    private List<ManaDieState> RollManaSource(int playerCount, bool isDay)
    {
        var diceCount = playerCount + 2; // Base rule: players + 2 dice
        var source = new List<ManaDieState>();

        do
        {
            source.Clear();
            for (int i = 0; i < diceCount; i++)
            {
                source.Add(new ManaDieState { Color = RollManaDie(), IsDepleted = false });
            }
        }
        while (source.Count(d => IsBasicMana(d.Color)) < Math.Ceiling(diceCount / 2.0));

        foreach (var die in source)
        {
            die.IsDepleted = IsDepletedForTime(die.Color, isDay);
        }

        return source;
    }

    private ManaColor RollManaDie()
    {
        var colors = new[] { ManaColor.Red, ManaColor.Blue, ManaColor.Green, ManaColor.White, ManaColor.Black, ManaColor.Gold };
        return colors[_random.Next(colors.Length)];
    }

    private static bool IsBasicMana(ManaColor color)
    {
        return color is ManaColor.Red or ManaColor.Blue or ManaColor.Green or ManaColor.White;
    }

    private static bool IsDepletedForTime(ManaColor color, bool isDay)
    {
        return (isDay && color == ManaColor.Black) || (!isDay && color == ManaColor.Gold);
    }

    private OfferState CreateInitialOffers(DeckState decks)
    {
        var offers = new OfferState();
        DrawOffer(decks.AdvancedActions, offers.AdvancedActions, 3);
        DrawOffer(decks.Spells, offers.Spells, 3);
        DrawOffer(decks.RegularUnits, offers.RegularUnits, 3);
        DrawOffer(decks.EliteUnits, offers.EliteUnits, 2);
        return offers;
    }

    private static void DrawOffer(List<string> deck, List<string> offer, int targetSize)
    {
        while (offer.Count < targetSize && deck.Count > 0)
        {
            offer.Add(deck[0]);
            deck.RemoveAt(0);
        }
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
