using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    public class TileTooltipUI : MonoBehaviour
    {
        private Text tooltipText;
        private Image background;
        private RectTransform rectTransform;
        private bool isVisible = false;

        public void Initialize()
        {
            CreateLayout();
        }

        private void CreateLayout()
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 80);

            // Background
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.9f);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(transform, false);

            tooltipText = textObj.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tooltipText.fontSize = 16;
            tooltipText.alignment = TextAnchor.MiddleCenter;
            tooltipText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            Hide();
        }

        private void Update()
        {
            if (isVisible)
            {
                // Update position to follow mouse
                Vector2 mousePos = Input.mousePosition;
                rectTransform.position = mousePos + new Vector2(10, -10);
            }

            // Check for hover - show tooltip for any raycast hit
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Convert hit position to hex coordinate and show tile info
                HexCoord coord = HexCoord.FromWorldPosition(hit.point, 1f);
                ShowTileInfo(coord);
            }
            else
            {
                Hide();
            }
        }

        private void ShowTileInfo(HexCoord coord)
        {
            if (GameManager.Instance?.state?.grid == null)
                return;

            Tile tile = GameManager.Instance.state.grid.GetTile(coord);
            if (tile != null)
            {
                string info = $"Position: {coord}\nType: {tile.type}";

                if (tile.islandId >= 0)
                {
                    info += $"\nIsland: {tile.islandId}";
                }

                tooltipText.text = info;
                Show();
            }
        }

        private void Show()
        {
            isVisible = true;
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            isVisible = false;
            gameObject.SetActive(false);
        }
    }
}
