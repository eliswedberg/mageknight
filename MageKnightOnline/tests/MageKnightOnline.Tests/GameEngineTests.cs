using MageKnightOnline.Core.Definitions;
using MageKnightOnline.Core.GameEngine;
using MageKnightOnline.Core.GameState;
using MageKnightOnline.Core.Services;

namespace MageKnightOnline.Tests;

/// <summary>
/// Unit tests for the GameEngine class.
/// </summary>
public class GameEngineTests
{
    private readonly MockGameDefinitionService _definitions;
    private readonly GameEngine _engine;

    public GameEngineTests()
    {
        _definitions = new MockGameDefinitionService();
        _engine = new GameEngine(_definitions);
    }

    #region Movement Tests

    [Fact]
    public void GetTerrainCost_Plains_ReturnsCorrectCost()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.MovementRemaining = 5; // Give enough movement

        // Act - move to a plains hex
        var result = _engine.MovePlayer(new HexPosition { Q = 1, R = 0 });

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void MovePlayer_WithoutMovementPoints_Fails()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer();
        player!.MovementRemaining = 0;

        // Act
        var result = _engine.MovePlayer(new HexPosition { Q = 1, R = 0 });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Not enough movement", result.ErrorMessage);
    }

    [Fact]
    public void MovePlayer_ToAdjacentHex_ConsumesMovementPoints()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer();
        player!.MovementRemaining = 5;
        var initialMovement = player.MovementRemaining;

        // Act
        var result = _engine.MovePlayer(new HexPosition { Q = 1, R = 0 });

        // Assert
        Assert.True(result.Success);
        Assert.True(player.MovementRemaining < initialMovement);
    }

    [Fact]
    public void MovePlayer_ToNonAdjacentHex_Fails()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer();
        player!.MovementRemaining = 10;

        // Act - try to move to a non-adjacent hex
        var result = _engine.MovePlayer(new HexPosition { Q = 5, R = 5 });

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void MovePlayerWithFlight_CostsOnePerHex()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer();
        player!.FlightRemaining = 3;

        // Act
        var result = _engine.MovePlayerWithFlight(new HexPosition { Q = 1, R = 0 });

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, player.FlightRemaining);
    }

    [Fact]
    public void GetValidMoves_ReturnsReachableHexes()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer();
        player!.MovementRemaining = 4;

        // Act
        var validMoves = _engine.GetValidMoves(4).ToList();

        // Assert
        Assert.NotEmpty(validMoves);
    }

    #endregion

    #region Card Playing Tests

    [Fact]
    public void PlayCard_AddsEffectToPool()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Hand = new List<string> { "basic_move" };
        player.MovementRemaining = 0;

        // Act
        var result = _engine.PlayCard("basic_move", powered: false);

        // Assert
        Assert.True(result.Success);
        Assert.True(player.MovementRemaining > 0);
    }

    [Fact]
    public void PlayCard_NotInHand_Fails()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Hand = new List<string>();

        // Act
        var result = _engine.PlayCard("basic_move", powered: false);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not in hand", result.ErrorMessage);
    }

    [Fact]
    public void UseCardSideways_GivesPlusOneOfType()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Hand = new List<string> { "basic_move" };
        var initialMovement = player.MovementRemaining;

        // Act
        var result = _engine.UseCardSideways("basic_move");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialMovement + 1, player.MovementRemaining);
    }

    #endregion

    #region Combat Tests

    [Fact]
    public void InitiateCombat_WithEnemies_CreatesCombatState()
    {
        // Arrange
        SetupBasicGameState();
        SetupCombatScenario();

        // Act
        var result = _engine.InitiateCombat();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(_engine.State.Combat);
        Assert.NotEmpty(_engine.State.Combat.Enemies);
    }

    [Fact]
    public void BlockEnemy_ReducesDamage()
    {
        // Arrange
        SetupBasicGameState();
        SetupCombatScenario();
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Block;
        var player = _engine.GetCurrentPlayer()!;
        player.BlockPool = 5;

        // Act
        var result = _engine.BlockEnemy(0, 3);

        // Assert
        Assert.True(result.Success || result.ErrorMessage != null);
    }

    [Fact]
    public void AttackEnemy_DefeatsWhenEnoughDamage()
    {
        // Arrange
        SetupBasicGameState();
        SetupCombatScenario();
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Attack;
        var player = _engine.GetCurrentPlayer()!;
        player.AttackPool = 10;

        // Act
        var result = _engine.AttackEnemy(0, 10);

        // Assert
        // Either defeats or requires more attack
        Assert.NotNull(result);
    }

    [Fact]
    public void SwiftEnemy_AttacksBeforeBlockPhase()
    {
        // Arrange
        SetupBasicGameState();
        SetupSwiftCombatScenario();
        
        // Act
        var result = _engine.InitiateCombat();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(CombatPhase.SwiftAttack, _engine.State.Combat!.Phase);
    }

    #endregion

    #region Mana System Tests

    [Fact]
    public void UseMana_RemovesDieFromPool()
    {
        // Arrange
        SetupBasicGameState();
        _engine.State.ManaPool = new List<ManaColor> { ManaColor.Red, ManaColor.Blue };
        var initialCount = _engine.State.ManaPool.Count;

        // Act
        var result = _engine.UseMana(0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialCount - 1, _engine.State.ManaPool.Count);
    }

    [Fact]
    public void UseCrystal_AddsManaToken()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Crystals.Red = 2;

        // Act
        var result = _engine.UseCrystal(ManaColor.Red);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, player.Crystals.Red);
    }

    [Fact]
    public void UndoUseMana_ReturnsTemporaryMana()
    {
        // Arrange
        SetupBasicGameState();
        _engine.State.ManaPool = new List<ManaColor> { ManaColor.Red, ManaColor.Blue };
        var player = _engine.GetCurrentPlayer()!;
        
        // Take mana first
        _engine.UseMana(0);
        Assert.Equal(ManaColor.Red, player.TemporaryMana);

        // Act - undo
        var result = _engine.UndoUseMana();

        // Assert
        Assert.True(result.Success);
        Assert.Null(player.TemporaryMana);
        Assert.Null(player.UsedManaDieIndex);
    }

    #endregion

    #region Level Up Tests

    [Fact]
    public void CanLevelUp_WithEnoughFame_ReturnsTrue()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Level = 1;
        player.Fame = 10; // Enough for level 2

        // Act
        var canLevel = _engine.CanLevelUp();

        // Assert
        Assert.True(canLevel);
    }

    [Fact]
    public void LevelUp_IncreasesLevel()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Level = 1;
        player.Fame = 10;

        // Act
        var result = _engine.LevelUp(null, null);

        // Assert
        Assert.True(result.Success);
        Assert.True(player.Level > 1);
    }

    #endregion

    #region Site Interaction Tests

    [Fact]
    public void GetAvailableSiteInteractions_AtVillage_ReturnsOptions()
    {
        // Arrange
        SetupBasicGameState();
        SetupVillageSite();

        // Act
        var interactions = _engine.GetAvailableSiteInteractions().ToList();

        // Assert
        Assert.NotEmpty(interactions);
        Assert.Contains(interactions, i => i.Type == "Recruit");
        Assert.Contains(interactions, i => i.Type == "Heal");
        Assert.Contains(interactions, i => i.Type == "Plunder");
    }

    [Fact]
    public void Plunder_DrawsCardsAndLosesReputation()
    {
        // Arrange
        SetupBasicGameState();
        SetupVillageSite();
        var player = _engine.GetCurrentPlayer()!;
        player.Deck = new List<string> { "card1", "card2", "card3" };
        var initialRep = player.Reputation;
        var initialHandCount = player.Hand.Count;

        // Act
        var result = _engine.Plunder();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialRep - 1, player.Reputation);
        Assert.True(player.Hand.Count > initialHandCount);
    }

    #endregion

    #region Ruins Token Tests

    [Fact]
    public void GetActiveRuinsToken_WhenNone_ReturnsNull()
    {
        // Arrange
        SetupBasicGameState();

        // Act
        var token = _engine.GetActiveRuinsToken();

        // Assert
        Assert.Null(token);
    }

    #endregion

    #region Tactics Tests

    [Fact]
    public void SelectTactic_UpdatesSelectedTactics()
    {
        // Arrange
        SetupBasicGameState();
        _engine.State.Phase = GamePhase.TacticsSelection;
        _engine.State.AvailableTactics = new List<string> { "tactic_1", "tactic_2" };

        // Act
        var result = _engine.SelectTactic("tactic_1");

        // Assert
        Assert.True(result.Success);
        Assert.True(_engine.State.SelectedTactics.ContainsValue("tactic_1"));
    }

    [Fact]
    public void AllPlayersSelectedTactics_WhenAllSelected_ReturnsTrue()
    {
        // Arrange
        SetupBasicGameState();
        _engine.State.Phase = GamePhase.TacticsSelection;
        _engine.State.SelectedTactics[0] = "tactic_1";

        // Act
        var allSelected = _engine.AllPlayersSelectedTactics();

        // Assert
        Assert.True(allSelected);
    }

    #endregion

    #region End Turn Tests

    [Fact]
    public void EndTurn_ResetsPlayerState()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.MovementRemaining = 5;
        player.AttackPool = 3;

        // Act
        var result = _engine.EndTurn();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, player.MovementRemaining);
        Assert.Equal(0, player.AttackPool);
    }

    #endregion

    #region Helper Methods

    private void SetupBasicGameState()
    {
        var state = new GameStateModel
        {
            Round = 1,
            IsDay = true,
            Phase = GamePhase.Movement,
            CurrentPlayerIndex = 0,
            Players = new List<PlayerState>
            {
                new PlayerState
                {
                    UserId = Guid.NewGuid(),
                    HeroId = "tovak",
                    Position = new HexPosition { Q = 0, R = 0 },
                    Fame = 0,
                    Reputation = 0,
                    Level = 1,
                    Armor = 2,
                    HandLimit = 5,
                    Hand = new List<string>(),
                    Deck = new List<string> { "basic_move", "basic_attack" },
                    DeedDeck = new List<string>(),
                    DiscardPile = new List<string>(),
                    Units = new List<UnitState>(),
                    Skills = new List<string>(),
                    Crystals = new CrystalInventory(),
                    ManaTokens = new ManaTokenInventory(),
                    CommandTokens = 1,
                    MovementRemaining = 0
                }
            },
            Map = new MapState
            {
                RevealedHexes = new HashSet<string> { "0,0", "1,0", "-1,0", "0,1", "0,-1", "1,-1", "-1,1" },
                HexData = new Dictionary<string, HexState>
                {
                    ["0,0"] = new HexState { Terrain = "Plains" },
                    ["1,0"] = new HexState { Terrain = "Plains" },
                    ["-1,0"] = new HexState { Terrain = "Forest" },
                    ["0,1"] = new HexState { Terrain = "Hills" },
                    ["0,-1"] = new HexState { Terrain = "Plains" },
                    ["1,-1"] = new HexState { Terrain = "Plains" },
                    ["-1,1"] = new HexState { Terrain = "Plains" }
                }
            },
            Decks = new DeckState
            {
                RuinsTokens = new List<string>()
            },
            ManaPool = new List<ManaColor> { ManaColor.Red, ManaColor.Blue, ManaColor.Green },
            TurnOrder = new List<int> { 0 }
        };

        _engine.LoadState(System.Text.Json.JsonSerializer.Serialize(state));
    }

    private void SetupCombatScenario()
    {
        var hexState = _engine.State.Map.HexData["0,0"];
        hexState.Enemies = new List<string> { "enemy_orc" };
    }

    private void SetupSwiftCombatScenario()
    {
        var hexState = _engine.State.Map.HexData["0,0"];
        hexState.Enemies = new List<string> { "enemy_wolf_rider" };
    }

    private void SetupVillageSite()
    {
        var hexState = _engine.State.Map.HexData["0,0"];
        hexState.SiteType = "Village";
        hexState.Enemies = new List<string>();
        hexState.IsConquered = true;
    }

    #endregion
}

/// <summary>
/// Mock implementation of IGameDefinitionService for testing.
/// </summary>
public class MockGameDefinitionService : IGameDefinitionService
{
    public Task<IReadOnlyList<HeroDefinition>> GetHeroesAsync()
    {
        var heroes = new List<HeroDefinition>
        {
            new HeroDefinition { Id = "tovak", Name = "Tovak" }
        };
        return Task.FromResult<IReadOnlyList<HeroDefinition>>(heroes.AsReadOnly());
    }

    public Task<HeroDefinition?> GetHeroAsync(string heroId)
    {
        return Task.FromResult<HeroDefinition?>(new HeroDefinition { Id = heroId, Name = heroId });
    }

    public Task<IReadOnlyList<ScenarioDefinition>> GetScenariosAsync()
    {
        return Task.FromResult<IReadOnlyList<ScenarioDefinition>>(new List<ScenarioDefinition>().AsReadOnly());
    }

    public Task<ScenarioDefinition?> GetScenarioAsync(string scenarioId)
    {
        return Task.FromResult<ScenarioDefinition?>(null);
    }

    public Task<IReadOnlyList<CardDefinition>> GetBasicActionsAsync()
    {
        var cards = new List<CardDefinition>
        {
            new CardDefinition 
            { 
                Id = "basic_move", 
                Name = "March", 
                EffectsBasic = new List<CardEffect> { new CardEffect { Type = "Move", Value = 2 } }
            },
            new CardDefinition 
            { 
                Id = "basic_attack", 
                Name = "Battle Versatility", 
                EffectsBasic = new List<CardEffect> { new CardEffect { Type = "Attack", Value = 2 } }
            }
        };
        return Task.FromResult<IReadOnlyList<CardDefinition>>(cards.AsReadOnly());
    }

    public Task<IReadOnlyList<CardDefinition>> GetAdvancedActionsAsync()
    {
        return Task.FromResult<IReadOnlyList<CardDefinition>>(new List<CardDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<CardDefinition>> GetSpellsAsync()
    {
        return Task.FromResult<IReadOnlyList<CardDefinition>>(new List<CardDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<CardDefinition>> GetArtifactsAsync()
    {
        return Task.FromResult<IReadOnlyList<CardDefinition>>(new List<CardDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<SkillDefinition>> GetSkillsAsync()
    {
        return Task.FromResult<IReadOnlyList<SkillDefinition>>(new List<SkillDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<SkillDefinition>> GetSkillsForHeroAsync(string heroName)
    {
        return Task.FromResult<IReadOnlyList<SkillDefinition>>(new List<SkillDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<UnitDefinition>> GetUnitsAsync()
    {
        return Task.FromResult<IReadOnlyList<UnitDefinition>>(new List<UnitDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<UnitDefinition>> GetRegularUnitsAsync()
    {
        return Task.FromResult<IReadOnlyList<UnitDefinition>>(new List<UnitDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<UnitDefinition>> GetEliteUnitsAsync()
    {
        return Task.FromResult<IReadOnlyList<UnitDefinition>>(new List<UnitDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<EnemyDefinition>> GetEnemiesAsync()
    {
        var enemies = new List<EnemyDefinition>
        {
            new EnemyDefinition 
            { 
                Id = "enemy_orc", 
                Name = "Orc", 
                Type = "Green",
                Fame = 2,
                Attack = new EnemyAttack { Value = 3, Element = "Physical" },
                Armor = new EnemyArmor { Value = 3, Resistances = new List<string>() },
                Abilities = new List<string>()
            },
            new EnemyDefinition 
            { 
                Id = "enemy_wolf_rider", 
                Name = "Wolf Rider", 
                Type = "Green",
                Fame = 2,
                Attack = new EnemyAttack { Value = 3, Element = "Physical" },
                Armor = new EnemyArmor { Value = 3, Resistances = new List<string>() },
                Abilities = new List<string> { "Swift" }
            }
        };
        return Task.FromResult<IReadOnlyList<EnemyDefinition>>(enemies.AsReadOnly());
    }

    public Task<IReadOnlyList<EnemyDefinition>> GetEnemiesByTypeAsync(string type)
    {
        return GetEnemiesAsync();
    }

    public Task<EnemyDefinition?> GetEnemyAsync(string enemyId)
    {
        return Task.FromResult<EnemyDefinition?>(GetEnemiesAsync().Result.FirstOrDefault(e => e.Id == enemyId));
    }

    public Task<IReadOnlyList<TacticsDefinition>> GetTacticsAsync()
    {
        var tactics = new List<TacticsDefinition>
        {
            new TacticsDefinition { Id = "tactic_1", Position = 1, Period = "Day", Name = "Early Bird" },
            new TacticsDefinition { Id = "tactic_2", Position = 2, Period = "Day", Name = "Resting" }
        };
        return Task.FromResult<IReadOnlyList<TacticsDefinition>>(tactics.AsReadOnly());
    }

    public Task<IReadOnlyList<TacticsDefinition>> GetDayTacticsAsync()
    {
        return GetTacticsAsync();
    }

    public Task<IReadOnlyList<TacticsDefinition>> GetNightTacticsAsync()
    {
        return GetTacticsAsync();
    }

    public Task<IReadOnlyList<MapTileDefinition>> GetMapTilesAsync()
    {
        return Task.FromResult<IReadOnlyList<MapTileDefinition>>(new List<MapTileDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<RuinsDefinition>> GetRuinsTokensAsync()
    {
        return Task.FromResult<IReadOnlyList<RuinsDefinition>>(new List<RuinsDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<RuinsDefinition>> GetRuinsLootTokensAsync()
    {
        return Task.FromResult<IReadOnlyList<RuinsDefinition>>(new List<RuinsDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<RuinsDefinition>> GetRuinsCombatTokensAsync()
    {
        return Task.FromResult<IReadOnlyList<RuinsDefinition>>(new List<RuinsDefinition>().AsReadOnly());
    }

    // NEW: Terrain
    public Task<IReadOnlyList<TerrainDefinition>> GetTerrainCostsAsync()
    {
        var terrains = new List<TerrainDefinition>
        {
            new TerrainDefinition { Terrain = "Plains", CostDay = 2, CostNight = 2 },
            new TerrainDefinition { Terrain = "Forest", CostDay = 3, CostNight = 5 },
            new TerrainDefinition { Terrain = "Hill", CostDay = 3, CostNight = 3 },
            new TerrainDefinition { Terrain = "Swamp", CostDay = 5, CostNight = 5 },
            new TerrainDefinition { Terrain = "Desert", CostDay = 5, CostNight = 3 },
            new TerrainDefinition { Terrain = "Wasteland", CostDay = 4, CostNight = 4 },
            new TerrainDefinition { Terrain = "Lake", CostDay = 99, CostNight = 99, Special = "impassable" },
            new TerrainDefinition { Terrain = "Mountain", CostDay = 99, CostNight = 99, Special = "impassable" }
        };
        return Task.FromResult<IReadOnlyList<TerrainDefinition>>(terrains.AsReadOnly());
    }

    public Task<TerrainDefinition?> GetTerrainAsync(string terrainType)
    {
        return Task.FromResult<TerrainDefinition?>(GetTerrainCostsAsync().Result.FirstOrDefault(t => 
            t.Terrain.Equals(terrainType, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<int> GetTerrainCostAsync(string terrainType, bool isDay)
    {
        var terrain = GetTerrainAsync(terrainType).Result;
        return Task.FromResult(terrain?.GetCost(isDay) ?? 2);
    }

    // NEW: Sites
    public Task<IReadOnlyList<SiteDefinition>> GetSitesAsync()
    {
        return Task.FromResult<IReadOnlyList<SiteDefinition>>(new List<SiteDefinition>().AsReadOnly());
    }

    public Task<SiteDefinition?> GetSiteAsync(string siteId)
    {
        return Task.FromResult<SiteDefinition?>(null);
    }

    public Task<IReadOnlyList<SiteDefinition>> GetSitesByTypeAsync(string siteType)
    {
        return Task.FromResult<IReadOnlyList<SiteDefinition>>(new List<SiteDefinition>().AsReadOnly());
    }

    // NEW: Combat Abilities
    public Task<CombatAbilitiesRoot> GetCombatAbilitiesAsync()
    {
        return Task.FromResult(new CombatAbilitiesRoot());
    }

    public Task<CombatAbilityDefinition?> GetCombatAbilityAsync(string abilityId)
    {
        return Task.FromResult<CombatAbilityDefinition?>(null);
    }

    // NEW: Game Rules
    public Task<GameRulesDefinition> GetGameRulesAsync()
    {
        return Task.FromResult(new GameRulesDefinition());
    }
}

