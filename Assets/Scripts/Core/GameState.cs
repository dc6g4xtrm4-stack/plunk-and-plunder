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

        // Collision resolution data
        public List<CollisionInfo> pendingCollisions;
        public Dictionary<string, bool> collisionYieldDecisions; // unitId -> yielding?

        // Multi-turn combat data
        public List<OngoingCombat> ongoingCombats;

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
            pendingCollisions = new List<CollisionInfo>();
            collisionYieldDecisions = new Dictionary<string, bool>();
            ongoingCombats = new List<OngoingCombat>();
        }
    }

    [Serializable]
    public class CollisionInfo
    {
        public List<string> unitIds;
        public HexCoord destination;
        public Dictionary<string, List<HexCoord>> unitPaths;
        public Dictionary<string, List<HexCoord>> unitRemainingPaths;

        public CollisionInfo(List<string> unitIds, HexCoord destination)
        {
            this.unitIds = unitIds;
            this.destination = destination;
            this.unitPaths = new Dictionary<string, List<HexCoord>>();
            this.unitRemainingPaths = new Dictionary<string, List<HexCoord>>();
        }
    }

    [Serializable]
    public class OngoingCombat
    {
        public string unitId1;
        public string unitId2;
        public HexCoord position1;
        public HexCoord position2;
        public int turnsActive;
        public int combatRoundNumber;

        public OngoingCombat(string unitId1, string unitId2, HexCoord position1, HexCoord position2)
        {
            this.unitId1 = unitId1;
            this.unitId2 = unitId2;
            this.position1 = position1;
            this.position2 = position2;
            this.turnsActive = 0;
            this.combatRoundNumber = 1;
        }
    }

    public enum GamePhase
    {
        MainMenu,
        Lobby,
        Loading,
        Planning,           // Players planning their moves
        Submitted,          // Waiting for all players to submit
        Resolving,          // Turn is being resolved
        CollisionResolution,// Waiting for players to make yield decisions
        Animating,          // Animating turn resolution events
        GameOver
    }
}
