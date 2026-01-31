using System;
using System.Collections;
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
    /// Visual dice rolling UI for combat with shaking animation
    /// </summary>
    public class DiceCombatUI : MonoBehaviour
    {
        private GameObject modalPanel;
        private Text titleText;
        private Text roundNumberText;
        private GameObject attackerPanel;
        private GameObject defenderPanel;
        private Text attackerNameText;
        private Text defenderNameText;
        private Text attackerHealthText;
        private Text defenderHealthText;
        private GameObject attackerDiceContainer;
        private GameObject defenderDiceContainer;
        private Text resultText;
        private Text combatContinuesText;
        private Button continueButton;

        private List<GameObject> attackerDiceObjects = new List<GameObject>();
        private List<GameObject> defenderDiceObjects = new List<GameObject>();

        private Action onContinueCallback;
        private bool isShaking = false;

        // Combat location indicator
        private GameObject combatIndicator;
        private HexCoord combatLocation;

        // Use simple numbers instead of Unicode (better compatibility)
        private readonly string[] diceFaces = new string[] { "1", "2", "3", "4", "5", "6" };

        public void Initialize()
        {
            CreateModal();
            HideModal();
        }

        private void CreateModal()
        {
            // Create top bar overlay (not full-screen modal)
            modalPanel = new GameObject("DiceCombatModal");
            modalPanel.transform.SetParent(transform, false);

            RectTransform panelRect = modalPanel.AddComponent<RectTransform>();
            // Anchor to top of screen
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(0, 200); // 200 pixels tall
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = modalPanel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.8f);

            // Create dialog box - centered horizontally in the top bar
            GameObject dialog = CreatePanel(new Vector2(0, -100), new Vector2(1400, 180), new Color(0.1f, 0.1f, 0.15f, 0.95f));
            dialog.transform.SetParent(modalPanel.transform, false);

            // Title and Round number combined (left side)
            titleText = CreateText("COMBAT - Round 1", 28, dialog.transform);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(-580, 70);
            titleRect.sizeDelta = new Vector2(200, 40);
            titleText.color = new Color(1f, 0.2f, 0.2f);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;

            // Round number (hidden, will be part of title)
            roundNumberText = CreateText("Round 1", 28, dialog.transform);
            roundNumberText.gameObject.SetActive(false); // Not displayed separately

            // Attacker panel (left side, compact)
            attackerPanel = CreatePanel(new Vector2(-380, 0), new Vector2(280, 160), new Color(0.2f, 0.3f, 0.5f, 0.5f));
            attackerPanel.transform.SetParent(dialog.transform, false);

            attackerNameText = CreateText("Attacker", 20, attackerPanel.transform);
            RectTransform attackerNameRect = attackerNameText.GetComponent<RectTransform>();
            attackerNameRect.anchoredPosition = new Vector2(0, 55);
            attackerNameRect.sizeDelta = new Vector2(260, 30);
            attackerNameText.color = new Color(0.5f, 0.8f, 1f);
            attackerNameText.fontStyle = FontStyle.Bold;

            attackerHealthText = CreateText("HP: 10/10", 18, attackerPanel.transform);
            RectTransform attackerHealthRect = attackerHealthText.GetComponent<RectTransform>();
            attackerHealthRect.anchoredPosition = new Vector2(0, 30);
            attackerHealthRect.sizeDelta = new Vector2(260, 25);

            attackerDiceContainer = new GameObject("AttackerDiceContainer");
            attackerDiceContainer.transform.SetParent(attackerPanel.transform, false);
            RectTransform attackerDiceRect = attackerDiceContainer.AddComponent<RectTransform>();
            attackerDiceRect.anchoredPosition = new Vector2(0, -25);
            attackerDiceRect.sizeDelta = new Vector2(260, 100);

            // Defender panel (right side, compact)
            defenderPanel = CreatePanel(new Vector2(-70, 0), new Vector2(280, 160), new Color(0.5f, 0.2f, 0.2f, 0.5f));
            defenderPanel.transform.SetParent(dialog.transform, false);

            defenderNameText = CreateText("Defender", 20, defenderPanel.transform);
            RectTransform defenderNameRect = defenderNameText.GetComponent<RectTransform>();
            defenderNameRect.anchoredPosition = new Vector2(0, 55);
            defenderNameRect.sizeDelta = new Vector2(260, 30);
            defenderNameText.color = new Color(1f, 0.5f, 0.5f);
            defenderNameText.fontStyle = FontStyle.Bold;

            defenderHealthText = CreateText("HP: 10/10", 18, defenderPanel.transform);
            RectTransform defenderHealthRect = defenderHealthText.GetComponent<RectTransform>();
            defenderHealthRect.anchoredPosition = new Vector2(0, 30);
            defenderHealthRect.sizeDelta = new Vector2(260, 25);

            defenderDiceContainer = new GameObject("DefenderDiceContainer");
            defenderDiceContainer.transform.SetParent(defenderPanel.transform, false);
            RectTransform defenderDiceRect = defenderDiceContainer.AddComponent<RectTransform>();
            defenderDiceRect.anchoredPosition = new Vector2(0, -25);
            defenderDiceRect.sizeDelta = new Vector2(260, 100);

            // Result text (right side)
            resultText = CreateText("", 20, dialog.transform);
            RectTransform resultRect = resultText.GetComponent<RectTransform>();
            resultRect.anchoredPosition = new Vector2(320, 0);
            resultRect.sizeDelta = new Vector2(400, 120);
            resultText.color = new Color(1f, 1f, 0.3f);
            resultText.fontStyle = FontStyle.Bold;

            // Combat continues text (right side, below result)
            combatContinuesText = CreateText("Combat continues next turn", 14, dialog.transform);
            RectTransform continuesRect = combatContinuesText.GetComponent<RectTransform>();
            continuesRect.anchoredPosition = new Vector2(320, -55);
            continuesRect.sizeDelta = new Vector2(400, 30);
            combatContinuesText.color = new Color(1f, 0.8f, 0.2f);

            // Continue button (right side)
            continueButton = CreateButton("Continue", new Vector2(580, 0), OnContinueClicked);
            continueButton.transform.SetParent(dialog.transform, false);
            RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(140, 50);
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
            bg.color = new Color(0.2f, 0.6f, 0.3f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
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

        public void ShowCombat(CombatOccurredEvent combatEvent, Unit attacker, Unit defender, int roundNumber, Action callback, PlunkAndPlunder.Players.PlayerManager playerManager = null)
        {
            Debug.Log($"[DiceCombatUI] ShowCombat called - Round {roundNumber}: {attacker.id} vs {defender.id}");
            onContinueCallback = callback;

            // Store combat location and show indicator
            combatLocation = attacker.position; // Both units are at the same location during combat
            ShowCombatIndicator();

            // Update title with round number
            titleText.text = $"COMBAT - Round {roundNumber}";

            // Update ship names and health using display names
            string attackerDisplayName = attacker.GetDisplayName(playerManager);
            string defenderDisplayName = defender.GetDisplayName(playerManager);

            attackerNameText.text = attackerDisplayName;
            defenderNameText.text = defenderDisplayName;

            attackerHealthText.text = $"HP: {attacker.health}/{attacker.maxHealth}";
            defenderHealthText.text = $"HP: {defender.health}/{defender.maxHealth}";

            // Show/hide combat continues message based on whether both ships survive
            if (combatContinuesText != null)
            {
                bool bothSurvive = !combatEvent.attackerDestroyed && !combatEvent.defenderDestroyed;
                combatContinuesText.gameObject.SetActive(bothSurvive);
            }

            // Start the dice rolling animation
            StartCoroutine(AnimateDiceRolls(combatEvent));

            modalPanel.SetActive(true);
        }

        private IEnumerator AnimateDiceRolls(CombatOccurredEvent combatEvent)
        {
            // Clear old dice
            ClearDice();

            // Disable continue button during animation
            continueButton.interactable = false;
            resultText.text = "Rolling dice...";

            // Create dice objects (initially transparent)
            CreateDiceObjects(attackerDiceContainer, 3, attackerDiceObjects);
            CreateDiceObjects(defenderDiceContainer, 2, defenderDiceObjects);

            // Smooth fade-in and pulse animation (1.2 seconds total)
            float animationDuration = 1.2f;
            isShaking = true;

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                float progress = elapsed / animationDuration;

                // Smooth fade-in using ease-out curve
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(progress * 2f, 1f));

                // Gentle pulse effect (scale oscillation)
                float pulseSpeed = 8f; // Cycles per second
                float pulseAmount = 0.1f; // 10% size variation
                float scale = 1f + pulseAmount * Mathf.Sin(progress * Mathf.PI * 2f * pulseSpeed) * (1f - progress * 0.5f); // Dampen pulse over time

                // Cycle through dice faces smoothly (slower than jittery version)
                int cycleSpeed = 8; // Lower = slower cycling
                int currentFrame = Mathf.FloorToInt(progress * animationDuration * cycleSpeed);

                // Apply fade and pulse to attacker dice
                for (int i = 0; i < attackerDiceObjects.Count; i++)
                {
                    GameObject diceObj = attackerDiceObjects[i];
                    Image diceImage = diceObj.GetComponent<Image>();
                    if (diceImage != null)
                    {
                        Color imageColor = diceImage.color;
                        imageColor.a = alpha;
                        diceImage.color = imageColor;
                    }

                    diceObj.transform.localScale = Vector3.one * scale;

                    Text diceText = diceObj.GetComponentInChildren<Text>();
                    if (diceText != null)
                    {
                        // Cycle through dice faces (use frame-based instead of fully random for smoother appearance)
                        int faceValue = ((currentFrame + i) % 6) + 1;
                        diceText.text = GetDiceDots(faceValue);

                        Color textColor = diceText.color;
                        textColor.a = alpha;
                        diceText.color = textColor;
                    }
                }

                // Apply fade and pulse to defender dice
                for (int i = 0; i < defenderDiceObjects.Count; i++)
                {
                    GameObject diceObj = defenderDiceObjects[i];
                    Image diceImage = diceObj.GetComponent<Image>();
                    if (diceImage != null)
                    {
                        Color imageColor = diceImage.color;
                        imageColor.a = alpha;
                        diceImage.color = imageColor;
                    }

                    diceObj.transform.localScale = Vector3.one * scale;

                    Text diceText = diceObj.GetComponentInChildren<Text>();
                    if (diceText != null)
                    {
                        // Cycle through dice faces (offset from attacker for variety)
                        int faceValue = ((currentFrame + i + 3) % 6) + 1;
                        diceText.text = GetDiceDots(faceValue);

                        Color textColor = diceText.color;
                        textColor.a = alpha;
                        diceText.color = textColor;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure final state is fully opaque and normal scale
            foreach (GameObject diceObj in attackerDiceObjects)
            {
                Image diceImage = diceObj.GetComponent<Image>();
                if (diceImage != null)
                {
                    Color imageColor = diceImage.color;
                    imageColor.a = 1f;
                    diceImage.color = imageColor;
                }
                diceObj.transform.localScale = Vector3.one;

                Text diceText = diceObj.GetComponentInChildren<Text>();
                if (diceText != null)
                {
                    Color textColor = diceText.color;
                    textColor.a = 1f;
                    diceText.color = textColor;
                }
            }

            foreach (GameObject diceObj in defenderDiceObjects)
            {
                Image diceImage = diceObj.GetComponent<Image>();
                if (diceImage != null)
                {
                    Color imageColor = diceImage.color;
                    imageColor.a = 1f;
                    diceImage.color = imageColor;
                }
                diceObj.transform.localScale = Vector3.one;

                Text diceText = diceObj.GetComponentInChildren<Text>();
                if (diceText != null)
                {
                    Color textColor = diceText.color;
                    textColor.a = 1f;
                    diceText.color = textColor;
                }
            }

            isShaking = false;

            // Show actual roll results
            ShowDiceResults(attackerDiceObjects, combatEvent.attackerRolls);
            ShowDiceResults(defenderDiceObjects, combatEvent.defenderRolls);

            // Wait a moment to let player see the results
            yield return new WaitForSeconds(0.5f);

            // Show damage results
            string damageText = "";
            if (combatEvent.damageToAttacker > 0 && combatEvent.damageToDefender > 0)
            {
                damageText = $"Both ships take damage!\nAttacker: -{combatEvent.damageToAttacker} HP | Defender: -{combatEvent.damageToDefender} HP";
            }
            else if (combatEvent.damageToAttacker > 0)
            {
                damageText = $"Defender wins!\nAttacker takes {combatEvent.damageToAttacker} damage";
            }
            else if (combatEvent.damageToDefender > 0)
            {
                damageText = $"Attacker wins!\nDefender takes {combatEvent.damageToDefender} damage";
            }

            if (combatEvent.attackerDestroyed)
            {
                damageText += "\n💥 ATTACKER DESTROYED! 💥";
            }
            else if (combatEvent.defenderDestroyed)
            {
                damageText += "\n💥 DEFENDER DESTROYED! 💥";
            }

            resultText.text = damageText;

            // Enable continue button
            continueButton.interactable = true;
        }

        private void CreateDiceObjects(GameObject container, int count, List<GameObject> diceList)
        {
            float spacing = 70f;
            float startX = -(count - 1) * spacing / 2f;

            for (int i = 0; i < count; i++)
            {
                GameObject diceObj = CreatePanel(new Vector2(startX + i * spacing, 0), new Vector2(60, 60), new Color(1f, 1f, 1f, 0.9f));
                diceObj.transform.SetParent(container.transform, false);

                // Add border
                Image diceImage = diceObj.GetComponent<Image>();
                diceImage.color = Color.white;

                // Create dice face text container
                Text diceText = CreateText("", 28, diceObj.transform);
                diceText.color = Color.black;
                diceText.fontStyle = FontStyle.Bold;
                diceText.lineSpacing = 0.5f; // Tighter spacing for dot patterns
                RectTransform textRect = diceText.GetComponent<RectTransform>();
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(60, 60);

                diceList.Add(diceObj);
            }
        }

        private string GetDiceDots(int value)
        {
            // Return traditional dice dot patterns using filled circles (●)
            // Using precise spacing for clean alignment
            switch (value)
            {
                case 1:
                    return "\n\n    ●";
                case 2:
                    return "●\n\n        ●";
                case 3:
                    return "●\n    ●\n        ●";
                case 4:
                    return "●       ●\n\n●       ●";
                case 5:
                    return "●       ●\n    ●\n●       ●";
                case 6:
                    return "●       ●\n●       ●\n●       ●";
                default:
                    return "?";
            }
        }

        private void ShowDiceResults(List<GameObject> diceObjects, List<int> rolls)
        {
            for (int i = 0; i < diceObjects.Count && i < rolls.Count; i++)
            {
                Text diceText = diceObjects[i].GetComponentInChildren<Text>();
                if (diceText != null)
                {
                    diceText.text = GetDiceDots(rolls[i]); // Use dot patterns instead of numbers
                }
            }
        }

        private void ClearDice()
        {
            foreach (GameObject dice in attackerDiceObjects)
            {
                if (dice != null)
                    Destroy(dice);
            }
            attackerDiceObjects.Clear();

            foreach (GameObject dice in defenderDiceObjects)
            {
                if (dice != null)
                    Destroy(dice);
            }
            defenderDiceObjects.Clear();
        }

        private void OnContinueClicked()
        {
            HideModal();
            onContinueCallback?.Invoke();
        }

        private void HideModal()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
            HideCombatIndicator();
        }

        private void ShowCombatIndicator()
        {
            // Create a pulsing red circle at the combat location
            if (combatIndicator == null)
            {
                combatIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                combatIndicator.name = "CombatIndicator";

                // Remove collider - we don't want it to interfere with mouse clicks
                Destroy(combatIndicator.GetComponent<Collider>());

                // Create pulsing red material - use existing material from primitive and modify it
                Renderer renderer = combatIndicator.GetComponent<Renderer>();

                // Try to find a suitable shader, with fallbacks
                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    // Last resort: just use the existing material and modify its color
                    Material existingMat = renderer.material;
                    existingMat.color = new Color(1f, 0f, 1f, 0.6f); // Purple/magenta for visibility
                    return;
                }

                Material mat = new Material(shader);
                mat.color = new Color(1f, 0f, 1f, 0.6f); // Purple/magenta for visibility

                // Try to set transparency if the shader supports it
                if (mat.HasProperty("_Mode"))
                {
                    mat.SetFloat("_Mode", 3); // Transparent rendering mode
                }
                if (mat.HasProperty("_SrcBlend") && mat.HasProperty("_DstBlend"))
                {
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                if (mat.HasProperty("_ZWrite"))
                {
                    mat.SetInt("_ZWrite", 0);
                }
                mat.renderQueue = 3000;
                renderer.material = mat;

                // Scale to fit hex tile
                combatIndicator.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
            }

            // Position at combat location (slightly above ground)
            Vector3 worldPos = combatLocation.ToWorldPosition(1f); // Using standard hex size of 1
            combatIndicator.transform.position = worldPos + Vector3.up * 0.3f;
            combatIndicator.SetActive(true);

            // Start pulsing animation
            StartCoroutine(PulseCombatIndicator());
        }

        private IEnumerator PulseCombatIndicator()
        {
            if (combatIndicator == null) yield break;

            Renderer renderer = combatIndicator.GetComponent<Renderer>();
            if (renderer == null) yield break;

            float time = 0f;
            while (combatIndicator != null && combatIndicator.activeSelf)
            {
                // Pulse between 0.4 and 0.8 alpha
                float alpha = 0.4f + 0.4f * (0.5f + 0.5f * Mathf.Sin(time * 3f));
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;

                // Also pulse scale slightly
                float scale = 1.5f + 0.2f * Mathf.Sin(time * 3f);
                combatIndicator.transform.localScale = new Vector3(scale, 0.1f, scale);

                time += Time.deltaTime;
                yield return null;
            }
        }

        private void HideCombatIndicator()
        {
            if (combatIndicator != null)
            {
                combatIndicator.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (combatIndicator != null)
            {
                Destroy(combatIndicator);
            }
        }
    }
}
