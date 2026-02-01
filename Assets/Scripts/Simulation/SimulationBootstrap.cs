using PlunkAndPlunder.Core;
using UnityEngine;

namespace PlunkAndPlunder.Simulation
{
    /// <summary>
    /// Simple bootstrap script to set up and run a game simulation
    /// Attach this to an empty GameObject in a scene and press Play
    /// </summary>
    public class SimulationBootstrap : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [SerializeField] private bool createRequiredObjects = true;

        private void Awake()
        {
            if (createRequiredObjects)
            {
                SetupSimulation();
            }
        }

        private void SetupSimulation()
        {
            Debug.Log("[SimulationBootstrap] Setting up simulation environment...");

            // Check if GameManager exists
            if (GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
                Debug.Log("[SimulationBootstrap] Created GameManager");
            }

            // Check if GameSimulator exists
            if (FindObjectOfType<GameSimulator>() == null)
            {
                GameObject simObj = new GameObject("GameSimulator");
                GameSimulator simulator = simObj.AddComponent<GameSimulator>();
                Debug.Log("[SimulationBootstrap] Created GameSimulator");
            }

            Debug.Log("[SimulationBootstrap] Setup complete. Simulation will start automatically.");
        }

        [ContextMenu("Start Simulation Manually")]
        public void StartSimulationManually()
        {
            GameSimulator sim = FindObjectOfType<GameSimulator>();
            if (sim != null)
            {
                sim.StartSimulation();
            }
            else
            {
                Debug.LogError("[SimulationBootstrap] GameSimulator not found!");
            }
        }
    }
}
