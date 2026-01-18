# Mage Knight Online - Ändringslogg

## Version 1.1.1 (2026-01-19)

### Buggfixar

#### 🗺️ Movement över Map Tiles
- **Problem:** Spelaren kunde inte flytta till hexar på nyexploerade tiles
- **Orsak:** Rotationslogiken i `GenerateTileHexesWithRotation()` använde fel mappning
  - Positionerna 1-6 var inte i cirkulär ordning i `positionToDirection`
  - Vid rotation mappades hexar till fel världskoordinater
- **Lösning:** 
  - Ny mappning: `positionToDirectionIndex` konverterar tile-position till `HexDirections`-index
  - Rotation sker nu på direction-index istället för position-index
  - Explicit kontroll säkerställer att "anslutningshexen" (som förbinder tiles) alltid läggs till
  - Förbättrad loggning av vilka hexar som skapas vid tile-placement

#### 💡 Movement Highlight-uppdatering
- **Problem 1:** Highlight låg kvar på hexar trots att movement points var slut
- **Lösning:** 
  - `UpdateValidTileMoves()` anropas nu efter varje flytt
  - `StateHasChanged()` anropas explicit efter highlight-uppdateringar

- **Problem 2:** Highlight uppdaterades inte när nya movement points köptes
- **Lösning:** Lade till `UpdateValidMoves()` och `UpdateValidTileMoves()` anrop i:
  - `PlayCard()` - vanliga korteffekter
  - `PlayCardPowered()` - powered korteffekter
  - `UseSidewaysCard()` - kort använda sideways för +1 bonus
  - `ResolveChoice()` - val-resolutioner (mana, effekttyp, etc.)

### Tekniska Förbättringar

#### Tile Rotation Algorithm
```
Före (fel):
  RotateTilePosition(position, rotation) roterade position-index
  Men positionToDirection hade icke-cirkulär ordning:
  1→East, 2→NW, 3→NE, 4→West, 5→SE, 6→SW

Efter (korrekt):
  Konverterar position till direction-index först:
  positionToDirectionIndex = [-1, 0, 2, 1, 3, 5, 4]
  Roterar direction-index i HexDirections-arrayen:
  HexDirections: [East(0), NE(1), NW(2), West(3), SW(4), SE(5)]
```

#### UI Responsiveness
- Alla state-ändrande operationer anropar nu `StateHasChanged()`
- SignalR-notifieringar (`NotifyGameStateChanged()`) synkar highlight mellan klienter

---

## Version 1.1.0 (2026-01-18)

### Nya Funktioner

#### 🔔 Turbaserade Notifikationer
- **NotificationService** (`Services/NotificationService.cs`)
  - Hanterar in-app notifikationer för spelarhändelser
  - Stödjer olika typer: `YourTurn`, `GameStarted`, `GameEnded`
  - Spårar olästa notifikationer med räknare

- **NotificationPanel** (`Components/Shared/NotificationPanel.razor`)
  - Klockikon i navbar med badge för olästa meddelanden
  - Pulsande animation när nya notifikationer finns
  - Dropdown-panel med notifikationslista
  - Klicka på notifikation navigerar direkt till spelet

- **Browser Notifications** (`wwwroot/js/notifications.js`)
  - Desktop-notifikationer via Web Notification API
  - Automatisk tillståndsbegäran
  - Fungerar även när användaren är på annan flik/sida

- **SignalR-utökningar** (`Hubs/GameHub.cs`)
  - `RegisterUser()` - registrerar användare för globala notifikationer
  - `NotifyUserTurn()` - skickar notifikation till specifik användare
  - `NotifyUser()` - generella spelnotifikationer

#### ⏪ Undo-funktion
- Spelare kan ångra sina drag så länge ingen ny information har avslöjats
- **Blockeras när:**
  - Ny map tile har placerats (exploration)
  - Kort dragna från delade decks (Advanced Actions, Spells, Artifacts, Ruins tokens)
- **Implementering:**
  - `UndoStack` och `CanUndo` sparas i `GameStateModel` för persistens
  - `SaveStateForUndo()` sparar state före kritiska actions
  - `MarkIrreversibleAction()` blockerar undo efter exploration/card draws
  - `ResetUndoForNewTurn()` återställer undo-möjlighet vid ny tur

#### 🎯 Förbättrad Lobby
- **Visa pågående spel** - ny sektion "🎮 Your Games" i lobbyn
  - Visar alla spel du deltar i (väntande och pågående)
  - Separerad från "🌐 Available Games"
  
- **Din tur-indikation**
  - ⚡ "YOUR TURN" badge vid spelets namn
  - Pulsande guldram runt spel där det är din tur
  - Gyllene "▶ Play Now" knapp istället för "Continue"
  - Status badges: "🎯 In Progress" (grön) / "⏳ Waiting" (orange)

#### 🃏 Förenklad Taktikval
- Klick på tactic card väljer och bekräftar direkt
- Borttagen "Confirm Selection"-knapp
- Snabbare spelflöde

### Buggfixar

#### 🗺️ Map Tile / Rörelselogik
- **Problem:** Spelaren kunde bara gå till 2 av 4 närliggande plains-hexar
- **Orsak:** `GameStateInitializer.cs` hade hårdkodade hexvärden som inte matchade `map_tiles.json`
- **Lösning:** Läser nu direkt från JSON-filen för startile
  ```
  Gammal (felaktig):           Ny (korrekt):
  (1,0) = Plains               (1,0) = Water
  (0,-1) = Water               (0,-1) = Forest
  (-1,0) = Water               (-1,0) = Plains
  (0,1) = Plains               (0,1) = Water
  ```

#### 🔄 Site Interactions
- **Problem:** Kunde använda site-interaktioner (t.ex. mine crystals) om och om igen
- **Lösning:** 
  - `UsedSiteInteractions` lista i `PlayerState` spårar använda interaktioner
  - Kontrollerar `SiteInteraction.Repeatable` property
  - Återställs vid ny tur i `ResetPlayerTurnState()`

#### ⏹️ End Turn
- **Problem:** "End Turn"-knappen gav ingen feedback vid fel
- **Lösning:**
  - Explicit fas-kontroller i `GameEngine.EndTurn()`
  - Felmeddelanden visas via `alert()` i UI
  - Blockerar avslut under combat eller tactics selection

### Tekniska Förbättringar

#### SignalR
- GameLobby har nu SignalR-stöd för realtidsuppdateringar
- Notifierar alla spelare när spel startar
- Notifierar nästa spelare vid turslut och efter taktikval

#### State Management
- Undo-stack persisteras i `GameStateModel` istället för instansvariabler
- Säkerställer korrekt state över server-restarts

---

## Version 1.0.0 (Initial Release)

### Kärnfunktionalitet
- Komplett spelmotor för Mage Knight
- 10 scenarios implementerade
- Turordning med taktikkort
- Dag/Natt-cykel
- Mana-system med source dice och kristaller

### Strid
- Alla stridsfaser (Ranged → Block → Damage → Attack)
- Fiende-abilities: Swift, Fortified, Poison, Paralyze, Brutal, Vampiric, Summon
- Enheter i strid med abilities

### Karta & Rörelse
- Hexagonal grid-system
- Terrängkostnader (dag/natt)
- Exploration av nya tiles
- Safe movement och Flight

### Site Interactions
- Village, Monastery, Mage Tower, Keep, Magical Glade, Crystal Mine
- Adventure sites: Dungeon, Tomb, Monster Den, Spawning Grounds, Ruins
- Burn Monastery, Cleanse Glade
- City conquest och interactions

### Leveling
- Fame-tracking
- Level up med Advanced Actions och Skills
- Reputation-system

### Multiplayer
- SignalR för realtidskommunikation
- Lobby-system
- Chat i spel

### UI
- Interaktiv hexkarta
- Korthand med drag-and-drop
- Combat panel
- Site interaction panel
- Victory screen
