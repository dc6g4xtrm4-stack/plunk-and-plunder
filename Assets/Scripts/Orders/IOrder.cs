using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Orders
{
    public interface IOrder
    {
        string unitId { get; }
        int playerId { get; }
        OrderType GetOrderType();
    }

    public enum OrderType
    {
        Move,
        DeployShipyard,
        BuildShip,
        RepairShip,
        UpgradeShip
    }

    [Serializable]
    public class MoveOrder : IOrder
    {
        public string unitId { get; set; }
        public int playerId { get; set; }
        public List<HexCoord> path;
        public HexCoord destination => path != null && path.Count > 0 ? path[path.Count - 1] : default;

        public MoveOrder(string unitId, int playerId, List<HexCoord> path)
        {
            this.unitId = unitId;
            this.playerId = playerId;
            this.path = path;
        }

        public OrderType GetOrderType()
        {
            return OrderType.Move;
        }
    }

    [Serializable]
    public class DeployShipyardOrder : IOrder
    {
        public string unitId { get; set; }
        public int playerId { get; set; }
        public HexCoord position;

        public DeployShipyardOrder(string unitId, int playerId, HexCoord position)
        {
            this.unitId = unitId;
            this.playerId = playerId;
            this.position = position;
        }

        public OrderType GetOrderType()
        {
            return OrderType.DeployShipyard;
        }
    }

    [Serializable]
    public class BuildShipOrder : IOrder
    {
        public string unitId { get; set; } // Empty for build orders - no unit being commanded
        public int playerId { get; set; }
        public string shipyardId;
        public HexCoord shipyardPosition;

        public BuildShipOrder(int playerId, string shipyardId, HexCoord shipyardPosition)
        {
            this.unitId = ""; // Build orders don't command a unit
            this.playerId = playerId;
            this.shipyardId = shipyardId;
            this.shipyardPosition = shipyardPosition;
        }

        public OrderType GetOrderType()
        {
            return OrderType.BuildShip;
        }
    }

    [Serializable]
    public class RepairShipOrder : IOrder
    {
        public string unitId { get; set; }
        public int playerId { get; set; }
        public string shipyardId;
        public HexCoord shipyardPosition;

        public RepairShipOrder(string unitId, int playerId, string shipyardId, HexCoord shipyardPosition)
        {
            this.unitId = unitId;
            this.playerId = playerId;
            this.shipyardId = shipyardId;
            this.shipyardPosition = shipyardPosition;
        }

        public OrderType GetOrderType()
        {
            return OrderType.RepairShip;
        }
    }

    [Serializable]
    public class UpgradeShipOrder : IOrder
    {
        public string unitId { get; set; }
        public int playerId { get; set; }
        public string shipyardId;
        public HexCoord shipyardPosition;

        public UpgradeShipOrder(string unitId, int playerId, string shipyardId, HexCoord shipyardPosition)
        {
            this.unitId = unitId;
            this.playerId = playerId;
            this.shipyardId = shipyardId;
            this.shipyardPosition = shipyardPosition;
        }

        public OrderType GetOrderType()
        {
            return OrderType.UpgradeShip;
        }
    }
}
