using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
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

        public void Initialize()
        {
            CreateLayout();
            SubscribeToEvents();
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

            selectedUnitText.text = $"UNIT\nID: {unit.id}\nOwner: Player {unit.ownerId}\nPosition: {unit.position}\nType: {unit.type}\n{tileInfo}";
        }

        private void SelectStructure(Structure structure)
        {
            selectedUnit = null;
            selectedStructure = structure;
            plannedPath = null;

            string ownerText = structure.ownerId == -1 ? "Neutral" : $"Player {structure.ownerId}";
            selectedUnitText.text = $"STRUCTURE\nID: {structure.id}\nOwner: {ownerText}\nPosition: {structure.position}\nType: {structure.type}";
        }

        private void ClearSelection()
        {
            selectedUnit = null;
            selectedStructure = null;
            plannedPath = null;
            selectedUnitText.text = "No selection";
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

                // Add move order to pending orders
                MoveOrder order = new MoveOrder(selectedUnit.id, 0, path);
                pendingPlayerOrders.Add(order);

                Debug.Log($"[GameHUD] Planned path for {selectedUnit.id}: {path.Count} steps");
                selectedUnitText.text += "\n\nMove order queued!";
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
            ClearSelection();
            selectedUnitText.text = "Orders submitted!";
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
                ClearSelection();
            }
        }

        private void HandleTurnResolved(List<GameEvent> events)
        {
            eventLog?.AddEvents(events);
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
