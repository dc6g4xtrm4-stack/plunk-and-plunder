using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// UI for collecting yield decisions when ships collide
    /// </summary>
    public class CollisionYieldUI : MonoBehaviour
    {
        private GameObject modalPanel;
        private Text titleText;
        private Text descriptionText;
        private GameObject buttonContainer;
        private Dictionary<string, Button> yieldButtons;
        private Dictionary<string, Button> pushButtons;
        private Dictionary<string, Text> statusTexts;

        private List<CollisionInfo> pendingCollisions;
        private Dictionary<string, bool> yieldDecisions;
        private int localPlayerId = 0; // Will be set by GameManager

        public void Initialize(int playerId)
        {
            Debug.Log($"[CollisionYieldUI] Initializing for player {playerId}");

            localPlayerId = playerId;
            yieldButtons = new Dictionary<string, Button>();
            pushButtons = new Dictionary<string, Button>();
            statusTexts = new Dictionary<string, Text>();
            yieldDecisions = new Dictionary<string, bool>();

            // Only create modal if it doesn't exist
            if (modalPanel == null)
            {
                CreateModal();
            }

            HideModal();
        }

        private void CreateModal()
        {
            Debug.Log("[CollisionYieldUI] Creating modal UI");

            // Create modal background (semi-transparent overlay)
            modalPanel = new GameObject("CollisionYieldModal");
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
            titleText = CreateText("COLLISION DETECTED!", 36, dialog.transform);
            titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
            titleText.color = new Color(1f, 0.8f, 0f); // Warning yellow

            // Description
            descriptionText = CreateText("Ships on collision course! Choose: PROCEED (pass peacefully) or ATTACK (combat)", 20, dialog.transform);
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

        public void ShowCollisions(List<CollisionInfo> collisions)
        {
            if (collisions == null || collisions.Count == 0)
            {
                Debug.LogWarning("[CollisionYieldUI] ShowCollisions called with null or empty collisions list");
                HideModal();
                return;
            }

            pendingCollisions = collisions;
            yieldDecisions.Clear();
            ClearButtons();

            Debug.Log($"[CollisionYieldUI] ShowCollisions called with {collisions.Count} collision(s)");

            // Find all units that belong to the local player
            List<string> localPlayerUnits = new List<string>();
            UnitManager unitManager = GameManager.Instance.state.unitManager;

            foreach (CollisionInfo collision in collisions)
            {
                Debug.Log($"[CollisionYieldUI] Collision at {collision.destination} with {collision.unitIds.Count} units: {string.Join(", ", collision.unitIds)}");

                foreach (string unitId in collision.unitIds)
                {
                    Unit unit = unitManager.GetUnit(unitId);
                    if (unit != null)
                    {
                        Debug.Log($"[CollisionYieldUI] Unit {unitId} ownerId={unit.ownerId}, localPlayerId={localPlayerId}");

                        if (unit.ownerId == localPlayerId)
                        {
                            if (!localPlayerUnits.Contains(unitId))
                            {
                                localPlayerUnits.Add(unitId);
                                Debug.Log($"[CollisionYieldUI] Added local player unit: {unitId}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CollisionYieldUI] Unit {unitId} not found in UnitManager");
                    }
                }
            }

            Debug.Log($"[CollisionYieldUI] Found {localPlayerUnits.Count} local player units in collisions");

            // If no local player units, don't show UI
            if (localPlayerUnits.Count == 0)
            {
                Debug.Log("[CollisionYieldUI] No local player units in collision, hiding UI");
                HideModal();
                return;
            }

            // Create buttons for each local player unit
            float yOffset = 150f;
            foreach (string unitId in localPlayerUnits)
            {
                Unit unit = unitManager.GetUnit(unitId);
                if (unit == null) continue;

                // Find which collision this unit is in
                HexCoord collisionPos = new HexCoord(0, 0);
                int otherUnitsCount = 0;
                foreach (CollisionInfo collision in collisions)
                {
                    if (collision.unitIds.Contains(unitId))
                    {
                        collisionPos = collision.destination;
                        otherUnitsCount = collision.unitIds.Count - 1;
                        break;
                    }
                }

                // Unit info text
                Text unitText = CreateText($"Ship {unitId} at {collisionPos}\nHP: {unit.health}/{unit.maxHealth} | Colliding with {otherUnitsCount} other ship(s)",
                    18, buttonContainer.transform);
                unitText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yOffset);
                unitText.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 50);
                unitText.alignment = TextAnchor.MiddleLeft;

                // Proceed button (let them pass / yield)
                Button yieldBtn = CreateButton("PROCEED", new Vector2(-220, yOffset - 40), () => OnYieldClicked(unitId), new Color(0.3f, 0.7f, 0.5f));
                yieldButtons[unitId] = yieldBtn;

                // Attack button (fight for position)
                Button pushBtn = CreateButton("ATTACK", new Vector2(-50, yOffset - 40), () => OnPushClicked(unitId), new Color(0.7f, 0.3f, 0.3f));
                pushButtons[unitId] = pushBtn;

                // Status text
                Text statusText = CreateText("", 16, buttonContainer.transform);
                statusText.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, yOffset - 40);
                statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
                statusText.color = Color.yellow;
                statusTexts[unitId] = statusText;

                yOffset -= 100f;
            }

            Debug.Log($"[CollisionYieldUI] Showing modal with {yieldButtons.Count} units");
            modalPanel.SetActive(true);
        }

        private void OnYieldClicked(string unitId)
        {
            yieldDecisions[unitId] = true;
            statusTexts[unitId].text = "Proceeding";
            statusTexts[unitId].color = Color.green;

            // Disable buttons for this unit
            yieldButtons[unitId].interactable = false;
            pushButtons[unitId].interactable = false;

            // Submit decision to GameManager
            GameManager.Instance.SubmitYieldDecision(unitId, true);

            // Check if all local player decisions are made
            if (AllLocalDecisionsMade())
            {
                HideModal();
            }
        }

        private void OnPushClicked(string unitId)
        {
            yieldDecisions[unitId] = false;
            statusTexts[unitId].text = "Attacking";
            statusTexts[unitId].color = Color.red;

            // Disable buttons for this unit
            yieldButtons[unitId].interactable = false;
            pushButtons[unitId].interactable = false;

            // Submit decision to GameManager
            GameManager.Instance.SubmitYieldDecision(unitId, false);

            // Check if all local player decisions are made
            if (AllLocalDecisionsMade())
            {
                HideModal();
            }
        }

        private bool AllLocalDecisionsMade()
        {
            return yieldDecisions.Count == yieldButtons.Count;
        }

        private void ClearButtons()
        {
            // Destroy all children of button container
            // Use a list to avoid modifying collection while iterating
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in buttonContainer.transform)
            {
                toDestroy.Add(child.gameObject);
            }

            foreach (GameObject obj in toDestroy)
            {
                DestroyImmediate(obj);
            }

            yieldButtons.Clear();
            pushButtons.Clear();
            statusTexts.Clear();

            Debug.Log("[CollisionYieldUI] Cleared all buttons and UI elements");
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
