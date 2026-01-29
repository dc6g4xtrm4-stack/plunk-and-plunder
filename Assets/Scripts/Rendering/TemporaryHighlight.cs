using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Component that creates a pulsing highlight effect on game objects
    /// Automatically destroys itself after the duration expires
    /// </summary>
    public class TemporaryHighlight : MonoBehaviour
    {
        private float duration = 4f;
        private float elapsed = 0f;
        private MeshRenderer[] renderers;
        private Color[] originalColors;
        private bool isActive = false;

        [Header("Highlight Settings")]
        public float pulseSpeed = 2f;
        public float pulseIntensity = 0.5f;
        public Color highlightColor = Color.white;

        public void StartHighlight(float highlightDuration)
        {
            duration = highlightDuration;
            elapsed = 0f;
            isActive = true;

            // Get all mesh renderers in children
            renderers = GetComponentsInChildren<MeshRenderer>();
            originalColors = new Color[renderers.Length];

            // Store original colors
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }

            Debug.Log($"[TemporaryHighlight] Started highlight on {gameObject.name} for {duration}s");
        }

        private void Update()
        {
            if (!isActive || renderers == null || renderers.Length == 0)
                return;

            elapsed += Time.deltaTime;

            // Calculate pulsing effect using sine wave
            float pulseValue = Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
            float intensity = pulseValue * pulseIntensity;

            // Apply pulsing to all renderers
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.color = Color.Lerp(originalColors[i], highlightColor, intensity);
                }
            }

            // Check if duration expired
            if (elapsed >= duration)
            {
                RestoreOriginalColors();
                Destroy(this);
            }
        }

        private void RestoreOriginalColors()
        {
            // Restore original colors before destroying
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.color = originalColors[i];
                }
            }

            Debug.Log($"[TemporaryHighlight] Highlight ended on {gameObject.name}");
        }

        private void OnDestroy()
        {
            if (isActive)
            {
                RestoreOriginalColors();
            }
        }
    }
}
