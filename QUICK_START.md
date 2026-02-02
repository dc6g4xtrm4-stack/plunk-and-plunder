# Quick Start Guide - HUD Redesign Testing

## 1. Open Unity
```bash
# If using Unity Hub, open the project
# Or from command line:
unity -projectPath "C:\Users\jjk21\repos\plunk-and-plunder"
```

## 2. Let Unity Compile
- Open Unity Editor
- Wait for scripts to compile (check bottom-right progress bar)
- Check Console window (Ctrl+Shift+C) for any red errors
- **Expected:** No compilation errors

## 3. Start the Game
- In Unity, press Play button (or F5)
- Game should load to main menu or game scene
- Start an offline game with 4 players

## 4. First Visual Check (Critical!)

### Look at Bottom-Left Corner:
You should see a NEW panel with:
```
┌─────────────────────────┐
│ PLAYER STATS            │  ← Gold header
├─────────────────────────┤
│ Player 1                │
│   💰 100                │
│   ⛵ 3 | 🏭 1            │
│                         │
│ AI 1                    │
│   💰 100                │
│   ⛵ 3 | 🏭 1            │
│ ...                     │
├─────────────────────────┤
│ SELECTED UNIT           │
│ No selection            │
├─────────────────────────┤
│ (BUILD QUEUE if needed) │
├─────────────────────────┤
│ [Deploy Shipyard]       │
│ [Build Ship]            │
│ [Upgrade Sails]         │
│ [Upgrade Cannons]       │
│ [Upgrade Max Life]      │
└─────────────────────────┘
```

**Position:** 10px from left edge, 10px from bottom edge

### Look at Bottom-Right Corner:
You should see:
```
┌─────────────────────────┐
│ EVENT LOG               │  ← Gold header
├─────────────────────────┤
│ Awaiting events...      │
│                         │
│                         │
│ (Events will appear     │
│  here during gameplay)  │
│                         │
│                         │
└─────────────────────────┘
```

**Position:** 10px from right edge, 10px from bottom edge

### Look at Top:
You should see:
```
┌──────────────────────────────────────────────────────────┐
│ Turn: 1 | Phase: Planning | Gold: 100 | [Submit Orders] │
└──────────────────────────────────────────────────────────┘
```

**Position:** Top of screen, full width, 100px tall

## 5. Quick Interaction Test

### Test 1: Select a Ship
1. Left-click any ship on the map
2. **Expected:** Left panel updates to show ship details
3. **Check:** Ship name, HP, movement stats appear

### Test 2: Build a Ship
1. Left-click a shipyard (building on island)
2. **Expected:** "BUILD QUEUE" section appears in left panel
3. Click the "Build Ship (50g)" button
4. **Expected:**
   - Ship appears in build queue
   - Gold decreases by 50
   - Button may disable if low on gold

### Test 3: Submit Orders
1. Right-click to move a ship
2. **Expected:** Path visualization appears
3. Click "Submit Orders" button in top bar
4. **Expected:**
   - Phase changes to "Resolution"
   - Events appear in right panel
   - Turn progresses

## 6. If Something Looks Wrong

### Left Panel Not at Bottom-Left?
- Check Console for errors
- Verify LeftPanelHUD component exists in hierarchy
- Check anchoring in Inspector (should be 0, 0)

### Buttons Not Working?
- Verify you're in Planning phase
- Check you have enough gold
- Verify proper selection (ship vs shipyard)

### Compilation Errors?
- Check all new files have .meta files
- Verify namespace `PlunkAndPlunder.UI` is correct
- Check for missing references

### Old UI Still Visible?
- Old unit panel should be hidden
- Old action buttons should be hidden off-screen
- If visible, check GameHUD.cs changes applied correctly

## 7. Full Test Run

Follow the comprehensive `TESTING_CHECKLIST.md` for detailed testing.

## 8. Report Issues

If you find bugs or issues:
1. Note the exact scenario
2. Check Console for error messages
3. Take screenshot if visual issue
4. Document in testing checklist

## Key Files to Check in Unity Hierarchy

When game is running, check Unity Hierarchy:
```
Canvas
├── GameHUD
│   ├── TopBar (new, using HUDStyles)
│   ├── EventLog (refactored position)
│   ├── Tooltip
│   └── (other components)
└── LeftPanelHUD (NEW!)
    ├── PlayerStatsSection
    ├── UnitDetailsSection
    ├── BuildQueueSection
    └── ActionButtonsSection
```

## Success Indicators

✅ **Left panel visible at bottom-left corner**
✅ **Player stats showing for all players**
✅ **Action buttons visible and responsive**
✅ **Event log at bottom-right corner**
✅ **Top bar centered at top**
✅ **No overlap or cut-off UI elements**
✅ **Gold colors used for headers**
✅ **Dark semi-transparent backgrounds**
✅ **Game fully playable**

## Debug Commands (if needed)

In Unity Console, you can filter logs:
- `LeftPanelHUD` - See left panel logs
- `GameManager` - See game state logs
- `GameHUD` - See input/order logs

## Quick Fixes

### If compilation fails:
```csharp
// Check these using statements are in each new file:
using UnityEngine;
using UnityEngine.UI;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Units;
using PlunkAndPlunder.Structures;
```

### If LeftPanelHUD not showing:
- Check GameManager.cs line ~147
- Should say: `leftPanelHUD = leftPanelHUDObj.AddComponent<LeftPanelHUD>();`
- Should NOT say: `playerStatsHUD = ...`

### If positioning wrong:
- Check HUDStyles.cs exists and compiles
- Verify constants defined (EdgeMargin = 10, etc.)
- Check RectTransform anchors in Inspector

## Next Steps

Once basic testing passes:
1. Complete full testing checklist
2. Play several full turns
3. Test all action buttons
4. Verify all edge cases
5. Check performance
6. Document any issues
7. Iterate and refine

Good luck! 🎮
