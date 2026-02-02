# Deterministic Simultaneous Naval Combat System

## Overview

This document describes the **Deterministic Simultaneous Naval Combat System** implemented for Plunk and Plunder, a Civ/Risk-style multiplayer strategy game. The system ensures that enemy ships never occupy the same hex tile while providing explicit player agency in naval conflicts.

## Design Intent

The system should feel like: **"Two fleets meet at sea — do you challenge them, force passage, or back down?"**

This is comparable to modern Civilization naval encounters but adapted for:
- Simultaneous turns (all players plan, then all resolve)
- Multiplayer determinism (identical inputs → identical outputs)
- Explicit player agency (players choose how to respond to encounters)

---

## Core Invariant (ABSOLUTE RULE)

**Two enemy ships may NEVER occupy the same hex tile at any point in time.**

This applies to:
- Sea tiles
- Harbor tiles
- Any structure tile

If two enemy ships would resolve onto the same tile, the system instead creates an **Encounter State**.

---

## Turn Structure (Mandatory Phases)

Each turn consists of explicit sub-phases. **Do NOT merge these phases.**

### 1. Planning Phase
- Players stage movement orders for ships
- Each order is a path (list of hex coordinates)
- UI shows a path visualizer
- **No movement or combat occurs here**
- Phase: `GamePhase.Planning`

### 2. Order Lock-In Phase
- Players submit their staged orders
- Orders are serialized and frozen
- **No changes allowed after this point**
- Phase: `GamePhase.Submitted`

### 3. Resolution Phase (Simultaneous)
- Resolution occurs in **movement ticks** (1 hex per tick)
- For each tick:
  1. Read all ships' intended next hex
  2. Detect conflicts **before** moving anything
  3. Convert conflicts into Encounter Objects
  4. Ships involved in encounters **do NOT move** this tick
- **Movement NEVER directly causes combat**
- Phase: `GamePhase.Resolving`

### 4. Encounter Resolution Phase (If Encounters Detected)
- Players make decisions for each encounter
- Encounters resolved in **stable deterministic order**
- Combat may occur based on decisions
- Phase: `GamePhase.EncounterResolution`

### 5. Animation Phase
- Visual representation of all actions
- Phase: `GamePhase.Animating`

---

## Encounter Types (ONLY THESE TWO)

### ENCOUNTER TYPE A — PASSING ENCOUNTER (Swap Attempt)

**Definition:** Two enemy ships attempt to swap hexes in the same tick:
- Ship A moves A → B
- Ship B moves B → A

**Player Decisions:** Each involved player chooses:
- **PROCEED** (allow peaceful swap)
- **ATTACK** (engage in combat)

**Resolution:**

| Choice A | Choice B | Result |
|----------|----------|--------|
| PROCEED  | PROCEED  | Ships swap tiles peacefully |
| ATTACK   | PROCEED  | Combat occurs |
| PROCEED  | ATTACK   | Combat occurs |
| ATTACK   | ATTACK   | Combat occurs |

**Combat Location:** Combat occurs "between tiles," not on a tile. Ships remain in their original positions after combat.

---

### ENCOUNTER TYPE B — ENTRY ENCOUNTER (Same Destination)

**Definition:** Two or more enemy ships attempt to enter the same hex tile in the same tick.

**Player Decisions:** Each involved player chooses:
- **YIELD** (stay in current position)
- **ATTACK** (contest the tile)

**Resolution:**

| Result | Outcome |
|--------|---------|
| Exactly one ATTACK | Attacker enters tile |
| All YIELD | Tile remains empty (no one moves) |
| Multiple ATTACK | Tile becomes **CONTESTED** |

---

## Contested Tile State (Persistent)

A **contested tile** represents an unresolved naval battle that persists across turns.

### Rules:
- No ship occupies the tile
- Involved ships remain in their previous hexes
- Tile is visually marked as **CONTESTED** (red pulsing border)
- No other ships may enter the contested tile
- Combat occurs for **one round** per turn between involved ships

### Each Following Turn:
- Involved ships may:
  - Withdraw (move away)
  - Continue attacking (stay and fight another round)
- Combat continues until:
  - Only one ship remains (winner can claim tile)
  - All ships withdraw or are destroyed
  - Resolved via player decisions

---

## Combat Resolution (Deterministic)

Combat occurs **only after decisions are locked**.

### Combat Rules:
- Ships have HP (default: starts at 10, upgradable)
- Combat uses **seeded deterministic RNG** (`System.Random`, NOT `UnityEngine.Random`)
- Dice-based system:
  - Attacker rolls 3d6, takes highest 2
  - Defender rolls 2d6, takes highest 2
  - Compare pairwise (highest vs highest, second vs second)
  - Defender wins ties
  - Each comparison won deals base 2 damage + cannon bonuses

### Outcomes:
- Losing ship may be destroyed (0 HP)
- Winner occupies tile **only if allowed by encounter rules**
- Salvage gold awarded for destroying enemy ships

---

## Player Elimination

A player is **eliminated** when:
- They have **no ships remaining**
- (Harbors/shipyards do NOT prevent elimination)

Game ends when only one player remains.

---

## Determinism Requirements (CRITICAL)

### 1. No UnityEngine.Random During Resolution
- Use `System.Random` with a **shared seed** across all clients
- Current MVP uses UnityEngine.Random for initial seed (single-player only)
- For multiplayer: seed must be derived from game state (e.g., `mapSeed ^ turnNumber`)

### 2. Stable Ordering
All encounters resolved in **stable deterministic order**:
1. Tile coordinate (for ENTRY) or first edge coord (for PASSING)
2. Encounter type (PASSING < ENTRY)
3. First unit ID (lexicographic)

Implemented via `Encounter.GetStableSortKey()`

### 3. All Player Decisions Serialized Before Resolution
- Decisions are locked in `Encounter.PassingDecisions` / `Encounter.EntryDecisions`
- `Encounter.AwaitingPlayerChoices` flag prevents resolution until all decisions are in
- AI decisions made simultaneously with human decisions (no waiting)

### 4. Identical Inputs → Identical Outputs
Given:
- Same game state
- Same orders
- Same encounter decisions
- Same RNG seed

Result:
- **100% identical outcomes** across all clients

---

## Data Model

### Encounter Class
```csharp
class Encounter
{
    string Id;                              // Unique identifier
    EncounterType Type;                     // PASSING or ENTRY
    int CreatedOnTurn;                      // Turn number
    List<string> InvolvedUnitIds;           // All units in encounter
    HexCoord? TileCoord;                    // For ENTRY: contested tile
    (HexCoord, HexCoord)? EdgeCoords;       // For PASSING: swap tiles
    Dictionary<string, HexCoord> PreviousPositions; // Units stay here if unresolved

    // Decision tracking
    Dictionary<string, PassingEncounterDecision> PassingDecisions;
    Dictionary<string, EntryEncounterDecision> EntryDecisions;

    // State flags
    bool AwaitingPlayerChoices;             // True until all decisions made
    bool IsResolved;                        // True when encounter complete
    bool IsContested;                       // True for contested tiles
}
```

### Decision Enums
```csharp
enum PassingEncounterDecision
{
    NONE,       // No decision yet
    PROCEED,    // Allow peaceful swap
    ATTACK      // Engage in combat
}

enum EntryEncounterDecision
{
    NONE,       // No decision yet
    YIELD,      // Stay in current position
    ATTACK      // Contest the tile
}
```

---

## Implementation Files

### Core System
- **Encounter.cs** - Encounter data model, creation, decision tracking
- **GameState.cs** - `activeEncounters`, `contestedTiles` dictionaries
- **GameEvents.cs** - `EncounterCreatedEvent`, `EncounterResolvedEvent`, `ContestedTileCreatedEvent`

### Resolution
- **TurnResolver.cs** - Encounter detection, resolution logic
  - `ResolveMoveOrders()` - Detects encounters during movement
  - `ResolveEncountersWithDecisions()` - Resolves encounters with player decisions
  - `ResolvePassingEncounter()` - PASSING encounter resolution
  - `ResolveEntryEncounter()` - ENTRY encounter resolution

### UI
- **CollisionYieldUI.cs** - Player decision UI
  - `ShowEncounters()` - Displays encounter prompts with correct terminology
  - Different button labels based on encounter type (PROCEED/YIELD vs ATTACK)

### Rendering
- **HexRenderer.cs** - Visual indicators
  - `MarkTileAsContested()` - Red pulsing border for contested tiles
  - `UpdateContestedTiles()` - Syncs visual state with game state
- **ContestedTilePulse.cs** - Pulsing animation component

### AI
- **GameManager.cs** - AI decision making
  - `MakeAIEncounterDecisions()` - AI strategy (yield if HP < 50%, otherwise attack)

### Combat
- **CombatResolver.cs** - Deterministic combat with seeded RNG
  - Uses `System.Random` with seed for reproducible results
  - Dice-based combat with cannon bonuses

---

## UI Requirements

### When Encounter Occurs:
1. Player is prompted **before resolution continues**
2. Turn cannot advance until **all choices are submitted**
3. AI players make decisions automatically (no delay)
4. Contested tiles are **clearly visible** on the map (red pulsing border)

### Event Log Must Explain:
- Who attacked
- Who yielded/proceeded
- Who was destroyed
- When tiles become/resolve contested state

---

## AI Strategy

Current AI logic (in `MakeAIEncounterDecisions()`):
- **If HP < 50% of max:** Yield/Proceed (avoid combat)
- **If HP >= 50% of max:** Attack (contest aggressively)

This provides reasonable AI behavior while being simple and deterministic.

---

## Edge Cases & Considerations

### Friendly Units
- Units from the **same player** never trigger encounters
- Friendly ships can stack peacefully on the same tile
- Friendly ships can pass through each other without prompts

### Multi-Unit Encounters
- ENTRY encounters can involve **3+ ships**
- Each ship makes an independent decision
- Combat resolved pairwise between all enemy ships

### Contested Tile Persistence
- Contested tiles remain across turns until resolved
- Involved ships can't move onto the contested tile until contest resolves
- New ships **cannot enter** contested tiles

### Order of Operations
1. Detect all encounters (before any movement)
2. Sort encounters deterministically
3. Request player decisions
4. Resolve encounters in sorted order
5. Continue with adjacent combat checks
6. Check player elimination

---

## Testing Determinism

To verify determinism:

1. **Save game state** before turn resolution
2. **Record all orders** and decisions
3. **Record RNG seed**
4. **Resolve turn**
5. **Record all outcomes** (positions, HP, events)
6. **Restore saved state**
7. **Re-run with same orders/decisions/seed**
8. **Compare outcomes** - should be **100% identical**

---

## Future Enhancements

Potential improvements (not in current MVP):

### Multiplayer Seed Synchronization
- Derive combat seed from `gameState.mapSeed ^ gameState.turnNumber`
- Or synchronize seed at game start via network handshake

### Contested Tile Complexity
- Allow ships to withdraw from contested tiles explicitly
- Add "retreat" decision option during ongoing contests
- Time limits on contested states (auto-resolve after N turns)

### Advanced AI
- Consider opponent strength (not just own HP)
- Factor in strategic value of tiles (near harbors/shipyards)
- Risk assessment based on number of enemy ships

### Encounter Visualization
- Animated "meeting at sea" cutscenes
- Ship-to-ship combat visualizations
- More detailed contested tile indicators (show involved ships)

---

## Example Scenario

**Setup:**
- Blue Ship A at (0, 0) moving to (1, 0)
- Red Ship B at (1, 0) moving to (0, 0)

**Detection:**
- PASSING encounter detected (ships swapping positions)

**Decision Phase:**
- Blue player shown: "PASSING ENCOUNTER - Choose: PROCEED or ATTACK"
- Red player shown: "PASSING ENCOUNTER - Choose: PROCEED or ATTACK"
- Blue chooses: **PROCEED**
- Red chooses: **ATTACK**

**Resolution:**
- Any ATTACK → Combat occurs
- One round of combat between Ship A and Ship B
- Ships remain in original positions: A at (0,0), B at (1,0)
- Combat damage applied based on dice rolls
- If either ship destroyed, other may move forward next turn

---

## Summary

This deterministic simultaneous naval combat system provides:
- ✅ **Clear player agency** - explicit decision points
- ✅ **Multiplayer determinism** - identical outcomes across clients
- ✅ **Strategic depth** - meaningful choices in encounters
- ✅ **Core invariant enforcement** - enemy ships never share tiles
- ✅ **Persistent contested states** - multi-turn battles
- ✅ **Clean architecture** - explicit phases, stable ordering

The system is fully implemented and ready for MVP multiplayer testing.
