# Unit Path Visualization and Selection Indicators Implementation

## Overview
This implementation adds visual feedback for unit selection and movement planning in the Plunk & Plunder game. Players can now see clearly which ship is selected and what path has been planned before submitting orders.

## Features Implemented

### 1. Path Visualization
**File**: `Assets/Scripts/Rendering/PathVisualizer.cs`

- Draws a yellow line along planned movement paths
- Shows directional arrows to indicate movement direction
- Arrows appear at waypoints and at the destination
- Path appears when right-clicking to set a destination
- Path clears when orders are submitted or a new turn starts

**How it works**:
- Uses Unity's `LineRenderer` component to draw the path
- Creates small cube arrows rotated to point in the direction of movement
- All path elements are positioned slightly above the water (y=0.2) for visibility

### 2. Ship Ownership via Sail Color
**File**: `Assets/Scripts/Rendering/UnitRenderer.cs` (modified)

- **Player 0 (Red)**: Red sails
- **Player 1 (Blue)**: Blue sails
- **Player 2 (Green)**: Green sails
- **Player 3 (Yellow)**: Yellow sails

**Changes made**:
- Line 104: Changed sail color from off-white to player color
- Line 145: Reduced flag size from (0.15f, 0.1f, 0.02f) to (0.08f, 0.06f, 0.02f)
- Flags remain but are much smaller and less prominent

### 3. Selection Indicator
**Files**:
- `Assets/Scripts/Rendering/UnitRenderer.cs` (added methods)
- `Assets/Scripts/Rendering/SelectionPulse.cs` (new)

- Shows a bright cyan glowing ring at the base of selected ships
- Ring pulses in and out to be highly visible
- Only one ship can have a selection indicator at a time
- Indicator clears when selecting a structure or clearing selection

**Implementation details**:
- `ShowSelectionIndicator(unitId)`: Creates a flat cylinder at y=0.05
- `HideSelectionIndicator()`: Removes all selection indicators
- `SelectionPulse`: Animates the alpha of the indicator material

### 4. GameHUD Integration
**File**: `Assets/Scripts/UI/GameHUD.cs` (modified)

Added path visualization to the game HUD:
- Line 5: Added `using PlunkAndPlunder.Rendering;`
- Line 34: Added `pathVisualizer` field
- Lines 43-49: Initialize path visualizer in `InitializeVisualizers()`
- Selection indicator shown when unit is selected (line 302)
- Path visualization shown when destination is set (line 343)
- Both clear when orders are submitted or turn changes

## Usage

### For Players:
1. **Select a ship**: Left-click on a ship
   - A cyan glowing ring appears under the ship
   - Ship information displayed in the UI panel

2. **Plan movement**: Right-click on a destination
   - A yellow line with arrows shows the planned path
   - "Move order queued!" message appears

3. **Submit orders**: Click "Submit Orders" button
   - Path visualization clears
   - Selection indicator clears
   - Orders are processed

### Visual Feedback:
- **Selection**: Bright cyan pulsing ring under selected ship
- **Movement Path**: Yellow line with directional arrows
- **Ownership**: Colored sails match player color (red/blue/green/yellow)
- **Flags**: Small flags at mast tops retain player color but are less prominent

## Technical Notes

### Rendering Order:
- Hex grid: y=0 (water level)
- Selection indicator: y=0.05 (just above water)
- Path line: y=0.2 (above selection indicator)
- Path arrows: y=0.25 (slightly above path line)
- Ships: y=0.3 (above all path elements)

### Material Shaders:
The implementation attempts to find appropriate shaders in this order:
1. "Standard" (preferred, supports emission)
2. "Legacy Shaders/Diffuse"
3. "Unlit/Color" (fallback)

### Performance Considerations:
- Path visualization uses a single LineRenderer per path
- Arrows are simple cubes with colliders removed
- Selection indicator is a single cylinder per selected unit
- All visualizations are destroyed when no longer needed

## Files Modified:
1. `Assets/Scripts/Rendering/UnitRenderer.cs` - Added selection indicator methods, changed sail colors
2. `Assets/Scripts/UI/GameHUD.cs` - Integrated path and selection visualization

## Files Created:
1. `Assets/Scripts/Rendering/PathVisualizer.cs` - Path rendering system
2. `Assets/Scripts/Rendering/SelectionPulse.cs` - Selection indicator animation
3. `Assets/Scripts/Rendering/PathVisualizer.cs.meta` - Unity meta file
4. `Assets/Scripts/Rendering/SelectionPulse.cs.meta` - Unity meta file

## Testing Recommendations:

1. Start a game and verify ships have colored sails matching their owner
2. Click on a ship and verify cyan ring appears underneath
3. Right-click to set a destination and verify yellow path with arrows appears
4. Submit orders and verify visualizations clear properly
5. Test with multiple players to verify different sail colors
6. Verify path arrows point in the correct direction of movement

## Future Enhancements:
- Add path cost/distance display
- Color-code paths based on movement speed
- Add hover preview for potential paths
- Animate path drawing from start to end
- Add sound effects for selection/planning
