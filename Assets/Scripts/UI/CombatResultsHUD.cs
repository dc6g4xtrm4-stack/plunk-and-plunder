using PlunkAndPlunder.Core;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Displays combat results with clear visual feedback.
    /// Shows health bars, damage dealt, and outcome.
    /// Deterministic combat - no dice, just pure strategy.
    /// </summary>
    public class CombatResultsHUD : MonoBehaviour
    {
        private GameObject panel;
        private Text titleText;
        private Text attackerNameText;
        private Text defenderNameText;
        private Slider attackerHealthBar;
        private Slider defenderHealthBar;
        private Text attackerDamageText;
        private Text defenderDamageText;
        private Text resultText;
        private Button continueButton;

        private float autoHideDelay = 3f;
        private float displayStartTime;
        private bool isShowing = false;

        public void Initialize()
        {
            CreateUI();
            Hide();
        }

        private void CreateUI()
        {
            // Create modal panel
            panel = new GameObject("CombatResultsPanel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 400);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            titleText = CreateText("⚔️ COMBAT RESULTS ⚔️", 28, panel.transform);
            titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 160);
            titleText.color = new Color(1f, 0.9f, 0.3f);

            // Attacker section
            attackerNameText = CreateText("Ship A", 20, panel.transform);
            attackerNameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

            attackerHealthBar = CreateHealthBar(panel.transform, new Vector2(0, 70));

            attackerDamageText = CreateText("-X damage", 18, panel.transform);
            attackerDamageText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
            attackerDamageText.color = new Color(1f, 0.3f, 0.3f);

            // VS separator
            Text vsText = CreateText("⚔️", 32, panel.transform);
            vsText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            vsText.color = Color.white;

            // Defender section
            defenderNameText = CreateText("Ship B", 20, panel.transform);
            defenderNameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);

            defenderHealthBar = CreateHealthBar(panel.transform, new Vector2(0, -70));

            defenderDamageText = CreateText("-X damage", 18, panel.transform);
            defenderDamageText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
            defenderDamageText.color = new Color(1f, 0.3f, 0.3f);

            // Result summary
            resultText = CreateText("Result", 16, panel.transform);
            resultText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);
            resultText.GetComponent<RectTransform>().sizeDelta = new Vector2(450, 40);

            // Continue button
            continueButton = CreateButton("CONTINUE", new Vector2(0, -180), OnContinueClicked);
        }

        private Text CreateText(string text, int fontSize, Transform parent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 40);

            return textComp;
        }

        private Slider CreateHealthBar(Transform parent, Vector2 position)
        {
            GameObject sliderObj = new GameObject("HealthBar");
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(350, 20);

            Slider slider = sliderObj.AddComponent<Slider>();

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.3f); // Green health bar
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;
            slider.interactable = false;

            return slider;
        }

        private Button CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(panel.transform, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);
            rect.anchoredPosition = position;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.6f, 0.8f);

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

        public void ShowCombatResult(CombatOccurredEvent combatEvent, GameState state)
        {
            Unit attacker = state.unitManager.GetUnit(combatEvent.attackerId);
            Unit defender = state.unitManager.GetUnit(combatEvent.defenderId);

            if (attacker == null || defender == null)
            {
                Debug.LogWarning("[CombatResultsHUD] Cannot show combat - unit not found");
                return;
            }

            // Set ship names
            attackerNameText.text = attacker.GetDisplayName(state.playerManager);
            defenderNameText.text = defender.GetDisplayName(state.playerManager);

            // Set health bars (current health / max health)
            // Color based on health percentage
            float attackerHealthPercent = (float)attacker.health / attacker.maxHealth;
            float defenderHealthPercent = (float)defender.health / defender.maxHealth;

            attackerHealthBar.value = attackerHealthPercent;
            defenderHealthBar.value = defenderHealthPercent;

            // Color health bars based on percentage
            attackerHealthBar.fillRect.GetComponent<Image>().color = GetHealthColor(attackerHealthPercent);
            defenderHealthBar.fillRect.GetComponent<Image>().color = GetHealthColor(defenderHealthPercent);

            // Set damage text
            attackerDamageText.text = $"-{combatEvent.damageToAttacker} HP  ({attacker.health}/{attacker.maxHealth})";
            defenderDamageText.text = $"-{combatEvent.damageToDefender} HP  ({defender.health}/{defender.maxHealth})";

            // Set result text
            if (combatEvent.attackerDestroyed && combatEvent.defenderDestroyed)
            {
                resultText.text = "Both ships destroyed!";
                resultText.color = new Color(1f, 0.5f, 0);
            }
            else if (combatEvent.attackerDestroyed)
            {
                resultText.text = $"{attacker.GetDisplayName(state.playerManager)} destroyed!";
                resultText.color = new Color(1f, 0.3f, 0.3f);
            }
            else if (combatEvent.defenderDestroyed)
            {
                resultText.text = $"{defender.GetDisplayName(state.playerManager)} destroyed!";
                resultText.color = new Color(1f, 0.3f, 0.3f);
            }
            else
            {
                resultText.text = "Both ships damaged - battle continues";
                resultText.color = new Color(1f, 0.9f, 0.3f);
            }

            panel.SetActive(true);
            isShowing = true;
            displayStartTime = Time.time;

            Debug.Log($"[CombatResultsHUD] Showing combat: {attacker.id} vs {defender.id}");
        }

        /// <summary>
        /// Get health bar color based on health percentage.
        /// </summary>
        private Color GetHealthColor(float healthPercent)
        {
            if (healthPercent > 0.7f)
                return new Color(0.2f, 0.8f, 0.3f); // Green
            else if (healthPercent > 0.3f)
                return new Color(1f, 0.9f, 0.2f); // Yellow
            else if (healthPercent > 0f)
                return new Color(1f, 0.3f, 0.3f); // Red
            else
                return new Color(0.5f, 0.5f, 0.5f); // Gray (destroyed)
        }

        private void Update()
        {
            if (isShowing && Time.time - displayStartTime > autoHideDelay)
            {
                Hide();
            }
        }

        private void OnContinueClicked()
        {
            Hide();
        }

        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
            isShowing = false;
        }
    }
}
