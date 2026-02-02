# Troubleshooting & Debug Guide

## Common Issues

### Upgrade Orders Not Working
**Symptoms:** Ship stats don't change after upgrade button clicked
**Cause:** Missing validation cases in `GameEngine.ValidateOrders()`
**Fix:** Ensure UpgradeSails, UpgradeCannons, UpgradeMaxLife cases exist in switch statement

### Pass Turn Button Disabled
**Symptoms:** Button stays gray after giving orders
**Cause:** `pendingPlayerOrders.Count == 0` or not in Planning phase
**Debug:** Check console for `[GameHUD] Pass Turn button: DISABLED` messages
**Fix:** Ensure `UpdateHUD()` is called after adding orders

### Health Bar Crash on Upgrade
**Symptoms:** `MissingReferenceException` when ship fully upgraded
**Cause:** Health bar GameObject destroyed during ship model rebuild
**Fix:** Check for null before accessing, recreate if destroyed (fixed in UnitRenderer.cs:785)

### Action Buttons Not Appearing
**Symptoms:** Upgrade buttons don't show when at shipyard
**Cause:** Button visibility logic not checking context correctly
**Fix:** `LeftPanelHUD.UpdateActionButtons()` should show/hide based on selection

### Ships Not Rendering After Upgrade
**Symptoms:** Ship disappears or looks wrong after upgrade
**Cause:** Ship model recreation logic destroying all child objects
**Fix:** Ensure `CreateShipModel()` rebuilds complete ship with new tier

## Debug Tools

### Console Logging
Enable verbose logging in relevant files:
- `[GameEngine]` - Order validation and resolution
- `[TurnResolver]` - Combat and movement resolution
- `[GameHUD]` - UI interactions and order submission
- `[UnitRenderer]` - Ship visual updates

### Debug Commands
Add to `GameManager` for testing:
```csharp
// Give player gold
player.gold += 1000;

// Force upgrade ship
selectedUnit.sails++;
selectedUnit.cannons++;
selectedUnit.maxHealth += 10;
```

### Visual Debugging
- Selection indicators show which unit is selected
- Path visualization shows planned movement
- Health bars show ship health status
- Player colors distinguish ownership

## Testing Checklist

### Basic Gameplay
- [ ] Select unit → unit highlights
- [ ] Right-click → path visualizes
- [ ] Pass Turn → orders execute
- [ ] Combat occurs when units meet
- [ ] Ships destroyed when health reaches 0

### Ship Upgrades
- [ ] Ship at shipyard → upgrade buttons appear
- [ ] Click upgrade → button grays out (can't double-upgrade)
- [ ] Pass Turn → gold deducted, stat increases
- [ ] Ship visual updates (bigger, more sails/cannons)
- [ ] Health bar still displays correctly

### Construction
- [ ] Ship on harbor → Deploy Shipyard button appears
- [ ] Shipyard selected → Build Ship button appears
- [ ] Build Ship → queue displays (3 turns remaining)
- [ ] After 3 turns → new ship spawns

### Multiplayer
- [ ] Host starts server → displays "Waiting for players"
- [ ] Client connects → game starts
- [ ] Both players can submit orders
- [ ] Turn advances only when both ready
- [ ] Game state syncs between players

### Replay
- [ ] Start game → log file created
- [ ] Play several turns → events recorded
- [ ] ESC → Replay → Load log file
- [ ] Playback controls work (play, pause, speed)
- [ ] Game state accurately reconstructed

## Performance Issues

### Frame Rate Drops
- Disable verbose logging (called every frame)
- Check `RenderUnits()` not recreating objects unnecessarily
- Ensure path visualization only updates on changes

### Memory Leaks
- Check GameObject cleanup in `OnDestroy()`
- Ensure dictionaries cleared when appropriate
- Destroy temporary UI elements

## Common Error Messages

### "Unit does not exist"
- Order references destroyed unit
- Check unit still alive before creating order

### "Player does not own this unit"
- AI trying to control player unit (or vice versa)
- Check `unit.ownerId` matches order.playerId

### "Not enough gold"
- Order cost exceeds player.gold
- Check gold before creating order
- Display cost to user

### "Unit must be at shipyard location to upgrade"
- Ship not at exact same tile as shipyard
- Check `unit.position.Equals(shipyard.position)`

## Building & Deployment

### Unity Build Issues
- Ensure all scenes added to build settings
- Check for missing script references
- Verify Unity version compatibility (Unity 6+)

### Network Build
- Use `create_client_build.bat` for standardized builds
- Ensure same Unity version for all players
- Check firewall allows game executable

## Log File Locations
- **Windows**: `C:/Users/[User]/AppData/LocalLow/DefaultCompany/Plunk and Plunder/Logs/`
- **Mac**: `~/Library/Application Support/DefaultCompany/Plunk and Plunder/Logs/`
- **Linux**: `~/.config/unity3d/DefaultCompany/Plunk and Plunder/Logs/`
