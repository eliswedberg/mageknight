using MageKnightOnline.Data;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Services;

public class DayNightService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DayNightService> _logger;

    public DayNightService(ApplicationDbContext context, ILogger<DayNightService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SetNightAsync(int gameSessionId, bool isNight)
    {
        var state = await _context.GameStates.FirstOrDefaultAsync(s => s.GameSessionId == gameSessionId);
        if (state == null) return false;
        state.IsNightPhase = isNight;
        if (isNight) state.NightRounds++; else state.DayRounds++;
        await _context.SaveChangesAsync();
        return true;
    }
}


