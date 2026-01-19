using MageKnightOnline.Core.Definitions;

namespace MageKnightOnline.Core.Services;

public interface IGameDefinitionService
{
    // Heroes
    Task<IReadOnlyList<HeroDefinition>> GetHeroesAsync();
    Task<HeroDefinition?> GetHeroAsync(string heroId);
    
    // Scenarios
    Task<IReadOnlyList<ScenarioDefinition>> GetScenariosAsync();
    Task<ScenarioDefinition?> GetScenarioAsync(string scenarioId);
    
    // Cards
    Task<IReadOnlyList<CardDefinition>> GetBasicActionsAsync();
    Task<IReadOnlyList<CardDefinition>> GetAdvancedActionsAsync();
    Task<IReadOnlyList<CardDefinition>> GetSpellsAsync();
    Task<IReadOnlyList<CardDefinition>> GetArtifactsAsync();
    
    // Skills
    Task<IReadOnlyList<SkillDefinition>> GetSkillsAsync();
    Task<IReadOnlyList<SkillDefinition>> GetSkillsForHeroAsync(string heroName);
    
    // Units
    Task<IReadOnlyList<UnitDefinition>> GetUnitsAsync();
    Task<IReadOnlyList<UnitDefinition>> GetRegularUnitsAsync();
    Task<IReadOnlyList<UnitDefinition>> GetEliteUnitsAsync();
    
    // Enemies
    Task<IReadOnlyList<EnemyDefinition>> GetEnemiesAsync();
    Task<IReadOnlyList<EnemyDefinition>> GetEnemiesByTypeAsync(string type);
    Task<EnemyDefinition?> GetEnemyAsync(string enemyId);
    
    // Tactics
    Task<IReadOnlyList<TacticsDefinition>> GetTacticsAsync();
    Task<IReadOnlyList<TacticsDefinition>> GetDayTacticsAsync();
    Task<IReadOnlyList<TacticsDefinition>> GetNightTacticsAsync();
    
    // Map Tiles
    Task<IReadOnlyList<MapTileDefinition>> GetMapTilesAsync();
    
    // Ruins Tokens
    Task<IReadOnlyList<RuinsDefinition>> GetRuinsTokensAsync();
    Task<IReadOnlyList<RuinsDefinition>> GetRuinsLootTokensAsync();
    Task<IReadOnlyList<RuinsDefinition>> GetRuinsCombatTokensAsync();
    
    // Terrain (NEW)
    Task<IReadOnlyList<TerrainDefinition>> GetTerrainCostsAsync();
    Task<TerrainDefinition?> GetTerrainAsync(string terrainType);
    Task<int> GetTerrainCostAsync(string terrainType, bool isDay);
    
    // Sites (NEW)
    Task<IReadOnlyList<SiteDefinition>> GetSitesAsync();
    Task<SiteDefinition?> GetSiteAsync(string siteId);
    Task<IReadOnlyList<SiteDefinition>> GetSitesByTypeAsync(string siteType);
    
    // Combat Abilities (NEW)
    Task<CombatAbilitiesRoot> GetCombatAbilitiesAsync();
    Task<CombatAbilityDefinition?> GetCombatAbilityAsync(string abilityId);
    
    // Game Rules (NEW)
    Task<GameRulesDefinition> GetGameRulesAsync();
}
