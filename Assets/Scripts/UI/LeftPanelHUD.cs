using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Consolidated left panel HUD at BOTTOM-LEFT containing:
    /// - Player Stats (top)
    /// - Unit Details (middle)
    /// - Build Queue (middle, when shipyard selected)
    /// - Action Buttons (bottom)
    /// </summary>
    public class LeftPanelHUD : MonoBehaviour
    {
        // Section containers
        private GameObject playerStatsSection;
        private GameObject unitDetailsSection;
        private GameObject buildQueueSection;
        private GameObject actionButtonsSection;

        // UI Elements
        private Text playerStatsText;
        private Text unitDetailsText;
        private Text buildQueueText;
        private Dictionary<string, Button> actionButtons = new Dictionary<string, Button>();

        // Selection state
        private Unit selectedUnit;
        private Structure selectedStructure;

        public void Initialize(GameState state)
        {
            // Don't store gameState - it gets replaced when starting a new game
            // Instead, get it fresh from GameManager each time
            BuildLeftPanel();
        }

        private void BuildLeftPanel()
        {
            // Setup RectTransform for bottom-left positioning (aligned with plan)
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // ANCHOR TO BOTTOM-LEFT (per plan spec)
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(HUDStyles.EdgeMargin, HUDStyles.EdgeMargin);

            // Calculate height: reference height - top bar - 2*edge margin
            float panelHeight = HUDStyles.ReferenceHeight - HUDStyles.TopBarHeight - (HUDStyles.EdgeMargin * 2);
            rectTransform.sizeDelta = new Vector2(HUDStyles.LeftPanelWidth, panelHeight);

            // Add background
            Image bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = HUDStyles.BackgroundColor;

            // Add border
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = HUDStyles.BorderColor;
            outline.effectDistance = new Vector2(2, -2);

            // Create vertical layout group for sections
            VerticalLayoutGroup layoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = HUDStyles.SectionSpacing;
            layoutGroup.padding = new RectOffset(HUDStyles.PanelPadding, HUDStyles.PanelPadding,
                                                  HUDStyles.PanelPadding, HUDStyles.PanelPadding);

            // Build sections in order: Player Stats → Selection Details → Build Queue → Actions (per plan)
            BuildPlayerStatsSection();
            BuildUnitDetailsSection();
            BuildBuildQueueSection();
            BuildActionButtonsSection();

            // Initially hide build queue
            buildQueueSection.SetActive(false);

            Debug.Log($"[LeftPanelHUD] Initialized at bottom-left: anchor={rectTransform.anchorMin}, position={rectTransform.anchoredPosition}, size={rectTransform.sizeDelta}");
        }

        #region Player Stats Section

        private void BuildPlayerStatsSection()
        {
            playerStatsSection = new GameObject("PlayerStatsSection", typeof(RectTransform));
            playerStatsSection.transform.SetParent(transform, false);

            RectTransform rt = playerStatsSection.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 200);

            VerticalLayoutGroup layout = playerStatsSection.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Header
            GameObject header = HUDLayoutManager.CreateHeaderText(playerStatsSection.transform, "PLAYER STATS");
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0, 30);

            // Separator line
            CreateSeparatorLine(playerStatsSection.transform);

            // Content text
            GameObject contentObj = HUDLayoutManager.CreateContentText(playerStatsSection.transform, "");
            playerStatsText = contentObj.GetComponent<Text>();
            playerStatsText.alignment = TextAnchor.UpperLeft;
            RectTransform contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(0, 150);
        }

        public void UpdatePlayerStats()
        {
            if (playerStatsText == null) return;

            GameState gameState = GameManager.Instance?.state;
            if (gameState == null) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<Player> players = gameState.playerManager.GetActivePlayers();

            foreach (Player player in players)
            {
                sb.AppendLine($"<b>{player.name}</b>");
                sb.AppendLine($"  💰 {player.gold}");

                int shipCount = gameState.unitManager.GetUnitsForPlayer(player.id).Count;
                int shipyardCount = gameState.structureManager.GetStructuresForPlayer(player.id)
                    .FindAll(s => s.type == StructureType.SHIPYARD).Count;

                sb.AppendLine($"  ⛵ {shipCount} | 🏭 {shipyardCount}");
                sb.AppendLine();
            }

            playerStatsText.text = sb.ToString().TrimEnd();

            // Adjust section height
            int playerCount = players.Count;
            float height = 50 + (playerCount * 70);
            RectTransform rt = playerStatsSection.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        }

        #endregion

        #region Unit Details Section

        private void BuildUnitDetailsSection()
        {
            unitDetailsSection = new GameObject("UnitDetailsSection", typeof(RectTransform));
            unitDetailsSection.transform.SetParent(transform, false);

            RectTransform rt = unitDetailsSection.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, HUDStyles.UnitDetailsSectionHeight);

            VerticalLayoutGroup layout = unitDetailsSection.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Header
            GameObject header = HUDLayoutManager.CreateHeaderText(unitDetailsSection.transform, "SELECTED UNIT");
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0, 30);

            // Separator
            CreateSeparatorLine(unitDetailsSection.transform);

            // Content text
            GameObject contentObj = HUDLayoutManager.CreateContentText(unitDetailsSection.transform, "No selection");
            unitDetailsText = contentObj.GetComponent<Text>();
            unitDetailsText.alignment = TextAnchor.UpperLeft;
            RectTransform contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(0, HUDStyles.UnitDetailsSectionHeight - 40);
        }

        public void UpdateSelection(Unit unit, Structure structure)
        {
            selectedUnit = unit;
            selectedStructure = structure;

            if (unitDetailsText == null) return;

            GameState gameState = GameManager.Instance?.state;
            if (gameState == null) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (unit != null)
            {
                Player owner = gameState.playerManager.GetPlayer(unit.ownerId);
                sb.AppendLine($"<b>SHIP: {unit.name}</b>");
                sb.AppendLine($"Owner: {owner?.name ?? "Unknown"}");
                sb.AppendLine($"Position: {unit.position}");
                sb.AppendLine();
                sb.AppendLine($"<b>Stats:</b>");
                sb.AppendLine($"HP: {unit.health}/{unit.maxHealth}");
                sb.AppendLine($"Movement: {unit.movementRemaining}/{unit.GetMovementCapacity()}");
                sb.AppendLine($"Sails: {unit.sails} | Cannons: {unit.cannons}");

                if (unit.isInCombat && unit.combatOpponentId != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"<b>IN COMBAT</b>");
                    sb.AppendLine($"Opponent: {unit.combatOpponentId}");
                }

                buildQueueSection.SetActive(false);
                UpdateActionButtons();
            }
            else if (structure != null)
            {
                Player owner = gameState.playerManager.GetPlayer(structure.ownerId);
                sb.AppendLine($"<b>STRUCTURE</b>");
                sb.AppendLine($"Type: {structure.type}");
                sb.AppendLine($"Owner: {owner?.name ?? "Neutral"}");
                sb.AppendLine($"Position: {structure.position}");

                if (structure.type == StructureType.SHIPYARD)
                {
                    sb.AppendLine();
                    sb.AppendLine($"<b>Shipyard Stats:</b>");
                    int queueCount = PlunkAndPlunder.Construction.ConstructionManager.Instance?.GetShipyardQueue(structure.id)?.Count ?? 0;
                    sb.AppendLine($"Build Queue: {queueCount}/{BuildingConfig.MAX_QUEUE_SIZE}");

                    buildQueueSection.SetActive(true);
                    UpdateBuildQueue(structure);
                }
                else
                {
                    buildQueueSection.SetActive(false);
                }

                UpdateActionButtons();
            }
            else
            {
                sb.AppendLine("No selection");
                buildQueueSection.SetActive(false);
                UpdateActionButtons();
            }

            unitDetailsText.text = sb.ToString();
        }

        #endregion

        #region Build Queue Section

        private void BuildBuildQueueSection()
        {
            buildQueueSection = new GameObject("BuildQueueSection", typeof(RectTransform));
            buildQueueSection.transform.SetParent(transform, false);

            RectTransform rt = buildQueueSection.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, HUDStyles.BuildQueueSectionHeight);

            VerticalLayoutGroup layout = buildQueueSection.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Header
            GameObject header = HUDLayoutManager.CreateHeaderText(buildQueueSection.transform, "BUILD QUEUE");
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0, 30);

            // Separator
            CreateSeparatorLine(buildQueueSection.transform);

            // Content text
            GameObject contentObj = HUDLayoutManager.CreateContentText(buildQueueSection.transform, "No ships in queue");
            buildQueueText = contentObj.GetComponent<Text>();
            buildQueueText.alignment = TextAnchor.UpperLeft;
            RectTransform contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(0, HUDStyles.BuildQueueSectionHeight - 40);
        }

        private void UpdateBuildQueue(Structure shipyard)
        {
            if (buildQueueText == null || shipyard == null) return;

            var queue = PlunkAndPlunder.Construction.ConstructionManager.Instance?.GetShipyardQueue(shipyard.id);
            if (queue == null || queue.Count == 0)
            {
                buildQueueText.text = "No ships in queue";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < queue.Count; i++)
            {
                var job = queue[i];
                sb.AppendLine($"{i + 1}. {job.itemType}");
                sb.AppendLine($"   Turns remaining: {job.turnsRemaining}");
                if (i < queue.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            buildQueueText.text = sb.ToString();
        }

        #endregion

        #region Action Buttons Section

        private void BuildActionButtonsSection()
        {
            // Create as CHILD of main panel (at top due to being called first)
            actionButtonsSection = new GameObject("ActionButtonsSection", typeof(RectTransform));
            actionButtonsSection.transform.SetParent(transform, false);

            RectTransform rt = actionButtonsSection.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 420); // Fixed height - fits all 6 buttons properly (6*50 + 5*10 + header + padding)

            // Layout
            VerticalLayoutGroup layout = actionButtonsSection.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = HUDStyles.ButtonSpacing;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Header
            GameObject header = HUDLayoutManager.CreateHeaderText(actionButtonsSection.transform, "ACTIONS");
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0, 30);

            // Separator
            CreateSeparatorLine(actionButtonsSection.transform);

            // Create action buttons
            CreateActionButton("DeployShipyard", "Deploy Shipyard (100g)", OnDeployShipyard);
            CreateActionButton("BuildShip", "Build Ship (50g)", OnBuildShip);
            CreateActionButton("UpgradeSails", "Upgrade Sails (60g)", OnUpgradeSails);
            CreateActionButton("UpgradeCannons", "Upgrade Cannons (80g)", OnUpgradeCannons);
            CreateActionButton("UpgradeMaxLife", "Upgrade Max Life (100g)", OnUpgradeMaxLife);
            CreateActionButton("AttackShipyard", "Attack Shipyard", OnAttackShipyard);

            UpdateActionButtons();
        }

        private void CreateActionButton(string id, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(id + "Button", typeof(RectTransform));
            buttonObj.transform.SetParent(actionButtonsSection.transform, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, HUDStyles.ButtonHeight);

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = HUDStyles.ButtonNormalColor;
            bg.raycastTarget = true; // CRITICAL: Enable raycasting for clicks

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg; // CRITICAL: Set target graphic for button
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = HUDStyles.ButtonNormalColor;
            colors.highlightedColor = HUDStyles.ButtonHoverColor;
            colors.disabledColor = HUDStyles.ButtonDisabledColor;
            button.colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(buttonObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = HUDStyles.ContentFontSize;
            text.color = HUDStyles.TextColor;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            actionButtons[id] = button;
        }

        private void UpdateActionButtons()
        {
            GameState gameState = GameManager.Instance?.state;

            if (gameState == null || gameState.phase != GamePhase.Planning)
            {
                // Hide all buttons if not in planning phase
                foreach (var btn in actionButtons.Values)
                {
                    btn.gameObject.SetActive(false);
                }
                return;
            }

            Player humanPlayer = gameState.playerManager.GetPlayer(0);
            if (humanPlayer == null)
            {
                return;
            }

            // Deploy Shipyard - only show if ship is on empty harbor tile
            bool showDeployShipyard = false;
            bool canDeployShipyard = false;
            if (selectedUnit != null && selectedUnit.ownerId == 0)
            {
                Tile tile = gameState.grid.GetTile(selectedUnit.position);
                if (tile != null && tile.type == TileType.HARBOR &&
                    gameState.structureManager.GetStructureAtPosition(selectedUnit.position) == null)
                {
                    showDeployShipyard = true;
                    canDeployShipyard = humanPlayer.gold >= BuildingConfig.DEPLOY_SHIPYARD_COST;
                }
            }
            actionButtons["DeployShipyard"].gameObject.SetActive(showDeployShipyard);
            if (showDeployShipyard)
            {
                actionButtons["DeployShipyard"].interactable = canDeployShipyard;
            }

            // Build Ship - only show if shipyard is selected
            bool showBuildShip = false;
            bool canBuildShip = false;
            if (selectedStructure != null &&
                selectedStructure.type == StructureType.SHIPYARD &&
                selectedStructure.ownerId == 0)
            {
                showBuildShip = true;
                var queue = PlunkAndPlunder.Construction.ConstructionManager.Instance?.GetShipyardQueue(selectedStructure.id);
                int queueCount = queue?.Count ?? 0;
                canBuildShip = queueCount < BuildingConfig.MAX_QUEUE_SIZE &&
                              humanPlayer.gold >= BuildingConfig.BUILD_SHIP_COST;
            }
            actionButtons["BuildShip"].gameObject.SetActive(showBuildShip);
            if (showBuildShip)
            {
                actionButtons["BuildShip"].interactable = canBuildShip;
            }

            // Upgrade buttons - only show if ship is at friendly shipyard
            bool showUpgrades = false;
            if (selectedUnit != null && selectedUnit.ownerId == 0)
            {
                Structure shipyard = gameState.structureManager.GetStructureAtPosition(selectedUnit.position);
                showUpgrades = shipyard != null &&
                              shipyard.type == StructureType.SHIPYARD &&
                              shipyard.ownerId == 0;
            }

            // Upgrade Sails - only show if at shipyard and not maxed
            bool showUpgradeSails = showUpgrades && selectedUnit.sails < BuildingConfig.MAX_SAILS_UPGRADES;
            actionButtons["UpgradeSails"].gameObject.SetActive(showUpgradeSails);
            if (showUpgradeSails)
            {
                actionButtons["UpgradeSails"].interactable = humanPlayer.gold >= BuildingConfig.UPGRADE_SAILS_COST;
            }

            // Upgrade Cannons - only show if at shipyard and not maxed
            bool showUpgradeCannons = showUpgrades && selectedUnit.cannons < BuildingConfig.MAX_CANNONS_UPGRADES;
            actionButtons["UpgradeCannons"].gameObject.SetActive(showUpgradeCannons);
            if (showUpgradeCannons)
            {
                actionButtons["UpgradeCannons"].interactable = humanPlayer.gold >= BuildingConfig.UPGRADE_CANNONS_COST;
            }

            // Upgrade Max Life - only show if at shipyard and not maxed
            bool showUpgradeMaxLife = showUpgrades && selectedUnit.maxHealth < BuildingConfig.MAX_SHIP_TIER;
            actionButtons["UpgradeMaxLife"].gameObject.SetActive(showUpgradeMaxLife);
            if (showUpgradeMaxLife)
            {
                actionButtons["UpgradeMaxLife"].interactable = humanPlayer.gold >= BuildingConfig.UPGRADE_MAX_LIFE_COST;
            }

            // Attack Shipyard - only show if ship is adjacent to enemy shipyard
            bool showAttackShipyard = false;
            if (selectedUnit != null && selectedUnit.ownerId == 0)
            {
                // Check adjacent tiles for enemy shipyards
                HexCoord[] neighbors = selectedUnit.position.GetNeighbors();
                foreach (HexCoord neighbor in neighbors)
                {
                    Structure structure = gameState.structureManager.GetStructureAtPosition(neighbor);
                    if (structure != null &&
                        structure.type == StructureType.SHIPYARD &&
                        structure.ownerId != 0) // Enemy shipyard
                    {
                        showAttackShipyard = true;
                        break;
                    }
                }
            }
            actionButtons["AttackShipyard"].gameObject.SetActive(showAttackShipyard);
            if (showAttackShipyard)
            {
                actionButtons["AttackShipyard"].interactable = true; // Always enabled if shown
            }
        }

        #endregion

        #region Action Button Handlers

        private void OnDeployShipyard()
        {
            Debug.Log("🔘 [LeftPanelHUD] ========== DEPLOY SHIPYARD BUTTON CLICKED! ==========");

            // Trigger deploy via GameHUD
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                Debug.Log("[LeftPanelHUD] Found GameHUD, sending message...");
                gameHUD.SendMessage("OnDeployShipyardClicked", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Debug.LogError("[LeftPanelHUD] GameHUD not found!");
            }
        }

        private void OnBuildShip()
        {
            Debug.Log("[LeftPanelHUD] Build Ship button clicked");

            // Trigger build via GameHUD
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.SendMessage("OnBuildShipClicked", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void OnUpgradeSails()
        {
            Debug.Log("[LeftPanelHUD] Upgrade Sails button clicked - finding GameHUD...");

            // Trigger upgrade via GameHUD
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                Debug.Log("[LeftPanelHUD] GameHUD found, calling OnUpgradeSailsClicked");
                gameHUD.SendMessage("OnUpgradeSailsClicked", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Debug.LogError("[LeftPanelHUD] GameHUD not found!");
            }
        }

        private void OnUpgradeCannons()
        {
            Debug.Log("[LeftPanelHUD] Upgrade Cannons button clicked");

            // Trigger upgrade via GameHUD
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.SendMessage("OnUpgradeCannonsClicked", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void OnUpgradeMaxLife()
        {
            Debug.Log("[LeftPanelHUD] Upgrade Max Life button clicked");

            // Trigger upgrade via GameHUD
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.SendMessage("OnUpgradeMaxLifeClicked", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void OnAttackShipyard()
        {
            Debug.Log("[LeftPanelHUD] Attack Shipyard button clicked");

            // Trigger attack via GameHUD
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                gameHUD.SendMessage("OnAttackShipyardClicked", SendMessageOptions.DontRequireReceiver);
            }
        }

        #endregion

        #region Helper Methods

        private void CreateSeparatorLine(Transform parent)
        {
            GameObject line = new GameObject("Separator", typeof(RectTransform));
            line.transform.SetParent(parent, false);

            RectTransform rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 2);

            Image img = line.AddComponent<Image>();
            img.color = HUDStyles.BorderColor;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
