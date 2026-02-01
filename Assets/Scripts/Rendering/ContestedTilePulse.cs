using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Creates a pulsing animation effect for contested tile markers.
    /// The border color and width pulse to draw attention to contested areas.
    /// </summary>
    public class ContestedTilePulse : MonoBehaviour
    {
        public LineRenderer lineRenderer;

        [Header("Pulse Settings")]
        public float pulseSpeed = 2f;
        public float minAlpha = 0.4f;
        public float maxAlpha = 1f;
        public float minWidth = 0.1f;
        public float maxWidth = 0.15f;

        private float time = 0f;

        /// <summary>
        /// Initializes the contested tile pulse effect with a LineRenderer border.
        /// </summary>
        public void Initialize(float hexSize)
        {
            // Create LineRenderer if it doesn't exist
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            // Configure LineRenderer for hex border
            lineRenderer.positionCount = 7; // 6 corners + 1 to close the loop
            lineRenderer.loop = false;
            lineRenderer.useWorldSpace = false;

            // Set material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Create hex border points
            Vector3[] points = new Vector3[7];
            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i * Mathf.Deg2Rad;
                points[i] = new Vector3(
                    hexSize * Mathf.Cos(angle),
                    0.21f, // Slightly above tile surface
                    hexSize * Mathf.Sin(angle)
                );
            }
            points[6] = points[0]; // Close the loop

            lineRenderer.SetPositions(points);

            // Initial color and width
            Color color = new Color(1f, 0f, 0f, maxAlpha);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = maxWidth;
            lineRenderer.endWidth = maxWidth;

            Debug.Log($"[ContestedTilePulse] Initialized pulse effect for hex size {hexSize}");
        }

        private void Update()
        {
            if (lineRenderer == null) return;

            time += Time.deltaTime * pulseSpeed;

            // Pulse alpha using sine wave
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1f) / 2f);
            Color color = new Color(1f, 0f, 0f, alpha); // Red with pulsing alpha

            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            // Pulse width slightly
            float width = Mathf.Lerp(minWidth, maxWidth, (Mathf.Sin(time * 1.5f) + 1f) / 2f);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }
}
