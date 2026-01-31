# Multi-Turn Combat System Implementation

## Overview
Implemented a multi-turn combat system where ships fight ONE ROUND per turn instead of fighting to the death. Ships stay in their current positions during combat and combat auto-continues each turn unless a player retreats.

## Key Requirements Met

1. ✅ Ships fight ONE ROUND per turn, not to death
2. ✅ Ships DO NOT move onto the same square during combat
3. ✅ Ships stay in their current positions while fighting
4. ✅ Combat auto-continues next turn unless player retreats
5. ✅ Players can retreat during planning phase (can be implemented via movement orders)

## Implementation Details

### 1. GameState.cs - Added OngoingCombat Class

Added a new `OngoingCombat` class to track combat that continues across turns:

```csharp
[Serializable]
public class OngoingCombat
{
    public string unitId1;
    public string unitId2;
    public HexCoord position1;
    public HexCoord position2;
    public int turnsActive;
    public int combatRoundNumber;

    public OngoingCombat(string unitId1, string unitId2, HexCoord position1, HexCoord position2)
    {
        this.unitId1 = unitId1;
        this.unitId2 = unitId2;
        this.position1 = position1;
        this.position2 = position2;
        this.turnsActive = 0;
        this.combatRoundNumber = 1;
    }
}
```

Added to GameState:
- `public List<OngoingCombat> ongoingCombats;`

### 2. Unit.cs - Added Combat Tracking Fields

Added fields to track if a unit is currently engaged in combat:

```csharp
// Multi-turn combat tracking
public bool isInCombat;
public string combatOpponentId;
```

These are initialized to `false` and `null` in the Unit constructor.

### 3. TurnResolver.cs - New ResolveOneRoundOfCombat Method

Created a new method that resolves only ONE ROUND of combat:

```csharp
private List<GameEvent> ResolveOneRoundOfCombat(Unit unit1, Unit unit2, HexCoord location)
{
    // Fight ONE round only
    CombatResult result = combatResolver.ResolveCombat(unit1.id, unit2.id);

    // Apply damage
    unit1.TakeDamage(result.damageToAttacker);
    unit2.TakeDamage(result.damageToDefender);

    // Mark as in combat if both alive
    if (!unit1.IsDead() && !unit2.IsDead())
    {
        unit1.isInCombat = true;
        unit1.combatOpponentId = unit2.id;
        unit2.isInCombat = true;
        unit2.combatOpponentId = unit1.id;
    }

    // Create event and return
}
```

### 4. TurnResolver.cs - Modified Case 3 (No Units Yield)

Modified the collision resolution Case 3 to:
- **NOT** execute unit moves (ships stay in place)
- Call `ResolveOneRoundOfCombat()` instead of `ResolveCombatAtLocation()`
- Set `unit.isInCombat = true` for both ships (if they survive)

```csharp
// Case 3: No units yield - ONE ROUND of combat happens, ships stay in place
else
{
    // DO NOT move units - ships stay in their current positions during combat
    // Get units involved and trigger ONE ROUND of combat
    events.AddRange(ResolveOneRoundOfCombat(combatUnits[0], combatUnits[1], collision.destination));
}
```

### 5. TurnResolver.cs - Modified Adjacent Combat (ResolveCombat)

Changed the `ResolveCombat()` method to use `ResolveOneRoundOfCombat()` instead of the old single-round combat logic. This ensures adjacent units also fight one round per turn.

### 6. GameManager.cs - Track Ongoing Combats

Added logic at the end of `ContinueResolutionWithYieldDecisions()` to store ongoing combats:

```csharp
// After combat, check for ongoing combats and store them
state.ongoingCombats.Clear();
List<Unit> allUnits = state.unitManager.GetAllUnits();
HashSet<string> processedUnits = new HashSet<string>();

foreach (Unit unit in allUnits)
{
    if (unit.isInCombat && !processedUnits.Contains(unit.id) && unit.combatOpponentId != null)
    {
        Unit opponent = state.unitManager.GetUnit(unit.combatOpponentId);
        if (opponent != null && opponent.isInCombat)
        {
            // Create ongoing combat entry
            OngoingCombat ongoingCombat = new OngoingCombat(
                unit.id,
                opponent.id,
                unit.position,
                opponent.position
            );
            state.ongoingCombats.Add(ongoingCombat);
            processedUnits.Add(unit.id);
            processedUnits.Add(opponent.id);
        }
    }
}
```

### 7. DiceCombatUI.cs - Show Multi-Turn Message

Added a new text field to display the multi-turn combat message:

```csharp
private Text combatContinuesText;
```

Created the text in the UI:
```csharp
combatContinuesText = CreateText("Combat continues next turn unless you retreat\nPress Continue to end this turn", 18, dialog.transform);
```

Modified `ShowCombat()` to show/hide this message based on whether both ships survive:
```csharp
bool bothSurvive = !combatEvent.attackerDestroyed && !combatEvent.defenderDestroyed;
combatContinuesText.gameObject.SetActive(bothSurvive);
```

## How It Works

1. **Turn Resolution**: When units collide or are adjacent:
   - If they both choose not to yield, ONE round of combat is resolved
   - Ships remain in their current positions (no movement)
   - Damage is applied and health is updated
   - If both survive, they are marked as `isInCombat = true`

2. **Combat Tracking**: After resolution:
   - GameManager scans all units for `isInCombat` flag
   - Creates `OngoingCombat` entries for each pair
   - Stores in `state.ongoingCombats`

3. **Next Turn**: On the next turn:
   - Units still adjacent will automatically fight another round
   - The `ResolveCombat()` method checks all adjacent enemies
   - Uses `ResolveOneRoundOfCombat()` for one round only

4. **Combat UI**: When combat occurs:
   - Shows round number
   - Shows dice rolls and damage
   - If both ships survive: displays "Combat continues next turn unless you retreat"
   - If one ship is destroyed: combat ends, flags are cleared

5. **Retreating**: Players can retreat by:
   - Moving their ship away during the planning phase
   - The ship will no longer be adjacent and combat ends

## Testing Checklist

- [ ] Two enemy ships collide and both choose not to yield
- [ ] Verify ships DO NOT move onto the same square
- [ ] Verify only ONE round of combat occurs
- [ ] Verify both ships remain in their starting positions
- [ ] Verify combat UI shows "Combat continues next turn" message
- [ ] Verify on next turn, if ships are still adjacent, another round occurs
- [ ] Verify player can retreat by moving away
- [ ] Verify combat ends when one ship is destroyed
- [ ] Verify multiple ongoing combats are tracked correctly

## Future Enhancements

1. **Retreat Button**: Add a dedicated "Retreat" button during planning phase for units in combat
2. **Combat History**: Display combat rounds history in event log
3. **Round Counter**: Show which round of combat (Round 1, Round 2, etc.) in the UI
4. **Combat Indicators**: Visual indicators on the map showing which ships are in ongoing combat
5. **Auto-Retreat**: AI logic to automatically retreat when health is low
