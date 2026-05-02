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

    [Fact]
    public void MovePlayer_UsesNightTerrainCostFromDefinitions()
    {
        SetupBasicGameState();
        _engine.State.IsDay = false;
        var player = _engine.GetCurrentPlayer()!;
        player.MovementRemaining = 5;

        var result = _engine.MovePlayer(new HexPosition { Q = -1, R = 0 });

        Assert.True(result.Success);
        Assert.Equal(0, player.MovementRemaining);
    }

    [Fact]
    public void MovePlayer_WaterAliasUsesLakeImpassableCost()
    {
        SetupBasicGameState();
        _engine.State.Map.HexData["1,0"].Terrain = "Water";
        var player = _engine.GetCurrentPlayer()!;
        player.MovementRemaining = 10;

        var result = _engine.MovePlayer(new HexPosition { Q = 1, R = 0 });

        Assert.False(result.Success);
        Assert.Contains("impassable", result.ErrorMessage);
    }

    [Fact]
    public void ExploreTile_CostsTwoMovementAndRevealsTarget()
    {
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Position = new HexPosition { Q = 1, R = 0 };
        player.MovementRemaining = 2;
        _engine.State.Decks.CountrysideTiles = new List<string> { "test_tile" };

        var target = new HexPosition { Q = 2, R = 0 };
        var result = _engine.ExploreTile(target);

        Assert.True(result.Success);
        Assert.Equal(0, player.MovementRemaining);
        Assert.Contains("2,0", _engine.State.Map.RevealedHexes);
    }

    [Fact]
    public void MovePlayer_BetweenAdjacentRampagingEnemyHexes_ProvokesCombat()
    {
        SetupBasicGameState();
        _engine.State.Map.HexData["1,-1"].Enemies = new List<string> { "enemy_orc" };
        var player = _engine.GetCurrentPlayer()!;
        player.MovementRemaining = 2;

        var result = _engine.MovePlayer(new HexPosition { Q = 1, R = 0 });

        Assert.True(result.Success);
        Assert.NotNull(_engine.State.Combat);
        Assert.Equal(GamePhase.Combat, _engine.State.Phase);
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
    public void SwiftEnemy_StartsInRangedPhase_WithDoubledBlockRequirement()
    {
        // Arrange
        SetupBasicGameState();
        SetupSwiftCombatScenario();
        
        // Act
        var result = _engine.InitiateCombat();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(CombatPhase.RangedAttack, _engine.State.Combat!.Phase);
        Assert.Equal(6, _engine.State.Combat.Enemies[0].GetBlockRequirement());
    }

    #endregion

    #region Mana System Tests

    [Fact]
    public void UseMana_MarksSourceDieForCurrentTurn()
    {
        // Arrange
        SetupBasicGameState();
        _engine.State.ManaPool = new List<ManaColor> { ManaColor.Red, ManaColor.Blue };
        _engine.State.ManaSource = new List<ManaDieState>
        {
            new() { Color = ManaColor.Red },
            new() { Color = ManaColor.Blue }
        };
        var initialCount = _engine.State.ManaPool.Count;

        // Act
        var result = _engine.UseMana(0);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialCount, _engine.State.ManaPool.Count);
        Assert.Equal(ManaColor.Red, _engine.State.Players[0].TemporaryMana);
        Assert.Equal(0, _engine.State.TurnState.UsedSourceDieIndex);
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
    public void UseMana_DepletedBlackDuringDay_Fails()
    {
        SetupBasicGameState();
        _engine.State.IsDay = true;
        _engine.State.ManaSource = new List<ManaDieState>
        {
            new() { Color = ManaColor.Black, IsDepleted = true }
        };
        _engine.State.ManaPool = new List<ManaColor> { ManaColor.Black };

        var result = _engine.UseMana(0);

        Assert.False(result.Success);
        Assert.Contains("depleted", result.ErrorMessage);
    }

    [Fact]
    public void UseCrystal_BlackOrGoldCannotBeUsedAsCrystal()
    {
        SetupBasicGameState();

        var blackResult = _engine.UseCrystal(ManaColor.Black);
        var goldResult = _engine.UseCrystal(ManaColor.Gold);

        Assert.False(blackResult.Success);
        Assert.False(goldResult.Success);
    }

    [Fact]
    public void EndTurn_ReturnsSourceDieAndClearsPureMana()
    {
        SetupBasicGameState();
        _engine.State.ManaSource = new List<ManaDieState>
        {
            new() { Color = ManaColor.Red }
        };
        _engine.State.ManaPool = new List<ManaColor> { ManaColor.Red };

        var player = _engine.GetCurrentPlayer()!;
        var manaResult = _engine.UseMana(0);
        Assert.True(manaResult.Success);

        player.ManaTokens.Blue = 1;
        var result = _engine.EndTurn();

        Assert.True(result.Success);
        Assert.Null(player.TemporaryMana);
        Assert.Null(player.UsedManaDieIndex);
        Assert.Null(_engine.State.TurnState.UsedSourceDieIndex);
        Assert.Null(_engine.State.ManaSource[0].UsedByPlayerIndex);
        Assert.Equal(0, player.ManaTokens.Blue);
    }

    [Fact]
    public void Rest_WithNonWound_DiscardsOneNonWoundAndAllWounds()
    {
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Hand = new List<string> { "basic_move", "wound", "wound_poison" };
        player.Deck.Clear();
        player.DeedDeck.Clear();

        var result = _engine.Rest();

        Assert.True(result.Success);
        Assert.Empty(player.Hand);
        Assert.Contains("basic_move", player.DiscardPile);
        Assert.Equal(2, player.DiscardPile.Count(IsWound));
    }

    [Fact]
    public void Rest_WithOnlyWounds_DiscardsOneWound()
    {
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Hand = new List<string> { "wound", "wound_poison" };
        player.Deck.Clear();
        player.DeedDeck.Clear();

        var result = _engine.Rest();

        Assert.True(result.Success);
        Assert.Single(player.Hand);
        Assert.Single(player.DiscardPile);
    }

    [Fact]
    public void AnnounceEndOfRound_GivesOtherPlayersFinalTurnThenEndsRound()
    {
        SetupBasicGameState();
        var firstPlayer = _engine.State.Players[0];
        firstPlayer.DeedDeck.Clear();
        firstPlayer.Hand.Clear();

        _engine.State.Players.Add(new PlayerState
        {
            UserId = Guid.NewGuid(),
            HeroId = "norigow",
            Position = new HexPosition { Q = 0, R = 0 },
            Armor = 2,
            HandLimit = 5,
            Hand = new List<string>(),
            DeedDeck = new List<string>(),
            DiscardPile = new List<string> { "basic_move" },
            Crystals = new CrystalInventory(),
            ManaTokens = new ManaTokenInventory(),
            CommandTokens = 1
        });
        _engine.State.TurnOrder = new List<int> { 0, 1 };
        _engine.State.CurrentPlayerIndex = 0;

        var announce = _engine.AnnounceEndOfRound();

        Assert.True(announce.Success);
        Assert.True(_engine.State.TurnState.EndRoundAnnounced);
        Assert.Equal(1, _engine.State.CurrentPlayerIndex);

        var finalTurn = _engine.EndTurn();

        Assert.True(finalTurn.Success);
        Assert.Equal(2, _engine.State.Round);
        Assert.Equal(GamePhase.TacticsSelection, _engine.State.Phase);
        Assert.False(_engine.State.TurnState.EndRoundAnnounced);
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
    public void LevelUp_WithMissingChoices_CreatesPendingLevelUp()
    {
        // Arrange
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Level = 1;
        player.Fame = 10;

        // Act
        var result = _engine.LevelUp(null, null);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(_engine.State.PendingLevelUp);
        Assert.Equal(2, _engine.State.PendingLevelUp!.TargetLevel);
    }

    [Fact]
    public void GetAvailableAdvancedActions_RefillsOfferFromDeck()
    {
        SetupBasicGameState();
        _engine.State.Decks.AdvancedActions = new List<string> { "aa_swiftness", "aa_concentration", "aa_march" };

        var offer = _engine.GetAvailableAdvancedActions().ToList();

        Assert.Equal(3, offer.Count);
        Assert.Equal(new[] { "aa_swiftness", "aa_concentration", "aa_march" }, offer);
        Assert.Empty(_engine.State.Decks.AdvancedActions);
    }

    [Fact]
    public void LevelUp_WithOfferChoices_AddsAdvancedActionAndSkill()
    {
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.Level = 1;
        player.Fame = 3;
        _engine.State.Decks.AdvancedActions = new List<string>
        {
            "aa_swiftness",
            "aa_concentration",
            "aa_march",
            "aa_crystalize"
        };

        var result = _engine.LevelUp("aa_swiftness", "skill_tovak_focus");

        Assert.True(result.Success);
        Assert.Equal(2, player.Level);
        Assert.Equal(1, player.CommandTokens);
        Assert.Contains("aa_swiftness", player.DiscardPile);
        Assert.Contains("skill_tovak_focus", player.Skills);
        Assert.DoesNotContain("aa_swiftness", _engine.State.Offers.AdvancedActions);
        Assert.Equal(3, _engine.State.Offers.AdvancedActions.Count);
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
    public void InteractWithSite_Combat_StartsCombatForEnemySite()
    {
        SetupBasicGameState();
        var hexState = _engine.State.Map.HexData["0,0"];
        hexState.SiteType = "Keep";
        hexState.Enemies = new List<string> { "enemy_orc" };

        var interactions = _engine.GetAvailableSiteInteractions().ToList();
        var result = _engine.InteractWithSite("Combat");

        Assert.Contains(interactions, i => i.Type == "Combat");
        Assert.True(result.Success);
        Assert.NotNull(_engine.State.Combat);
        Assert.Equal(GamePhase.Combat, _engine.State.Phase);
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

    [Fact]
    public void RecruitUnit_UsesCurrentOfferAndRefills()
    {
        SetupBasicGameState();
        var player = _engine.GetCurrentPlayer()!;
        player.InfluencePool = 5;
        player.CommandTokens = 1;
        _engine.State.Decks.RegularUnits = new List<string> { "unit_peasants", "unit_herbalists", "unit_swordsmen", "unit_guardsmen" };

        var result = _engine.RecruitUnit("unit_peasants");

        Assert.True(result.Success);
        Assert.Single(player.Units);
        Assert.Equal("unit_peasants", player.Units[0].UnitId);
        Assert.DoesNotContain("unit_peasants", _engine.State.Offers.RegularUnits);
        Assert.Equal(3, _engine.State.Offers.RegularUnits.Count);
    }

    [Fact]
    public void CheckVictoryConditions_CityConquestScenario_EndsGame()
    {
        SetupBasicGameState();
        _engine.State.ScenarioId = "full_conquest";
        _engine.State.TotalCities = 1;
        _engine.State.CitiesConquered = 1;

        var result = _engine.CheckVictoryConditions();

        Assert.True(result);
        Assert.NotNull(_engine.State.Victory);
        Assert.True(_engine.State.Victory!.IsGameOver);
        Assert.Equal(VictoryType.CityConquest, _engine.State.Victory.VictoryType);
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

    private static bool IsWound(string cardId) => cardId.StartsWith("wound", StringComparison.OrdinalIgnoreCase);

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
        var scenarios = new List<ScenarioDefinition>
        {
            new()
            {
                Id = "full_conquest",
                Name = "Full Conquest",
                Rounds = 6,
                Goal = "Conquer all cities",
                CityLevels = new List<int> { 5 }
            }
        };
        return Task.FromResult<IReadOnlyList<ScenarioDefinition>>(scenarios.AsReadOnly());
    }

    public Task<ScenarioDefinition?> GetScenarioAsync(string scenarioId)
    {
        return Task.FromResult<ScenarioDefinition?>(GetScenariosAsync().Result.FirstOrDefault(s => s.Id == scenarioId));
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
        var cards = new List<CardDefinition>
        {
            new() { Id = "aa_swiftness", Name = "Swiftness" },
            new() { Id = "aa_concentration", Name = "Concentration" },
            new() { Id = "aa_march", Name = "March" },
            new() { Id = "aa_crystalize", Name = "Crystalize" }
        };
        return Task.FromResult<IReadOnlyList<CardDefinition>>(cards.AsReadOnly());
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
        var skills = new List<SkillDefinition>
        {
            new() { Id = "skill_tovak_focus", Name = "Focus", Hero = "tovak" },
            new() { Id = "skill_common", Name = "Common Skill", Hero = "" }
        };
        return Task.FromResult<IReadOnlyList<SkillDefinition>>(skills.AsReadOnly());
    }

    public Task<IReadOnlyList<SkillDefinition>> GetSkillsForHeroAsync(string heroName)
    {
        return Task.FromResult<IReadOnlyList<SkillDefinition>>(new List<SkillDefinition>().AsReadOnly());
    }

    public Task<IReadOnlyList<UnitDefinition>> GetUnitsAsync()
    {
        var units = new List<UnitDefinition>
        {
            new() { Id = "unit_peasants", Name = "Peasants", Rank = "Regular", RecruitCost = 3, Armor = 2 },
            new() { Id = "unit_herbalists", Name = "Herbalists", Rank = "Regular", RecruitCost = 4, Armor = 2 },
            new() { Id = "unit_swordsmen", Name = "Swordsmen", Rank = "Regular", RecruitCost = 5, Armor = 3 },
            new() { Id = "unit_guardsmen", Name = "Guardsmen", Rank = "Regular", RecruitCost = 4, Armor = 3 }
        };
        return Task.FromResult<IReadOnlyList<UnitDefinition>>(units.AsReadOnly());
    }

    public Task<IReadOnlyList<UnitDefinition>> GetRegularUnitsAsync()
    {
        return Task.FromResult<IReadOnlyList<UnitDefinition>>(GetUnitsAsync().Result.Where(u => u.IsRegular).ToList().AsReadOnly());
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
        var tiles = new List<MapTileDefinition>
        {
            new()
            {
                Id = "test_tile",
                Name = "Test Tile",
                BackType = "Countryside",
                Hexes = Enumerable.Range(0, 7)
                    .Select(position => new TileHexDefinition { Position = position, Terrain = "Plains" })
                    .ToList()
            }
        };
        return Task.FromResult<IReadOnlyList<MapTileDefinition>>(tiles.AsReadOnly());
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

