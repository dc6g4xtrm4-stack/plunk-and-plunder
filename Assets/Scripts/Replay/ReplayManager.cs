using System;
using System.Collections;
using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Resolution;
using UnityEngine;

namespace PlunkAndPlunder.Replay
{
    /// <summary>
    /// Orchestrates replay playback, manages state, and controls animation
    /// </summary>
    public class ReplayManager : MonoBehaviour
    {
        // Replay data
        private ReplayData replayData;
        private GameState state;
        private int currentTurn = 0;

        // Systems (reused from regular game)
        private TurnAnimator turnAnimator;
        private Pathfinding pathfinding;

        // Playback control
        private bool isPaused = false;
        private float speedMultiplier = 1.0f;
        private bool isComplete = false;

        // Constants for timing
        private const float BASE_HEX_STEP = 0.5f;
        private const float BASE_COMBAT_PAUSE = 1.0f;
        private const float BASE_EVENT_PAUSE = 0.5f;

        // Events for UI updates
        public event Action<int, int> OnTurnChanged; // (current, total)
        public event Action<GameState> OnStateUpdated;
        public event Action OnReplayComplete;

        public bool IsAnimating => turnAnimator != null && turnAnimator.IsAnimating;
        public bool IsPaused => isPaused;
        public bool IsComplete => isComplete;
        public int CurrentTurn => currentTurn;
        public int TotalTurns => replayData?.turns.Count ?? 0;
        public GameState State => state;

        public void StartReplay(string logFilePath)
        {
            Debug.Log($"[ReplayManager] Starting replay from: {logFilePath}");

            try
            {
                // 1. Parse log
                SimulationLogParser parser = new SimulationLogParser();
                replayData = parser.ParseLog(logFilePath);

                // 2. Reconstruct initial state
                ReplayStateReconstructor reconstructor = new ReplayStateReconstructor();
                state = reconstructor.ReconstructInitialState(replayData.initialization);

                // 3. Initialize systems
                pathfinding = new Pathfinding(state.grid);
                turnAnimator = gameObject.AddComponent<TurnAnimator>();
                turnAnimator.Initialize(state.unitManager);
                turnAnimator.OnAnimationComplete += HandleTurnAnimationComplete;

                // 4. Trigger initial render
                Debug.Log("[ReplayManager] Triggering initial state render");
                OnStateUpdated?.Invoke(state);

                // 5. Start playback
                currentTurn = 0;
                isComplete = false;
                StartCoroutine(PlaybackCoroutine());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayManager] Failed to start replay: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private IEnumerator PlaybackCoroutine()
        {
            Debug.Log("[ReplayManager] Starting playback coroutine");

            while (currentTurn < replayData.turns.Count)
            {
                // Wait if paused
                while (isPaused)
                {
                    yield return new WaitForSeconds(0.1f);
                }

                // Get turn data
                TurnData turnData = replayData.turns[currentTurn];
                Debug.Log($"[ReplayManager] Playing turn {currentTurn}/{replayData.turns.Count}");

                // Update UI
                OnTurnChanged?.Invoke(currentTurn, replayData.turns.Count);

                // Update game state turn number
                state.currentTurn = currentTurn;

                // Apply speed multiplier
                ApplySpeedMultiplier();

                // Animate turn events (if any)
                if (turnData.events.Count > 0)
                {
                    Debug.Log($"[ReplayManager] Animating {turnData.events.Count} events for turn {currentTurn}");
                    turnAnimator.AnimateEvents(turnData.events, state);

                    // Wait for animation complete
                    while (turnAnimator.IsAnimating)
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.Log($"[ReplayManager] No events to animate for turn {currentTurn}");
                }

                // Trigger state update after turn
                OnStateUpdated?.Invoke(state);

                // Move to next turn
                currentTurn++;

                // Small delay between turns
                yield return new WaitForSeconds(0.5f / speedMultiplier);
            }

            Debug.Log("[ReplayManager] Replay complete!");
            isComplete = true;
            OnReplayComplete?.Invoke();
        }

        private void HandleTurnAnimationComplete()
        {
            Debug.Log($"[ReplayManager] Turn {currentTurn} animation complete");
        }

        public void SetSpeed(float multiplier)
        {
            Debug.Log($"[ReplayManager] Setting speed multiplier to {multiplier}x");
            speedMultiplier = multiplier;
            ApplySpeedMultiplier();
        }

        private void ApplySpeedMultiplier()
        {
            if (turnAnimator != null)
            {
                turnAnimator.hexStepDelay = BASE_HEX_STEP / speedMultiplier;
                turnAnimator.combatPauseDelay = BASE_COMBAT_PAUSE / speedMultiplier;
                turnAnimator.eventPauseDelay = BASE_EVENT_PAUSE / speedMultiplier;
            }
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Debug.Log($"[ReplayManager] Pause toggled: {isPaused}");

            if (turnAnimator != null)
            {
                if (isPaused)
                {
                    turnAnimator.PauseAnimation();
                }
                else
                {
                    turnAnimator.ResumeAnimation();
                }
            }
        }

        public void Stop()
        {
            Debug.Log("[ReplayManager] Stopping replay");
            StopAllCoroutines();
            isComplete = true;
        }

        private void OnDestroy()
        {
            if (turnAnimator != null)
            {
                turnAnimator.OnAnimationComplete -= HandleTurnAnimationComplete;
            }
        }
    }
}
