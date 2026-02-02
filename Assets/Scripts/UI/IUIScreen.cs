using UnityEngine;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Interface for all UI screens with proper lifecycle management
    /// </summary>
    public interface IUIScreen
    {
        /// <summary>
        /// Show this screen (enable GameObject, make raycasts active)
        /// </summary>
        void Show();

        /// <summary>
        /// Hide this screen (disable GameObject, stop raycasts)
        /// </summary>
        void Hide();

        /// <summary>
        /// Dispose this screen (destroy GameObject, unregister events, release resources)
        /// CRITICAL: This must destroy all visual components and stop all interaction
        /// </summary>
        void Dispose();

        /// <summary>
        /// Check if this screen is currently visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Get the root GameObject for this screen
        /// </summary>
        GameObject GetRootObject();
    }
}
