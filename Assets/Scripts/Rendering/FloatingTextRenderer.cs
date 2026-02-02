using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Renders floating text numbers that appear at world positions and float upward
    /// Used for damage numbers, gold gain, notifications, etc.
    /// </summary>
    public class FloatingTextRenderer : MonoBehaviour
    {
        [Header("Text Settings")]
        public float fontSize = 2f;
        public float floatDistance = 1.5f;
        public float duration = 2f;
        public bool useBillboard = true;

        [Header("Animation")]
        public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
        public float randomHorizontalOffset = 0.3f;

        [Header("Damage Colors")]
        public Color damageColor = new Color(1f, 0.3f, 0.3f); // Red
        public Color healColor = new Color(0.3f, 1f, 0.3f); // Green
        public Color goldColor = new Color(1f, 0.8f, 0f); // Yellow
        public Color infoColor = Color.white;

        [Header("Performance")]
        public int maxActiveTexts = 20;
        public bool useObjectPooling = true;

        private List<FloatingTextInstance> activeTexts = new List<FloatingTextInstance>();
        private Queue<GameObject> textPool = new Queue<GameObject>();

        /// <summary>
        /// Spawn floating damage number at position
        /// </summary>
        public void SpawnDamageNumber(Vector3 worldPosition, int damage, bool isHealing = false)
        {
            Color color = isHealing ? healColor : damageColor;
            string text = isHealing ? $"+{damage}" : $"-{damage}";
            SpawnText(worldPosition, text, color);
        }

        /// <summary>
        /// Spawn floating gold notification
        /// </summary>
        public void SpawnGoldNotification(Vector3 worldPosition, int goldAmount)
        {
            string text = $"+{goldAmount}g";
            SpawnText(worldPosition, text, goldColor);
        }

        /// <summary>
        /// Spawn generic floating text
        /// </summary>
        public void SpawnText(Vector3 worldPosition, string text, Color color)
        {
            // Enforce max active texts
            if (activeTexts.Count >= maxActiveTexts)
            {
                // Remove oldest
                var oldest = activeTexts[0];
                if (oldest.gameObject != null)
                    DestroyOrRecycle(oldest.gameObject);
                activeTexts.RemoveAt(0);
            }

            GameObject textObj = GetOrCreateTextObject();
            textObj.transform.position = worldPosition + Vector3.up * 0.5f;

            // Apply random horizontal offset to prevent stacking
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomHorizontalOffset, randomHorizontalOffset),
                0f,
                Random.Range(-randomHorizontalOffset, randomHorizontalOffset)
            );
            textObj.transform.position += randomOffset;

            // Set up world-space text
            Canvas canvas = textObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = textObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                RectTransform canvasRT = textObj.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(2, 1);
            }

            // Add text child
            GameObject textChild = new GameObject("Text");
            textChild.transform.SetParent(textObj.transform, false);

            Text txt = textChild.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 32;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;

            RectTransform textRT = textChild.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            Outline outline = textChild.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            // Billboard
            if (useBillboard)
            {
                Billboard billboard = textObj.GetComponent<Billboard>();
                if (billboard == null)
                {
                    billboard = textObj.AddComponent<Billboard>();
                }
            }

            // Track instance
            FloatingTextInstance instance = new FloatingTextInstance
            {
                gameObject = textObj,
                startPosition = textObj.transform.position,
                startTime = Time.time
            };
            activeTexts.Add(instance);

            // Animate
            StartCoroutine(AnimateFloatingText(instance));
        }

        private GameObject GetOrCreateTextObject()
        {
            if (useObjectPooling && textPool.Count > 0)
            {
                GameObject obj = textPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            GameObject newObj = new GameObject("FloatingText");
            newObj.transform.SetParent(transform);
            return newObj;
        }

        private void DestroyOrRecycle(GameObject obj)
        {
            if (useObjectPooling && textPool.Count < maxActiveTexts * 2)
            {
                obj.SetActive(false);
                textPool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private IEnumerator AnimateFloatingText(FloatingTextInstance instance)
        {
            float elapsed = 0f;
            Vector3 startPos = instance.startPosition;
            Vector3 endPos = startPos + Vector3.up * floatDistance;

            Text[] texts = instance.gameObject.GetComponentsInChildren<Text>();

            while (elapsed < duration && instance.gameObject != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float up using curve
                float floatT = floatCurve.Evaluate(t);
                instance.gameObject.transform.position = Vector3.Lerp(startPos, endPos, floatT);

                // Fade using curve
                float alpha = fadeCurve.Evaluate(t);
                foreach (Text txt in texts)
                {
                    if (txt != null)
                    {
                        Color color = txt.color;
                        color.a = alpha;
                        txt.color = color;
                    }
                }

                yield return null;
            }

            // Cleanup
            activeTexts.Remove(instance);
            if (instance.gameObject != null)
            {
                DestroyOrRecycle(instance.gameObject);
            }
        }

        /// <summary>
        /// Clear all active floating texts
        /// </summary>
        public void ClearAll()
        {
            foreach (var instance in activeTexts)
            {
                if (instance.gameObject != null)
                    DestroyOrRecycle(instance.gameObject);
            }
            activeTexts.Clear();
        }

        /// <summary>
        /// Billboard component to make text face camera
        /// </summary>
        private class Billboard : MonoBehaviour
        {
            private void LateUpdate()
            {
                if (Camera.main != null)
                {
                    transform.rotation = Camera.main.transform.rotation;
                }
            }
        }

        private class FloatingTextInstance
        {
            public GameObject gameObject;
            public Vector3 startPosition;
            public float startTime;
        }
    }
}
