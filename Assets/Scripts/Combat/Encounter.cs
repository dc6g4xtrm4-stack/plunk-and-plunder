using System;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.Combat
{
    /// <summary>
    /// Defines the type of naval encounter between enemy ships.
    /// </summary>
    public enum EncounterType
    {
        /// <summary>
        /// Two ships attempting to swap positions (A→B, B→A).
        /// Players choose: PROCEED or ATTACK
        /// </summary>
        PASSING,

        /// <summary>
        /// Two or more ships attempting to enter the same hex tile.
        /// Players choose: YIELD or ATTACK
        /// </summary>
        ENTRY
    }

    /// <summary>
    /// Represents the decision a player can make in a PASSING encounter.
    /// </summary>
    public enum PassingEncounterDecision
    {
        NONE,       // No decision made yet
        PROCEED,    // Allow peaceful swap
        ATTACK      // Initiate combat
    }

    /// <summary>
    /// Represents the decision a player can make in an ENTRY encounter.
    /// </summary>
    public enum EntryEncounterDecision
    {
        NONE,       // No decision made yet
        YIELD,      // Stay in current position, let opponent pass
        ATTACK      // Contest the tile
    }

    /// <summary>
    /// Represents a naval encounter between enemy ships.
    /// Encounters occur when enemy ships would violate the core invariant:
    /// "Two enemy ships may NEVER occupy the same hex tile at any point in time."
    ///
    /// Design Intent:
    /// This system should feel like "Two fleets meet at sea — do you challenge them,
    /// force passage, or back down?" It provides explicit player agency in naval conflicts.
    /// </summary>
    [Serializable]
    public class Encounter
    {
        /// <summary>
        /// Unique identifier for this encounter.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The type of encounter (PASSING or ENTRY).
        /// </summary>
        public EncounterType Type { get; private set; }

        /// <summary>
        /// The turn number when this encounter was created.
        /// </summary>
        public int CreatedOnTurn { get; private set; }

        /// <summary>
        /// IDs of all units involved in this encounter.
        /// For PASSING: exactly 2 units.
        /// For ENTRY: 2 or more units.
        /// </summary>
        public List<string> InvolvedUnitIds { get; private set; }

        /// <summary>
        /// For ENTRY encounters: the tile coordinate that multiple ships are trying to enter.
        /// For PASSING encounters: null (use EdgeCoords instead).
        /// </summary>
        public HexCoord? TileCoord { get; private set; }

        /// <summary>
        /// For PASSING encounters: the two tiles being swapped (edge of conflict).
        /// For ENTRY encounters: null.
        /// EdgeCoords[0] = position of first unit, EdgeCoords[1] = position of second unit.
        /// </summary>
        public (HexCoord, HexCoord)? EdgeCoords { get; private set; }

        /// <summary>
        /// Previous positions of all involved units (before they attempted movement).
        /// Key: unit ID, Value: previous position.
        /// Units remain in these positions if encounter is not resolved.
        /// </summary>
        public Dictionary<string, HexCoord> PreviousPositions { get; private set; }

        /// <summary>
        /// Decisions made by each unit for PASSING encounters.
        /// Key: unit ID, Value: PROCEED or ATTACK.
        /// </summary>
        public Dictionary<string, PassingEncounterDecision> PassingDecisions { get; private set; }

        /// <summary>
        /// Decisions made by each unit for ENTRY encounters.
        /// Key: unit ID, Value: YIELD or ATTACK.
        /// </summary>
        public Dictionary<string, EntryEncounterDecision> EntryDecisions { get; private set; }

        /// <summary>
        /// Whether this encounter is awaiting player choices.
        /// True until all involved players have made their decisions.
        /// </summary>
        public bool AwaitingPlayerChoices { get; private set; }

        /// <summary>
        /// Whether this encounter has been resolved.
        /// Resolved encounters should be removed from active encounter tracking.
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// If this is a contested tile (multiple ATTACK decisions in ENTRY encounter),
        /// this flag marks the tile as persistently contested across turns.
        /// </summary>
        public bool IsContested { get; private set; }

        /// <summary>
        /// Creates a new PASSING encounter.
        /// </summary>
        /// <param name="unitIdA">First unit attempting swap</param>
        /// <param name="unitIdB">Second unit attempting swap</param>
        /// <param name="positionA">Position of first unit</param>
        /// <param name="positionB">Position of second unit</param>
        /// <param name="turnNumber">Current turn number</param>
        public static Encounter CreatePassingEncounter(
            string unitIdA,
            string unitIdB,
            HexCoord positionA,
            HexCoord positionB,
            int turnNumber)
        {
            var encounter = new Encounter
            {
                Id = Guid.NewGuid().ToString(),
                Type = EncounterType.PASSING,
                CreatedOnTurn = turnNumber,
                InvolvedUnitIds = new List<string> { unitIdA, unitIdB },
                TileCoord = null,
                EdgeCoords = (positionA, positionB),
                PreviousPositions = new Dictionary<string, HexCoord>
                {
                    { unitIdA, positionA },
                    { unitIdB, positionB }
                },
                PassingDecisions = new Dictionary<string, PassingEncounterDecision>
                {
                    { unitIdA, PassingEncounterDecision.NONE },
                    { unitIdB, PassingEncounterDecision.NONE }
                },
                EntryDecisions = new Dictionary<string, EntryEncounterDecision>(),
                AwaitingPlayerChoices = true,
                IsResolved = false,
                IsContested = false
            };

            return encounter;
        }

        /// <summary>
        /// Creates a new ENTRY encounter.
        /// </summary>
        /// <param name="unitIds">All units attempting to enter the same tile</param>
        /// <param name="targetTile">The contested destination tile</param>
        /// <param name="previousPositions">Previous positions of all units</param>
        /// <param name="turnNumber">Current turn number</param>
        public static Encounter CreateEntryEncounter(
            List<string> unitIds,
            HexCoord targetTile,
            Dictionary<string, HexCoord> previousPositions,
            int turnNumber)
        {
            var encounter = new Encounter
            {
                Id = Guid.NewGuid().ToString(),
                Type = EncounterType.ENTRY,
                CreatedOnTurn = turnNumber,
                InvolvedUnitIds = new List<string>(unitIds),
                TileCoord = targetTile,
                EdgeCoords = null,
                PreviousPositions = new Dictionary<string, HexCoord>(previousPositions),
                PassingDecisions = new Dictionary<string, PassingEncounterDecision>(),
                EntryDecisions = unitIds.ToDictionary(id => id, id => EntryEncounterDecision.NONE),
                AwaitingPlayerChoices = true,
                IsResolved = false,
                IsContested = false
            };

            return encounter;
        }

        /// <summary>
        /// Records a PASSING decision for a specific unit.
        /// </summary>
        public void RecordPassingDecision(string unitId, PassingEncounterDecision decision)
        {
            if (Type != EncounterType.PASSING)
                throw new InvalidOperationException("Cannot record PASSING decision for non-PASSING encounter");

            if (!InvolvedUnitIds.Contains(unitId))
                throw new ArgumentException($"Unit {unitId} is not involved in this encounter");

            PassingDecisions[unitId] = decision;

            // Check if all decisions are in
            CheckIfAllDecisionsMade();
        }

        /// <summary>
        /// Records an ENTRY decision for a specific unit.
        /// </summary>
        public void RecordEntryDecision(string unitId, EntryEncounterDecision decision)
        {
            if (Type != EncounterType.ENTRY)
                throw new InvalidOperationException("Cannot record ENTRY decision for non-ENTRY encounter");

            if (!InvolvedUnitIds.Contains(unitId))
                throw new ArgumentException($"Unit {unitId} is not involved in this encounter");

            EntryDecisions[unitId] = decision;

            // Check if all decisions are in
            CheckIfAllDecisionsMade();
        }

        /// <summary>
        /// Checks if all involved players have made their decisions.
        /// </summary>
        private void CheckIfAllDecisionsMade()
        {
            if (Type == EncounterType.PASSING)
            {
                AwaitingPlayerChoices = PassingDecisions.Values.Any(d => d == PassingEncounterDecision.NONE);
            }
            else if (Type == EncounterType.ENTRY)
            {
                AwaitingPlayerChoices = EntryDecisions.Values.Any(d => d == EntryEncounterDecision.NONE);
            }
        }

        /// <summary>
        /// Marks this encounter as contested (multiple ATTACK decisions in ENTRY encounter).
        /// Contested tiles persist across turns until resolved.
        /// </summary>
        public void MarkAsContested()
        {
            if (Type != EncounterType.ENTRY)
                throw new InvalidOperationException("Only ENTRY encounters can be contested");

            IsContested = true;
            AwaitingPlayerChoices = false; // Decisions have been made
        }

        /// <summary>
        /// Marks this encounter as resolved.
        /// Resolved encounters should be removed from active tracking.
        /// </summary>
        public void MarkAsResolved()
        {
            IsResolved = true;
            AwaitingPlayerChoices = false;
        }

        /// <summary>
        /// Gets the owner IDs of all involved units.
        /// Requires unit lookup from game state.
        /// </summary>
        public List<int> GetInvolvedOwnerIds(UnitManager unitManager)
        {
            return InvolvedUnitIds
                .Select(unitId => unitManager.GetUnit(unitId))
                .Where(unit => unit != null)
                .Select(unit => unit.ownerId)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Checks if all involved units belong to the same player.
        /// Used to filter out friendly "encounters" (which should not trigger).
        /// </summary>
        public bool AreAllUnitsFromSamePlayer(UnitManager unitManager)
        {
            var ownerIds = GetInvolvedOwnerIds(unitManager);
            return ownerIds.Count == 1;
        }

        /// <summary>
        /// Gets a stable sort key for deterministic encounter ordering.
        /// Encounters are resolved in order of:
        /// 1. Tile coordinate (for ENTRY) or first edge coord (for PASSING)
        /// 2. Encounter type
        /// 3. First unit ID (lexicographic)
        /// </summary>
        public string GetStableSortKey()
        {
            var coordPart = Type == EncounterType.ENTRY
                ? TileCoord.Value.ToString()
                : EdgeCoords.Value.Item1.ToString();

            var typePart = ((int)Type).ToString();
            var unitPart = InvolvedUnitIds.OrderBy(id => id).First();

            return $"{coordPart}_{typePart}_{unitPart}";
        }

        public override string ToString()
        {
            var location = Type == EncounterType.ENTRY
                ? $"tile {TileCoord}"
                : $"edge {EdgeCoords?.Item1} <-> {EdgeCoords?.Item2}";

            return $"{Type} encounter at {location} with units [{string.Join(", ", InvolvedUnitIds)}]";
        }
    }
}
