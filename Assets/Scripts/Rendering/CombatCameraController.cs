using PlunkAndPlunder.Core;
using PlunkAndPlunder.Units;
using System.Collections;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Automatically focuses camera on combat as it occurs
    /// Phase 2.1: Smart Camera System
    /// </summary>
    public class CombatCameraController : MonoBehaviour
    {
        [Header("Auto-Focus Settings")]
        public bool autoFocusEnabled = true;
        public float focusDuration = 1.5f;
        public float zoomInHeight = 12f;
        public float overviewHeight = 25f;

        private CameraController cameraController;
        private TurnAnimator turnAnimator;
        private UnitManager unitManager;
        private bool isManualControl = false;
        private Vector3 lastManualInput = Vector3.zero;

        private void Awake()
        {
            cameraController = GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.LogError("[CombatCameraController] CameraController component not found!");
            }
        }

        public void Initialize(TurnAnimator animator, UnitManager manager)
        {
            turnAnimator = animator;
            unitManager = manager;

            // Subscribe to combat events
            if (turnAnimator != null)
            {
                turnAnimator.OnCombatOccurred += HandleCombatOccurred;
                Debug.Log("[CombatCameraController] Subscribed to OnCombatOccurred event");
            }
        }

        private void Update()
        {
            // Detect manual camera control
            bool manualInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                              Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                              Input.GetMouseButton(1) || Input.GetMouseButton(2);

            if (manualInput && !isManualControl)
            {
                isManualControl = true;
                autoFocusEnabled = false;
                Debug.Log("[CombatCameraController] Manual control detected, auto-focus disabled");
            }

            // Re-enable with F key
            if (Input.GetKeyDown(KeyCode.F))
            {
                autoFocusEnabled = !autoFocusEnabled;
                isManualControl = false;
                Debug.Log($"[CombatCameraController] Auto-focus {(autoFocusEnabled ? "ENABLED" : "DISABLED")}");
            }
        }

        private void HandleCombatOccurred(CombatOccurredEvent combatEvent)
        {
            if (!autoFocusEnabled)
            {
                Debug.Log("[CombatCameraController] Auto-focus disabled, skipping camera movement");
                return;
            }

            // Get combatant units
            Unit attacker = unitManager?.GetUnit(combatEvent.attackerId);
            Unit defender = unitManager?.GetUnit(combatEvent.defenderId);

            if (attacker == null || defender == null)
            {
                Debug.LogWarning($"[CombatCameraController] Could not find units for combat: {combatEvent.attackerId} vs {combatEvent.defenderId}");
                return;
            }

            // Calculate focus position (midpoint between combatants)
            Vector3 attackerPos = attacker.position.ToWorldPosition();
            Vector3 defenderPos = defender.position.ToWorldPosition();
            Vector3 midpoint = (attackerPos + defenderPos) / 2f;

            // Calculate distance to determine zoom level
            float distance = Vector3.Distance(attackerPos, defenderPos);
            float zoomHeight = Mathf.Lerp(zoomInHeight, zoomInHeight + 5f, distance / 10f);

            Debug.Log($"[CombatCameraController] 🎥 Auto-focusing on combat at {midpoint} (distance: {distance:F1})");

            // Focus camera on combat
            StartCoroutine(FocusOnCombatSequence(midpoint, zoomHeight));
        }

        private IEnumerator FocusOnCombatSequence(Vector3 focusPosition, float zoomHeight)
        {
            if (cameraController == null) yield break;

            // Zoom in on combat
            Vector3 combatCameraPos = new Vector3(
                focusPosition.x,
                zoomHeight,
                focusPosition.z - 7.5f
            );

            // Smooth transition to combat
            cameraController.FocusOnPosition(focusPosition, smooth: true);

            // Wait while combat is displayed
            yield return new WaitForSeconds(focusDuration);

            // Return to overview (not implemented yet - would need to remember original position)
            // For now, just let player manually pan back or continue watching
        }

        private void OnDestroy()
        {
            if (turnAnimator != null)
            {
                turnAnimator.OnCombatOccurred -= HandleCombatOccurred;
            }
        }
    }
}
