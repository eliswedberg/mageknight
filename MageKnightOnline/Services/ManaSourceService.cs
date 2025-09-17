using MageKnightOnline.Data;
using MageKnightOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class ManaSourceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ManaSourceService> _logger;

    public ManaSourceService(ApplicationDbContext context, ILogger<ManaSourceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public record ManaSourceState(bool IsNight, List<ManaDie> Dice);

    // Number of dice = players + 2 (solo rules simplified)
    private static int DiceForPlayers(int playerCount) => Math.Max(2, playerCount + 2);

    public async Task<ManaSourceState> GetOrCreateAsync(int gameSessionId)
    {
        // Load players to know dice count
        var session = await _context.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.Id == gameSessionId);
        if (session == null) return new ManaSourceState(false, new());

        // Load game state to know day/night
        var state = await _context.GameStates.FirstOrDefaultAsync(g => g.GameSessionId == gameSessionId);
        var isNight = state?.IsNightPhase ?? false;

        // For now, we generate in-memory dice each call; persistence can be added later
        var diceCount = DiceForPlayers(session.Players.Count);
        var dice = RollDice(diceCount, isNight);
        return new ManaSourceState(isNight, dice);
    }

    public List<ManaDie> RollDice(int count, bool isNight)
    {
        var rng = new Random();
        var results = new List<ManaDie>(count);
        for (int i = 0; i < count; i++)
        {
            results.Add(new ManaDie { Color = RollFace(rng, isNight) });
        }
        return results;
    }

    private static ManaColor RollFace(Random rng, bool isNight)
    {
        // Standard die: 1 each of white/blue/red/green, and two gold (day) or black (night)
        var faces = isNight
            ? new[] { ManaColor.White, ManaColor.Blue, ManaColor.Red, ManaColor.Green, ManaColor.Black, ManaColor.Black }
            : new[] { ManaColor.White, ManaColor.Blue, ManaColor.Red, ManaColor.Green, ManaColor.Gold, ManaColor.Gold };
        return faces[rng.Next(faces.Length)];
    }
}


