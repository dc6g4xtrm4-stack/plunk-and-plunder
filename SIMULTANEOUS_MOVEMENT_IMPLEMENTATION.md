# Simultaneous Movement & Collision Yield System Implementation

## Overview
Implemented a simultaneous movement system where all ships move at once based on queued inputs. When collisions occur, players are prompted to make yield decisions, determining the outcome.

## Key Changes

### 1. Game State Changes (GameState.cs)
- Added `CollisionResolution` phase to `GamePhase` enum
- Added collision tracking:
  - `pendingCollisions` - List of detected collisions
  - `collisionYieldDecisions` - Dictionary tracking each unit's yield decision
- Added `CollisionInfo` class to store collision details:
  - Unit IDs involved
  - Destination hex
  - Movement paths for each unit

### 2. Turn Resolution Changes (TurnResolver.cs)

#### Modified Movement Resolution
- `ResolveMoveOrders()` now detects collisions and returns early with collision events
- No longer automatically blocks units; instead creates `CollisionNeedsResolutionEvent`
- Returns control to GameManager for yield decision collection

#### New Methods
- `ResolveCollisionsWithYieldDecisions()` - Resolves collisions based on player decisions:
  - **Both yield**: Neither moves
  - **One yields**: Non-yielding unit moves
  - **Neither yields**: Both move to collision point, combat is triggered
  - **Multiple don't yield**: All move, combat between all non-yielding units

- `ResolveCombatAfterMovement()` - Called after collision resolution to check for adjacent enemies
- `ExecuteUnitMove()` - Helper to execute a single unit's movement
- `ResolveCombatAtLocation()` - Triggers combat between multiple units at the same location
- `ApplyCombatResult()` - Applies damage and handles unit destruction

### 3. Game Events (GameEvents.cs)
Added three new event types:
- `CollisionDetectedEvent` - Notifies when collision is detected
- `CollisionNeedsResolutionEvent` - Contains collision info for UI display
- `CollisionResolvedEvent` - Records how collision was resolved

### 4. Game Manager Changes (GameManager.cs)

#### New Field
- `collisionYieldUI` - UI component for collecting yield decisions

#### Modified Resolution Flow
- `ResolveCurrentTurn()` checks for collision events
- If collisions detected:
  1. Store collisions in `state.pendingCollisions`
  2. Transition to `CollisionResolution` phase
  3. Show `CollisionYieldUI` for human players
  4. Auto-submit AI yield decisions

#### New Methods
- `SubmitYieldDecision(unitId, isYielding)` - Called by UI when player makes decision
- `AllYieldDecisionsCollected()` - Checks if all players have decided
- `ContinueResolutionWithYieldDecisions()` - Resumes turn resolution after decisions
- `MakeAIYieldDecisions()` - Auto-generates AI decisions (yield if HP < 50%)

### 5. Collision Yield UI (CollisionYieldUI.cs)
New UI component that:
- Shows all local player's ships involved in collisions
- Displays collision location and opponent count
- Provides "YIELD" and "PUSH THROUGH" buttons for each ship
- Auto-submits decisions to GameManager
- Closes when all local player decisions are made

Features:
- Color-coded buttons (blue for yield, red for push)
- Shows ship health and collision details
- Disables buttons after decision made
- Status text shows current decision

### 6. Turn Animator Changes (TurnAnimator.cs)
Added handlers for new event types:
- `CollisionDetectedEvent` - Brief pause
- `CollisionNeedsResolutionEvent` - Skip (handled by UI)
- `CollisionResolvedEvent` - Brief pause with log message

## Game Flow

### Planning Phase
1. Players plan ship movements
2. All players submit orders
3. Game transitions to Resolving phase

### Resolving Phase
1. Orders processed by priority (Deploy → Build → Repair → Upgrade → Move)
2. All ships move simultaneously
3. Collisions detected (multiple ships targeting same hex)
4. **If collisions detected**:
   - Transition to CollisionResolution phase
   - Show UI to human players
   - AI players auto-decide
5. **If no collisions**:
   - Continue to Animating phase

### Collision Resolution Phase
1. UI shows all collisions to each player
2. Players choose to YIELD or PUSH for each of their ships
3. AI players automatically decide based on health
4. Once all decisions collected:
   - Resolve collisions based on decisions
   - Continue with combat resolution (for adjacent enemies)
   - Transition to Animating phase

### Animating Phase
1. Animate all movement and combat events
2. Return to Planning phase for next turn

## Collision Resolution Logic

When units collide at the same destination:

| Unit A | Unit B | Result |
|--------|--------|--------|
| Yield | Yield | Neither moves |
| Yield | Push | B moves, A stays |
| Push | Yield | A moves, B stays |
| Push | Push | Both move, combat triggered |

For 3+ units:
- All yielding units stay put
- Non-yielding units move to collision point
- If 2+ units don't yield, they all fight

## AI Behavior
AI players automatically yield if:
- Unit health < 50% of max health

Otherwise they push through, potentially triggering combat.

## Notes
- Ships still move simultaneously (this was already implemented)
- The change is in collision handling: from "both bounce back" to "player choice + combat"
- Combat uses existing Risk-style dice system (3d6 vs 2d6)
- Winner of combat gets loser's gold
- All decisions must be collected before resolution continues
- System supports 2+ ships colliding at same location

## Testing Recommendations
1. Test 2-ship collision with various yield combinations
2. Test 3+ ship collision scenarios
3. Test AI yield decision logic
4. Verify combat triggers correctly when both push
5. Test partial movement with collision (multi-turn paths)
6. Ensure UI shows/hides correctly
7. Verify gold transfer on combat victory
