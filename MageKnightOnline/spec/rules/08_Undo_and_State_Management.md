# Undo och State Management

## Översikt

Mage Knight Online implementerar ett undo-system som låter spelare ångra sina drag under vissa förutsättningar. Detta följer brädspelsreglerna där spelare kan "ta tillbaka" beslut så länge ingen ny information har avslöjats.

## Undo-regler

### När kan man ångra?

En spelare kan ångra sina senaste drag **så länge ingen ny information har avslöjats**:

- ✅ Spela kort (basic, powered, sideways)
- ✅ Använda mana från source
- ✅ Rörelse på revealed hexar
- ✅ Site interactions (som inte avslöjar något)
- ✅ Använda kristaller

### När blockeras undo?

Undo blir **permanent blockerat** för resten av turen när:

1. **Ny map tile avslöjas** (Exploration)
   - Spelaren ser vilken tile som ligger där
   - Terrain och sites på nya hexar avslöjas
   - Fiender på adventure sites avslöjas

2. **Kort dras från delade decks**:
   - Advanced Actions (via Training)
   - Spells (via Learn Spell eller Mage Tower)
   - Artifacts (via Ruins tokens eller site rewards)
   - Ruins tokens (vid Ruins-besök)

3. **Fiender avslöjas**
   - Draconum eller andra dolda fiender
   - City defenders

### Ny tur = Ny undo-möjlighet

Vid början av varje spelares tur:
- Undo-stacken rensas
- `CanUndo` återställs till `true`
- Spelaren börjar "fresh"

## Teknisk Implementation

### GameStateModel

```csharp
public class GameStateModel
{
    // Undo state - persisteras i databasen
    public List<string> UndoStack { get; set; } = new();
    public bool CanUndo { get; set; } = true;
}
```

### GameEngine Metoder

#### SaveStateForUndo()
Sparar nuvarande state till undo-stacken före kritiska actions.

```csharp
private void SaveStateForUndo()
{
    if (!_state.CanUndo) return;
    
    var serialized = JsonSerializer.Serialize(_state);
    _state.UndoStack.Add(serialized);
    
    // Begränsa stack-storleken
    while (_state.UndoStack.Count > 20)
        _state.UndoStack.RemoveAt(0);
}
```

#### MarkIrreversibleAction()
Blockerar undo när ny information avslöjas.

```csharp
private void MarkIrreversibleAction()
{
    _state.CanUndo = false;
    _state.UndoStack.Clear();
}
```

#### ResetUndoForNewTurn()
Återställer undo vid ny tur.

```csharp
private void ResetUndoForNewTurn()
{
    _state.CanUndo = true;
    _state.UndoStack.Clear();
}
```

#### UndoLastAction()
Återställer till föregående state.

```csharp
public GameActionResult UndoLastAction()
{
    if (!_state.CanUndo || _state.UndoStack.Count == 0)
        return GameActionResult.Fail("Cannot undo");
    
    var previousState = _state.UndoStack.Last();
    _state.UndoStack.RemoveAt(_state.UndoStack.Count - 1);
    
    // Återställ state
    var restored = JsonSerializer.Deserialize<GameStateModel>(previousState);
    // ... kopiera relevanta fält
    
    return GameActionResult.Ok("Action undone");
}
```

## Actions som sparar state

Följande actions anropar `SaveStateForUndo()`:

| Action | Metod |
|--------|-------|
| Spela kort | `PlayCard()` |
| Använda mana | `UseMana()` |
| Rörelse | `MovePlayer()` |
| Flight | `MovePlayerWithFlight()` |
| Safe movement | `MovePlayerSafely()` |
| Site interaction | `InteractWithSite()` |

## Actions som blockerar undo

Följande actions anropar `MarkIrreversibleAction()`:

| Action | Metod | Anledning |
|--------|-------|-----------|
| Exploration | `ExploreTile()` | Ny tile avslöjas |
| Training | `Training()` | Advanced Action dras |
| Learn Spell | `LearnSpell()` | Spell dras |
| Draw Ruins Token | `DrawRuinsToken()` | Token avslöjas |
| Ruins Loot | `ApplyRuinsTokenEffects()` | Artifact/Spell dras |

## UI-implementation

### Undo-knapp

```razor
@if (_canUndo)
{
    <button class="btn btn-warning btn-sm" @onclick="UndoLastAction">
        ↩️ Undo
    </button>
}
```

### Visuell feedback

- Undo-knappen visas bara när undo är möjligt
- Efter exploration försvinner knappen
- Tooltip förklarar varför undo inte är tillgängligt

## Multiplayer-överväganden

- Undo-state persisteras i databasen
- Varje spelares undo är oberoende
- SignalR uppdaterar andra spelare efter undo
- Ingen undo av andra spelares drag (naturligtvis)
