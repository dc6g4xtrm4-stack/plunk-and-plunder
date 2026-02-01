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
        ShipQueued,
        ShipRepaired,
        ShipUpgraded,
        CombatOccurred,
        ConflictDetected,
        CollisionDetected,
        CollisionNeedsResolution,
        CollisionResolved,
        ShipyardAttacked, // DEPRECATED: Use StructureAttacked instead
        ShipyardDestroyed, // DEPRECATED: Use StructureCaptured instead
        ConstructionProgressed, // NEW: Construction system event
        GoldEarned, // NEW: Income system event
        EncounterDetected, // NEW: Encounter system event
        EncounterNeedsResolution, // NEW: Encounter system event
        EncounterResolved, // NEW: Encounter system event
        ContestedTileCreated, // NEW: Encounter system event
        ContestedTileResolved, // NEW: Encounter system event
        StructureAttacked, // NEW: Deterministic structure damage
        StructureCaptured // NEW: Structure ownership changed
    }

    [Serializable]
    public class UnitMovedEvent : GameEvent
    {
        public string unitId;
        public HexCoord from;
        public HexCoord to;
        public List<HexCoord> path; // Full path for animation (only the portion moved this turn)
        public bool isPartialMove; // Whether this is a partial move (path continues next turn)
        public List<HexCoord> remainingPath; // Path remaining for next turn (if partial)
        public int movementUsed; // How much movement was used
        public int movementRemaining; // How much movement remains

        public UnitMovedEvent(int turnNumber, string unitId, HexCoord from, HexCoord to, List<HexCoord> path = null,
            bool isPartialMove = false, List<HexCoord> remainingPath = null, int movementUsed = 0, int movementRemaining = 0)
            : base(turnNumber, GameEventType.UnitMoved,
                isPartialMove ? $"Unit {unitId} moved {movementUsed} tiles (path continues)" : $"Unit {unitId} moved from {from} to {to}")
        {
            this.unitId = unitId;
            this.from = from;
            this.to = to;
            this.path = path;
            this.isPartialMove = isPartialMove;
            this.remainingPath = remainingPath;
            this.movementUsed = movementUsed;
            this.movementRemaining = movementRemaining;
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

    [Serializable]
    public class CombatOccurredEvent : GameEvent
    {
        public string attackerId;
        public string defenderId;
        public int damageToAttacker;
        public int damageToDefender;
        public bool attackerDestroyed;
        public bool defenderDestroyed;

        // REMOVED: attackerRolls, defenderRolls - no more dice, deterministic combat only

        public CombatOccurredEvent(int turnNumber, string attackerId, string defenderId,
            int damageToAttacker, int damageToDefender,
            bool attackerDestroyed, bool defenderDestroyed)
            : base(turnNumber, GameEventType.CombatOccurred,
                $"Combat: {attackerId} vs {defenderId} - Damage: {damageToAttacker} to attacker, {damageToDefender} to defender")
        {
            this.attackerId = attackerId;
            this.defenderId = defenderId;
            this.damageToAttacker = damageToAttacker;
            this.damageToDefender = damageToDefender;
            this.attackerDestroyed = attackerDestroyed;
            this.defenderDestroyed = defenderDestroyed;
        }
    }

    [Serializable]
    public class ConflictDetectedEvent : GameEvent
    {
        public List<string> unitIds;
        public HexCoord position;

        public ConflictDetectedEvent(int turnNumber, List<string> unitIds, HexCoord position)
            : base(turnNumber, GameEventType.ConflictDetected, $"Conflict detected at {position} with {unitIds.Count} units")
        {
            this.unitIds = unitIds;
            this.position = position;
        }
    }

    [Serializable]
    public class CollisionDetectedEvent : GameEvent
    {
        public List<string> unitIds;
        public HexCoord destination;

        public CollisionDetectedEvent(int turnNumber, List<string> unitIds, HexCoord destination)
            : base(turnNumber, GameEventType.CollisionDetected, $"{unitIds.Count} units colliding at {destination}")
        {
            this.unitIds = unitIds;
            this.destination = destination;
        }
    }

    [Serializable]
    public class CollisionNeedsResolutionEvent : GameEvent
    {
        public CollisionInfo collision;

        public CollisionNeedsResolutionEvent(int turnNumber, CollisionInfo collision)
            : base(turnNumber, GameEventType.CollisionNeedsResolution, $"Collision at {collision.destination} needs yield decisions")
        {
            this.collision = collision;
        }
    }

    [Serializable]
    public class CollisionResolvedEvent : GameEvent
    {
        public List<string> unitIds;
        public HexCoord destination;
        public string resolution;

        public CollisionResolvedEvent(int turnNumber, List<string> unitIds, HexCoord destination, string resolution)
            : base(turnNumber, GameEventType.CollisionResolved, resolution)
        {
            this.unitIds = unitIds;
            this.destination = destination;
            this.resolution = resolution;
        }
    }

    [Serializable]
    public class ShipyardAttackedEvent : GameEvent
    {
        public string attackerUnitId;
        public string shipyardId;
        public int attackingPlayerId;
        public int defendingPlayerId;
        public HexCoord position;
        public int diceRoll;
        public bool success; // true if roll was 5-6

        public ShipyardAttackedEvent(int turnNumber, string attackerUnitId, string shipyardId,
            int attackingPlayerId, int defendingPlayerId, HexCoord position, int diceRoll, bool success)
            : base(turnNumber, GameEventType.ShipyardAttacked,
                success ? $"Player {attackingPlayerId} rolled {diceRoll} and successfully attacked shipyard at {position}!"
                        : $"Player {attackingPlayerId} rolled {diceRoll} and failed to capture shipyard at {position}")
        {
            this.attackerUnitId = attackerUnitId;
            this.shipyardId = shipyardId;
            this.attackingPlayerId = attackingPlayerId;
            this.defendingPlayerId = defendingPlayerId;
            this.position = position;
            this.diceRoll = diceRoll;
            this.success = success;
        }
    }

    [Serializable]
    public class ShipyardDestroyedEvent : GameEvent
    {
        public string shipyardId;
        public int ownerId;
        public HexCoord position;
        public string attackerUnitId; // Unit that destroyed it

        public ShipyardDestroyedEvent(int turnNumber, string shipyardId, int ownerId, HexCoord position, string attackerUnitId)
            : base(turnNumber, GameEventType.ShipyardDestroyed, $"Shipyard {shipyardId} (Player {ownerId}) destroyed at {position}")
        {
            this.shipyardId = shipyardId;
            this.ownerId = ownerId;
            this.position = position;
            this.attackerUnitId = attackerUnitId;
        }
    }

    /// <summary>
    /// NEW: Deterministic structure attack event (replaces dice-based ShipyardAttackedEvent)
    /// </summary>
    [Serializable]
    public class StructureAttackedEvent : GameEvent
    {
        public string attackerUnitId;
        public string structureId;
        public int attackingPlayerId;
        public int defendingPlayerId;
        public HexCoord position;
        public int oldHealth;
        public int newHealth;

        public StructureAttackedEvent(int turnNumber, string attackerUnitId, string structureId,
            int attackingPlayerId, int defendingPlayerId, HexCoord position, int oldHealth, int newHealth)
            : base(turnNumber, GameEventType.StructureAttacked,
                $"Player {attackingPlayerId} attacked shipyard at {position} ({oldHealth} HP → {newHealth} HP)")
        {
            this.attackerUnitId = attackerUnitId;
            this.structureId = structureId;
            this.attackingPlayerId = attackingPlayerId;
            this.defendingPlayerId = defendingPlayerId;
            this.position = position;
            this.oldHealth = oldHealth;
            this.newHealth = newHealth;
        }
    }

    /// <summary>
    /// NEW: Structure ownership changed (capture after health reaches 0)
    /// </summary>
    [Serializable]
    public class StructureCapturedEvent : GameEvent
    {
        public string structureId;
        public int previousOwnerId;
        public int newOwnerId;
        public HexCoord position;
        public string attackerUnitId;

        public StructureCapturedEvent(int turnNumber, string structureId,
            int previousOwnerId, int newOwnerId, HexCoord position, string attackerUnitId)
            : base(turnNumber, GameEventType.StructureCaptured,
                $"Player {newOwnerId} captured shipyard from Player {previousOwnerId} at {position}!")
        {
            this.structureId = structureId;
            this.previousOwnerId = previousOwnerId;
            this.newOwnerId = newOwnerId;
            this.position = position;
            this.attackerUnitId = attackerUnitId;
        }
    }

    [Serializable]
    public class GoldEarnedEvent : GameEvent
    {
        public int playerId;
        public int amount;
        public int baseIncome;
        public int shipyardBonus;
        public int shipBonus;
        public int newTotal;

        public GoldEarnedEvent(int turnNumber, int playerId, int amount, int baseIncome, int shipyardBonus, int shipBonus, int newTotal)
            : base(turnNumber, GameEventType.GoldEarned,
                $"Player {playerId} earned {amount}g (base: {baseIncome}g, shipyards: {shipyardBonus}g, ships: {shipBonus}g) → {newTotal}g")
        {
            this.playerId = playerId;
            this.amount = amount;
            this.baseIncome = baseIncome;
            this.shipyardBonus = shipyardBonus;
            this.shipBonus = shipBonus;
            this.newTotal = newTotal;
        }
    }

    [Serializable]
    public class EncounterDetectedEvent : GameEvent
    {
        public Combat.Encounter encounter;

        public EncounterDetectedEvent(int turnNumber, Combat.Encounter encounter)
            : base(turnNumber, GameEventType.EncounterDetected, $"{encounter.Type} encounter detected: {encounter}")
        {
            this.encounter = encounter;
        }
    }

    [Serializable]
    public class EncounterNeedsResolutionEvent : GameEvent
    {
        public List<Combat.Encounter> encounters;

        public EncounterNeedsResolutionEvent(int turnNumber, List<Combat.Encounter> encounters)
            : base(turnNumber, GameEventType.EncounterNeedsResolution, $"{encounters.Count} encounters need resolution")
        {
            this.encounters = encounters;
        }
    }

    [Serializable]
    public class EncounterResolvedEvent : GameEvent
    {
        public Combat.Encounter encounter;
        public string resolution;

        public EncounterResolvedEvent(int turnNumber, Combat.Encounter encounter, string resolution)
            : base(turnNumber, GameEventType.EncounterResolved, resolution)
        {
            this.encounter = encounter;
            this.resolution = resolution;
        }
    }

    [Serializable]
    public class ContestedTileCreatedEvent : GameEvent
    {
        public HexCoord tileCoord;
        public Combat.Encounter encounter;
        public List<string> attackerIds;

        public ContestedTileCreatedEvent(int turnNumber, HexCoord tileCoord, Combat.Encounter encounter, List<string> attackerIds)
            : base(turnNumber, GameEventType.ContestedTileCreated,
                $"Tile {tileCoord} is now contested by {attackerIds.Count} units")
        {
            this.tileCoord = tileCoord;
            this.encounter = encounter;
            this.attackerIds = attackerIds;
        }
    }

    [Serializable]
    public class ContestedTileResolvedEvent : GameEvent
    {
        public HexCoord tileCoord;
        public string winnerId; // null if no winner

        public ContestedTileResolvedEvent(int turnNumber, HexCoord tileCoord, string winnerId)
            : base(turnNumber, GameEventType.ContestedTileResolved,
                winnerId != null ? $"Tile {tileCoord} claimed by unit {winnerId}" : $"Tile {tileCoord} no longer contested")
        {
            this.tileCoord = tileCoord;
            this.winnerId = winnerId;
        }
    }
}
