using System.Collections.Generic;
using PlunkAndPlunder.Combat;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// UI for collecting encounter decisions when ships meet
    /// Context-aware: shows PROCEED/ATTACK for PASSING encounters, YIELD/ATTACK for ENTRY encounters
    /// </summary>
    public class EncounterUI : MonoBehaviour
    {
        private GameObject modalPanel;
        private Text titleText;
        private Text descriptionText;
        private GameObject buttonContainer;
        private Dictionary<string, Button> primaryButtons; // PROCEED or YIELD
        private Dictionary<string, Button> attackButtons;
        private Dictionary<string, Text> statusTexts;

        private List<Encounter> pendingEncounters;
        private Dictionary<string, Encounter> unitToEncounter; // Map unitId -> encounter
        private int localPlayerId = 0; // Will be set by GameManager

        public void Initialize(int playerId)
        {
            Debug.Log($"[EncounterUI] Initializing for player {playerId}");

            localPlayerId = playerId;
            primaryButtons = new Dictionary<string, Button>();
            attackButtons = new Dictionary<string, Button>();
            statusTexts = new Dictionary<string, Text>();
            unitToEncounter = new Dictionary<string, Encounter>();

            // Only create modal if it doesn't exist
            if (modalPanel == null)
            {
                CreateModal();
            }

            HideModal();
        }

        private void CreateModal()
        {
            Debug.Log("[EncounterUI] Creating modal UI");

            // Create modal background (semi-transparent overlay)
            modalPanel = new GameObject("EncounterModal");
            modalPanel.transform.SetParent(transform, false);

            RectTransform panelRect = modalPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = modalPanel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.8f);

            // Create dialog box
            GameObject dialog = CreatePanel(new Vector2(0, 0), new Vector2(700, 500), new Color(0.15f, 0.15f, 0.15f, 0.95f));
            dialog.transform.SetParent(modalPanel.transform, false);

            // Title
            titleText = CreateText("ENCOUNTER DETECTED!", 36, dialog.transform);
            titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
            titleText.color = new Color(1f, 0.8f, 0f); // Warning yellow

            // Description
            descriptionText = CreateText("Ships meeting at sea - choose your action", 20, dialog.transform);
            descriptionText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140);
            descriptionText.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 60);

            // Button container for dynamic buttons
            buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(dialog.transform, false);
            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchoredPosition = new Vector2(0, -20);
            containerRect.sizeDelta = new Vector2(650, 350);
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

        private Button CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick, Color color)
        {
            GameObject buttonObj = new GameObject($"Button_{label}");
            buttonObj.transform.SetParent(buttonContainer.transform, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 40);
            rect.anchoredPosition = position;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = color;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        public void ShowEncounters(List<Encounter> encounters)
        {
            if (encounters == null || encounters.Count == 0)
            {
                Debug.LogWarning("[EncounterUI] ShowEncounters called with null or empty encounters list");
                HideModal();
                return;
            }

            pendingEncounters = encounters;
            unitToEncounter.Clear();
            ClearButtons();

            Debug.Log($"[EncounterUI] ShowEncounters called with {encounters.Count} encounter(s)");

            // Find all units that belong to the local player
            List<string> localPlayerUnits = new List<string>();
            UnitManager unitManager = GameManager.Instance.state.unitManager;

            foreach (Encounter encounter in encounters)
            {
                Debug.Log($"[EncounterUI] {encounter.Type} encounter with {encounter.InvolvedUnitIds.Count} units");

                foreach (string unitId in encounter.InvolvedUnitIds)
                {
                    Unit unit = unitManager.GetUnit(unitId);
                    if (unit != null)
                    {
                        Debug.Log($"[EncounterUI] Unit {unitId} ownerId={unit.ownerId}, localPlayerId={localPlayerId}");

                        if (unit.ownerId == localPlayerId)
                        {
                            if (!localPlayerUnits.Contains(unitId))
                            {
                                localPlayerUnits.Add(unitId);
                                unitToEncounter[unitId] = encounter;
                                Debug.Log($"[EncounterUI] Added local player unit: {unitId}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[EncounterUI] Unit {unitId} not found in UnitManager");
                    }
                }
            }

            Debug.Log($"[EncounterUI] Found {localPlayerUnits.Count} local player units in encounters");

            // If no local player units, don't show UI
            if (localPlayerUnits.Count == 0)
            {
                Debug.Log("[EncounterUI] No local player units in encounter, hiding UI");
                HideModal();
                return;
            }

            // Create buttons for each local player unit
            float yOffset = 150f;
            foreach (string unitId in localPlayerUnits)
            {
                Unit unit = unitManager.GetUnit(unitId);
                if (unit == null) continue;

                Encounter encounter = unitToEncounter[unitId];

                // Context-aware labels and description
                string encounterTypeLabel = encounter.Type == EncounterType.PASSING ? "PASSING" : "ENTRY";
                string primaryButtonLabel = encounter.Type == EncounterType.PASSING ? "PROCEED" : "YIELD";
                string locationLabel = GetLocationLabel(encounter);

                // Unit info text
                Text unitText = CreateText($"{encounterTypeLabel} ENCOUNTER\nShip {unitId} at {locationLabel}\nHP: {unit.health}/{unit.maxHealth}",
                    18, buttonContainer.transform);
                unitText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yOffset);
                unitText.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);
                unitText.alignment = TextAnchor.MiddleLeft;

                // Primary button (PROCEED for PASSING, YIELD for ENTRY)
                Button primaryBtn = CreateButton(primaryButtonLabel, new Vector2(-220, yOffset - 50),
                    () => OnPrimaryDecision(unitId, encounter), new Color(0.3f, 0.7f, 0.5f));
                primaryButtons[unitId] = primaryBtn;

                // Attack button (same for both types)
                Button attackBtn = CreateButton("ATTACK", new Vector2(-50, yOffset - 50),
                    () => OnAttackDecision(unitId, encounter), new Color(0.7f, 0.3f, 0.3f));
                attackButtons[unitId] = attackBtn;

                // Status text
                Text statusText = CreateText("", 16, buttonContainer.transform);
                statusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, yOffset - 50);
                statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
                statusText.color = Color.yellow;
                statusTexts[unitId] = statusText;

                yOffset -= 120f;
            }

            Debug.Log($"[EncounterUI] Showing modal with {primaryButtons.Count} units");
            modalPanel.SetActive(true);
        }

        private string GetLocationLabel(Encounter encounter)
        {
            if (encounter.Type == EncounterType.ENTRY && encounter.TileCoord.HasValue)
            {
                return $"tile {encounter.TileCoord.Value}";
            }
            else if (encounter.Type == EncounterType.PASSING && encounter.EdgeCoords.HasValue)
            {
                var edge = encounter.EdgeCoords.Value;
                return $"{edge.Item1} <-> {edge.Item2}";
            }
            return "unknown";
        }

        private void OnPrimaryDecision(string unitId, Encounter encounter)
        {
            string decision = encounter.Type == EncounterType.PASSING ? "Proceeding" : "Yielding";
            statusTexts[unitId].text = decision;
            statusTexts[unitId].color = Color.green;

            // Disable buttons for this unit
            primaryButtons[unitId].interactable = false;
            attackButtons[unitId].interactable = false;

            // Submit decision to GameManager (isAttacking = false)
            GameManager.Instance.SubmitEncounterDecision(unitId, encounter, isAttacking: false);

            Debug.Log($"[EncounterUI] Unit {unitId} decided {decision} for {encounter.Type} encounter");

            // Check if all local player decisions are made
            if (CheckIfAllLocalDecisionsMade())
            {
                HideModal();
            }
        }

        private void OnAttackDecision(string unitId, Encounter encounter)
        {
            statusTexts[unitId].text = "Attacking";
            statusTexts[unitId].color = Color.red;

            // Disable buttons for this unit
            primaryButtons[unitId].interactable = false;
            attackButtons[unitId].interactable = false;

            // Submit decision to GameManager (isAttacking = true)
            GameManager.Instance.SubmitEncounterDecision(unitId, encounter, isAttacking: true);

            Debug.Log($"[EncounterUI] Unit {unitId} decided ATTACK for {encounter.Type} encounter");

            // Check if all local player decisions are made
            if (CheckIfAllLocalDecisionsMade())
            {
                HideModal();
            }
        }

        private bool CheckIfAllLocalDecisionsMade()
        {
            // Count how many local player units have made decisions
            int decisionsMade = 0;
            foreach (var kvp in unitToEncounter)
            {
                string unitId = kvp.Key;
                Encounter encounter = kvp.Value;

                bool hasMadeDecision = false;
                if (encounter.Type == EncounterType.PASSING)
                {
                    hasMadeDecision = encounter.PassingDecisions.ContainsKey(unitId) &&
                                      encounter.PassingDecisions[unitId] != PassingEncounterDecision.NONE;
                }
                else if (encounter.Type == EncounterType.ENTRY)
                {
                    hasMadeDecision = encounter.EntryDecisions.ContainsKey(unitId) &&
                                      encounter.EntryDecisions[unitId] != EntryEncounterDecision.NONE;
                }

                if (hasMadeDecision)
                {
                    decisionsMade++;
                }
            }

            Debug.Log($"[EncounterUI] Decisions made: {decisionsMade}/{unitToEncounter.Count}");
            return decisionsMade == unitToEncounter.Count;
        }

        private void ClearButtons()
        {
            // Destroy all children of button container
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in buttonContainer.transform)
            {
                toDestroy.Add(child.gameObject);
            }

            foreach (GameObject obj in toDestroy)
            {
                DestroyImmediate(obj);
            }

            primaryButtons.Clear();
            attackButtons.Clear();
            statusTexts.Clear();

            Debug.Log("[EncounterUI] Cleared all buttons and UI elements");
        }

        private void HideModal()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }
    }
}
