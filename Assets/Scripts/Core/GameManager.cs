using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.AI;
using PlunkAndPlunder.Construction;
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

        // Simulation support: skip animation for headless AI testing
        public bool skipAnimation = false;

        // Core game engine (deterministic logic)
        private GameEngine engine;

        // UI/Rendering systems (Unity-specific)
        private TurnAnimator turnAnimator;
        private NetworkManager networkManager;
        private ConflictResolutionUI conflictResolutionUI;
        private CombatResultsUI combatResultsUI;
        private CollisionYieldUI collisionYieldUI;
        private EncounterUI encounterUI; // NEW: Encounter system UI
        private CombatResultsHUD combatResultsHUD; // NEW: Deterministic combat results display
        private RightPanelHUD rightPanelHUD; // NEW: Right panel with combat log
        // DEPRECATED: PlayerStatsHUD is now integrated into LeftPanelHUD
        // private PlayerStatsHUD playerStatsHUD;

        // Combat tracking
        private Dictionary<string, int> combatRounds; // Track combat rounds per unit pair
        private Rendering.CombatIndicator combatIndicator; // Visual indicator for combat
        private Rendering.CombatConnectionRenderer combatConnectionRenderer; // NEW: Combat connection lines
        private Rendering.FloatingTextRenderer floatingTextRenderer; // NEW: Floating damage numbers and notifications

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

            // Initialize ConstructionManager (singleton)
            if (ConstructionManager.Instance == null)
            {
                GameObject constructionManagerObj = new GameObject("ConstructionManager");
                constructionManagerObj.AddComponent<ConstructionManager>();
                Debug.Log("[GameManager] Created ConstructionManager");
            }

            Debug.Log("[GameManager] Initialized");
        }

        public void StartOfflineGame(int numPlayers = 4)
        {
            Debug.Log($"[GameManager] Starting offline game with {numPlayers} players");

            // Initialize GameEngine with config
            engine = new GameEngine(config);

            // Setup player configs
            List<PlayerConfig> players = new List<PlayerConfig>();
            players.Add(new PlayerConfig("Player 1", PlayerType.Human));
            for (int i = 1; i < numPlayers; i++)
            {
                players.Add(new PlayerConfig($"AI {i}", PlayerType.AI));
            }

            // Initialize game using engine (handles map gen, player setup, starting units)
            int? seed = debugMapSeed != 0 ? debugMapSeed : (int?)null;
            engine.InitializeGame(players, seed);

            // Get reference to state for convenience
            state = engine.State;

            Debug.Log($"[GameManager] Map generated with {state.grid.playerStartIslands.Count} player start islands");

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
            turnAnimator.OnStructureAttacked += HandleStructureAttacked;
            turnAnimator.OnStructureCaptured += HandleStructureCaptured;

            // Initialize combat camera controller (Phase 2.1: auto-focus on combat)
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                PlunkAndPlunder.Rendering.CombatCameraController combatCamera = mainCamera.GetComponent<PlunkAndPlunder.Rendering.CombatCameraController>();
                if (combatCamera == null)
                {
                    combatCamera = mainCamera.gameObject.AddComponent<PlunkAndPlunder.Rendering.CombatCameraController>();
                }
                combatCamera.Initialize(turnAnimator, state.unitManager);
                Debug.Log("[GameManager] CombatCameraController initialized - auto-focus on combat enabled (press F to toggle)");
            }

            // Initialize conflict resolution UI and combat results UI
            Canvas canvas = FindFirstObjectByType<Canvas>();
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

                // NEW: Initialize EncounterUI
                GameObject encounterUIObj = new GameObject("EncounterUI");
                encounterUIObj.transform.SetParent(canvas.transform, false);
                encounterUI = encounterUIObj.AddComponent<EncounterUI>();
                encounterUI.Initialize(0); // Local player ID (human player is always 0 in offline mode)

                // NEW: Initialize CombatResultsHUD for deterministic combat
                GameObject combatResultsHUDObj = new GameObject("CombatResultsHUD");
                combatResultsHUDObj.transform.SetParent(canvas.transform, false);
                combatResultsHUD = combatResultsHUDObj.AddComponent<CombatResultsHUD>();
                combatResultsHUD.Initialize();

                // NEW: Initialize Right Panel HUD with combat log
                GameObject rightPanelHUDObj = new GameObject("RightPanelHUD");
                rightPanelHUDObj.transform.SetParent(canvas.transform, false);
                rightPanelHUD = rightPanelHUDObj.AddComponent<RightPanelHUD>();
                rightPanelHUD.Initialize(state);

                // DEPRECATED: PlayerStatsHUD is now integrated into LeftPanelHUD
                // GameObject playerStatsHUDObj = new GameObject("PlayerStatsHUD");
                // playerStatsHUDObj.transform.SetParent(canvas.transform, false);
                // playerStatsHUD = playerStatsHUDObj.AddComponent<PlayerStatsHUD>();
                // playerStatsHUD.Initialize();
            }

            // Initialize combat indicator
            GameObject combatIndicatorObj = new GameObject("CombatIndicator");
            combatIndicator = combatIndicatorObj.AddComponent<Rendering.CombatIndicator>();
            combatIndicator.Initialize();
            Debug.Log("[GameManager] Combat indicator initialized");

            // Initialize combat connection renderer
            GameObject combatConnectionObj = new GameObject("CombatConnectionRenderer");
            combatConnectionRenderer = combatConnectionObj.AddComponent<Rendering.CombatConnectionRenderer>();
            Debug.Log("[GameManager] Combat connection renderer initialized");

            // Initialize floating text renderer
            GameObject floatingTextObj = new GameObject("FloatingTextRenderer");
            floatingTextRenderer = floatingTextObj.AddComponent<Rendering.FloatingTextRenderer>();
            Debug.Log("[GameManager] Floating text renderer initialized");

            // Initialization (map, structures, units) already handled by engine.InitializeGame()

            // Trigger initial render
            OnGameStateUpdated?.Invoke(state);

            // DEPRECATED: Player stats now shown in LeftPanelHUD
            // Update player stats HUD
            // if (playerStatsHUD != null)
            // {
            //     playerStatsHUD.UpdateStats(state);
            //     playerStatsHUD.Show();
            // }

            // Transition to game
            ChangePhase(GamePhase.Planning);
        }

        // Initialization methods moved to GameInitializer for shared use across game modes

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
                    StartCoroutine(StartPlanningPhaseAsync());
                    break;

                case GamePhase.Resolving:
                    ResolveCurrentTurn();
                    break;

                case GamePhase.Animating:
                    // Animation will be started by ResolveCurrentTurn
                    break;
            }
        }

        private System.Collections.IEnumerator StartPlanningPhaseAsync()
        {
            // Use engine to start turn (advances turn, awards income, processes construction)
            List<GameEvent> events = engine.StartTurn();

            GameLogger.LogTurnStart(state.turnNumber);

            // Log income events
            foreach (Player player in state.playerManager.players)
            {
                if (!player.isEliminated)
                {
                    int shipyardCount = state.structureManager.GetStructuresForPlayer(player.id)
                        .FindAll(s => s.type == Structures.StructureType.SHIPYARD).Count;
                    Debug.Log($"[GameManager] Player {player.id} has {player.gold} gold from {shipyardCount} shipyard(s)");
                    GameLogger.LogPlayerAction(player.id, $"Gold: {player.gold} from {shipyardCount} shipyard(s)");
                }
            }

            Debug.Log($"[GameManager] Turn {state.turnNumber} - Planning phase started");
            GameLogger.LogGameState(state);

            // Flash player units at start of each turn
            FlashPlayerUnits();

            // DEPRECATED: Player stats now shown in LeftPanelHUD
            // Update player stats HUD after awarding gold
            // if (playerStatsHUD != null)
            // {
            //     playerStatsHUD.UpdateStats(state);
            // }

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

            // Trigger AI planning (use engine's AI controller)
            // Run async with yields between players to prevent UI freeze
            AIController aiController = engine.GetAIController();
            if (aiController != null)
            {
                foreach (Player aiPlayer in state.playerManager.GetAIPlayers())
                {
                    List<IOrder> aiOrders = aiController.PlanTurn(aiPlayer.id);
                    SubmitOrders(aiPlayer.id, aiOrders);

                    // Yield to allow UI to update between AI players
                    yield return null;
                }
            }
        }

        public void SubmitOrders(int playerId, List<IOrder> orders)
        {
            Debug.Log($"🎮 [GameManager] ========== SubmitOrders CALLED! ==========");
            Debug.Log($"[GameManager] playerId={playerId}, orders.Count={orders?.Count ?? 0}");

            if (orders != null)
            {
                foreach (var order in orders)
                {
                    Debug.Log($"[GameManager]   Order: {order.GetType().Name}");
                }
            }

            Player player = state.playerManager.GetPlayer(playerId);
            if (player == null)
            {
                Debug.LogError($"[GameManager] ❌ Player {playerId} NOT FOUND!");
                return;
            }

            if (player.isEliminated)
            {
                Debug.LogWarning($"[GameManager] ⚠️ Player {playerId} is ELIMINATED!");
                return;
            }

            Debug.Log($"[GameManager] Player {playerId} validation passed, calling engine.SubmitOrders()...");

            // Use engine to validate and submit orders
            bool allReady = engine.SubmitOrders(playerId, orders);

            Debug.Log($"[GameManager] ✅ Player {playerId} submitted {orders.Count} orders (validated by engine), allReady={allReady}");

            // Check if all players ready
            if (allReady)
            {
                Debug.Log("[GameManager] All players ready! Changing phase to Resolving...");
                ChangePhase(GamePhase.Resolving);
            }
            else
            {
                Debug.Log($"[GameManager] Not all players ready yet. Waiting for other players...");
            }
        }

        private void ResolveCurrentTurn()
        {
            Debug.Log($"[GameManager] Resolving turn {state.turnNumber}");

            // Use engine to resolve turn (generates AI orders for non-ready players, resolves all orders)
            TurnResult result = engine.ResolveTurn();

            Debug.Log($"[GameManager] Turn resolved with {result.events.Count} events");

            OnTurnResolved?.Invoke(result.events);

            // NEW: Check for encounters first (new system)
            if (result.encounters.Count > 0)
            {
                Debug.Log($"[GameManager] {result.encounters.Count} encounter(s) detected, waiting for player decisions");
                ChangePhase(GamePhase.CollisionResolution); // Reuse collision resolution phase

                // Extract encounters into game state
                engine.ExtractEncounters(result.events);

                // Show encounter UI for players to make decisions
                if (encounterUI != null)
                {
                    encounterUI.ShowEncounters(result.encounters);
                }

                // AI players automatically make decisions
                MakeAIEncounterDecisions();
            }
            // OLD: Backward compatibility for old collision system
            else if (result.collisions.Count > 0)
            {
                Debug.Log($"[GameManager] {result.collisions.Count} collision(s) detected, waiting for yield decisions");
                state.collisionYieldDecisions.Clear();
                ChangePhase(GamePhase.CollisionResolution);

                // Show collision yield UI for players to make decisions
                if (collisionYieldUI != null)
                {
                    collisionYieldUI.ShowCollisions(result.collisions);
                }

                // AI players automatically make decisions
                MakeAIYieldDecisions();
            }
            else
            {
                // No collisions/encounters, proceed to animation (or skip if headless)
                Debug.Log($"[GameManager] No collisions or encounters detected, proceeding to animation with {result.events.Count} events");
                ChangePhase(GamePhase.Animating);

                if (skipAnimation)
                {
                    // Headless mode: skip animation and immediately complete turn
                    Debug.Log("[GameManager] Skipping animation (headless mode)");
                    HandleAnimationComplete();
                }
                else
                {
                    turnAnimator.AnimateEvents(result.events, state);
                }
            }
        }

        private void HandleAnimationStep(GameState updatedState)
        {
            Debug.Log("[GameManager] HandleAnimationStep called - triggering OnGameStateUpdated");
            // Trigger visual update after each animation step
            OnGameStateUpdated?.Invoke(updatedState);

            // DEPRECATED: Player stats now shown in LeftPanelHUD
            // Update player stats HUD
            // if (playerStatsHUD != null)
            // {
            //     playerStatsHUD.UpdateStats(updatedState);
            // }
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

            // DEPRECATED: Player stats now shown in LeftPanelHUD
            // Update player stats HUD
            // if (playerStatsHUD != null)
            // {
            //     playerStatsHUD.UpdateStats(state);
            // }

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

        // ====================
        // NEW ENCOUNTER SYSTEM METHODS
        // ====================

        /// <summary>
        /// Submit an encounter decision for a unit involved in an encounter.
        /// </summary>
        public void SubmitEncounterDecision(string unitId, Combat.Encounter encounter, bool isAttacking)
        {
            if (state.phase != GamePhase.CollisionResolution)
            {
                Debug.LogWarning($"[GameManager] Cannot submit encounter decision - not in CollisionResolution phase");
                return;
            }

            // Record decision based on encounter type
            if (encounter.Type == Combat.EncounterType.PASSING)
            {
                var decision = isAttacking ? Combat.PassingEncounterDecision.ATTACK : Combat.PassingEncounterDecision.PROCEED;
                encounter.RecordPassingDecision(unitId, decision);
                Debug.Log($"[GameManager] Unit {unitId} PASSING decision: {decision}");
            }
            else if (encounter.Type == Combat.EncounterType.ENTRY)
            {
                var decision = isAttacking ? Combat.EntryEncounterDecision.ATTACK : Combat.EntryEncounterDecision.YIELD;
                encounter.RecordEntryDecision(unitId, decision);
                Debug.Log($"[GameManager] Unit {unitId} ENTRY decision: {decision}");
            }

            // Check if all decisions are collected
            if (AllEncounterDecisionsCollected())
            {
                ContinueResolutionWithEncounterDecisions();
            }
        }

        private bool AllEncounterDecisionsCollected()
        {
            foreach (var encounter in state.activeEncounters)
            {
                if (encounter.AwaitingPlayerChoices)
                {
                    Debug.Log($"[GameManager] Still waiting for decisions on encounter {encounter.Id}");
                    return false;
                }
            }

            Debug.Log($"[GameManager] All encounter decisions collected!");
            return true;
        }

        private void MakeAIEncounterDecisions()
        {
            engine.MakeAIEncounterDecisions(state.activeEncounters);

            // Check if all decisions are now collected (might be all-AI encounter)
            if (AllEncounterDecisionsCollected())
            {
                ContinueResolutionWithEncounterDecisions();
            }
        }

        private void ContinueResolutionWithEncounterDecisions()
        {
            Debug.Log($"[GameManager] All encounter decisions collected, resolving encounters");

            // Use engine to resolve encounters with decisions
            List<GameEvent> encounterEvents = engine.ResolveEncounters(state.activeEncounters);

            OnTurnResolved?.Invoke(encounterEvents);

            // Clear encounter data
            state.activeEncounters.Clear();

            // Proceed to animation phase
            ChangePhase(GamePhase.Animating);

            if (skipAnimation)
            {
                // Headless mode: skip animation and immediately complete turn
                Debug.Log("[GameManager] Skipping animation after encounter resolution (headless mode)");
                HandleAnimationComplete();
            }
            else
            {
                turnAnimator.AnimateEvents(encounterEvents, state);
            }
        }

        // ====================
        // OLD COLLISION SYSTEM METHODS (backward compatibility)
        // ====================

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
            // Use engine to generate AI yield decisions
            Dictionary<string, bool> aiDecisions = engine.GetAIYieldDecisions(state.pendingCollisions);

            // Submit all AI decisions
            foreach (var kvp in aiDecisions)
            {
                SubmitYieldDecision(kvp.Key, kvp.Value);

                Unit unit = state.unitManager.GetUnit(kvp.Key);
                if (unit != null)
                {
                    Debug.Log($"[GameManager] AI Player {unit.ownerId} unit {kvp.Key}: {(kvp.Value ? "yielding" : "pushing")} (HP: {unit.health}/{unit.maxHealth})");
                }
            }
        }

        private void ContinueResolutionWithYieldDecisions()
        {
            Debug.Log($"[GameManager] All yield decisions collected, resolving collisions");

            // Use engine to resolve collisions with yield decisions
            List<GameEvent> collisionEvents = engine.ResolveCollisions(state.collisionYieldDecisions);

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

                        Debug.Log($"[GameManager] Ongoing combat tracked: {unit.id} at {unit.position} vs {opponent.id} at {opponent.position}");
                    }
                }
            }

            // Combine all events for animation
            List<GameEvent> allEvents = new List<GameEvent>(state.eventHistory);

            // Transition to animation (or skip if headless)
            Debug.Log($"[GameManager] Collisions resolved, proceeding to animation with {allEvents.Count} total events");
            ChangePhase(GamePhase.Animating);

            if (skipAnimation)
            {
                // Headless mode: skip animation and immediately complete turn
                Debug.Log("[GameManager] Skipping animation (headless mode)");
                HandleAnimationComplete();
            }
            else
            {
                turnAnimator.AnimateEvents(allEvents, state);
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

        private void HandleCombatOccurred(CombatOccurredEvent combatEvent)
        {
            Debug.Log($"[GameManager] ☠️ COMBAT: {combatEvent.attackerId} vs {combatEvent.defenderId}");

            // DON'T PAUSE ANIMATION - let ships continue moving!
            // turnAnimator.PauseAnimation(); // REMOVED

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
                combatRounds.Remove(combatKey);
                // DON'T resume - we never paused!
                return;
            }

            // SHOW SKULL AND CROSSBONES INDICATOR at combat location
            // Combat occurs at the defender's position
            HexCoord combatPosition = defender.position;
            ShowCombatIndicator(combatPosition);

            // Show combat connection line between combatants
            if (combatConnectionRenderer != null)
            {
                Vector3 attackerWorldPos = attacker.position.ToWorldPosition(1f);
                Vector3 defenderWorldPos = defender.position.ToWorldPosition(1f);

                // Determine combat outcome for visual styling
                Rendering.CombatOutcome outcome;
                if (combatEvent.attackerDestroyed && combatEvent.defenderDestroyed)
                {
                    outcome = Rendering.CombatOutcome.MutualDestruction;
                }
                else if (combatEvent.defenderDestroyed && attacker.ownerId == 0)
                {
                    outcome = Rendering.CombatOutcome.PlayerVictory;
                }
                else if (!combatEvent.attackerDestroyed && !combatEvent.defenderDestroyed)
                {
                    outcome = Rendering.CombatOutcome.Ongoing;
                }
                else
                {
                    outcome = Rendering.CombatOutcome.Standard;
                }

                combatConnectionRenderer.ShowCombatLine(
                    attackerWorldPos,
                    defenderWorldPos,
                    combatEvent.damageToDefender,
                    outcome
                );
            }

            // Add to right panel combat log
            if (rightPanelHUD != null)
            {
                rightPanelHUD.AddCombatEntry(combatEvent, state);
            }

            // DEPRECATED: Old center popup (now using right panel combat log)
            // Check if human player is involved in combat
            // bool humanInvolved = (attacker != null && attacker.ownerId == 0) || (defender != null && defender.ownerId == 0);

            // Show combat results HUD (non-blocking overlay) for human combats
            // DISABLED: Now using right panel combat log instead
            // if (humanInvolved && !isAutoResolving)
            // {
            //     if (combatResultsHUD != null)
            //     {
            //         combatResultsHUD.ShowCombatResult(combatEvent, state);
            //         combatRounds[combatKey] = roundNumber + 1;
            //         // HUD auto-hides after 3 seconds (no animation pause!)
            //     }
            // }
            // else
            // {
            //     Debug.Log($"[GameManager] AI vs AI combat or auto-resolve - showing indicator only");
            // }

            // Update combat rounds tracking
            combatRounds[combatKey] = roundNumber + 1;

            // Award salvage gold for destroyed ships (50% of build cost = 25g for basic ships)
            const int SALVAGE_VALUE = 25; // 50% of 50g build cost
            if (combatEvent.defenderDestroyed && attacker != null)
            {
                // Attacker destroyed defender - award salvage to attacker's owner
                state.playerManager.GetPlayer(attacker.ownerId).gold += SALVAGE_VALUE;
                Debug.Log($"[GameManager] Player {attacker.ownerId} earned {SALVAGE_VALUE}g salvage from destroying {defender?.id ?? combatEvent.defenderId}");

                // Show floating gold notification at defender's position
                if (floatingTextRenderer != null)
                {
                    Vector3 defenderWorldPos = defender.position.ToWorldPosition(1f);
                    floatingTextRenderer.SpawnGoldNotification(defenderWorldPos, SALVAGE_VALUE);
                }
            }
            else if (combatEvent.attackerDestroyed && defender != null)
            {
                // Defender destroyed attacker - award salvage to defender's owner
                state.playerManager.GetPlayer(defender.ownerId).gold += SALVAGE_VALUE;
                Debug.Log($"[GameManager] Player {defender.ownerId} earned {SALVAGE_VALUE}g salvage from destroying {attacker?.id ?? combatEvent.attackerId}");

                // Show floating gold notification at attacker's position
                if (floatingTextRenderer != null)
                {
                    Vector3 attackerWorldPos = attacker.position.ToWorldPosition(1f);
                    floatingTextRenderer.SpawnGoldNotification(attackerWorldPos, SALVAGE_VALUE);
                }
            }

            // If either unit is destroyed, clear the combat tracking
            if (combatEvent.attackerDestroyed || combatEvent.defenderDestroyed)
            {
                combatRounds.Remove(combatKey);
            }
        }

        /// <summary>
        /// Show visual combat indicator (RED SKULL AND CROSSBONES) at combat location
        /// </summary>
        private void ShowCombatIndicator(HexCoord position)
        {
            if (combatIndicator != null)
            {
                combatIndicator.ShowCombatAt(position, hexSize: 1f, duration: 2.0f);
                Debug.Log($"[GameManager] ☠️ Combat indicator shown at {position}");
            }
        }

        /// <summary>
        /// Handle structure attacked event - show combat indicator at structure location
        /// </summary>
        private void HandleStructureAttacked(StructureAttackedEvent structureEvent)
        {
            Debug.Log($"[GameManager] ⚔️ STRUCTURE ATTACK: Ship {structureEvent.attackerUnitId} attacked shipyard at {structureEvent.position} ({structureEvent.oldHealth} → {structureEvent.newHealth} HP)");

            // Show combat indicator at structure location
            ShowCombatIndicator(structureEvent.position);
        }

        /// <summary>
        /// Handle structure captured event - show combat indicator with emphasis
        /// </summary>
        private void HandleStructureCaptured(StructureCapturedEvent captureEvent)
        {
            Debug.Log($"[GameManager] 🏴 STRUCTURE CAPTURED: Player {captureEvent.newOwnerId} captured shipyard from Player {captureEvent.previousOwnerId} at {captureEvent.position}");

            // Show combat indicator for capture (reuse ShowCombatIndicator)
            ShowCombatIndicator(captureEvent.position);
        }

        // NOTE: These methods are now UNUSED since we don't pause for combat
        // Keeping them commented for reference in case we need pause functionality later

        /*
        private IEnumerator AutoContinueCombat()
        {
            yield return new WaitForSeconds(0.1f);
            turnAnimator.ResumeAnimation();
        }

        private IEnumerator AutoResumeCombatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            turnAnimator.ResumeAnimation();
        }

        private void OnCombatResultsContinue()
        {
            turnAnimator.ResumeAnimation();
        }
        */

        public Pathfinding GetPathfinding() => engine?.GetPathfinding();

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
        /// Flash all player units at the start of a turn
        /// </summary>
        private void FlashPlayerUnits()
        {
            Player humanPlayer = state.playerManager.GetPlayer(0);
            if (humanPlayer == null) return;

            UnitRenderer unitRenderer = FindFirstObjectByType<UnitRenderer>();
            if (unitRenderer != null)
            {
                int flashedCount = 0;
                foreach (Unit unit in state.unitManager.GetAllUnits())
                {
                    if (unit.ownerId == humanPlayer.id)
                    {
                        unitRenderer.AddTemporaryHighlight(unit.id, duration: 3f);
                        flashedCount++;
                    }
                }
                Debug.Log($"[GameManager] 💡 Flashed {flashedCount} player units at turn start");
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
                BuildingRenderer buildingRenderer = FindFirstObjectByType<BuildingRenderer>();
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
            UnitRenderer unitRenderer = FindFirstObjectByType<UnitRenderer>();
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
