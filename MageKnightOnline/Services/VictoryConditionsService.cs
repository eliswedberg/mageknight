using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class VictoryConditionsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VictoryConditionsService> _logger;

    public VictoryConditionsService(ApplicationDbContext context, ILogger<VictoryConditionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VictoryCheckResult> CheckVictoryConditionsAsync(int gameSessionId)
    {
        var gameSession = await _context.GameSessions
            .Include(gs => gs.Players)
            .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

        if (gameSession == null)
            throw new ArgumentException("Game session not found");

        var result = new VictoryCheckResult
        {
            GameSessionId = gameSessionId,
            HasWinner = false,
            CheckedAt = DateTime.UtcNow
        };

        var winners = new List<VictoryCondition>();

        // Check fame victory (first to reach 50 fame)
        var fameWinner = gameSession.Players
            .Where(p => p.Fame >= 50)
            .OrderByDescending(p => p.Fame)
            .FirstOrDefault();

        if (fameWinner != null)
        {
            winners.Add(new VictoryCondition
            {
                PlayerId = fameWinner.Id,
                VictoryType = VictoryType.Fame,
                Score = fameWinner.Fame,
                Threshold = 50
            });
        }

        // Check reputation victory (first to reach 30 reputation)
        var reputationWinner = gameSession.Players
            .Where(p => p.Reputation >= 30)
            .OrderByDescending(p => p.Reputation)
            .FirstOrDefault();

        if (reputationWinner != null)
        {
            winners.Add(new VictoryCondition
            {
                PlayerId = reputationWinner.Id,
                VictoryType = VictoryType.Reputation,
                Score = reputationWinner.Reputation,
                Threshold = 30
            });
        }

        // Check conquest victory (first to conquer 3 sites)
        var conquestWinner = gameSession.Players
            .Where(p => p.SitesConquered >= 3)
            .OrderByDescending(p => p.SitesConquered)
            .FirstOrDefault();

        if (conquestWinner != null)
        {
            winners.Add(new VictoryCondition
            {
                PlayerId = conquestWinner.Id,
                VictoryType = VictoryType.Conquest,
                Score = conquestWinner.SitesConquered,
                Threshold = 3
            });
        }

        if (winners.Any())
        {
            result.HasWinner = true;
            result.Winners = winners;
            result.Winner = winners.First().PlayerId;
            result.VictoryType = winners.First().VictoryType;
        }

        _context.VictoryCheckResults.Add(result);
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<PlayerScore> CalculatePlayerScoreAsync(int playerId)
    {
        var player = await _context.GamePlayers
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            throw new ArgumentException("Player not found");

        return new PlayerScore
        {
            PlayerId = playerId,
            Fame = player.Fame,
            Reputation = player.Reputation,
            ConqueredSites = player.SitesConquered,
            CombatVictories = player.EnemiesDefeated,
            TotalScore = player.Fame + player.Reputation + (player.SitesConquered * 10) + (player.EnemiesDefeated * 5)
        };
    }

    public async Task<GameStatistics> GetGameStatisticsAsync(int gameSessionId)
    {
        var gameSession = await _context.GameSessions
            .Include(gs => gs.Players)
            .FirstOrDefaultAsync(gs => gs.Id == gameSessionId);

        if (gameSession == null)
            throw new ArgumentException("Game session not found");

        var totalTurns = await _context.GameActions
            .Where(ga => ga.GameSessionId == gameSessionId)
            .CountAsync();

        var totalCombats = await _context.Combats
            .Where(c => c.GameSessionId == gameSessionId)
            .CountAsync();

        var totalSitesConquered = gameSession.Players.Sum(p => p.SitesConquered);

        var totalCardsPlayed = await _context.GameActions
            .Where(ga => ga.GameSessionId == gameSessionId && ga.Type == Models.ActionType.CardPlayed)
            .CountAsync();

        var gameDuration = gameSession.EndedAt.HasValue 
            ? gameSession.EndedAt.Value - gameSession.StartedAt
            : DateTime.UtcNow - gameSession.StartedAt;

        return new GameStatistics
        {
            GameSessionId = gameSessionId,
            TotalTurns = totalTurns,
            TotalCombats = totalCombats,
            TotalSitesConquered = totalSitesConquered,
            TotalCardsPlayed = totalCardsPlayed,
            GameDuration = gameDuration,
            PlayerCount = gameSession.Players.Count
        };
    }
}