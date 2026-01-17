# Site Tokens and Enemy Placement

## Overview

When map tiles are revealed, each hex with a special site gets appropriate enemies or tokens placed based on the site type. This document describes the enemy generation logic.

## Site Categories

### Safe Sites (No Enemies)
These sites are friendly and have no combat encounters:

| Site Type | Icon | Description |
|-----------|------|-------------|
| Village | 🏘️ | Safe haven for recruiting and healing |
| Monastery | ⛪ | Sacred place for training and healing |
| Portal | 🌀 | Starting location |
| Magical Glade | ✨ | Healing and mana source |
| Crystal Mines | ⛏️ | Resource gathering (Mine_Red, Mine_Blue, etc.) |

### Fortified Sites (Defenders)
These sites have guards that must be defeated to conquer:

| Site Type | Icon | Enemy Type | Count | Notes |
|-----------|------|------------|-------|-------|
| Keep | 🏰 | Grey | 1 | Siege required |
| Mage Tower | 🗼 | Violet | 1 | Siege required |
| City | 🏙️ | White | 2-6 | Count based on city level |

### Adventure Sites (Monsters)
Entering triggers mandatory combat:

| Site Type | Icon | Enemy Type | Count | Special Rules |
|-----------|------|------------|-------|---------------|
| Dungeon | 🕳️ | Brown | 1 | Night combat rules |
| Tomb | ⚰️ | Red (Draconum) | 1 | Night combat rules |
| Monster Den | 🦴 | Brown | 1 | Normal rules |
| Spawning Grounds | 👹 | Brown | 2 | Fight both at once |
| Ruins | 🏛️ | Variable | - | Draw Ruins token |

### Rampaging Enemies (On Map)
These enemies block movement and can be challenged:

| Site Type | Icon | Enemy Type | Count | Behavior |
|-----------|------|------------|-------|----------|
| Orc Marauders | ⚔️ | Green | 1 | Provokes when moving between adjacent hexes |
| Draconum | 🐉 | Red | 1 | Extremely powerful dragon |

## Enemy Generation Algorithm

```csharp
private List<string> GenerateEnemiesForSite(string? siteType)
{
    // 1. Determine enemy type and count based on site
    var (enemyType, count) = GetEnemyConfig(siteType);
    
    // 2. If no enemies needed, return empty list
    if (enemyType == null || count == 0)
        return new List<string>();
    
    // 3. Get all enemies of the required type
    var availableEnemies = GetEnemiesByType(enemyType);
    
    // 4. Randomly select the required number
    var enemies = new List<string>();
    for (int i = 0; i < count; i++)
    {
        var randomEnemy = availableEnemies[random.Next(availableEnemies.Count)];
        enemies.Add(randomEnemy.Id);
    }
    
    return enemies;
}
```

## Enemy Types

Enemies are categorized by color, which determines where they appear:

| Color | Token Back | Typical Locations |
|-------|------------|-------------------|
| **Green** | Green | Orc Marauders |
| **Grey** | Grey | Keeps |
| **Violet** | Purple | Mage Towers, Monasteries |
| **Brown** | Brown | Dungeons, Monster Dens, Spawning Grounds |
| **Red** | Red | Tombs, Draconum Lairs |
| **White** | White | Cities |

## Visual Indicators

### On the Map
- **Site Icon**: Circular badge with emoji showing site type
- **Site Color**: Background color indicates danger level
  - Green/Blue: Safe sites
  - Orange/Purple: Fortified sites  
  - Red/Brown: Dangerous adventure sites
- **Enemy Counter**: Red circle with number shows enemy count

### Color Coding
```
Safe:       Green (#22c55e), Blue (#3b82f6)
Fortified:  Orange (#f97316), Purple (#8b5cf6)
Adventure:  Brown (#b45309), Gray (#78716c)
Rampaging:  Red (#dc2626), Green (#16a34a)
```

## Ruins Token System

Ruins sites use a special token draw system instead of predetermined enemies:

1. When entering Ruins, draw a Ruins token
2. Token determines encounter:
   - **Loot tokens**: Immediate rewards (crystals, cards)
   - **Combat tokens**: Specify enemies to fight

### Ruins Token Types

| Token | Type | Effect |
|-------|------|--------|
| Wealth | Loot | 2 Crystals + 1 Mana |
| Ancient Knowledge | Loot | Gain a Spell |
| Lost Artifact | Loot | Gain an Artifact |
| Reinforcements | Loot | Free Unit recruit |
| Enemies (1 Green) | Combat | Fight 1 Green enemy |
| Enemies (2 Green) | Combat | Fight 2 Green enemies |
| Enemies (1 Brown) | Combat | Fight 1 Brown enemy |
| Enemies (1 Green + 1 Brown) | Combat | Fight mixed enemies |
| Enemies (1 Grey) | Combat | Fight 1 Grey enemy |
| Enemies (1 Violet) | Combat | Fight 1 Violet enemy |
| Enemies (1 Red) | Combat | Fight 1 Draconum |

## Implementation Files

- `src/MageKnightOnline.Core/GameEngine/GameEngine.cs` - `GenerateEnemiesForSite()` method
- `src/MageKnightOnline.Web/Components/Game/TileMap.razor` - Visual rendering
- `spec/definitions/sites.json` - Site definitions
- `spec/definitions/enemies.json` - Enemy definitions
- `spec/definitions/ruins.json` - Ruins token definitions

## Related Documentation

- [Movement and Exploration](03_Movement_and_Exploration.md)
- [Combat](04_Combat.md)
- [Tile Placement Logic](06_Tile_Placement_Logic.md)
