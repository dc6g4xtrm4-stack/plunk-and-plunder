using System.Collections.Generic;
using PlunkAndPlunder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class EventLogUI : MonoBehaviour
    {
        private Text logText;
        private ScrollRect scrollRect;
        private List<string> logMessages = new List<string>();
        private const int maxMessages = 20;

        public void Initialize()
        {
            CreateLayout();
        }

        private void CreateLayout()
        {
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 300);
            rect.anchoredPosition = new Vector2(700, -300);

            // Background
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // ScrollRect
            scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(380, 280);
            contentRect.anchoredPosition = Vector2.zero;

            scrollRect.content = contentRect;

            // Log text
            GameObject textObj = new GameObject("LogText");
            textObj.transform.SetParent(content.transform, false);

            logText = textObj.AddComponent<Text>();
            logText.text = "Event Log:\n";
            logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logText.fontSize = 16;
            logText.alignment = TextAnchor.UpperLeft;
            logText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(360, 280);
            textRect.anchoredPosition = new Vector2(0, 0);
        }

        public void AddEvent(GameEvent gameEvent)
        {
            AddMessage($"[T{gameEvent.turnNumber}] {gameEvent.message}");
        }

        public void AddEvents(List<GameEvent> events)
        {
            foreach (GameEvent evt in events)
            {
                AddEvent(evt);
            }
        }

        private void AddMessage(string message)
        {
            logMessages.Add(message);

            if (logMessages.Count > maxMessages)
            {
                logMessages.RemoveAt(0);
            }

            UpdateLogDisplay();
        }

        private void UpdateLogDisplay()
        {
            logText.text = "Event Log:\n" + string.Join("\n", logMessages);
        }
    }
}
