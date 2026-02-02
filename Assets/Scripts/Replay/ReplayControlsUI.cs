using System.IO;
using PlunkAndPlunder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.Replay
{
    /// <summary>
    /// UI controls for replay playback (play/pause, speed, progress)
    /// </summary>
    public class ReplayControlsUI : MonoBehaviour
    {
        private ReplayManager replayManager;
        private Canvas canvas;
        private Text titleText;
        private Text turnText;
        private Image progressBar;
        private Button pauseButton;
        private Text pauseButtonText;
        private Button[] speedButtons;
        private Button exitButton;

        private float currentSpeed = 1.0f;

        public void Initialize(ReplayManager manager)
        {
            replayManager = manager;
            CreateLayout();

            // Subscribe to replay events
            replayManager.OnTurnChanged += UpdateProgress;
            replayManager.OnReplayComplete += HandleReplayComplete;
            replayManager.OnStateUpdated += HandleStateUpdated;
        }

        private void CreateLayout()
        {
            // Create canvas
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // On top of everything

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            // Create background panel
            GameObject panel = CreatePanel();

            // Title
            titleText = CreateText(panel, new Vector2(0, 100), "REPLAY MODE", 24);
            titleText.alignment = TextAnchor.MiddleCenter;

            // Turn counter
            turnText = CreateText(panel, new Vector2(0, 60), "Turn 0/0", 20);
            turnText.alignment = TextAnchor.MiddleCenter;

            // Progress bar background
            GameObject progressBg = CreateImage(panel, new Vector2(0, 20), new Vector2(600, 20), new Color(0.2f, 0.2f, 0.2f));

            // Progress bar fill
            GameObject progressFill = CreateImage(progressBg, Vector2.zero, new Vector2(600, 20), new Color(0.3f, 0.6f, 0.9f));
            progressBar = progressFill.GetComponent<Image>();
            progressBar.type = Image.Type.Filled;
            progressBar.fillMethod = Image.FillMethod.Horizontal;
            progressBar.fillAmount = 0f;

            // Pause button
            pauseButton = CreateButton(panel, new Vector2(-200, -40), new Vector2(150, 40), "|| Pause", OnPauseClicked);
            pauseButtonText = pauseButton.GetComponentInChildren<Text>();

            // Speed buttons
            float[] speeds = { 0.5f, 1f, 2f, 5f, 10f };
            speedButtons = new Button[speeds.Length];
            for (int i = 0; i < speeds.Length; i++)
            {
                float speed = speeds[i];
                Vector2 pos = new Vector2(-100 + i * 80, -40);
                speedButtons[i] = CreateButton(panel, pos, new Vector2(70, 40), $"{speed}x", () => OnSpeedClicked(speed));
            }

            // Exit button
            exitButton = CreateButton(panel, new Vector2(0, -90), new Vector2(200, 40), "Exit to Main Menu", OnExitClicked);

            // Highlight default speed (1x)
            UpdateSpeedButtons(1.0f);
        }

        private GameObject CreatePanel()
        {
            GameObject panel = new GameObject("ReplayPanel");
            panel.transform.SetParent(transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(800, 200);

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            return panel;
        }

        private Text CreateText(GameObject parent, Vector2 position, string text, int fontSize)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(700, 30);

            Text txt = textObj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            return txt;
        }

        private GameObject CreateImage(GameObject parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject imgObj = new GameObject("Image");
            imgObj.transform.SetParent(parent.transform, false);

            RectTransform rect = imgObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = imgObj.AddComponent<Image>();
            img.color = color;

            return imgObj;
        }

        private Button CreateButton(GameObject parent, Vector2 position, Vector2 size, string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Button");
            btnObj.transform.SetParent(parent.transform, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            // Button text
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private void OnPauseClicked()
        {
            replayManager.TogglePause();
            UpdatePauseButton();
        }

        private void OnSpeedClicked(float speed)
        {
            currentSpeed = speed;
            replayManager.SetSpeed(speed);
            UpdateSpeedButtons(speed);
        }

        private void OnExitClicked()
        {
            Debug.Log("[ReplayControlsUI] Exit button clicked");
            replayManager.Stop();

            // Return to main menu
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ChangePhase(GamePhase.MainMenu);
            }

            // Destroy replay objects
            Destroy(replayManager.gameObject);
            Destroy(gameObject);
        }

        private void UpdatePauseButton()
        {
            if (pauseButtonText != null)
            {
                pauseButtonText.text = replayManager.IsPaused ? "▶ Resume" : "|| Pause";
            }
        }

        private void UpdateSpeedButtons(float speed)
        {
            float[] speeds = { 0.5f, 1f, 2f, 5f, 10f };
            for (int i = 0; i < speedButtons.Length && i < speeds.Length; i++)
            {
                if (speedButtons[i] != null)
                {
                    Image img = speedButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = Mathf.Approximately(speeds[i], speed) ?
                            new Color(0.3f, 0.5f, 0.7f) : new Color(0.2f, 0.2f, 0.2f);
                    }
                }
            }
        }

        private void UpdateProgress(int current, int total)
        {
            if (turnText != null)
            {
                turnText.text = $"Turn {current}/{total}";
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = total > 0 ? (float)current / total : 0f;
            }
        }

        private void HandleReplayComplete()
        {
            Debug.Log("[ReplayControlsUI] Replay complete!");
            if (turnText != null)
            {
                turnText.text = $"Replay Complete! ({replayManager.TotalTurns} turns)";
                turnText.color = Color.green;
            }
        }

        private void HandleStateUpdated(GameState state)
        {
            // Update filename display if needed
            if (titleText != null && !string.IsNullOrEmpty(replayManager.State?.ToString()))
            {
                string filename = Path.GetFileName(replayManager.State.ToString());
                // Keep it simple for now
            }
        }

        private void OnDestroy()
        {
            if (replayManager != null)
            {
                replayManager.OnTurnChanged -= UpdateProgress;
                replayManager.OnReplayComplete -= HandleReplayComplete;
                replayManager.OnStateUpdated -= HandleStateUpdated;
            }
        }
    }
}
