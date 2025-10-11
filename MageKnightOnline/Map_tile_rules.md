# 🗺️ Mage Knight – Map Tile Logic Specification

Version: 1.0  
Source: Official Mage Knight Ultimate Edition Rulebook (2018)  
Focus: Map Tile placement, exploration, and interaction logic.

---

## 1. Overview

The **Map System** is a hex-based world builder composed of **Map Tiles**.  
Each **Map Tile** contains **7 interactive hexes** (hex spaces).  
Tiles are revealed, placed, and explored dynamically as players move across the map.

Cursor AI should use this document to implement deterministic and rule-accurate tile logic.

---

## 2. Core Entities

### 2.1 HexSpace

A single playable space on the board.

| Property | Type | Description |
|-----------|------|-------------|
| `id` | string | Unique identifier (e.g. `hex_3_5_A`). |
| `q`, `r` | number | Axial hex coordinates (map-level). |
| `terrainType` | enum | `PLAIN`, `FOREST`, `HILL`, `DESERT`, `LAKE`, `MOUNTAIN`, `SWAMP`, etc. |
| `site` | object or null | Optional reference to a map site (village, monastery, ruin, etc.). |
| `occupant` | object or null | Can hold hero, unit, rampaging enemy, etc. |
| `isAccessible` | boolean | Derived from terrain + day/night conditions. |

---

### 2.2 MapTile

A composite of **7 HexSpaces** arranged in a fixed pattern.

| Property | Type | Description |
|-----------|------|-------------|
| `tileId` | string | Unique tile reference (e.g. `COUNTRYSIDE_04`). |
| `tileType` | enum | `STARTING`, `COUNTRYSIDE`, `CORE_NON_CITY`, `CORE_CITY`. |
| `hexes` | HexSpace[7] | The 7 internal hex spaces of the tile. |
| `position` | object | Tile center coordinates in map space. |
| `orientation` | number | Always the same as the starting tile (rotation locked). |
| `adjacentTiles` | string[] | List of neighboring tile IDs. |
| `revealed` | boolean | Whether the tile is currently visible. |

---

### 2.3 MapGraph

The overall game map.

| Property | Type | Description |
|-----------|------|-------------|
| `tiles` | Map\<tileId, MapTile\> | All placed tiles. |
| `edges` | Array<[tileA, tileB]> | Adjacency links. |
| `coastlineMask` | function | Defines restricted placement zones (from scenario). |

---

## 3. Tile Categories

| Type | Back Color | Usage | Placement Rule |
|------|-------------|--------|----------------|
| **Starting Tile** | Neutral | Always in play | Defines base orientation (A or B layout). |
| **Countryside Tile** | Green | Early exploration | Must touch ≥2 existing tiles OR a tile that touches ≥2 others. |
| **Core Tile (non-city)** | Brown | Late exploration | Must touch ≥2 existing tiles. |
| **Core City Tile** | Brown + City symbol | Endgame zones | Same as Core, but spawns City object when revealed. |

---

## 4. Exploration Logic

### 4.1 Preconditions

A player may **explore (reveal) a new tile** if:

1. Their current hex space **borders an empty tile slot** (a “placement position”).
2. The empty slot is **allowed** by the coastline and adjacency mask.
3. The player **pays 2 Move Points**.

### 4.2 Procedure (step-by-step)

1. **Declare exploration** → Choose one valid empty border position.
2. **Pay 2 Move Points**.
3. **Draw top tile** from the current map deck:
   - If Countryside tiles remain → draw from Countryside deck.
   - Otherwise → draw from Core deck.
4. **Orient the tile** identically to the starting tile (rotation is fixed).
5. **Validate placement:**
   - Must be adjacent to at least 2 other placed tiles, OR
   - Adjacent to a tile that itself is adjacent to ≥2 others.
   - Cannot violate coastline mask.
6. **Place tile** → Update MapGraph (create edges between connected tiles).
7. **Trigger reveal effects**:
   - For each site on the new tile, execute its `onReveal()` rule.
   - If a Monastery is revealed → add 1 Advanced Action card to the Unit Offer.
8. **If City Tile:**
   - Instantiate corresponding `City` object.
   - Assign City Level from scenario definition.
   - Spawn city enemies (according to City Level and base color).
   - Register the City in the global `sites` registry.

---

## 5. Placement Validation

Implement a validator function:

```ts
function validateTilePlacement(tile, position, mapGraph, scenarioRules): boolean
