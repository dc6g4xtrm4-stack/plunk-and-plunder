# Button Click Debugging Guide

## Current Issue

Buttons are being detected by EventSystem ("Click on UI detected"), but the OnClick events aren't firing. No logs from button handlers appear.

## Debug Logging Added

I've added extensive logging to trace the issue. Here's what to look for:

### 1. Button Creation Logs (On Startup)

When LeftPanelHUD initializes, you should see:
```
[LeftPanelHUD] Created button 'DeployShipyard' with onClick listener attached
[LeftPanelHUD] Created button 'BuildShip' with onClick listener attached
[LeftPanelHUD] Created button 'UpgradeSails' with onClick listener attached
[LeftPanelHUD] Created button 'UpgradeCannons' with onClick listener attached
[LeftPanelHUD] Created button 'UpgradeMaxLife' with onClick listener attached
```

**If you DON'T see these**: LeftPanelHUD.BuildActionButtonsSection() isn't being called.

### 2. Button State Update Logs (When Selecting Unit)

When you select a unit, you should see:
```
[LeftPanelHUD] UpdateActionButtons called - gameState=True, phase=Planning
[LeftPanelHUD] Human player gold: 150, Selected unit: unit_1
[LeftPanelHUD] Button states:
  DeployShipyard: False
  BuildShip: False
  UpgradeSails: True (canUpgrade=True, sails=0, gold=150)
  UpgradeCannons: True
  UpgradeMaxLife: True
```

**If buttons show `False`**: They're disabled, so clicks won't work!

### 3. Button Click Logs (When Clicking Button)

When you click an upgrade button, you should see:
```
🔘 [LeftPanelHUD] ========== UPGRADE SAILS BUTTON CLICKED! ==========
[LeftPanelHUD] GameHUD found, calling OnUpgradeSailsClicked
[GameHUD] ✅ Upgrade sails order QUEUED for unit_xxx!
```

**If you DON'T see the "BUTTON CLICKED" log**: The onClick listener isn't firing!

## Diagnostic Scenarios

### Scenario A: No Button Creation Logs
**Problem**: Buttons aren't being created at all.
**Solution**: Check if LeftPanelHUD.Initialize() is being called.

### Scenario B: Buttons Created, But All Show `interactable: False`
**Problem**: Buttons are disabled due to validation.
**Causes**:
- `gameState` is null
- Game phase isn't "Planning"
- Not enough gold
- Unit not at shipyard (for upgrades)

**Check the logs for**:
```
[LeftPanelHUD] UpdateActionButtons called - gameState=???, phase=???
```

If gameState is null or phase isn't Planning, buttons will be disabled.

### Scenario C: Buttons Show `interactable: True`, But Clicks Don't Work
**Problem**: Button component isn't receiving clicks properly.
**Possible causes**:
- targetGraphic not set (FIXED in latest code)
- raycastTarget = false on Image (FIXED in latest code)
- Another UI element blocking clicks
- EventSystem issue

### Scenario D: Click Logs Appear, But Order Not Queued
**Problem**: Button works, but GameHUD handler has validation issue.
**Check for warnings**:
```
[GameHUD] Ship must be at a friendly shipyard to upgrade
[GameHUD] Not enough gold to upgrade sails
```

## Testing Steps

1. **Start the game in Unity**

2. **Check console for button creation logs**:
   - Should see 5 "Created button" messages
   - If not, LeftPanelHUD didn't initialize properly

3. **Select a unit (click on a ship)**:
   - Should see "UpdateActionButtons called"
   - Should see "Button states:" with True/False for each button
   - If all False, check the reasons (gameState, phase, gold, etc.)

4. **Click an enabled button**:
   - Should see "🔘 ========== BUTTON CLICKED! =========="
   - If you don't see this, the onClick event isn't firing
   - If you DO see it, check for validation warnings

5. **Report back with**:
   - All console output from steps 1-4
   - Screenshot of the HUD
   - Which buttons are visible/enabled

## Quick Fixes to Try

### If gameState is null:
The issue is that LeftPanelHUD is initialized before GameState exists.

**Fix**: Call `leftPanelHUD.UpdatePlayerStats()` after game starts.

### If buttons never show as enabled:
Check BuildingConfig values - they might be wrong.

**Test**: Add this temporary code:
```csharp
actionButtons["UpgradeSails"].interactable = true; // Force enable for testing
```

If this makes the button clickable, then the validation logic is the problem.

### If onClick never fires:
The Button component might not be set up correctly.

**Check in Unity Inspector**:
1. Find LeftPanelHUD → ActionButtonsSection → UpgradeSailsButton
2. Look at Button component
3. Check if "On Click()" has any entries
4. Check if "Interactable" is checked

## Expected Working Output

```
[LeftPanelHUD] Created button 'UpgradeSails' with onClick listener attached
[GameHUD] Unit selected: unit_1
[LeftPanelHUD] UpdateActionButtons called - gameState=True, phase=Planning
[LeftPanelHUD] Human player gold: 150, Selected unit: unit_1
[LeftPanelHUD] Button states:
  UpgradeSails: True (canUpgrade=True, sails=0, gold=150)
[GameHUD] Click on UI detected - skipping game world input
🔘 [LeftPanelHUD] ========== UPGRADE SAILS BUTTON CLICKED! ==========
[LeftPanelHUD] Found GameHUD, sending message...
[GameHUD] ✅ Upgrade sails order QUEUED for unit_xxx! Pending orders: 1
[GameHUD] 💡 Click PASS TURN button (top-right) to submit orders and apply upgrades!
```

---

**Please run the game and paste the console output!** This will tell us exactly what's failing.
