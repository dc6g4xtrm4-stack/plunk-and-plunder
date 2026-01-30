using System.Collections.Generic;
using PlunkAndPlunder.Structures;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class BuildQueueUI : MonoBehaviour
    {
        private GameObject queuePanel;
        private List<GameObject> queueSlots = new List<GameObject>();
        private Structure currentShipyard;

        public void Initialize()
        {
            CreateQueuePanel();
            HideQueue();
        }

        private void CreateQueuePanel()
        {
            // Create queue panel (positioned below the selected unit text)
            queuePanel = CreatePanel(new Vector2(-800, -200), new Vector2(300, 200), new Color(0.15f, 0.15f, 0.15f, 0.9f));
            queuePanel.transform.SetParent(transform, false);

            // Title
            Text titleText = CreateText("BUILD QUEUE", 18, queuePanel.transform);
            titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
            titleText.alignment = TextAnchor.MiddleCenter;

            // Create 5 queue slots
            for (int i = 0; i < BuildingConfig.MAX_QUEUE_SIZE; i++)
            {
                GameObject slot = CreateQueueSlot(i);
                slot.transform.SetParent(queuePanel.transform, false);
                queueSlots.Add(slot);
            }
        }

        private GameObject CreateQueueSlot(int index)
        {
            // Calculate position (stack vertically)
            float yPos = 40 - (index * 30);
            Vector2 position = new Vector2(0, yPos);

            GameObject slot = CreatePanel(position, new Vector2(280, 28), new Color(0.2f, 0.2f, 0.2f, 0.8f));

            // Slot text
            Text slotText = CreateText($"Slot {index + 1}: Empty", 14, slot.transform);
            slotText.GetComponent<RectTransform>().sizeDelta = new Vector2(270, 26);
            slotText.alignment = TextAnchor.MiddleLeft;
            slotText.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 0);

            // Store the text component for updates
            slot.name = $"QueueSlot_{index}";
            slot.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            return slot;
        }

        private GameObject CreatePanel(Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject("Panel");
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image bg = panel.AddComponent<Image>();
            bg.color = color;

            return panel;
        }

        private Text CreateText(string text, int fontSize, Transform parent = null)
        {
            GameObject textObj = new GameObject("Text");
            if (parent != null)
                textObj.transform.SetParent(parent, false);

            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 30);

            return textComponent;
        }

        public void ShowQueue(Structure shipyard)
        {
            if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
            {
                HideQueue();
                return;
            }

            currentShipyard = shipyard;
            queuePanel.SetActive(true);
            UpdateQueueDisplay();
        }

        public void HideQueue()
        {
            currentShipyard = null;
            if (queuePanel != null)
            {
                queuePanel.SetActive(false);
            }
        }

        public void UpdateQueueDisplay()
        {
            if (currentShipyard == null || queuePanel == null || !queuePanel.activeSelf)
                return;

            List<BuildQueueItem> queue = currentShipyard.buildQueue;

            for (int i = 0; i < queueSlots.Count; i++)
            {
                GameObject slot = queueSlots[i];
                Text slotText = slot.GetComponentInChildren<Text>();

                if (i < queue.Count)
                {
                    BuildQueueItem item = queue[i];
                    int turnsComplete = BuildingConfig.SHIP_BUILD_TIME - item.turnsRemaining;
                    string progressText = $"{turnsComplete}/{BuildingConfig.SHIP_BUILD_TIME}";

                    if (i == 0)
                    {
                        // First slot shows actively building item
                        slotText.text = $"  Building: {item.itemType} ({progressText})";
                        slot.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.2f, 0.8f); // Green tint
                    }
                    else
                    {
                        // Other slots show queued items
                        slotText.text = $"  Queued: {item.itemType} (0/3)";
                        slot.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.2f, 0.8f); // Yellow tint
                    }
                }
                else
                {
                    // Empty slot
                    slotText.text = $"  Slot {i + 1}: Empty";
                    slot.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Gray
                }
            }
        }

        private void Update()
        {
            // Update display if queue is visible
            if (currentShipyard != null && queuePanel != null && queuePanel.activeSelf)
            {
                UpdateQueueDisplay();
            }
        }
    }
}
