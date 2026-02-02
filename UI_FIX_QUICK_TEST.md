# UI State Machine Fix - Quick Test Guide

## CRITICAL TEST (Do This First!)

### Step 1: Start Application
- Open Unity
- Press Play

### Step 2: Verify Main Menu
- ✅ See dark overlay with "Plunk & Plunder" title
- ✅ See 4 buttons: Play Offline, Host Game, Join Game, Quit
- ✅ Buttons are clickable

### Step 3: Enter Gameplay
- Click **"Play Offline (1 Human + 3 AI)"**
- Wait 1-2 seconds for map generation

### Step 4: CHECK FOR BUG
**Look at the game screen:**

✅ **SUCCESS (Bug Fixed):**
- Main menu is COMPLETELY GONE
- Only see: Gameboard (hex tiles) + Top bar (Turn/Phase) + Bottom-left panel (stats)
- Can click on hex tiles and ships
- No dark overlay
- No semi-transparent menu

❌ **FAILURE (Bug Still Present):**
- See dark semi-transparent overlay over gameboard
- Menu buttons faintly visible
- Can't click on tiles (clicks blocked by invisible menu)
- Gameboard visible but input doesn't work

### Step 5: Check Console Logs
**Look for these SUCCESS indicators:**
```
[UIBootstrapper] Showing screen: GameHUD
[UIBootstrapper] Hiding previous screen: MainMenu
[MainMenuUI] Hidden - deactivated and not raycasting
[GameHUD] Shown - active and raycasting
[UIBootstrapper] ===== VERIFYING GAME UI STATE =====
[UIBootstrapper] Game UI state verification complete
```

**Look for these FAILURE indicators (bug detected):**
```
[UIBootstrapper] CRITICAL BUG: MainMenu is still visible during gameplay!
[UI STATE VIOLATION] After entering gameplay: Found 1 visible 'MainMenu' objects
```

If you see FAILURE logs, the system detected and auto-fixed the bug, but it means the root cause persists.

## Deep Verification (Unity Editor Only)

### Hierarchy Inspection
1. Enter gameplay (click Play Offline)
2. In Unity Hierarchy, find "UI Canvas"
3. Expand it
4. Check children states:

**Expected:**
```
UI Canvas (active)
├─ MainMenu (INACTIVE - unchecked box)
├─ Lobby (INACTIVE - unchecked box)
└─ GameHUD (ACTIVE - checked box)
```

**Bug Present:**
```
UI Canvas (active)
├─ MainMenu (ACTIVE - checked box) ← WRONG!
├─ Lobby (INACTIVE - unchecked box)
└─ GameHUD (ACTIVE - checked box)
```

### Inspector Check (If MainMenu Active)
1. Select MainMenu in Hierarchy
2. Check Inspector components:
   - **CanvasGroup**: alpha should be 0, blocksRaycasts should be FALSE
   - **GraphicRaycaster**: enabled should be FALSE
   - GameObject should be inactive

## Manual Debug Commands (If Stuck)

### Force Hide Menu
Open Unity console (Ctrl+Shift+C) and paste:
```csharp
UnityEngine.Object.FindObjectOfType<PlunkAndPlunder.UI.UIBootstrapper>().ForceHideMainMenu();
```

### Log UI State
```csharp
UnityEngine.Object.FindObjectOfType<PlunkAndPlunder.UI.UIBootstrapper>().DebugLogUIState();
```

### Emergency Cleanup
```csharp
UnityEngine.Object.FindObjectOfType<PlunkAndPlunder.UI.UIBootstrapper>().ForceCleanupMenu();
```

### Log All Canvases
```csharp
PlunkAndPlunder.UI.UIDebugUtility.LogAllCanvases();
```

## Repeat Test (Robustness Check)

1. Enter gameplay (Test 1-4 above)
2. Stop Play mode (Ctrl+Shift+P or click Stop button)
3. Start Play mode again (press Play)
4. Enter gameplay again
5. **Repeat 3 times total**

**Expected:** Bug fix works consistently every time

**Failure:** Bug appears on 2nd or 3rd attempt (indicates initialization issue)

## What Changed (For Reference)

### New Files
- `IUIScreen.cs` - Interface for UI lifecycle
- `UIDebugUtility.cs` - Diagnostic tools

### Modified Files
- `MainMenuUI.cs` - Now implements IUIScreen with CanvasGroup control
- `LobbyUI.cs` - Now implements IUIScreen with CanvasGroup control
- `GameHUD.cs` - Now implements IUIScreen with CanvasGroup control
- `UIBootstrapper.cs` - Complete rewrite with strict state machine
- `GameManager.cs` - Added verification coroutine

## Expected Behavior

### Phase Transitions
```
MainMenu Phase:
  MainMenu = SHOWN (alpha=1, raycasts=true, active)
  Lobby = HIDDEN (alpha=0, raycasts=false, inactive)
  GameHUD = HIDDEN (alpha=0, raycasts=false, inactive)

Lobby Phase:
  MainMenu = HIDDEN
  Lobby = SHOWN
  GameHUD = HIDDEN

Planning/Resolving/Animating Phases:
  MainMenu = HIDDEN
  Lobby = HIDDEN
  GameHUD = SHOWN
```

**GUARANTEE:** Only ONE screen is shown at a time.

## Success Criteria

✅ **BUG FIXED IF:**
1. Main menu disappears completely when entering gameplay
2. No semi-transparent overlay visible
3. Can click on hex tiles and ships without issues
4. Console logs show clean transition with no CRITICAL BUG errors
5. Unity Hierarchy shows MainMenu as INACTIVE during gameplay
6. Repeat test passes (consistent behavior across multiple play sessions)

## Failure Recovery

If the bug still occurs:

1. Run `DebugLogUIState()` to see current state
2. Check console for specific error messages
3. Verify all files were updated correctly
4. Check Unity compilation errors (Console tab)
5. Try `ForceHideMainMenu()` as temporary workaround
6. Report findings with full console log

## Next Steps After Testing

If bug is fixed:
- Remove debug logging (optional - it's not expensive)
- Consider removing `UIDebugUtility` calls from production code
- Keep `IUIScreen` interface and lifecycle management (this is the core fix)

If bug persists:
- Provide full console log from test
- Take screenshot of Unity Hierarchy during bug
- Check if there are scene-placed UI objects conflicting with code-generated ones
