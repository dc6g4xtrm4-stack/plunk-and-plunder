using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
                    TextMeshPro tmp = damageTextObj.GetComponent<TextMeshPro>();
                    if (tmp != null)
                    {
                        Color textColor = tmp.color;
                        textColor.a = 1f - t;
                        tmp.color = textColor;
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

            // Add TextMeshPro component
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = $"-{damage}";
            tmp.fontSize = damageTextSize;
            tmp.color = damageTextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            // Add outline
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;

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
