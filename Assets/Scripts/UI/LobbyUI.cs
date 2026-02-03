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
        private InputField nameInputField;
        private string localPlayerName = "Player";

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

            // Name input
            GameObject nameLabel = CreateText("Your Name:", 24);
            RectTransform nameLabelRect = nameLabel.GetComponent<RectTransform>();
            nameLabelRect.SetParent(transform, false);
            nameLabelRect.anchoredPosition = new Vector2(0, 220);

            nameInputField = CreateInputField("Enter your name...", new Vector2(0, 170));
            nameInputField.text = localPlayerName;
            nameInputField.onValueChanged.AddListener(OnNameChanged);

            // Players list
            GameObject playersObj = CreateText("Players: Waiting...", 30);
            playersText = playersObj.GetComponent<Text>();
            playersText.alignment = TextAnchor.UpperLeft;
            RectTransform playersRect = playersObj.GetComponent<RectTransform>();
            playersRect.SetParent(transform, false);
            playersRect.sizeDelta = new Vector2(600, 300);
            playersRect.anchoredPosition = new Vector2(0, 20);

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

        private InputField CreateInputField(string placeholder, Vector2 position)
        {
            GameObject inputObj = new GameObject("InputField_Name");
            inputObj.transform.SetParent(transform, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 40);
            rect.anchoredPosition = position;

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f);

            InputField inputField = inputObj.AddComponent<InputField>();

            // Text component for input
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.supportRichText = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            inputField.textComponent = text;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);

            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 20;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);

            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);

            inputField.placeholder = placeholderText;

            return inputField;
        }

        private void OnNameChanged(string newName)
        {
            localPlayerName = string.IsNullOrWhiteSpace(newName) ? "Player" : newName;

            // Update player name in network transport
            if (NetworkManager.Instance != null && NetworkManager.Instance.Transport is TCPTransport tcpTransport)
            {
                tcpTransport.SetLocalPlayerName(localPlayerName);
                Debug.Log($"[LobbyUI] Player name changed to: {localPlayerName}");
            }
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
            Debug.Log("[LobbyUI] Starting network game");

            // Hide lobby UI
            gameObject.SetActive(false);

            // Start network game with connected players + AI
            GameManager.Instance?.StartNetworkGame();
        }

        private void OnBackClicked()
        {
            NetworkManager.Instance?.LeaveLobby();
            GameManager.Instance?.ChangePhase(GamePhase.MainMenu);
        }
    }
}
