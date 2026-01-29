using System;
using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// UI for resolving conflicts when units collide or meet during turn animation
    /// </summary>
    public class ConflictResolutionUI : MonoBehaviour
    {
        private GameObject modalPanel;
        private Text titleText;
        private Text descriptionText;
        private Button rerouteButton;
        private Button combatButton;

        private ConflictData currentConflict;
        private Action<ConflictResolution> onResolutionCallback;

        public void Initialize()
        {
            CreateModal();
            HideModal();
        }

        private void CreateModal()
        {
            // Create modal background (semi-transparent overlay)
            modalPanel = new GameObject("ConflictModal");
            modalPanel.transform.SetParent(transform, false);

            RectTransform panelRect = modalPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = modalPanel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.7f);

            // Create dialog box
            GameObject dialog = CreatePanel(new Vector2(0, 0), new Vector2(600, 400), new Color(0.15f, 0.15f, 0.15f, 0.95f));
            dialog.transform.SetParent(modalPanel.transform, false);

            // Title
            titleText = CreateText("CONFLICT DETECTED", 32, dialog.transform);
            titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
            titleText.color = new Color(1f, 0.3f, 0.3f); // Red warning color

            // Description
            descriptionText = CreateText("Description", 20, dialog.transform);
            descriptionText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
            descriptionText.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 150);
            descriptionText.alignment = TextAnchor.MiddleCenter;

            // Re-route button
            rerouteButton = CreateButton("Re-route Orders", new Vector2(-150, -120), OnRerouteClicked);
            rerouteButton.transform.SetParent(dialog.transform, false);
            rerouteButton.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 60);

            // Set button color to blue
            Image rerouteImage = rerouteButton.GetComponent<Image>();
            rerouteImage.color = new Color(0.2f, 0.4f, 0.7f);

            // Combat button
            combatButton = CreateButton("Continue to Combat", new Vector2(150, -120), OnCombatClicked);
            combatButton.transform.SetParent(dialog.transform, false);
            combatButton.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 60);

            // Set button color to red
            Image combatImage = combatButton.GetComponent<Image>();
            combatImage.color = new Color(0.7f, 0.2f, 0.2f);
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
            rect.sizeDelta = new Vector2(500, 60);

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

        public void ShowConflict(ConflictData conflict, Action<ConflictResolution> callback)
        {
            currentConflict = conflict;
            onResolutionCallback = callback;

            // Build description text
            string description = $"Units are about to collide:\n\n";
            foreach (Unit unit in conflict.involvedUnits)
            {
                description += $"Player {unit.ownerId} - {unit.id}\n";
                description += $"Position: {unit.position}\n";
                description += $"HP: {unit.health}/{unit.maxHealth}\n\n";
            }

            description += "\nChoose how to resolve this conflict:";

            descriptionText.text = description;

            modalPanel.SetActive(true);
        }

        private void HideModal()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        private void OnRerouteClicked()
        {
            HideModal();
            onResolutionCallback?.Invoke(ConflictResolution.Reroute);
        }

        private void OnCombatClicked()
        {
            HideModal();
            onResolutionCallback?.Invoke(ConflictResolution.Combat);
        }
    }

    [Serializable]
    public class ConflictData
    {
        public List<Unit> involvedUnits;
        public Map.HexCoord position;

        public ConflictData(List<Unit> units, Map.HexCoord pos)
        {
            involvedUnits = units;
            position = pos;
        }
    }

    public enum ConflictResolution
    {
        Reroute,    // Cancel turn, return to planning
        Combat      // Resolve with dice combat
    }
}
