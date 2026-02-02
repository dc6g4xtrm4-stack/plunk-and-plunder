using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Renders visual connection lines between combatants with damage indicators
    /// Shows red dotted lines connecting attacker to defender with damage numbers at midpoint
    /// </summary>
    public class CombatConnectionRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        public float lineWidth = 0.15f;
        public float lineHeight = 0.6f; // Y position above ground
        public float lineDuration = 2.5f;
        public int lineSegments = 20; // For dashed effect

        [Header("Colors")]
        public Color standardCombatColor = new Color(1f, 0.2f, 0.2f, 0.9f); // Red
        public Color ongoingCombatColor = new Color(1f, 0.6f, 0f, 0.9f); // Orange
        public Color playerVictoryColor = new Color(0.2f, 1f, 0.2f, 0.9f); // Green
        public Color mutualDestructionColor = new Color(1f, 0.8f, 0f, 0.9f); // Yellow

        [Header("Damage Text")]
        public float damageTextSize = 1.5f;
        public Color damageTextColor = Color.white;
        public float textFloatSpeed = 0.3f;

        [Header("Animation")]
        public float fadeInDuration = 0.2f;
        public float fadeOutDuration = 0.3f;
        public bool animateLineGrowth = true;

        private List<CombatLineInstance> activeLines = new List<CombatLineInstance>();
        private Material lineMaterial;

        private void Awake()
        {
            // Create unlit material for lines
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.color = standardCombatColor;
        }

        /// <summary>
        /// Show combat line between attacker and defender
        /// </summary>
        public void ShowCombatLine(Vector3 attackerPos, Vector3 defenderPos, int damageDealt, CombatOutcome outcome = CombatOutcome.Standard)
        {
            StartCoroutine(AnimateCombatLine(attackerPos, defenderPos, damageDealt, outcome));
        }

        private IEnumerator AnimateCombatLine(Vector3 attackerPos, Vector3 defenderPos, int damage, CombatOutcome outcome)
        {
            // Create line object
            GameObject lineObj = new GameObject("CombatLine");
            lineObj.transform.SetParent(transform);

            CombatLineInstance instance = new CombatLineInstance
            {
                gameObject = lineObj,
                startTime = Time.time
            };

            // Create LineRenderer
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            // Set positions (elevated above ground)
            Vector3 start = attackerPos + Vector3.up * lineHeight;
            Vector3 end = defenderPos + Vector3.up * lineHeight;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Set color based on outcome
            Color lineColor = GetColorForOutcome(outcome);
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            // Make dashed
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.material.mainTextureScale = new Vector2(5f, 1f);

            // Create damage text at midpoint
            Vector3 midpoint = (start + end) / 2f + Vector3.up * 0.3f;
            GameObject damageTextObj = CreateDamageText(midpoint, damage);
            damageTextObj.transform.SetParent(lineObj.transform);
            instance.damageText = damageTextObj;

            activeLines.Add(instance);

            // Animate line growth if enabled
            if (animateLineGrowth)
            {
                float growthTime = 0f;
                while (growthTime < fadeInDuration)
                {
                    growthTime += Time.deltaTime;
                    float t = growthTime / fadeInDuration;
                    lineRenderer.SetPosition(1, Vector3.Lerp(start, end, t));
                    yield return null;
                }
                lineRenderer.SetPosition(1, end);
            }

            // Fade in
            float fadeInTime = 0f;
            Color startColor = lineColor;
            startColor.a = 0f;
            while (fadeInTime < fadeInDuration)
            {
                fadeInTime += Time.deltaTime;
                float t = fadeInTime / fadeInDuration;
                Color currentColor = Color.Lerp(startColor, lineColor, t);
                lineRenderer.startColor = currentColor;
                lineRenderer.endColor = currentColor;
                yield return null;
            }

            // Hold
            float holdTime = lineDuration - fadeInDuration - fadeOutDuration;
            yield return new WaitForSeconds(holdTime);

            // Fade out
            float fadeOutTime = 0f;
            while (fadeOutTime < fadeOutDuration)
            {
                fadeOutTime += Time.deltaTime;
                float t = fadeOutTime / fadeOutDuration;
                Color currentColor = Color.Lerp(lineColor, startColor, t);
                lineRenderer.startColor = currentColor;
                lineRenderer.endColor = currentColor;

                // Fade damage text too
                if (damageTextObj != null)
                {
                    Text[] texts = damageTextObj.GetComponentsInChildren<Text>();
                    foreach (Text txt in texts)
                    {
                        Color textColor = txt.color;
                        textColor.a = 1f - t;
                        txt.color = textColor;
                    }
                }

                yield return null;
            }

            // Cleanup
            activeLines.Remove(instance);
            if (lineObj != null)
                Destroy(lineObj);
        }

        private GameObject CreateDamageText(Vector3 position, int damage)
        {
            GameObject textObj = new GameObject("DamageText");
            textObj.transform.position = position;

            // Add Canvas for world-space text
            Canvas canvas = textObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRT = textObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(2, 1);

            // Add Text component
            GameObject textChild = new GameObject("Text");
            textChild.transform.SetParent(textObj.transform, false);

            Text text = textChild.AddComponent<Text>();
            text.text = $"-{damage}";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.color = damageTextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;

            RectTransform textRT = textChild.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            // Add outline
            Outline outline = textChild.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            // Billboard component to face camera
            Billboard billboard = textObj.AddComponent<Billboard>();

            // Float up animation
            StartCoroutine(AnimateDamageText(textObj));

            return textObj;
        }

        private IEnumerator AnimateDamageText(GameObject textObj)
        {
            float elapsed = 0f;
            Vector3 startPos = textObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * textFloatSpeed;

            while (elapsed < lineDuration && textObj != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lineDuration;
                textObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }

        private Color GetColorForOutcome(CombatOutcome outcome)
        {
            switch (outcome)
            {
                case CombatOutcome.PlayerVictory:
                    return playerVictoryColor;
                case CombatOutcome.Ongoing:
                    return ongoingCombatColor;
                case CombatOutcome.MutualDestruction:
                    return mutualDestructionColor;
                default:
                    return standardCombatColor;
            }
        }

        /// <summary>
        /// Show multiple ships attacking one target (Phase 3.2: Multi-ship combat)
        /// Creates radial connection pattern with staggered animations
        /// </summary>
        public void ShowMultiShipCombat(List<Vector3> attackerPositions, Vector3 defenderPos, List<int> damages, int totalDamage)
        {
            Debug.Log($"[CombatConnectionRenderer] Multi-ship combat: {attackerPositions.Count} attackers vs 1 defender, total damage: {totalDamage}");

            // Show lines from each attacker with staggered delay
            for (int i = 0; i < attackerPositions.Count; i++)
            {
                Vector3 attackerPos = attackerPositions[i];
                int damage = i < damages.Count ? damages[i] : 0;
                float delay = i * 0.2f; // Stagger by 0.2s each

                StartCoroutine(ShowDelayedCombatLine(attackerPos, defenderPos, damage, delay));
            }

            // Show total damage at defender position (after all lines appear)
            float totalDelay = attackerPositions.Count * 0.2f + 0.3f;
            StartCoroutine(ShowTotalDamage(defenderPos, totalDamage, totalDelay));
        }

        private IEnumerator ShowDelayedCombatLine(Vector3 attackerPos, Vector3 defenderPos, int damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowCombatLine(attackerPos, defenderPos, damage, CombatOutcome.Standard);
        }

        private IEnumerator ShowTotalDamage(Vector3 position, int totalDamage, float delay)
        {
            yield return new WaitForSeconds(delay);

            Vector3 textPos = position + Vector3.up * 1.0f;
            GameObject textObj = CreateDamageText(textPos, totalDamage);
            textObj.transform.SetParent(transform);

            // Make it bigger and more prominent
            Text text = textObj.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 48; // Larger than individual damage
                text.color = Color.yellow; // Yellow for total
                text.text = $"TOTAL: -{totalDamage}";
            }

            // Auto-destroy after duration
            Destroy(textObj, lineDuration);
        }

        /// <summary>
        /// Clear all active combat lines
        /// </summary>
        public void ClearAllLines()
        {
            foreach (var instance in activeLines)
            {
                if (instance.gameObject != null)
                    Destroy(instance.gameObject);
            }
            activeLines.Clear();
        }

        /// <summary>
        /// Billboard component to make text face camera
        /// </summary>
        private class Billboard : MonoBehaviour
        {
            private void Update()
            {
                if (Camera.main != null)
                {
                    transform.rotation = Camera.main.transform.rotation;
                }
            }
        }

        private class CombatLineInstance
        {
            public GameObject gameObject;
            public GameObject damageText;
            public float startTime;
        }
    }

    public enum CombatOutcome
    {
        Standard,          // Normal combat
        PlayerVictory,     // Player ship won
        Ongoing,           // Combat continues next turn
        MutualDestruction  // Both ships destroyed
    }
}
