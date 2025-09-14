using System.ComponentModel.DataAnnotations;

namespace MageKnightOnline.Models;

public class Site
{
    public int Id { get; set; }
    
    public int GameBoardId { get; set; }
    public GameBoard GameBoard { get; set; } = null!;
    
    public int X { get; set; }
    public int Y { get; set; }
    
    public SiteType Type { get; set; }
    
    // Enhanced site properties
    public SiteSubType SiteSubType { get; set; }
    public int DifficultyLevel { get; set; } = 1;
    public int RequiredLevel { get; set; } = 1;
    public bool IsRepeatable { get; set; } = false;
    public string? SpecialRequirements { get; set; } // JSON string
    public string? EnemyIds { get; set; } // JSON string
    public string? LootTable { get; set; } // JSON string
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsExplored { get; set; } = false;
    
    public bool IsConquered { get; set; } = false;
    
    public int? ConqueredByPlayerId { get; set; }
    public GamePlayer? ConqueredByPlayer { get; set; }
    
    public DateTime? ConqueredAt { get; set; }
    
    public int AttackCost { get; set; } = 0;
    
    public int FameReward { get; set; } = 0;
    
    public int ReputationReward { get; set; } = 0;
    
    public int CrystalsReward { get; set; } = 0;
    
    public string? ArtifactReward { get; set; }
    
    public string? SpellReward { get; set; }
    
    public string? UnitReward { get; set; }
    
    public ICollection<SiteEnemy> Enemies { get; set; } = new List<SiteEnemy>();
}

public enum SiteType
{
    Ruins,
    Dungeon,
    Keep,
    MageTower,
    Monastery,
    Village,
    City,
    Volkaire,
    Krang,
    Portal,
    Tomb,
    Library,
    Laboratory
}

public enum SiteSubType
{
    Village,
    Keep,
    MageTower,
    Monastery,
    Dungeon,
    Ruins,
    City,
    Portal,
    Tomb,
    Library,
    Laboratory
}

public class SiteEnemy
{
    public int Id { get; set; }
    
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    
    public int Attack { get; set; }
    
    public int Block { get; set; }
    
    public int Health { get; set; }
    
    public int CurrentHealth { get; set; }
    
    public bool IsDefeated { get; set; } = false;
    
    public string? SpecialAbilities { get; set; }
    
    public int FameReward { get; set; } = 0;
    
    public int ReputationReward { get; set; } = 0;
}
