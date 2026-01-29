using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Defines the state of a unit selection indicator
    /// </summary>
    public enum SelectionState
    {
        WaitingForOrder,  // Pulsing cyan - unit has no order yet
        OrderSet          // Solid/slow pulse green - unit has an order
    }

    /// <summary>
    /// Animates the selection indicator with a pulsing effect
    /// </summary>
    public class SelectionPulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        public float pulseSpeed = 2f;
        public float minAlpha = 0.4f;
        public float maxAlpha = 1f;

        [Header("State Settings")]
        public SelectionState currentState = SelectionState.WaitingForOrder;

        // WaitingForOrder colors (cyan)
        private readonly Color waitingColor = new Color(0f, 1f, 1f, 0.8f);
        private readonly Color waitingEmission = new Color(0f, 0.5f, 0.5f, 1f);

        // OrderSet colors (green)
        private readonly Color orderSetColor = new Color(0f, 1f, 0f, 0.9f);
        private readonly Color orderSetEmission = new Color(0f, 0.7f, 0f, 1f);

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
            ApplyStateColors();
        }

        /// <summary>
        /// Set the state of the selection indicator
        /// </summary>
        public void SetState(SelectionState state)
        {
            if (currentState != state)
            {
                currentState = state;
                ApplyStateColors();
            }
        }

        private void ApplyStateColors()
        {
            if (material == null)
                return;

            Color baseColor;
            Color emissionColor;

            switch (currentState)
            {
                case SelectionState.WaitingForOrder:
                    baseColor = waitingColor;
                    emissionColor = waitingEmission;
                    break;
                case SelectionState.OrderSet:
                    baseColor = orderSetColor;
                    emissionColor = orderSetEmission;
                    break;
                default:
                    baseColor = waitingColor;
                    emissionColor = waitingEmission;
                    break;
            }

            material.color = baseColor;

            // Update emission if using Standard shader
            if (material.IsKeywordEnabled("_EMISSION"))
            {
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        private void Update()
        {
            if (material == null)
                return;

            Color baseColor;
            float currentPulseSpeed;
            float currentMinAlpha;

            // Adjust pulse behavior based on state
            switch (currentState)
            {
                case SelectionState.WaitingForOrder:
                    baseColor = waitingColor;
                    currentPulseSpeed = pulseSpeed;
                    currentMinAlpha = minAlpha;
                    break;
                case SelectionState.OrderSet:
                    baseColor = orderSetColor;
                    currentPulseSpeed = pulseSpeed * 0.5f; // Slower pulse
                    currentMinAlpha = 0.7f; // More solid (less transparent at minimum)
                    break;
                default:
                    baseColor = waitingColor;
                    currentPulseSpeed = pulseSpeed;
                    currentMinAlpha = minAlpha;
                    break;
            }

            // Pulse the alpha over time
            pulseTimer += Time.deltaTime * currentPulseSpeed;
            float alpha = Mathf.Lerp(currentMinAlpha, maxAlpha, (Mathf.Sin(pulseTimer) + 1f) / 2f);

            // Apply to material
            Color color = baseColor;
            color.a = alpha;
            material.color = color;
        }
    }
}
