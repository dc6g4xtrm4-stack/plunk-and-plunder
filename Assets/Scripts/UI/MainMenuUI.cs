using System;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private Button hostDirectButton;
        private Button joinDirectButton;
        private Button offlineButton;
        private Text debugText;
        private InputField ipInputField;

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
            titleRect.anchoredPosition = new Vector2(0, 250);

            // Play Offline button
            offlineButton = CreateButton("Play Offline", new Vector2(0, 120), OnOfflineClicked);

            // Multiplayer section label
            GameObject multiplayerLabel = CreateText("Multiplayer (TCP Direct Connect)", 28);
            RectTransform multiplayerRect = multiplayerLabel.GetComponent<RectTransform>();
            multiplayerRect.SetParent(transform, false);
            multiplayerRect.anchoredPosition = new Vector2(0, 40);

            // Host button
            hostDirectButton = CreateButton("Host", new Vector2(0, -20), OnHostDirectClicked);

            // Join section
            ipInputField = CreateInputField("Enter IP:Port (e.g., 192.168.1.5:7777)", new Vector2(0, -90));
            joinDirectButton = CreateButton("Join", new Vector2(0, -150), OnJoinDirectClicked);

            // Debug text (bottom)
            GameObject debugTextObj = CreateText("Ready", 18);
            RectTransform debugRect = debugTextObj.GetComponent<RectTransform>();
            debugRect.SetParent(transform, false);
            debugRect.anchoredPosition = new Vector2(0, -250);
            debugText = debugTextObj.GetComponent<Text>();
            debugText.color = Color.green;
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

        private InputField CreateInputField(string placeholder, Vector2 position)
        {
            GameObject inputObj = new GameObject("InputField_IP");
            inputObj.transform.SetParent(transform, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 50);
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

        private void OnHostDirectClicked()
        {
            Debug.Log("[MainMenuUI] Hosting direct connection game");
            if (debugText != null) debugText.text = "Hosting on port 7777...";

            try
            {
                NetworkManager.Instance?.SetMode(NetworkMode.DirectConnection);
                NetworkManager.Instance?.CreateLobby(4);
                GameManager.Instance?.ChangePhase(GamePhase.Lobby);

                if (debugText != null)
                {
                    debugText.text = "Hosting! Share your IP with friends";
                    debugText.color = Color.green;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuUI] Failed to host: {ex.Message}");
                if (debugText != null)
                {
                    debugText.text = $"ERROR: {ex.Message}";
                    debugText.color = Color.red;
                }
            }
        }

        private void OnJoinDirectClicked()
        {
            string ipAddress = ipInputField?.text?.Trim();

            if (string.IsNullOrEmpty(ipAddress))
            {
                Debug.LogWarning("[MainMenuUI] No IP address entered");
                if (debugText != null)
                {
                    debugText.text = "Please enter an IP address first!";
                    debugText.color = Color.yellow;
                }
                return;
            }

            Debug.Log($"[MainMenuUI] Joining game at {ipAddress}");
            if (debugText != null) debugText.text = $"Connecting to {ipAddress}...";

            try
            {
                NetworkManager.Instance?.SetMode(NetworkMode.DirectConnection);
                NetworkManager.Instance?.JoinLobby(ipAddress);
                GameManager.Instance?.ChangePhase(GamePhase.Lobby);

                if (debugText != null)
                {
                    debugText.text = "Connected!";
                    debugText.color = Color.green;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuUI] Failed to join: {ex.Message}");
                if (debugText != null)
                {
                    debugText.text = $"ERROR: {ex.Message}";
                    debugText.color = Color.red;
                }
            }
        }

    }
}
