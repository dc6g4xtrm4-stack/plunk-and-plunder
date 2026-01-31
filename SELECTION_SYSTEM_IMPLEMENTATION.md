# Starcraft-Style Unit Selection Implementation

## Overview
Implemented a comprehensive Starcraft-style unit selection system with drag selection, multi-unit control, Tab cycling, and visual indicators.

## Features Implemented

### 1. Drag Selection (Box Selection)
- **Left-click drag**: Create a green selection box to select multiple ships
- **Visual feedback**: Semi-transparent green box with border while dragging
- **Threshold detection**: Only activates box selection if dragged more than 5 pixels (prevents accidental drags)
- **Multi-unit selection**: All ships within the box are selected

### 2. Multi-Unit Control
- **Group selection**: Select multiple ships at once using box selection
- **Unified movement**: Right-click sets destination for ALL selected ships simultaneously
- **Group orders**: All selected ships receive move orders together
- **Visual indicators**: All selected ships show selection rings

### 3. Tab Key Cycling
- **Active unit concept**: Within a selection group, one ship is the "active" ship
- **Press TAB**: Cycle through all selected ships
- **HUD updates**: The HUD shows detailed info for the active ship
- **Visual distinction**: Active ship's selection ring is the "primary" path visualization

### 4. Visual Indicators
- **Selection rings**: Cyan glowing rings appear under all selected ships
- **Multi-indicator support**: Multiple ships can show selection rings simultaneously
- **State-based coloring**:
  - Cyan pulsing: Ship has no order yet
  - Green slow pulse: Ship has an order queued
- **Persistent indicators**: Selection rings follow ships as they move and stack

### 5. HUD Display
- **Active ship info**: Shows detailed stats for the currently active ship in the selection
- **Group counter**: Displays "[GROUP: X/Y]" showing which ship is active
- **Tab hint**: Shows "Press TAB to cycle" when multiple ships are selected
- **Order status**: Shows which ships have pending orders

## Implementation Details

### New Files Created

#### SelectionBox.cs (`Assets/Scripts/UI/SelectionBox.cs`)
```csharp
// Handles the visual drag selection box
- StartSelection(Vector2): Begin drag from screen position
- UpdateSelection(Vector2): Update box size as mouse moves
- EndSelection(): Complete selection and return bounds
- GetSelectionBounds(): Get current selection rectangle
```

### Modified Files

#### GameHUD.cs (`Assets/Scripts/UI/GameHUD.cs`)
**New Fields:**
- `List<Unit> selectedUnits`: All selected ships (multi-select)
- `int activeUnitIndex`: Which ship in the group is currently active
- `SelectionBox selectionBox`: Reference to drag selection UI component
- `bool isDraggingSelection`: Tracks if user is currently dragging
- `Vector2 dragStartPosition`: Starting position of drag

**New Methods:**
- `PerformClickSelection()`: Handle single-click ship selection
- `PerformBoxSelection(Rect)`: Select all ships within screen rectangle
- `HandleTabCycling()`: Handle Tab key to cycle through selected ships
- `SelectSingleUnit(Unit)`: Select a single ship
- `SelectMultipleUnits(List<Unit>)`: Select multiple ships
- `UpdateSelectedUnitDisplay()`: Update HUD text for active ship
- `UpdateSelectionIndicators()`: Show/update selection rings for all selected ships
- `SetSelectedUnitsDestination(HexCoord)`: Move all selected ships to destination

**Modified Methods:**
- `HandleInput()`: Completely rewritten to support drag selection
- `ClearSelection()`: Now clears the entire selection list
- `SelectUnit(Unit)`: Redirects to `SelectSingleUnit()`
- `SetUnitDestination(HexCoord)`: Redirects to `SetSelectedUnitsDestination()`

#### UnitRenderer.cs (`Assets/Scripts/Rendering/UnitRenderer.cs`)
**Modified Methods:**
- `ShowSelectionIndicator(string)`: Now supports multiple simultaneous indicators
  - Removed the `HideSelectionIndicator()` call at the start
  - Checks if indicator already exists before creating
  - Names each indicator uniquely: `SelectionIndicator_{unitId}`
- `UpdateUnitVisual(Unit)`: Now updates selection indicator position when unit moves
- `ApplyStackingOffsets(UnitManager)`: Updates selection indicator positions when ships stack

## Usage

### Basic Selection
1. **Single ship**: Left-click on a ship
2. **Multiple ships**: Left-click and drag to create a box around ships
3. **Clear selection**: Left-click on empty space

### Commanding Selected Ships
1. Select one or more ships
2. Right-click on destination hex
3. All selected ships receive move orders to that location

### Cycling Through Selection
1. Select multiple ships (box select)
2. Press TAB to cycle through them
3. HUD shows the active ship's details
4. Press TAB again to move to the next ship

### Visual Feedback
- **Cyan pulsing ring**: Ship is selected but has no order
- **Green slow pulse ring**: Ship is selected and has an order queued
- **Multiple rings**: All selected ships show their selection rings
- **HUD counter**: Shows "1/3", "2/3", "3/3" etc. when cycling

## Technical Notes

### Screen Space to World Space Conversion
The box selection uses `Camera.main.WorldToScreenPoint()` to convert ship positions to screen coordinates for hit testing against the selection rectangle.

### Selection Indicator Persistence
Selection indicators are stored in a `Dictionary<string, GameObject>` keyed by unit ID, allowing multiple indicators to exist simultaneously. The indicators are updated in:
- `UpdateUnitVisual()`: When a single unit moves
- `ApplyStackingOffsets()`: When units stack on the same tile

### Path Visualization Integration
The path visualizer distinguishes between "primary" (active ship) and "secondary" (other selected ships) paths. The active ship's path is rendered more prominently.

### Order Management
- Each ship in the selection gets its own `MoveOrder`
- Orders are tracked in `unitsWithOrders` HashSet
- Paths are stored per-unit in `pendingMovePaths` Dictionary
- When submitting orders, all ships' orders are submitted together

## Updated Help Text
```
HOW TO PLAY:
Left-click: Select ship/building
Drag: Box select ships
Right-click: Move selected ships
TAB: Cycle through selection
Queue orders, then Submit
```

## Testing Recommendations

1. **Basic selection**:
   - Click to select single ships
   - Verify selection ring appears

2. **Box selection**:
   - Drag to select multiple ships
   - Verify all ships show selection rings
   - Check that only human player ships are selected

3. **Tab cycling**:
   - Select multiple ships
   - Press Tab repeatedly
   - Verify HUD updates with each press
   - Check counter shows correct position

4. **Multi-unit movement**:
   - Select 2-3 ships
   - Right-click a destination
   - Verify all ships get move orders
   - Check all selection rings turn green

5. **Visual persistence**:
   - Select ships and give them orders
   - Submit orders and watch them move
   - Verify selection rings follow the ships

6. **Stacking compatibility**:
   - Select multiple ships on the same tile
   - Verify rings are offset with the ships

## Known Limitations

1. **Camera angle dependency**: Box selection depends on camera position/angle for screen-to-world conversion
2. **No shift-add selection**: Currently no way to add ships to existing selection (would need Shift+click)
3. **No control group hotkeys**: No numbered control groups (1-9) like Starcraft
4. **Single formation**: All ships path to exact same destination (no formation offset)

## Future Enhancements

1. **Shift-click to add/remove**: Hold Shift to add/remove individual ships from selection
2. **Control groups**: Assign selections to number keys (Ctrl+1, Ctrl+2, etc.)
3. **Smart formations**: Spread ships around destination tile instead of all going to same spot
4. **Double-click selection**: Double-click to select all ships of same type on screen
5. **Selection preview**: Show ship count before completing box selection
6. **Deselect with Escape**: Press Escape to clear selection

## Files Modified
- `Assets/Scripts/UI/GameHUD.cs` - Core selection logic
- `Assets/Scripts/Rendering/UnitRenderer.cs` - Multi-indicator support
- `Assets/Scripts/UI/SelectionBox.cs` - NEW: Drag selection UI component
