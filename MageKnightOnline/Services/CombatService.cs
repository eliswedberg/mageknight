using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MageKnightOnline.Services;

public class CombatService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CombatService> _logger;

    public CombatService(ApplicationDbContext context, ILogger<CombatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Combat?> StartCombatAsync(int gameSessionId, int attackingPlayerId, int? siteId = null, int? defendingPlayerId = null, CombatType combatType = CombatType.SiteConquest)
    {
        try
        {
            var combat = new Combat
            {
                GameSessionId = gameSessionId,
                AttackingPlayerId = attackingPlayerId,
                SiteId = siteId,
                DefendingPlayerId = defendingPlayerId,
                Type = combatType,
                Status = CombatStatus.Preparing,
                CurrentPhase = CombatPhase.Preparation,
                TurnNumber = 1
            };

            _context.Combats.Add(combat);
            await _context.SaveChangesAsync();

            // Add attacking player as participant
            await AddPlayerParticipantAsync(combat.Id, attackingPlayerId);

            // Add site defenders if attacking a site
            if (siteId.HasValue)
            {
                await AddSiteDefendersAsync(combat.Id, siteId.Value);
            }

            // Add defending player if PvP combat
            if (defendingPlayerId.HasValue)
            {
                await AddPlayerParticipantAsync(combat.Id, defendingPlayerId.Value);
            }

            // Start combat flow
            await StartCombatFlowAsync(combat.Id);

            _logger.LogInformation("Started combat {CombatId} of type {CombatType} in game session {GameSessionId}", 
                combat.Id, combatType, gameSessionId);

            return combat;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting combat for game session {GameSessionId}", gameSessionId);
            return null;
        }
    }

    public async Task<bool> AddPlayerParticipantAsync(int combatId, int playerId)
    {
        try
        {
            var player = await _context.GamePlayers
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null) return false;

            var participant = new CombatParticipant
            {
                CombatId = combatId,
                PlayerId = playerId,
                AttackValue = 0, // Will be calculated from cards/units
                BlockValue = 0,  // Will be calculated from cards/units
                Health = player.CurrentHealth,
                CurrentHealth = player.CurrentHealth,
                Initiative = 0,  // Will be determined by cards
                IsActive = true
            };

            _context.CombatParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding player participant {PlayerId} to combat {CombatId}", playerId, combatId);
            return false;
        }
    }

    public async Task<bool> AddSiteDefendersAsync(int combatId, int siteId)
    {
        try
        {
            var site = await _context.Sites
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null) return false;

            // For now, create basic defenders based on site type
            // In a full implementation, this would read from sites.json and create appropriate enemies
            var defenders = await CreateSiteDefendersAsync(site);

            foreach (var defender in defenders)
            {
                var participant = new CombatParticipant
                {
                    CombatId = combatId,
                    EnemyId = defender.Id,
                    AttackValue = defender.Attack,
                    BlockValue = defender.Armor,
                    Health = defender.Health,
                    CurrentHealth = defender.Health,
                    Initiative = defender.Initiative,
                    IsActive = true
                };

                _context.CombatParticipants.Add(participant);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding site defenders for site {SiteId} to combat {CombatId}", siteId, combatId);
            return false;
        }
    }

    public async Task<bool> StartCombatFlowAsync(int combatId)
    {
        try
        {
            var combat = await _context.Combats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == combatId);

            if (combat == null) return false;

            // Set combat status to in progress
            combat.Status = CombatStatus.InProgress;
            combat.CurrentPhase = CombatPhase.Initiative;

            // Determine initiative order
            await DetermineInitiativeOrderAsync(combatId);

            // Set first participant as current
            var firstParticipant = combat.Participants
                .OrderBy(p => p.CombatOrder)
                .FirstOrDefault();

            if (firstParticipant != null)
            {
                combat.CurrentParticipantId = firstParticipant.Id;
            }

            _context.Combats.Update(combat);
            await _context.SaveChangesAsync();

            // Log combat start
            await LogCombatActionAsync(combatId, 0, CombatActionType.CombatStart, "Combat started", 0);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting combat flow for combat {CombatId}", combatId);
            return false;
        }
    }

    public async Task<bool> DetermineInitiativeOrderAsync(int combatId)
    {
        try
        {
            var participants = await _context.CombatParticipants
                .Where(p => p.CombatId == combatId && p.IsActive)
                .ToListAsync();

            // Sort by initiative (highest first), then by random for ties
            var random = new Random();
            var orderedParticipants = participants
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => random.Next())
                .ToList();

            for (int i = 0; i < orderedParticipants.Count; i++)
            {
                orderedParticipants[i].CombatOrder = i + 1;
            }

            _context.CombatParticipants.UpdateRange(orderedParticipants);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining initiative order for combat {CombatId}", combatId);
            return false;
        }
    }

    public async Task<bool> ProcessCombatActionAsync(int combatId, int participantId, CombatActionType actionType, int value, int? targetId = null, string description = "")
    {
        try
        {
            var combat = await _context.Combats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == combatId);

            if (combat == null || combat.Status != CombatStatus.InProgress) return false;

            var participant = combat.Participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null || !participant.IsActive) return false;

            // Create combat action
            var action = new CombatAction
            {
                CombatId = combatId,
                ParticipantId = participantId,
                ActionType = actionType,
                Value = value,
                TargetId = targetId,
                Description = description,
                TurnNumber = combat.CurrentTurn,
                ActionSequence = combat.Actions.Count + 1
            };

            _context.CombatActions.Add(action);

            // Process the action based on type
            bool actionProcessed = false;
            switch (actionType)
            {
                case CombatActionType.Attack:
                    actionProcessed = await ProcessAttackActionAsync(combat, participant, value, targetId);
                    break;
                case CombatActionType.Block:
                    actionProcessed = await ProcessBlockActionAsync(combat, participant, value);
                    break;
                case CombatActionType.RangedAttack:
                    actionProcessed = await ProcessRangedAttackActionAsync(combat, participant, value, targetId);
                    break;
                case CombatActionType.SiegeAttack:
                    actionProcessed = await ProcessSiegeAttackActionAsync(combat, participant, value, targetId);
                    break;
                default:
                    actionProcessed = true; // For other action types, just log them
                    break;
            }

            if (actionProcessed)
            {
                action.IsResolved = true;
                action.Result = "Success";
            }

            _context.CombatActions.Update(action);
            await _context.SaveChangesAsync();

            // Check if combat should end
            await CheckCombatEndConditionsAsync(combatId);

            return actionProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing combat action for combat {CombatId}", combatId);
            return false;
        }
    }

    private async Task<bool> ProcessAttackActionAsync(Combat combat, CombatParticipant attacker, int attackValue, int? targetId)
    {
        try
        {
            if (!targetId.HasValue) return false;

            var target = combat.Participants.FirstOrDefault(p => p.Id == targetId.Value);
            if (target == null || !target.IsActive) return false;

            // Calculate total attack value
            var totalAttack = attackValue + attacker.AttackValue + attacker.AttackModifier;

            // Calculate total block value
            var totalBlock = target.BlockValue + target.BlockModifier;

            // Determine damage
            var damage = Math.Max(0, totalAttack - totalBlock);

            // Apply damage
            target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);

            // Check if target is defeated
            if (target.CurrentHealth <= 0)
            {
                target.IsDefeated = true;
                target.IsActive = false;
            }

            _context.CombatParticipants.Update(target);

            // Log the attack
            await LogCombatActionAsync(combat.Id, attacker.Id, CombatActionType.Attack, 
                $"Attacked for {totalAttack} vs {totalBlock} block, dealing {damage} damage", damage, targetId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attack action");
            return false;
        }
    }

    private async Task<bool> ProcessBlockActionAsync(Combat combat, CombatParticipant blocker, int blockValue)
    {
        try
        {
            // Block actions are typically used to increase block value for the turn
            blocker.BlockModifier += blockValue;
            _context.CombatParticipants.Update(blocker);

            await LogCombatActionAsync(combat.Id, blocker.Id, CombatActionType.Block, 
                $"Increased block by {blockValue}", blockValue);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing block action");
            return false;
        }
    }

    private async Task<bool> ProcessRangedAttackActionAsync(Combat combat, CombatParticipant attacker, int attackValue, int? targetId)
    {
        // Ranged attacks work similarly to regular attacks but may have different rules
        return await ProcessAttackActionAsync(combat, attacker, attackValue, targetId);
    }

    private async Task<bool> ProcessSiegeAttackActionAsync(Combat combat, CombatParticipant attacker, int attackValue, int? targetId)
    {
        // Siege attacks are effective against fortified positions
        return await ProcessAttackActionAsync(combat, attacker, attackValue, targetId);
    }

    public async Task<bool> CheckCombatEndConditionsAsync(int combatId)
    {
        try
        {
            var combat = await _context.Combats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == combatId);

            if (combat == null) return false;

            var activeParticipants = combat.Participants.Where(p => p.IsActive).ToList();
            var playerParticipants = activeParticipants.Where(p => p.PlayerId.HasValue).ToList();
            var enemyParticipants = activeParticipants.Where(p => p.EnemyId.HasValue).ToList();

            // Check if all enemies are defeated
            if (enemyParticipants.All(e => e.IsDefeated))
            {
                await EndCombatAsync(combatId, CombatResultType.Victory);
                return true;
            }

            // Check if all players are defeated
            if (playerParticipants.All(p => p.IsDefeated))
            {
                await EndCombatAsync(combatId, CombatResultType.Defeat);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking combat end conditions for combat {CombatId}", combatId);
            return false;
        }
    }

    public async Task<bool> EndCombatAsync(int combatId, CombatResultType result)
    {
        try
        {
            var combat = await _context.Combats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == combatId);

            if (combat == null) return false;

            combat.Status = CombatStatus.Resolved;
            combat.EndedAt = DateTime.UtcNow;

            // Create combat result
            var combatResult = new CombatResult
            {
                CombatId = combatId,
                AttackerId = combat.AttackingPlayerId ?? 0,
                DefenderId = combat.DefendingPlayerId ?? 0,
                AttackerTotal = 0, // Calculate from final state
                DefenderTotal = 0, // Calculate from final state
                Winner = result == CombatResultType.Victory ? combat.AttackingPlayerId : combat.DefendingPlayerId,
                DamageDealt = 0, // Calculate from actions
                IsVictory = result == CombatResultType.Victory,
                ResolvedAt = DateTime.UtcNow,
                Notes = $"Combat ended with {result}"
            };

            _context.CombatResults.Add(combatResult);
            _context.Combats.Update(combat);
            await _context.SaveChangesAsync();

            await LogCombatActionAsync(combatId, 0, CombatActionType.CombatEnd, 
                $"Combat ended with result: {result}", 0);

            _logger.LogInformation("Combat {CombatId} ended with result {Result}", combatId, result);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending combat {CombatId}", combatId);
            return false;
        }
    }

    private async Task LogCombatActionAsync(int combatId, int participantId, CombatActionType actionType, string description, int value, int? targetId = null)
    {
        try
        {
            var action = new CombatAction
            {
                CombatId = combatId,
                ParticipantId = participantId,
                ActionType = actionType,
                Description = description,
                Value = value,
                TargetId = targetId,
                IsResolved = true,
                Result = "Logged"
            };

            _context.CombatActions.Add(action);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging combat action");
        }
    }

    private async Task<List<Enemy>> CreateSiteDefendersAsync(Site site)
    {
        // This is a simplified implementation
        // In a full implementation, this would read from sites.json and create appropriate enemies
        var defenders = new List<Enemy>();

        // Create basic defenders based on site type
        var defender = new Enemy
        {
            Name = $"{site.Type} Defender",
            Type = EnemyType.Humanoid,
            Color = EnemyColor.Brown,
            Attack = 2,
            Armor = 1,
            Health = 3,
            Initiative = 1,
            FameValue = 1
        };

        _context.Enemies.Add(defender);
        await _context.SaveChangesAsync();

        defenders.Add(defender);
        return defenders;
    }

    public async Task<Combat?> GetCombatAsync(int combatId)
    {
        return await _context.Combats
            .Include(c => c.Participants)
            .ThenInclude(p => p.Player)
            .Include(c => c.Participants)
            .ThenInclude(p => p.Enemy)
            .Include(c => c.Actions)
            .Include(c => c.Results)
            .FirstOrDefaultAsync(c => c.Id == combatId);
    }

    public async Task<List<Combat>> GetActiveCombatsAsync(int gameSessionId)
    {
        return await _context.Combats
            .Include(c => c.Participants)
            .Where(c => c.GameSessionId == gameSessionId && c.Status == CombatStatus.InProgress)
            .ToListAsync();
    }
}

public enum CombatResultType
{
    Victory,
    Defeat,
    Draw,
    Cancelled
}
