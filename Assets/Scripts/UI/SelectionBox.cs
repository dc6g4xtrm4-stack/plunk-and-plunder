using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Draws a selection box UI element for drag-selecting units
    /// </summary>
    public class SelectionBox : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Image image;
        private Vector2 startPosition;
        private bool isActive = false;

        public void Initialize()
        {
            // Create UI container
            GameObject boxObj = new GameObject("SelectionBoxVisual");
            boxObj.transform.SetParent(transform, false);

            rectTransform = boxObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);

            // Add visual
            image = boxObj.AddComponent<Image>();
            image.color = new Color(0f, 1f, 0f, 0.2f); // Semi-transparent green

            // Add border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(boxObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(0f, 1f, 0f, 0.8f); // Green border

            // Create outline effect by using a slightly smaller inner transparent area
            Outline outline = borderObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 1f, 0f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            Hide();
        }

        /// <summary>
        /// Start drawing the selection box from a screen position
        /// </summary>
        public void StartSelection(Vector2 screenPosition)
        {
            startPosition = screenPosition;
            isActive = true;

            rectTransform.anchoredPosition = screenPosition;
            rectTransform.sizeDelta = Vector2.zero;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Update the selection box to the current mouse position
        /// </summary>
        public void UpdateSelection(Vector2 currentScreenPosition)
        {
            if (!isActive)
                return;

            Vector2 size = currentScreenPosition - startPosition;

            // Handle negative sizes (dragging up/left)
            Vector2 anchoredPos = startPosition;
            Vector2 sizeDelta = size;

            if (size.x < 0)
            {
                anchoredPos.x = currentScreenPosition.x;
                sizeDelta.x = -size.x;
            }

            if (size.y < 0)
            {
                anchoredPos.y = currentScreenPosition.y;
                sizeDelta.y = -size.y;
            }

            rectTransform.anchoredPosition = anchoredPos;
            rectTransform.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// End the selection and return the selection bounds in screen space
        /// </summary>
        public Rect EndSelection()
        {
            if (!isActive)
                return new Rect();

            Vector2 min = rectTransform.anchoredPosition;
            Vector2 max = min + rectTransform.sizeDelta;

            Rect selectionRect = new Rect(min, rectTransform.sizeDelta);

            Hide();

            return selectionRect;
        }

        /// <summary>
        /// Get the current selection bounds
        /// </summary>
        public Rect GetSelectionBounds()
        {
            if (!isActive)
                return new Rect();

            Vector2 min = rectTransform.anchoredPosition;
            Vector2 max = min + rectTransform.sizeDelta;

            return new Rect(min, rectTransform.sizeDelta);
        }

        /// <summary>
        /// Hide the selection box
        /// </summary>
        public void Hide()
        {
            isActive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Check if the selection box is currently active
        /// </summary>
        public bool IsActive()
        {
            return isActive;
        }
    }
}
