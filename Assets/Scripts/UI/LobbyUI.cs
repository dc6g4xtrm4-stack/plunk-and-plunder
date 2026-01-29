using PlunkAndPlunder.Core;
using PlunkAndPlunder.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class LobbyUI : MonoBehaviour
    {
        private Text playersText;
        private Button startButton;
        private Button backButton;

        public void Initialize()
        {
            CreateLayout();
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
            GameObject titleObj = CreateText("Lobby", 50);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.SetParent(transform, false);
            titleRect.anchoredPosition = new Vector2(0, 300);

            // Players list
            GameObject playersObj = CreateText("Players: Waiting...", 30);
            playersText = playersObj.GetComponent<Text>();
            playersText.alignment = TextAnchor.UpperLeft;
            RectTransform playersRect = playersObj.GetComponent<RectTransform>();
            playersRect.SetParent(transform, false);
            playersRect.sizeDelta = new Vector2(600, 400);
            playersRect.anchoredPosition = new Vector2(0, 50);

            // Buttons
            startButton = CreateButton("Start Game", new Vector2(0, -200), OnStartClicked);
            backButton = CreateButton("Back", new Vector2(0, -300), OnBackClicked);
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
            rect.sizeDelta = new Vector2(300, 50);
            rect.anchoredPosition = position;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);

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

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private void Update()
        {
            UpdatePlayersList();
        }

        private void UpdatePlayersList()
        {
            if (playersText != null && NetworkManager.Instance != null)
            {
                var players = NetworkManager.Instance.Transport.GetConnectedPlayers();
                string text = "Players:\n";
                foreach (var player in players)
                {
                    text += $"- {player.playerName} {(player.isReady ? "[Ready]" : "")}\n";
                }
                playersText.text = text;
            }
        }

        private void OnStartClicked()
        {
            Debug.Log("[LobbyUI] Starting game");
            // TODO: Send start command through network
            GameManager.Instance?.StartOfflineGame(4);
        }

        private void OnBackClicked()
        {
            NetworkManager.Instance?.LeaveLobby();
            GameManager.Instance?.ChangePhase(GamePhase.MainMenu);
        }
    }
}
