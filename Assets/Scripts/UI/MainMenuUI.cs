using System;
using System.IO;
using System.Linq;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Networking;
using PlunkAndPlunder.Rendering;
using PlunkAndPlunder.Replay;
using PlunkAndPlunder.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private Button hostButton;
        private Button joinButton;
        private Button hostDirectButton;
        private Button joinDirectButton;
        private Button offlineButton;
        private Button simulationButton;
        private Button replayButton;
        private Button quitButton;
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
            titleRect.anchoredPosition = new Vector2(0, 300);

            // Buttons
            offlineButton = CreateButton("Play Offline (1 Human + 3 AI)", new Vector2(0, 200), OnOfflineClicked);
            hostDirectButton = CreateButton("Host Direct Connection", new Vector2(0, 130), OnHostDirectClicked);

            // IP input field for joining
            ipInputField = CreateInputField("Enter IP:Port (e.g., 192.168.1.5:7777)", new Vector2(0, 60));
            joinDirectButton = CreateButton("Join Direct Connection", new Vector2(0, 0), OnJoinDirectClicked);

            simulationButton = CreateButton("Run AI Simulation (4 AI, 100 turns)", new Vector2(0, -70), OnSimulationClicked);
            replayButton = CreateButton("Replay Latest Simulation", new Vector2(0, -140), OnReplayClicked);
            hostButton = CreateButton("Host Game (Steam) [TODO]", new Vector2(0, -210), OnHostClicked);
            joinButton = CreateButton("Join Game (Steam) [TODO]", new Vector2(0, -280), OnJoinClicked);
            quitButton = CreateButton("Quit", new Vector2(0, -350), OnQuitClicked);

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

        private void OnSimulationClicked()
        {
            Debug.Log("========== SIMULATION BUTTON CLICKED ==========");
            Debug.Log("[MainMenuUI] Starting 100-turn HEADLESS AI simulation");
            if (debugText != null) debugText.text = "Generating simulation...";

            try
            {
                // Create headless simulation (NO UI, pure game logic)
                GameObject simObj = new GameObject("HeadlessSimulation");
                HeadlessSimulation simulation = simObj.AddComponent<HeadlessSimulation>();

                // Set up completion callback
                simulation.OnSimulationComplete = (logFilePath) =>
                {
                    Debug.Log($"[MainMenuUI] Simulation complete! Log: {logFilePath}");
                    if (debugText != null)
                    {
                        debugText.text = $"Simulation complete! Log: {System.IO.Path.GetFileName(logFilePath)}";
                        debugText.color = Color.green;
                    }

                    // TODO: Optionally load UI and replay the game from log
                    // For now, just show success message
                };

                // Run simulation (4 AI players, 100 turns, generates timestamped log)
                simulation.RunSimulation(numPlayers: 4, maxTurns: 100);

                Debug.Log("[MainMenuUI] Headless simulation started - generating game...");
                if (debugText != null)
                {
                    debugText.text = "Simulating 100 turns... (check Console)";
                    debugText.color = Color.yellow;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainMenuUI] Exception in OnSimulationClicked: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                if (debugText != null) debugText.text = $"ERROR: {e.Message}";
            }
        }

        private void OnReplayClicked()
        {
            Debug.Log("[MainMenuUI] Replay button clicked");

            // Find most recent simulation_*.txt file
            string projectRoot = Application.dataPath + "/..";
            string[] simFiles = Directory.GetFiles(projectRoot, "simulation_*.txt");

            if (simFiles.Length == 0)
            {
                Debug.LogWarning("[MainMenuUI] No simulation files found");
                if (debugText != null)
                {
                    debugText.text = "No simulation logs found. Run a simulation first!";
                    debugText.color = Color.yellow;
                }
                return;
            }

            // Sort by filename (timestamp) and take most recent
            Array.Sort(simFiles);
            string latestSimFile = simFiles[simFiles.Length - 1];

            Debug.Log($"[MainMenuUI] Loading replay: {latestSimFile}");
            if (debugText != null)
            {
                debugText.text = $"Loading {Path.GetFileName(latestSimFile)}...";
            }

            // Start replay
            StartReplayMode(latestSimFile);
        }

        private void StartReplayMode(string logFilePath)
        {
            try
            {
                Debug.Log($"[MainMenuUI] Starting replay mode with log: {logFilePath}");

                // Create ReplayManager if not exists
                GameObject rmObj = GameObject.Find("ReplayManager");
                if (rmObj == null)
                {
                    rmObj = new GameObject("ReplayManager");
                    DontDestroyOnLoad(rmObj);
                }

                ReplayManager replayManager = rmObj.GetComponent<ReplayManager>();
                if (replayManager == null)
                {
                    replayManager = rmObj.AddComponent<ReplayManager>();
                }

                // Create ReplayControlsUI
                GameObject rcObj = new GameObject("ReplayControls");
                rcObj.transform.SetParent(transform.parent, false);
                ReplayControlsUI controlsUI = rcObj.AddComponent<ReplayControlsUI>();
                controlsUI.Initialize(replayManager);

                // Subscribe rendering systems to replay state updates
                HexRenderer hexRenderer = FindFirstObjectByType<HexRenderer>();
                UnitRenderer unitRenderer = FindFirstObjectByType<UnitRenderer>();
                BuildingRenderer buildingRenderer = FindFirstObjectByType<BuildingRenderer>();

                replayManager.OnStateUpdated += (state) =>
                {
                    // Directly update renderers
                    if (state.grid != null && hexRenderer != null)
                    {
                        hexRenderer.RenderGrid(state.grid);
                    }

                    if (state.unitManager != null && unitRenderer != null)
                    {
                        unitRenderer.RenderUnits(state.unitManager);
                    }

                    if (state.structureManager != null && buildingRenderer != null)
                    {
                        buildingRenderer.RenderBuildings(state.structureManager);
                    }
                };

                // Hide main menu
                gameObject.SetActive(false);

                // Start replay
                replayManager.StartReplay(logFilePath);

                if (debugText != null)
                {
                    debugText.text = "Replay started!";
                    debugText.color = Color.green;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenuUI] Failed to start replay: {ex.Message}\n{ex.StackTrace}");
                if (debugText != null)
                {
                    debugText.text = $"ERROR: {ex.Message}";
                    debugText.color = Color.red;
                }
            }
        }

        private void OnQuitClicked()
        {
            GameManager.Instance?.QuitGame();
        }
    }
}
