using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Animates a button's background with a pulsing effect to draw attention
    /// </summary>
    public class ButtonPulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        public float pulseSpeed = 2f;
        public bool isPulsing = false;

        [Header("Colors")]
        public Color normalColor = new Color(0.2f, 0.4f, 0.2f);
        public Color minPulseColor = new Color(0f, 0.6f, 0f);      // Green
        public Color maxPulseColor = new Color(0f, 1f, 0f);        // Bright green

        private Image buttonImage;
        private float pulseTimer;

        private void Awake()
        {
            buttonImage = GetComponent<Image>();
            if (buttonImage == null)
            {
                Debug.LogError("[ButtonPulse] No Image component found on button!");
            }
        }

        /// <summary>
        /// Enable or disable the pulsing effect
        /// </summary>
        public void SetPulsing(bool shouldPulse)
        {
            isPulsing = shouldPulse;

            if (!isPulsing && buttonImage != null)
            {
                // Reset to normal color when not pulsing
                buttonImage.color = normalColor;
            }
        }

        private void Update()
        {
            if (!isPulsing || buttonImage == null)
                return;

            // Pulse the color over time
            pulseTimer += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(pulseTimer) + 1f) / 2f;

            // Lerp between min and max pulse colors
            buttonImage.color = Color.Lerp(minPulseColor, maxPulseColor, t);
        }
    }
}
