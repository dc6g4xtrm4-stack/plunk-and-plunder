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
        // References
        private GameState gameState;

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
            gameState = state;
            BuildLeftPanel();
        }

        private void BuildLeftPanel()
        {
            // Setup RectTransform for bottom-left positioning
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // CRITICAL: Anchor to BOTTOM-LEFT
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(HUDStyles.EdgeMargin, HUDStyles.EdgeMargin);

            float height = Screen.height - HUDStyles.TopBarHeight - (HUDStyles.EdgeMargin * 2);
            rectTransform.sizeDelta = new Vector2(HUDStyles.LeftPanelWidth, height);

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

            // Build sections - ACTION BUTTONS FIRST so they appear at top!
            BuildActionButtonsSection();
            BuildPlayerStatsSection();
            BuildUnitDetailsSection();
            BuildBuildQueueSection();

            // Initially hide build queue
            buildQueueSection.SetActive(false);

            Debug.Log($"[LeftPanelHUD] Initialized at bottom-left: position={rectTransform.anchoredPosition}, size={rectTransform.sizeDelta}");
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
            if (playerStatsText == null || gameState == null) return;

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

            if (unitDetailsText == null || gameState == null) return;

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
            actionButtonsSection = new GameObject("ActionButtonsSection", typeof(RectTransform));
            actionButtonsSection.transform.SetParent(transform, false);

            // Use RectTransform sizeDelta like other sections (NOT LayoutElement!)
            RectTransform rt = actionButtonsSection.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 250); // Fixed height for 5 buttons

            VerticalLayoutGroup layout = actionButtonsSection.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = HUDStyles.ButtonSpacing;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Create action buttons
            CreateActionButton("DeployShipyard", "Deploy Shipyard (100g)", OnDeployShipyard);
            CreateActionButton("BuildShip", "Build Ship (50g)", OnBuildShip);
            CreateActionButton("UpgradeSails", "Upgrade Sails (60g)", OnUpgradeSails);
            CreateActionButton("UpgradeCannons", "Upgrade Cannons (80g)", OnUpgradeCannons);
            CreateActionButton("UpgradeMaxLife", "Upgrade Max Life (100g)", OnUpgradeMaxLife);

            UpdateActionButtons();
        }

        private void CreateActionButton(string id, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(id + "Button", typeof(RectTransform));
            buttonObj.transform.SetParent(actionButtonsSection.transform, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, HUDStyles.ButtonHeight);

            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = HUDStyles.ButtonNormalColor;

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
            if (gameState == null || gameState.phase != GamePhase.Planning)
            {
                // Disable all buttons if not in planning phase
                foreach (var btn in actionButtons.Values)
                {
                    btn.interactable = false;
                }
                return;
            }

            Player humanPlayer = gameState.playerManager.GetPlayer(0);
            if (humanPlayer == null) return;

            // Deploy Shipyard - available on harbor tiles with units
            bool canDeployShipyard = false;
            if (selectedUnit != null && selectedUnit.ownerId == 0)
            {
                Tile tile = gameState.grid.GetTile(selectedUnit.position);
                canDeployShipyard = tile != null &&
                                   tile.type == TileType.HARBOR &&
                                   gameState.structureManager.GetStructureAtPosition(selectedUnit.position) == null &&
                                   humanPlayer.gold >= BuildingConfig.DEPLOY_SHIPYARD_COST;
            }
            actionButtons["DeployShipyard"].interactable = canDeployShipyard;

            // Build Ship - available on owned shipyards
            bool canBuildShip = false;
            if (selectedStructure != null &&
                selectedStructure.type == StructureType.SHIPYARD &&
                selectedStructure.ownerId == 0)
            {
                canBuildShip = selectedStructure.buildQueue.Count < BuildingConfig.MAX_QUEUE_SIZE &&
                              humanPlayer.gold >= BuildingConfig.BUILD_SHIP_COST;
            }
            actionButtons["BuildShip"].interactable = canBuildShip;

            // Upgrade buttons - available on owned units at shipyards
            bool canUpgrade = false;
            if (selectedUnit != null && selectedUnit.ownerId == 0)
            {
                Structure shipyard = gameState.structureManager.GetStructureAtPosition(selectedUnit.position);
                canUpgrade = shipyard != null &&
                            shipyard.type == StructureType.SHIPYARD &&
                            shipyard.ownerId == 0;
            }

            actionButtons["UpgradeSails"].interactable = canUpgrade &&
                                                         selectedUnit.sails < BuildingConfig.MAX_SAILS_UPGRADES &&
                                                         humanPlayer.gold >= BuildingConfig.UPGRADE_SAILS_COST;
            actionButtons["UpgradeCannons"].interactable = canUpgrade &&
                                                           selectedUnit.cannons < BuildingConfig.MAX_CANNONS_UPGRADES &&
                                                           humanPlayer.gold >= BuildingConfig.UPGRADE_CANNONS_COST;
            actionButtons["UpgradeMaxLife"].interactable = canUpgrade &&
                                                           selectedUnit.maxHealth < BuildingConfig.MAX_SHIP_TIER &&
                                                           humanPlayer.gold >= BuildingConfig.UPGRADE_MAX_LIFE_COST;
        }

        #endregion

        #region Action Button Handlers

        private void OnDeployShipyard()
        {
            // Note: This should be handled by GameHUD/GameManager to create orders
            Debug.Log("[LeftPanelHUD] Deploy Shipyard button clicked");
        }

        private void OnBuildShip()
        {
            if (selectedStructure != null && selectedStructure.type == StructureType.SHIPYARD)
            {
                Player humanPlayer = gameState.playerManager.GetPlayer(0);
                if (humanPlayer != null && humanPlayer.gold >= BuildingConfig.BUILD_SHIP_COST)
                {
                    // Immediately add to build queue
                    BuildQueueItem item = new BuildQueueItem("Ship", BuildingConfig.SHIP_BUILD_TIME, BuildingConfig.BUILD_SHIP_COST);
                    selectedStructure.buildQueue.Add(item);

                    // Deduct gold
                    humanPlayer.gold -= BuildingConfig.BUILD_SHIP_COST;

                    // Refresh display
                    UpdateSelection(selectedUnit, selectedStructure);
                    UpdatePlayerStats();

                    Debug.Log($"[LeftPanelHUD] Ship added to build queue at {selectedStructure.id}");
                }
            }
        }

        private void OnUpgradeSails()
        {
            Debug.Log("[LeftPanelHUD] Upgrade Sails button clicked");
        }

        private void OnUpgradeCannons()
        {
            Debug.Log("[LeftPanelHUD] Upgrade Cannons button clicked");
        }

        private void OnUpgradeMaxLife()
        {
            Debug.Log("[LeftPanelHUD] Upgrade Max Life button clicked");
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
