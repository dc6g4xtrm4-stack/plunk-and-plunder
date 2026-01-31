# Collision Yield UI Fixes

## Issues Found

### 1. **Blank UI - No Local Player Units**
**Problem:** UI was showing even when no local player (human) units were involved in the collision
- All-AI collisions would show blank UI to human player
- UI appeared but had no buttons or content

**Fix:**
```csharp
// Added check to hide UI if no local player units
if (localPlayerUnits.Count == 0)
{
    Debug.Log("[CollisionYieldUI] No local player units in collision, hiding UI");
    HideModal();
    return;
}
```

### 2. **Too Many Elements - Improper Cleanup**
**Problem:** Previous collision UI elements weren't properly destroyed
- Unity's `Destroy()` is deferred, not immediate
- Old buttons from previous collisions remained visible
- Elements accumulated across multiple collisions

**Fix:**
```csharp
// Changed from Destroy() to DestroyImmediate()
private void ClearButtons()
{
    List<GameObject> toDestroy = new List<GameObject>();
    foreach (Transform child in buttonContainer.transform)
    {
        toDestroy.Add(child.gameObject);
    }

    foreach (GameObject obj in toDestroy)
    {
        DestroyImmediate(obj);  // ← Immediate destruction
    }

    yieldButtons.Clear();
    pushButtons.Clear();
    statusTexts.Clear();
}
```

### 3. **Null/Empty Collisions**
**Problem:** No null check when ShowCollisions is called

**Fix:**
```csharp
if (collisions == null || collisions.Count == 0)
{
    Debug.LogWarning("[CollisionYieldUI] ShowCollisions called with null or empty collisions list");
    HideModal();
    return;
}
```

### 4. **Multiple Modal Creation**
**Problem:** CreateModal could be called multiple times

**Fix:**
```csharp
public void Initialize(int playerId)
{
    // Only create modal if it doesn't exist
    if (modalPanel == null)
    {
        CreateModal();
    }
    HideModal();
}
```

## Changes Made to CollisionYieldUI.cs

### Added Comprehensive Debug Logging
```csharp
Debug.Log($"[CollisionYieldUI] ShowCollisions called with {collisions.Count} collision(s)");
Debug.Log($"[CollisionYieldUI] Collision at {collision.destination} with {collision.unitIds.Count} units");
Debug.Log($"[CollisionYieldUI] Unit {unitId} ownerId={unit.ownerId}, localPlayerId={localPlayerId}");
Debug.Log($"[CollisionYieldUI] Found {localPlayerUnits.Count} local player units in collisions");
Debug.Log($"[CollisionYieldUI] Showing modal with {yieldButtons.Count} units");
```

### Safety Checks Added
1. ✅ Null/empty collision list check
2. ✅ No local player units check (hide UI)
3. ✅ Prevent duplicate modal creation
4. ✅ Unit not found warnings
5. ✅ Proper cleanup with DestroyImmediate

## Changes Made to GameManager.cs

### Added Debug Logging
```csharp
Debug.Log($"[GameManager] Checking yield decisions: {state.collisionYieldDecisions.Count}/{allUnitsInCollisions.Count} units decided");
Debug.Log($"[GameManager] Still waiting for decision from unit {unitId}");
Debug.Log($"[GameManager] All yield decisions collected!");
```

## Expected Behavior

### Scenario 1: Human Player Units in Collision
1. Collision detected
2. UI shows with buttons for each human player ship
3. Player clicks YIELD or PUSH for each ship
4. UI auto-closes when all decisions made
5. Game continues with collision resolution

### Scenario 2: Only AI Units in Collision
1. Collision detected
2. AI makes decisions automatically
3. UI remains hidden (no human player units)
4. Game immediately continues with collision resolution

### Scenario 3: Mixed Human and AI Units
1. Collision detected
2. AI makes decisions immediately
3. UI shows only human player ships
4. Human makes decisions
5. Once all decisions collected, game continues

## Testing Checklist

- [ ] Human ship vs Human ship (same player)
- [ ] Human ship vs AI ship
- [ ] AI ship vs AI ship (UI should not show)
- [ ] Multiple human ships in one collision
- [ ] Multiple separate collisions in same turn
- [ ] Collision with 3+ ships
- [ ] UI cleanup between turns
- [ ] UI buttons work correctly
- [ ] Auto-close after all decisions made

## Debug Console Output

When testing, look for these log messages:

### Good Flow:
```
[CollisionYieldUI] ShowCollisions called with 1 collision(s)
[CollisionYieldUI] Collision at (5, 10) with 2 units: ship_0, ship_1
[CollisionYieldUI] Unit ship_0 ownerId=0, localPlayerId=0
[CollisionYieldUI] Added local player unit: ship_0
[CollisionYieldUI] Found 1 local player units in collisions
[CollisionYieldUI] Showing modal with 1 units
[GameManager] Unit ship_0 yield decision: True
[GameManager] AI Player 1 unit ship_1: pushing (HP: 10/10)
[GameManager] All yield decisions collected!
```

### AI-Only Collision (No UI):
```
[CollisionYieldUI] ShowCollisions called with 1 collision(s)
[CollisionYieldUI] Collision at (5, 10) with 2 units: ship_1, ship_2
[CollisionYieldUI] Unit ship_1 ownerId=1, localPlayerId=0
[CollisionYieldUI] Unit ship_2 ownerId=2, localPlayerId=0
[CollisionYieldUI] Found 0 local player units in collisions
[CollisionYieldUI] No local player units in collision, hiding UI
[GameManager] AI Player 1 unit ship_1: pushing (HP: 10/10)
[GameManager] AI Player 2 unit ship_2: yielding (HP: 4/10)
[GameManager] All yield decisions collected!
```

## Common Issues and Solutions

### Issue: "UI shows but is blank"
**Cause:** No local player units in collision
**Solution:** Fixed - UI now hides if no local player units

### Issue: "UI shows duplicate elements"
**Cause:** ClearButtons not destroying immediately
**Solution:** Fixed - Using DestroyImmediate now

### Issue: "UI doesn't close after clicking"
**Cause:** AllYieldDecisionsCollected not working correctly
**Solution:** Check debug logs to see which units haven't decided

### Issue: "Game hangs on collision"
**Cause:** Missing yield decision for some unit
**Solution:** Check logs - AI should auto-decide, ensure all units have decisions

## Files Modified

1. `Assets\Scripts\UI\CollisionYieldUI.cs`
   - Added null checks
   - Fixed button cleanup with DestroyImmediate
   - Hide UI if no local player units
   - Added comprehensive debug logging
   - Prevent multiple modal creation

2. `Assets\Scripts\Core\GameManager.cs`
   - Added debug logging to AllYieldDecisionsCollected
   - Shows which units are pending decisions
