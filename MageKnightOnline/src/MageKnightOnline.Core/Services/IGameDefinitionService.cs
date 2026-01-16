using MageKnightOnline.Core.Definitions;

namespace MageKnightOnline.Core.Services;

public interface IGameDefinitionService
{
    Task<IReadOnlyList<HeroDefinition>> GetHeroesAsync();
    Task<HeroDefinition?> GetHeroAsync(string heroId);
    
    Task<IReadOnlyList<ScenarioDefinition>> GetScenariosAsync();
    Task<ScenarioDefinition?> GetScenarioAsync(string scenarioId);
    
    Task<IReadOnlyList<CardDefinition>> GetBasicActionsAsync();
    Task<IReadOnlyList<CardDefinition>> GetAdvancedActionsAsync();
    Task<IReadOnlyList<CardDefinition>> GetSpellsAsync();
    Task<IReadOnlyList<CardDefinition>> GetArtifactsAsync();
    
    Task<IReadOnlyList<SkillDefinition>> GetSkillsAsync();
    Task<IReadOnlyList<SkillDefinition>> GetSkillsForHeroAsync(string heroName);
    
    Task<IReadOnlyList<UnitDefinition>> GetUnitsAsync();
    Task<IReadOnlyList<UnitDefinition>> GetRegularUnitsAsync();
    Task<IReadOnlyList<UnitDefinition>> GetEliteUnitsAsync();
    
    Task<IReadOnlyList<EnemyDefinition>> GetEnemiesAsync();
    Task<IReadOnlyList<EnemyDefinition>> GetEnemiesByTypeAsync(string type);
    
    Task<IReadOnlyList<TacticsDefinition>> GetTacticsAsync();
    Task<IReadOnlyList<TacticsDefinition>> GetDayTacticsAsync();
    Task<IReadOnlyList<TacticsDefinition>> GetNightTacticsAsync();
    
    Task<IReadOnlyList<MapTileDefinition>> GetMapTilesAsync();
}
