# Rendering Issue - Troubleshooting

## Issue Found
**Problem**: GameBootstrap script reference was broken in MainScene.unity
**Error**: "The referenced script on this Behaviour (Game Object 'GameBootstrap') is missing!"

## Root Cause
The MainScene.unity file had a placeholder GUID that didn't match the actual GameBootstrap.cs script GUID.

## Fix Applied
Updated MainScene.unity to reference the correct GUID:
```
guid: 1976bc7386a9c744cb923be0521987c1
```

## Additional Setup Required

Unity needs an **EventSystem** for UI input. Here's how to add it properly:

### Option 1: Let Unity Auto-Create (Recommended)
1. Open MainScene.unity in Unity
2. Delete the existing EventSystem GameObject if present
3. In Hierarchy, right-click → UI → EventSystem
4. Unity will auto-create a proper EventSystem

### Option 2: Manual Scene Recreation
If the scene still has issues:

1. **Delete MainScene.unity** completely
2. **Create new scene** in Unity: File → New Scene
3. **Add GameBootstrap**:
   - Create Empty GameObject (Ctrl+Shift+N)
   - Name it "GameBootstrap"
   - Add Component → Scripts → GameBootstrap
4. **Save scene** as MainScene in Assets/Scenes/

### Option 3: Check After Fix
The scene should now work. To verify:

1. Open Unity project
2. Open MainScene.unity
3. Check Console for errors
4. Press Play
5. UI should appear

## What GameBootstrap Does

When the scene starts:
1. `GameBootstrap.Awake()` runs
2. Creates GameManager
3. Creates NetworkManager
4. Creates Renderers (HexRenderer, UnitRenderer)
5. **Creates UIBootstrapper**
6. UIBootstrapper creates all UI:
   - Main Menu
   - Lobby
   - Game HUD

## Expected Initialization Order

```
Scene Loads
    ↓
GameBootstrap.Awake()
    ↓
GameManager created (DontDestroyOnLoad)
    ↓
NetworkManager created (DontDestroyOnLoad)
    ↓
Renderers created
    ↓
UIBootstrapper created
    ↓
UIBootstrapper.Start()
    ↓
UI screens created:
  - MainMenuUI
  - LobbyUI
  - GameHUD
    ↓
Main Menu shown
```

## Debugging Steps

### 1. Check Console
Look for these messages:
```
[GameBootstrap] GameManager created
[GameBootstrap] NetworkManager created
[GameBootstrap] Renderers created
[GameBootstrap] UI created
[GameBootstrap] Camera configured
[GameBootstrap] Plunk & Plunder initialized
```

### 2. Check Hierarchy
After pressing Play, you should see:
```
MainScene
├── GameBootstrap
└── EventSystem

DontDestroyOnLoad:
├── GameManager
├── NetworkManager
├── Renderers
│   ├── HexRenderer
│   └── UnitRenderer
└── UIBootstrapper
    └── UI Canvas
        ├── MainMenu
        ├── Lobby
        └── GameHUD
```

### 3. Check for Missing Scripts
In Hierarchy, any GameObject with a yellow warning icon = missing script

### 4. Enable Debug Logging
In GameManager.cs, set:
```csharp
public bool enableDeterministicLogging = true;
```

## Common Issues

### Issue: "NullReferenceException" on GameManager.Instance
**Cause**: GameManager not created
**Fix**: Check GameBootstrap is attached to GameObject in scene

### Issue: "No EventSystem in scene"
**Cause**: Missing EventSystem
**Fix**: Add UI → EventSystem in Hierarchy

### Issue: Black screen, no errors
**Cause**: Camera not configured or UI not showing
**Fix**:
1. Check Main Camera exists
2. Check Canvas is in ScreenSpaceOverlay mode
3. Check MainMenuUI.gameObject.SetActive(true) is called

### Issue: Scripts compile but scene doesn't load
**Cause**: Scene file corrupted or has wrong references
**Fix**: Recreate scene (see Option 2 above)

## Quick Fix Script

If nothing works, you can create a simple test scene:

1. Create new scene
2. Create empty GameObject "TestInit"
3. Add this script:

```csharp
using UnityEngine;
using PlunkAndPlunder.Core;

public class TestInit : MonoBehaviour
{
    void Start()
    {
        Debug.Log("TestInit starting...");

        // Create GameManager
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        Debug.Log("GameManager created, should start game soon...");
    }
}
```

4. Press Play - should see Main Menu

## Verification Checklist

After fix, verify:
- [ ] Scene opens without "missing script" error
- [ ] Console shows GameBootstrap initialization messages
- [ ] GameManager appears in Hierarchy (DontDestroyOnLoad)
- [ ] UI Canvas appears
- [ ] Main Menu is visible
- [ ] Can click buttons

---

**Status**: Scene fixed, needs Unity reload
**Next**: Open Unity, let it reimport, then test
