# HUD Redesign Testing Checklist

## Quick Visual Test (5 minutes)

Launch the game and verify:

### Top Bar
- [ ] Top bar appears at top of screen, full width
- [ ] Shows "Turn: X" on the left
- [ ] Shows "Phase: Planning" in center
- [ ] Shows "Gold: XXX | Orders: X" in center-right
- [ ] "PASS TURN" button appears on right side
- [ ] Button is gray/disabled when no orders
- [ ] Button turns green when orders are queued
- [ ] Button pulses bright green when all units have orders

### Left Panel
- [ ] Left panel appears at bottom-left corner
- [ ] Has gold border and dark background
- [ ] **PLAYER STATS** section at top shows:
  - All player names with color coding
  - Gold amount with 💰 icon
  - Ship count with ⛵ icon
  - Shipyard count with 🏭 icon
- [ ] **SELECTED UNIT** section shows "No selection" initially
- [ ] **BUILD QUEUE** section hidden initially
- [ ] **ACTIONS** section at bottom shows action button labels

## Functional Test (10 minutes)

### Turn Workflow
1. [ ] Click a ship → Selection details appear in left panel
2. [ ] Right-click destination → Yellow path appears
3. [ ] "Orders: 1" appears in top bar
4. [ ] Pass Turn button becomes green
5. [ ] Click Pass Turn → Orders submit, phase changes

### Action Buttons
1. [ ] Select ship on harbor → "Deploy Shipyard (100g)" button appears
2. [ ] Click Deploy Shipyard → Order queued (check console log)
3. [ ] Select shipyard → "Build Ship (50g)" button appears
4. [ ] Click Build Ship → Ship added to queue
5. [ ] Select ship at shipyard → Upgrade buttons appear
6. [ ] Click upgrade button → Order queued (check console log)

### Selection States
1. [ ] Click ship → Unit details show in left panel
2. [ ] Click shipyard → Structure details show, build queue appears
3. [ ] Click empty tile → "No selection" appears
4. [ ] Select different units → Details update correctly

### Button States
1. [ ] With insufficient gold → Buttons are gray/disabled
2. [ ] With sufficient gold → Buttons are colored/enabled
3. [ ] At max upgrades → Upgrade buttons disabled
4. [ ] Queue full → Build Ship button disabled

## Regression Test (10 minutes)

### Core Gameplay
- [ ] Left-click selection still works
- [ ] Right-click movement still works
- [ ] Path visualization appears (yellow lines)
- [ ] Turn resolution still works
- [ ] Combat encounters still trigger
- [ ] Combat results display correctly

### UI Components
- [ ] Tile tooltip appears on hover
- [ ] Event log displays game events
- [ ] ESC menu opens with Escape key
- [ ] Selection indicator appears on units
- [ ] Selection indicator changes color when order queued

### Multiplayer (if applicable)
- [ ] Player stats show all players correctly
- [ ] AI players execute turns normally
- [ ] Turn order maintained correctly

## Known Issues to Check

- [ ] No console errors on startup
- [ ] No missing sprite warnings
- [ ] No null reference exceptions
- [ ] UI doesn't overlap game view
- [ ] Text is readable at all resolutions
- [ ] Buttons respond to clicks (no dead zones)

## Performance Check

- [ ] No lag when opening left panel
- [ ] Button animations smooth
- [ ] UI updates don't stutter
- [ ] Memory usage normal (check Task Manager)

## Edge Cases

- [ ] Select unit, then structure → Details switch correctly
- [ ] Queue multiple moves → All paths show
- [ ] Submit orders with combat path → Combat triggers
- [ ] Phase change during selection → No errors
- [ ] Screen resize → UI scales correctly

## Quick Fix Checklist

If something doesn't work:

1. **Top bar not showing**
   - Check TopBarHUD Initialize() is called in GameHUD
   - Check anchors: (0,1) to (1,1)

2. **Left panel not showing**
   - Check LeftPanelHUD Initialize() is called with GameState
   - Check anchors: (0,0) to (0,0)

3. **Pass Turn button not responding**
   - Check OnPassTurnClicked() is public in GameHUD
   - Check SendMessage call in TopBarHUD

4. **Action buttons not working**
   - Check button handlers are public in GameHUD
   - Check SendMessage calls in LeftPanelHUD

5. **Stats not updating**
   - Check UpdateHUD() calls topBarHUD.UpdateTurnInfo()
   - Check UpdateHUD() calls leftPanelHUD.UpdatePlayerStats()

## Success Criteria

✅ All visual elements appear correctly positioned
✅ All buttons respond to clicks
✅ Game loop (select, move, submit) works
✅ No console errors or warnings
✅ Performance is smooth (60 FPS)

---

## Console Debug Commands

Useful for testing:
- Look for "[TopBarHUD]" logs on initialization
- Look for "[LeftPanelHUD]" logs on initialization
- Look for "[GameHUD]" logs on button clicks
- Check for "Orders submitted" log on Pass Turn

## Report Template

```
TESTING REPORT - HUD Redesign
Date: [DATE]
Tester: [NAME]

Visual Test: PASS / FAIL
- Issues: [LIST]

Functional Test: PASS / FAIL
- Issues: [LIST]

Regression Test: PASS / FAIL
- Issues: [LIST]

Overall Status: READY / NEEDS FIXES

Critical Bugs: [LIST]
Minor Issues: [LIST]
Suggestions: [LIST]
```
