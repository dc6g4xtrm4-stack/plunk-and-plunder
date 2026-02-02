# Fleet System - Implementation Guide

## Overview

The fleet system is a **Quality of Life (QoL) feature** that allows players to group multiple ships at the same location into a fleet, enabling them to be commanded together. Fleets do not change gameplay mechanics - each ship maintains its own stats, health, and acts independently in combat.

## Features

### Fleet Creation
- **"Combine into Fleet" button** appears when 2+ human-owned ships are at the same position
- Click the button to form a fleet
- All ships at that position (not already in a fleet) are combined

### Fleet Commands
- **Select fleet**: Click on any ship in a fleet to select the entire fleet
- **Move fleet**: Right-click to move all ships in the fleet together (same path for all)
- **Fleet info**: Left panel shows fleet name, ship count, and individual ship stats

### Fleet Disbanding
- **"Disband Fleet" button** appears when a fleet is selected
- Click to break the fleet back into individual ships
- **Auto-disband**: Fleets automatically disband when:
  - Ships move to different positions
  - Ships are destroyed (less than 2 remain)
  - After combat resolution

## Implementation

### Core Classes

#### Fleet.cs
```csharp
public class Fleet
{
    public string id;
    public int ownerId;
    public List<string> unitIds;
    public string name;

    // Methods
    public HexCoord? GetPosition(UnitManager)
    public List<Unit> GetUnits(UnitManager)
    public bool IsValid(UnitManager)
    public int GetShipCount(UnitManager)
}
```

#### FleetManager.cs
```csharp
public class FleetManager
{
    // Methods
    public Fleet CreateFleet(int ownerId, List<string> unitIds, UnitManager)
    public void DisbandFleet(string fleetId)
    public Fleet GetFleet(string fleetId)
    public Fleet GetFleetContainingUnit(string unitId)
    public void ValidateFleets(UnitManager) // Auto-disband invalid fleets
    public List<Unit> GetFleetableUnitsAtPosition(HexCoord, int playerId, UnitManager)
}
```

### Integration Points

#### GameState
- Added `FleetManager fleetManager` field
- Initialized in constructor

#### GameHUD
- Added `Fleet selectedFleet` field
- `SelectFleet(Fleet)` method for fleet selection
- `SetFleetDestination(HexCoord)` gives same move order to all ships in fleet
- `OnCombineFleetClicked()` creates new fleet
- `OnDisbandFleetClicked()` disbands selected fleet

#### LeftPanelHUD
- `UpdateSelection(Unit, Structure, Fleet)` accepts optional Fleet parameter
- Displays fleet info: name, ship count, individual ship stats
- Shows "Combine into Fleet" button when applicable
- Shows "Disband Fleet" button when fleet selected

#### GameEngine
- Calls `State.fleetManager.ValidateFleets()` after:
  - `ResolveOrders()` (after movement)
  - `ResolveCollisions()` (after collision resolution)
  - `ResolveEncounters()` (after encounter resolution)

## User Experience

### Creating a Fleet
1. Move multiple ships to the same position
2. Click on one of the ships
3. "Combine into Fleet" button appears in action panel
4. Click button → fleet created

### Using a Fleet
1. Click any ship in the fleet → entire fleet selected
2. Left panel shows: "FLEET: Fleet_XXXX" with ship count
3. Right-click destination → all ships move together
4. Each ship still moves independently and fights independently

### Disbanding a Fleet
1. Select the fleet
2. "Disband Fleet" button appears
3. Click button → ships become individual units again
4. First ship from fleet is auto-selected

## Technical Notes

### QoL Only - No Gameplay Changes
- ✅ Same stats per ship
- ✅ Same health bars
- ✅ Same stacking visualization
- ✅ Same combat behavior
- ✅ Orders issued per-ship (just automated)
- ❌ No fleet-wide bonuses
- ❌ No shared health pool
- ❌ No formation bonuses

### Fleet Validation
Fleets are automatically validated after turn resolution:
- If ships are at different positions → auto-disband
- If less than 2 ships remain → auto-disband
- If any ship is destroyed → remove from fleet, check count

### Order Processing
When a fleet moves:
1. Calculate path from fleet position to destination
2. Issue identical MoveOrder to each ship in the fleet
3. Each ship executes its own order independently
4. If ships separate during movement → fleet auto-disbands

## Future Enhancements

Possible additions (not implemented):
- Named fleets (e.g., "Northern Squadron")
- Fleet-wide upgrades button (upgrade all ships in fleet)
- Formation bonuses (if gameplay changes desired)
- Fleet merge/split options
- Ctrl+click to add/remove ships from fleet

## Testing Checklist

- [ ] Create fleet with 2+ ships at same position
- [ ] Move fleet → all ships move together
- [ ] Disband fleet → ships become individual
- [ ] Ships separate → fleet auto-disbands
- [ ] Ship destroyed → fleet updates or disbands
- [ ] Combine → Disband → Recombine (same ships)
- [ ] Multiple fleets at different positions
- [ ] Fleet selection indicator shows correctly
- [ ] Left panel displays fleet info
- [ ] Buttons appear/disappear correctly

## Files Modified

### New Files
- `Assets/Scripts/Units/Fleet.cs` - Fleet data class
- `Assets/Scripts/Units/FleetManager.cs` - Fleet management
- `docs/FLEET_SYSTEM.md` - This documentation

### Modified Files
- `Assets/Scripts/Core/GameState.cs` - Added FleetManager
- `Assets/Scripts/Core/GameEngine.cs` - Added fleet validation calls
- `Assets/Scripts/UI/GameHUD.cs` - Fleet selection and commands
- `Assets/Scripts/UI/LeftPanelHUD.cs` - Fleet UI display and buttons

---

**Design Philosophy**: Fleets are a convenience feature for grouping ships, not a strategic gameplay mechanic. They reduce micromanagement without changing balance or combat.
