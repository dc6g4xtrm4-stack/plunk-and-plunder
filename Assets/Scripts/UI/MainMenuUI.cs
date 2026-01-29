using PlunkAndPlunder.Core;
using PlunkAndPlunder.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private Button hostButton;
        private Button joinButton;
        private Button offlineButton;
        private Button quitButton;
        private Text debugText;

        public void Initialize()
        {
            CreateLayout();
        }

        private void Update()
        {
            // Update debug status
            if (debugText != null && Input.GetKeyDown(KeyCode.Space))
            {
                var eventSys = UnityEngine.EventSystems.EventSystem.current;
                debugText.text = $"EventSystem: {(eventSys != null ? "Active" : "NULL")} | Press buttons to test";
            }
        }

        private void CreateLayout()
        {
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Title
            GameObject titleObj = CreateText("Plunk & Plunder", 60);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.SetParent(transform, false);
            titleRect.anchoredPosition = new Vector2(0, 300);

            // Buttons
            offlineButton = CreateButton("Play Offline (1 Human + 3 AI)", new Vector2(0, 100), OnOfflineClicked);
            hostButton = CreateButton("Host Game (Steam)", new Vector2(0, 0), OnHostClicked);
            joinButton = CreateButton("Join Game (Steam)", new Vector2(0, -100), OnJoinClicked);
            quitButton = CreateButton("Quit", new Vector2(0, -200), OnQuitClicked);

            // Debug text (bottom)
            GameObject debugTextObj = CreateText("Ready - Click Play Offline to start", 18);
            RectTransform debugRect = debugTextObj.GetComponent<RectTransform>();
            debugRect.SetParent(transform, false);
            debugRect.anchoredPosition = new Vector2(0, -400);
            debugText = debugTextObj.GetComponent<Text>();
            debugText.color = Color.yellow;
        }

        private GameObject CreateText(string text, int fontSize)
        {
            GameObject textObj = new GameObject("Text");
            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 100);

            return textObj;
        }

        private Button CreateButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject($"Button_{label}");
            buttonObj.transform.SetParent(transform, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);
            rect.anchoredPosition = position;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private void OnOfflineClicked()
        {
            Debug.Log("========== OFFLINE BUTTON CLICKED ==========");
            Debug.Log("[MainMenuUI] Starting offline game");
            if (debugText != null) debugText.text = "Button clicked! Starting game...";

            try
            {
                Debug.Log($"NetworkManager.Instance is null: {NetworkManager.Instance == null}");
                if (debugText != null) debugText.text = $"NetworkManager: {(NetworkManager.Instance != null ? "OK" : "NULL")}";

                NetworkManager.Instance?.SetMode(NetworkMode.Offline);
                Debug.Log("NetworkManager mode set");

                Debug.Log($"GameManager.Instance is null: {GameManager.Instance == null}");
                if (debugText != null) debugText.text = $"GameManager: {(GameManager.Instance != null ? "OK" : "NULL")}";

                if (GameManager.Instance != null)
                {
                    if (debugText != null) debugText.text = "Calling StartOfflineGame...";
                    GameManager.Instance.StartOfflineGame(4);
                    Debug.Log("StartOfflineGame called successfully");
                    if (debugText != null) debugText.text = "Game started! Loading...";
                }
                else
                {
                    Debug.LogError("GameManager.Instance is NULL!");
                    if (debugText != null) debugText.text = "ERROR: GameManager is NULL!";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception in OnOfflineClicked: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                if (debugText != null) debugText.text = $"ERROR: {e.Message}";
            }
        }

        private void OnHostClicked()
        {
            Debug.Log("[MainMenuUI] Hosting game (Steam)");
            NetworkManager.Instance?.SetMode(NetworkMode.Steam);
            NetworkManager.Instance?.CreateLobby(4);
            GameManager.Instance?.ChangePhase(GamePhase.Lobby);
        }

        private void OnJoinClicked()
        {
            Debug.Log("[MainMenuUI] Joining game (Steam) - Not implemented in MVP");
            // TODO: Show lobby browser or input field for lobby ID
        }

        private void OnQuitClicked()
        {
            GameManager.Instance?.QuitGame();
        }
    }
}
