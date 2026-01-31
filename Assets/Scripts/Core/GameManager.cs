using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.AI;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Networking;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Rendering;
using PlunkAndPlunder.Resolution;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.UI;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Core
{
    /// <summary>
    /// Central game manager - owns state machine and coordinates all systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState state;
        public GameConfig config;

        // Systems
        private TurnResolver turnResolver;
        private TurnAnimator turnAnimator;
        private Pathfinding pathfinding;
        private OrderValidator orderValidator;
        private AIController aiController;
        private NetworkManager networkManager;
        private ConflictResolutionUI conflictResolutionUI;
        private CombatResultsUI combatResultsUI;
        private CollisionYieldUI collisionYieldUI;
        private DiceCombatUI diceCombatUI;
        private PlayerStatsHUD playerStatsHUD;

        // Combat tracking
        private Dictionary<string, int> combatRounds; // Track combat rounds per unit pair

        // Events
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<List<GameEvent>> OnTurnResolved;
        public event Action<GameState> OnGameStateUpdated;

        // Debug
        public bool enableDeterministicLogging = false;
        public int debugMapSeed = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void InitializeGame()
        {
            state = new GameState();
            config = new GameConfig();

            // Initialize file logging
            GameLogger.Initialize();

            Debug.Log("[GameManager] Initialized");
        }

        public void StartOfflineGame(int numPlayers = 4)
        {
            Debug.Log($"[GameManager] Starting offline game with {numPlayers} players");

            // RESET GAME STATE - clear everything from previous games
            state = new GameState();
            state.playerManager = new PlayerManager();
            state.unitManager = new UnitManager();
            state.structureManager = new StructureManager();

            // Setup players
            state.playerManager.AddPlayer("Player 1", PlayerType.Human);
            for (int i = 1; i < numPlayers; i++)
            {
                state.playerManager.AddPlayer($"AI {i}", PlayerType.AI);
            }

            // Generate map with player start islands
            int seed = debugMapSeed != 0 ? debugMapSeed : UnityEngine.Random.Range(0, int.MaxValue);
            state.mapSeed = seed;
            MapGenerator mapGen = new MapGenerator(seed);
            state.grid = mapGen.GenerateMap(config.numSeaTiles, config.numIslands, config.minIslandSize, config.maxIslandSize, numPlayers);

            Debug.Log($"[GameManager] Map generated with {state.grid.playerStartIslands.Count} player start islands");

            // Initialize systems
            pathfinding = new Pathfinding(state.grid);
            orderValidator = new OrderValidator(state.grid, state.unitManager, state.structureManager, state.playerManager);
            turnResolver = new TurnResolver(state.grid, state.unitManager, state.playerManager, state.structureManager, enableDeterministicLogging);
            aiController = new AIController(state.grid, state.unitManager, state.playerManager, state.structureManager, pathfinding);

            // Initialize turn animator (add component if not present)
            turnAnimator = GetComponent<TurnAnimator>();
            if (turnAnimator == null)
            {
                turnAnimator = gameObject.AddComponent<TurnAnimator>();
            }
            turnAnimator.Initialize(state.unitManager);
            turnAnimator.OnAnimationStep += HandleAnimationStep;
            turnAnimator.OnAnimationComplete += HandleAnimationComplete;
            turnAnimator.OnConflictDetected += HandleConflictDetected;
            turnAnimator.OnCombatOccurred += HandleCombatOccurred;

            // Initialize conflict resolution UI and combat results UI
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject conflictUIObj = new GameObject("ConflictResolutionUI");
                conflictUIObj.transform.SetParent(canvas.transform, false);
                conflictResolutionUI = conflictUIObj.AddComponent<ConflictResolutionUI>();
                conflictResolutionUI.Initialize();

                GameObject combatResultsUIObj = new GameObject("CombatResultsUI");
                combatResultsUIObj.transform.SetParent(canvas.transform, false);
                combatResultsUI = combatResultsUIObj.AddComponent<CombatResultsUI>();
                combatResultsUI.Initialize();

                GameObject collisionYieldUIObj = new GameObject("CollisionYieldUI");
                collisionYieldUIObj.transform.SetParent(canvas.transform, false);
                collisionYieldUI = collisionYieldUIObj.AddComponent<CollisionYieldUI>();
                collisionYieldUI.Initialize(0); // Local player ID (human player is always 0 in offline mode)

                GameObject diceCombatUIObj = new GameObject("DiceCombatUI");
                diceCombatUIObj.transform.SetParent(canvas.transform, false);
                diceCombatUI = diceCombatUIObj.AddComponent<DiceCombatUI>();
                diceCombatUI.Initialize();

                GameObject playerStatsHUDObj = new GameObject("PlayerStatsHUD");
                playerStatsHUDObj.transform.SetParent(canvas.transform, false);
                playerStatsHUD = playerStatsHUDObj.AddComponent<PlayerStatsHUD>();
                playerStatsHUD.Initialize();
            }

            // Place harbors
            PlaceHarbors();

            // Place starting shipyards for each player (must be before units)
            PlaceStartingShipyards();

            // Place starting units for each player near their shipyards
            PlaceStartingUnits();

            // Trigger initial render
            OnGameStateUpdated?.Invoke(state);

            // Update player stats HUD
            if (playerStatsHUD != null)
            {
                playerStatsHUD.UpdateStats(state);
                playerStatsHUD.Show();
            }

            // Transition to game
            ChangePhase(GamePhase.Planning);
        }

        private void PlaceStartingUnits()
        {
            Debug.Log($"[GameManager] ===== PLACING STARTING UNITS (3 ships per player) =====");

            // Place 3 starting ships for each player using their start island spawn positions
            for (int i = 0; i < state.playerManager.players.Count; i++)
            {
                Player player = state.playerManager.players[i];

                if (i >= state.grid.playerStartIslands.Count)
                {
                    Debug.LogError($"[GameManager] No start island found for player {player.id}");
                    continue;
                }

                PlayerStartIsland startIsland = state.grid.playerStartIslands[i];

                Debug.Log($"[GameManager] Player {player.id} ({player.name}) start island has {startIsland.shipSpawnPositions.Count} spawn positions");

                // Place 3 ships at the designated spawn positions
                int shipsToPlace = Mathf.Min(3, startIsland.shipSpawnPositions.Count);
                for (int j = 0; j < shipsToPlace; j++)
                {
                    HexCoord spawnPos = startIsland.shipSpawnPositions[j];
                    Unit ship = state.unitManager.CreateUnit(player.id, spawnPos, UnitType.SHIP);
                    Debug.Log($"[GameManager]   Ship {ship.id} spawned at {spawnPos}");
                }

                Debug.Log($"[GameManager] Player {player.id} starts with {shipsToPlace} ships near shipyard at {startIsland.shipyardPosition}");
            }

            Debug.Log($"[GameManager] Placed {state.unitManager.units.Count} total starting units");
        }

        private void PlaceHarbors()
        {
            Debug.Log($"[GameManager] ===== PLACING HARBOUR STRUCTURES =====");

            // Harbors are already part of the map as HARBOR tiles
            // Create neutral harbor structures for ALL harbours on the map (including player start islands)
            List<Tile> harborTiles = state.grid.GetTilesOfType(TileType.HARBOR);

            foreach (Tile harborTile in harborTiles)
            {
                // Create neutral harbour structure (will be converted to shipyard for player start harbours)
                state.structureManager.CreateStructure(-1, harborTile.coord, StructureType.HARBOR);
            }

            Debug.Log($"[GameManager] Placed {harborTiles.Count} neutral harbour structures");
        }

        private void PlaceStartingShipyards()
        {
            Debug.Log($"[GameManager] ===== PLACING STARTING SHIPYARDS (using player start islands) =====");

            int shipyardsPlaced = 0;

            // Use player start islands to place shipyards
            for (int i = 0; i < state.playerManager.players.Count; i++)
            {
                Player player = state.playerManager.players[i];

                if (i >= state.grid.playerStartIslands.Count)
                {
                    Debug.LogError($"[GameManager] No start island found for player {player.id}");
                    continue;
                }

                PlayerStartIsland startIsland = state.grid.playerStartIslands[i];
                HexCoord shipyardCoord = startIsland.shipyardPosition;

                Debug.Log($"[GameManager] Player {player.id} ({player.name}) start island: {startIsland.landTiles.Count} land, {startIsland.harbourTiles.Count} harbours, shipyard at {shipyardCoord}");

                // Get the existing harbor structure at the shipyard position and CONVERT it to a shipyard
                Structure harbor = state.structureManager.GetStructureAtPosition(shipyardCoord);
                if (harbor != null && harbor.type == StructureType.HARBOR)
                {
                    // Convert the harbor to a shipyard by changing its type and owner
                    harbor.type = StructureType.SHIPYARD;
                    harbor.ownerId = player.id;
                    shipyardsPlaced++;

                    Debug.Log($"[GameManager]   Shipyard structure {harbor.id} created at {shipyardCoord} (converted from harbour)");
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Could not find harbor structure at {shipyardCoord} for player {player.id}");
                }
            }

            Debug.Log($"[GameManager] Placed {shipyardsPlaced} starting shipyards on player start islands");
        }

        public void ChangePhase(GamePhase newPhase)
        {
            GamePhase oldPhase = state.phase;
            state.phase = newPhase;
            Debug.Log($"[GameManager] ===== PHASE CHANGED: {oldPhase} -> {newPhase} =====");
            GameLogger.LogPhaseChange(oldPhase, newPhase);

            OnPhaseChanged?.Invoke(newPhase);

            // Handle phase-specific logic
            switch (newPhase)
            {
                case GamePhase.Planning:
                    StartPlanningPhase();
                    break;

                case GamePhase.Resolving:
                    ResolveCurrentTurn();
                    break;

                case GamePhase.Animating:
                    // Animation will be started by ResolveCurrentTurn
                    break;
            }
        }

        private void StartPlanningPhase()
        {
            state.turnNumber++;
            state.pendingOrders.Clear();

            GameLogger.LogTurnStart(state.turnNumber);

            // Reset ready status and award gold
            foreach (Player player in state.playerManager.players)
            {
                player.isReady = false;

                // Award gold: 100 doubloons per shipyard per turn
                if (!player.isEliminated)
                {
                    int shipyardCount = state.structureManager.GetStructuresForPlayer(player.id)
                        .FindAll(s => s.type == Structures.StructureType.SHIPYARD).Count;

                    int goldEarned = shipyardCount * 100;
                    player.gold += goldEarned;

                    Debug.Log($"[GameManager] Player {player.id} earned {goldEarned} gold from {shipyardCount} shipyard(s). Total: {player.gold}");
                    GameLogger.LogPlayerAction(player.id, $"Earned {goldEarned} gold from {shipyardCount} shipyard(s). Total: {player.gold}");
                }
            }

            Debug.Log($"[GameManager] Turn {state.turnNumber} - Planning phase started");
            GameLogger.LogGameState(state);

            // Update player stats HUD after awarding gold
            if (playerStatsHUD != null)
            {
                playerStatsHUD.UpdateStats(state);
            }

            // COMBAT PATH QUEUE: Set default attack paths for units in ongoing combat
            // When combat occurs, units should automatically continue attacking unless player changes orders
            foreach (Unit unit in state.unitManager.GetAllUnits())
            {
                if (unit.isInCombat && unit.combatOpponentId != null)
                {
                    Unit opponent = state.unitManager.GetUnit(unit.combatOpponentId);
                    if (opponent != null && opponent.isInCombat)
                    {
                        // Set queued path to continue attacking opponent
                        // This creates a default path that shows the unit will continue combat
                        List<HexCoord> attackPath = new List<HexCoord> { unit.position, opponent.position };
                        unit.queuedPath = attackPath;

                        Debug.Log($"[GameManager] Unit {unit.id} in ongoing combat with {opponent.id} - queued default attack path {unit.position} -> {opponent.position}");
                        GameLogger.LogPlayerAction(unit.ownerId, $"Unit {unit.id} continues combat with {opponent.id} at {opponent.position}");
                    }
                    else
                    {
                        // Opponent destroyed or not in combat anymore - clear combat flags
                        unit.isInCombat = false;
                        unit.combatOpponentId = null;
                        unit.queuedPath = null;

                        Debug.Log($"[GameManager] Unit {unit.id} combat ended (opponent destroyed or withdrew)");
                    }
                }
            }

            // Initialize player view on first turn
            if (state.turnNumber == 1)
            {
                InitializePlayerView();
            }

            // Trigger AI planning
            if (aiController != null)
            {
                foreach (Player aiPlayer in state.playerManager.GetAIPlayers())
                {
                    List<IOrder> aiOrders = aiController.PlanTurn(aiPlayer.id);
                    SubmitOrders(aiPlayer.id, aiOrders);
                }
            }
        }

        public void SubmitOrders(int playerId, List<IOrder> orders)
        {
            Player player = state.playerManager.GetPlayer(playerId);
            if (player == null || player.isEliminated)
                return;

            // Validate orders
            List<IOrder> validOrders = new List<IOrder>();
            foreach (IOrder order in orders)
            {
                bool isValid = false;
                string error = "";

                switch (order)
                {
                    case MoveOrder moveOrder:
                        isValid = orderValidator.ValidateMoveOrder(moveOrder, out error);
                        break;
                    case DeployShipyardOrder deployOrder:
                        isValid = orderValidator.ValidateDeployShipyardOrder(deployOrder, out error);
                        break;
                    case BuildShipOrder buildOrder:
                        isValid = orderValidator.ValidateBuildShipOrder(buildOrder, out error);
                        break;
                    case RepairShipOrder repairOrder:
                        isValid = orderValidator.ValidateRepairShipOrder(repairOrder, out error);
                        break;
                    case UpgradeShipOrder upgradeOrder:
                        isValid = orderValidator.ValidateUpgradeShipOrder(upgradeOrder, out error);
                        break;
                    case AttackShipyardOrder attackOrder:
                        isValid = orderValidator.ValidateAttackShipyardOrder(attackOrder, out error);
                        break;
                }

                if (isValid)
                {
                    validOrders.Add(order);
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Invalid {order.GetOrderType()} order from Player {playerId}: {error}");
                }
            }

            state.pendingOrders[playerId] = validOrders;
            player.isReady = true;

            Debug.Log($"[GameManager] Player {playerId} submitted {validOrders.Count} orders");

            // Check if all players ready
            if (state.playerManager.AllPlayersReady())
            {
                ChangePhase(GamePhase.Resolving);
            }
        }

        private void ResolveCurrentTurn()
        {
            Debug.Log($"[GameManager] Resolving turn {state.turnNumber}");

            // Collect all orders
            List<IOrder> allOrders = new List<IOrder>();
            foreach (var kvp in state.pendingOrders)
            {
                allOrders.AddRange(kvp.Value);
            }

            // Resolve
            List<GameEvent> events = turnResolver.ResolveTurn(allOrders, state.turnNumber);
            state.eventHistory.AddRange(events);

            Debug.Log($"[GameManager] Turn resolved with {events.Count} events");

            OnTurnResolved?.Invoke(events);

            // Check if any collisions need resolution
            bool hasCollisions = false;
            state.pendingCollisions.Clear();

            foreach (GameEvent evt in events)
            {
                if (evt is CollisionNeedsResolutionEvent collisionEvent)
                {
                    hasCollisions = true;
                    state.pendingCollisions.Add(collisionEvent.collision);
                }
            }

            // If collisions detected, transition to collision resolution phase
            if (hasCollisions)
            {
                Debug.Log($"[GameManager] {state.pendingCollisions.Count} collision(s) detected, waiting for yield decisions");
                state.collisionYieldDecisions.Clear();
                ChangePhase(GamePhase.CollisionResolution);

                // Show collision yield UI for players to make decisions
                if (collisionYieldUI != null)
                {
                    collisionYieldUI.ShowCollisions(state.pendingCollisions);
                }

                // AI players automatically make decisions
                MakeAIYieldDecisions();
            }
            else
            {
                // No collisions, proceed to animation
                Debug.Log($"[GameManager] No collisions detected, proceeding to animation with {events.Count} events");
                ChangePhase(GamePhase.Animating);
                turnAnimator.AnimateEvents(events, state);
            }
        }

        private void HandleAnimationStep(GameState updatedState)
        {
            Debug.Log("[GameManager] HandleAnimationStep called - triggering OnGameStateUpdated");
            // Trigger visual update after each animation step
            OnGameStateUpdated?.Invoke(updatedState);

            // Update player stats HUD
            if (playerStatsHUD != null)
            {
                playerStatsHUD.UpdateStats(updatedState);
            }
        }

        private void HandleAnimationComplete()
        {
            Debug.Log("[GameManager] Animation complete, transitioning to next phase");

            // Reset auto-resolve flag
            if (isAutoResolving)
            {
                Debug.Log("[GameManager] Auto-resolve turn complete");
                isAutoResolving = false;
            }

            // Final state update
            OnGameStateUpdated?.Invoke(state);

            // Update player stats HUD
            if (playerStatsHUD != null)
            {
                playerStatsHUD.UpdateStats(state);
            }

            // Check for game over
            if (state.playerManager.GetWinner() != null)
            {
                ChangePhase(GamePhase.GameOver);
            }
            else
            {
                // Next turn
                ChangePhase(GamePhase.Planning);
            }
        }

        /// <summary>
        /// Submit a yield decision for a unit involved in a collision
        /// </summary>
        public void SubmitYieldDecision(string unitId, bool isYielding)
        {
            if (state.phase != GamePhase.CollisionResolution)
            {
                Debug.LogWarning($"[GameManager] Cannot submit yield decision - not in CollisionResolution phase");
                return;
            }

            state.collisionYieldDecisions[unitId] = isYielding;
            Debug.Log($"[GameManager] Unit {unitId} yield decision: {isYielding}");

            // Check if all decisions are collected
            if (AllYieldDecisionsCollected())
            {
                ContinueResolutionWithYieldDecisions();
            }
        }

        private bool AllYieldDecisionsCollected()
        {
            // Get all unit IDs involved in collisions
            HashSet<string> allUnitsInCollisions = new HashSet<string>();
            foreach (CollisionInfo collision in state.pendingCollisions)
            {
                foreach (string unitId in collision.unitIds)
                {
                    allUnitsInCollisions.Add(unitId);
                }
            }

            Debug.Log($"[GameManager] Checking yield decisions: {state.collisionYieldDecisions.Count}/{allUnitsInCollisions.Count} units decided");

            // Check if all units have submitted decisions
            foreach (string unitId in allUnitsInCollisions)
            {
                if (!state.collisionYieldDecisions.ContainsKey(unitId))
                {
                    Debug.Log($"[GameManager] Still waiting for decision from unit {unitId}");
                    return false;
                }
            }

            Debug.Log($"[GameManager] All yield decisions collected!");
            return true;
        }

        private void MakeAIYieldDecisions()
        {
            // AI players automatically make yield decisions based on ship health
            foreach (CollisionInfo collision in state.pendingCollisions)
            {
                foreach (string unitId in collision.unitIds)
                {
                    Unit unit = state.unitManager.GetUnit(unitId);
                    if (unit == null) continue;

                    Player player = state.playerManager.GetPlayer(unit.ownerId);
                    if (player == null || player.type == PlayerType.Human) continue;

                    // AI logic: yield if health is below 50%, otherwise push through
                    bool shouldYield = unit.health < (unit.maxHealth * 0.5f);

                    // Submit AI decision
                    SubmitYieldDecision(unitId, shouldYield);

                    Debug.Log($"[GameManager] AI Player {unit.ownerId} unit {unitId}: {(shouldYield ? "yielding" : "pushing")} (HP: {unit.health}/{unit.maxHealth})");
                }
            }
        }

        private void ContinueResolutionWithYieldDecisions()
        {
            Debug.Log($"[GameManager] All yield decisions collected, resolving collisions");

            // Resolve collisions with yield decisions
            List<GameEvent> collisionEvents = turnResolver.ResolveCollisionsWithYieldDecisions(
                state.pendingCollisions,
                state.collisionYieldDecisions
            );

            state.eventHistory.AddRange(collisionEvents);
            OnTurnResolved?.Invoke(collisionEvents);

            // Clear collision data
            state.pendingCollisions.Clear();
            state.collisionYieldDecisions.Clear();

            // After combat, check for ongoing combats and store them
            state.ongoingCombats.Clear();
            List<Unit> allUnits = state.unitManager.GetAllUnits();
            HashSet<string> processedUnits = new HashSet<string>();

            foreach (Unit unit in allUnits)
            {
                if (unit.isInCombat && !processedUnits.Contains(unit.id) && unit.combatOpponentId != null)
                {
                    Unit opponent = state.unitManager.GetUnit(unit.combatOpponentId);
                    if (opponent != null && opponent.isInCombat)
                    {
                        // Create ongoing combat entry
                        OngoingCombat ongoingCombat = new OngoingCombat(
                            unit.id,
                            opponent.id,
                            unit.position,
                            opponent.position
                        );
                        state.ongoingCombats.Add(ongoingCombat);

                        // Mark both as processed
                        processedUnits.Add(unit.id);
                        processedUnits.Add(opponent.id);

                        if (enableDeterministicLogging)
                        {
                            Debug.Log($"[GameManager] Ongoing combat tracked: {unit.id} at {unit.position} vs {opponent.id} at {opponent.position}");
                        }
                    }
                }
            }

            // Continue with regular combat resolution (for units not in collisions)
            List<GameEvent> combatEvents = turnResolver.ResolveCombatAfterMovement();
            state.eventHistory.AddRange(combatEvents);

            // Combine all events for animation
            List<GameEvent> allEvents = new List<GameEvent>(state.eventHistory);

            // Transition to animation
            Debug.Log($"[GameManager] Collisions resolved, proceeding to animation with {allEvents.Count} total events");
            ChangePhase(GamePhase.Animating);
            turnAnimator.AnimateEvents(allEvents, state);
        }

        private void HandleConflictDetected(ConflictDetectedEvent conflictEvent)
        {
            Debug.Log($"[GameManager] Conflict detected at {conflictEvent.position}");

            // Pause animation
            turnAnimator.PauseAnimation();

            // Gather involved units
            List<Unit> involvedUnits = new List<Unit>();
            foreach (string unitId in conflictEvent.unitIds)
            {
                Unit unit = state.unitManager.GetUnit(unitId);
                if (unit != null)
                {
                    involvedUnits.Add(unit);
                }
            }

            // Show conflict resolution UI
            ConflictData conflictData = new ConflictData(involvedUnits, conflictEvent.position);
            if (conflictResolutionUI != null)
            {
                conflictResolutionUI.ShowConflict(conflictData, OnConflictResolved);
            }
        }

        private void OnConflictResolved(ConflictResolution resolution)
        {
            Debug.Log($"[GameManager] Conflict resolved: {resolution}");

            if (resolution == ConflictResolution.Reroute)
            {
                // Cancel turn animation and return to planning phase
                Debug.Log("[GameManager] Re-routing - cancelling turn and returning to planning");

                // Stop animation
                if (turnAnimator != null)
                {
                    StopAllCoroutines();
                }

                // Clear pending orders
                state.pendingOrders.Clear();

                // Return to planning phase
                ChangePhase(GamePhase.Planning);
            }
            else if (resolution == ConflictResolution.Combat)
            {
                // Continue with combat - resume animation
                Debug.Log("[GameManager] Continuing to combat");
                turnAnimator.ResumeAnimation();
            }
        }

        private void HandleCombatOccurred(CombatOccurredEvent combatEvent)
        {
            Debug.Log($"[GameManager] Combat occurred: {combatEvent.attackerId} vs {combatEvent.defenderId}");

            // Pause animation
            turnAnimator.PauseAnimation();

            // Get units involved (may be destroyed by combat, so check)
            Unit attacker = state.unitManager.GetUnit(combatEvent.attackerId);
            Unit defender = state.unitManager.GetUnit(combatEvent.defenderId);

            // Track combat round for this pair
            if (combatRounds == null)
            {
                combatRounds = new Dictionary<string, int>();
            }

            string combatKey = $"{combatEvent.attackerId}_{combatEvent.defenderId}";
            if (!combatRounds.ContainsKey(combatKey))
            {
                combatRounds[combatKey] = 1;
            }
            int roundNumber = combatRounds[combatKey];

            if (attacker == null || defender == null)
            {
                Debug.LogWarning($"[GameManager] Could not find units for combat: {combatEvent.attackerId} or {combatEvent.defenderId}");

                // If units are destroyed, clear the combat round tracking
                combatRounds.Remove(combatKey);

                turnAnimator.ResumeAnimation();
                return;
            }

            // Check if human player is involved in combat
            bool humanInvolved = (attacker != null && attacker.ownerId == 0) || (defender != null && defender.ownerId == 0);

            // If auto-resolving OR no human involved, skip UI and auto-continue
            if (isAutoResolving || !humanInvolved)
            {
                if (!humanInvolved)
                {
                    Debug.Log($"[GameManager] AI vs AI combat: {attacker?.id ?? "?"} (P{attacker?.ownerId}) vs {defender?.id ?? "?"} (P{defender?.ownerId}) - skipping UI");
                }
                else
                {
                    Debug.Log("[GameManager] Auto-resolve: skipping combat UI, auto-continuing");
                }
                StartCoroutine(AutoContinueCombat());
            }
            else
            {
                // Show dice combat UI (human player involved)
                if (diceCombatUI != null)
                {
                    diceCombatUI.ShowCombat(combatEvent, attacker, defender, roundNumber, OnCombatResultsContinue, state.playerManager);

                    // Increment round number for next combat between these units
                    combatRounds[combatKey] = roundNumber + 1;
                }
                else
                {
                    // Fallback to old UI if dice UI not available
                    if (combatResultsUI != null)
                    {
                        combatResultsUI.ShowCombatResults(combatEvent, attacker, defender, OnCombatResultsContinue, state.playerManager);
                    }
                }
            }

            // Award salvage gold for destroyed ships (50% of build cost = 25g for basic ships)
            const int SALVAGE_VALUE = 25; // 50% of 50g build cost
            if (combatEvent.defenderDestroyed && attacker != null)
            {
                // Attacker destroyed defender - award salvage to attacker's owner
                state.playerManager.GetPlayer(attacker.ownerId).gold += SALVAGE_VALUE;
                Debug.Log($"[GameManager] Player {attacker.ownerId} earned {SALVAGE_VALUE}g salvage from destroying {defender?.id ?? combatEvent.defenderId}");
            }
            else if (combatEvent.attackerDestroyed && defender != null)
            {
                // Defender destroyed attacker - award salvage to defender's owner
                state.playerManager.GetPlayer(defender.ownerId).gold += SALVAGE_VALUE;
                Debug.Log($"[GameManager] Player {defender.ownerId} earned {SALVAGE_VALUE}g salvage from destroying {attacker?.id ?? combatEvent.attackerId}");
            }

            // If either unit is destroyed, clear the combat tracking
            if (combatEvent.attackerDestroyed || combatEvent.defenderDestroyed)
            {
                combatRounds.Remove(combatKey);
            }
        }

        private IEnumerator AutoContinueCombat()
        {
            // Wait briefly to simulate combat happening
            yield return new WaitForSeconds(0.1f);
            Debug.Log("[GameManager] Auto-resolve: continuing after combat");
            turnAnimator.ResumeAnimation();
        }

        private void OnCombatResultsContinue()
        {
            Debug.Log("[GameManager] Player acknowledged combat results, resuming animation");
            turnAnimator.ResumeAnimation();
        }

        public Pathfinding GetPathfinding() => pathfinding;

        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // Debug method for testing
        private bool isAutoResolving = false;

        public void AutoResolveTurn()
        {
            try
            {
                if (state == null)
                {
                    Debug.LogError("[GameManager] Cannot auto-resolve: state is null");
                    return;
                }

                if (state.phase == GamePhase.Planning)
                {
                    Debug.Log("[GameManager] Auto-resolving turn - submitting empty orders for all players");
                    isAutoResolving = true;

                    // Auto-submit for all non-ready players
                    foreach (Player player in state.playerManager.GetActivePlayers())
                    {
                        if (!player.isReady)
                        {
                            SubmitOrders(player.id, new List<IOrder>());
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Cannot auto-resolve during {state.phase} phase");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] Auto-resolve failed: {ex.Message}\n{ex.StackTrace}");
                isAutoResolving = false;
            }
        }

        /// <summary>
        /// Initialize camera and highlight player assets at game start
        /// Called on first turn only
        /// </summary>
        private void InitializePlayerView()
        {
            // Find Player 0 (human player)
            Player humanPlayer = state.playerManager.GetPlayer(0);
            if (humanPlayer == null)
            {
                Debug.LogWarning("[GameManager] Could not find human player (Player 0) for camera initialization");
                return;
            }

            Debug.Log($"[GameManager] Initializing view for {humanPlayer.name} (Player {humanPlayer.id})");

            // Find player's shipyard
            Structure playerShipyard = null;
            foreach (Structure structure in state.structureManager.GetAllStructures())
            {
                if (structure.type == StructureType.SHIPYARD && structure.ownerId == humanPlayer.id)
                {
                    playerShipyard = structure;
                    break;
                }
            }

            if (playerShipyard != null)
            {
                // Move camera to focus on player's shipyard
                Vector3 shipyardWorldPos = playerShipyard.position.ToWorldPosition(1f); // hexSize = 1f
                CameraController cameraController = Camera.main?.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.FocusOnPosition(shipyardWorldPos, smooth: true);
                    Debug.Log($"[GameManager] Camera focusing on player shipyard at {playerShipyard.position}");
                }

                // Highlight player's shipyard
                BuildingRenderer buildingRenderer = FindObjectOfType<BuildingRenderer>();
                if (buildingRenderer != null)
                {
                    buildingRenderer.AddTemporaryHighlight(playerShipyard.id, duration: 5f);
                    Debug.Log($"[GameManager] Highlighting player shipyard");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] Could not find player's shipyard for camera initialization");
            }

            // Highlight player's ships
            UnitRenderer unitRenderer = FindObjectOfType<UnitRenderer>();
            if (unitRenderer != null)
            {
                int highlightedShips = 0;
                foreach (Unit unit in state.unitManager.GetAllUnits())
                {
                    if (unit.ownerId == humanPlayer.id)
                    {
                        unitRenderer.AddTemporaryHighlight(unit.id, duration: 5f);
                        highlightedShips++;
                    }
                }
                Debug.Log($"[GameManager] Highlighted {highlightedShips} player ships");
            }
        }

        private void OnApplicationQuit()
        {
            // Shutdown the game logger
            GameLogger.Shutdown();
        }
    }

    [Serializable]
    public class GameConfig
    {
        public int numSeaTiles = 1500; // Tripled from 500 for much more sea area
        public int numIslands = 40; // Increased from 25 for proportional coverage
        public int minIslandSize = 4;
        public int maxIslandSize = 8;
    }
}
