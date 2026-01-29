using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Animates the selection indicator with a pulsing effect
    /// </summary>
    public class SelectionPulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        public float pulseSpeed = 2f;
        public float minAlpha = 0.4f;
        public float maxAlpha = 1f;

        private MeshRenderer meshRenderer;
        private Material material;
        private float pulseTimer;

        public void Initialize()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                material = meshRenderer.material;
            }
        }

        private void Update()
        {
            if (material == null)
                return;

            // Pulse the alpha over time
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(pulseTimer) + 1f) / 2f);

            // Apply to material
            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }
    }
}
