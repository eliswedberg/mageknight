using MageKnightOnline.Models;

namespace MageKnightOnline.Models;

public class LevelUpResult
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public GamePlayer? Player { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int HealthIncrease { get; set; }
    public int ManaIncrease { get; set; }
    public int FameIncrease { get; set; }
    public List<string> NewAbilities { get; set; } = new();
    public List<MageKnightCard> NewSpells { get; set; } = new();
    public DateTime LeveledUpAt { get; set; }
}

public class PlayerStats
{
    public int PlayerId { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Health { get; set; }
    public int Mana { get; set; }
    public int Fame { get; set; }
    public int Reputation { get; set; }
    public int ExperienceToNextLevel { get; set; }
}
