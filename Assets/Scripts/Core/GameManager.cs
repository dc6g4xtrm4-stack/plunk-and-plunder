using System;
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

            // Generate map
            int seed = debugMapSeed != 0 ? debugMapSeed : UnityEngine.Random.Range(0, int.MaxValue);
            state.mapSeed = seed;
            MapGenerator mapGen = new MapGenerator(seed);
            state.grid = mapGen.GenerateMap(config.numSeaTiles, config.numIslands, config.minIslandSize, config.maxIslandSize);

            // Initialize systems
            pathfinding = new Pathfinding(state.grid);
            orderValidator = new OrderValidator(state.grid, state.unitManager, state.structureManager, state.playerManager);
            turnResolver = new TurnResolver(state.grid, state.unitManager, state.playerManager, state.structureManager, enableDeterministicLogging);
            aiController = new AIController(state.grid, state.unitManager, state.playerManager, pathfinding);

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

            // Initialize conflict resolution UI
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject conflictUIObj = new GameObject("ConflictResolutionUI");
                conflictUIObj.transform.SetParent(canvas.transform, false);
                conflictResolutionUI = conflictUIObj.AddComponent<ConflictResolutionUI>();
                conflictResolutionUI.Initialize();
            }

            // Place starting units for each player
            PlaceStartingUnits();

            // Place harbors
            PlaceHarbors();

            // Place starting shipyards for each player
            PlaceStartingShipyards();

            // Trigger initial render
            OnGameStateUpdated?.Invoke(state);

            // Transition to game
            ChangePhase(GamePhase.Planning);
        }

        private void PlaceStartingUnits()
        {
            List<Tile> seaTiles = state.grid.GetTilesOfType(TileType.SEA);

            // Shuffle for random starting positions (deterministic based on map seed)
            System.Random rng = new System.Random(state.mapSeed);
            seaTiles = seaTiles.OrderBy(t => rng.Next()).ToList();

            int tilesPerPlayer = Mathf.Max(1, seaTiles.Count / state.playerManager.players.Count / 4);

            for (int i = 0; i < state.playerManager.players.Count; i++)
            {
                Player player = state.playerManager.players[i];

                // Give each player 2 starting ships
                for (int j = 0; j < 2 && (i * tilesPerPlayer + j) < seaTiles.Count; j++)
                {
                    HexCoord startPos = seaTiles[i * tilesPerPlayer + j].coord;
                    state.unitManager.CreateUnit(player.id, startPos, UnitType.SHIP);
                }
            }

            Debug.Log($"[GameManager] Placed {state.unitManager.units.Count} starting units");
        }

        private void PlaceHarbors()
        {
            // Harbors are already part of the map as HARBOR tiles
            // Create structures for each harbor (initially neutral)
            List<Tile> harborTiles = state.grid.GetTilesOfType(TileType.HARBOR);

            foreach (Tile harborTile in harborTiles)
            {
                state.structureManager.CreateStructure(-1, harborTile.coord, StructureType.HARBOR);
            }

            Debug.Log($"[GameManager] Placed {harborTiles.Count} harbors");
        }

        private void PlaceStartingShipyards()
        {
            List<Tile> harborTiles = state.grid.GetTilesOfType(TileType.HARBOR);
            System.Random rng = new System.Random(state.mapSeed + 1); // Different seed for shipyard placement

            // Shuffle harbors for random assignment
            harborTiles = harborTiles.OrderBy(t => rng.Next()).ToList();

            int shipyardsPlaced = 0;
            for (int i = 0; i < state.playerManager.players.Count && i < harborTiles.Count; i++)
            {
                Player player = state.playerManager.players[i];
                HexCoord harborCoord = harborTiles[i].coord;

                // Change the harbor ownership to the player
                Structure harbor = state.structureManager.GetStructureAtPosition(harborCoord);
                if (harbor != null)
                {
                    state.structureManager.ChangeOwner(harbor.id, player.id);
                }

                // Create a shipyard at this harbor for the player
                state.structureManager.CreateStructure(player.id, harborCoord, StructureType.SHIPYARD);
                shipyardsPlaced++;

                Debug.Log($"[GameManager] Player {player.id} ({player.name}) starts with shipyard at {harborCoord}");
            }

            Debug.Log($"[GameManager] Placed {shipyardsPlaced} starting shipyards");
        }

        public void ChangePhase(GamePhase newPhase)
        {
            state.phase = newPhase;
            Debug.Log($"[GameManager] Phase changed to {newPhase}");

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

            // Reset ready status
            foreach (Player player in state.playerManager.players)
            {
                player.isReady = false;
            }

            Debug.Log($"[GameManager] Turn {state.turnNumber} - Planning phase started");

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

            // Transition to Animating phase and start animation
            ChangePhase(GamePhase.Animating);
            turnAnimator.AnimateEvents(events, state);
        }

        private void HandleAnimationStep(GameState updatedState)
        {
            // Trigger visual update after each animation step
            OnGameStateUpdated?.Invoke(updatedState);
        }

        private void HandleAnimationComplete()
        {
            Debug.Log("[GameManager] Animation complete, transitioning to next phase");

            // Final state update
            OnGameStateUpdated?.Invoke(state);

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
        public void AutoResolveTurn()
        {
            if (state.phase == GamePhase.Planning)
            {
                // Auto-submit for all non-ready players
                foreach (Player player in state.playerManager.GetActivePlayers())
                {
                    if (!player.isReady)
                    {
                        SubmitOrders(player.id, new List<IOrder>());
                    }
                }
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
    }

    [Serializable]
    public class GameConfig
    {
        public int numSeaTiles = 500;
        public int numIslands = 25;
        public int minIslandSize = 4;
        public int maxIslandSize = 8;
    }
}
