using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Right panel HUD at BOTTOM-RIGHT containing:
    /// - Combat Event Log (scrollable list of recent combats)
    /// - Turn Summary
    /// </summary>
    public class RightPanelHUD : MonoBehaviour
    {
        [Header("Settings")]
        public int maxCombatEntries = 15;
        public bool showAICombats = true;
        public bool autoScrollToLatest = true;

        // Section containers
        private GameObject combatLogSection;
        private GameObject turnSummarySection;

        // Combat log UI
        private ScrollRect scrollRect;
        private GameObject contentContainer;
        private Text turnSummaryText;

        // Data
        private List<CombatLogEntry> combatEntries = new List<CombatLogEntry>();
        private Dictionary<GameObject, CombatLogEntry> entryObjects = new Dictionary<GameObject, CombatLogEntry>();

        // Colors
        private Color playerWinColor = new Color(0.3f, 1f, 0.3f); // Green
        private Color playerLossColor = new Color(1f, 0.3f, 0.3f); // Red
        private Color aiCombatColor = new Color(0.7f, 0.7f, 0.7f); // Gray

        public void Initialize(GameState state)
        {
            BuildRightPanel();
        }

        private void BuildRightPanel()
        {
            // Setup RectTransform for bottom-right positioning
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // ANCHOR TO BOTTOM-RIGHT
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-HUDStyles.EdgeMargin, HUDStyles.EdgeMargin);

            // Calculate height: reference height - top bar - 2*edge margin
            float panelHeight = HUDStyles.ReferenceHeight - HUDStyles.TopBarHeight - (HUDStyles.EdgeMargin * 2);
            rectTransform.sizeDelta = new Vector2(HUDStyles.RightPanelWidth, panelHeight);

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

            // Build sections
            BuildTurnSummarySection();
            BuildCombatLogSection();

            Debug.Log($"[RightPanelHUD] ===== INITIALIZED =====");
            Debug.Log($"[RightPanelHUD] Position: anchor={rectTransform.anchorMin}, position={rectTransform.anchoredPosition}, size={rectTransform.sizeDelta}");
            Debug.Log($"[RightPanelHUD] Reference Resolution: {HUDStyles.ReferenceWidth}x{HUDStyles.ReferenceHeight}");
            Debug.Log($"[RightPanelHUD] Panel active: {gameObject.activeSelf}");
            Debug.Log($"[RightPanelHUD] Combat log section active: {combatLogSection.activeSelf}");
        }

        #region Turn Summary Section

        private void BuildTurnSummarySection()
        {
            turnSummarySection = new GameObject("TurnSummary");
            turnSummarySection.transform.SetParent(transform, false);

            RectTransform sectionRT = turnSummarySection.AddComponent<RectTransform>();
            sectionRT.sizeDelta = new Vector2(0, 80);

            // Add LayoutElement to control size in VerticalLayoutGroup
            LayoutElement layoutElement = turnSummarySection.AddComponent<LayoutElement>();
            layoutElement.minHeight = 80;
            layoutElement.preferredHeight = 80;

            // Background
            Image sectionBg = turnSummarySection.AddComponent<Image>();
            sectionBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            // Header
            GameObject header = HUDLayoutManager.CreateHeaderText(turnSummarySection.transform, "TURN SUMMARY");
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0, 30);

            // Separator
            CreateSeparatorLine(turnSummarySection.transform);

            // Summary text
            GameObject textObj = new GameObject("SummaryText");
            textObj.transform.SetParent(turnSummarySection.transform, false);

            turnSummaryText = textObj.AddComponent<Text>();
            turnSummaryText.text = "Turn 1 - Planning Phase";
            turnSummaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            turnSummaryText.fontSize = HUDStyles.ContentFontSize;
            turnSummaryText.color = HUDStyles.TextColor;
            turnSummaryText.alignment = TextAnchor.UpperLeft;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 0);
            textRT.pivot = new Vector2(0, 0);
            textRT.anchoredPosition = new Vector2(10, 10);
            textRT.sizeDelta = new Vector2(-20, 30);
        }

        public void UpdateTurnSummary(GameState state)
        {
            if (turnSummaryText == null) return;

            int totalCombats = combatEntries.Count;
            int playerCombats = combatEntries.FindAll(e =>
                e.attackerOwnerId == 0 || e.defenderOwnerId == 0).Count;

            turnSummaryText.text = $"Turn {state.turnNumber}\n" +
                                   $"Combats: {totalCombats} ({playerCombats} yours)";
        }

        #endregion

        #region Combat Log Section

        private void BuildCombatLogSection()
        {
            combatLogSection = new GameObject("CombatLog");
            combatLogSection.transform.SetParent(transform, false);

            RectTransform sectionRT = combatLogSection.AddComponent<RectTransform>();
            sectionRT.sizeDelta = new Vector2(0, 600);

            // Add LayoutElement for proper sizing in parent layout
            LayoutElement layoutElement = combatLogSection.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1; // Take remaining space

            // Background
            Image sectionBg = combatLogSection.AddComponent<Image>();
            sectionBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            // Vertical layout
            VerticalLayoutGroup layout = combatLogSection.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Header
            GameObject header = HUDLayoutManager.CreateHeaderText(combatLogSection.transform, "⚔️ COMBAT LOG");
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(0, 30);

            // Separator
            CreateSeparatorLine(combatLogSection.transform);

            // Scroll view
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(combatLogSection.transform, false);

            RectTransform scrollViewRT = scrollView.AddComponent<RectTransform>();
            scrollViewRT.sizeDelta = new Vector2(0, 550);

            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;

            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = Color.clear;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            scrollRect.viewport = viewportRT;

            // Content container
            contentContainer = new GameObject("Content");
            contentContainer.transform.SetParent(viewport.transform, false);

            RectTransform contentRT = contentContainer.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.spacing = 5f;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter fitter = contentContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
        }

        public void AddCombatEntry(CombatOccurredEvent combatEvent, GameState state)
        {
            Debug.Log($"[RightPanelHUD] AddCombatEntry called - attacker:{combatEvent.attackerId} defender:{combatEvent.defenderId}");

            // Get unit details
            Unit attacker = state.unitManager.GetUnit(combatEvent.attackerId);
            Unit defender = state.unitManager.GetUnit(combatEvent.defenderId);

            if (attacker == null || defender == null)
            {
                Debug.LogWarning($"[RightPanelHUD] Could not find units for combat log: {combatEvent.attackerId} or {combatEvent.defenderId}");
                return;
            }

            Debug.Log($"[RightPanelHUD] Units found - creating entry");

            // Check if should show (filter AI combats if disabled)
            bool humanInvolved = attacker.ownerId == 0 || defender.ownerId == 0;
            if (!showAICombats && !humanInvolved)
            {
                return;
            }

            // Create entry
            CombatLogEntry entry = new CombatLogEntry
            {
                attackerName = attacker.GetDisplayName(state.playerManager),
                defenderName = defender.GetDisplayName(state.playerManager),
                attackerOwnerId = attacker.ownerId,
                defenderOwnerId = defender.ownerId,
                damageToDefender = combatEvent.damageToDefender,
                damageToAttacker = combatEvent.damageToAttacker,
                attackerDestroyed = combatEvent.attackerDestroyed,
                defenderDestroyed = combatEvent.defenderDestroyed,
                turnNumber = state.turnNumber,
                location = defender.position
            };

            combatEntries.Add(entry);

            // Remove oldest if exceeds max
            while (combatEntries.Count > maxCombatEntries)
            {
                var oldest = combatEntries[0];
                combatEntries.RemoveAt(0);

                // Remove UI entry
                if (entryObjects.ContainsKey(oldest.uiObject))
                {
                    Destroy(oldest.uiObject);
                    entryObjects.Remove(oldest.uiObject);
                }
            }

            // Create UI entry
            CreateCombatEntryUI(entry);
            Debug.Log($"[RightPanelHUD] Created UI entry, total entries: {combatEntries.Count}");

            // Auto-scroll to latest
            if (autoScrollToLatest)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }

            // Update turn summary
            UpdateTurnSummary(state);
            Debug.Log($"[RightPanelHUD] Entry added successfully");
        }

        private void CreateCombatEntryUI(CombatLogEntry entry)
        {
            GameObject entryObj = new GameObject("CombatEntry");
            entryObj.transform.SetParent(contentContainer.transform, false);

            RectTransform entryRT = entryObj.AddComponent<RectTransform>();

            // Background
            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);

            // Layout element for proper sizing in parent's VerticalLayoutGroup
            LayoutElement entryLayoutElement = entryObj.AddComponent<LayoutElement>();
            entryLayoutElement.minHeight = 70;
            entryLayoutElement.preferredHeight = 70;

            // Layout group for stacking text - DISABLED, use manual positioning instead
            // VerticalLayoutGroup causes positioning issues, use simple stacking

            // Main combat text line 1: "Attacker → Defender"
            GameObject line1Obj = new GameObject("Line1");
            line1Obj.transform.SetParent(entryObj.transform, false);

            RectTransform line1RT = line1Obj.AddComponent<RectTransform>();
            line1RT.anchorMin = new Vector2(0, 1);
            line1RT.anchorMax = new Vector2(1, 1);
            line1RT.pivot = new Vector2(0, 1);
            line1RT.anchoredPosition = new Vector2(8, -5);
            line1RT.sizeDelta = new Vector2(-16, 20);

            Text line1Text = line1Obj.AddComponent<Text>();
            line1Text.text = $"{GetCombatIcon(entry)} {entry.attackerName} → {entry.defenderName}";
            line1Text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            line1Text.fontSize = 14;
            line1Text.fontStyle = FontStyle.Bold;
            line1Text.color = GetEntryColor(entry);
            line1Text.alignment = TextAnchor.MiddleLeft;

            // Line 2: Outcome
            GameObject line2Obj = new GameObject("Line2");
            line2Obj.transform.SetParent(entryObj.transform, false);

            RectTransform line2RT = line2Obj.AddComponent<RectTransform>();
            line2RT.anchorMin = new Vector2(0, 1);
            line2RT.anchorMax = new Vector2(1, 1);
            line2RT.pivot = new Vector2(0, 1);
            line2RT.anchoredPosition = new Vector2(8, -27);
            line2RT.sizeDelta = new Vector2(-16, 18);

            Text line2Text = line2Obj.AddComponent<Text>();
            line2Text.text = $"   {GetOutcomeText(entry)}";
            line2Text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            line2Text.fontSize = 12;
            line2Text.color = new Color(0.9f, 0.9f, 0.9f);
            line2Text.alignment = TextAnchor.MiddleLeft;

            // Line 3: Turn info
            GameObject line3Obj = new GameObject("Line3");
            line3Obj.transform.SetParent(entryObj.transform, false);

            RectTransform line3RT = line3Obj.AddComponent<RectTransform>();
            line3RT.anchorMin = new Vector2(0, 1);
            line3RT.anchorMax = new Vector2(1, 1);
            line3RT.pivot = new Vector2(0, 1);
            line3RT.anchoredPosition = new Vector2(8, -47);
            line3RT.sizeDelta = new Vector2(-16, 16);

            Text line3Text = line3Obj.AddComponent<Text>();
            line3Text.text = $"   Turn {entry.turnNumber}";
            line3Text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            line3Text.fontSize = 11;
            line3Text.color = new Color(0.7f, 0.7f, 0.7f);
            line3Text.alignment = TextAnchor.MiddleLeft;

            entry.uiObject = entryObj;
            entryObjects[entryObj] = entry;

            Debug.Log($"[RightPanelHUD] Entry UI created: active={entryObj.activeSelf}, parent={contentContainer.name}, childCount={contentContainer.transform.childCount}");
            Debug.Log($"[RightPanelHUD] Entry position: {entryObj.transform.position}, localPos: {entryObj.transform.localPosition}");
            Debug.Log($"[RightPanelHUD] Entry has {entryObj.transform.childCount} children (should be 3 text lines)");
        }

        private string GetCombatIcon(CombatLogEntry entry)
        {
            if (entry.attackerDestroyed && entry.defenderDestroyed)
                return "[X-X]"; // Mutual destruction
            if (entry.defenderDestroyed)
                return "[WIN]"; // Attacker won
            if (entry.attackerDestroyed)
                return "[LOST]"; // Defender won
            return "[DMG]"; // Ongoing combat
        }

        private string GetOutcomeText(CombatLogEntry entry)
        {
            if (entry.attackerDestroyed && entry.defenderDestroyed)
                return "Mutual destruction";
            if (entry.defenderDestroyed)
                return $"{entry.defenderName} destroyed";
            if (entry.attackerDestroyed)
                return $"{entry.attackerName} destroyed";
            return $"-{entry.damageToDefender} HP dealt";
        }

        private Color GetEntryColor(CombatLogEntry entry)
        {
            // Player involved and victorious = green
            if (entry.attackerOwnerId == 0 && entry.defenderDestroyed)
                return playerWinColor;
            if (entry.defenderOwnerId == 0 && entry.attackerDestroyed)
                return playerWinColor;

            // Player involved and damaged/destroyed = red
            if (entry.attackerOwnerId == 0 && entry.attackerDestroyed)
                return playerLossColor;
            if (entry.defenderOwnerId == 0 && entry.defenderDestroyed)
                return playerLossColor;
            if (entry.attackerOwnerId == 0 || entry.defenderOwnerId == 0)
                return playerLossColor;

            // AI only = gray
            return aiCombatColor;
        }

        public void ClearCombatLog()
        {
            foreach (var entry in combatEntries)
            {
                if (entry.uiObject != null)
                    Destroy(entry.uiObject);
            }
            combatEntries.Clear();
            entryObjects.Clear();
        }

        #endregion

        #region Helper Methods

        private void CreateSeparatorLine(Transform parent)
        {
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(parent, false);

            RectTransform separatorRT = separator.AddComponent<RectTransform>();
            separatorRT.sizeDelta = new Vector2(0, 2);

            Image separatorImg = separator.AddComponent<Image>();
            separatorImg.color = HUDStyles.BorderColor;
        }

        #endregion
    }

    public class CombatLogEntry
    {
        public string attackerName;
        public string defenderName;
        public int attackerOwnerId;
        public int defenderOwnerId;
        public int damageToDefender;
        public int damageToAttacker;
        public bool attackerDestroyed;
        public bool defenderDestroyed;
        public int turnNumber;
        public HexCoord location;
        public GameObject uiObject;
    }
}
