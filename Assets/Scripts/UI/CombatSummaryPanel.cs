using PlunkAndPlunder.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Shows end-of-turn combat summary with statistics
    /// Displays total combats, ships destroyed, and notable events
    /// </summary>
    public class CombatSummaryPanel : MonoBehaviour
    {
        [Header("Display Settings")]
        public float displayDuration = 4f;
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.3f;

        private GameObject panel;
        private CanvasGroup canvasGroup;
        private Text summaryText;
        private bool isShowing = false;

        public void Initialize()
        {
            CreatePanel();
            gameObject.SetActive(false);
        }

        private void CreatePanel()
        {
            // Create panel container
            panel = new GameObject("CombatSummaryPanel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 300);
            panelRect.anchoredPosition = Vector2.zero;

            // Add canvas group for fading
            canvasGroup = panel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Background
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "COMBAT SUMMARY";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.8f, 0.2f);
            titleText.alignment = TextAnchor.MiddleCenter;

            // Summary text
            GameObject textObj = new GameObject("SummaryText");
            textObj.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.1f);
            textRect.anchorMax = new Vector2(0.95f, 0.75f);
            textRect.sizeDelta = Vector2.zero;

            summaryText = textObj.AddComponent<Text>();
            summaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            summaryText.fontSize = 20;
            summaryText.color = Color.white;
            summaryText.alignment = TextAnchor.UpperLeft;

            Debug.Log("[CombatSummaryPanel] Initialized");
        }

        /// <summary>
        /// Show combat summary with statistics from the turn
        /// </summary>
        public void ShowSummary(List<CombatOccurredEvent> combatEvents, List<UnitDestroyedEvent> destroyedEvents)
        {
            if (isShowing || combatEvents.Count == 0)
                return;

            // Analyze combat events
            int totalCombats = combatEvents.Count;
            int shipsDestroyed = destroyedEvents.Count;

            Dictionary<CombatType, int> combatsByType = new Dictionary<CombatType, int>();
            int mutualDestructions = 0;

            foreach (var combat in combatEvents)
            {
                if (!combatsByType.ContainsKey(combat.combatType))
                    combatsByType[combat.combatType] = 0;
                combatsByType[combat.combatType]++;

                if (combat.attackerDestroyed && combat.defenderDestroyed)
                    mutualDestructions++;
            }

            // Build summary text
            string summary = $"<b>Total Combats:</b> {totalCombats}\n";
            summary += $"<b>Ships Destroyed:</b> {shipsDestroyed}\n\n";

            if (combatsByType.Count > 0)
            {
                summary += "<b>Combat Types:</b>\n";
                foreach (var kvp in combatsByType)
                {
                    string typeName = kvp.Key.ToString();
                    summary += $"  • {typeName}: {kvp.Value}\n";
                }
            }

            if (mutualDestructions > 0)
            {
                summary += $"\n<b>Mutual Destructions:</b> {mutualDestructions}";
            }

            // Show panel
            summaryText.text = summary;
            StartCoroutine(ShowPanelCoroutine());
        }

        private System.Collections.IEnumerator ShowPanelCoroutine()
        {
            isShowing = true;
            gameObject.SetActive(true);

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Display
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
            isShowing = false;
        }

        /// <summary>
        /// Show a simple message (for non-combat summaries)
        /// </summary>
        public void ShowMessage(string message, float duration = 3f)
        {
            if (isShowing)
                return;

            summaryText.text = message;
            displayDuration = duration;
            StartCoroutine(ShowPanelCoroutine());
        }
    }
}
