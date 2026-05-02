# Mage Knight Online - Specification

This directory contains the complete specifications for implementing Mage Knight Board Game as an online multiplayer game.

## Technical Stack

- **Backend**: .NET 10, Blazor Server, Entity Framework Core
- **Real-time**: SignalR for multiplayer synchronization
- **Database**: SQL Server
- **Frontend**: Blazor Components with CSS

## Directory Structure

```
spec/
├── definitions/          # JSON game data files
├── rules/               # Game rules documentation
├── entities/            # C# entity examples
└── README.md            # This file
```

---

## Game Definition Files

### Core Game Components

| File | Description |
|------|-------------|
| `heroes.json` | The 4 playable heroes (Tovak, Arythea, Goldyx, Norowas) |
| `basic_actions.json` | 16 starting cards shared by all heroes |
| `advanced_actions.json` | 38 powerful cards gained through leveling/monasteries |
| `spells.json` | 25 magical spells requiring mana to cast |
| `artifacts.json` | 18 rare items with powerful effects |
| `units.json` | 22 recruitable units (13 Regular, 9 Elite) |
| `enemies.json` | 30 enemy types across 6 categories |

### Game Systems

| File | Description |
|------|-------------|
| `combat_abilities.json` | **NEW** All offensive/defensive abilities and resistances |
| `game_rules.json` | **NEW** Complete turn structure and combat phases |
| `terrain_costs.json` | Movement costs for all terrain types |
| `sites.json` | All map locations and their interactions |
| `ruins.json` | Ruins tokens (loot and combat encounters) |
| `tactics.json` | 12 Tactics cards (6 Day, 6 Night) |
| `leveling.json` | Fame thresholds and level-up rewards |
| `reputation.json` | Reputation levels and their effects |
| `map_tiles.json` | Map tile definitions |
| `scenarios.json` | Game scenario configurations |
| `hero_skills.json` | Hero-specific skill trees |

---

## Player Actions Summary

### During Any Turn

1. **Use Mana from Source** - Take ONE mana die from Source per turn
2. **Play Cards** - Use cards from hand for their effects
3. **Use Crystals** - Spend stored crystals (unlimited per turn)
4. **Activate Units** - Use recruited units' abilities (once per round each)

### Regular Turn Structure

1. **Movement Phase** (Optional)
   - Play Move cards to generate Move points
   - Move between hexes (pay terrain cost)
   - Explore new tiles (2 Move points from edge hex)
   
2. **Action Phase** (Optional/Mandatory based on location)
   - Combat with enemies
   - Interact with locals (recruit, heal, learn)
   - PvP combat (if on same space as opponent)

3. **End of Turn**
   - Return mana dice to Source
   - Discard played cards
   - Draw up to Hand limit

### Rest Turn

- **Standard Rest**: Discard 1 non-Wound + all Wounds
- **Slow Recovery**: Discard 1 Wound (only if hand is all Wounds)

---

## Combat System

Combat follows strict phase order from the [Mage Knight Rulebook](Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf):

### Combat Phases

| Phase | Actions |
|-------|---------|
| 1. Ranged/Siege | Attack enemies before they strike. Siege required for Fortified. |
| 2. Block | Block enemy attacks. Each attack blocked separately. |
| 3. Assign Damage | Unblocked damage goes to Units (wound) or Hero (Wound cards). |
| 4. Attack | Melee phase. Any attack type works. Cards sideways = Attack 1. |

### Enemy Abilities (Offensive)

| Ability | Effect |
|---------|--------|
| **Swift** | Requires 2x Block value to fully block |
| **Brutal** | Deals 2x damage if unblocked |
| **Poison** | Units get 2 Wounds; Hero puts extra Wound in discard |
| **Paralyze** | Wounded Units destroyed; Hero discards non-Wounds |
| **Assassination** | Damage cannot be assigned to Units |
| **Summon** | Draws replacement enemy for Block/Damage phases |
| **Cumbersome** | Can spend Move points to reduce attack value |
| **Vampiric** | Armor +1 per wound caused |

### Enemy Abilities (Defensive)

| Ability | Effect |
|---------|--------|
| **Fortified** | Only Siege attacks in Ranged phase |
| **Physical Resistance** | Physical attacks halved |
| **Fire Resistance** | Fire attacks halved, ignores red effects |
| **Ice Resistance** | Ice attacks halved, ignores blue effects |
| **Elusive** | Two armor values; lower only if all attacks blocked |
| **Arcane Immunity** | Immune to non-Attack/Block effects |

### Attack Elements

| Element | Efficient Blocks |
|---------|-----------------|
| Physical | Any block type |
| Fire | Ice, Cold Fire |
| Ice | Fire, Cold Fire |
| Cold Fire | Cold Fire only |

---

## Mana System

### Mana Sources

| Source | Rules |
|--------|-------|
| **Source Pool** | Shared dice. Use ONE per turn. Rerolled at end of turn. |
| **Crystals** | Personal storage. Use unlimited. Persist between turns. |
| **Mana Tokens** | Temporary mana. Disappear at end of turn. |

### Mana Colors

| Color | Availability | Special |
|-------|--------------|---------|
| Red, Blue, White, Green | Always | Basic colors |
| Gold | Day only | Substitutes any color |
| Black | Night only | Substitutes any color |

### Powered Effects

- **Basic Actions**: Any mana powers them
- **Advanced Actions**: May require specific color
- **Spells**: Require matching color for Strong effect
- **Gold/Black**: Can substitute for any required color

---

## Sites and Locations

### Safe Locations

| Site | Key Interactions |
|------|------------------|
| **Village** | Recruit Regular Units, Heal (3 Influence), Plunder (-1 Rep) |
| **Monastery** | Recruit, Heal (2 Inf), Training (6 Inf → Adv Action), Burn (-2 Rep) |
| **Magical Glade** | Free Heal, Empower (Gold/Black mana), Cleanse Wounds |
| **Crystal Mine** | Harvest 1 Crystal of mine's color |

### Fortified Sites (Require Siege to attack defenders)

| Site | Defenders | Reward |
|------|-----------|--------|
| **Mage Tower** | 1 Violet | Learn Spells, Recruit |
| **Keep** | 1 Grey | Control (shelter, recruit) |
| **City** | 2-6 White | Control, Fame, all interactions |

### Adventure Sites (Mandatory combat on entry)

| Site | Enemies | Reward |
|------|---------|--------|
| **Dungeon** | 1 Brown (Night rules) | 1 Artifact |
| **Tomb** | 1 Red (Night rules) | 1 Artifact + 1 Spell |
| **Monster Den** | 1 Brown | 2 Crystals |
| **Spawning Grounds** | 2 Brown | 1 Artifact |
| **Ancient Ruins** | Variable (token) | Token reward |

### Rampaging Enemies

| Site | Enemy | Notes |
|------|-------|-------|
| **Marauding Orcs** | 1 Green | Provoke if moving between adjacent spaces |
| **Draconum** | 1 Red | Extremely powerful, +2 Artifacts reward |

---

## Terrain and Movement

| Terrain | Day Cost | Night Cost | Notes |
|---------|----------|------------|-------|
| Plains | 2 | 2 | Easy |
| Hills | 3 | 3 | Moderate |
| Forest | 3 | 5 | Harder at night |
| Wasteland | 4 | 4 | Consistent |
| Desert | 5 | 3 | Cooler at night |
| Swamp | 5 | 5 | Always hard |
| Lake | ∞ | ∞ | Impassable (need Flight/special) |
| Mountain | ∞ | ∞ | Impassable (need special) |

### Special Movement

- **Safe Movement**: Move through enemy hexes without combat
- **Flight**: Ignore terrain costs
- **Underground Travel**: Teleport to Dungeons/Tombs/Mines

---

## Leveling System

| Level | Fame Required | Armor | Hand | Reward |
|-------|---------------|-------|------|--------|
| 1 | 0 | 2 | 5 | Starting |
| 2 | 3 | 2 | 5 | Skill + Advanced Action |
| 3 | 8 | 2 | 5 | Command Token |
| 4 | 15 | 3 | 5 | Skill + Advanced Action |
| 5 | 24 | 3 | 5 | Command Token |
| 6 | 35 | 3 | 5 | Skill + Advanced Action |
| 7 | 48 | 3 | 6 | Command Token |
| 8 | 64 | 4 | 6 | Skill + Advanced Action |
| 9 | 82 | 4 | 6 | Command Token |
| 10 | 104 | 4 | 6 | Skill + Advanced Action |

---

## Reputation Effects

| Position | Title | Influence Modifier |
|----------|-------|-------------------|
| 0 (X) | Criminal | -5 (Cannot interact) |
| 1 | Villain | -3 |
| 2 | Thug | -2 |
| 3 | Mercenary | -1 |
| 4 | Wanderer | 0 (Starting) |
| 5 | Captain | +1 |
| 6 | Guardian | +2 |
| 7 | Hero | +3 |
| 8 | Legend | +5 |

---

## Implementation Notes

### Card Playing

1. **Normal Play**: Use card's Basic or Powered effect
2. **Sideways Play**: Any non-Wound card = +1 of its type (limited 1/turn normally)
3. **Powered Effects**: Require mana matching card's color (or any for Basic Actions)

### Unit Management

1. Each Unit needs a Command Token to recruit
2. Units activate once per Round, then exhaust (flip)
3. Units refresh at Round end
4. Wounded Units have Wound cards on them (2 Wounds = destroyed)

### Round Structure

1. Flip Day/Night board
2. Reroll Source dice
3. Select Tactics (lowest Fame first)
4. Take turns until End of Round announced
5. Shuffle discards into decks
6. Refresh Units

---

## File Cross-References

- Enemy abilities → `combat_abilities.json`
- Unit abilities → `combat_abilities.json`
- Site interactions → `sites.json`
- Combat rules → `game_rules.json`
- Card effects → `basic_actions.json`, `advanced_actions.json`, `spells.json`
- Level rewards → `leveling.json`
- Terrain costs → `terrain_costs.json`

---

## Source Material

Based on **Mage Knight Board Game Ultimate Edition Rule Book (September 2018)**.

See `Mage-Knight-Board-Game-Ultimate-Edition-Rule-Book-September-2018.pdf` for complete official rules.
