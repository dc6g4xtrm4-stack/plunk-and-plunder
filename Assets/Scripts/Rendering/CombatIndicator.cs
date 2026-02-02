using UnityEngine;
using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Visual indicator for combat (RED SKULL AND CROSSBONES)
    /// </summary>
    public class CombatIndicator : MonoBehaviour
    {
        private static GameObject indicatorPrefab;
        private GameObject currentIndicator;
        private float displayDuration = 2.0f;
        private float displayTimer = 0f;

        public void Initialize()
        {
            // Create indicator prefab if it doesn't exist
            if (indicatorPrefab == null)
            {
                CreateIndicatorPrefab();
            }
        }

        private void CreateIndicatorPrefab()
        {
            // Create a simple quad with skull texture (placeholder - use Unity primitive for now)
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CombatIndicator";

            // Scale up to be large and visible
            quad.transform.localScale = new Vector3(2f, 2f, 1f);

            // Get or create material
            Material mat = quad.GetComponent<Renderer>().material;
            mat.color = new Color(1f, 0f, 0f, 0.8f); // RED with transparency

            // Make it unlit
            mat.shader = Shader.Find("Unlit/Transparent");

            // Orient to face camera (billboard)
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Position slightly above ground
            quad.transform.position = new Vector3(0, 0.5f, 0);

            // Add pulsing animation component
            var pulser = quad.AddComponent<CombatIndicatorPulse>();

            indicatorPrefab = quad;
            indicatorPrefab.SetActive(false);
            DontDestroyOnLoad(indicatorPrefab);

            Debug.Log("[CombatIndicator] Prefab created");
        }

        /// <summary>
        /// Show combat indicator at world position for specified duration
        /// </summary>
        public void ShowCombatAt(Vector3 worldPosition, float duration = 2.0f)
        {
            // Clear any existing indicator
            HideIndicator();

            // Instantiate new indicator
            if (indicatorPrefab != null)
            {
                currentIndicator = Instantiate(indicatorPrefab);
                currentIndicator.transform.position = worldPosition + Vector3.up * 0.5f; // Slightly above ground
                currentIndicator.SetActive(true);

                displayDuration = duration;
                displayTimer = 0f;

                Debug.Log($"[CombatIndicator] ☠️ COMBAT at {worldPosition}");
            }
        }

        /// <summary>
        /// Show combat indicator at hex coord
        /// </summary>
        public void ShowCombatAt(HexCoord coord, float hexSize = 1f, float duration = 2.0f)
        {
            Vector3 worldPos = coord.ToWorldPosition(hexSize);
            ShowCombatAt(worldPos, duration);
        }

        public void HideIndicator()
        {
            if (currentIndicator != null)
            {
                Destroy(currentIndicator);
                currentIndicator = null;
            }
        }

        private void Update()
        {
            // Auto-hide after duration
            if (currentIndicator != null)
            {
                displayTimer += Time.deltaTime;
                if (displayTimer >= displayDuration)
                {
                    HideIndicator();
                }
            }
        }

        private void OnDestroy()
        {
            HideIndicator();
        }
    }

    /// <summary>
    /// Makes the combat indicator pulse/scale for visibility
    /// </summary>
    public class CombatIndicatorPulse : MonoBehaviour
    {
        private float pulseSpeed = 3f;
        private float pulseAmount = 0.2f;
        private Vector3 baseScale;

        private void Start()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            // Pulse scale
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * pulse;

            // Rotate to face camera (billboard effect)
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(90f, 0f, 0f); // Face up
            }
        }
    }
}
