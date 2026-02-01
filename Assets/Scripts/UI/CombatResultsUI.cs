using System;
using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Legacy UI for displaying combat results (fallback)
    /// NOTE: CombatResultsHUD.cs is the primary UI for deterministic combat
    /// </summary>
    public class CombatResultsUI : MonoBehaviour
    {
        private GameObject modalPanel;
        private Text titleText;
        private Text attackerNameText;
        private Text defenderNameText;
        private Text attackerDiceText;
        private Text defenderDiceText;
        private Text attackerDamageText;
        private Text defenderDamageText;
        private Text attackerHPText;
        private Text defenderHPText;
        private Button continueButton;

        private Action onContinueCallback;

        public void Initialize()
        {
            CreateModal();
            HideModal();
        }

        private void CreateModal()
        {
            // Create modal background (semi-transparent overlay)
            modalPanel = new GameObject("CombatResultsModal");
            modalPanel.transform.SetParent(transform, false);

            RectTransform panelRect = modalPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = modalPanel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.8f);

            // Create dialog box
            GameObject dialog = CreatePanel(new Vector2(0, 0), new Vector2(700, 500), new Color(0.1f, 0.1f, 0.1f, 0.98f));
            dialog.transform.SetParent(modalPanel.transform, false);

            // Title
            titleText = CreateText("COMBAT!", 36, dialog.transform);
            titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
            titleText.color = new Color(1f, 0.2f, 0.2f); // Red
            titleText.fontStyle = FontStyle.Bold;

            // Attacker Section (Left)
            CreateText("ATTACKER", 24, dialog.transform).GetComponent<RectTransform>().anchoredPosition = new Vector2(-250, 140);

            attackerNameText = CreateText("Unit ID", 18, dialog.transform);
            attackerNameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-250, 100);
            attackerNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            attackerDiceText = CreateText("Dice: [?, ?, ?]", 20, dialog.transform);
            attackerDiceText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-250, 40);
            attackerDiceText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);
            attackerDiceText.color = new Color(0.8f, 0.8f, 1f);

            attackerDamageText = CreateText("Damage: 0", 18, dialog.transform);
            attackerDamageText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-250, -40);
            attackerDamageText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
            attackerDamageText.color = new Color(1f, 0.3f, 0.3f);

            attackerHPText = CreateText("HP: 10/10", 18, dialog.transform);
            attackerHPText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-250, -80);
            attackerHPText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            // VS Divider
            Text vsText = CreateText("VS", 32, dialog.transform);
            vsText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
            vsText.fontStyle = FontStyle.Bold;
            vsText.color = new Color(1f, 1f, 0.5f);

            // Defender Section (Right)
            CreateText("DEFENDER", 24, dialog.transform).GetComponent<RectTransform>().anchoredPosition = new Vector2(250, 140);

            defenderNameText = CreateText("Unit ID", 18, dialog.transform);
            defenderNameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, 100);
            defenderNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            defenderDiceText = CreateText("Dice: [?, ?]", 20, dialog.transform);
            defenderDiceText.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, 40);
            defenderDiceText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);
            defenderDiceText.color = new Color(0.8f, 0.8f, 1f);

            defenderDamageText = CreateText("Damage: 0", 18, dialog.transform);
            defenderDamageText.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, -40);
            defenderDamageText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
            defenderDamageText.color = new Color(1f, 0.3f, 0.3f);

            defenderHPText = CreateText("HP: 10/10", 18, dialog.transform);
            defenderHPText.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, -80);
            defenderHPText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            // Continue button
            continueButton = CreateButton("Continue", new Vector2(0, -180), OnContinueClicked);
            continueButton.transform.SetParent(dialog.transform, false);
            continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 70);

            // Set button color to green
            Image continueImage = continueButton.GetComponent<Image>();
            continueImage.color = new Color(0.2f, 0.6f, 0.2f);
        }

        private GameObject CreatePanel(Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject("Panel");
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image bg = panel.AddComponent<Image>();
            bg.color = color;

            // Add border
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.7f, 0.2f, 0.2f);
            outline.effectDistance = new Vector2(3, -3);

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
            rect.sizeDelta = new Vector2(400, 60);

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
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        public void ShowCombatResults(CombatOccurredEvent combatEvent, Unit attacker, Unit defender, Action callback, PlunkAndPlunder.Players.PlayerManager playerManager = null)
        {
            onContinueCallback = callback;

            // Set unit names using display names
            attackerNameText.text = attacker.GetDisplayName(playerManager);
            defenderNameText.text = defender.GetDisplayName(playerManager);

            // Deterministic combat - no dice rolls
            attackerDiceText.text = $"Cannons: {attacker.cannons}";
            defenderDiceText.text = $"Cannons: {defender.cannons}";

            // Show damage dealt
            attackerDamageText.text = combatEvent.damageToAttacker > 0
                ? $"Took {combatEvent.damageToAttacker} damage!"
                : "No damage";

            defenderDamageText.text = combatEvent.damageToDefender > 0
                ? $"Took {combatEvent.damageToDefender} damage!"
                : "No damage";

            // Show resulting HP
            int attackerNewHP = attacker.health;
            int defenderNewHP = defender.health;

            attackerHPText.text = $"HP: {attackerNewHP}/{attacker.maxHealth}";
            defenderHPText.text = $"HP: {defenderNewHP}/{defender.maxHealth}";

            // Color HP text based on status
            if (combatEvent.attackerDestroyed)
            {
                attackerHPText.text = "DESTROYED!";
                attackerHPText.color = new Color(1f, 0.2f, 0.2f);
            }
            else
            {
                attackerHPText.color = attackerNewHP > attacker.maxHealth / 2
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(1f, 0.7f, 0.3f);
            }

            if (combatEvent.defenderDestroyed)
            {
                defenderHPText.text = "DESTROYED!";
                defenderHPText.color = new Color(1f, 0.2f, 0.2f);
            }
            else
            {
                defenderHPText.color = defenderNewHP > defender.maxHealth / 2
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(1f, 0.7f, 0.3f);
            }

            modalPanel.SetActive(true);
        }

        private string FormatDiceRolls(List<int> rolls, bool highlightTop2)
        {
            if (rolls == null || rolls.Count == 0)
                return "[No rolls]";

            // Get sorted copy to find top 2 values
            List<int> sorted = new List<int>(rolls);
            sorted.Sort();
            sorted.Reverse();

            int topValue1 = sorted.Count > 0 ? sorted[0] : 0;
            int topValue2 = sorted.Count > 1 ? sorted[1] : 0;

            string result = "";
            int top2Count = 0;

            for (int i = 0; i < rolls.Count; i++)
            {
                int roll = rolls[i];

                // Highlight the top 2 rolls
                bool isTop2 = highlightTop2 && top2Count < 2 && (roll == topValue1 || (top2Count < 1 && roll == topValue2));

                if (isTop2)
                {
                    result += $"[{roll}*]";
                    top2Count++;
                }
                else
                {
                    result += $"[{roll}]";
                }

                if (i < rolls.Count - 1)
                    result += " ";
            }

            return result;
        }

        private void HideModal()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        private void OnContinueClicked()
        {
            HideModal();
            onContinueCallback?.Invoke();
        }
    }
}
