# Game Structure

## Rounds
The game is played over several **Rounds**, alternating between **Day** and **Night**.
* **Round End**: A round ends when a player with empty Deed deck announces End of Round.
* **Final Turns**: After announcement, all other players get one final turn.
* **Day/Night Board**: Tracks current time. Affects movement costs and mana availability.

## Round Start Procedure

1. **Flip Day/Night Board**: Day → Night or Night → Day.
2. **Swap Tactics Cards**: Day Tactics ↔ Night Tactics.
3. **Reroll Source Dice**: 
   * Roll all mana dice in the Source (including any that were set aside as depleted in the previous round).
   * At least half must show basic colors (Red, Blue, White, Green).
   * If not, reroll all Gold/Black dice until condition met.
4. **Tactics Selection**:
   * Player with lowest Fame selects first (later in Round Order if tied).
   * Each player picks one Tactics card.
   * Tactics determines turn order and grants a special ability.

## Turn Structure

### Regular Turn
1. **Movement Phase** (Optional)
   * Play Move cards to generate Move points.
   * Spend Move points to travel between hexes (pay terrain cost).
   * May reveal new tiles (2 Move points from edge hex).
   * Moving between spaces adjacent to Rampaging Enemy provokes combat.

2. **Action Phase** (Optional, sometimes Mandatory)
   * **Combat**: Assault fortified sites, provoke/challenge enemies, enter adventure sites.
   * **Interaction**: Recruit units, heal, buy cards with Influence.
   * **PvP**: Attack another player on same space.
   * Only ONE action type per turn.

3. **Any Time During Turn**
   * Play Special effects (not during combat).
   * Use Healing effects (not during combat).
   * Use ONE mana die from the Source.

4. **End of Turn**
   * Return and reroll used mana dice.
   * Perform Forced Withdrawal if not on safe space.
   * Discard all played cards.
   * Return all mana tokens (keep crystals).
   * Claim combat rewards and process Level ups.
   * Discard any number of cards (at least 1 if no cards played).
   * Draw cards up to Hand Limit.

### Rest Turn
Instead of a regular turn, you may Rest:
* **Standard Rest**: Discard 1 non-Wound card + any number of Wound cards.
* **Slow Recovery**: Discard 1 Wound card (only if hand is ALL Wounds).

## Player Order
* Determined by **Tactics cards** selected at round start.
* Player with Tactics #1 goes first, then #2, etc.
* Ties in Fame: Later Round Order position picks first.

## Day vs. Night

### Movement Differences
| Terrain | Day Cost | Night Cost |
|---------|----------|------------|
| Forest | 3 | 5 |
| Desert | 5 | 3 |
| Others | Same | Same |

### Mana Differences (Source Dice)
| Time | Wild color | Depleted (set aside) |
|------|------------|----------------------|
| Day | Gold | Black — dice showing Black are set aside and **depleted**; they cannot be used until the round switches to Night. |
| Night | Black | Gold — dice showing Gold are set aside and **depleted**; they cannot be used until the round switches to Day. |

* **Using a mana die**: The die color must match the card you want to play (or power), **or** you may use the current wild color (Gold during Day, Black during Night) as a substitute for any basic color.
* **Depleted dice**: During Day, any die in the Source that shows Black is set aside and is depleted for the rest of that round. During Night, any die showing Gold is set aside and depleted. At **Round Start**, when Day/Night is flipped, all Source dice are rerolled (including previously set-aside dice), so depleted dice re-enter the pool.
* Adventure sites (Dungeons, Tombs) always use Night rules regardless of actual time.

## End of Round

### Triggering End of Round
* A player with **empty Deed deck** may announce End of Round instead of taking a turn.
* If Deed deck AND hand are both empty, they **must** announce End of Round.

### End of Round Cleanup
1. All players shuffle their discard pile into their Deed deck.
2. All Units are readied (flipped face-up).
3. All mana tokens are removed (crystals remain).
4. Start next round (flip Day/Night, etc.).

## Victory Conditions
Determined by the chosen Scenario. Common conditions:
* **Time Limit**: Game ends after X rounds.
* **City Conquest**: Game ends when all cities conquered.
* **Fame Race**: Player with highest Fame wins.
* **Survival**: Last player standing wins.

See `scenarios.json` and individual scenario rules for specific conditions.
