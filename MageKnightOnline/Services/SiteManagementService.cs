using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class SiteManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SiteManagementService> _logger;

    public SiteManagementService(ApplicationDbContext context, ILogger<SiteManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SiteConquestResult> AttemptSiteConquestAsync(int siteId, int playerId, int attackPower)
    {
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == siteId);

        if (site == null)
            throw new ArgumentException("Site not found");

        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            throw new ArgumentException("Player not found");

        var isSuccessful = attackPower >= site.AttackCost;
        var damageDealt = isSuccessful ? attackPower - site.AttackCost : 0;
        var damageTaken = isSuccessful ? 0 : site.AttackCost - attackPower;

        var result = new SiteConquestResult
        {
            SiteId = siteId,
            PlayerId = playerId,
            AttackPower = attackPower,
            IsSuccessful = isSuccessful,
            DamageDealt = damageDealt,
            DamageTaken = damageTaken,
            FameReward = isSuccessful ? site.FameReward : 0,
            ReputationReward = isSuccessful ? site.ReputationReward : 0,
            CrystalsReward = isSuccessful ? site.CrystalsReward : 0,
            UnitReward = isSuccessful ? site.UnitReward : null,
            SpellReward = isSuccessful ? site.SpellReward : null,
            ArtifactReward = isSuccessful ? site.ArtifactReward : null,
            AttemptedAt = DateTime.UtcNow
        };

        if (isSuccessful)
        {
            // Mark site as conquered
            site.IsConquered = true;
            site.ConqueredAt = DateTime.UtcNow;
            site.ConqueredByPlayerId = playerId;

            // Apply rewards to player
            player.Fame += site.FameReward;
            player.Reputation += site.ReputationReward;
            player.Crystals += site.CrystalsReward;
            player.SitesConquered++;

            // Apply damage to player if any
            if (damageTaken > 0)
            {
                player.CurrentHealth -= damageTaken;
                if (player.CurrentHealth < 0)
                    player.CurrentHealth = 0;
            }
        }
        else
        {
            // Apply damage to player for failed attempt
            player.CurrentHealth -= damageTaken;
            if (player.CurrentHealth < 0)
                player.CurrentHealth = 0;
        }

        _context.SiteConquestResults.Add(result);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Site conquest attempt: Site {SiteId}, Player {PlayerId}, Success: {Success}",
            siteId, playerId, isSuccessful);

        return result;
    }

    public async Task<List<Site>> GetAvailableSitesAsync(int gameSessionId)
    {
        return await _context.Sites
            .Where(s => s.GameBoard.GameSessionId == gameSessionId && !s.IsConquered)
            .ToListAsync();
    }

    public async Task<List<Site>> GetConqueredSitesAsync(int gameSessionId)
    {
        return await _context.Sites
            .Where(s => s.GameBoard.GameSessionId == gameSessionId && s.IsConquered)
            .ToListAsync();
    }

    public async Task<List<SiteConquestResult>> GetConquestHistoryAsync(int playerId)
    {
        return await _context.SiteConquestResults
            .Where(sr => sr.PlayerId == playerId)
            .OrderByDescending(sr => sr.AttemptedAt)
            .ToListAsync();
    }
}