using PlunkAndPlunder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Escape menu that appears when ESC is pressed
    /// Provides options to return to main menu or quit game
    /// </summary>
    public class EscapeMenuUI : MonoBehaviour
    {
        private GameObject menuPanel;
        private Button resumeButton;
        private Button mainMenuButton;
        private Button quitButton;
        private bool isVisible = false;

        private void Start()
        {
            CreateMenu();
            Hide();
        }

        private void Update()
        {
            // Toggle menu on ESC key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isVisible)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
        }

        private void CreateMenu()
        {
            // Create canvas if not already on this object
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // On top of everything
            }

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Create semi-transparent background overlay
            GameObject overlayObj = new GameObject("Overlay");
            overlayObj.transform.SetParent(transform, false);

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f); // Dark semi-transparent

            // Create menu panel
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(overlayObj.transform, false);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 350);

            Image panelImg = menuPanel.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // Title
            GameObject titleObj = CreateText(menuPanel, new Vector2(0, 120), "PAUSED", 36);
            Text titleText = titleObj.GetComponent<Text>();
            titleText.alignment = TextAnchor.MiddleCenter;

            // Buttons
            resumeButton = CreateButton(menuPanel, new Vector2(0, 40), new Vector2(300, 50), "Resume (ESC)", OnResumeClicked);
            mainMenuButton = CreateButton(menuPanel, new Vector2(0, -20), new Vector2(300, 50), "Back to Main Menu", OnMainMenuClicked);
            quitButton = CreateButton(menuPanel, new Vector2(0, -80), new Vector2(300, 50), "Exit Game", OnQuitClicked);
        }

        private GameObject CreateText(GameObject parent, Vector2 position, string text, int fontSize)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(350, 50);

            Text txt = textObj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return textObj;
        }

        private Button CreateButton(GameObject parent, Vector2 position, Vector2 size, string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Button_" + text);
            btnObj.transform.SetParent(parent.transform, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f);

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
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private void OnResumeClicked()
        {
            Debug.Log("[EscapeMenuUI] Resume clicked");
            Hide();
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("[EscapeMenuUI] Back to Main Menu clicked");
            Hide();

            // Return to main menu
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ChangePhase(GamePhase.MainMenu);
            }
            else
            {
                // Fallback: reload scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[EscapeMenuUI] Exit Game clicked");

            // Quit the application
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void Show()
        {
            if (menuPanel != null)
            {
                menuPanel.transform.parent.gameObject.SetActive(true);
                isVisible = true;
                Debug.Log("[EscapeMenuUI] Menu shown");
            }
        }

        public void Hide()
        {
            if (menuPanel != null)
            {
                menuPanel.transform.parent.gameObject.SetActive(false);
                isVisible = false;
                Debug.Log("[EscapeMenuUI] Menu hidden");
            }
        }

        public bool IsVisible => isVisible;
    }
}
