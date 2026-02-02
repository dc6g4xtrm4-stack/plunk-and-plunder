using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Debug utility for diagnosing UI state issues
    /// </summary>
    public static class UIDebugUtility
    {
        /// <summary>
        /// Print all active canvases and their children
        /// </summary>
        public static void LogAllCanvases()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"\n========== ALL CANVASES ({canvases.Length}) ==========");

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                sb.AppendLine($"\nCanvas {i + 1}: {canvas.name}");
                sb.AppendLine($"  Active: {canvas.gameObject.activeInHierarchy}");
                sb.AppendLine($"  Enabled: {canvas.enabled}");
                sb.AppendLine($"  RenderMode: {canvas.renderMode}");
                sb.AppendLine($"  SortingOrder: {canvas.sortingOrder}");
                sb.AppendLine($"  OverrideSorting: {canvas.overrideSorting}");

                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                sb.AppendLine($"  GraphicRaycaster: {(raycaster != null ? (raycaster.enabled ? "ACTIVE" : "DISABLED") : "NONE")}");

                // List children
                sb.AppendLine($"  Children ({canvas.transform.childCount}):");
                for (int j = 0; j < canvas.transform.childCount; j++)
                {
                    Transform child = canvas.transform.GetChild(j);
                    sb.AppendLine($"    - {child.name} (active: {child.gameObject.activeInHierarchy})");

                    // Check for CanvasGroup
                    CanvasGroup cg = child.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        sb.AppendLine($"      CanvasGroup: alpha={cg.alpha}, blocksRaycasts={cg.blocksRaycasts}");
                    }
                }
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Verify that no UI objects matching the given name pattern are visible
        /// </summary>
        public static void AssertNoVisibleUI(string namePattern, string context)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<GameObject> violations = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains(namePattern) && obj.activeInHierarchy)
                {
                    // Check if it's actually rendering (has visible components)
                    bool isRendering = false;

                    Image img = obj.GetComponent<Image>();
                    if (img != null && img.enabled && img.color.a > 0.01f)
                        isRendering = true;

                    Text txt = obj.GetComponent<Text>();
                    if (txt != null && txt.enabled && txt.color.a > 0.01f)
                        isRendering = true;

                    Canvas canvas = obj.GetComponent<Canvas>();
                    if (canvas != null && canvas.enabled)
                        isRendering = true;

                    if (isRendering)
                    {
                        violations.Add(obj);
                    }
                }
            }

            if (violations.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"\n[UI STATE VIOLATION] {context}: Found {violations.Count} visible '{namePattern}' objects:");
                foreach (GameObject obj in violations)
                {
                    sb.AppendLine($"  - {GetGameObjectPath(obj)}");

                    // Try to force cleanup
                    Debug.LogError($"[UIDebugUtility] FORCE DESTROYING: {obj.name}");
                    Object.Destroy(obj);
                }
                Debug.LogError(sb.ToString());
            }
        }

        /// <summary>
        /// Get full hierarchy path of a GameObject
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// Check for duplicate canvases (more than one Canvas component in scene)
        /// </summary>
        public static void CheckForDuplicateCanvases()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (canvases.Length > 1)
            {
                Debug.LogWarning($"[UIDebugUtility] Found {canvases.Length} Canvas objects (expected 1):");
                foreach (Canvas canvas in canvases)
                {
                    Debug.LogWarning($"  - {GetGameObjectPath(canvas.gameObject)} (active: {canvas.gameObject.activeInHierarchy})");
                }
            }
        }

        /// <summary>
        /// Force destroy all UI screens except the specified one
        /// </summary>
        public static void ForceCleanupExcept(string[] keepNames)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                {
                    GameObject child = canvas.transform.GetChild(i).gameObject;

                    bool shouldKeep = false;
                    foreach (string keepName in keepNames)
                    {
                        if (child.name.Contains(keepName))
                        {
                            shouldKeep = true;
                            break;
                        }
                    }

                    if (!shouldKeep && (child.name.Contains("Menu") || child.name.Contains("Lobby")))
                    {
                        Debug.Log($"[UIDebugUtility] Force cleanup: Destroying {child.name}");
                        Object.Destroy(child);
                    }
                }
            }
        }

        /// <summary>
        /// NUCLEAR OPTION: Destroy ALL MainMenu and Lobby UI objects in the entire scene
        /// Call this from Unity console if the overlay bug persists:
        /// PlunkAndPlunder.UI.UIDebugUtility.NuclearCleanup();
        /// </summary>
        public static void NuclearCleanup()
        {
            Debug.LogWarning("========== NUCLEAR CLEANUP: DESTROYING ALL MENU/LOBBY UI ==========");

            int destroyedCount = 0;

            // Find ALL GameObjects (including inactive ones)
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("MainMenu") || obj.name.Contains("Menu") || obj.name.Contains("Lobby"))
                {
                    // Don't destroy if it's a child of GameHUD (like "MainMenuUI" might be a component name)
                    bool isPartOfGameHUD = false;
                    Transform parent = obj.transform.parent;
                    while (parent != null)
                    {
                        if (parent.name.Contains("GameHUD"))
                        {
                            isPartOfGameHUD = true;
                            break;
                        }
                        parent = parent.parent;
                    }

                    if (!isPartOfGameHUD)
                    {
                        Debug.LogWarning($"[UIDebugUtility] NUCLEAR: Destroying {GetGameObjectPath(obj)}");
                        Object.DestroyImmediate(obj);
                        destroyedCount++;
                    }
                }
            }

            Debug.LogWarning($"[UIDebugUtility] NUCLEAR CLEANUP COMPLETE: Destroyed {destroyedCount} objects");
            Debug.LogWarning("[UIDebugUtility] You may need to reload the scene or restart the game");

            // Log remaining canvases
            LogAllCanvases();
        }

        /// <summary>
        /// Check if MainMenu is currently visible and report detailed diagnostics
        /// Call from Unity console: PlunkAndPlunder.UI.UIDebugUtility.DiagnoseMenuBug();
        /// </summary>
        public static void DiagnoseMenuBug()
        {
            Debug.Log("========== DIAGNOSING MAIN MENU OVERLAY BUG ==========");

            // Find all GameObjects with "MainMenu" in the name
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<GameObject> menuObjects = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("MainMenu") || obj.name == "MainMenu")
                {
                    menuObjects.Add(obj);
                }
            }

            Debug.Log($"Found {menuObjects.Count} objects with 'MainMenu' in name:");

            foreach (GameObject obj in menuObjects)
            {
                Debug.Log($"\n--- {GetGameObjectPath(obj)} ---");
                Debug.Log($"  Active in hierarchy: {obj.activeInHierarchy}");
                Debug.Log($"  Active self: {obj.activeSelf}");
                Debug.Log($"  Layer: {LayerMask.LayerToName(obj.layer)}");

                // Check for Image component (background)
                UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    Debug.Log($"  Image: enabled={img.enabled}, color={img.color}, raycastTarget={img.raycastTarget}");
                }

                // Check for CanvasGroup
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    Debug.Log($"  CanvasGroup: alpha={cg.alpha}, blocksRaycasts={cg.blocksRaycasts}, interactable={cg.interactable}");
                }

                // Check for GraphicRaycaster
                UnityEngine.UI.GraphicRaycaster raycaster = obj.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null)
                {
                    Debug.Log($"  GraphicRaycaster: enabled={raycaster.enabled}");
                }

                // Check for Canvas
                Canvas canvas = obj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"  Canvas: enabled={canvas.enabled}, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}");
                }
            }

            Debug.Log("\n========== DIAGNOSIS COMPLETE ==========");
            Debug.Log("If MainMenu is visible but alpha=0 or active=false, there may be duplicate objects.");
            Debug.Log("Run NuclearCleanup() to force-destroy all menu objects.");
        }
    }
}
