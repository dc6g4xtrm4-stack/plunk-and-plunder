# Combat to the Death & Dice Visualization Implementation

## Overview
Implemented multi-round combat where ships on the same tile fight until one is destroyed, with animated dice rolling visualization.

## Key Changes

### 1. Combat Resolution (TurnResolver.cs)

#### New Method: ResolveCombatToTheDeath
Ships that end up on the same square now fight in multiple rounds until one dies.

**Before:**
```csharp
// Single round of combat
CombatResult result = combatResolver.ResolveCombat(unit1.id, unit2.id);
ApplyCombatResult(unit1, unit2, result, events, location);
```

**After:**
```csharp
// Loop until one ship dies
while (!unit1.IsDead() && !unit2.IsDead() && roundNumber <= MAX_ROUNDS)
{
    CombatResult result = combatResolver.ResolveCombat(unit1.id, unit2.id);
    unit1.TakeDamage(result.damageToAttacker);
    unit2.TakeDamage(result.damageToDefender);

    // Create combat event for this round
    events.Add(new CombatOccurredEvent(...));

    roundNumber++;
}
```

**Features:**
- ✅ Fights continue until one ship reaches 0 HP
- ✅ Each round generates a separate CombatOccurredEvent
- ✅ Safety limit of 50 rounds to prevent infinite loops
- ✅ Detailed logging for each round
- ✅ Supports 1v1 and multi-ship scenarios

**Example Combat Sequence:**
```
Round 1: Ship A (10HP) vs Ship B (10HP)
  Rolls: A[5,4,3] vs B[6,2]
  Damage: 2 to A, 2 to B
  Result: A has 8HP, B has 8HP

Round 2: Ship A (8HP) vs Ship B (8HP)
  Rolls: A[6,5,2] vs B[3,3]
  Damage: 0 to A, 4 to B
  Result: A has 8HP, B has 4HP

Round 3: Ship A (8HP) vs Ship B (4HP)
  Rolls: A[4,4,1] vs B[5,4]
  Damage: 2 to A, 2 to B
  Result: A has 6HP, B has 2HP

Round 4: Ship A (6HP) vs Ship B (2HP)
  Rolls: A[6,3,2] vs B[2,1]
  Damage: 0 to A, 4 to B
  Result: A has 6HP, B DESTROYED
```

### 2. Visual Dice Combat UI (DiceCombatUI.cs)

Created a new UI component that displays dice rolls with shaking animation.

#### UI Components:
- **Modal overlay** - Semi-transparent dark background
- **Combat title** - "⚔️ COMBAT ⚔️"
- **Round counter** - "Round 1", "Round 2", etc.
- **Attacker panel** (left, blue) - Ship name, HP, and dice
- **Defender panel** (right, red) - Ship name, HP, and dice
- **Dice displays** - 3 dice for attacker, 2 for defender
- **Result text** - Shows damage dealt and who won
- **Continue button** - Proceeds to next round or event

#### Dice Faces:
Uses Unicode dice characters:
- ⚀ (1 pip)
- ⚁ (2 pips)
- ⚂ (3 pips)
- ⚃ (4 pips)
- ⚄ (5 pips)
- ⚅ (6 pips)

#### Animation Sequence:
1. **Setup Phase**
   - Clear previous dice
   - Disable continue button
   - Create dice objects

2. **Shaking Phase (1 second)**
   - Dice shake with random position offsets
   - Dice faces rapidly change (random 1-6)
   - Creates tension and anticipation
   - Shake intensity: ±15 pixels

3. **Result Phase**
   - Dice settle to final positions
   - Show actual roll results
   - Display damage calculations
   - Show destruction if applicable

4. **Wait for Player**
   - Enable continue button
   - Player clicks to proceed

#### Visual Design:
```
┌────────────────────────────────────────────┐
│          ⚔️ COMBAT ⚔️                      │
│              Round 3                        │
│                                             │
│  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Ship_0 (P0)     │  │ Ship_1 (P1)     │ │
│  │ HP: 6/10        │  │ HP: 2/10        │ │
│  │                 │  │                 │ │
│  │  ⚅  ⚃  ⚁       │  │  ⚁  ⚀          │ │
│  │                 │  │                 │ │
│  └─────────────────┘  └─────────────────┘ │
│                                             │
│    Attacker wins!                          │
│    Defender takes 4 damage                 │
│    💥 DEFENDER DESTROYED! 💥               │
│                                             │
│         [ Continue ]                        │
└────────────────────────────────────────────┘
```

### 3. GameManager Integration

#### Combat Round Tracking:
```csharp
private Dictionary<string, int> combatRounds; // Track rounds per unit pair

string combatKey = $"{attackerId}_{defenderId}";
int roundNumber = combatRounds[combatKey]; // Increment each round
```

#### Combat Flow:
1. Combat event fires from TurnAnimator
2. GameManager pauses animation
3. Determine round number for this unit pair
4. Show DiceCombatUI with combat details
5. Player clicks "Continue"
6. Resume animation
7. If more combat events exist, repeat from step 1

### 4. Modified ResolveCombatAtLocation

**Before:** Single round of combat
**After:** Multi-round combat

```csharp
if (units.Count == 2 && units[0].ownerId != units[1].ownerId)
{
    // Direct 1v1 combat - fight until one dies
    events.AddRange(ResolveCombatToTheDeath(units[0], units[1], location));
}
```

## Game Flow

### Collision → Combat Sequence:

1. **Movement Phase**
   - All ships move simultaneously
   - Collision detected at tile (5, 10)

2. **Yield Decision Phase**
   - UI shows: "Ship A and Ship B colliding!"
   - Player A: Choose YIELD or PUSH
   - Player B: Choose YIELD or PUSH

3. **Yield Resolution**
   - **Scenario 1:** One yields → Other moves, no combat
   - **Scenario 2:** Both yield → Neither moves, no combat
   - **Scenario 3:** Neither yields → Both move to tile, **COMBAT!**

4. **Combat Phase** (Neither yielded)
   - Ships now on same tile
   - **Round 1:**
     - Dice UI appears with shaking animation
     - Shows rolls: A[6,5,3] vs B[4,4]
     - Damage calculated: 2 to A, 2 to B
     - Player clicks Continue

   - **Round 2:**
     - Dice UI appears again
     - Shows rolls: A[5,5,2] vs B[6,3]
     - Damage: 2 to A, 2 to B
     - Player clicks Continue

   - **Round 3:**
     - Dice UI appears again
     - Shows rolls: A[6,6,4] vs B[3,2]
     - Damage: 0 to A, 4 to B
     - Ship B destroyed!
     - "💥 DEFENDER DESTROYED! 💥"

5. **Resolution**
   - Dead ship removed from board
   - Animation continues
   - Game returns to planning phase

## Technical Details

### Combat Safety Features:
- **MAX_ROUNDS = 50** - Prevents infinite combat loops
- **IsDead() checks** - Ensures combat stops when ship destroyed
- **Null checks** - Handles units that may be destroyed mid-animation
- **Combat key tracking** - Unique identifier per unit pair

### Event Generation:
Each combat round creates:
- 1x `CombatOccurredEvent` (with dice rolls and damage)
- 0-2x `UnitDestroyedEvent` (if ship(s) destroyed)

### Performance:
- Average combat: 3-5 rounds
- Maximum: 50 rounds (unlikely with 2-4 damage per round)
- Each round: ~1.5 seconds (1s shake + 0.5s display)
- Total combat time: 4-8 seconds typical

## Configuration

### Adjustable Parameters:

**TurnResolver.cs:**
```csharp
const int MAX_ROUNDS = 50; // Maximum combat rounds
```

**DiceCombatUI.cs:**
```csharp
float shakeDuration = 1.0f;        // Dice shaking time
float shakeIntensity = 15f;        // Shake movement range
yield return WaitForSeconds(0.5f); // Result display time
```

## Testing Checklist

- [ ] Two ships collide and both push (trigger combat)
- [ ] Combat lasts multiple rounds (ships with similar HP)
- [ ] Combat ends in 1 round (lucky rolls)
- [ ] Ship destroyed message appears
- [ ] Round counter increments correctly
- [ ] Dice show correct roll values
- [ ] Shaking animation works smoothly
- [ ] Health updates between rounds
- [ ] Combat with upgraded ships (more HP)
- [ ] 3+ ships colliding at once
- [ ] Ship destroyed on round 1
- [ ] Ship destroyed after 10+ rounds
- [ ] Combat UI disappears after continue clicked

## Files Modified

1. **TurnResolver.cs**
   - Added `ResolveCombatToTheDeath()` method
   - Modified `ResolveCombatAtLocation()` to use new method
   - Removed old `ApplyCombatResult()` method

2. **DiceCombatUI.cs** (NEW)
   - Full dice rolling UI component
   - Shaking animation
   - Round tracking display

3. **GameManager.cs**
   - Added `diceCombatUI` field
   - Added `combatRounds` tracking dictionary
   - Modified `HandleCombatOccurred()` to use dice UI
   - Initialize dice UI in StartOfflineGame

## Debug Console Output

### Typical Combat:
```
[TurnResolver] Combat to the death: ship_0 (10HP) vs ship_1 (10HP)
[TurnResolver] Round 1: ship_0 (8HP) vs ship_1 (8HP) - Damage: 2 to attacker, 2 to defender
[TurnResolver] Round 2: ship_0 (6HP) vs ship_1 (6HP) - Damage: 2 to attacker, 2 to defender
[TurnResolver] Round 3: ship_0 (6HP) vs ship_1 (2HP) - Damage: 0 to attacker, 4 to defender
[TurnResolver] Round 4: ship_0 (6HP) vs ship_1 (0HP) - Damage: 0 to attacker, 2 to defender
[TurnResolver] ship_1 destroyed after 4 rounds
[GameManager] Combat occurred: ship_0 vs ship_1
[GameManager] Combat occurred: ship_0 vs ship_1
[GameManager] Combat occurred: ship_0 vs ship_1
[GameManager] Combat occurred: ship_0 vs ship_1
[TurnAnimator] Animating combat: ship_0 vs ship_1
```

## Benefits

1. ✅ **Rule Enforcement** - Two ships can't occupy same tile
2. ✅ **Visual Excitement** - Shaking dice animation creates tension
3. ✅ **Clear Feedback** - Players see exactly what was rolled
4. ✅ **Round Counter** - Shows how long combat has lasted
5. ✅ **Damage Display** - Clear who won each round
6. ✅ **Death Animation** - Explosion emoji for destroyed ships
7. ✅ **Fair Combat** - Multiple rounds give weaker ships a chance
8. ✅ **Strategic Depth** - Players must consider HP when deciding to yield/push

## Future Enhancements

Possible improvements:
- Add sound effects for dice rolling
- Add particle effects on dice land
- Show "top 2" highlighting on dice
- Add ship portraits/icons
- Display upgrades (cannons, hull, etc.)
- Show combat history/log
- Add "auto-continue" option for AI battles
- Replace Unicode dice with sprite images
- Add victory/defeat sound effects
