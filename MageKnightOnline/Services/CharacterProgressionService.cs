using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class CharacterProgressionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CharacterProgressionService> _logger;

    public CharacterProgressionService(ApplicationDbContext context, ILogger<CharacterProgressionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LevelUpResult> ProcessLevelUpAsync(int playerId)
    {
        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            throw new ArgumentException("Player not found");

        var oldLevel = player.Level;
        var experienceRequired = CalculateExperienceRequired(player.Level + 1);

        if (player.Experience < experienceRequired)
            throw new InvalidOperationException("Not enough experience to level up");

        // Level up the player
        player.Level++;
        player.Experience -= experienceRequired;
        player.ExperienceToNextLevel = CalculateExperienceRequired(player.Level + 1) - player.Experience;

        // Apply level up benefits
        var healthIncrease = CalculateHealthIncrease(player.Level);
        var manaIncrease = CalculateManaIncrease(player.Level);
        var fameIncrease = CalculateFameIncrease(player.Level);

        player.MaxHealth += healthIncrease;
        player.CurrentHealth += healthIncrease;
        player.MaxMana += manaIncrease;
        player.CurrentMana += manaIncrease;
        player.Fame += fameIncrease;

        // Create level up result
        var result = new LevelUpResult
        {
            PlayerId = playerId,
            OldLevel = oldLevel,
            NewLevel = player.Level,
            HealthIncrease = healthIncrease,
            ManaIncrease = manaIncrease,
            FameIncrease = fameIncrease,
            LeveledUpAt = DateTime.UtcNow
        };

        _context.LevelUpResults.Add(result);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} leveled up from {OldLevel} to {NewLevel}",
            playerId, oldLevel, player.Level);

        return result;
    }

    public async Task<PlayerStats> GetPlayerStatsAsync(int playerId)
    {
        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            throw new ArgumentException("Player not found");

        return new PlayerStats
        {
            PlayerId = playerId,
            Level = player.Level,
            Experience = player.Experience,
            Health = player.CurrentHealth,
            Mana = player.CurrentMana,
            Fame = player.Fame,
            Reputation = player.Reputation,
            ExperienceToNextLevel = CalculateExperienceRequired(player.Level + 1) - player.Experience
        };
    }

    public async Task<bool> CanLevelUpAsync(int playerId)
    {
        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            return false;

        var experienceRequired = CalculateExperienceRequired(player.Level + 1);
        return player.Experience >= experienceRequired;
    }

    public async Task<List<MageKnightCard>> GetAvailableSpellsAsync(int playerId)
    {
        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            return new List<MageKnightCard>();

        return await _context.MageKnightCards
            .Where(c => c.CardSubType == CardSubType.Spell && c.Cost <= player.Level)
            .ToListAsync();
    }

    public async Task<List<LevelUpResult>> GetLevelUpHistoryAsync(int playerId)
    {
        return await _context.LevelUpResults
            .Where(lr => lr.PlayerId == playerId)
            .OrderByDescending(lr => lr.LeveledUpAt)
            .ToListAsync();
    }

    private int CalculateExperienceRequired(int level)
    {
        // Exponential growth: 100 * level^2
        return 100 * level * level;
    }

    private int CalculateHealthIncrease(int level)
    {
        // +2 health per level
        return 2;
    }

    private int CalculateManaIncrease(int level)
    {
        // +1 mana per level
        return 1;
    }

    private int CalculateFameIncrease(int level)
    {
        // +5 fame per level
        return 5;
    }
}