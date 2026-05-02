using MageKnightOnline.Core.Entities;
using MageKnightOnline.Core.GameEngine;
using MageKnightOnline.Core.GameState;
using MageKnightOnline.Core.Services;

namespace MageKnightOnline.Tests;

public class PlaythroughTests
{
    private readonly MockGameDefinitionService _definitions = new();

    [Fact]
    public async Task RealDefinitions_CanInitializeAndEnterFirstTurn()
    {
        var definitions = new GameDefinitionService(Path.Combine(GetRepositoryRoot(), "src", "MageKnightOnline.Web", "wwwroot", "data", "definitions"));
        var scenario = (await definitions.GetScenarioAsync("scn_01"))!;
        var userId = Guid.NewGuid();
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Real Data Smoke",
            ScenarioId = scenario.Id,
            Status = GameStatus.InProgress,
            Players = new List<GamePlayer>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    HeroId = "hero_tovak",
                    IsReady = true,
                    TurnOrder = 0
                }
            }
        };

        var state = await new GameStateInitializer(definitions).InitializeAsync(game, scenario);
        var engine = Load(state, definitions);

        Assert.Equal(GamePhase.TacticsSelection, engine.State.Phase);
        Assert.Equal(3, engine.State.ManaSource.Count);
        Assert.Equal(5, engine.State.Players[0].Hand.Count);
        Assert.NotEmpty(engine.State.AvailableTactics);
        Assert.NotEmpty(engine.State.Map.RevealedHexes);

        var tactic = engine.State.AvailableTactics.First();
        engine = Step(engine, e => e.SelectTactic(tactic), "select real-data tactic", definitions);

        engine.State.Players[0].MovementRemaining = 2;
        Assert.Equal(GamePhase.Movement, engine.State.Phase);
        Assert.NotEmpty(engine.GetValidMoves(2));
    }

    [Fact]
    public async Task SoloPlaythrough_TraversesCoreGameLoop()
    {
        var userId = Guid.NewGuid();
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Playthrough",
            ScenarioId = "full_conquest",
            Status = GameStatus.InProgress,
            Players = new List<GamePlayer>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    HeroId = "tovak",
                    IsReady = true,
                    TurnOrder = 0
                }
            }
        };

        var scenario = (await _definitions.GetScenarioAsync("full_conquest"))!;
        var state = await new GameStateInitializer(_definitions).InitializeAsync(game, scenario);

        // Deterministic fixture: a nearby keep guarded by an orc, enough cards to move and attack,
        // and a small unit/advanced action deck so post-combat offers can refill.
        var player = state.Players[0];
        player.Hand = new List<string> { "basic_move", "basic_attack", "basic_move" };
        player.DeedDeck = new List<string> { "basic_move", "basic_attack" };
        player.DiscardPile.Clear();
        state.Decks.RegularUnits = new List<string> { "unit_peasants", "unit_herbalists", "unit_swordsmen", "unit_guardsmen" };
        state.Decks.AdvancedActions = new List<string> { "aa_swiftness", "aa_concentration", "aa_march" };
        state.Offers.RegularUnits.Clear();
        state.Offers.AdvancedActions.Clear();
        state.Map.HexData["-1,0"].Terrain = "Plains";
        state.Map.HexData["-1,0"].SiteType = "Keep";
        state.Map.HexData["-1,0"].Enemies = new List<string> { "enemy_orc" };
        state.ManaSource = new List<ManaDieState>
        {
            new() { Color = ManaColor.Red },
            new() { Color = ManaColor.Blue },
            new() { Color = ManaColor.Green }
        };
        state.ManaPool = state.ManaSource.Select(d => d.Color).ToList();

        var engine = Load(state, _definitions);

        Assert.Equal(GamePhase.TacticsSelection, engine.State.Phase);

        engine = Step(engine, e => e.SelectTactic("tactic_1"), "select tactic");
        Assert.Equal(GamePhase.Movement, engine.State.Phase);

        engine = Step(engine, e => e.UseMana(0), "use source mana");
        Assert.Equal(ManaColor.Red, engine.State.Players[0].TemporaryMana);

        engine = Step(engine, e => e.PlayCard("basic_move"), "play movement card");
        engine = Step(engine, e => e.MovePlayer(new HexPosition { Q = -1, R = 0 }), "move to guarded keep");
        Assert.Equal(GamePhase.Combat, engine.State.Phase);
        Assert.NotNull(engine.State.Combat);
        Assert.Equal(CombatPhase.RangedAttack, engine.State.Combat!.Phase);

        engine = Step(engine, e => e.EndCombatPhase(), "skip ranged phase");
        Assert.Equal(CombatPhase.Block, engine.State.Combat!.Phase);

        engine = Step(engine, e => e.EndCombatPhase(), "take unblocked attack");
        Assert.Equal(CombatPhase.AssignDamage, engine.State.Combat!.Phase);
        Assert.Equal(3, engine.State.Combat.TotalUnblockedDamage);

        engine = Step(engine, e => e.EndCombatPhase(), "assign hero damage");
        Assert.Equal(CombatPhase.Attack, engine.State.Combat!.Phase);
        Assert.Equal(2, engine.State.Players[0].Hand.Count(IsWound));

        engine = Step(engine, e => e.PlayCard("basic_attack"), "play attack card");
        engine = Step(engine, e => e.UseCardSideways("basic_move", "attack"), "play sideways attack");
        Assert.Equal(3, engine.State.Players[0].AttackPool);

        engine = Step(engine, e => e.AttackEnemy(0, 3), "defeat enemy");
        engine = Step(engine, e => e.EndCombatPhase(), "resolve combat victory");
        Assert.Null(engine.State.Combat);
        Assert.True(engine.State.Map.HexData["-1,0"].IsConquered);
        Assert.Empty(engine.State.Map.HexData["-1,0"].Enemies);
        Assert.Equal(GamePhase.Movement, engine.State.Phase);

        engine.State.Players[0].InfluencePool = 5;
        engine = Step(engine, e => e.RecruitUnit("unit_peasants"), "recruit at conquered keep");
        Assert.Single(engine.State.Players[0].Units);

        engine = Step(engine, e => e.EndTurn(), "end turn cleanup");
        Assert.Null(engine.State.Players[0].TemporaryMana);
        Assert.Null(engine.State.TurnState.UsedSourceDieIndex);

        engine.State.Players[0].Hand.Clear();
        engine.State.Players[0].DeedDeck.Clear();
        engine = Step(engine, e => e.AnnounceEndOfRound(), "announce end of round");

        Assert.Equal(2, engine.State.Round);
        Assert.False(engine.State.IsDay);
        Assert.Equal(GamePhase.TacticsSelection, engine.State.Phase);
    }

    private static GameEngine Load(GameStateModel state, IGameDefinitionService definitions)
    {
        var engine = new GameEngine(definitions);
        engine.LoadState(System.Text.Json.JsonSerializer.Serialize(state));
        return engine;
    }

    private GameEngine Reload(GameEngine engine, IGameDefinitionService? definitions = null)
    {
        var reloaded = new GameEngine(definitions ?? _definitions);
        reloaded.LoadState(engine.SaveState());
        return reloaded;
    }

    private GameEngine Step(GameEngine engine, Func<GameEngine, GameActionResult> action, string stepName, IGameDefinitionService? definitions = null)
    {
        var result = action(engine);
        Assert.True(result.Success, $"{stepName} failed: {result.ErrorMessage}");
        return Reload(engine, definitions);
    }

    private static bool IsWound(string cardId) => cardId.StartsWith("wound", StringComparison.OrdinalIgnoreCase);

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "spec")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
