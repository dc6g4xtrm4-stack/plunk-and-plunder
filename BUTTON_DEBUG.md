# Debug: Play Offline Button Not Working

## Symptoms
- Host button: ✅ Works
- Quit button: ✅ Works
- Play Offline button: ❌ Dead (no response)

## Possible Causes

### 1. Button Rendering Order Issue
The offline button is created first and might be covered by another UI element.

### 2. Callback Not Assigned
The UnityAction might not be properly assigned to the button.

### 3. Silent Exception
StartOfflineGame() might be throwing an exception that's being caught silently.

## Immediate Test

Try this in Unity Editor:

1. **In Hierarchy while game is running:**
   - Find: DontDestroyOnLoad → UIBootstrapper → UI Canvas → MainMenu
   - Look for button objects

2. **Select each button and check Inspector:**
   - Button component should show "On Click ()" with an entry
   - Verify "Play Offline" button has a click handler

3. **Add Debug.Log at the very start of OnOfflineClicked:**

```csharp
private void OnOfflineClicked()
{
    Debug.Log("========== BUTTON CLICKED ==========");
    Debug.Log("[MainMenuUI] Starting offline game");
    // rest of code...
}
```

## Quick Fix Attempt

Try changing button order - move offline button creation to AFTER host:

```csharp
// Buttons - try different order
hostButton = CreateButton("Host Game (Steam)", new Vector2(0, 100), OnHostClicked);
offlineButton = CreateButton("Play Offline (1 Human + 3 AI)", new Vector2(0, 0), OnOfflineClicked);
joinButton = CreateButton("Join Game (Steam)", new Vector2(0, -100), OnJoinClicked);
quitButton = CreateButton("Quit", new Vector2(0, -200), OnQuitClicked);
```

## Alternative: Recreate Button

Replace the button creation with this explicit version:

```csharp
// Create offline button explicitly
GameObject offlineButtonObj = new GameObject("Button_PlayOffline");
offlineButtonObj.transform.SetParent(transform, false);

RectTransform rect = offlineButtonObj.AddComponent<RectTransform>();
rect.sizeDelta = new Vector2(400, 60);
rect.anchoredPosition = new Vector2(0, 100);

Image bg = offlineButtonObj.AddComponent<Image>();
bg.color = new Color(0.2f, 0.2f, 0.2f);

offlineButton = offlineButtonObj.AddComponent<Button>();
offlineButton.targetGraphic = bg;

// Explicitly create the callback
offlineButton.onClick.AddListener(() => {
    Debug.Log("OFFLINE BUTTON LAMBDA FIRED");
    OnOfflineClicked();
});

// Add text
GameObject textObj = new GameObject("Text");
textObj.transform.SetParent(offlineButtonObj.transform, false);
Text text = textObj.AddComponent<Text>();
text.text = "Play Offline (1 Human + 3 AI)";
text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
text.fontSize = 24;
text.alignment = TextAnchor.MiddleCenter;
text.color = Color.white;

RectTransform textRect = textObj.GetComponent<RectTransform>();
textRect.anchorMin = Vector2.zero;
textRect.anchorMax = Vector2.one;
textRect.offsetMin = Vector2.zero;
textRect.offsetMax = Vector2.zero;
```

This will help identify if it's a CreateButton issue or an OnOfflineClicked issue.
