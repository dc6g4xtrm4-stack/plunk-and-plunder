# Quick Test Checklist - Main Menu Bug Fix

## 5-MINUTE SMOKE TEST

### Step 1: Start the Game
- [ ] Unity Editor: Press Play
- [ ] Main Menu appears (dark background, buttons visible)
- [ ] **CHECK CONSOLE**: Should see `[UIBootstrapper] Singleton instance created`

### Step 2: Enter Gameplay
- [ ] Click "Play Offline (1 Human + 3 AI)"
- [ ] Wait 2-3 seconds for map generation
- [ ] **CRITICAL CHECK**: Is the dark menu overlay GONE? (YES = PASS)
- [ ] **CRITICAL CHECK**: Can you see the hex map clearly? (YES = PASS)
- [ ] **CRITICAL CHECK**: Is the menu background completely invisible? (YES = PASS)

### Step 3: Test Input
- [ ] Hover mouse over hex tiles
- [ ] Left-click a ship (should select it)
- [ ] Right-click on a sea tile (should create yellow path)
- [ ] **CHECK**: Did clicks work without double-clicking? (YES = PASS)

### Step 4: Check Console (IMPORTANT)
- [ ] Open Unity Console (Ctrl+Shift+C)
- [ ] Search for "CRITICAL BUG" (should find ZERO results)
- [ ] Search for "VIOLATION" (should find ZERO results)
- [ ] Search for "FORCE DESTROYING" (should find ZERO results)
- [ ] **If you find any**: The bug occurred but was auto-corrected

### Step 5: Verify Hierarchy (Optional but Recommended)
- [ ] In Unity Hierarchy, find "UI Canvas"
- [ ] Expand it
- [ ] **CHECK**: MainMenu child is INACTIVE (grayed out, unchecked)
- [ ] **CHECK**: GameHUD child is ACTIVE (not grayed out, checked)

---

## PASS/FAIL CRITERIA

### ✅ PASS - Bug is Fixed
- Main menu is completely invisible during gameplay
- No semi-transparent dark overlay
- Clicks work on map tiles without issues
- Console shows NO "CRITICAL BUG" or "VIOLATION" errors
- MainMenu GameObject is inactive in hierarchy

### ❌ FAIL - Bug Still Exists
- Semi-transparent dark overlay visible during gameplay
- Menu buttons or text visible during gameplay
- Clicks don't work on map (menu intercepts input)
- Console shows "CRITICAL BUG: MainMenu is still visible"
- Console shows "VIOLATION DETECTED: MainMenu=True"
- MainMenu GameObject is ACTIVE in hierarchy during gameplay

---

## IF THE TEST FAILS

Run this command in Unity Console to diagnose:
```csharp
PlunkAndPlunder.UI.UIDebugUtility.DiagnoseMenuBug();
```

Then report the console output.

---

## ADVANCED TEST (IF YOU HAVE TIME)

### Scene Reload Test (Robustness)
- [ ] With game running, press Ctrl+R (reload scene)
- [ ] Main menu should reappear
- [ ] Click "Play Offline" again
- [ ] **CHECK**: Menu disappears cleanly again? (YES = PASS)
- [ ] **CHECK**: Console shows "Destroying duplicate UIBootstrapper"? (YES = EXPECTED)
- [ ] Repeat 3 times total
- [ ] **CHECK**: Behavior consistent each time? (YES = PASS)

---

## EMERGENCY FIX (IF BUG PERSISTS)

If the bug still occurs, run this in Unity Console:
```csharp
PlunkAndPlunder.UI.UIDebugUtility.NuclearCleanup();
```

This will force-destroy ALL menu objects. You may need to restart the game after.

---

## EXPECTED CONSOLE OUTPUT (SUCCESS)

```
[UIBootstrapper] Singleton instance created
[UIBootstrapper] Created UI Canvas
[UIBootstrapper] Created all UI screens
[UIBootstrapper] ===== SHOWING SCREEN: MainMenu =====
[MainMenuUI] Shown - active and raycasting

(User clicks Play Offline)

[UIBootstrapper] ===== PHASE CHANGED: Planning =====
[UIBootstrapper] ===== SHOWING SCREEN: GameHUD =====
[UIBootstrapper] Force hiding MainMenu
[MainMenuUI] Hidden - deactivated and not raycasting
[GameHUD] Shown - active and raycasting
[UIBootstrapper] Screen transition complete: GameHUD is now active
[UIBootstrapper] ===== VERIFYING GAME UI STATE =====
[UIBootstrapper] Game UI state verification complete
```

NO errors or warnings should appear.

---

## TIME TO TEST: ~5 minutes

✅ All steps passed? **Bug is FIXED!**
❌ Any step failed? **Run DiagnoseMenuBug() and report output**
