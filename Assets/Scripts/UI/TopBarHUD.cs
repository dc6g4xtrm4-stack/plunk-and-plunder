using PlunkAndPlunder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Top bar HUD displaying turn number, phase, resources, and Pass Turn button.
    /// Positioned at top-center with full-width layout.
    /// </summary>
    public class TopBarHUD : MonoBehaviour
    {
        // UI Elements
        private Text turnText;
        private Text phaseText;
        private Text resourceText;
        private Button passTurnButton;
        private Text passTurnButtonText;

        // Button pulsing
        private ButtonPulse buttonPulse;

        public void Initialize()
        {
            BuildTopBar();
        }

        private void BuildTopBar()
        {
            // Setup RectTransform for top-center positioning
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // Anchor to top-center, full-width
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, HUDStyles.TopBarHeight);

            // Add background
            Image bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = HUDStyles.BackgroundColor;

            // Add border
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = HUDStyles.BorderColor;
            outline.effectDistance = new Vector2(2, -2);

            // Create turn text (left side)
            GameObject turnTextObj = new GameObject("TurnText", typeof(RectTransform));
            turnTextObj.transform.SetParent(transform, false);

            turnText = turnTextObj.AddComponent<Text>();
            turnText.text = "Turn: 0";
            turnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            turnText.fontSize = HUDStyles.LargeFontSize;
            turnText.color = HUDStyles.TextColor;
            turnText.alignment = TextAnchor.MiddleLeft;
            turnText.fontStyle = FontStyle.Bold;

            RectTransform turnRT = turnTextObj.GetComponent<RectTransform>();
            turnRT.anchorMin = new Vector2(0, 0);
            turnRT.anchorMax = new Vector2(0.25f, 1);
            turnRT.offsetMin = new Vector2(20, 0);
            turnRT.offsetMax = new Vector2(-10, 0);

            // Create phase text (center-left)
            GameObject phaseTextObj = new GameObject("PhaseText", typeof(RectTransform));
            phaseTextObj.transform.SetParent(transform, false);

            phaseText = phaseTextObj.AddComponent<Text>();
            phaseText.text = "Phase: Planning";
            phaseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phaseText.fontSize = HUDStyles.LargeFontSize;
            phaseText.color = HUDStyles.TextColor;
            phaseText.alignment = TextAnchor.MiddleCenter;
            phaseText.fontStyle = FontStyle.Bold;

            RectTransform phaseRT = phaseTextObj.GetComponent<RectTransform>();
            phaseRT.anchorMin = new Vector2(0.25f, 0);
            phaseRT.anchorMax = new Vector2(0.5f, 1);
            phaseRT.offsetMin = new Vector2(10, 0);
            phaseRT.offsetMax = new Vector2(-10, 0);

            // Create resource text (center-right)
            GameObject resourceTextObj = new GameObject("ResourceText", typeof(RectTransform));
            resourceTextObj.transform.SetParent(transform, false);

            resourceText = resourceTextObj.AddComponent<Text>();
            resourceText.text = "Gold: 0 | Orders: 0";
            resourceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resourceText.fontSize = HUDStyles.ContentFontSize;
            resourceText.color = HUDStyles.TextColor;
            resourceText.alignment = TextAnchor.MiddleCenter;

            RectTransform resourceRT = resourceTextObj.GetComponent<RectTransform>();
            resourceRT.anchorMin = new Vector2(0.5f, 0);
            resourceRT.anchorMax = new Vector2(0.7f, 1);
            resourceRT.offsetMin = new Vector2(10, 0);
            resourceRT.offsetMax = new Vector2(-10, 0);

            // Create Pass Turn button (right side)
            GameObject buttonObj = new GameObject("PassTurnButton", typeof(RectTransform));
            buttonObj.transform.SetParent(transform, false);

            RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(1, 0.5f);
            buttonRT.anchorMax = new Vector2(1, 0.5f);
            buttonRT.pivot = new Vector2(1, 0.5f);
            buttonRT.anchoredPosition = new Vector2(-20, 0);
            buttonRT.sizeDelta = new Vector2(200, 60);

            passTurnButton = buttonObj.AddComponent<Button>();
            passTurnButton.onClick.AddListener(OnPassTurnClicked);

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = HUDStyles.ButtonNormalColor;

            ColorBlock colors = passTurnButton.colors;
            colors.normalColor = HUDStyles.ButtonNormalColor;
            colors.highlightedColor = HUDStyles.ButtonHoverColor;
            colors.disabledColor = HUDStyles.ButtonDisabledColor;
            passTurnButton.colors = colors;
            passTurnButton.targetGraphic = buttonBg;

            // Add button pulse component
            buttonPulse = buttonObj.AddComponent<ButtonPulse>();
            buttonPulse.normalColor = new Color(0.2f, 0.4f, 0.2f);
            buttonPulse.minPulseColor = new Color(0f, 0.6f, 0f);
            buttonPulse.maxPulseColor = new Color(0f, 1f, 0f);

            // Button text
            GameObject buttonTextObj = new GameObject("Text", typeof(RectTransform));
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            passTurnButtonText = buttonTextObj.AddComponent<Text>();
            passTurnButtonText.text = "PASS TURN";
            passTurnButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            passTurnButtonText.fontSize = HUDStyles.ButtonFontSize;
            passTurnButtonText.color = HUDStyles.TextColor;
            passTurnButtonText.alignment = TextAnchor.MiddleCenter;
            passTurnButtonText.fontStyle = FontStyle.Bold;

            RectTransform buttonTextRT = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRT.anchorMin = Vector2.zero;
            buttonTextRT.anchorMax = Vector2.one;
            buttonTextRT.sizeDelta = Vector2.zero;

            Debug.Log("[TopBarHUD] Initialized at top-center");
        }

        public void UpdateTurnInfo(int turn, GamePhase phase)
        {
            if (turnText != null)
            {
                turnText.text = $"Turn: {turn}";
            }

            if (phaseText != null)
            {
                phaseText.text = $"Phase: {phase}";
            }
        }

        public void UpdateResourceInfo(int gold, int orderCount)
        {
            if (resourceText != null)
            {
                resourceText.text = $"Gold: {gold} | Orders: {orderCount}";
            }
        }

        public void SetPassTurnInteractable(bool interactable)
        {
            if (passTurnButton != null)
            {
                passTurnButton.interactable = interactable;
            }
        }

        public void SetPassTurnPulsing(bool pulsing)
        {
            if (buttonPulse != null)
            {
                buttonPulse.SetPulsing(pulsing);
            }
        }

        private void OnPassTurnClicked()
        {
            Debug.Log("🔘 [TopBarHUD] ========== PASS TURN BUTTON CLICKED! ==========");

            // Find GameHUD and trigger submit
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            if (gameHUD != null)
            {
                Debug.Log("[TopBarHUD] GameHUD found, sending OnPassTurnClicked message...");
                gameHUD.SendMessage("OnPassTurnClicked", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[TopBarHUD] Message sent to GameHUD");
            }
            else
            {
                Debug.LogError("[TopBarHUD] ❌ GameHUD NOT FOUND! Cannot submit orders.");
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
