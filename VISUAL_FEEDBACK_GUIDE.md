# Visual Feedback System - Quick Reference

## Ship Ownership Colors (Sail-Based)

Ships now display their owner through **colored sails** instead of prominent flags:

| Player | Sail Color |
|--------|-----------|
| Player 0 (Human) | Red |
| Player 1 (AI) | Blue |
| Player 2 (AI) | Green |
| Player 3 (AI) | Yellow |

Small flags still appear at the top of masts but are much less prominent (reduced by ~50% in size).

## Selection Indicator

When you select a ship:
- A **bright cyan glowing ring** appears at the base of the ship
- The ring **pulses** (fades in/out) to draw attention
- Only one ship can be selected at a time
- The indicator disappears when you:
  - Select a different unit
  - Select a structure
  - Clear the selection
  - Submit orders

**Visual specs:**
- Color: Cyan (RGB: 0, 255, 255)
- Position: y=0.05 (just above water)
- Size: 0.8 unit diameter
- Animation: Alpha pulses between 0.4 and 1.0

## Path Visualization

When you plan a movement:
- A **yellow line** shows the path from current position to destination
- **Direction arrows** appear along the path showing movement direction
- Arrows appear:
  - Every other waypoint
  - Always at the final destination

**Visual specs:**
- Color: Yellow (RGB: 255, 255, 0)
- Line width: 0.15 units
- Position: y=0.2 (above selection indicator)
- Arrow size: 0.2 x 0.05 x 0.3 units

The path clears when:
- Orders are submitted
- A new turn starts
- Selection is cleared
- A structure is selected

## Rendering Layers (Y-Axis Heights)

From bottom to top:
1. **Water/Hexes**: y=0.0
2. **Selection Indicator**: y=0.05
3. **Path Line**: y=0.2
4. **Path Arrows**: y=0.25
5. **Ships**: y=0.3

This ordering ensures all UI elements are visible and don't overlap incorrectly.

## Implementation Files

### Core Components:
- **PathVisualizer.cs**: Handles all path rendering (line + arrows)
- **SelectionPulse.cs**: Animates the selection indicator
- **UnitRenderer.cs**: Creates selection indicators, manages sail colors
- **GameHUD.cs**: Integrates visualizations with game UI

### Key Methods:
```csharp
// In UnitRenderer
unitRenderer.ShowSelectionIndicator(unitId);
unitRenderer.HideSelectionIndicator();

// In PathVisualizer
pathVisualizer.ShowPath(List<HexCoord> path);
pathVisualizer.ClearPath();
```

## Usage Flow

1. **Player clicks ship** → Selection indicator appears
2. **Player right-clicks destination** → Path visualization appears
3. **Player clicks "Submit Orders"** → Both visualizations clear
4. **Turn resolves** → Ships move, visualizations remain clear
5. **New planning phase** → Player can select and plan again

## Design Rationale

### Why sail colors?
- More visible than small flags
- Instantly recognizable from any angle
- Works well with multiple ships on screen
- Maintains naval theme (historically accurate)

### Why cyan selection indicator?
- High contrast against blue water
- Doesn't clash with any player colors
- Glowing effect makes it stand out
- Pulsing animation draws the eye

### Why yellow path visualization?
- High visibility against both water and land
- Universal "caution/attention" color
- Doesn't match any player's color
- Clear directional arrows prevent confusion

## Customization Options

If you want to adjust the visuals, here are the key parameters:

### PathVisualizer.cs:
```csharp
public Color pathColor = new Color(1f, 1f, 0f, 0.8f); // Yellow
public float lineWidth = 0.15f;
public float pathHeight = 0.2f;
```

### SelectionPulse.cs:
```csharp
public float pulseSpeed = 2f;
public float minAlpha = 0.4f;
public float maxAlpha = 1f;
```

### UnitRenderer.cs (ShowSelectionIndicator):
```csharp
indicator.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
mat.color = new Color(0f, 1f, 1f, 0.8f); // Bright cyan
```

### UnitRenderer.cs (Player Colors):
```csharp
[Header("Player Colors")]
public Color[] playerColors = new Color[]
{
    Color.red,    // Player 0
    Color.blue,   // Player 1
    Color.green,  // Player 2
    Color.yellow  // Player 3
};
```

## Troubleshooting

### Path not showing?
- Check that pathVisualizer was initialized in GameHUD
- Verify path has at least 2 waypoints
- Check shader availability (Standard/Diffuse/Unlit)

### Selection indicator not appearing?
- Verify unit exists in unitObjects dictionary
- Check that UnitRenderer is in the scene
- Confirm shader is available

### Colors not showing correctly?
- Check if shader supports color property
- Verify materials are being created properly
- Try switching to different shader fallback

### Visual elements z-fighting?
- Adjust y-positions to prevent overlap
- Increase separation between layers
- Check camera near/far clip planes
