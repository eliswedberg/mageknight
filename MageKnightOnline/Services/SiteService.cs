using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MageKnightOnline.Services;

/// <summary>
/// Service for managing sites according to sites.json schema
/// </summary>
public class SiteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SiteService> _logger;
    private readonly CombatService _combatService;

    public SiteService(ApplicationDbContext context, ILogger<SiteService> logger, CombatService combatService)
    {
        _context = context;
        _logger = logger;
        _combatService = combatService;
    }

    /// <summary>
    /// Create a site from sites.json data
    /// </summary>
    public async Task<Site?> CreateSiteAsync(int gameSessionId, string siteId, int? hexSpaceId = null)
    {
        try
        {
            // Load site template from sites.json
            var siteTemplate = await LoadSiteTemplateAsync(siteId);
            if (siteTemplate == null)
            {
                _logger.LogWarning("Site template not found for siteId: {SiteId}", siteId);
                return null;
            }

            var site = new Site
            {
                SiteId = siteId,
                Type = ParseSiteType(siteTemplate.Type),
                Color = siteTemplate.Color,
                IsFortified = siteTemplate.Fortified ?? false,
                EnteringAssaults = siteTemplate.EnteringAssaults ?? false,
                WhenRevealed = JsonSerializer.Serialize(siteTemplate.WhenRevealed ?? new List<SiteEffect>()),
                InteractOptions = JsonSerializer.Serialize(siteTemplate.InteractOptions ?? new List<SiteInteraction>()),
                InteractConquered = JsonSerializer.Serialize(siteTemplate.InteractConquered ?? new List<SiteInteraction>()),
                Defenders = JsonSerializer.Serialize(siteTemplate.Defenders ?? new List<object>()),
                Rewards = JsonSerializer.Serialize(siteTemplate.Rewards ?? new List<object>()),
                Burn = siteTemplate.Burn != null ? JsonSerializer.Serialize(siteTemplate.Burn) : null,
                GameSessionId = gameSessionId,
                HexSpaceId = hexSpaceId,
                Name = GetSiteDisplayName(siteId, siteTemplate.Type),
                Description = GetSiteDescription(siteId, siteTemplate.Type)
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created site {SiteId} of type {Type} for game session {GameSessionId}", 
                siteId, siteTemplate.Type, gameSessionId);

            return site;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site {SiteId} for game session {GameSessionId}", siteId, gameSessionId);
            return null;
        }
    }

    /// <summary>
    /// Reveal a site and trigger when_revealed effects
    /// </summary>
    public async Task<bool> RevealSiteAsync(int siteId, int playerId, int turnNumber)
    {
        try
        {
            var site = await _context.Sites
                .Include(s => s.GameSession)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || site.IsRevealed)
            {
                return false;
            }

            site.IsRevealed = true;
            _context.Sites.Update(site);

            // Trigger when_revealed effects
            await TriggerWhenRevealedEffectsAsync(site, playerId, turnNumber);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Revealed site {SiteId} for player {PlayerId}", siteId, playerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revealing site {SiteId}", siteId);
            return false;
        }
    }

    /// <summary>
    /// Interact with a site (heal, recruit, learn, etc.)
    /// </summary>
    public async Task<bool> InteractWithSiteAsync(int siteId, int playerId, string interactionType, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var site = await _context.Sites
                .Include(s => s.GameSession)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null)
            {
                return false;
            }

            // Check if interaction is available
            var availableInteractions = JsonSerializer.Deserialize<List<SiteInteraction>>(site.InteractOptions);
            var conqueredInteractions = JsonSerializer.Deserialize<List<SiteInteraction>>(site.InteractConquered);

            var allInteractions = new List<SiteInteraction>();
            allInteractions.AddRange(availableInteractions ?? new List<SiteInteraction>());
            
            if (site.IsConquered)
            {
                allInteractions.AddRange(conqueredInteractions ?? new List<SiteInteraction>());
            }

            var interaction = allInteractions.FirstOrDefault(i => i.Type == interactionType);
            if (interaction == null)
            {
                _logger.LogWarning("Interaction {InteractionType} not available at site {SiteId}", interactionType, siteId);
                return false;
            }

            // Process the interaction
            var success = await ProcessSiteInteractionAsync(site, playerId, interaction, parameters);
            
            if (success)
            {
                _logger.LogInformation("Player {PlayerId} performed {InteractionType} at site {SiteId}", 
                    playerId, interactionType, siteId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interacting with site {SiteId}", siteId);
            return false;
        }
    }

    /// <summary>
    /// Conquer a site through combat
    /// </summary>
    public async Task<bool> ConquerSiteAsync(int siteId, int playerId, int turnNumber)
    {
        try
        {
            var site = await _context.Sites
                .Include(s => s.GameSession)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || site.IsConquered)
            {
                return false;
            }

            site.IsConquered = true;
            site.ConqueredByPlayerId = playerId;
            site.ConqueredAt = DateTime.UtcNow;
            site.ConqueredOnTurn = turnNumber;

            _context.Sites.Update(site);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Site {SiteId} conquered by player {PlayerId} on turn {TurnNumber}", 
                siteId, playerId, turnNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error conquering site {SiteId}", siteId);
            return false;
        }
    }

    /// <summary>
    /// Burn a site (for monasteries, etc.)
    /// </summary>
    public async Task<bool> BurnSiteAsync(int siteId, int playerId, int turnNumber)
    {
        try
        {
            var site = await _context.Sites
                .Include(s => s.GameSession)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || site.IsBurned)
            {
                return false;
            }

            site.IsBurned = true;
            site.BurnedByPlayerId = playerId;
            site.BurnedAt = DateTime.UtcNow;

            _context.Sites.Update(site);
            await _context.SaveChangesAsync();

            // Apply burn effects (reputation loss, etc.)
            await ApplyBurnEffectsAsync(site, playerId, turnNumber);

            _logger.LogInformation("Site {SiteId} burned by player {PlayerId} on turn {TurnNumber}", 
                siteId, playerId, turnNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error burning site {SiteId}", siteId);
            return false;
        }
    }

    /// <summary>
    /// Get all sites in a game session
    /// </summary>
    public async Task<List<Site>> GetSitesAsync(int gameSessionId)
    {
        return await _context.Sites
            .Where(s => s.GameSessionId == gameSessionId)
            .ToListAsync();
    }

    /// <summary>
    /// Get sites at a specific hex location
    /// </summary>
    public async Task<List<Site>> GetSitesAtHexAsync(int gameSessionId, int q, int r)
    {
        return await _context.Sites
            .Include(s => s.HexSpace)
            .Where(s => s.GameSessionId == gameSessionId && 
                       s.HexSpace != null && 
                       s.HexSpace.Q == q && 
                       s.HexSpace.R == r)
            .ToListAsync();
    }

    #region Private Helper Methods

    private async Task<SiteItem?> LoadSiteTemplateAsync(string siteId)
    {
        try
        {
            var sitesJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sites.json");
            if (!File.Exists(sitesJsonPath))
            {
                _logger.LogError("Sites.json file not found at {Path}", sitesJsonPath);
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(sitesJsonPath);
            var sitesData = JsonSerializer.Deserialize<SitesData>(jsonContent);

            return sitesData?.Items?.FirstOrDefault(item => item.Id == siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading site template for {SiteId}", siteId);
            return null;
        }
    }

    private SiteType ParseSiteType(string typeString)
    {
        return typeString.ToLower() switch
        {
            "village" => SiteType.Village,
            "monastery" => SiteType.Monastery,
            "keep" => SiteType.Keep,
            "mage_tower" => SiteType.MageTower,
            "city" => SiteType.City,
            "ruins" => SiteType.Ruins,
            "dungeon" => SiteType.Dungeon,
            "tomb" => SiteType.Tomb,
            "monster_den" => SiteType.MonsterDen,
            "spawning_grounds" => SiteType.SpawningGrounds,
            "portal" => SiteType.Portal,
            _ => SiteType.Village
        };
    }

    private string GetSiteDisplayName(string siteId, string type)
    {
        return type.ToLower() switch
        {
            "village" => "Village",
            "monastery" => "Monastery",
            "keep" => "Keep",
            "mage_tower" => "Mage Tower",
            "city" => siteId.Contains("red") ? "Red City" : 
                     siteId.Contains("blue") ? "Blue City" : 
                     siteId.Contains("white") ? "White City" : 
                     siteId.Contains("green") ? "Green City" : "City",
            "ruins" => "Ruins",
            "dungeon" => "Dungeon",
            "tomb" => "Tomb",
            "monster_den" => "Monster Den",
            "spawning_grounds" => "Spawning Grounds",
            "portal" => "Portal",
            _ => siteId
        };
    }

    private string GetSiteDescription(string siteId, string type)
    {
        return type.ToLower() switch
        {
            "village" => "A peaceful village where you can heal and recruit units.",
            "monastery" => "A monastery where you can learn advanced actions and heal.",
            "keep" => "A fortified keep that triggers assaults when entered.",
            "mage_tower" => "A mage tower where you can learn spells after conquering.",
            "city" => "A powerful city with unique rewards for conquest.",
            "ruins" => "Ancient ruins that may contain valuable artifacts.",
            "dungeon" => "A dangerous dungeon filled with monsters and treasure.",
            "tomb" => "An ancient tomb with undead guardians.",
            "monster_den" => "A den where dangerous monsters spawn.",
            "spawning_grounds" => "Grounds where powerful creatures are born.",
            "portal" => "A mystical portal to other realms.",
            _ => $"A {type} site."
        };
    }

    private async Task TriggerWhenRevealedEffectsAsync(Site site, int playerId, int turnNumber)
    {
        try
        {
            var whenRevealedEffects = JsonSerializer.Deserialize<List<SiteEffect>>(site.WhenRevealed);
            if (whenRevealedEffects == null) return;

            foreach (var effect in whenRevealedEffects)
            {
                switch (effect.Type)
                {
                    case "add_advanced_action_to_unit_offer":
                        // TODO: Implement unit offer system
                        _logger.LogInformation("Triggered add_advanced_action_to_unit_offer for site {SiteId}", site.Id);
                        break;
                    default:
                        _logger.LogWarning("Unknown when_revealed effect type: {EffectType}", effect.Type);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering when_revealed effects for site {SiteId}", site.Id);
        }
    }

    private async Task<bool> ProcessSiteInteractionAsync(Site site, int playerId, SiteInteraction interaction, Dictionary<string, object>? parameters)
    {
        try
        {
            switch (interaction.Type)
            {
                case "heal":
                    return await ProcessHealInteractionAsync(site, playerId, interaction);
                case "recruit":
                    return await ProcessRecruitInteractionAsync(site, playerId, interaction);
                case "learn_advanced_action":
                    return await ProcessLearnAdvancedActionInteractionAsync(site, playerId, interaction);
                case "learn_spell":
                    return await ProcessLearnSpellInteractionAsync(site, playerId, interaction);
                case "buy_artifact":
                    return await ProcessBuyArtifactInteractionAsync(site, playerId, interaction);
                case "buy_spell":
                    return await ProcessBuySpellInteractionAsync(site, playerId, interaction);
                case "recruit_all_types":
                    return await ProcessRecruitAllTypesInteractionAsync(site, playerId, interaction);
                case "add_elite_unit_to_offer":
                    return await ProcessAddEliteUnitToOfferInteractionAsync(site, playerId, interaction);
                case "gain_advanced_action":
                    return await ProcessGainAdvancedActionInteractionAsync(site, playerId, interaction);
                default:
                    _logger.LogWarning("Unknown interaction type: {InteractionType}", interaction.Type);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing site interaction {InteractionType} for site {SiteId}", 
                interaction.Type, site.Id);
            return false;
        }
    }

    private async Task<bool> ProcessHealInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement healing logic
        _logger.LogInformation("Processing heal interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessRecruitInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement recruitment logic
        _logger.LogInformation("Processing recruit interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessLearnAdvancedActionInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement advanced action learning logic
        _logger.LogInformation("Processing learn_advanced_action interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessLearnSpellInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement spell learning logic
        _logger.LogInformation("Processing learn_spell interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessBuyArtifactInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement artifact buying logic
        _logger.LogInformation("Processing buy_artifact interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessBuySpellInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement spell buying logic
        _logger.LogInformation("Processing buy_spell interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessRecruitAllTypesInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement recruit all types logic
        _logger.LogInformation("Processing recruit_all_types interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessAddEliteUnitToOfferInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement add elite unit to offer logic
        _logger.LogInformation("Processing add_elite_unit_to_offer interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task<bool> ProcessGainAdvancedActionInteractionAsync(Site site, int playerId, SiteInteraction interaction)
    {
        // TODO: Implement gain advanced action logic
        _logger.LogInformation("Processing gain_advanced_action interaction for player {PlayerId} at site {SiteId}", playerId, site.Id);
        return true;
    }

    private async Task ApplyBurnEffectsAsync(Site site, int playerId, int turnNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(site.Burn))
            {
                return;
            }

            var burnEffects = JsonSerializer.Deserialize<BurnEffect>(site.Burn);
            if (burnEffects == null) return;

            // Apply reputation change
            if (burnEffects.ReputationDelta != 0)
            {
                // TODO: Implement reputation system
                _logger.LogInformation("Applied reputation change {ReputationDelta} to player {PlayerId} for burning site {SiteId}", 
                    burnEffects.ReputationDelta, playerId, site.Id);
            }

            // Spawn defenders if specified
            if (!string.IsNullOrEmpty(burnEffects.DefenderColor))
            {
                // TODO: Implement defender spawning
                _logger.LogInformation("Spawning {DefenderColor} defenders for burned site {SiteId}", 
                    burnEffects.DefenderColor, site.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying burn effects for site {SiteId}", site.Id);
        }
    }

    #endregion
}

