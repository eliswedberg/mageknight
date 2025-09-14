using MageKnightOnline.Models;

namespace MageKnightOnline.Models;

public class VictoryCheckResult
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public GameSession? GameSession { get; set; }
    public bool HasWinner { get; set; }
    public int? Winner { get; set; }
    public VictoryType? VictoryType { get; set; }
    public List<VictoryCondition> Winners { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

public class VictoryCondition
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public VictoryType VictoryType { get; set; }
    public int Score { get; set; }
    public int Threshold { get; set; }
}

public class PlayerScore
{
    public int PlayerId { get; set; }
    public int Fame { get; set; }
    public int Reputation { get; set; }
    public int ConqueredSites { get; set; }
    public int CombatVictories { get; set; }
    public int TotalScore { get; set; }
}

public class GameStatistics
{
    public int GameSessionId { get; set; }
    public int TotalTurns { get; set; }
    public int TotalCombats { get; set; }
    public int TotalSitesConquered { get; set; }
    public int TotalCardsPlayed { get; set; }
    public TimeSpan? GameDuration { get; set; }
    public int PlayerCount { get; set; }
}

public enum VictoryType
{
    Fame,
    Reputation,
    Conquest,
    Time
}
