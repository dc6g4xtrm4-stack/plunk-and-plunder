# Pass Turn Button Debug Guide

## Debug Logging Added

I've added extensive logging to trace the Pass Turn button click from TopBarHUD → GameHUD → GameManager → GameEngine.

## Expected Log Flow (When Working)

When you click the **Pass Turn** button after queuing an upgrade order, you should see this sequence:

### 1. TopBarHUD (Button Click)
```
🔘 [TopBarHUD] ========== PASS TURN BUTTON CLICKED! ==========
[TopBarHUD] GameHUD found, sending OnPassTurnClicked message...
[TopBarHUD] Message sent to GameHUD
```

### 2. GameHUD (Receive Message)
```
🔘 [GameHUD] ========== OnPassTurnClicked RECEIVED! ==========
[GameHUD] GameManager.Instance=exists
[GameHUD] pendingPlayerOrders.Count=1
[GameHUD] ✅ Calling GameManager.SubmitOrders(playerId=0, orderCount=1)...
[GameHUD] ✅ GameManager.SubmitOrders() CALLED!
[GameHUD] ✅ Orders submitted and state cleared!
```

### 3. GameManager (Submit Orders)
```
🎮 [GameManager] ========== SubmitOrders CALLED! ==========
[GameManager] playerId=0, orders.Count=1
[GameManager]   Order: UpgradeSailsOrder
[GameManager] Player 0 validation passed, calling engine.SubmitOrders()...
[GameManager] ✅ Player 0 submitted 1 orders (validated by engine), allReady=True
[GameManager] All players ready! Changing phase to Resolving...
```

### 4. Phase Change
```
[GameManager] Resolving turn X
[GameManager] Turn resolved with Y events
```

## Possible Failure Scenarios

### Scenario A: Button Click Not Detected
**Symptoms**: No "PASS TURN BUTTON CLICKED" log appears

**Causes**:
- Button not interactable (disabled state)
- Button missing onClick listener
- UI blocker preventing clicks

**Check**:
- Is button green (enabled)?
- Are there orders queued? (TopBarHUD shows "Orders: 1")

### Scenario B: GameHUD Not Found
**Symptoms**: See "❌ GameHUD NOT FOUND!" in logs

**Causes**:
- GameHUD destroyed or not initialized
- FindFirstObjectByType failed

**Fix**:
- Check Unity Hierarchy - GameHUD should exist as child of Canvas

### Scenario C: No Pending Orders
**Symptoms**: See "⚠️ BLOCKED: No pending orders to submit!"

**Causes**:
- Orders weren't actually queued when button was clicked
- pendingPlayerOrders list was cleared before submission

**Check**:
- Did you see "✅ Upgrade sails order QUEUED" message earlier?
- What does TopBarHUD show for order count?

### Scenario D: GameManager is Null
**Symptoms**: See "❌ BLOCKED: GameManager.Instance is NULL!"

**Causes**:
- GameManager destroyed or not initialized
- Singleton instance not set

**Fix**:
- Restart the game - GameManager should be created in Awake()

### Scenario E: Player Not Found
**Symptoms**: See "❌ Player 0 NOT FOUND!" in GameManager

**Causes**:
- Game state not initialized properly
- Player 0 doesn't exist

**Fix**:
- Check if offline game was started correctly

### Scenario F: Orders Submitted But Phase Doesn't Change
**Symptoms**: See "Not all players ready yet" instead of "Changing phase to Resolving"

**Causes**:
- In multiplayer mode, waiting for other players
- In offline mode, this shouldn't happen (allReady should be true)

**Check**:
- Are you in offline mode or multiplayer mode?

## Testing Steps

1. **Start offline game** (Play Offline button)

2. **Queue an upgrade order**:
   - Select a ship at a shipyard
   - Click "Upgrade Sails" button
   - Should see: "✅ Upgrade sails order QUEUED for unit_X!"
   - Top bar should show: "Orders: 1"

3. **Click Pass Turn button**:
   - Button should be green (enabled)
   - Check console for the log sequence above

4. **Report results**:
   - Paste ALL console logs from step 2-3
   - Which step failed (TopBarHUD, GameHUD, GameManager)?
   - What error messages appeared?

## Quick Diagnosis

**If you see**:
- ✅ All three "=========" headers → System is working correctly
- ✅ TopBar and GameHUD headers, but NO GameManager header → Problem is in GameHUD.OnPassTurnClicked()
- ✅ TopBar header only → Problem is in SendMessage or GameHUD not found
- ❌ No headers at all → Button click not being detected

---

**Run the test and paste the console output here!**
