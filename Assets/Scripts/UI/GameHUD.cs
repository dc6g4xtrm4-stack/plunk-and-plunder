using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Rendering;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class GameHUD : MonoBehaviour
    {
        // HUD Components (NEW SYSTEM)
        private TopBarHUD topBarHUD;
        private LeftPanelHUD leftPanelHUD;

        // Legacy UI components (to be removed/refactored)
        private EventLogUI eventLog;
        private TileTooltipUI tooltip;
        private BuildQueueUI buildQueueUI;

        // Interaction state
        private Unit selectedUnit;
        private Structure selectedStructure;
        private List<HexCoord> plannedPath;
        private List<IOrder> pendingPlayerOrders = new List<IOrder>();
        private HashSet<string> unitsWithOrders = new HashSet<string>(); // Track which units have pending orders
        private Dictionary<string, List<HexCoord>> pendingMovePaths = new Dictionary<string, List<HexCoord>>(); // Track paths per unit

        // Visualization components
        private PathVisualizer pathVisualizer;

        public void Initialize()
        {
            // CRITICAL: Setup GameHUD RectTransform to fill entire screen
            // This allows child elements to anchor correctly
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // Anchor to fill entire screen
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Debug.Log("[GameHUD] RectTransform configured to fill screen");

            CreateLayout();
            SubscribeToEvents();
            InitializeVisualizers();
        }

        private void InitializeVisualizers()
        {
            // CRITICAL: Create PathVisualizer as sibling of GameHUD, NOT child
            // This prevents it from being affected when GameHUD state changes during combat

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[GameHUD] Cannot find Canvas for PathVisualizer");
                return;
            }

            GameObject pathVisualizerObj = new GameObject("PathVisualizer");
            pathVisualizerObj.transform.SetParent(canvas.transform, false);  // Sibling of GameHUD!
            pathVisualizer = pathVisualizerObj.AddComponent<PathVisualizer>();

            Debug.Log("[GameHUD] PathVisualizer created as Canvas child (independent of GameHUD lifecycle)");
        }

        private void CreateLayout()
        {
            // Create TopBarHUD
            GameObject topBarObj = new GameObject("TopBarHUD");
            topBarObj.transform.SetParent(transform, false);
            topBarHUD = topBarObj.AddComponent<TopBarHUD>();
            topBarHUD.Initialize();

            // Create LeftPanelHUD
            GameObject leftPanelObj = new GameObject("LeftPanelHUD");
            leftPanelObj.transform.SetParent(transform, false);
            leftPanelHUD = leftPanelObj.AddComponent<LeftPanelHUD>();
            leftPanelHUD.Initialize(GameManager.Instance?.state);

            // Event log (keep for now, will move to RightPanelHUD later)
            GameObject eventLogObj = new GameObject("EventLog");
            eventLogObj.transform.SetParent(transform, false);
            eventLog = eventLogObj.AddComponent<EventLogUI>();
            eventLog.Initialize();

            // Tooltip
            GameObject tooltipObj = new GameObject("Tooltip");
            tooltipObj.transform.SetParent(transform, false);
            tooltip = tooltipObj.AddComponent<TileTooltipUI>();
            tooltip.Initialize();

            // Build Queue UI (keep for now, integrated into LeftPanelHUD)
            GameObject buildQueueObj = new GameObject("BuildQueue");
            buildQueueObj.transform.SetParent(transform, false);
            buildQueueUI = buildQueueObj.AddComponent<BuildQueueUI>();
            buildQueueUI.Initialize();

            Debug.Log("[GameHUD] Layout created with TopBarHUD and LeftPanelHUD");
        }


        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                GameManager.Instance.OnTurnResolved += HandleTurnResolved;
            }
        }

        private void Update()
        {
            // Only update HUD when needed, not every frame
            // UpdateHUD is called by state change events instead
            HandleInput();
        }

        private void UpdateHUD()
        {
            if (GameManager.Instance?.state != null)
            {
                GameState state = GameManager.Instance.state;

                // Don't update HUD during MainMenu/Lobby phases
                if (state.phase == GamePhase.MainMenu || state.phase == GamePhase.Lobby)
                {
                    return;
                }

                // Update TopBarHUD
                if (topBarHUD != null)
                {
                    topBarHUD.UpdateTurnInfo(state.turnNumber, state.phase);

                    var humanPlayer = state.playerManager.GetPlayer(0);
                    if (humanPlayer != null)
                    {
                        topBarHUD.UpdateResourceInfo(humanPlayer.gold, pendingPlayerOrders.Count);
                    }

                    // Pass Turn button interactability and pulsing
                    // Enable if there are pending orders OR if there's a path visualization that can be resolved
                    bool hasPendingOrders = pendingPlayerOrders.Count > 0;
                    bool hasPathVisualization = plannedPath != null && plannedPath.Count > 0;
                    bool canSubmit = (state.phase == GamePhase.Planning && (hasPendingOrders || hasPathVisualization));
                    topBarHUD.SetPassTurnInteractable(canSubmit);
                    Debug.Log($"[GameHUD] Pass Turn button: {(canSubmit ? "ENABLED (green)" : "DISABLED (grey)")} - phase={state.phase}, orders={pendingPlayerOrders.Count}, pathViz={hasPathVisualization}");

                    bool shouldPulse = (state.phase == GamePhase.Planning && pendingPlayerOrders.Count > 0 && AllHumanUnitsHaveOrders(state));
                    topBarHUD.SetPassTurnPulsing(shouldPulse);
                }

                // Update LeftPanelHUD
                if (leftPanelHUD != null)
                {
                    leftPanelHUD.UpdatePlayerStats();
                    leftPanelHUD.UpdateSelection(selectedUnit, selectedStructure);
                }
            }
        }


        private void HandleInput()
        {
            if (GameManager.Instance?.state?.phase != GamePhase.Planning)
                return;

            // Left click: select unit or structure
            if (Input.GetMouseButtonDown(0))
            {
                // CRITICAL: Don't handle game world clicks if mouse is over UI!
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("[GameHUD] Click on UI detected - skipping game world input");
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    HexCoord clickedCoord = HexCoord.FromWorldPosition(hit.point, 1f);

                    // Check for units first
                    List<Unit> unitsAtPos = GameManager.Instance.state.unitManager.GetUnitsAtPosition(clickedCoord);
                    if (unitsAtPos.Count > 0)
                    {
                        SelectUnit(unitsAtPos[0]);
                    }
                    else
                    {
                        // Check for structures
                        Structure structure = GameManager.Instance.state.structureManager.GetStructureAtPosition(clickedCoord);
                        if (structure != null)
                        {
                            SelectStructure(structure);
                        }
                        else
                        {
                            // Clear selection
                            ClearSelection();
                        }
                    }
                }
            }

            // Right click: set destination or context menu
            if (Input.GetMouseButtonDown(1) && selectedUnit != null)
            {
                // Don't handle game world clicks if mouse is over UI
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    HexCoord destination = HexCoord.FromWorldPosition(hit.point, 1f);
                    SetUnitDestination(destination);
                }
            }
        }

        private void SelectUnit(Unit unit)
        {
            selectedUnit = unit;
            selectedStructure = null;

            // Show selection indicator
            var unitRenderer = FindFirstObjectByType<UnitRenderer>();
            if (unitRenderer != null)
            {
                unitRenderer.ShowSelectionIndicator(unit.id);

                // Update indicator state based on whether unit has an order
                UpdateSelectionIndicatorState(unit.id);
            }

            // Update path visualization - set this unit's path as primary if it exists
            if (pathVisualizer != null)
            {
                UpdatePathVisualizations();
            }

            // Hide build queue UI
            if (buildQueueUI != null)
            {
                buildQueueUI.HideQueue();
            }

            // Update LeftPanelHUD
            if (leftPanelHUD != null)
            {
                leftPanelHUD.UpdateSelection(selectedUnit, selectedStructure);
            }

            Debug.Log($"[GameHUD] Unit selected: {unit.id}");
        }

        private void SelectStructure(Structure structure)
        {
            selectedUnit = null;
            selectedStructure = structure;
            plannedPath = null;

            // Hide selection indicator when selecting a structure
            var unitRenderer = FindFirstObjectByType<UnitRenderer>();
            if (unitRenderer != null)
            {
                unitRenderer.HideSelectionIndicator();
            }

            // Show build queue if it's a shipyard
            if (buildQueueUI != null && structure.type == StructureType.SHIPYARD)
            {
                buildQueueUI.ShowQueue(structure);
            }
            else if (buildQueueUI != null)
            {
                buildQueueUI.HideQueue();
            }

            // Update path visualization - show all paths with no primary
            if (pathVisualizer != null)
            {
                UpdatePathVisualizations();
            }

            // Update LeftPanelHUD
            if (leftPanelHUD != null)
            {
                leftPanelHUD.UpdateSelection(selectedUnit, selectedStructure);
            }

            Debug.Log($"[GameHUD] Structure selected: {structure.id}");
        }

        private void ClearSelection()
        {
            selectedUnit = null;
            selectedStructure = null;
            plannedPath = null;

            // Hide selection indicator
            var unitRenderer = FindFirstObjectByType<UnitRenderer>();
            if (unitRenderer != null)
            {
                unitRenderer.HideSelectionIndicator();
            }

            // Hide build queue UI
            if (buildQueueUI != null)
            {
                buildQueueUI.HideQueue();
            }

            // Update path visualization - show all paths with no primary
            if (pathVisualizer != null)
            {
                UpdatePathVisualizations();
            }

            // Update LeftPanelHUD
            if (leftPanelHUD != null)
            {
                leftPanelHUD.UpdateSelection(null, null);
            }

            Debug.Log("[GameHUD] Selection cleared");
        }

        private void SetUnitDestination(HexCoord destination)
        {
            if (selectedUnit == null || GameManager.Instance == null)
                return;

            // Don't allow moving if not owned by human player
            if (selectedUnit.ownerId != 0)
                return;

            Pathfinding pathfinding = GameManager.Instance.GetPathfinding();
            List<HexCoord> path = pathfinding.FindPath(selectedUnit.position, destination, 10);

            if (path != null && path.Count > 1)
            {
                plannedPath = path;

                // Check if this unit was in combat and clear combat flags
                bool wasCombatUnit = selectedUnit.isInCombat;
                if (wasCombatUnit)
                {
                    Debug.Log($"[GameHUD] Unit {selectedUnit.id} was in combat - clearing combat flags and overriding combat path");
                    selectedUnit.isInCombat = false;
                    selectedUnit.combatOpponentId = null;
                }

                // Clear any queued path from previous turn (especially combat paths)
                selectedUnit.queuedPath = null;

                // Remove existing move order for this unit if any
                pendingPlayerOrders.RemoveAll(o => o is MoveOrder moveOrder && moveOrder.unitId == selectedUnit.id);

                // Add move order to pending orders
                MoveOrder order = new MoveOrder(selectedUnit.id, 0, path);
                pendingPlayerOrders.Add(order);

                // Track that this unit has an order
                unitsWithOrders.Add(selectedUnit.id);

                // Store the path for this unit
                pendingMovePaths[selectedUnit.id] = path;

                Debug.Log($"[GameHUD] Planned {(wasCombatUnit ? "NEW" : "")} path for {selectedUnit.id}: {path.Count} steps (replacing {(wasCombatUnit ? "combat" : "previous")} path)");

                // Update selection indicator to OrderSet state
                UpdateSelectionIndicatorState(selectedUnit.id);

                // Update path visualization - add/update this unit's path
                if (pathVisualizer != null)
                {
                    UpdatePathVisualizations();
                }

                // Update HUD to enable Pass Turn button
                UpdateHUD();
            }
        }

        public void OnPassTurnClicked()
        {
            Debug.Log("🔘 [GameHUD] ========== OnPassTurnClicked RECEIVED! ==========");
            Debug.Log($"[GameHUD] GameManager.Instance={(GameManager.Instance != null ? "exists" : "NULL")}");
            Debug.Log($"[GameHUD] pendingPlayerOrders.Count={pendingPlayerOrders.Count}");

            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameHUD] ❌ BLOCKED: GameManager.Instance is NULL!");
                return;
            }

            if (pendingPlayerOrders.Count == 0)
            {
                Debug.LogWarning("[GameHUD] ⚠️ BLOCKED: No pending orders to submit!");
                return;
            }

            Debug.Log($"[GameHUD] ✅ Calling GameManager.SubmitOrders(playerId=0, orderCount={pendingPlayerOrders.Count})...");

            // Submit all pending orders
            GameManager.Instance.SubmitOrders(0, pendingPlayerOrders);

            Debug.Log("[GameHUD] ✅ GameManager.SubmitOrders() CALLED!");

            // Clear state
            pendingPlayerOrders.Clear();
            unitsWithOrders.Clear();
            pendingMovePaths.Clear();
            ClearSelection();

            // Clear all path visualizations
            if (pathVisualizer != null)
            {
                pathVisualizer.ClearAllPaths();
            }

            Debug.Log("[GameHUD] ✅ Orders submitted and state cleared!");
        }

        public void OnAutoResolveClicked()
        {
            GameManager.Instance?.AutoResolveTurn();
        }

        public void OnDeployShipyardClicked()
        {
            Debug.Log($"[GameHUD] Deploy Shipyard button clicked!");

            if (GameManager.Instance == null || selectedUnit == null)
            {
                Debug.LogWarning($"[GameHUD] Deploy blocked: GameManager={(GameManager.Instance != null)}, selectedUnit={(selectedUnit != null)}");
                return;
            }

            // Don't allow if not owned by human player
            if (selectedUnit.ownerId != 0)
            {
                Debug.LogWarning($"[GameHUD] Deploy blocked: Unit owned by player {selectedUnit.ownerId}, not human player 0");
                return;
            }

            GameState state = GameManager.Instance.state;
            Tile tile = state.grid.GetTile(selectedUnit.position);

            // Verify ship is on harbor
            if (tile == null || tile.type != TileType.HARBOR)
            {
                Debug.LogWarning($"[GameHUD] Deploy blocked: Tile={tile}, TileType={(tile?.type.ToString() ?? "null")}. Ship must be on harbor to deploy shipyard");
                return;
            }

            Debug.Log($"[GameHUD] All validation passed! Creating deploy order for unit {selectedUnit.id} at {selectedUnit.position}");

            // Show visual indicator on the harbor tile where shipyard will be deployed
            // NOTE: This creates a visible path visualization showing the user where the shipyard will be built
            // The visualization remains visible until orders are submitted
            List<HexCoord> deploymentIndicator = new List<HexCoord> { selectedUnit.position, selectedUnit.position };

            // CRITICAL: Add deployment to pendingMovePaths so it persists through UpdatePathVisualizations
            pendingMovePaths[selectedUnit.id] = deploymentIndicator;

            if (pathVisualizer != null)
            {
                // Create a single-point path to highlight the deployment location
                // The path starts and ends at the same position, which PathVisualizer renders as a highlight
                // Use movement capacity 1 for deployment indicator (not actual movement)
                pathVisualizer.AddPath(selectedUnit.id, deploymentIndicator, isPrimary: true, movementCapacity: 1);
                Debug.Log($"[GameHUD] VISUALIZATION: Showing DEPLOYMENT indicator for shipyard at {selectedUnit.position} (added to pendingMovePaths)");
            }

            // Create deploy shipyard order
            DeployShipyardOrder order = new DeployShipyardOrder(selectedUnit.id, 0, selectedUnit.position);
            pendingPlayerOrders.Add(order);

            // Track that this unit has an order
            unitsWithOrders.Add(selectedUnit.id);

            Debug.Log($"[GameHUD] Deploy shipyard order queued for {selectedUnit.id} at {selectedUnit.position}");

            // Update HUD to enable Pass Turn button
            UpdateHUD();

            // Clear selection since unit will be consumed
            Unit deployedUnit = selectedUnit;
            ClearSelection();
        }

        public void OnBuildShipClicked()
        {
            if (GameManager.Instance == null || selectedStructure == null)
                return;

            // Verify structure is a shipyard owned by human player
            if (selectedStructure.type != StructureType.SHIPYARD || selectedStructure.ownerId != 0)
                return;

            // NEW SYSTEM: Delegate to ConstructionManager
            if (PlunkAndPlunder.Construction.ConstructionManager.Instance != null)
            {
                var result = PlunkAndPlunder.Construction.ConstructionManager.Instance.QueueShip(
                    playerId: 0, // Human player
                    shipyardId: selectedStructure.id
                );

                if (result.success)
                {
                    Debug.Log($"[GameHUD] Successfully queued ship at {selectedStructure.id}, job {result.jobId}");

                    // Refresh display
                    SelectStructure(selectedStructure);
                }
                else
                {
                    // Show error message
                    Debug.LogWarning($"[GameHUD] Failed to queue ship: {result.reason}");
                }
            }
            else
            {
                // ConstructionManager not available - this should not happen in normal gameplay
                Debug.LogError("[GameHUD] ConstructionManager not available! Cannot build ship.");
            }
        }

        public void OnUpgradeSailsClicked()
        {
            if (GameManager.Instance == null || selectedUnit == null)
                return;

            // Don't allow if not owned by human player
            if (selectedUnit.ownerId != 0)
                return;

            GameState state = GameManager.Instance.state;
            var player = state.playerManager.GetPlayer(0);

            // Find shipyard at unit's position
            Structure shipyard = state.structureManager.GetStructureAtPosition(selectedUnit.position);
            if (shipyard == null || shipyard.type != StructureType.SHIPYARD || shipyard.ownerId != 0)
            {
                Debug.LogWarning("[GameHUD] Ship must be at a friendly shipyard to upgrade");
                return;
            }

            // Check if already at max sails
            if (selectedUnit.sails >= BuildingConfig.MAX_SAILS_UPGRADES)
            {
                Debug.LogWarning($"[GameHUD] Ship already has maximum sails upgrades ({BuildingConfig.MAX_SAILS_UPGRADES})");
                return;
            }

            // Check if player has enough gold
            if (player == null || player.gold < BuildingConfig.UPGRADE_SAILS_COST)
            {
                Debug.LogWarning($"[GameHUD] Not enough gold to upgrade sails. Need {BuildingConfig.UPGRADE_SAILS_COST}, have {player?.gold ?? 0}");
                return;
            }

            // Create upgrade sails order
            UpgradeSailsOrder order = new UpgradeSailsOrder(selectedUnit.id, 0, shipyard.id, shipyard.position);
            pendingPlayerOrders.Add(order);

            // Track that this unit has an order
            unitsWithOrders.Add(selectedUnit.id);

            Debug.Log($"[GameHUD] ✅ Upgrade sails order QUEUED for {selectedUnit.id}! Pending orders: {pendingPlayerOrders.Count}");
            Debug.Log($"[GameHUD] 💡 Click PASS TURN button (top-right) to submit orders and apply upgrades!");

            // Update HUD to enable Pass Turn button
            UpdateHUD();
        }

        public void OnUpgradeCannonsClicked()
        {
            if (GameManager.Instance == null || selectedUnit == null)
                return;

            // Don't allow if not owned by human player
            if (selectedUnit.ownerId != 0)
                return;

            GameState state = GameManager.Instance.state;
            var player = state.playerManager.GetPlayer(0);

            // Find shipyard at unit's position
            Structure shipyard = state.structureManager.GetStructureAtPosition(selectedUnit.position);
            if (shipyard == null || shipyard.type != StructureType.SHIPYARD || shipyard.ownerId != 0)
            {
                Debug.LogWarning("[GameHUD] Ship must be at a friendly shipyard to upgrade");
                return;
            }

            // Check if already at max cannons
            if (selectedUnit.cannons >= BuildingConfig.MAX_CANNONS_UPGRADES)
            {
                Debug.LogWarning($"[GameHUD] Ship already has maximum cannons upgrades ({BuildingConfig.MAX_CANNONS_UPGRADES})");
                return;
            }

            // Check if player has enough gold
            if (player == null || player.gold < BuildingConfig.UPGRADE_CANNONS_COST)
            {
                Debug.LogWarning($"[GameHUD] Not enough gold to upgrade cannons. Need {BuildingConfig.UPGRADE_CANNONS_COST}, have {player?.gold ?? 0}");
                return;
            }

            // Create upgrade cannons order
            UpgradeCannonsOrder order = new UpgradeCannonsOrder(selectedUnit.id, 0, shipyard.id, shipyard.position);
            pendingPlayerOrders.Add(order);

            // Track that this unit has an order
            unitsWithOrders.Add(selectedUnit.id);

            Debug.Log($"[GameHUD] ✅ Upgrade cannons order QUEUED for {selectedUnit.id}! Pending orders: {pendingPlayerOrders.Count}");

            // Update HUD to enable Pass Turn button
            UpdateHUD();
        }

        public void OnUpgradeMaxLifeClicked()
        {
            if (GameManager.Instance == null || selectedUnit == null)
                return;

            // Don't allow if not owned by human player
            if (selectedUnit.ownerId != 0)
                return;

            GameState state = GameManager.Instance.state;
            var player = state.playerManager.GetPlayer(0);

            // Find shipyard at unit's position
            Structure shipyard = state.structureManager.GetStructureAtPosition(selectedUnit.position);
            if (shipyard == null || shipyard.type != StructureType.SHIPYARD || shipyard.ownerId != 0)
            {
                Debug.LogWarning("[GameHUD] Ship must be at a friendly shipyard to upgrade");
                return;
            }

            // Check if already at max health tier
            if (selectedUnit.maxHealth >= BuildingConfig.MAX_SHIP_TIER)
            {
                Debug.LogWarning($"[GameHUD] Ship already at maximum health tier ({BuildingConfig.MAX_SHIP_TIER})");
                return;
            }

            // Check if player has enough gold
            if (player == null || player.gold < BuildingConfig.UPGRADE_MAX_LIFE_COST)
            {
                Debug.LogWarning($"[GameHUD] Not enough gold to upgrade max life. Need {BuildingConfig.UPGRADE_MAX_LIFE_COST}, have {player?.gold ?? 0}");
                return;
            }

            // Create upgrade max life order
            UpgradeMaxLifeOrder order = new UpgradeMaxLifeOrder(selectedUnit.id, 0, shipyard.id, shipyard.position);
            pendingPlayerOrders.Add(order);

            // Track that this unit has an order
            unitsWithOrders.Add(selectedUnit.id);

            Debug.Log($"[GameHUD] ✅ Upgrade max life order QUEUED for {selectedUnit.id}! Pending orders: {pendingPlayerOrders.Count}");

            // Update HUD to enable Pass Turn button
            UpdateHUD();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            // Update HUD when phase changes
            UpdateHUD();

            // Clear pending orders when new turn starts
            if (phase == GamePhase.Planning)
            {
                pendingPlayerOrders.Clear();
                unitsWithOrders.Clear();
                pendingMovePaths.Clear();
                ClearSelection();

                // Clear all path visualizations
                if (pathVisualizer != null)
                {
                    pathVisualizer.ClearAllPaths();
                }

                // Restore remaining paths from previous turn's incomplete moves
                RestoreRemainingPaths();
            }
        }

        private void HandleTurnResolved(List<GameEvent> events)
        {
            eventLog?.AddEvents(events);

            // Store remaining paths from partial movement events for next turn
            StoreRemainingPathsFromEvents(events);
        }

        /// <summary>
        /// Store remaining paths from UnitMovedEvents that had partial movement
        /// </summary>
        private void StoreRemainingPathsFromEvents(List<GameEvent> events)
        {
            if (GameManager.Instance?.state == null)
                return;

            GameState state = GameManager.Instance.state;

            foreach (GameEvent evt in events)
            {
                if (evt is UnitMovedEvent moveEvent)
                {
                    // Only care about human player's units
                    Unit unit = state.unitManager.GetUnit(moveEvent.unitId);
                    if (unit == null || unit.ownerId != 0)
                        continue;

                    // If this was a partial move with remaining path, store it
                    if (moveEvent.isPartialMove && moveEvent.remainingPath != null && moveEvent.remainingPath.Count > 1)
                    {
                        // Note: We don't add it to pendingMovePaths yet - that happens in RestoreRemainingPaths
                        // Just log for debugging
                        Debug.Log($"[GameHUD] Unit {moveEvent.unitId} has {moveEvent.remainingPath.Count - 1} tiles remaining for next turn");
                    }
                }
            }
        }

        /// <summary>
        /// Restore remaining paths from units that had incomplete movement last turn
        /// This makes the previously "dotted" segments become "solid" for the new turn
        /// CRITICAL: Combat paths should remain as default orders unless player overrides them
        /// </summary>
        private void RestoreRemainingPaths()
        {
            if (GameManager.Instance?.state == null)
                return;

            GameState state = GameManager.Instance.state;

            // Get all human player units
            List<Unit> humanUnits = state.unitManager.GetAllUnits().FindAll(u => u.ownerId == 0);

            int restoredCount = 0;
            int combatPathsRestored = 0;

            foreach (Unit unit in humanUnits)
            {
                // Check if this unit has a queued path from the previous turn
                if (unit.queuedPath != null && unit.queuedPath.Count > 1)
                {
                    // Check if this is a combat path (unit is in combat)
                    bool isCombatPath = unit.isInCombat && unit.combatOpponentId != null;

                    // Automatically queue a move order with the remaining path
                    MoveOrder order = new MoveOrder(unit.id, 0, unit.queuedPath);
                    pendingPlayerOrders.Add(order);

                    // Track that this unit has an order
                    unitsWithOrders.Add(unit.id);

                    // Store the path for visualization
                    pendingMovePaths[unit.id] = unit.queuedPath;

                    restoredCount++;

                    if (isCombatPath)
                    {
                        combatPathsRestored++;
                        Debug.Log($"[GameHUD] Restored COMBAT path for unit {unit.id} -> opponent at {unit.queuedPath[unit.queuedPath.Count - 1]} (path length: {unit.queuedPath.Count - 1})");
                    }
                    else
                    {
                        Debug.Log($"[GameHUD] Restored MOVEMENT path for unit {unit.id}: {unit.queuedPath.Count - 1} tiles");
                    }

                    // IMPORTANT: DO NOT clear unit.queuedPath here!
                    // Keep it so that if the player doesn't submit orders, the combat continues
                    // Only clear it when the player explicitly changes orders or submits
                    // unit.queuedPath = null; // REMOVED - this was causing combat paths to disappear!
                }
            }

            if (restoredCount > 0)
            {
                Debug.Log($"[GameHUD] ===== RESTORED {restoredCount} PATHS =====");
                Debug.Log($"[GameHUD]   Combat paths: {combatPathsRestored}");
                Debug.Log($"[GameHUD]   Movement paths: {restoredCount - combatPathsRestored}");

                // Update visualizations to show restored paths
                if (pathVisualizer != null)
                {
                    UpdatePathVisualizations();
                }
            }
        }

        /// <summary>
        /// Update all path visualizations based on current selection and pending orders
        ///
        /// PATH VISUALIZATION DESIGN:
        /// - Shows ALL pending actions for user's units before they submit orders
        /// - Movement: Yellow line showing path, split into "this turn" (solid) and "future turns" (dotted)
        /// - Deployment: Highlights the tile where shipyard will be built
        /// - Combat: Shows path to enemy, indicating attack intent
        /// - Attack Shipyard: Shows path to target shipyard
        /// - Primary (selected unit): Brighter/solid yellow
        /// - Secondary (other units): Semi-transparent yellow
        /// - User can see all planned actions before clicking "Submit Orders"
        /// </summary>
        private void UpdatePathVisualizations()
        {
            if (pathVisualizer == null || GameManager.Instance?.state == null)
                return;

            GameState state = GameManager.Instance.state;

            // Don't clear all paths - PathVisualizer.AddPath() handles per-unit clearing
            // Clearing all would remove paths we're about to re-add

            // Add all pending move paths (includes movement, combat paths, attack shipyard paths, deployment indicators)
            int pathsAdded = 0;
            int combatPathsAdded = 0;
            int deploymentPathsAdded = 0;
            int movementPathsAdded = 0;

            foreach (var kvp in pendingMovePaths)
            {
                string unitId = kvp.Key;
                List<HexCoord> path = kvp.Value;

                // Get the unit to determine its movement capacity and combat status
                Unit unit = state.unitManager.GetUnit(unitId);
                if (unit == null)
                {
                    Debug.LogWarning($"[GameHUD] Unit {unitId} not found for path visualization");
                    continue;
                }

                int movementCapacity = unit.GetMovementCapacity();

                // Determine path type for logging
                bool isCombatPath = unit.isInCombat && unit.combatOpponentId != null;
                bool isDeploymentPath = (path.Count == 2 && path[0].Equals(path[1])); // Deployment indicator
                string pathType = isCombatPath ? "COMBAT" : (isDeploymentPath ? "DEPLOYMENT" : "MOVEMENT");

                // Determine if this is the primary path (currently selected unit)
                bool isPrimary = (selectedUnit != null && selectedUnit.id == unitId);

                try
                {
                    // Add or update the path with movement capacity
                    pathVisualizer.AddPath(unitId, path, isPrimary, movementCapacity);
                    pathsAdded++;

                    if (isCombatPath) combatPathsAdded++;
                    else if (isDeploymentPath) deploymentPathsAdded++;
                    else movementPathsAdded++;

                    Debug.Log($"[GameHUD] Added {pathType} path for {unitId}: {path.Count} waypoints, {(isPrimary ? "PRIMARY" : "secondary")}, capacity={movementCapacity}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameHUD] EXCEPTION in pathVisualizer.AddPath() for unit {unitId}:");
                    Debug.LogError($"  Path count: {path?.Count ?? 0}");
                    Debug.LogError($"  Movement capacity: {movementCapacity}");
                    Debug.LogError($"  IsPrimary: {isPrimary}");
                    Debug.LogError($"  Exception type: {ex.GetType().Name}");
                    Debug.LogError($"  Exception message: {ex.Message}");
                    Debug.LogError($"  Stack trace: {ex.StackTrace}");
                }
            }

            Debug.Log($"[GameHUD] ===== PATH VISUALIZATION UPDATE =====");
            Debug.Log($"[GameHUD]   Total paths: {pathsAdded}/{pendingMovePaths.Count}");
            Debug.Log($"[GameHUD]   Combat: {combatPathsAdded}, Movement: {movementPathsAdded}, Deployment: {deploymentPathsAdded}");
            Debug.Log($"[GameHUD]   Primary unit: {selectedUnit?.id ?? "none"}");
        }

        /// <summary>
        /// Check if all human units have orders queued
        /// </summary>
        private bool AllHumanUnitsHaveOrders(GameState state)
        {
            if (state == null || state.unitManager == null)
                return false;

            var humanUnits = state.unitManager.GetAllUnits().FindAll(u => u.ownerId == 0);
            if (humanUnits.Count == 0)
                return false;

            foreach (var unit in humanUnits)
            {
                if (!unitsWithOrders.Contains(unit.id))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Update the selection indicator state based on whether the unit has an order
        /// </summary>
        private void UpdateSelectionIndicatorState(string unitId)
        {
            var unitRenderer = FindFirstObjectByType<UnitRenderer>();
            if (unitRenderer == null)
                return;

            // Find the selection indicator for this unit using reflection
            var selectionIndicators = unitRenderer.GetType()
                .GetField("selectionIndicators", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(unitRenderer) as Dictionary<string, GameObject>;

            if (selectionIndicators != null && selectionIndicators.TryGetValue(unitId, out GameObject indicator))
            {
                SelectionPulse pulse = indicator.GetComponent<SelectionPulse>();
                if (pulse != null)
                {
                    // Set state based on whether unit has an order
                    SelectionState newState = unitsWithOrders.Contains(unitId)
                        ? SelectionState.OrderSet
                        : SelectionState.WaitingForOrder;
                    pulse.SetState(newState);
                    Debug.Log($"[GameHUD] Updated selection indicator for {unitId} to state: {newState}");
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up PathVisualizer (no longer a child, so won't be destroyed automatically)
            if (pathVisualizer != null && pathVisualizer.gameObject != null)
            {
                Debug.Log("[GameHUD] Destroying PathVisualizer");
                Destroy(pathVisualizer.gameObject);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                GameManager.Instance.OnTurnResolved -= HandleTurnResolved;
            }
        }
    }
}
