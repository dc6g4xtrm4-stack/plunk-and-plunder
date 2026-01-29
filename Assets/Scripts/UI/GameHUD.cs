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
        private Text turnText;
        private Text phaseText;
        private Text selectedUnitText;
        private Text playerInfoText;
        private Text helpText;
        private Button submitButton;
        private Button autoResolveButton;
        private Button deployShipyardButton;
        private Button buildShipButton;
        private EventLogUI eventLog;
        private TileTooltipUI tooltip;

        // Interaction state
        private Unit selectedUnit;
        private Structure selectedStructure;
        private List<HexCoord> plannedPath;
        private List<IOrder> pendingPlayerOrders = new List<IOrder>();
        private HashSet<string> unitsWithOrders = new HashSet<string>(); // Track which units have pending orders
        private Dictionary<string, List<HexCoord>> pendingMovePaths = new Dictionary<string, List<HexCoord>>(); // Track paths per unit

        // Visualization components
        private PathVisualizer pathVisualizer;
        private ButtonPulse buttonPulse;

        public void Initialize()
        {
            CreateLayout();
            SubscribeToEvents();
            InitializeVisualizers();
        }

        private void InitializeVisualizers()
        {
            // Create path visualizer
            GameObject pathVisualizerObj = new GameObject("PathVisualizer");
            pathVisualizerObj.transform.SetParent(transform, false);
            pathVisualizer = pathVisualizerObj.AddComponent<PathVisualizer>();
        }

        private void CreateLayout()
        {
            // Top bar
            GameObject topBar = CreatePanel(new Vector2(0, 500), new Vector2(1800, 80), new Color(0.1f, 0.1f, 0.1f, 0.8f));
            topBar.transform.SetParent(transform, false);

            turnText = CreateText("Turn: 0", 28, topBar.transform);
            turnText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-700, 0);
            turnText.alignment = TextAnchor.MiddleLeft;

            phaseText = CreateText("Phase: Planning", 28, topBar.transform);
            phaseText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

            // Player info text (gold)
            playerInfoText = CreateText("Gold: 0", 24, topBar.transform);
            playerInfoText.GetComponent<RectTransform>().anchoredPosition = new Vector2(600, 0);
            playerInfoText.alignment = TextAnchor.MiddleRight;

            // Submit button
            submitButton = CreateButton("Submit Orders", new Vector2(700, 500), OnSubmitClicked);
            submitButton.transform.SetParent(transform, false);

            // Add button pulse component
            buttonPulse = submitButton.gameObject.AddComponent<ButtonPulse>();
            buttonPulse.normalColor = new Color(0.2f, 0.4f, 0.2f);
            buttonPulse.minPulseColor = new Color(0f, 0.6f, 0f);
            buttonPulse.maxPulseColor = new Color(0f, 1f, 0f);

            // Auto-resolve button (debug)
            autoResolveButton = CreateButton("Auto-Resolve (Debug)", new Vector2(500, 500), OnAutoResolveClicked);
            autoResolveButton.transform.SetParent(transform, false);

            // Selected unit panel
            GameObject unitPanel = CreatePanel(new Vector2(-800, -300), new Vector2(300, 250), new Color(0.1f, 0.1f, 0.1f, 0.8f));
            unitPanel.transform.SetParent(transform, false);

            selectedUnitText = CreateText("No unit selected", 20, unitPanel.transform);
            selectedUnitText.alignment = TextAnchor.UpperLeft;
            selectedUnitText.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -10);
            selectedUnitText.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 180);

            // Deploy shipyard button (initially hidden)
            deployShipyardButton = CreateButton("Deploy Shipyard", new Vector2(-800, -480), OnDeployShipyardClicked);
            deployShipyardButton.transform.SetParent(transform, false);
            deployShipyardButton.gameObject.SetActive(false);

            // Build ship button (initially hidden)
            buildShipButton = CreateButton("Build Ship (50g)", new Vector2(-800, -480), OnBuildShipClicked);
            buildShipButton.transform.SetParent(transform, false);
            buildShipButton.gameObject.SetActive(false);

            // Event log
            GameObject eventLogObj = new GameObject("EventLog");
            eventLogObj.transform.SetParent(transform, false);
            eventLog = eventLogObj.AddComponent<EventLogUI>();
            eventLog.Initialize();

            // Tooltip
            GameObject tooltipObj = new GameObject("Tooltip");
            tooltipObj.transform.SetParent(transform, false);
            tooltip = tooltipObj.AddComponent<TileTooltipUI>();
            tooltip.Initialize();

            // Help text panel (bottom right)
            GameObject helpPanel = CreatePanel(new Vector2(650, -450), new Vector2(400, 150), new Color(0.1f, 0.1f, 0.1f, 0.7f));
            helpPanel.transform.SetParent(transform, false);

            helpText = CreateText("HOW TO PLAY:\nLeft-click: Select ship/building\nRight-click: Move ship\nQueue orders, then Submit\nOr: Auto-Resolve for AI turns", 16, helpPanel.transform);
            helpText.alignment = TextAnchor.UpperLeft;
            helpText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-190, -10);
            helpText.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 130);
        }

        private GameObject CreatePanel(Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject("Panel");
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image bg = panel.AddComponent<Image>();
            bg.color = color;

            return panel;
        }

        private Text CreateText(string text, int fontSize, Transform parent = null)
        {
            GameObject textObj = new GameObject("Text");
            if (parent != null)
                textObj.transform.SetParent(parent, false);

            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 60);

            return textComponent;
        }

        private Button CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject($"Button_{label}");
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            rect.anchoredPosition = position;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.4f, 0.2f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
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
            UpdateHUD();
            HandleInput();
        }

        private void UpdateHUD()
        {
            if (GameManager.Instance?.state != null)
            {
                GameState state = GameManager.Instance.state;
                turnText.text = $"Turn: {state.turnNumber}";
                phaseText.text = $"Phase: {state.phase}";

                // Update player info
                var humanPlayer = state.playerManager.GetPlayer(0); // Assume player 0 is human
                if (humanPlayer != null)
                {
                    playerInfoText.text = $"Gold: {humanPlayer.gold} | Orders: {pendingPlayerOrders.Count}";
                }

                // Update submit button
                submitButton.interactable = (state.phase == GamePhase.Planning && pendingPlayerOrders.Count > 0);

                // Enable button pulsing when player has queued orders
                if (buttonPulse != null)
                {
                    bool shouldPulse = (state.phase == GamePhase.Planning && pendingPlayerOrders.Count > 0 && AllHumanUnitsHaveOrders(state));
                    buttonPulse.SetPulsing(shouldPulse);
                }

                // Update action buttons based on selection
                UpdateActionButtons();
            }
        }

        private void UpdateActionButtons()
        {
            if (GameManager.Instance?.state == null)
                return;

            GameState state = GameManager.Instance.state;

            // Show deploy shipyard button if ship is on harbor
            if (selectedUnit != null && selectedUnit.ownerId == 0) // Human player
            {
                Tile tile = state.grid.GetTile(selectedUnit.position);
                bool isOnHarbor = tile != null && tile.type == TileType.HARBOR;
                deployShipyardButton.gameObject.SetActive(isOnHarbor);
            }
            else
            {
                deployShipyardButton.gameObject.SetActive(false);
            }

            // Show build ship button if shipyard is selected
            if (selectedStructure != null && selectedStructure.type == StructureType.SHIPYARD && selectedStructure.ownerId == 0)
            {
                buildShipButton.gameObject.SetActive(true);
            }
            else
            {
                buildShipButton.gameObject.SetActive(false);
            }
        }

        private void HandleInput()
        {
            if (GameManager.Instance?.state?.phase != GamePhase.Planning)
                return;

            // Left click: select unit or structure
            if (Input.GetMouseButtonDown(0))
            {
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

            GameState state = GameManager.Instance.state;
            Tile tile = state.grid.GetTile(unit.position);
            string tileInfo = tile != null ? $"Tile: {tile.type}" : "Tile: Unknown";

            // Get movement info
            int movementCapacity = unit.GetMovementCapacity();
            int movementRemaining = unit.movementRemaining;
            string movementInfo = $"Movement: {movementRemaining}/{movementCapacity}";

            // Get path info if unit has a pending order
            string pathInfo = "";
            if (pendingMovePaths.ContainsKey(unit.id))
            {
                List<HexCoord> path = pendingMovePaths[unit.id];
                int pathLength = path.Count - 1; // Subtract 1 since path includes starting position
                int thisTurnMoves = Mathf.Min(movementCapacity, pathLength);
                int queuedMoves = Mathf.Max(0, pathLength - movementCapacity);

                if (queuedMoves > 0)
                {
                    pathInfo = $"\nPath Length: {pathLength}\n({thisTurnMoves} this turn, {queuedMoves} queued)";
                }
                else
                {
                    pathInfo = $"\nPath Length: {pathLength}";
                }
            }

            string orderStatus = unitsWithOrders.Contains(unit.id) ? "\n[HAS ORDER]" : "";
            string healthInfo = $"HP: {unit.health}/{unit.maxHealth}";
            selectedUnitText.text = $"UNIT\nID: {unit.id}\nOwner: Player {unit.ownerId}\nPosition: {unit.position}\nType: {unit.type}\n{healthInfo}\n{tileInfo}\n{movementInfo}{pathInfo}{orderStatus}";

            // Show selection indicator
            var unitRenderer = FindObjectOfType<UnitRenderer>();
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
        }

        private void SelectStructure(Structure structure)
        {
            selectedUnit = null;
            selectedStructure = structure;
            plannedPath = null;

            string ownerText = structure.ownerId == -1 ? "Neutral" : $"Player {structure.ownerId}";
            selectedUnitText.text = $"STRUCTURE\nID: {structure.id}\nOwner: {ownerText}\nPosition: {structure.position}\nType: {structure.type}";

            // Hide selection indicator when selecting a structure
            var unitRenderer = FindObjectOfType<UnitRenderer>();
            if (unitRenderer != null)
            {
                unitRenderer.HideSelectionIndicator();
            }

            // Update path visualization - show all paths with no primary
            if (pathVisualizer != null)
            {
                UpdatePathVisualizations();
            }
        }

        private void ClearSelection()
        {
            selectedUnit = null;
            selectedStructure = null;
            plannedPath = null;
            selectedUnitText.text = "No selection";

            // Hide selection indicator
            var unitRenderer = FindObjectOfType<UnitRenderer>();
            if (unitRenderer != null)
            {
                unitRenderer.HideSelectionIndicator();
            }

            // Update path visualization - show all paths with no primary
            if (pathVisualizer != null)
            {
                UpdatePathVisualizations();
            }
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

                // Remove existing move order for this unit if any
                pendingPlayerOrders.RemoveAll(o => o is MoveOrder moveOrder && moveOrder.unitId == selectedUnit.id);

                // Add move order to pending orders
                MoveOrder order = new MoveOrder(selectedUnit.id, 0, path);
                pendingPlayerOrders.Add(order);

                // Track that this unit has an order
                unitsWithOrders.Add(selectedUnit.id);

                // Store the path for this unit
                pendingMovePaths[selectedUnit.id] = path;

                Debug.Log($"[GameHUD] Planned path for {selectedUnit.id}: {path.Count} steps");
                selectedUnitText.text += "\n\nMove order queued!";

                // Update selection indicator to OrderSet state
                UpdateSelectionIndicatorState(selectedUnit.id);

                // Update path visualization - add/update this unit's path
                if (pathVisualizer != null)
                {
                    UpdatePathVisualizations();
                }
            }
        }

        private void OnSubmitClicked()
        {
            if (GameManager.Instance == null || pendingPlayerOrders.Count == 0)
                return;

            // Submit all pending orders
            GameManager.Instance.SubmitOrders(0, pendingPlayerOrders);

            // Clear state
            pendingPlayerOrders.Clear();
            unitsWithOrders.Clear();
            pendingMovePaths.Clear();
            ClearSelection();
            selectedUnitText.text = "Orders submitted!";

            // Clear all path visualizations
            if (pathVisualizer != null)
            {
                pathVisualizer.ClearAllPaths();
            }
        }

        private void OnAutoResolveClicked()
        {
            GameManager.Instance?.AutoResolveTurn();
        }

        private void OnDeployShipyardClicked()
        {
            if (GameManager.Instance == null || selectedUnit == null)
                return;

            // Don't allow if not owned by human player
            if (selectedUnit.ownerId != 0)
                return;

            GameState state = GameManager.Instance.state;
            Tile tile = state.grid.GetTile(selectedUnit.position);

            // Verify ship is on harbor
            if (tile == null || tile.type != TileType.HARBOR)
            {
                Debug.LogWarning("[GameHUD] Ship must be on harbor to deploy shipyard");
                return;
            }

            // Create deploy shipyard order
            DeployShipyardOrder order = new DeployShipyardOrder(selectedUnit.id, 0, selectedUnit.position);
            pendingPlayerOrders.Add(order);

            // Track that this unit has an order
            unitsWithOrders.Add(selectedUnit.id);

            Debug.Log($"[GameHUD] Deploy shipyard order queued for {selectedUnit.id}");
            selectedUnitText.text += "\n\nDeploy Shipyard order queued!";

            // Clear selection since unit will be consumed
            Unit deployedUnit = selectedUnit;
            ClearSelection();
        }

        private void OnBuildShipClicked()
        {
            if (GameManager.Instance == null || selectedStructure == null)
                return;

            // Verify structure is a shipyard owned by human player
            if (selectedStructure.type != StructureType.SHIPYARD || selectedStructure.ownerId != 0)
                return;

            GameState state = GameManager.Instance.state;
            var player = state.playerManager.GetPlayer(0);

            // Check if player has enough gold
            if (player == null || player.gold < BuildingConfig.BUILD_SHIP_COST)
            {
                Debug.LogWarning($"[GameHUD] Not enough gold to build ship. Need {BuildingConfig.BUILD_SHIP_COST}, have {player?.gold ?? 0}");
                selectedUnitText.text += $"\n\nNeed {BuildingConfig.BUILD_SHIP_COST} gold!";
                return;
            }

            // Create build ship order
            BuildShipOrder order = new BuildShipOrder(0, selectedStructure.id, selectedStructure.position);
            pendingPlayerOrders.Add(order);

            Debug.Log($"[GameHUD] Build ship order queued at {selectedStructure.id}");
            selectedUnitText.text += "\n\nBuild Ship order queued!";
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
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
        /// </summary>
        private void RestoreRemainingPaths()
        {
            if (GameManager.Instance?.state == null)
                return;

            GameState state = GameManager.Instance.state;

            // Get all human player units
            List<Unit> humanUnits = state.unitManager.GetAllUnits().FindAll(u => u.ownerId == 0);

            int restoredCount = 0;

            foreach (Unit unit in humanUnits)
            {
                // Check if this unit has a queued path from the previous turn
                if (unit.queuedPath != null && unit.queuedPath.Count > 1)
                {
                    // Automatically queue a move order with the remaining path
                    MoveOrder order = new MoveOrder(unit.id, 0, unit.queuedPath);
                    pendingPlayerOrders.Add(order);

                    // Track that this unit has an order
                    unitsWithOrders.Add(unit.id);

                    // Store the path for visualization
                    pendingMovePaths[unit.id] = unit.queuedPath;

                    restoredCount++;

                    Debug.Log($"[GameHUD] Restored queued path for unit {unit.id}: {unit.queuedPath.Count - 1} tiles");

                    // Clear the queued path from the unit (it's now in pending orders)
                    unit.queuedPath = null;
                }
            }

            if (restoredCount > 0)
            {
                Debug.Log($"[GameHUD] Restored {restoredCount} queued paths from previous turn");

                // Update visualizations to show restored paths
                if (pathVisualizer != null)
                {
                    UpdatePathVisualizations();
                }
            }
        }

        /// <summary>
        /// Update all path visualizations based on current selection and pending orders
        /// </summary>
        private void UpdatePathVisualizations()
        {
            if (pathVisualizer == null || GameManager.Instance?.state == null)
                return;

            GameState state = GameManager.Instance.state;

            // Don't clear all paths - PathVisualizer.AddPath() handles per-unit clearing
            // Clearing all would remove paths we're about to re-add

            // Add all pending move paths
            int pathsAdded = 0;
            foreach (var kvp in pendingMovePaths)
            {
                string unitId = kvp.Key;
                List<HexCoord> path = kvp.Value;

                // Get the unit to determine its movement capacity
                Unit unit = state.unitManager.GetUnit(unitId);
                if (unit == null)
                {
                    Debug.LogWarning($"[GameHUD] Unit {unitId} not found for path visualization");
                    continue;
                }

                int movementCapacity = unit.GetMovementCapacity();

                // Determine if this is the primary path (currently selected unit)
                bool isPrimary = (selectedUnit != null && selectedUnit.id == unitId);

                // Add or update the path with movement capacity
                pathVisualizer.AddPath(unitId, path, isPrimary, movementCapacity);
                pathsAdded++;

                Debug.Log($"[GameHUD] Added path for {unitId}: {path.Count} waypoints, {(isPrimary ? "PRIMARY" : "secondary")}, capacity={movementCapacity}");
            }

            Debug.Log($"[GameHUD] Updated path visualizations: {pathsAdded}/{pendingMovePaths.Count} paths added, primary={selectedUnit?.id ?? "none"}");
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
            var unitRenderer = FindObjectOfType<UnitRenderer>();
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                GameManager.Instance.OnTurnResolved -= HandleTurnResolved;
            }
        }
    }
}
