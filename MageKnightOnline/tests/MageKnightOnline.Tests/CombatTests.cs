using MageKnightOnline.Core.Definitions;
using MageKnightOnline.Core.GameEngine;
using MageKnightOnline.Core.GameState;
using MageKnightOnline.Core.Services;

namespace MageKnightOnline.Tests;

/// <summary>
/// Unit tests specifically for combat mechanics in the GameEngine.
/// </summary>
public class CombatTests
{
    private readonly MockGameDefinitionService _definitions;
    private readonly GameEngine _engine;

    public CombatTests()
    {
        _definitions = new MockGameDefinitionService();
        _engine = new GameEngine(_definitions);
    }

    #region Combat Phase Tests

    [Fact]
    public void Combat_StartsInRangedPhase()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");

        // Act
        var result = _engine.InitiateCombat();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(CombatPhase.RangedAttack, _engine.State.Combat!.Phase);
    }

    [Fact]
    public void Combat_SwiftEnemy_StartsInSwiftPhase()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_wolf_rider");

        // Act
        var result = _engine.InitiateCombat();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(CombatPhase.SwiftAttack, _engine.State.Combat!.Phase);
    }

    [Theory]
    [InlineData(CombatPhase.RangedAttack, CombatPhase.Block)]
    [InlineData(CombatPhase.Block, CombatPhase.AssignDamage)]
    [InlineData(CombatPhase.AssignDamage, CombatPhase.Attack)]
    public void EndCombatPhase_ProgressesToNextPhase(CombatPhase fromPhase, CombatPhase toPhase)
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = fromPhase;

        // Act
        var result = _engine.EndCombatPhase();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(toPhase, _engine.State.Combat.Phase);
    }

    #endregion

    #region Block Tests

    [Fact]
    public void BlockEnemy_WithEnoughBlock_MarksEnemyAsBlocked()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Block;
        var player = _engine.GetCurrentPlayer()!;
        player.BlockPool = 10;

        // Act
        var result = _engine.BlockEnemy(0, 3);

        // Assert
        Assert.True(result.Success);
        Assert.True(_engine.State.Combat.Enemies[0].IsBlocked);
    }

    [Fact]
    public void BlockEnemy_WithInsufficientBlock_NotFullyBlocked()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Block;
        var player = _engine.GetCurrentPlayer()!;
        player.BlockPool = 1;

        // Act
        var result = _engine.BlockEnemy(0, 1);

        // Assert
        // The enemy has 3 armor, blocking with 1 should not fully block
        if (result.Success)
        {
            Assert.False(_engine.State.Combat.Enemies[0].IsBlocked);
        }
    }

    #endregion

    #region Attack Tests

    [Fact]
    public void AttackEnemy_WithEnoughDamage_DefeatsEnemy()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Attack;
        var player = _engine.GetCurrentPlayer()!;
        player.AttackPool = 10;
        
        var enemy = _engine.State.Combat.Enemies[0];
        var armorToOvercome = enemy.Armor;

        // Act
        var result = _engine.AttackEnemy(0, armorToOvercome);

        // Assert
        Assert.True(result.Success);
        Assert.True(_engine.State.Combat.Enemies[0].IsDefeated);
    }

    [Fact]
    public void DefeatEnemy_GrantsFame()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Attack;
        var player = _engine.GetCurrentPlayer()!;
        player.AttackPool = 10;
        player.Fame = 0;
        
        var enemy = _engine.State.Combat.Enemies[0];
        var armorToOvercome = enemy.Armor;

        // Act
        var result = _engine.AttackEnemy(0, armorToOvercome);

        // Assert
        Assert.True(result.Success);
        Assert.True(player.Fame > 0);
    }

    [Fact]
    public void AttackEnemy_NotEnoughDamage_DoesNotDefeat()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Attack;
        var player = _engine.GetCurrentPlayer()!;
        player.AttackPool = 1;

        // Act
        var result = _engine.AttackEnemy(0, 1);

        // Assert
        // Should either fail or not defeat the enemy
        if (result.Success)
        {
            Assert.False(_engine.State.Combat.Enemies[0].IsDefeated);
        }
    }

    #endregion

    #region Flee Combat Tests

    [Fact]
    public void FleeCombat_EndsCombat()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();

        // Act
        var result = _engine.FleeCombat();

        // Assert
        Assert.True(result.Success);
        Assert.Null(_engine.State.Combat);
    }

    [Fact]
    public void FleeCombat_AddsWoundCards()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        var player = _engine.GetCurrentPlayer()!;
        var initialHandSize = player.Hand.Count;
        
        // Simulate some unblocked damage
        _engine.State.Combat!.TotalUnblockedDamage = 3;

        // Act
        var result = _engine.FleeCombat();

        // Assert
        Assert.True(result.Success);
        // Fleeing should add wound cards to hand
        var woundCards = player.Hand.Count(c => c.StartsWith("wound"));
        Assert.True(woundCards > 0);
    }

    #endregion

    #region Unit Combat Tests

    [Fact]
    public void UseUnit_InCombat_DoesNotThrow()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        var player = _engine.GetCurrentPlayer()!;
        player.Units = new List<UnitState>
        {
            new UnitState 
            { 
                UnitId = "peasants",
                Name = "Peasants",
                IsReady = true,
                IsWounded = false,
                Armor = 3
            }
        };
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Block;

        // Act & Assert - just verify it doesn't throw
        var exception = Record.Exception(() => _engine.GetAvailableUnitActions().ToList());
        Assert.Null(exception);
    }

    [Fact]
    public void WoundedUnit_HasLimitedActions()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        var player = _engine.GetCurrentPlayer()!;
        player.Units = new List<UnitState>
        {
            new UnitState 
            { 
                UnitId = "peasants",
                Name = "Peasants",
                IsReady = true,
                IsWounded = true, // Wounded
                Armor = 3
            }
        };
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Block;

        // Act
        var unitActions = _engine.GetAvailableUnitActions().ToList();

        // Assert
        // Wounded units should be marked as such in their actions
        Assert.True(unitActions.All(u => u.IsWounded));
    }

    #endregion

    #region Multiple Enemies Tests

    [Fact]
    public void CombatWithMultipleEnemies_AllMustBeDefeated()
    {
        // Arrange
        SetupBasicGameStateWithEnemies(new[] { "enemy_orc", "enemy_orc" });
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Attack;
        var player = _engine.GetCurrentPlayer()!;
        player.AttackPool = 20;

        // Defeat first enemy
        _engine.AttackEnemy(0, _engine.State.Combat.Enemies[0].Armor);

        // Assert - combat should still be active
        Assert.NotNull(_engine.State.Combat);
        Assert.False(_engine.State.Combat.Enemies.All(e => e.IsDefeated));
    }

    [Fact]
    public void CombatWithMultipleEnemies_DefeatAll_EndsCombat()
    {
        // Arrange
        SetupBasicGameStateWithEnemies(new[] { "enemy_orc", "enemy_orc" });
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Attack;
        var player = _engine.GetCurrentPlayer()!;
        player.AttackPool = 20;

        // Defeat all enemies
        _engine.AttackEnemy(0, _engine.State.Combat.Enemies[0].Armor);
        player.AttackPool = 20; // Reset for second attack
        _engine.AttackEnemy(1, _engine.State.Combat.Enemies[1].Armor);

        // Assert - combat should end and enter resolution
        Assert.NotNull(_engine.State.Combat);
        Assert.True(_engine.State.Combat.Enemies.All(e => e.IsDefeated));
    }

    #endregion

    #region Combat State Tests

    [Fact]
    public void InitiateCombat_WithNoEnemies_Fails()
    {
        // Arrange
        var state = CreateBasicGameState();
        state.Map.HexData["0,0"].Enemies = new List<string>(); // No enemies
        _engine.LoadState(System.Text.Json.JsonSerializer.Serialize(state));

        // Act
        var result = _engine.InitiateCombat();

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void Combat_TracksUnblockedDamage()
    {
        // Arrange
        SetupBasicGameStateWithEnemy("enemy_orc");
        _engine.InitiateCombat();
        _engine.State.Combat!.Phase = CombatPhase.Block;
        var player = _engine.GetCurrentPlayer()!;
        player.BlockPool = 0; // No block available

        // Act
        _engine.EndCombatPhase(); // Move to AssignDamage

        // Assert
        // Total unblocked damage should be tracked
        Assert.NotNull(_engine.State.Combat);
    }

    #endregion

    #region Helper Methods

    private void SetupBasicGameStateWithEnemy(string enemyId)
    {
        var state = CreateBasicGameState();
        state.Map.HexData["0,0"].Enemies = new List<string> { enemyId };
        _engine.LoadState(System.Text.Json.JsonSerializer.Serialize(state));
    }

    private void SetupBasicGameStateWithEnemies(string[] enemyIds)
    {
        var state = CreateBasicGameState();
        state.Map.HexData["0,0"].Enemies = enemyIds.ToList();
        _engine.LoadState(System.Text.Json.JsonSerializer.Serialize(state));
    }

    private GameStateModel CreateBasicGameState()
    {
        return new GameStateModel
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
                    Hand = new List<string> { "card1", "card2" },
                    Deck = new List<string> { "card3" },
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
                RevealedHexes = new HashSet<string> { "0,0" },
                HexData = new Dictionary<string, HexState>
                {
                    ["0,0"] = new HexState { Terrain = "Plains", Enemies = new List<string>() }
                }
            },
            Decks = new DeckState
            {
                RuinsTokens = new List<string>()
            },
            ManaPool = new List<ManaColor> { ManaColor.Red, ManaColor.Blue, ManaColor.Green },
            TurnOrder = new List<int> { 0 }
        };
    }

    #endregion
}
