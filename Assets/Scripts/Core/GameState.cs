using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.Core
{
    /// <summary>
    /// Complete serializable game state - authoritative data
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int turnNumber;
        public GamePhase phase;
        public int mapSeed;

        // Core data
        public HexGrid grid;
        public PlayerManager playerManager;
        public UnitManager unitManager;
        public StructureManager structureManager;

        // Current turn data
        public Dictionary<int, List<IOrder>> pendingOrders; // playerId -> orders
        public List<GameEvent> eventHistory;

        public GameState()
        {
            turnNumber = 0;
            phase = GamePhase.MainMenu;
            grid = new HexGrid();
            playerManager = new PlayerManager();
            unitManager = new UnitManager();
            structureManager = new StructureManager();
            pendingOrders = new Dictionary<int, List<IOrder>>();
            eventHistory = new List<GameEvent>();
        }
    }

    public enum GamePhase
    {
        MainMenu,
        Lobby,
        Loading,
        Planning,      // Players planning their moves
        Submitted,     // Waiting for all players to submit
        Resolving,     // Turn is being resolved
        GameOver
    }
}
