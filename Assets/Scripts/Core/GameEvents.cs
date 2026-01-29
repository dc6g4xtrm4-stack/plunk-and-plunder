using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Core
{
    [Serializable]
    public class GameEvent
    {
        public int turnNumber;
        public GameEventType type;
        public string message;

        public GameEvent(int turnNumber, GameEventType type, string message)
        {
            this.turnNumber = turnNumber;
            this.type = type;
            this.message = message;
        }
    }

    public enum GameEventType
    {
        UnitMoved,
        UnitDestroyed,
        UnitsCollided,
        PlayerEliminated,
        GameWon,
        TurnStarted,
        ShipyardDeployed,
        ShipBuilt,
        ShipRepaired,
        ShipUpgraded
    }

    [Serializable]
    public class UnitMovedEvent : GameEvent
    {
        public string unitId;
        public HexCoord from;
        public HexCoord to;
        public List<HexCoord> path; // Full path for animation

        public UnitMovedEvent(int turnNumber, string unitId, HexCoord from, HexCoord to, List<HexCoord> path = null)
            : base(turnNumber, GameEventType.UnitMoved, $"Unit {unitId} moved from {from} to {to}")
        {
            this.unitId = unitId;
            this.from = from;
            this.to = to;
            this.path = path;
        }
    }

    [Serializable]
    public class UnitDestroyedEvent : GameEvent
    {
        public string unitId;
        public int ownerId;
        public HexCoord position;

        public UnitDestroyedEvent(int turnNumber, string unitId, int ownerId, HexCoord position)
            : base(turnNumber, GameEventType.UnitDestroyed, $"Unit {unitId} (Player {ownerId}) was destroyed at {position}")
        {
            this.unitId = unitId;
            this.ownerId = ownerId;
            this.position = position;
        }
    }

    [Serializable]
    public class UnitsCollidedEvent : GameEvent
    {
        public List<string> unitIds;
        public HexCoord position;

        public UnitsCollidedEvent(int turnNumber, List<string> unitIds, HexCoord position)
            : base(turnNumber, GameEventType.UnitsCollided, $"{unitIds.Count} units collided at {position}")
        {
            this.unitIds = unitIds;
            this.position = position;
        }
    }

    [Serializable]
    public class PlayerEliminatedEvent : GameEvent
    {
        public int playerId;
        public string playerName;

        public PlayerEliminatedEvent(int turnNumber, int playerId, string playerName)
            : base(turnNumber, GameEventType.PlayerEliminated, $"Player {playerName} has been eliminated")
        {
            this.playerId = playerId;
            this.playerName = playerName;
        }
    }

    [Serializable]
    public class ShipyardDeployedEvent : GameEvent
    {
        public string shipId;
        public string shipyardId;
        public int playerId;
        public HexCoord position;

        public ShipyardDeployedEvent(int turnNumber, string shipId, string shipyardId, int playerId, HexCoord position)
            : base(turnNumber, GameEventType.ShipyardDeployed, $"Player {playerId} deployed shipyard at {position}")
        {
            this.shipId = shipId;
            this.shipyardId = shipyardId;
            this.playerId = playerId;
            this.position = position;
        }
    }

    [Serializable]
    public class ShipBuiltEvent : GameEvent
    {
        public string shipId;
        public string shipyardId;
        public int playerId;
        public HexCoord position;
        public int cost;

        public ShipBuiltEvent(int turnNumber, string shipId, string shipyardId, int playerId, HexCoord position, int cost)
            : base(turnNumber, GameEventType.ShipBuilt, $"Player {playerId} built ship at {position} for {cost} gold")
        {
            this.shipId = shipId;
            this.shipyardId = shipyardId;
            this.playerId = playerId;
            this.position = position;
            this.cost = cost;
        }
    }

    [Serializable]
    public class ShipRepairedEvent : GameEvent
    {
        public string shipId;
        public string shipyardId;
        public int playerId;
        public int oldHealth;
        public int newHealth;
        public int cost;

        public ShipRepairedEvent(int turnNumber, string shipId, string shipyardId, int playerId, int oldHealth, int newHealth, int cost)
            : base(turnNumber, GameEventType.ShipRepaired, $"Player {playerId} repaired ship {shipId} ({oldHealth} -> {newHealth} HP) for {cost} gold")
        {
            this.shipId = shipId;
            this.shipyardId = shipyardId;
            this.playerId = playerId;
            this.oldHealth = oldHealth;
            this.newHealth = newHealth;
            this.cost = cost;
        }
    }

    [Serializable]
    public class ShipUpgradedEvent : GameEvent
    {
        public string shipId;
        public string shipyardId;
        public int playerId;
        public int oldMaxHealth;
        public int newMaxHealth;
        public int cost;

        public ShipUpgradedEvent(int turnNumber, string shipId, string shipyardId, int playerId, int oldMaxHealth, int newMaxHealth, int cost)
            : base(turnNumber, GameEventType.ShipUpgraded, $"Player {playerId} upgraded ship {shipId} (max HP: {oldMaxHealth} -> {newMaxHealth}) for {cost} gold")
        {
            this.shipId = shipId;
            this.shipyardId = shipyardId;
            this.playerId = playerId;
            this.oldMaxHealth = oldMaxHealth;
            this.newMaxHealth = newMaxHealth;
            this.cost = cost;
        }
    }
}
