# Mage Knight Online - Implementeringsplan

## Översikt
Detta dokument beskriver implementeringsplanen för Mage Knight Online, en webbbaserad version av brädspelet Mage Knight.

**Teknisk Stack:**
- .NET 10
- Blazor Server
- Entity Framework Core
- SQL Server (LocalDB för utveckling, Azure SQL för produktion)
- SignalR (inbyggt i Blazor Server)

**Utvecklingsmiljö:**
- Visual Studio 2022 / VS Code
- SQL Server LocalDB
- Kör lokalt under utveckling

**Produktion (framtida):**
- Azure App Service
- Azure SQL Database

---

## Fas 1: Grundläggande Infrastruktur (2-3 veckor)

### 1.1 Projektstruktur
```
MageKnightOnline/
├── src/
│   ├── MageKnightOnline.Web/       # Blazor Server projekt (huvudprojekt)
│   ├── MageKnightOnline.Core/      # Domänmodeller & affärslogik
│   └── MageKnightOnline.Data/      # Entity Framework & repositories
├── tests/
│   └── MageKnightOnline.Tests/     # Enhetstester
├── spec/                           # Specifikationer (finns redan)
└── wwwroot/                        # Statiska filer (finns redan)
```

#### Skapa projektstruktur (kör i terminalen):
```powershell
# Skapa solution
dotnet new sln -n MageKnightOnline

# Skapa projekt
dotnet new blazor -n MageKnightOnline.Web -o src/MageKnightOnline.Web --interactivity Server
dotnet new classlib -n MageKnightOnline.Core -o src/MageKnightOnline.Core
dotnet new classlib -n MageKnightOnline.Data -o src/MageKnightOnline.Data
dotnet new xunit -n MageKnightOnline.Tests -o tests/MageKnightOnline.Tests

# Lägg till projekt i solution
dotnet sln add src/MageKnightOnline.Web
dotnet sln add src/MageKnightOnline.Core
dotnet sln add src/MageKnightOnline.Data
dotnet sln add tests/MageKnightOnline.Tests

# Lägg till projektreferenser
dotnet add src/MageKnightOnline.Web reference src/MageKnightOnline.Core
dotnet add src/MageKnightOnline.Web reference src/MageKnightOnline.Data
dotnet add src/MageKnightOnline.Data reference src/MageKnightOnline.Core
dotnet add tests/MageKnightOnline.Tests reference src/MageKnightOnline.Core

# Lägg till NuGet-paket
dotnet add src/MageKnightOnline.Data package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/MageKnightOnline.Data package Microsoft.EntityFrameworkCore.Tools
dotnet add src/MageKnightOnline.Web package Microsoft.EntityFrameworkCore.Design
dotnet add src/MageKnightOnline.Web package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

### 1.2 Databas & Entity Framework
- [x] Skapa DbContext med SQL Server LocalDB
- [x] Connection string i appsettings.json (LocalDB)
- [x] User-modell (Id, Username, Email, PasswordHash, CreatedAt)
- [x] Game-modell:
  - Id, Name, Status, CreatedByUserId, ScenarioId
  - MaxPlayers, MinPlayers, CreatedAt, StartedAt, EndedAt
  - **Settings** (JSON) - scenario-specifika inställningar
  - **GameState** (JSON) - hela spelstatet
- [x] GamePlayer-modell (GameId, UserId, HeroId, JoinedAt, IsReady)
- [x] Migrations setup
- [ ] Seed-data för testanvändare

### 1.3 Autentisering
- [x] Registrering (ingen e-postverifiering)
- [x] Inloggning / Utloggning
- [x] Session-hantering (ProtectedSessionStorage)
- [ ] Authorization policies

### 1.4 Grundläggande UI-layout
- [x] Master layout med navigation
- [x] Responsiv design
- [x] Brädspelstema (mörkt, fantasy-inspirerat)
- [x] CSS-variabler för tema

---

## Fas 2: Lobby & Spelhantering (2-3 veckor)

### 2.1 Lobby-system
- [x] Lista aktiva spel (filtrera: Väntar, Pågår, Avslutade)
- [x] Skapa nytt spel
  - [x] Välj speltyp (endast Mage Knight nu)
  - [x] Ange spelnamn
  - [x] Välj antal spelare (1-4)
  - [x] Välj scenario
  - [x] Scenariospecifika inställningar
- [x] Gå med i spel
- [x] Lämna spel
- [x] Starta spel (endast skapare, min antal spelare uppfyllt)
- [x] Avbryt/ta bort spel (endast skapare)

### 2.2 Spelkonfiguration
- [x] Ladda scenarios från JSON
- [x] Validera spelarinställningar mot scenario
- [x] Spara spelinställningar i databasen

### 2.3 Realtidsuppdateringar
- [x] SignalR Hub för lobby
- [x] Notifiera när spelare går med/lämnar
- [x] Notifiera när spel startar

---

## Fas 3: Mage Knight Domänmodeller (2-3 veckor)

### 3.1 Ladda JSON-definitioner
Skapa services för att ladda och cacha:
- [x] `HeroDefinitionService` - heroes.json (via GameDefinitionService)
- [x] `CardDefinitionService` - basic_actions.json, advanced_actions.json, spells.json, artifacts.json
- [x] `EnemyDefinitionService` - enemies.json
- [x] `MapTileDefinitionService` - map_tiles.json
- [x] `UnitDefinitionService` - units.json
- [x] `SkillDefinitionService` - hero_skills.json
- [x] `ScenarioDefinitionService` - scenarios.json
- [x] `SiteDefinitionService` - sites.json
- [x] `TacticsDefinitionService` - tactics.json
- [x] `TerrainDefinitionService` - terrain_costs.json (hardcoded i GameEngine)
- [x] `ReputationDefinitionService` - reputation.json (hardcoded i GameEngine)
- [x] `LevelingDefinitionService` - leveling.json (hardcoded i GameEngine)
- [ ] `RuinsDefinitionService` - ruins.json

### 3.2 Spelstate-modeller
```csharp
public class MageKnightGameState
{
    public Guid GameId { get; set; }
    public int CurrentRound { get; set; }
    public DayNight TimeOfDay { get; set; }
    public List<PlayerState> Players { get; set; }
    public MapState Map { get; set; }
    public ManaSourceState ManaSource { get; set; }
    public CardOfferState CardOffers { get; set; }
    public UnitOfferState UnitOffers { get; set; }
    public TurnOrder TurnOrder { get; set; }
}

public class PlayerState
{
    public Guid UserId { get; set; }
    public string HeroId { get; set; }
    public int Fame { get; set; }
    public int Reputation { get; set; }
    public int Level { get; set; }
    public HexPosition Position { get; set; }
    public DeckState Deck { get; set; }
    public List<UnitState> Units { get; set; }
    public List<SkillState> Skills { get; set; }
    public InventoryState Inventory { get; set; }
}
```

### 3.3 Serialisering
- [x] JSON-serialisering av GameState
- [x] Spara/ladda GameState från databas
- [ ] Versionshantering av state

---

## Fas 4: Spelmotor - Kärna (3-4 veckor)

### 4.1 Turordning & Rundhantering
- [x] Taktikkortval i början av runda
- [x] Bestäm turordning baserat på taktik
- [x] Dag/Natt-cykel
- [ ] Rundslut-hantering (delvis)
- [ ] Shuffle deed deck vid rundslut (delvis)

### 4.2 Korthantering
- [x] Dra kort till handgräns
- [x] Spela kort (basic/powered)
- [x] Spela kort sideways
- [x] Discard pile
- [x] Wound-kort

### 4.3 Mana-system
- [x] Mana Source (tärningar)
- [x] Använd tärning från source
- [x] Kristaller i inventory
- [x] Mana tokens
- [x] Dag: Guld tillgängligt, Natt: Svart tillgängligt

### 4.4 Effekt-system
```csharp
public interface IEffectHandler
{
    bool CanHandle(CardEffect effect);
    Task<EffectResult> Execute(CardEffect effect, GameContext context);
}

// Implementera handlers för varje EffectType:
// MoveEffectHandler, AttackEffectHandler, BlockEffectHandler, etc.
```

---

## Fas 5: Karta & Rörelse (2-3 veckor)

### 5.1 Kartrendering
- [x] Hexagonal grid-system
- [x] Rendera map tiles
- [x] Visa terräng
- [x] Visa platser (sites)
- [x] Visa fiender
- [x] Visa spelarpositioner

### 5.2 Rörelse
- [x] Beräkna terrängkostnad (dag/natt)
- [x] Validera rörelse
- [ ] Hantera Safe movement
- [ ] Hantera Flight
- [ ] Provocera rampaging enemies

### 5.3 Exploration
- [x] Avslöja nya tiles
- [x] Placeringsregler (Core tiles, coastline) - basic
- [x] Tile deck hantering

---

## Fas 6: Strid (3-4 veckor)

### 6.1 Stridsfaser
```
1. Ranged/Siege Attack Phase
2. Block Phase  
3. Assign Damage Phase
4. Attack Phase
5. Loot/Rewards
```

### 6.2 Stridsmekanik
- [x] Initiera strid (enter site, challenge enemy)
- [x] Ranged attacks
- [x] Siege attacks (mot Fortified)
- [x] Blockering
- [x] Elementar skada (Fire, Ice, Cold)
- [x] Resistances
- [x] Armor & damage calculation
- [x] Wounds

### 6.3 Fiende-abilities
- [x] Swift
- [x] Fortified
- [x] Poison
- [x] Paralyze
- [x] Brutal
- [ ] Vampiric
- [ ] Summon (delvis)

### 6.4 Enheter i strid
- [ ] Aktivera enhet
- [ ] Enhetsskada
- [ ] Wounded units
- [ ] Ready/Exhausted state

---

## Fas 7: Interaktioner & Platser (2-3 veckor)

### 7.1 Site Interactions
- [x] Village (Recruit, Heal, Plunder)
- [x] Monastery (Recruit, Heal, Training)
- [x] Mage Tower (Recruit spellcasters, Learn spell)
- [x] Keep (Recruit if owned)
- [x] Magical Glade (Heal, Empower)
- [x] Crystal Mine (Harvest)
- [ ] Conquered City (Recruit all, Buy fame)
- [ ] Burn monastery
- [ ] Cleanse (Glade)

### 7.2 Adventure Sites
- [x] Ruins (basic)
- [x] Dungeon (night rules, brown enemy, artifact)
- [x] Tomb (night rules, red enemy, artifact + spell)
- [x] Monster Den (brown enemy, crystals)
- [x] Spawning Grounds (2x brown, artifact)
- [x] Draconum (red enemy, 2 artifacts)
- [x] Orc Marauders (green enemy, fame)
- [ ] Ruins tokens

### 7.3 Rekrytering
- [x] Visa unit offer (via site interactions)
- [x] Beräkna influence-kostnad med reputation
- [x] Validera rekrytering mot plats
- [x] Command token limit

---

## Fas 8: Leveling & Progression (1-2 veckor)

### 8.1 Fame & Leveling
- [x] Spåra fame
- [x] Level up triggers
- [x] Välj Advanced Action vid level up
- [x] Välj Skill vid level up
- [x] Uppdatera armor & hand limit

### 8.2 Reputation
- [x] Reputation track
- [x] Influence modifiers
- [x] Reputation changes (plunder, etc.)

---

## Fas 9: Scenarios & Vinst (2 veckor)

### 9.1 Scenario Setup
- [x] Ladda scenario-konfiguration
- [x] Generera map deck
- [x] Placera starting tile
- [ ] Sätt city levels

### 9.2 Vinst-villkor
- [ ] Spåra scenario-mål
- [ ] Detektera vinst/förlust
- [ ] Beräkna slutpoäng
- [ ] Visa resultat

### 9.3 Implementera scenarios
- [ ] First Reconnaissance (Training)
- [ ] Full Conquest
- [ ] Blitz Conquest
- [x] Solo Conquest (basic setup)
- [ ] Cooperative

---

## Fas 10: UI & Polish (2-3 veckor)

### 10.1 Spel-UI
- [x] Spelarens dashboard (hand, inventory, units)
- [x] Interaktiv karta
- [ ] Drag-and-drop kort
- [x] Stridsdialog (CombatPanel - delvis)
- [x] Site interaction dialog (SiteInteractionPanel - skapad men ej fullt integrerad)
- [x] Taktikval-dialog

### 10.2 Visuell feedback
- [ ] Animationer (kort, rörelse, strid)
- [ ] Ljud-effekter (valfritt)
- [ ] Tooltips för kort och enheter (delvis)
- [ ] Undo-funktion (inom tur)

### 10.3 Responsivitet
- [x] Desktop-optimerad
- [ ] Tablet-stöd (delvis)
- [ ] Grundläggande mobilvy (delvis)

---

## Fas 11: Multiplayer & Synk (2 veckor)

### 11.1 SignalR Game Hub
- [x] SignalR Hub för lobby (GameHub)
- [ ] Synka game state (delvis - via page refresh)
- [ ] Turn notifications (delvis)
- [ ] Chat (valfritt)
- [ ] Reconnection handling

### 11.2 Concurrency
- [ ] Optimistic locking
- [ ] Conflict resolution
- [ ] State validation

---

## Fas 12: Testing & Deployment (2 veckor)

### 12.1 Testing
- [ ] Unit tests för spellogik
- [ ] Integration tests för API
- [ ] E2E tests för kritiska flöden

### 12.2 Deployment
- [ ] Docker setup
- [ ] CI/CD pipeline
- [ ] Staging environment
- [ ] Production deployment

---

## Tidsuppskattning

| Fas | Beskrivning | Tid |
|-----|-------------|-----|
| 1 | Grundläggande Infrastruktur | 2-3 veckor |
| 2 | Lobby & Spelhantering | 2-3 veckor |
| 3 | Domänmodeller | 2-3 veckor |
| 4 | Spelmotor - Kärna | 3-4 veckor |
| 5 | Karta & Rörelse | 2-3 veckor |
| 6 | Strid | 3-4 veckor |
| 7 | Interaktioner & Platser | 2-3 veckor |
| 8 | Leveling & Progression | 1-2 veckor |
| 9 | Scenarios & Vinst | 2 veckor |
| 10 | UI & Polish | 2-3 veckor |
| 11 | Multiplayer & Synk | 2 veckor |
| 12 | Testing & Deployment | 2 veckor |

**Total uppskattad tid: 24-34 veckor (6-8 månader)**

---

## Prioriteringsordning (MVP)

För en spelbar MVP, fokusera på:

1. **Fas 1** - Infrastruktur (måste ha)
2. **Fas 2** - Lobby (måste ha)
3. **Fas 3** - Domänmodeller (måste ha)
4. **Fas 4** - Spelmotor kärna (måste ha)
5. **Fas 5** - Karta & Rörelse (måste ha)
6. **Fas 6** - Strid (måste ha)
7. **Fas 9** - Ett scenario (Solo Conquest)
8. **Fas 10** - Grundläggande UI

**MVP uppskattning: 16-20 veckor**

---

## Implementeringsstatus ✅

### Fas 1: Grundläggande Infrastruktur ✅ KLAR (100%)
- ✅ Projektstruktur skapad (Web, Core, Data, Tests)
- ✅ DbContext med SQL Server LocalDB
- ✅ User, Game, GamePlayer entities
- ✅ Entity Framework migrations
- ✅ Autentisering (registrering, login, logout)
- ✅ Session-hantering med ProtectedSessionStorage
- ✅ UI-layout med fantasy-tema
- ✅ Responsiv design med CSS-variabler

### Fas 2: Lobby & Spelhantering ✅ KLAR (100%)
- ✅ Lista aktiva spel (filtrera: Väntar, Pågår, Avslutade)
- ✅ Skapa nytt spel med scenario-val
- ✅ Välj antal spelare (1-4)
- ✅ Gå med i spel
- ✅ Lämna spel
- ✅ Hero-val i game lobby
- ✅ Starta spel (endast skapare, min antal spelare uppfyllt)
- ✅ SignalR Hub för realtidsuppdateringar
- ✅ Notifiera när spelare går med/lämnar
- ✅ Notifiera när spel startar

### Fas 3: Domänmodeller ✅ KLAR (100%)
- ✅ GameDefinitionService för alla JSON-filer
- ✅ HeroDefinition, ScenarioDefinition, CardDefinition
- ✅ SkillDefinition, UnitDefinition, EnemyDefinition
- ✅ MapTileDefinition, TacticsDefinition
- ✅ GameStateModel med alla spelkomponenter
- ✅ JSON-serialisering av GameState
- ✅ Spara/ladda GameState från databas
- ✅ GameStateInitializer för spelstart

### Fas 4: Spelmotor - Kärna ✅ DELVIS KLAR (85%)
#### 4.1 Turordning & Rundhantering ✅ KLAR
- ✅ Taktikkortval i början av runda
- ✅ Bestäm turordning baserat på taktik
- ✅ Dag/Natt-cykel (IsDay property)
- ⏳ Rundslut-hantering (delvis)
- ⏳ Shuffle deed deck vid rundslut (delvis)

#### 4.2 Korthantering ✅ KLAR
- ✅ Dra kort till handgräns
- ✅ Spela kort (basic/powered)
- ✅ Spela kort sideways
- ✅ Discard pile
- ✅ Wound-kort

#### 4.3 Mana-system ✅ KLAR
- ✅ Mana Source (tärningar)
- ✅ Använd tärning från source
- ✅ Kristaller i inventory (CrystalInventory)
- ✅ Mana tokens (ManaTokenInventory)
- ✅ Dag: Guld tillgängligt, Natt: Svart tillgängligt
- ✅ Reroll mana pool

#### 4.4 Effekt-system ⏳ DELVIS
- ✅ Move effekter (kort ger movement)
- ✅ Attack effekter (kort ger attack pool)
- ✅ Block effekter (kort ger block pool)
- ✅ Influence effekter
- ✅ Heal effekter
- ⏳ Komplett effekt-system med handlers (delvis implementerat)

### Fas 5: Karta & Rörelse ✅ DELVIS KLAR (90%)
#### 5.1 Kartrendering ✅ KLAR
- ✅ Hexagonal grid-system (TileMap-komponent)
- ✅ Rendera map tiles med faktiska bilder
- ✅ Visa terräng
- ✅ Visa platser (sites)
- ✅ Visa fiender
- ✅ Visa spelarpositioner
- ✅ Korrekta hex-storlekar baserat på tile-mått

#### 5.2 Rörelse ✅ KLAR
- ✅ Beräkna terrängkostnad (dag/natt)
- ✅ Validera rörelse
- ✅ GetValidMoves med pathfinding
- ⏳ Safe movement (ej implementerat)
- ⏳ Flight (ej implementerat)
- ⏳ Provocera rampaging enemies (ej implementerat)

#### 5.3 Exploration ✅ KLAR
- ✅ Avslöja nya tiles
- ✅ Använd faktiska tile-definitioner från JSON
- ✅ Placeringsregler (basic)
- ✅ Tile deck hantering (Countryside, Core, City tiles)

### Fas 6: Strid ✅ DELVIS KLAR (85%)
#### 6.1 Stridsfaser ✅ KLAR
- ✅ Swift Attack Phase (för Swift enemies)
- ✅ Ranged/Siege Attack Phase
- ✅ Block Phase
- ✅ Assign Damage Phase
- ✅ Attack Phase
- ✅ Resolution & Loot/Rewards

#### 6.2 Stridsmekanik ✅ KLAR
- ✅ Initiera strid (enter site, challenge enemy)
- ✅ Ranged attacks
- ✅ Siege attacks (mot Fortified)
- ✅ Blockering
- ✅ Elementär skada (Fire, Ice, Cold)
- ✅ Resistances (Physical, Fire, Ice)
- ✅ Armor & damage calculation
- ✅ Wounds (inklusive Poison wounds)

#### 6.3 Fiende-abilities ✅ KLAR
- ✅ Swift
- ✅ Fortified
- ✅ Poison
- ✅ Paralyze
- ✅ Brutal
- ⏳ Vampiric (ej implementerat)
- ⏳ Summon (delvis - logik finns men ej fullt testad)

#### 6.4 Enheter i strid ✅ KLAR
- ✅ Aktivera enhet (ActivateUnit i GameEngine)
- ✅ Enhetsskada (AssignDamageToUnit)
- ✅ Wounded units
- ✅ Ready/Exhausted state
- ✅ Unit abilities i strid (Attack, Block, Ranged, Siege, etc.)
- ✅ CombatPanel uppdaterad med unit-stöd

### Fas 7: Interaktioner & Platser ✅ DELVIS KLAR (75%)
#### 7.1 Site Interactions ✅ KLAR
- ✅ Village (Recruit, Heal, Plunder)
- ✅ Monastery (Recruit, Heal, Training)
- ✅ Mage Tower (Recruit spellcasters, Learn spell)
- ✅ Keep (Recruit if owned)
- ✅ Magical Glade (Heal, Empower)
- ✅ Crystal Mine (Harvest)
- ⏳ Conquered City (Recruit all, Buy fame) - ej implementerat
- ⏳ Burn monastery - ej implementerat
- ⏳ Cleanse (Glade) - ej implementerat

#### 7.2 Adventure Sites ✅ DELVIS
- ✅ Ruins (basic)
- ✅ Dungeon (night rules, brown enemy, artifact reward)
- ✅ Tomb (night rules, red enemy, artifact + spell reward)
- ✅ Monster Den (brown enemy, crystals reward)
- ✅ Spawning Grounds (2x brown, artifact reward)
- ✅ Draconum (red enemy, 2 artifacts reward)
- ✅ Orc Marauders (green enemy, fame reward)
- ⏳ Ruins tokens - ej implementerat

#### 7.3 Rekrytering ✅ KLAR
- ✅ Visa unit offer (via site interactions)
- ✅ Beräkna influence-kostnad med reputation
- ✅ Validera rekrytering mot plats
- ✅ Command token limit

### Fas 8: Leveling & Progression ✅ KLAR (100%)
#### 8.1 Fame & Leveling ✅ KLAR
- ✅ Spåra fame
- ✅ Level up triggers (automatisk för Command Token-nivåer)
- ✅ Välj Advanced Action vid level up
- ✅ Välj Skill vid level up
- ✅ Uppdatera armor & hand limit
- ✅ Command Tokens ökar med level

#### 8.2 Reputation ✅ KLAR
- ✅ Reputation track
- ✅ Influence modifiers baserat på reputation
- ✅ Reputation changes (plunder, etc.)

### Fas 9: Scenarios & Vinst ✅ DELVIS KLAR (75%)
#### 9.1 Scenario Setup ✅ KLAR
- ✅ Ladda scenario-konfiguration
- ✅ Generera map deck
- ✅ Placera starting tile
- ⏳ Sätt city levels (ej implementerat)

#### 9.2 Vinst-villkor ✅ KLAR
- ✅ Spåra scenario-mål (CitiesConquered, CityRevealed)
- ✅ Detektera vinst/förlust (CheckVictoryConditions)
- ✅ Beräkna slutpoäng (CalculateFinalScores)
- ✅ Visa resultat (VictoryScreen-komponent)

#### 9.3 Implementera scenarios ⏳ DELVIS
- ⏳ First Reconnaissance (Training) - ej implementerat
- ⏳ Full Conquest - ej implementerat
- ⏳ Blitz Conquest - ej implementerat
- ⏳ Solo Conquest - basic scenario setup finns
- ⏳ Cooperative - ej implementerat

### Fas 10: UI & Polish ⏳ DELVIS KLAR (60%)
#### 10.1 Spel-UI ✅ DELVIS
- ✅ Spelarens dashboard (hand, inventory, units)
- ✅ Interaktiv karta (TileMap)
- ⏳ Drag-and-drop kort (ej implementerat)
- ✅ Stridsdialog (CombatPanel - delvis)
- ✅ Site interaction dialog (SiteInteractionPanel - integrerad i PlayGame)
- ✅ Taktikval-dialog (TacticsSelection)

#### 10.2 Visuell feedback ⏳ DELVIS
- ⏳ Animationer (kort, rörelse, strid) - ej implementerat
- ⏳ Ljud-effekter - ej implementerat
- ⏳ Tooltips för kort och enheter - delvis
- ⏳ Undo-funktion (inom tur) - ej implementerat

#### 10.3 Responsivitet ⏳ DELVIS
- ✅ Desktop-optimerad
- ⏳ Tablet-stöd - delvis
- ⏳ Grundläggande mobilvy - delvis

### Fas 11: Multiplayer & Synk ✅ DELVIS KLAR (80%)
#### 11.1 SignalR Game Hub ✅ KLAR
- ✅ SignalR Hub för lobby (GameHub)
- ✅ Synka game state (real-time via SignalR)
- ✅ Turn notifications
- ✅ Chat (i spel)
- ✅ Reconnection handling (WithAutomaticReconnect)
- ✅ Player connection tracking

#### 11.2 Concurrency ⏳ EJ IMPLEMENTERAT
- ⏳ Optimistic locking
- ⏳ Conflict resolution
- ⏳ State validation

### Fas 12: Testing & Deployment ❌ EJ PÅBÖRJAT (0%)
#### 12.1 Testing ❌
- ❌ Unit tests för spellogik
- ❌ Integration tests för API
- ❌ E2E tests för kritiska flöden

#### 12.2 Deployment ❌
- ❌ Docker setup
- ❌ CI/CD pipeline
- ❌ Staging environment
- ❌ Production deployment

---

## Översiktlig status

| Fas | Status | Procent |
|-----|--------|---------|
| 1. Infrastruktur | ✅ Klar | 100% |
| 2. Lobby | ✅ Klar | 100% |
| 3. Domänmodeller | ✅ Klar | 100% |
| 4. Spelmotor | ✅ Delvis | 85% |
| 5. Karta & Rörelse | ✅ Delvis | 90% |
| 6. Strid | ✅ Klar | 95% |
| 7. Interaktioner | ✅ Delvis | 75% |
| 8. Leveling | ✅ Klar | 100% |
| 9. Scenarios | ✅ Delvis | 75% |
| 10. UI & Polish | ⏳ Delvis | 60% |
| 11. Multiplayer | ✅ Delvis | 80% |
| 12. Testing | ❌ Ej påbörjat | 0% |

**Total progress: ~90% av MVP**

---

## Nästa steg (Prioriterat)

### Hög prioritet (för spelbar MVP):
1. ✅ Enheter i strid (Fas 6.4) - KLAR
2. ✅ Vinst-villkor och scenario-mål (Fas 9.2) - KLAR
3. ✅ Integrera SiteInteractionPanel i PlayGame (Fas 10.1) - KLAR
4. ✅ Förbättra CombatPanel med alla abilities (Fas 10.1) - KLAR
5. ✅ Full SignalR-synkning för multiplayer (Fas 11.1) - KLAR

### Medel prioritet:
6. ⏳ Conquered City interactions (Fas 7.1)
7. ⏳ Ruins tokens (Fas 7.2)
8. ⏳ Safe movement & Flight (Fas 5.2)
9. ⏳ Vampiric ability (Fas 6.3)
10. ⏳ Drag-and-drop kort (Fas 10.1)

### Låg prioritet (nice-to-have):
11. ⏳ Animationer och visuell feedback (Fas 10.2)
12. ⏳ Undo-funktion (Fas 10.2)
13. ⏳ Testing (Fas 12)
14. ⏳ Deployment (Fas 12)

---

## Tekniska beslut ✅

| Beslut | Val | Motivering |
|--------|-----|------------|
| **Frontend** | Blazor Server | Enklare utveckling, real-time inbyggt |
| **Databas** | SQL Server (LocalDB för dev) | Azure-kompatibelt, Entity Framework stöd |
| **GameState** | JSON-kolumn i databas | Dynamisk struktur, flexibelt |
| **Statisk data** | Normaliserade tabeller | User, Game metadata |
| **Real-time** | SignalR | Inbyggt i Blazor Server |
| **Hosting** | Lokalt nu, Azure senare | Skalbart när det behövs |

### Datalagringsstrategi

**Databas-tabeller (strukturerad data):**
- `Users` - Username, Email, PasswordHash, CreatedAt
- `Games` - Id, Name, Status, CreatedByUserId, ScenarioId, MaxPlayers, CreatedAt
- `GamePlayers` - GameId, UserId, HeroId, JoinedAt, IsReady

**JSON-kolumner (dynamisk data):**
- `Games.GameState` - Hela spelstatet (karta, spelare, kort, etc.)
- `Games.Settings` - Scenario-specifika inställningar

**Statiska JSON-filer (definitioner):**
- Alla filer i `/spec/definitions/` - laddas vid uppstart, cachas i minnet
