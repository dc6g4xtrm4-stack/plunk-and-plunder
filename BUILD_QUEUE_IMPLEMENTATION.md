# Build Queue System Implementation

## Overview
Implemented a Starcraft-style build queue system for shipyards with 3-turn ship construction and visual UI feedback.

## Implementation Summary

### 1. Created BuildQueueItem.cs
**Location:** `Assets/Scripts/Structures/BuildQueueItem.cs`

A serializable class that tracks individual items in the build queue:
- `itemType`: Type of item being built (e.g., "Ship")
- `turnsRemaining`: Number of turns until completion (3, 2, 1, 0)
- `cost`: Gold cost of the item

### 2. Updated Structure.cs
**Location:** `Assets/Scripts/Structures/Structure.cs`

Added build queue support to structures:
- Added `List<BuildQueueItem> buildQueue` field
- Imported `System.Collections.Generic` namespace

### 3. Updated BuildingConfig.cs
**Location:** `Assets/Scripts/Structures/BuildingConfig.cs`

Added build queue configuration constants:
- `SHIP_BUILD_TIME = 3`: Ships take 3 turns to build
- `MAX_QUEUE_SIZE = 5`: Maximum 5 items in queue
- Kept `BUILD_SHIP_COST = 50` (already set correctly)

### 4. Created BuildQueueUI.cs
**Location:** `Assets/Scripts/UI/BuildQueueUI.cs`

Complete UI system for displaying build queue:
- Shows 5 queue slots per shipyard
- Visual progress display (0/3, 1/3, 2/3, 3/3)
- Color-coded slots:
  - Green: Currently building (slot 1)
  - Yellow: Queued items (slots 2-5)
  - Gray: Empty slots
- Auto-updates when queue changes
- Shows/hides based on shipyard selection

### 5. Updated TurnResolver.cs
**Location:** `Assets/Scripts/Resolution/TurnResolver.cs`

#### Added ProcessBuildQueues() Method
Called at the start of each turn to:
- Iterate through all shipyards
- Decrement `turnsRemaining` for first queue item
- Spawn ship when `turnsRemaining` reaches 0
- Remove completed item from queue
- Generate `ShipBuiltEvent` for completed ships

#### Modified ResolveBuildShipOrders() Method
Changed from instant ship spawn to queue-based system:
- Checks if queue has space (max 5 items)
- Deducts gold immediately when queuing
- Creates `BuildQueueItem` with 3 turns remaining
- Adds item to shipyard's build queue
- Generates `ShipQueued` event instead of instant `ShipBuilt`

### 6. Updated GameHUD.cs
**Location:** `Assets/Scripts/UI/GameHUD.cs`

Integrated build queue UI into main HUD:
- Added `BuildQueueUI buildQueueUI` field
- Initializes BuildQueueUI in CreateLayout()
- Shows queue when shipyard is selected
- Hides queue when selection changes
- Updated OnBuildShipClicked() to check queue capacity
- Shows queue position feedback (e.g., "Ship queued! (2/5)")

### 7. Updated GameEvents.cs
**Location:** `Assets/Scripts/Core/GameEvents.cs`

Added new event type:
- `ShipQueued`: Fired when ship is added to build queue (distinct from `ShipBuilt` which fires when ship completes)

## How It Works

### Player Queues a Ship
1. Player selects shipyard and clicks "Build Ship" button
2. System checks:
   - Queue has available slots (< 5)
   - Player has enough gold (50)
3. Gold is deducted immediately
4. New `BuildQueueItem` created with 3 turns remaining
5. Item added to shipyard's `buildQueue`
6. UI updates to show queued item

### Turn Processing
1. At start of each turn, `ProcessBuildQueues()` runs
2. For each shipyard with items in queue:
   - First item's `turnsRemaining` decrements by 1
   - When `turnsRemaining` reaches 0:
     - New ship spawns at shipyard location
     - `ShipBuiltEvent` generated
     - Item removed from queue
     - Next queued item (if any) becomes active

### Visual Feedback
The BuildQueueUI displays:
```
BUILD QUEUE
Building: Ship (2/3)     [Green background]
Queued: Ship (0/3)       [Yellow background]
Slot 3: Empty            [Gray background]
Slot 4: Empty            [Gray background]
Slot 5: Empty            [Gray background]
```

## Key Features

1. **3-Turn Build Time**: Ships take exactly 3 turns to complete
2. **5-Slot Queue**: Can queue up to 5 ships per shipyard
3. **Immediate Payment**: Gold deducted when queuing (not when completing)
4. **Visual Progress**: Clear display of build progress for each slot
5. **Automatic Processing**: Queue advances automatically each turn
6. **Queue Management**: First-in-first-out queue system

## Testing Checklist

- [ ] Ship queuing costs 50 gold
- [ ] Ships take exactly 3 turns to complete
- [ ] Queue displays correctly (5 slots)
- [ ] Progress updates each turn (3/3 -> 2/3 -> 1/3 -> spawned)
- [ ] Multiple ships can be queued
- [ ] Queue full message appears at 5/5
- [ ] Ships spawn at correct location when complete
- [ ] Gold is deducted when queuing, not when spawning
- [ ] UI shows/hides correctly based on selection

## Files Modified
1. `Assets/Scripts/Structures/BuildQueueItem.cs` (NEW)
2. `Assets/Scripts/Structures/BuildQueueItem.cs.meta` (NEW)
3. `Assets/Scripts/UI/BuildQueueUI.cs` (NEW)
4. `Assets/Scripts/UI/BuildQueueUI.cs.meta` (NEW)
5. `Assets/Scripts/Structures/Structure.cs` (MODIFIED)
6. `Assets/Scripts/Structures/BuildingConfig.cs` (MODIFIED)
7. `Assets/Scripts/Resolution/TurnResolver.cs` (MODIFIED)
8. `Assets/Scripts/UI/GameHUD.cs` (MODIFIED)
9. `Assets/Scripts/Core/GameEvents.cs` (MODIFIED)

## Configuration Constants
All configuration is in `BuildingConfig.cs`:
- `BUILD_SHIP_COST = 50`
- `SHIP_BUILD_TIME = 3`
- `MAX_QUEUE_SIZE = 5`

Adjust these values to change game balance.
