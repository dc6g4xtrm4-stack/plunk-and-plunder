using System;
using System.Collections.Generic;
using PlunkAndPlunder.AI;
using PlunkAndPlunder.Construction;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Resolution;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.Core
{
    /// <summary>
    /// Pure C# class - Core deterministic game engine
    /// Contains ALL game rule logic, no Unity dependencies
    /// Used by GameManager (offline UI), HeadlessSimulation (batch), and NetworkManager (multiplayer)
    /// </summary>
    public class GameEngine
    {
        // State (read-only from outside)
        public GameState State { get; private set; }
        public GameConfig Config { get; private set; }

        // Core systems (owned by engine)
        private TurnResolver turnResolver;
        private OrderValidator orderValidator;
        private AIController aiController;
        private Pathfinding pathfinding;

        // Events (consumers subscribe)
        public event Action<GameState> OnStateChanged;
        public event Action<int> OnTurnStarted; // turnNumber
        public event Action<List<GameEvent>> OnEventsGenerated;
        public event Action<List<CollisionInfo>> OnCollisionsDetected;
        public event Action<int> OnPlayerEliminated; // playerId
        public event Action<int> OnGameWon; // winnerId

        // Lifecycle
        public GameEngine(GameConfig config)
        {
            this.Config = config;
            this.State = new GameState();
        }

        /// <summary>
        /// Initialize a new game with players
        /// </summary>
        public void InitializeGame(List<PlayerConfig> players, int? mapSeed = null)
        {
            // Reset state
            State = new GameState();
            State.playerManager = new PlayerManager();
            State.unitManager = new UnitManager();
            State.structureManager = new StructureManager();

            // Setup players
            foreach (var playerConfig in players)
            {
                State.playerManager.AddPlayer(playerConfig.name, playerConfig.type);
            }

            // Generate map
            int seed = mapSeed ?? UnityEngine.Random.Range(0, int.MaxValue);
            State.mapSeed = seed;
            MapGenerator mapGen = new MapGenerator(seed);
            State.grid = mapGen.GenerateMap(
                Config.numSeaTiles,
                Config.numIslands,
                Config.minIslandSize,
                Config.maxIslandSize,
                players.Count
            );

            // Initialize systems
            pathfinding = new Pathfinding(State.grid);
            orderValidator = new OrderValidator(State.grid, State.unitManager, State.structureManager, State.playerManager);
            turnResolver = new TurnResolver(State.grid, State.unitManager, State.playerManager, State.structureManager, enableLogging: true, deferUnitRemoval: true);
            aiController = new AIController(State.grid, State.unitManager, State.playerManager, State.structureManager, pathfinding);

            // Place starting structures and units
            GameInitializer.PlaceStartingShipyards(State);
            GameInitializer.PlaceStartingUnits(State);

            // Initialize ConstructionManager with state
            if (ConstructionManager.Instance != null)
            {
                ConstructionManager.Instance.Reset();
                ConstructionManager.Instance.Initialize(State);
            }

            OnStateChanged?.Invoke(State);
        }

        /// <summary>
        /// Submit orders for a player
        /// Returns: true if all players ready, false if waiting
        /// </summary>
        public bool SubmitOrders(int playerId, List<IOrder> orders)
        {
            Player player = State.playerManager.GetPlayer(playerId);
            if (player == null || player.isEliminated)
                return false;

            // Validate orders
            List<IOrder> validOrders = ValidateOrders(playerId, orders);

            // Store validated orders
            State.pendingOrders[playerId] = validOrders;
            player.isReady = true;

            // Check if all players ready
            return State.playerManager.AllPlayersReady();
        }

        /// <summary>
        /// Start a new turn (advance turn counter, award income, process construction)
        /// Use this in planning phase before collecting orders
        /// </summary>
        public List<GameEvent> StartTurn()
        {
            // Advance turn
            State.turnNumber++;
            OnTurnStarted?.Invoke(State.turnNumber);

            // Award income
            AwardIncome();

            // Process construction queues
            var constructionEvents = ProcessConstruction();

            State.eventHistory.AddRange(constructionEvents);
            OnEventsGenerated?.Invoke(constructionEvents);
            OnStateChanged?.Invoke(State);

            return constructionEvents;
        }

        /// <summary>
        /// Resolve submitted orders (generates AI orders for non-ready players, then resolves)
        /// Use this after all orders are collected
        /// </summary>
        public TurnResult ResolveTurn()
        {
            // Generate AI orders for non-ready players
            GenerateAIOrders();

            // Resolve all orders
            var resolutionEvents = ResolveOrders();

            // Check for collisions
            var collisions = ExtractCollisions(resolutionEvents);

            State.eventHistory.AddRange(resolutionEvents);
            OnEventsGenerated?.Invoke(resolutionEvents);
            OnStateChanged?.Invoke(State);

            // Check win condition
            Player winner = State.playerManager.GetWinner();
            if (winner != null)
            {
                OnGameWon?.Invoke(winner.id);
            }

            return new TurnResult
            {
                turnNumber = State.turnNumber,
                events = resolutionEvents,
                collisions = collisions,
                winner = winner
            };
        }

        /// <summary>
        /// Process a complete turn (advance turn, award income, generate AI orders, resolve)
        /// Returns: events generated this turn
        /// </summary>
        public TurnResult ProcessTurn()
        {
            // Advance turn
            State.turnNumber++;
            OnTurnStarted?.Invoke(State.turnNumber);

            // Award income
            AwardIncome();

            // Process construction queues
            var constructionEvents = ProcessConstruction();

            // Generate AI orders for non-ready players
            GenerateAIOrders();

            // Resolve all orders
            var resolutionEvents = ResolveOrders();

            // Check for collisions
            var collisions = ExtractCollisions(resolutionEvents);

            // Combine events
            var allEvents = new List<GameEvent>();
            allEvents.AddRange(constructionEvents);
            allEvents.AddRange(resolutionEvents);

            State.eventHistory.AddRange(allEvents);

            // Emit events
            OnEventsGenerated?.Invoke(allEvents);
            OnStateChanged?.Invoke(State);

            // Check win condition
            Player winner = State.playerManager.GetWinner();
            if (winner != null)
            {
                OnGameWon?.Invoke(winner.id);
            }

            return new TurnResult
            {
                turnNumber = State.turnNumber,
                events = allEvents,
                collisions = collisions,
                winner = winner
            };
        }

        /// <summary>
        /// Resolve collisions with player decisions
        /// </summary>
        public List<GameEvent> ResolveCollisions(Dictionary<string, bool> yieldDecisions)
        {
            var events = turnResolver.ResolveCollisionsWithYieldDecisions(
                State.pendingCollisions,
                yieldDecisions
            );

            // Continue with combat after collisions resolved
            var combatEvents = turnResolver.ResolveCombatAfterMovement();
            events.AddRange(combatEvents);

            State.eventHistory.AddRange(events);
            OnEventsGenerated?.Invoke(events);
            OnStateChanged?.Invoke(State);

            return events;
        }

        /// <summary>
        /// Get AI yield decisions for collisions (for auto-resolve)
        /// </summary>
        public Dictionary<string, bool> GetAIYieldDecisions(List<CollisionInfo> collisions)
        {
            var decisions = new Dictionary<string, bool>();

            foreach (var collision in collisions)
            {
                foreach (string unitId in collision.unitIds)
                {
                    Unit unit = State.unitManager.GetUnit(unitId);
                    if (unit == null) continue;

                    Player player = State.playerManager.GetPlayer(unit.ownerId);
                    if (player == null || player.type == PlayerType.Human) continue;

                    // AI logic: yield if health below 50%
                    bool shouldYield = unit.health < (unit.maxHealth * 0.5f);
                    decisions[unitId] = shouldYield;
                }
            }

            return decisions;
        }

        // Public accessors for UI systems that need them
        public Pathfinding GetPathfinding() => pathfinding;
        public AIController GetAIController() => aiController;

        // Private helper methods

        private List<IOrder> ValidateOrders(int playerId, List<IOrder> orders)
        {
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
                    UnityEngine.Debug.Log($"[GameEngine] Player {playerId} order validated: {order.GetOrderType()}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[GameEngine] Player {playerId} order REJECTED: {order.GetOrderType()} - {error}");
                }
            }

            return validOrders;
        }

        private void AwardIncome()
        {
            foreach (Player player in State.playerManager.players)
            {
                if (!player.isEliminated)
                {
                    int shipyardCount = State.structureManager.GetStructuresForPlayer(player.id)
                        .FindAll(s => s.type == StructureType.SHIPYARD).Count;

                    int goldEarned = shipyardCount * 100; // Rule: 100g per shipyard per turn
                    player.gold += goldEarned;
                }

                // Reset ready status for next turn
                player.isReady = false;
            }
        }

        private List<GameEvent> ProcessConstruction()
        {
            if (ConstructionManager.Instance != null)
            {
                return ConstructionManager.Instance.ProcessTurn(State.turnNumber);
            }
            return new List<GameEvent>();
        }

        private void GenerateAIOrders()
        {
            foreach (Player aiPlayer in State.playerManager.GetAIPlayers())
            {
                if (!aiPlayer.isReady)
                {
                    List<IOrder> aiOrders = aiController.PlanTurn(aiPlayer.id);
                    State.pendingOrders[aiPlayer.id] = ValidateOrders(aiPlayer.id, aiOrders);
                    aiPlayer.isReady = true;
                }
            }
        }

        private List<GameEvent> ResolveOrders()
        {
            // Collect all orders
            List<IOrder> allOrders = new List<IOrder>();
            foreach (var kvp in State.pendingOrders)
            {
                allOrders.AddRange(kvp.Value);
            }

            // Resolve
            var events = turnResolver.ResolveTurn(allOrders, State.turnNumber);

            // Clear pending orders
            State.pendingOrders.Clear();

            return events;
        }

        private List<CollisionInfo> ExtractCollisions(List<GameEvent> events)
        {
            State.pendingCollisions.Clear();

            foreach (GameEvent evt in events)
            {
                if (evt is CollisionNeedsResolutionEvent collisionEvent)
                {
                    State.pendingCollisions.Add(collisionEvent.collision);
                }
            }

            if (State.pendingCollisions.Count > 0)
            {
                OnCollisionsDetected?.Invoke(State.pendingCollisions);
            }

            return State.pendingCollisions;
        }
    }

    // Supporting classes

    [Serializable]
    public struct PlayerConfig
    {
        public string name;
        public PlayerType type;

        public PlayerConfig(string name, PlayerType type)
        {
            this.name = name;
            this.type = type;
        }
    }

    public class TurnResult
    {
        public int turnNumber;
        public List<GameEvent> events;
        public List<CollisionInfo> collisions;
        public Player winner;
    }
}
