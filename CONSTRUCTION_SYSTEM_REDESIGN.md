# Construction System Redesign - Game Design Document

**Author**: Senior Game Designer
**Date**: 2026-01-31
**Status**: Proposal for Review
**Scope**: Complete refactor of Structure/Unit construction, queuing, and UI systems

---

## Executive Summary

The current construction system has become convoluted with responsibilities scattered across GameHUD, TurnResolver, and multiple Order classes. Building ships requires manual button clicks that immediately deduct gold, build queues are processed in TurnResolver, and deployment logic is mixed with UI code. This proposal presents a clean, event-driven architecture with clear separation of concerns.

---

## Current System Analysis - Critical Issues

### 1. **Immediate Gold Deduction in UI Layer**
**Location**: `GameHUD.cs:751`
```csharp
// Immediately add to build queue for instant visual feedback
selectedStructure.buildQueue.Add(queueItem);
// Deduct gold immediately
player.gold -= BuildingConfig.BUILD_SHIP_COST;
```

**Problem**:
- UI layer directly mutates game state
- Gold deducted before order validation
- Can lead to desync if order is rejected
- No transaction rollback on failure

### 2. **Mixed Responsibilities**
**GameHUD.cs** handles:
- UI rendering ✓
- Order creation ✓
- Gold validation ✗ (should be in validator)
- Queue modification ✗ (should be in structure manager)
- Path visualization ✗ (should be separate)

### 3. **Fragile Build Queue Processing**
**Location**: `TurnResolver.cs:1177-1219`
- Modifies queue directly: `shipyard.buildQueue.RemoveAt(0)`
- No atomic transactions
- Events generated after mutation (hard to rollback)
- No validation of queue integrity

### 4. **Order vs Instant Action Confusion**
- **BuildShipOrder**: Goes through order system, validated on submit
- **BuildShip UI**: Adds to queue immediately in UI, THEN creates order
- **Deploy Shipyard**: Creates order immediately, consumes ship on resolution
- Inconsistent mental models

### 5. **No Construction Manager**
- No single source of truth for construction state
- Build progress scattered across Structure.buildQueue
- No centralized validation
- Can't easily query "what's being built where"

---

## Proposed Architecture

### Core Principle: **Command Pattern + Event Sourcing**

All construction actions flow through a centralized **ConstructionManager** that:
1. Validates requests
2. Creates immutable commands
3. Executes commands atomically
4. Emits events for UI updates
5. Maintains construction state separate from structures

---

## New Component Structure

```
Construction/
├── ConstructionManager.cs           # Central coordinator
├── ConstructionCommand.cs            # Base command class
├── Commands/
│   ├── QueueShipCommand.cs          # Add ship to queue
│   ├── CancelConstructionCommand.cs # Remove from queue
│   ├── DeployShipyardCommand.cs     # Convert ship to shipyard
│   └── PrioritizeQueueCommand.cs    # Reorder queue
├── ConstructionState.cs             # Current construction snapshot
├── ConstructionValidator.cs         # Validation rules
├── ConstructionProcessor.cs         # Turn processing
└── ConstructionEvents.cs            # Event definitions

UI/Construction/
├── ConstructionPanel.cs             # Main construction UI
├── BuildQueueWidget.cs              # Queue visualization
├── ShipyardDeploymentUI.cs          # Deployment visualization
└── ConstructionTooltip.cs           # Hover info

Visualization/
└── ConstructionVisualizer.cs        # Path/placement visualization
```

---

## Detailed Component Design

### 1. ConstructionManager (Singleton Service)

```csharp
namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Central authority for all construction operations
    /// Manages queue state, validation, and command execution
    /// </summary>
    public class ConstructionManager : MonoBehaviour
    {
        public static ConstructionManager Instance { get; private set; }

        // Current construction state (read-only from outside)
        private ConstructionState state;
        public ConstructionState State => state.Clone(); // Immutable copy

        // Events for UI updates
        public event Action<ConstructionQueuedEvent> OnConstructionQueued;
        public event Action<ConstructionProgressedEvent> OnConstructionProgressed;
        public event Action<ConstructionCompletedEvent> OnConstructionCompleted;
        public event Action<ConstructionCancelledEvent> OnConstructionCancelled;

        /// <summary>
        /// Queue a ship for construction at a shipyard
        /// Returns: Success + job ID, or Failure + reason
        /// </summary>
        public ConstructionResult QueueShip(int playerId, string shipyardId)
        {
            // 1. Validate request
            var validation = ConstructionValidator.ValidateQueueShip(
                playerId, shipyardId, state, GameManager.Instance.state
            );

            if (!validation.isValid)
                return ConstructionResult.Failure(validation.reason);

            // 2. Create command
            var command = new QueueShipCommand(playerId, shipyardId);

            // 3. Execute (atomic)
            var result = command.Execute(state, GameManager.Instance.state);

            if (result.success)
            {
                // 4. Emit event
                OnConstructionQueued?.Invoke(new ConstructionQueuedEvent {
                    jobId = result.jobId,
                    shipyardId = shipyardId,
                    itemType = "Ship",
                    turnsRequired = BuildingConfig.SHIP_BUILD_TIME,
                    cost = BuildingConfig.BUILD_SHIP_COST
                });
            }

            return result;
        }

        /// <summary>
        /// Process all construction queues for turn advancement
        /// Called by TurnResolver
        /// </summary>
        public List<GameEvent> ProcessTurn(int turnNumber)
        {
            return ConstructionProcessor.ProcessAllQueues(state, turnNumber);
        }

        /// <summary>
        /// Deploy a ship as a shipyard (consumes ship)
        /// </summary>
        public ConstructionResult DeployShipyard(int playerId, string shipId, HexCoord position)
        {
            var validation = ConstructionValidator.ValidateDeployShipyard(
                playerId, shipId, position, GameManager.Instance.state
            );

            if (!validation.isValid)
                return ConstructionResult.Failure(validation.reason);

            var command = new DeployShipyardCommand(playerId, shipId, position);
            return command.Execute(state, GameManager.Instance.state);
        }
    }
}
```

### 2. ConstructionState (Centralized State)

```csharp
/// <summary>
/// Immutable snapshot of all construction activity
/// Replaces scattered Structure.buildQueue fields
/// </summary>
[Serializable]
public class ConstructionState
{
    // All active construction jobs
    public Dictionary<string, ConstructionJob> activeJobs;

    // Queue per shipyard (ordered list of job IDs)
    public Dictionary<string, List<string>> shipyardQueues;

    // Job lookup by ID
    public ConstructionJob GetJob(string jobId) => activeJobs.GetValueOrDefault(jobId);

    // Query methods
    public List<ConstructionJob> GetQueueForShipyard(string shipyardId);
    public int GetQueueLength(string shipyardId);
    public bool IsQueueFull(string shipyardId);
    public ConstructionJob GetActiveJob(string shipyardId); // First in queue

    // Immutable copy
    public ConstructionState Clone();
}

/// <summary>
/// Individual construction job
/// </summary>
[Serializable]
public class ConstructionJob
{
    public string jobId;              // Unique identifier
    public string shipyardId;         // Where it's being built
    public int playerId;              // Who ordered it
    public string itemType;           // "Ship"
    public int turnsRemaining;        // 3 -> 2 -> 1 -> 0
    public int turnsTotal;            // 3 (for progress %)
    public int costPaid;              // Gold already spent
    public ConstructionStatus status; // Queued, Building, Completed, Cancelled

    public float ProgressPercent => 1f - (turnsRemaining / (float)turnsTotal);
}

public enum ConstructionStatus
{
    Queued,    // Waiting in queue (not active yet)
    Building,  // First in queue, actively progressing
    Completed, // Finished, ship spawned
    Cancelled  // Cancelled by player
}
```

### 3. ConstructionValidator (Separate Validation Logic)

```csharp
/// <summary>
/// All validation rules in one place
/// Pure functions - no side effects
/// </summary>
public static class ConstructionValidator
{
    public static ValidationResult ValidateQueueShip(
        int playerId,
        string shipyardId,
        ConstructionState constructionState,
        GameState gameState)
    {
        // 1. Shipyard exists and is owned by player
        Structure shipyard = gameState.structureManager.GetStructure(shipyardId);
        if (shipyard == null)
            return ValidationResult.Invalid("Shipyard not found");

        if (shipyard.type != StructureType.SHIPYARD)
            return ValidationResult.Invalid("Structure is not a shipyard");

        if (shipyard.ownerId != playerId)
            return ValidationResult.Invalid("You don't own this shipyard");

        // 2. Queue has space
        if (constructionState.IsQueueFull(shipyardId))
            return ValidationResult.Invalid($"Queue is full ({BuildingConfig.MAX_QUEUE_SIZE}/{BuildingConfig.MAX_QUEUE_SIZE})");

        // 3. Player has gold
        Player player = gameState.playerManager.GetPlayer(playerId);
        if (player == null || player.gold < BuildingConfig.BUILD_SHIP_COST)
            return ValidationResult.Invalid($"Insufficient gold (need {BuildingConfig.BUILD_SHIP_COST}g, have {player?.gold ?? 0}g)");

        return ValidationResult.Valid();
    }

    public static ValidationResult ValidateDeployShipyard(
        int playerId,
        string shipId,
        HexCoord position,
        GameState gameState)
    {
        // 1. Ship exists and is owned by player
        Unit ship = gameState.unitManager.GetUnit(shipId);
        if (ship == null)
            return ValidationResult.Invalid("Ship not found");

        if (ship.ownerId != playerId)
            return ValidationResult.Invalid("You don't own this ship");

        // 2. Ship is on harbor tile
        if (ship.position != position)
            return ValidationResult.Invalid("Ship must be at target position");

        Tile tile = gameState.grid.GetTile(position);
        if (tile == null || tile.type != TileType.HARBOR)
            return ValidationResult.Invalid("Must deploy on harbor tile");

        // 3. No structure already at position
        Structure existingStructure = gameState.structureManager.GetStructureAtPosition(position);
        if (existingStructure != null)
            return ValidationResult.Invalid("Harbor already has a structure");

        // 4. Player has gold for deployment
        Player player = gameState.playerManager.GetPlayer(playerId);
        if (player == null || player.gold < BuildingConfig.DEPLOY_SHIPYARD_COST)
            return ValidationResult.Invalid($"Insufficient gold for deployment (need {BuildingConfig.DEPLOY_SHIPYARD_COST}g)");

        return ValidationResult.Valid();
    }
}

public struct ValidationResult
{
    public bool isValid;
    public string reason;

    public static ValidationResult Valid() => new ValidationResult { isValid = true };
    public static ValidationResult Invalid(string reason) => new ValidationResult { isValid = false, reason = reason };
}
```

### 4. QueueShipCommand (Atomic Command)

```csharp
/// <summary>
/// Atomic command to queue a ship
/// Either succeeds completely or fails with no side effects
/// </summary>
public class QueueShipCommand : ConstructionCommand
{
    private int playerId;
    private string shipyardId;

    public QueueShipCommand(int playerId, string shipyardId)
    {
        this.playerId = playerId;
        this.shipyardId = shipyardId;
    }

    public override ConstructionResult Execute(ConstructionState constructionState, GameState gameState)
    {
        // Create new job
        string jobId = GenerateJobId();
        var job = new ConstructionJob
        {
            jobId = jobId,
            shipyardId = shipyardId,
            playerId = playerId,
            itemType = "Ship",
            turnsRemaining = BuildingConfig.SHIP_BUILD_TIME,
            turnsTotal = BuildingConfig.SHIP_BUILD_TIME,
            costPaid = BuildingConfig.BUILD_SHIP_COST,
            status = constructionState.GetQueueLength(shipyardId) == 0
                ? ConstructionStatus.Building
                : ConstructionStatus.Queued
        };

        // Atomic state updates
        constructionState.activeJobs[jobId] = job;
        constructionState.shipyardQueues[shipyardId].Add(jobId);

        // Deduct gold (only after state update succeeds)
        Player player = gameState.playerManager.GetPlayer(playerId);
        player.gold -= BuildingConfig.BUILD_SHIP_COST;

        Debug.Log($"[QueueShipCommand] Queued ship at {shipyardId} for player {playerId}, job {jobId}");

        return ConstructionResult.Success(jobId);
    }

    private string GenerateJobId() => $"job_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
}
```

---

## UI Layer - Clean Separation

### ConstructionPanel.cs

```csharp
/// <summary>
/// Main construction UI panel
/// Displays shipyard info and build queue
/// ONLY reads state, NEVER mutates
/// </summary>
public class ConstructionPanel : MonoBehaviour
{
    private BuildQueueWidget queueWidget;
    private Text shipyardNameText;
    private Button buildShipButton;

    private string currentShipyardId;

    public void ShowForShipyard(string shipyardId)
    {
        currentShipyardId = shipyardId;
        gameObject.SetActive(true);

        // Read state from ConstructionManager
        var queue = ConstructionManager.Instance.State.GetQueueForShipyard(shipyardId);
        queueWidget.DisplayQueue(queue);

        // Update button state
        UpdateBuildButton();
    }

    private void UpdateBuildButton()
    {
        var state = ConstructionManager.Instance.State;
        bool canBuild = !state.IsQueueFull(currentShipyardId);

        buildShipButton.interactable = canBuild;
        buildShipButton.GetComponentInChildren<Text>().text = canBuild
            ? $"Build Ship ({BuildingConfig.BUILD_SHIP_COST}g)"
            : $"Queue Full ({state.GetQueueLength(currentShipyardId)}/{BuildingConfig.MAX_QUEUE_SIZE})";
    }

    private void OnBuildShipClicked()
    {
        // Delegate to ConstructionManager
        var result = ConstructionManager.Instance.QueueShip(
            playerId: 0, // Human player
            shipyardId: currentShipyardId
        );

        if (!result.success)
        {
            // Show error tooltip
            TooltipManager.Instance.ShowError(result.reason);
        }
        else
        {
            // Success feedback
            AudioManager.Instance.PlaySound("construction_queued");
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        ConstructionManager.Instance.OnConstructionQueued += HandleConstructionQueued;
        ConstructionManager.Instance.OnConstructionProgressed += HandleConstructionProgressed;
    }

    private void OnDisable()
    {
        // Unsubscribe
        ConstructionManager.Instance.OnConstructionQueued -= HandleConstructionQueued;
        ConstructionManager.Instance.OnConstructionProgressed -= HandleConstructionProgressed;
    }

    private void HandleConstructionQueued(ConstructionQueuedEvent evt)
    {
        if (evt.shipyardId == currentShipyardId)
        {
            // Refresh display
            ShowForShipyard(currentShipyardId);
        }
    }

    private void HandleConstructionProgressed(ConstructionProgressedEvent evt)
    {
        if (evt.shipyardId == currentShipyardId)
        {
            queueWidget.UpdateProgress(evt.jobId, evt.turnsRemaining);
        }
    }
}
```

### BuildQueueWidget.cs

```csharp
/// <summary>
/// Visual representation of build queue
/// 5 slots, shows progress, status, ETA
/// </summary>
public class BuildQueueWidget : MonoBehaviour
{
    private List<QueueSlotView> slots = new List<QueueSlotView>();

    public void DisplayQueue(List<ConstructionJob> queue)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < queue.Count)
            {
                slots[i].Display(queue[i], isActive: i == 0);
            }
            else
            {
                slots[i].DisplayEmpty(i + 1);
            }
        }
    }

    public void UpdateProgress(string jobId, int turnsRemaining)
    {
        var slot = slots.Find(s => s.CurrentJobId == jobId);
        slot?.UpdateProgress(turnsRemaining);
    }
}

/// <summary>
/// Individual queue slot visualization
/// </summary>
public class QueueSlotView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Text labelText;
    [SerializeField] private Image progressBar;
    [SerializeField] private Text etaText;

    public string CurrentJobId { get; private set; }

    public void Display(ConstructionJob job, bool isActive)
    {
        CurrentJobId = job.jobId;

        // Visual style based on status
        if (isActive)
        {
            background.color = HUDStyles.ActiveConstructionColor; // Green
            labelText.text = $"⚙ Building: {job.itemType}";
        }
        else
        {
            background.color = HUDStyles.QueuedConstructionColor; // Yellow
            labelText.text = $"⏳ Queued: {job.itemType}";
        }

        // Progress bar
        progressBar.fillAmount = job.ProgressPercent;

        // ETA
        etaText.text = job.turnsRemaining > 0
            ? $"{job.turnsRemaining} turn{(job.turnsRemaining > 1 ? "s" : "")}"
            : "Complete!";

        gameObject.SetActive(true);
    }

    public void DisplayEmpty(int slotNumber)
    {
        CurrentJobId = null;
        background.color = HUDStyles.EmptySlotColor; // Gray
        labelText.text = $"Slot {slotNumber}: Empty";
        progressBar.fillAmount = 0;
        etaText.text = "";
        gameObject.SetActive(true);
    }

    public void UpdateProgress(int turnsRemaining)
    {
        var job = ConstructionManager.Instance.State.GetJob(CurrentJobId);
        if (job != null)
        {
            progressBar.fillAmount = job.ProgressPercent;
            etaText.text = turnsRemaining > 0
                ? $"{turnsRemaining} turn{(turnsRemaining > 1 ? "s" : "")}"
                : "Complete!";
        }
    }
}
```

---

## Turn Resolution Integration

### TurnResolver Changes

```csharp
// OLD: TurnResolver directly mutates Structure.buildQueue
shipyard.buildQueue.RemoveAt(0);

// NEW: Delegate to ConstructionManager
public List<GameEvent> ResolveTurn(List<IOrder> orders, int turnNumber)
{
    List<GameEvent> events = new List<GameEvent>();

    // Process construction BEFORE orders
    events.AddRange(ConstructionManager.Instance.ProcessTurn(turnNumber));

    // ... rest of turn resolution
}
```

### ConstructionProcessor.cs

```csharp
public static class ConstructionProcessor
{
    public static List<GameEvent> ProcessAllQueues(ConstructionState state, int turnNumber)
    {
        List<GameEvent> events = new List<GameEvent>();

        // Process each shipyard's queue
        foreach (var kvp in state.shipyardQueues)
        {
            string shipyardId = kvp.Key;
            List<string> queue = kvp.Value;

            if (queue.Count == 0) continue;

            // Get first job (actively building)
            string activeJobId = queue[0];
            ConstructionJob job = state.GetJob(activeJobId);

            if (job == null || job.status != ConstructionStatus.Building)
                continue;

            // Advance progress
            job.turnsRemaining--;

            events.Add(new ConstructionProgressedEvent {
                jobId = job.jobId,
                shipyardId = shipyardId,
                turnsRemaining = job.turnsRemaining
            });

            // Check completion
            if (job.turnsRemaining <= 0)
            {
                events.AddRange(CompleteConstruction(job, state));
            }
        }

        return events;
    }

    private static List<GameEvent> CompleteConstruction(ConstructionJob job, ConstructionState state)
    {
        List<GameEvent> events = new List<GameEvent>();

        // Spawn ship
        Structure shipyard = GameManager.Instance.state.structureManager.GetStructure(job.shipyardId);
        Unit newShip = GameManager.Instance.state.unitManager.CreateUnit(
            job.playerId,
            shipyard.position,
            UnitType.SHIP
        );

        // Update job status
        job.status = ConstructionStatus.Completed;

        // Remove from queue
        state.shipyardQueues[job.shipyardId].RemoveAt(0);

        // Promote next job to Building status
        if (state.shipyardQueues[job.shipyardId].Count > 0)
        {
            string nextJobId = state.shipyardQueues[job.shipyardId][0];
            ConstructionJob nextJob = state.GetJob(nextJobId);
            nextJob.status = ConstructionStatus.Building;
        }

        // Create event
        events.Add(new ShipBuiltEvent(
            turnNumber: GameManager.Instance.state.turnNumber,
            shipId: newShip.id,
            shipyardId: job.shipyardId,
            playerId: job.playerId,
            position: shipyard.position,
            cost: job.costPaid
        ));

        Debug.Log($"[ConstructionProcessor] Completed {job.itemType} at {job.shipyardId}, spawned {newShip.id}");

        return events;
    }
}
```

---

## Visualization System

### ConstructionVisualizer.cs

```csharp
/// <summary>
/// Handles visual feedback for construction actions
/// - Preview where shipyard will be deployed
/// - Highlight valid harbor tiles
/// - Show construction progress overlays
/// </summary>
public class ConstructionVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject deploymentPreviewPrefab;
    [SerializeField] private Material validHarborMaterial;
    [SerializeField] private Material invalidHarborMaterial;

    private GameObject currentPreview;
    private List<GameObject> highlightedTiles = new List<GameObject>();

    /// <summary>
    /// Show deployment preview when ship is selected on harbor
    /// </summary>
    public void ShowDeploymentPreview(HexCoord position, bool isValid)
    {
        ClearPreview();

        currentPreview = Instantiate(deploymentPreviewPrefab, position.ToWorldPosition(), Quaternion.identity);

        // Visual feedback for validity
        var renderer = currentPreview.GetComponentInChildren<Renderer>();
        renderer.material = isValid ? validHarborMaterial : invalidHarborMaterial;
    }

    public void ClearPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    /// <summary>
    /// Highlight all valid harbor tiles for deployment
    /// </summary>
    public void HighlightValidHarbors(Unit ship, GameState state)
    {
        ClearHighlights();

        // Find all harbors within ship's movement range
        List<HexCoord> harbors = FindAccessibleHarbors(ship, state);

        foreach (var harbor in harbors)
        {
            GameObject highlight = CreateTileHighlight(harbor);
            highlightedTiles.Add(highlight);
        }
    }

    public void ClearHighlights()
    {
        foreach (var highlight in highlightedTiles)
        {
            Destroy(highlight);
        }
        highlightedTiles.Clear();
    }

    private List<HexCoord> FindAccessibleHarbors(Unit ship, GameState state)
    {
        // TODO: Use pathfinding to find reachable harbors
        return new List<HexCoord>();
    }

    private GameObject CreateTileHighlight(HexCoord position)
    {
        // TODO: Create glowing outline on tile
        return new GameObject("TileHighlight");
    }
}
```

---

## Migration Strategy

### Phase 1: Add New System (Parallel)
1. Implement ConstructionManager, ConstructionState, ConstructionValidator
2. Keep old code functional
3. Add feature flag: `useNewConstructionSystem`

### Phase 2: Migrate UI
1. Create ConstructionPanel, BuildQueueWidget
2. Wire to ConstructionManager
3. Test with feature flag ON
4. Compare behavior with old system

### Phase 3: Migrate Turn Resolution
1. Update TurnResolver to use ConstructionProcessor
2. Remove direct buildQueue mutations
3. Extensive testing

### Phase 4: Remove Old Code
1. Delete old BuildShipOrder immediate mutation logic
2. Remove Structure.buildQueue (replace with references to ConstructionState)
3. Clean up GameHUD button handlers

### Phase 5: Polish
1. Add animations for queue updates
2. Improve visual feedback
3. Add sound effects
4. Implement queue reordering/cancellation

---

## Benefits of New System

### 1. **Clear Responsibility**
- ConstructionManager: Business logic
- ConstructionValidator: Validation rules
- ConstructionPanel: UI rendering
- ConstructionVisualizer: Visual feedback

### 2. **Atomic Transactions**
- Commands either succeed completely or fail with no side effects
- No partial state mutations
- Easy to add rollback/undo

### 3. **Testability**
- Pure validation functions
- Commands can be unit tested
- State can be snapshotted and compared

### 4. **Extensibility**
- Easy to add new construction types (buildings, upgrades)
- Queue management features (pause, cancel, reorder)
- Multi-stage construction
- Resource requirements beyond gold

### 5. **Debuggability**
- Single source of truth for construction state
- Event log for all construction actions
- Clear command execution flow

### 6. **UI/Logic Separation**
- UI only reads state and sends commands
- Never mutates game state directly
- Easy to add new UI views (mobile, VR, etc.)

---

## Acceptance Criteria

✅ UI cannot directly mutate game state
✅ All construction actions go through ConstructionManager
✅ Gold is deducted atomically with queue addition
✅ Build queue processing is transactional
✅ Validation rules are centralized and testable
✅ UI updates reactively to construction events
✅ Deployment preview shows valid/invalid placements
✅ Queue displays progress bars and ETAs
✅ System supports 5-item queue per shipyard
✅ Ships spawn at shipyard position on completion

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Migration breaks existing saves | High | High | Implement save migration script |
| Performance regression with large queues | Low | Medium | Profile with 100+ active jobs |
| UI event desync | Medium | Medium | Add reconciliation checks |
| Old code dependencies | High | Low | Gradual migration with feature flags |

---

## Timeline Estimate

- Phase 1 (New System): 2-3 days
- Phase 2 (UI Migration): 2 days
- Phase 3 (Turn Resolution): 1 day
- Phase 4 (Old Code Removal): 1 day
- Phase 5 (Polish): 2-3 days

**Total**: 8-10 development days + 2 days testing

---

## Open Questions for Review

1. **Queue Cancellation**: Should players be able to cancel queued items? Refund policy?
2. **Queue Reordering**: Allow dragging items to reorder priority?
3. **Multiple Construction Types**: Support for building upgrades, defenses, etc.?
4. **Resource System**: Expand beyond gold (wood, iron, etc.)?
5. **Construction Speed Modifiers**: Bonuses from upgrades, technologies?
6. **Batch Operations**: Queue multiple ships at once?
7. **Save Format**: Serialize ConstructionState or reconstruct from Structure state?

---

## Conclusion

This refactor transforms a fragile, UI-driven system into a robust, testable, event-driven architecture. The Command Pattern provides atomic transactions, the centralized ConstructionManager gives a single source of truth, and clean separation enables independent UI/logic evolution.

**Recommendation**: Approve for implementation with phased rollout using feature flags.
