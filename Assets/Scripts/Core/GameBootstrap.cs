using PlunkAndPlunder.Networking;
using PlunkAndPlunder.Rendering;
using PlunkAndPlunder.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlunkAndPlunder.Core
{
    /// <summary>
    /// Initializes the game scene and creates all necessary GameObjects
    /// This should be the only script in the initial scene
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            CreateGameManager();
            CreateNetworkManager();
            CreateLighting();
            CreateRenderers();
            CreateUI();
            CreateCamera();
        }

        private void CreateGameManager()
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            Debug.Log("[GameBootstrap] GameManager created");
        }

        private void CreateNetworkManager()
        {
            GameObject nmObj = new GameObject("NetworkManager");
            nmObj.AddComponent<NetworkManager>();
            Debug.Log("[GameBootstrap] NetworkManager created");
        }

        private void CreateLighting()
        {
            // Create directional light (sun)
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.0f;

            // Position and rotate to shine from above and slightly to the side
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Debug.Log("[GameBootstrap] Directional light created");
        }

        private void CreateRenderers()
        {
            GameObject renderersObj = new GameObject("Renderers");

            GameObject hexRendererObj = new GameObject("HexRenderer");
            hexRendererObj.transform.SetParent(renderersObj.transform);
            HexRenderer hexRenderer = hexRendererObj.AddComponent<HexRenderer>();

            GameObject unitRendererObj = new GameObject("UnitRenderer");
            unitRendererObj.transform.SetParent(renderersObj.transform);
            UnitRenderer unitRenderer = unitRendererObj.AddComponent<UnitRenderer>();

            GameObject buildingRendererObj = new GameObject("BuildingRenderer");
            buildingRendererObj.transform.SetParent(renderersObj.transform);
            BuildingRenderer buildingRenderer = buildingRendererObj.AddComponent<BuildingRenderer>();

            // Subscribe to game state updates
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateUpdated += (state) =>
                {
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
            }

            Debug.Log("[GameBootstrap] Renderers created");
        }

        private void CreateUI()
        {
            // Ensure EventSystem exists for UI input
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[GameBootstrap] EventSystem created");
            }

            GameObject uiObj = new GameObject("UIBootstrapper");
            uiObj.AddComponent<UIBootstrapper>();
            Debug.Log("[GameBootstrap] UI created");
        }

        private void CreateCamera()
        {
            // Check if main camera exists
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            // Add camera controller
            if (mainCam.GetComponent<CameraController>() == null)
            {
                mainCam.gameObject.AddComponent<CameraController>();
            }

            Debug.Log("[GameBootstrap] Camera configured");
        }

        private void Start()
        {
            Debug.Log("[GameBootstrap] Plunk & Plunder initialized");
        }
    }
}
