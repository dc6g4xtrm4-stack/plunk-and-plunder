# Combat Visualization System Redesign

## Executive Summary

**Problem**: Current combat UI obscures the game board, making it unclear where combat occurs and what led to it.

**Solution**: Move combat visualization TO the game board with:
- Red connection lines between combatants
- Camera auto-focus on combat zones
- Non-intrusive corner event log
- Clear movement path visualization
- Floating damage numbers at combat locations

---

## Design Principles

1. **Board-First**: Primary combat indicators on the game board, not in popup boxes
2. **Clarity**: Every combat should clearly show who fought whom and why
3. **Non-Intrusive**: UI elements should not block the game board
4. **Deterministic**: All visuals must work with deterministic combat resolution
5. **Scalable**: Must handle 1v1, 1vMany, and ManyVMany scenarios

---

## Core Components

### 1. Combat Connection Lines

**Component**: `CombatConnectionRenderer.cs`

**Purpose**: Draw visual lines connecting combatants on the game board

**Visual Design**:
```
Attacker Ship ━━━━━━━━━━> Defender Ship
              ╲  -5 dmg  ╱
               ╲        ╱
                ╲      ╱
```

**Implementation**:
- **Line Style**:
  - Dotted/dashed red line (LineRenderer component)
  - Width: 0.1 units
  - Material: Unlit with additive blending for glow effect

- **Line Variations**:
  - **Standard Combat**: Red dashed line
  - **Mutual Destruction**: Red solid line, both ends pulse
  - **Ongoing Combat**: Orange dashed line (persists between turns)
  - **One-sided Victory**: Green dashed line (for player wins)

- **Damage Display**:
  - Billboard text at line midpoint
  - Shows damage dealt: "-5 HP"
  - Color: White text with red outline
  - Font size scales with camera distance
  - Fades in over 0.2s, persists 2s, fades out 0.3s

- **Animation**:
  - Line draws from attacker to defender (0.2s)
  - Arrow pulses at defender end
  - Damage number floats up slightly

**Data Requirements**:
- Attacker position (HexCoord)
- Defender position (HexCoord)
- Damage dealt
- Combat outcome (ongoing, destroyed, mutual)

---

### 2. Combat Event Log

**Component**: `CombatEventLog.cs`

**Purpose**: Persistent, scrollable log of all combat events in the current turn

**UI Layout**:
```
┌─────────────────────────────┐
│ COMBAT LOG         [X]      │
├─────────────────────────────┤
│ ⚔️ Salt Spray → Black Pearl │
│    -5 HP (Turn 3, Tick 2)   │
│                              │
│ ⚔️ Typhoon → Stormbreaker   │
│    -3 HP (Turn 3, Tick 1)   │
│                              │
│ 💀 Tempest destroyed         │
│    by Maelstrom (Turn 3)    │
└─────────────────────────────┘
```

**Position**: Bottom-left corner, 300px width, up to 400px height

**Features**:
- Auto-scrolls to latest entry
- Click entry to focus camera on that location
- Color-coded:
  - Green text: Player ships involved and victorious
  - Red text: Player ships involved and damaged/destroyed
  - Gray text: AI-only combat
- Collapsible (minimize to just icon)
- Shows up to 20 recent combats
- Persists through entire turn

**Entry Format**:
```
[Icon] [Attacker Name] → [Defender Name]
       [Outcome] (Turn X, Tick Y)
```

**Icons**:
- ⚔️ Combat occurred
- 💀 Ship destroyed
- 🔥 Mutual destruction
- 🔁 Ongoing combat

---

### 3. Smart Combat Camera

**Enhancement**: `CameraController.cs` + new `CombatCameraManager.cs`

**Purpose**: Automatically focus camera on combat as it occurs

**Behavior**:
1. **Single Combat**:
   - Frame both attacker and defender in view
   - Smooth transition (0.5s lerp)
   - Zoom to show both ships clearly
   - Hold for 1.5s after combat resolves

2. **Multiple Combats**:
   - Queue combat locations
   - Visit each in sequence
   - Faster transitions (0.3s) if many combats
   - Skip AI-only combats unless player has combat visualization setting enabled

3. **Multi-Combatant (3v1, etc.)**:
   - Frame entire combat zone
   - Show all attackers and defender
   - Hold longer (2s) for player to process

4. **Edge Cases**:
   - If player manually moves camera during combat, cancel auto-focus
   - If more than 5 combats occur, show combat log notification instead
   - Configurable: "Auto-follow combat" toggle in settings

**Algorithm**:
```csharp
void FocusOnCombat(List<HexCoord> positions)
{
    // Calculate bounding box
    Vector3 min, max = CalculateBounds(positions);
    Vector3 center = (min + max) / 2;

    // Calculate zoom level to fit all positions
    float distance = CalculateZoom(min, max);

    // Smooth transition
    SmoothMoveTo(center, distance, duration: 0.5f);
}
```

---

### 4. Floating Damage Numbers

**Component**: `FloatingDamageNumber.cs`

**Purpose**: Show damage dealt at the combat location in world space

**Visual Design**:
```
      -5
     ↑
  [Ship]
```

**Implementation**:
- **Text Style**:
  - Bold, sans-serif font
  - Size: 0.5 world units (scales with camera)
  - Color: Red for damage, green for healing (future)
  - Outline: Black, 2px
  - Billboard (faces camera)

- **Animation**:
  - Spawns at defender position + (0, 1.5, 0) offset
  - Floats up: +0.5 units over 1.5 seconds
  - Fades out: Alpha 1 → 0 over last 0.5 seconds
  - Slight random horizontal offset to prevent stacking

- **Lifecycle**:
  - Created when combat damage applied
  - Self-destructs after 2 seconds
  - Pooled for performance (reuse gameobjects)

**Multi-hit Scenario**:
- If same ship takes multiple hits in quick succession:
  - Show separate numbers, offset horizontally
  - Or combine: "-5, -3" with slight delay between appearances

---

### 5. Tick-by-Tick Movement Visualization

**Enhancement**: `TurnAnimator.cs` + new `MovementPathRenderer.cs`

**Purpose**: Show where ships are moving and when combat occurs

**Visual Design**:
```
Ship A: [o]─────→[x]
                  ↓
                [Ship B]

Ghost trail: [·]────[o] (faded previous position)
```

**Implementation**:

**During Movement Phase**:
1. **Active Movement Paths**:
   - Show line from ship's current position to next position
   - Yellow/orange color for active movement
   - Fades after ship moves

2. **Ghost Trails**:
   - Semi-transparent copy of ship at previous hex
   - Fades out over 0.5s
   - Only show for last position, not entire path
   - Uses UnitRenderer with 40% alpha

3. **Combat Range Indicators**:
   - When ship moves adjacent to enemy, highlight border red
   - Pulse effect on hex border
   - Shows "entering combat range" before combat resolves

4. **Tick Synchronization**:
   - All ships move simultaneously on same tick
   - Pause between ticks: 0.3s (configurable)
   - Show "TICK X" indicator at top of screen

**Tick Display**:
```
┌─────────────────┐
│   TICK 2 / 5    │
└─────────────────┘
```

---

### 6. Corner Combat Panel

**Redesign**: `CombatResultsHUD.cs`

**Purpose**: Show current combat details without blocking board

**Old Design** (Current):
- Center screen, 500x400px
- Modal popup style
- Blocks game view

**New Design**:
```
┌──────────────────────────┐
│ CURRENT COMBAT           │
├──────────────────────────┤
│ Salt Spray (Player 1)    │
│ HP: ████░░ 8/10          │
│ Dealt: 5 damage          │
│                          │
│        ⚔️ VS             │
│                          │
│ Black Pearl (AI 2)       │
│ HP: ██░░░░ 3/8           │
│ Dealt: 2 damage          │
│                          │
│ Result: Ongoing combat   │
└──────────────────────────┘
```

**Position**: Bottom-right corner, 280px width, auto height

**Features**:
- Compact design
- Semi-transparent background (80% opacity)
- Auto-hides after 3 seconds
- Click to expand for full details
- Shows health bars inline
- Minimize button to hide

**States**:
- **Expanded**: Full details as shown above
- **Minimized**: Just icon with count: "⚔️ 3 combats"
- **Hidden**: Completely off screen

---

### 7. Multi-Combatant Visualization

**Component**: `MultiCombatRenderer.cs`

**Purpose**: Handle scenarios with 3+ ships fighting over same tile

**Scenario 1: Many vs One (3 attackers, 1 defender)**
```
     [Ship A]
        ╲
         ╲  -4
    -3   ╲
[Ship B]━━━[Target]
          ╱
      -2 ╱
        ╱
     [Ship C]
```

**Implementation**:
- Draw all attack lines to target
- Each line shows individual damage
- Lines offset slightly to avoid overlap
- Target pulses red
- Camera frames entire group

**Scenario 2: Multiple Separate Combats**
```
[A]━━→[B]     [C]━━→[D]
```

**Implementation**:
- Show all combats simultaneously
- Use combat event log to track
- Camera focuses on player's combat first
- If multiple player combats, queue them

**Scenario 3: Collision Combat (2 ships enter same hex)**
```
       [Hex X]
        ↙  ↘
   [Ship A] [Ship B]
```

**Implementation**:
- Show both movement paths converging
- Flash red at collision hex
- Draw combat line between ships after collision
- "COLLISION!" indicator at hex center

---

### 8. Ongoing Combat Indicators

**Component**: `OngoingCombatIndicator.cs`

**Purpose**: Show when ships are locked in multi-turn combat

**Visual Design**:
```
   ┌──────┐
   │[Ship]│  ← Red pulsing border
   └──────┘
     ⚔️ vs Ship B
```

**Implementation**:
- Red ring around ship (0.1 unit thick)
- Pulsing animation (scale 1.0 → 1.1 → 1.0, 1s cycle)
- Small text label showing opponent name
- Persists through Planning phase
- Removed when combat ends or ship destroyed

**Behavior**:
- Automatically applied when `unit.isInCombat == true`
- Uses `unit.combatOpponentId` to show opponent
- Different color for player vs AI: Red for player, orange for AI
- Tooltip on hover: "Continuing combat with [opponent]"

---

### 9. Health Bar Optimization

**Enhancement**: `UnitRenderer.cs` (UpdateHealthBar method)

**Problem**: Too many health bars clutter the map

**Solution**: Conditional visibility

**Visibility Rules**:
```csharp
bool ShouldShowHealthBar(Unit unit)
{
    // Always show if selected
    if (unit == selectedUnit) return true;

    // Always show if in active combat
    if (unit.isInCombat) return true;

    // Show for 5 seconds after taking damage
    if (Time.time - unit.lastDamageTime < 5f) return true;

    // Show if health not full
    if (unit.health < unit.maxHealth) return true;

    // Otherwise hide
    return false;
}
```

**Animation**:
- Fade in over 0.2s when becoming visible
- Fade out over 0.3s when hiding
- Smooth alpha transition

**Performance**:
- Only update visible health bars each frame
- Hidden health bars don't render

---

## Technical Implementation Details

### Line Renderer Setup

```csharp
public class CombatConnectionRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Material lineMaterial;

    void Initialize()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Dashed effect
        lineMaterial.SetFloat("_DashSize", 0.5f);
        lineMaterial.SetFloat("_GapSize", 0.3f);
    }

    public void ShowCombatLine(Vector3 attacker, Vector3 defender, int damage)
    {
        lineRenderer.SetPosition(0, attacker + Vector3.up * 0.5f);
        lineRenderer.SetPosition(1, defender + Vector3.up * 0.5f);

        // Spawn damage text at midpoint
        Vector3 midpoint = (attacker + defender) / 2f + Vector3.up * 1f;
        SpawnDamageText(midpoint, damage);

        // Auto-hide after 2 seconds
        StartCoroutine(HideAfterDelay(2f));
    }
}
```

### Combat Event Log Entry

```csharp
public class CombatLogEntry
{
    public string attackerName;
    public string defenderName;
    public int damageDealt;
    public bool attackerDestroyed;
    public bool defenderDestroyed;
    public int turnNumber;
    public int tickNumber;
    public HexCoord location;

    public string GetDisplayText()
    {
        string outcome = "";
        if (defenderDestroyed && attackerDestroyed)
            outcome = "Mutual destruction";
        else if (defenderDestroyed)
            outcome = $"{defenderName} destroyed";
        else if (attackerDestroyed)
            outcome = $"{attackerName} destroyed";
        else
            outcome = $"-{damageDealt} HP";

        return $"{attackerName} → {defenderName}: {outcome}";
    }
}
```

### Camera Focus Algorithm

```csharp
public class CombatCameraManager
{
    private Queue<CombatFocusRequest> focusQueue = new Queue<CombatFocusRequest>();

    public void RequestFocus(List<HexCoord> positions, float duration)
    {
        focusQueue.Enqueue(new CombatFocusRequest
        {
            positions = positions,
            duration = duration
        });
    }

    public IEnumerator ProcessFocusQueue()
    {
        while (focusQueue.Count > 0)
        {
            var request = focusQueue.Dequeue();

            // Calculate bounds
            Bounds bounds = CalculateBounds(request.positions);

            // Move camera
            yield return cameraController.FocusOnBounds(bounds, 0.5f);

            // Hold
            yield return new WaitForSeconds(request.duration);
        }
    }
}
```

---

## Integration with Existing System

### TurnAnimator Changes

**Current Flow**:
```
AnimateCombat(event) → Fire OnCombatOccurred → Wait if paused
```

**New Flow**:
```
AnimateCombat(event) →
    1. Show combat connection line
    2. Spawn floating damage numbers
    3. Request camera focus
    4. Fire OnCombatOccurred
    5. Add to combat log
    6. Wait for camera transition + display duration
    7. Hide combat line
```

**Code Changes** (`TurnAnimator.cs`):
```csharp
private IEnumerator AnimateCombat(CombatOccurredEvent e, GameState state)
{
    Unit attacker = state.unitManager.GetUnit(e.attackerId);
    Unit defender = state.unitManager.GetUnit(e.defenderId);

    // Show connection line
    combatConnectionRenderer.ShowCombatLine(
        attacker.position.ToWorldPosition(1f),
        defender.position.ToWorldPosition(1f),
        e.damageToDefender
    );

    // Spawn damage numbers
    floatingDamageRenderer.SpawnDamageNumber(
        defender.position.ToWorldPosition(1f),
        e.damageToDefender
    );

    // Request camera focus
    combatCameraManager.RequestFocus(
        new List<HexCoord> { attacker.position, defender.position },
        duration: 1.5f
    );

    // Add to log
    combatEventLog.AddEntry(e, state);

    // Fire event for HUD
    OnCombatOccurred?.Invoke(e);
    OnAnimationStep?.Invoke(state);

    // Wait for visualization
    yield return new WaitForSeconds(combatPauseDelay * 2f);
}
```

### GameManager Changes

**Modifications** (`GameManager.cs`):
```csharp
private void HandleCombatOccurred(CombatOccurredEvent combatEvent)
{
    // Get units
    Unit attacker = state.unitManager.GetUnit(combatEvent.attackerId);
    Unit defender = state.unitManager.GetUnit(combatEvent.defenderId);

    // OLD: Show center popup
    // combatResultsHUD.ShowCombatResult(combatEvent, state);

    // NEW: Update corner panel
    combatCornerPanel.UpdateCombat(combatEvent, state);

    // Combat connection line handled by TurnAnimator
    // Combat log handled by TurnAnimator
    // Camera focus handled by CombatCameraManager

    // Award salvage gold
    if (combatEvent.defenderDestroyed && attacker != null)
    {
        state.playerManager.GetPlayer(attacker.ownerId).gold += 25;

        // Show floating "+25g" text at salvage location
        floatingTextRenderer.SpawnText(
            defender.position.ToWorldPosition(1f),
            "+25g",
            Color.yellow
        );
    }
}
```

---

## Settings & Configuration

### Player Preferences

```csharp
public class CombatVisualizationSettings
{
    public bool autoFollowCombat = true;          // Camera auto-focus
    public bool showCombatLines = true;           // Connection lines
    public bool showDamageNumbers = true;         // Floating numbers
    public bool showCombatLog = true;             // Event log
    public bool showAICombat = false;             // Show AI vs AI
    public float combatDisplayDuration = 1.5f;    // How long to show
    public bool pauseOnPlayerCombat = false;      // Pause animation
}
```

### Inspector Tweakables

```csharp
[Header("Combat Visualization")]
public float combatLineDuration = 2f;
public float damageNumberDuration = 1.5f;
public float cameraTransitionSpeed = 0.5f;
public Color playerCombatColor = Color.green;
public Color enemyCombatColor = Color.red;
public Color mutualDestructionColor = Color.orange;
```

---

## Accessibility Considerations

1. **Color Blind Mode**:
   - Use patterns in addition to colors (dashed vs solid)
   - Add text labels for all indicators
   - High contrast options

2. **Visual Clarity**:
   - Configurable text size for damage numbers
   - Toggle to disable animations
   - Slow-motion replay option

3. **Audio Cues**:
   - Sound effect when combat occurs
   - Different tones for player win/loss
   - Spatial audio (combat sound from direction)

---

## Performance Considerations

1. **Object Pooling**:
   - Pool LineRenderer objects
   - Pool damage number TextMeshPro objects
   - Pool combat log UI entries

2. **Culling**:
   - Don't render combat lines outside camera view
   - Limit simultaneous damage numbers to 10
   - Cache world position calculations

3. **Batching**:
   - Use same material for all combat lines
   - Use texture atlas for combat icons
   - Batch damage numbers into single mesh

4. **Memory**:
   - Combat log limited to 20 entries
   - Auto-cleanup old entries
   - Clear all visual indicators after turn ends

---

## Testing Scenarios

### Scenario 1: Single Combat
**Setup**: 2 ships, adjacent, both attack
**Expected**:
- Red line connects ships
- Damage numbers float above each
- Camera frames both
- Combat log shows 2 entries
- Health bars update

### Scenario 2: 3v1 Gang-Up
**Setup**: 3 ships surround 1 ship, all attack
**Expected**:
- 3 lines converge on target
- 3 damage numbers (offset)
- Camera frames entire group
- Log shows 3 entries
- Target likely destroyed

### Scenario 3: Collision Combat
**Setup**: 2 ships move into same hex
**Expected**:
- Movement paths shown converging
- Red flash at collision hex
- Combat line appears
- "COLLISION!" indicator
- Log shows collision entry

### Scenario 4: Multi-Turn Combat
**Setup**: 2 ships fight, both survive, continue next turn
**Expected**:
- Red rings persist on both ships through Planning
- Indicator shows opponent names
- On next turn, rings remain until combat resolves

### Scenario 5: Many Simultaneous Combats
**Setup**: 8 ships in 4 pairs, all fighting
**Expected**:
- Camera visits player combats first
- AI combats shown briefly or skipped
- Log shows all 4 combats
- No UI clutter

---

## Success Metrics

1. **Clarity**: Players can identify combat participants at a glance
2. **Visibility**: No game board obstruction by UI
3. **Context**: Clear what actions led to combat
4. **Performance**: 60 FPS with 10+ simultaneous combats
5. **Determinism**: Works identically across all clients

---

## Future Enhancements

1. **Combat Replay**: Scrub through turn to review combats
2. **Combat Statistics**: Track kills, damage dealt/taken
3. **Kill Feed**: MMO-style notifications in corner
4. **Battle Animations**: Ships rock/flash on hit
5. **Particle Effects**: Cannon fire, explosions, water splashes
6. **Sound Design**: Cannon booms, ship creaks, destruction sounds

---

## Implementation Timeline

**Phase 1** (Foundation - 2-3 days):
- Combat connection line renderer
- Floating damage numbers
- Combat event log UI

**Phase 2** (Integration - 2-3 days):
- Smart camera system
- TurnAnimator integration
- Corner combat panel

**Phase 3** (Polish - 2-3 days):
- Multi-combatant scenarios
- Ongoing combat indicators
- Health bar optimization

**Phase 4** (Testing - 1-2 days):
- All test scenarios
- Performance optimization
- Bug fixes

**Total Estimate**: 7-11 days

---

## Conclusion

This redesign shifts combat visualization from intrusive popups to elegant, board-based indicators that:
- ✅ Keep focus on the game board
- ✅ Clearly show combat participants and outcomes
- ✅ Provide context for what led to combat
- ✅ Handle complex multi-combatant scenarios
- ✅ Maintain deterministic resolution
- ✅ Improve player experience dramatically

The system is modular, configurable, and built for scalability.