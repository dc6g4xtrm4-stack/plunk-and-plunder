using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Manages layout and positioning of all HUD elements
    /// Centralizes positioning logic and handles screen resize events
    /// </summary>
    public class HUDLayoutManager : MonoBehaviour
    {
        [Header("HUD Components")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform rightPanel;
        [SerializeField] private RectTransform bottomBar;

        private void Start()
        {
            InitializeLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            // Handle screen resize
            if (topBar != null) PositionTopBar();
            if (leftPanel != null) PositionLeftPanel();
            if (rightPanel != null) PositionRightPanel();
            if (bottomBar != null) PositionBottomBar();
        }

        public void InitializeLayout()
        {
            PositionTopBar();
            PositionLeftPanel();
            PositionRightPanel();
            if (bottomBar != null) PositionBottomBar();
        }

        private void PositionTopBar()
        {
            if (topBar == null) return;

            // Anchor: Top-center
            topBar.anchorMin = new Vector2(0.5f, 1f);
            topBar.anchorMax = new Vector2(0.5f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = Vector2.zero; // Flush with top
            topBar.sizeDelta = new Vector2(Screen.width, HUDStyles.TopBarHeight);
        }

        private void PositionLeftPanel()
        {
            if (leftPanel == null) return;

            // Anchor: Bottom-left
            leftPanel.anchorMin = new Vector2(0f, 0f);
            leftPanel.anchorMax = new Vector2(0f, 0f);
            leftPanel.pivot = new Vector2(0f, 0f);
            leftPanel.anchoredPosition = new Vector2(HUDStyles.EdgeMargin, HUDStyles.EdgeMargin);

            float height = Screen.height - HUDStyles.TopBarHeight - (HUDStyles.EdgeMargin * 2);
            leftPanel.sizeDelta = new Vector2(HUDStyles.LeftPanelWidth, height);
        }

        private void PositionRightPanel()
        {
            if (rightPanel == null) return;

            // Anchor: Bottom-right
            rightPanel.anchorMin = new Vector2(1f, 0f);
            rightPanel.anchorMax = new Vector2(1f, 0f);
            rightPanel.pivot = new Vector2(1f, 0f);
            rightPanel.anchoredPosition = new Vector2(-HUDStyles.EdgeMargin, HUDStyles.EdgeMargin);

            float height = Screen.height - HUDStyles.TopBarHeight - (HUDStyles.EdgeMargin * 2);
            rightPanel.sizeDelta = new Vector2(HUDStyles.RightPanelWidth, height);
        }

        private void PositionBottomBar()
        {
            if (bottomBar == null) return;

            // Anchor: Bottom-center
            bottomBar.anchorMin = new Vector2(0.5f, 0f);
            bottomBar.anchorMax = new Vector2(0.5f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.anchoredPosition = Vector2.zero; // Flush with bottom
            bottomBar.sizeDelta = new Vector2(Screen.width, HUDStyles.BottomBarHeight);
        }

        /// <summary>
        /// Helper method to create a panel with standard styling
        /// </summary>
        public static GameObject CreateStyledPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            Image bg = panel.AddComponent<Image>();
            bg.color = HUDStyles.BackgroundColor;

            return panel;
        }

        /// <summary>
        /// Helper method to create a header text element
        /// </summary>
        public static GameObject CreateHeaderText(Transform parent, string text)
        {
            GameObject headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(parent, false);

            Text headerText = headerObj.AddComponent<Text>();
            headerText.text = text;
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = HUDStyles.HeaderFontSize;
            headerText.color = HUDStyles.HeaderColor;
            headerText.alignment = TextAnchor.MiddleLeft;
            headerText.fontStyle = FontStyle.Bold;

            return headerObj;
        }

        /// <summary>
        /// Helper method to create content text element
        /// </summary>
        public static GameObject CreateContentText(Transform parent, string text)
        {
            GameObject textObj = new GameObject("Content", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            Text contentText = textObj.AddComponent<Text>();
            contentText.text = text;
            contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contentText.fontSize = HUDStyles.ContentFontSize;
            contentText.color = HUDStyles.TextColor;
            contentText.alignment = TextAnchor.UpperLeft;

            return textObj;
        }

        /// <summary>
        /// Helper method to add a border to a panel
        /// </summary>
        public static void AddBorder(GameObject panel, Color color, float thickness = 2f)
        {
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
        }
    }
}
