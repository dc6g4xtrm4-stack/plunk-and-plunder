using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Displays all player statistics (gold, ships, shipyards)
    /// </summary>
    public class PlayerStatsHUD : MonoBehaviour
    {
        private GameObject panel;
        private Dictionary<int, PlayerStatRow> playerRows = new Dictionary<int, PlayerStatRow>();

        private class PlayerStatRow
        {
            public GameObject rowObject;
            public Text playerNameText;
            public Text goldText;
            public Text shipsText;
            public Text shipyardsText;
        }

        public void Initialize()
        {
            CreatePanel();
            Debug.Log("[PlayerStatsHUD] Initialized");
        }

        private void CreatePanel()
        {
            // Create panel on BOTTOM LEFT of screen, positioned above the selection window
            panel = new GameObject("PlayerStatsPanel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0); // Anchor to bottom left corner
            panelRect.anchorMax = new Vector2(0, 0); // Same anchor point
            panelRect.pivot = new Vector2(0, 0); // Pivot at bottom left
            panelRect.anchoredPosition = new Vector2(10, 10); // 10px from left, 10px from bottom
            panelRect.sizeDelta = new Vector2(350, 200); // Width x Height

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            // Add title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "PLAYER STATS";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.8f, 0.2f);

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(-20, 30);
        }

        public void UpdateStats(GameState state)
        {
            if (panel == null) return;

            // Clear old rows
            foreach (var kvp in playerRows)
            {
                if (kvp.Value.rowObject != null)
                {
                    Destroy(kvp.Value.rowObject);
                }
            }
            playerRows.Clear();

            // Get all active players
            List<Player> players = state.playerManager.GetActivePlayers();

            // Create row for each player
            float yOffset = -50; // Start below title
            float rowHeight = 35;

            foreach (Player player in players)
            {
                // Count ships
                int shipCount = state.unitManager.GetUnitsForPlayer(player.id).Count;

                // Count shipyards
                int shipyardCount = state.structureManager.GetStructuresForPlayer(player.id)
                    .FindAll(s => s.type == StructureType.SHIPYARD).Count;

                // Create row
                PlayerStatRow row = CreatePlayerRow(player, shipCount, shipyardCount, yOffset);
                playerRows[player.id] = row;

                yOffset -= rowHeight;
            }

            // Adjust panel height to fit all rows
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            float totalHeight = 50 + (players.Count * rowHeight) + 10; // Title + rows + padding
            panelRect.sizeDelta = new Vector2(350, totalHeight);
        }

        private PlayerStatRow CreatePlayerRow(Player player, int shipCount, int shipyardCount, float yOffset)
        {
            PlayerStatRow row = new PlayerStatRow();

            // Create row container
            row.rowObject = new GameObject($"Player{player.id}Row");
            row.rowObject.transform.SetParent(panel.transform, false);

            RectTransform rowRect = row.rowObject.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(10, yOffset);
            rowRect.sizeDelta = new Vector2(-20, 30);

            // Player name color based on ID
            Color playerColor = GetPlayerColor(player.id);

            // Player name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.rowObject.transform, false);
            row.playerNameText = nameObj.AddComponent<Text>();
            row.playerNameText.text = player.name;
            row.playerNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            row.playerNameText.fontSize = 16;
            row.playerNameText.fontStyle = FontStyle.Bold;
            row.playerNameText.alignment = TextAnchor.MiddleLeft;
            row.playerNameText.color = playerColor;

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.3f, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            // Gold (Doubloons)
            GameObject goldObj = new GameObject("Gold");
            goldObj.transform.SetParent(row.rowObject.transform, false);
            row.goldText = goldObj.AddComponent<Text>();
            row.goldText.text = $"💰 {player.gold}";
            row.goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            row.goldText.fontSize = 14;
            row.goldText.alignment = TextAnchor.MiddleCenter;
            row.goldText.color = new Color(1f, 0.85f, 0.2f);

            RectTransform goldRect = goldObj.GetComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.3f, 0);
            goldRect.anchorMax = new Vector2(0.5f, 1);
            goldRect.offsetMin = Vector2.zero;
            goldRect.offsetMax = Vector2.zero;

            // Ships
            GameObject shipsObj = new GameObject("Ships");
            shipsObj.transform.SetParent(row.rowObject.transform, false);
            row.shipsText = shipsObj.AddComponent<Text>();
            row.shipsText.text = $"⛵ {shipCount}";
            row.shipsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            row.shipsText.fontSize = 14;
            row.shipsText.alignment = TextAnchor.MiddleCenter;
            row.shipsText.color = new Color(0.6f, 0.8f, 1f);

            RectTransform shipsRect = shipsObj.GetComponent<RectTransform>();
            shipsRect.anchorMin = new Vector2(0.5f, 0);
            shipsRect.anchorMax = new Vector2(0.7f, 1);
            shipsRect.offsetMin = Vector2.zero;
            shipsRect.offsetMax = Vector2.zero;

            // Shipyards
            GameObject shipyardsObj = new GameObject("Shipyards");
            shipyardsObj.transform.SetParent(row.rowObject.transform, false);
            row.shipyardsText = shipyardsObj.AddComponent<Text>();
            row.shipyardsText.text = $"🏭 {shipyardCount}";
            row.shipyardsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            row.shipyardsText.fontSize = 14;
            row.shipyardsText.alignment = TextAnchor.MiddleCenter;
            row.shipyardsText.color = new Color(0.8f, 0.6f, 0.4f);

            RectTransform shipyardsRect = shipyardsObj.GetComponent<RectTransform>();
            shipyardsRect.anchorMin = new Vector2(0.7f, 0);
            shipyardsRect.anchorMax = new Vector2(1, 1);
            shipyardsRect.offsetMin = Vector2.zero;
            shipyardsRect.offsetMax = Vector2.zero;

            return row;
        }

        private Color GetPlayerColor(int playerId)
        {
            // Different colors for each player
            switch (playerId)
            {
                case 0: return new Color(0.5f, 0.8f, 1f);    // Light blue
                case 1: return new Color(1f, 0.5f, 0.5f);    // Light red
                case 2: return new Color(0.5f, 1f, 0.5f);    // Light green
                case 3: return new Color(1f, 1f, 0.5f);      // Light yellow
                default: return Color.white;
            }
        }

        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
