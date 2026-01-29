using PlunkAndPlunder.Core;
using UnityEngine;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Creates and manages all UI programmatically
    /// </summary>
    public class UIBootstrapper : MonoBehaviour
    {
        private Canvas canvas;
        private MainMenuUI mainMenuUI;
        private LobbyUI lobbyUI;
        private GameHUD gameHUD;

        private void Start()
        {
            CreateCanvas();
            CreateUIScreens();
            SubscribeToGameEvents();

            // Show main menu by default
            ShowMainMenu();
        }

        private void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("UI Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            DontDestroyOnLoad(canvasObj);
        }

        private void CreateUIScreens()
        {
            // Create main menu
            GameObject mainMenuObj = new GameObject("MainMenu");
            mainMenuObj.transform.SetParent(canvas.transform, false);
            mainMenuUI = mainMenuObj.AddComponent<MainMenuUI>();
            mainMenuUI.Initialize();

            // Create lobby
            GameObject lobbyObj = new GameObject("Lobby");
            lobbyObj.transform.SetParent(canvas.transform, false);
            lobbyUI = lobbyObj.AddComponent<LobbyUI>();
            lobbyUI.Initialize();

            // Create game HUD
            GameObject hudObj = new GameObject("GameHUD");
            hudObj.transform.SetParent(canvas.transform, false);
            gameHUD = hudObj.AddComponent<GameHUD>();
            gameHUD.Initialize();
        }

        private void SubscribeToGameEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MainMenu:
                    ShowMainMenu();
                    break;

                case GamePhase.Lobby:
                    ShowLobby();
                    break;

                case GamePhase.Planning:
                case GamePhase.Resolving:
                case GamePhase.GameOver:
                    ShowGameHUD();
                    break;
            }
        }

        private void ShowMainMenu()
        {
            mainMenuUI?.gameObject.SetActive(true);
            lobbyUI?.gameObject.SetActive(false);
            gameHUD?.gameObject.SetActive(false);
        }

        private void ShowLobby()
        {
            mainMenuUI?.gameObject.SetActive(false);
            lobbyUI?.gameObject.SetActive(true);
            gameHUD?.gameObject.SetActive(false);
        }

        private void ShowGameHUD()
        {
            mainMenuUI?.gameObject.SetActive(false);
            lobbyUI?.gameObject.SetActive(false);
            gameHUD?.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }
    }
}
