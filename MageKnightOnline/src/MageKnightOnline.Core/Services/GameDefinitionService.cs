using System.Text.Json;
using MageKnightOnline.Core.Definitions;

namespace MageKnightOnline.Core.Services;

public class GameDefinitionService : IGameDefinitionService
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cached data
    private List<HeroDefinition>? _heroes;
    private List<ScenarioDefinition>? _scenarios;
    private List<CardDefinition>? _basicActions;
    private List<CardDefinition>? _advancedActions;
    private List<CardDefinition>? _spells;
    private List<CardDefinition>? _artifacts;
    private List<SkillDefinition>? _skills;
    private List<UnitDefinition>? _units;
    private List<EnemyDefinition>? _enemies;
    private List<TacticsDefinition>? _tactics;
    private List<MapTileDefinition>? _mapTiles;
    private List<RuinsDefinition>? _ruins;

    public GameDefinitionService(string basePath)
    {
        _basePath = basePath;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    private async Task<List<T>> LoadJsonAsync<T>(string fileName)
    {
        var filePath = Path.Combine(_basePath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Definition file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
    }

    // Heroes
    public async Task<IReadOnlyList<HeroDefinition>> GetHeroesAsync()
    {
        _heroes ??= await LoadJsonAsync<HeroDefinition>("heroes.json");
        return _heroes.AsReadOnly();
    }

    public async Task<HeroDefinition?> GetHeroAsync(string heroId)
    {
        var heroes = await GetHeroesAsync();
        return heroes.FirstOrDefault(h => h.Id == heroId);
    }

    // Scenarios
    public async Task<IReadOnlyList<ScenarioDefinition>> GetScenariosAsync()
    {
        _scenarios ??= await LoadJsonAsync<ScenarioDefinition>("scenarios.json");
        return _scenarios.AsReadOnly();
    }

    public async Task<ScenarioDefinition?> GetScenarioAsync(string scenarioId)
    {
        var scenarios = await GetScenariosAsync();
        return scenarios.FirstOrDefault(s => s.Id == scenarioId);
    }

    // Cards
    public async Task<IReadOnlyList<CardDefinition>> GetBasicActionsAsync()
    {
        _basicActions ??= await LoadJsonAsync<CardDefinition>("basic_actions.json");
        return _basicActions.AsReadOnly();
    }

    public async Task<IReadOnlyList<CardDefinition>> GetAdvancedActionsAsync()
    {
        _advancedActions ??= await LoadJsonAsync<CardDefinition>("advanced_actions.json");
        return _advancedActions.AsReadOnly();
    }

    public async Task<IReadOnlyList<CardDefinition>> GetSpellsAsync()
    {
        _spells ??= await LoadJsonAsync<CardDefinition>("spells.json");
        return _spells.AsReadOnly();
    }

    public async Task<IReadOnlyList<CardDefinition>> GetArtifactsAsync()
    {
        _artifacts ??= await LoadJsonAsync<CardDefinition>("artifacts.json");
        return _artifacts.AsReadOnly();
    }

    // Skills
    public async Task<IReadOnlyList<SkillDefinition>> GetSkillsAsync()
    {
        _skills ??= await LoadJsonAsync<SkillDefinition>("hero_skills.json");
        return _skills.AsReadOnly();
    }

    public async Task<IReadOnlyList<SkillDefinition>> GetSkillsForHeroAsync(string heroName)
    {
        var skills = await GetSkillsAsync();
        return skills.Where(s => s.Hero.Equals(heroName, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    // Units
    public async Task<IReadOnlyList<UnitDefinition>> GetUnitsAsync()
    {
        _units ??= await LoadJsonAsync<UnitDefinition>("units.json");
        return _units.AsReadOnly();
    }

    public async Task<IReadOnlyList<UnitDefinition>> GetRegularUnitsAsync()
    {
        var units = await GetUnitsAsync();
        return units.Where(u => u.Rank == "Regular").ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<UnitDefinition>> GetEliteUnitsAsync()
    {
        var units = await GetUnitsAsync();
        return units.Where(u => u.Rank == "Elite").ToList().AsReadOnly();
    }

    // Enemies
    public async Task<IReadOnlyList<EnemyDefinition>> GetEnemiesAsync()
    {
        _enemies ??= await LoadJsonAsync<EnemyDefinition>("enemies.json");
        return _enemies.AsReadOnly();
    }

    public async Task<IReadOnlyList<EnemyDefinition>> GetEnemiesByTypeAsync(string type)
    {
        var enemies = await GetEnemiesAsync();
        return enemies.Where(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    // Tactics
    public async Task<IReadOnlyList<TacticsDefinition>> GetTacticsAsync()
    {
        _tactics ??= await LoadJsonAsync<TacticsDefinition>("tactics.json");
        return _tactics.AsReadOnly();
    }

    public async Task<IReadOnlyList<TacticsDefinition>> GetDayTacticsAsync()
    {
        var tactics = await GetTacticsAsync();
        return tactics.Where(t => t.Period == "Day").ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<TacticsDefinition>> GetNightTacticsAsync()
    {
        var tactics = await GetTacticsAsync();
        return tactics.Where(t => t.Period == "Night").ToList().AsReadOnly();
    }

    // Map Tiles
    public async Task<IReadOnlyList<MapTileDefinition>> GetMapTilesAsync()
    {
        _mapTiles ??= await LoadJsonAsync<MapTileDefinition>("map_tiles.json");
        return _mapTiles.AsReadOnly();
    }

    // Ruins Tokens
    public async Task<IReadOnlyList<RuinsDefinition>> GetRuinsTokensAsync()
    {
        _ruins ??= await LoadJsonAsync<RuinsDefinition>("ruins.json");
        return _ruins.AsReadOnly();
    }

    public async Task<IReadOnlyList<RuinsDefinition>> GetRuinsLootTokensAsync()
    {
        var ruins = await GetRuinsTokensAsync();
        return ruins.Where(r => r.IsLootToken).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RuinsDefinition>> GetRuinsCombatTokensAsync()
    {
        var ruins = await GetRuinsTokensAsync();
        return ruins.Where(r => r.IsCombatToken).ToList().AsReadOnly();
    }
}
