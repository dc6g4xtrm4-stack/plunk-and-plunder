using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Displays a scrollable log of combat events in the corner of the screen
    /// Non-intrusive, persistent through turn, click to focus camera
    /// </summary>
    public class CombatEventLog : MonoBehaviour
    {
        [Header("Layout")]
        public int maxEntries = 20;
        public float entryHeight = 50f;
        public float panelWidth = 320f;
        public float maxPanelHeight = 400f;

        [Header("Colors")]
        public Color playerCombatColor = new Color(0.3f, 1f, 0.3f); // Green
        public Color playerDamagedColor = new Color(1f, 0.3f, 0.3f); // Red
        public Color aiCombatColor = new Color(0.7f, 0.7f, 0.7f); // Gray
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        public Color headerColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        [Header("Settings")]
        public bool autoScrollToLatest = true;
        public bool showAICombats = true;

        private GameObject panel;
        private GameObject contentContainer;
        private ScrollRect scrollRect;
        private List<CombatLogEntry> entries = new List<CombatLogEntry>();
        private Dictionary<GameObject, CombatLogEntry> entryObjects = new Dictionary<GameObject, CombatLogEntry>();
        private bool isMinimized = false;
        private Text entryCountText;

        public void Initialize()
        {
            CreateUI();
            Debug.Log("[CombatEventLog] Combat event log initialized");
        }

        private void CreateUI()
        {
            // Main panel - bottom-left corner
            panel = new GameObject("CombatEventLogPanel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(0, 0);
            panelRT.pivot = new Vector2(0, 0);
            panelRT.anchoredPosition = new Vector2(10, 10);
            panelRT.sizeDelta = new Vector2(panelWidth, 100); // Start small, expand as needed

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = backgroundColor;

            // Header
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);

            RectTransform headerRT = header.AddComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.anchoredPosition = Vector2.zero;
            headerRT.sizeDelta = new Vector2(0, 35);

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = headerColor;

            // Header text
            GameObject headerText = new GameObject("HeaderText");
            headerText.transform.SetParent(header.transform, false);

            Text headerLabel = headerText.AddComponent<Text>();
            headerLabel.text = "⚔️ COMBAT LOG";
            headerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerLabel.fontSize = 14;
            headerLabel.fontStyle = FontStyle.Bold;
            headerLabel.color = Color.white;
            headerLabel.alignment = TextAnchor.MiddleLeft;

            RectTransform headerTextRT = headerText.GetComponent<RectTransform>();
            headerTextRT.anchorMin = Vector2.zero;
            headerTextRT.anchorMax = Vector2.one;
            headerTextRT.offsetMin = new Vector2(10, 0);
            headerTextRT.offsetMax = new Vector2(-50, 0);

            // Minimize button
            GameObject minButton = CreateButton(header.transform, "[_]", OnMinimizeClicked);
            RectTransform minButtonRT = minButton.GetComponent<RectTransform>();
            minButtonRT.anchorMin = new Vector2(1, 0);
            minButtonRT.anchorMax = new Vector2(1, 1);
            minButtonRT.pivot = new Vector2(1, 0.5f);
            minButtonRT.anchoredPosition = new Vector2(-5, 0);
            minButtonRT.sizeDelta = new Vector2(40, 25);

            // Scroll view for entries
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(panel.transform, false);

            RectTransform scrollViewRT = scrollView.AddComponent<RectTransform>();
            scrollViewRT.anchorMin = new Vector2(0, 0);
            scrollViewRT.anchorMax = new Vector2(1, 1);
            scrollViewRT.offsetMin = new Vector2(5, 5);
            scrollViewRT.offsetMax = new Vector2(-5, -40); // Leave room for header

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

            VerticalLayoutGroup layout = contentContainer.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 3f;
            layout.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter fitter = contentContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;

            // Entry count text (when minimized)
            GameObject countTextObj = new GameObject("CountText");
            countTextObj.transform.SetParent(panel.transform, false);
            countTextObj.SetActive(false); // Hidden initially

            entryCountText = countTextObj.AddComponent<Text>();
            entryCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            entryCountText.fontSize = 16;
            entryCountText.fontStyle = FontStyle.Bold;
            entryCountText.color = Color.white;
            entryCountText.alignment = TextAnchor.MiddleCenter;

            RectTransform countTextRT = countTextObj.GetComponent<RectTransform>();
            countTextRT.anchorMin = Vector2.zero;
            countTextRT.anchorMax = Vector2.one;
            countTextRT.sizeDelta = Vector2.zero;
        }

        public void AddCombatEntry(CombatOccurredEvent combatEvent, GameState state)
        {
            // Get unit details
            Unit attacker = state.unitManager.GetUnit(combatEvent.attackerId);
            Unit defender = state.unitManager.GetUnit(combatEvent.defenderId);

            if (attacker == null || defender == null)
            {
                Debug.LogWarning($"[CombatEventLog] Could not find units for combat log: {combatEvent.attackerId} or {combatEvent.defenderId}");
                return;
            }

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

            entries.Add(entry);

            // Remove oldest if exceeds max
            while (entries.Count > maxEntries)
            {
                var oldest = entries[0];
                entries.RemoveAt(0);

                // Remove UI entry
                if (entryObjects.ContainsKey(oldest.uiObject))
                {
                    Destroy(oldest.uiObject);
                    entryObjects.Remove(oldest.uiObject);
                }
            }

            // Create UI entry
            CreateEntryUI(entry);

            // Auto-scroll to latest
            if (autoScrollToLatest && !isMinimized)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom
            }

            // Update panel size
            UpdatePanelSize();
        }

        private void CreateEntryUI(CombatLogEntry entry)
        {
            GameObject entryObj = new GameObject("CombatEntry");
            entryObj.transform.SetParent(contentContainer.transform, false);

            RectTransform entryRT = entryObj.AddComponent<RectTransform>();
            entryRT.sizeDelta = new Vector2(0, entryHeight);

            // Background
            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // Make clickable
            Button button = entryObj.AddComponent<Button>();
            button.targetGraphic = entryBg;
            button.onClick.AddListener(() => OnEntryClicked(entry));

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            button.colors = colors;

            // Entry text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(entryObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = entry.GetDisplayText();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 11;
            text.color = GetEntryColor(entry);
            text.alignment = TextAnchor.MiddleLeft;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(5, 5);
            textRT.offsetMax = new Vector2(-5, -5);

            entry.uiObject = entryObj;
            entryObjects[entryObj] = entry;
        }

        private Color GetEntryColor(CombatLogEntry entry)
        {
            // Player involved and victorious = green
            if (entry.attackerOwnerId == 0 && entry.defenderDestroyed)
                return playerCombatColor;

            if (entry.defenderOwnerId == 0 && entry.attackerDestroyed)
                return playerCombatColor;

            // Player involved and damaged/destroyed = red
            if (entry.attackerOwnerId == 0 && entry.attackerDestroyed)
                return playerDamagedColor;

            if (entry.defenderOwnerId == 0 && entry.defenderDestroyed)
                return playerDamagedColor;

            if (entry.attackerOwnerId == 0 || entry.defenderOwnerId == 0)
                return playerDamagedColor;

            // AI only = gray
            return aiCombatColor;
        }

        private void UpdatePanelSize()
        {
            if (isMinimized)
            {
                RectTransform panelRT = panel.GetComponent<RectTransform>();
                panelRT.sizeDelta = new Vector2(panelWidth, 50); // Just header
                return;
            }

            int visibleEntries = Mathf.Min(entries.Count, maxEntries);
            float contentHeight = visibleEntries * (entryHeight + 3f) + 10f; // Include spacing and padding
            float totalHeight = Mathf.Min(contentHeight + 40f, maxPanelHeight); // Add header height

            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(panelWidth, totalHeight);
        }

        private void OnEntryClicked(CombatLogEntry entry)
        {
            Debug.Log($"[CombatEventLog] Entry clicked: {entry.GetDisplayText()}");

            // Request camera focus on combat location
            // This will be implemented when we add the camera manager
            // For now, just log
            Debug.Log($"[CombatEventLog] TODO: Focus camera on {entry.location}");
        }

        private void OnMinimizeClicked()
        {
            isMinimized = !isMinimized;

            if (isMinimized)
            {
                // Hide scroll view, show count
                scrollRect.gameObject.SetActive(false);
                entryCountText.gameObject.SetActive(true);
                entryCountText.text = $"⚔️ {entries.Count}";
            }
            else
            {
                // Show scroll view, hide count
                scrollRect.gameObject.SetActive(true);
                entryCountText.gameObject.SetActive(false);
            }

            UpdatePanelSize();
        }

        private GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(parent, false);

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            button.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            return buttonObj;
        }

        public void ClearAll()
        {
            foreach (var entry in entries)
            {
                if (entry.uiObject != null)
                    Destroy(entry.uiObject);
            }
            entries.Clear();
            entryObjects.Clear();
            UpdatePanelSize();
        }
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
        public Map.HexCoord location;
        public GameObject uiObject;

        public string GetDisplayText()
        {
            string icon = "⚔️";
            string outcome = "";

            if (attackerDestroyed && defenderDestroyed)
            {
                icon = "🔥";
                outcome = "Mutual destruction";
            }
            else if (defenderDestroyed)
            {
                icon = "💀";
                outcome = $"{defenderName} destroyed";
            }
            else if (attackerDestroyed)
            {
                icon = "💀";
                outcome = $"{attackerName} destroyed";
            }
            else
            {
                outcome = $"-{damageToDefender} HP";
            }

            return $"{icon} {attackerName} → {defenderName}\n    {outcome} (Turn {turnNumber})";
        }
    }
}
