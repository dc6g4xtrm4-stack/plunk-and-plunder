# Simultaneous Movement Animation Update

## Problem
Ships were moving sequentially in the animation:
- Ship A moves tiles 1, 2, 3
- Ship B moves tiles 1, 2, 3
- Ship C moves tiles 1, 2, 3

This made it look like ships were taking turns instead of moving simultaneously.

## Solution
Modified TurnAnimator to animate ships step-by-step together:
- **Step 1:** All ships move to tile 1 simultaneously
- **Step 2:** All ships move to tile 2 simultaneously
- **Step 3:** All ships move to tile 3 simultaneously

## Changes Made to TurnAnimator.cs

### 1. Modified AnimateEventsCoroutine
**Before:**
```csharp
foreach (GameEvent gameEvent in events)
{
    case UnitMovedEvent moveEvent:
        yield return AnimateMovement(moveEvent, state); // Animates one ship at a time
        break;
    // ... other events
}
```

**After:**
```csharp
// Separate movement events from other events
List<UnitMovedEvent> moveEvents = new List<UnitMovedEvent>();
List<GameEvent> otherEvents = new List<GameEvent>();

foreach (GameEvent gameEvent in events)
{
    if (gameEvent is UnitMovedEvent moveEvent)
        moveEvents.Add(moveEvent);
    else
        otherEvents.Add(gameEvent);
}

// Animate ALL movements simultaneously (step-by-step)
if (moveEvents.Count > 0)
{
    yield return AnimateSimultaneousMovement(moveEvents, state);
}

// Then animate other events
foreach (GameEvent gameEvent in otherEvents)
{
    // ... handle other events
}
```

### 2. New Method: AnimateSimultaneousMovement

```csharp
private IEnumerator AnimateSimultaneousMovement(List<UnitMovedEvent> moveEvents, GameState state)
```

**How it works:**
1. **Build path dictionary:** Collect all unit paths from move events
2. **Find max path length:** Determine the longest path among all units
3. **Step-by-step animation:**
   - Loop from step 0 to max path length
   - For each step, move ALL units that have a position at that step index
   - Fire `OnAnimationStep` event to update rendering
   - Wait `hexStepDelay` (default 0.25s) before next step

**Example with 3 ships:**
```
Ship A path: [A1, A2, A3, A4]        (4 steps)
Ship B path: [B1, B2]                (2 steps)
Ship C path: [C1, C2, C3]            (3 steps)

Animation sequence:
Step 0: Move A→A1, B→B1, C→C1 [wait 0.25s]
Step 1: Move A→A2, B→B2, C→C2 [wait 0.25s]
Step 2: Move A→A3, C→C3       [wait 0.25s]
Step 3: Move A→A4             [wait 0.25s]
```

## Animation Flow

### Turn Processing:
1. **Game Logic:** All orders resolved simultaneously (already working)
2. **Animation Phase:**
   - Collect all UnitMovedEvents
   - Animate them step-by-step together
   - Then animate combat, destruction, and other events
3. **Back to Planning:** Next turn begins

### Visual Result:
- All ships appear to move in formation
- Ships with longer paths continue moving after others stop
- Creates a more realistic simultaneous movement effect
- Maintains the same timing (0.25s per hex step)

## Technical Details

### Key Variables:
- `unitPaths`: Dictionary mapping unit ID to their movement path
- `unitCurrentStep`: Tracks which step index each unit is on
- `maxPathLength`: The longest path among all moving units

### Performance:
- O(n*m) where n = number of units, m = max path length
- Same total animation time as before
- No performance impact on large maps

## Benefits

1. ✅ **Visual clarity:** Players can see all movements happening together
2. ✅ **Better game feel:** Reinforces simultaneous turn resolution
3. ✅ **Collision awareness:** Makes it obvious when ships will collide
4. ✅ **Strategic feedback:** Players can see how their moves interact
5. ✅ **Maintains timing:** No change to total animation duration

## Testing Checklist

- [ ] Multiple ships moving different distances
- [ ] Ships starting from different positions
- [ ] Ships with 1-tile moves vs 5-tile moves
- [ ] Single ship movement (edge case)
- [ ] Ships that don't move at all
- [ ] Collision scenarios
- [ ] Combat after movement
- [ ] Multi-turn paths (partial movements)

## Configuration

Animation timing can be adjusted in TurnAnimator:
```csharp
public float hexStepDelay = 0.25f;     // Time per hex step
public float combatPauseDelay = 0.5f;  // Pause for combat
public float eventPauseDelay = 0.3f;   // Pause for events
```

Increase `hexStepDelay` for slower, more observable movement.
Decrease for faster-paced gameplay.
