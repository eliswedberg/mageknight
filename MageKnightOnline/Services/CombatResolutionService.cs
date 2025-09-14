using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class CombatResolutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CombatResolutionService> _logger;

    public CombatResolutionService(ApplicationDbContext context, ILogger<CombatResolutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CombatResult> ResolveCombatAsync(int combatId)
    {
        var combat = await _context.Combats
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == combatId);

        if (combat == null)
            throw new ArgumentException("Combat not found");

        // Get combat participants
        var participants = combat.Participants.ToList();
        var attacker = participants.FirstOrDefault(p => p.PlayerId.HasValue);
        var defender = participants.FirstOrDefault(p => p.EnemyId.HasValue);

        if (attacker == null || defender == null)
            throw new InvalidOperationException("Combat participants not found");

        var result = new CombatResult
        {
            CombatId = combatId,
            AttackerId = attacker.PlayerId ?? 0,
            DefenderId = defender.EnemyId ?? 0,
            ResolvedAt = DateTime.UtcNow
        };

        // Calculate total attack and defense values
        var attackerTotal = CalculateTotalAttack(attacker);
        var defenderTotal = CalculateTotalDefense(defender);

        result.AttackerTotal = attackerTotal;
        result.DefenderTotal = defenderTotal;

        // Determine combat outcome
        if (attackerTotal > defenderTotal)
        {
            result.Winner = attacker.PlayerId;
            result.DamageDealt = attackerTotal - defenderTotal;
            result.IsVictory = true;
        }
        else if (defenderTotal > attackerTotal)
        {
            result.Winner = defender.EnemyId;
            result.DamageDealt = defenderTotal - attackerTotal;
            result.IsVictory = false;
        }
        else
        {
            result.Winner = null; // Draw
            result.DamageDealt = 0;
            result.IsVictory = false;
        }

        // Apply damage and effects
        await ApplyCombatResultsAsync(combat, result);

        // Save combat result
        _context.CombatResults.Add(result);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Combat {CombatId} resolved: Attacker {AttackerTotal} vs Defender {DefenderTotal}, Winner: {Winner}",
            combatId, attackerTotal, defenderTotal, result.Winner);

        return result;
    }

    private int CalculateTotalAttack(CombatParticipant attacker)
    {
        return attacker.AttackValue;
    }

    private int CalculateTotalDefense(CombatParticipant defender)
    {
        return defender.BlockValue;
    }

    private async Task ApplyCombatResultsAsync(Combat combat, CombatResult result)
    {
        if (result.Winner == null) return; // Draw, no effects

        var participants = combat.Participants.ToList();
        var attacker = participants.FirstOrDefault(p => p.PlayerId.HasValue);
        var defender = participants.FirstOrDefault(p => p.EnemyId.HasValue);

        if (attacker == null || defender == null) return;

        // Apply damage to loser
        if (result.Winner == attacker.PlayerId)
        {
            defender.CurrentHealth -= result.DamageDealt;
            if (defender.CurrentHealth <= 0)
            {
                defender.CurrentHealth = 0;
                defender.IsDefeated = true;
            }
        }
        else
        {
            attacker.CurrentHealth -= result.DamageDealt;
            if (attacker.CurrentHealth <= 0)
            {
                attacker.CurrentHealth = 0;
                attacker.IsDefeated = true;
            }
        }

        // Update combat status
        combat.Status = CombatStatus.Completed;
        combat.EndedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<List<CombatResult>> GetCombatHistoryAsync(int gameSessionId)
    {
        return await _context.CombatResults
            .Include(cr => cr.Combat)
            .Where(cr => cr.Combat.GameSessionId == gameSessionId)
            .OrderByDescending(cr => cr.ResolvedAt)
            .ToListAsync();
    }

    public async Task<CombatResult> GetCombatResultAsync(int combatId)
    {
        return await _context.CombatResults
            .Include(cr => cr.Combat)
            .FirstOrDefaultAsync(cr => cr.CombatId == combatId);
    }
}