using UnityEngine;

namespace PlunkAndPlunder.UI
{
    /// <summary>
    /// Shared styling constants for all HUD elements
    /// </summary>
    public static class HUDStyles
    {
        // Colors
        public static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.95f);
        public static readonly Color HeaderColor = new Color(1f, 0.8f, 0.2f, 1f); // Gold
        public static readonly Color BorderColor = new Color(1f, 0.8f, 0.2f, 0.8f); // Gold with transparency
        public static readonly Color TextColor = Color.white;
        public static readonly Color ButtonNormalColor = new Color(0.2f, 0.2f, 0.3f, 0.95f);
        public static readonly Color ButtonHoverColor = new Color(0.3f, 0.3f, 0.4f, 0.95f);
        public static readonly Color ButtonDisabledColor = new Color(0.15f, 0.15f, 0.2f, 0.7f);

        // Font Sizes
        public const int HeaderFontSize = 20;
        public const int ContentFontSize = 16;
        public const int SmallFontSize = 14;
        public const int LargeFontSize = 24;
        public const int ButtonFontSize = 18;

        // Reference Resolution (must match UIBootstrapper Canvas settings)
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;

        // Spacing & Layout
        public const int PanelPadding = 10;
        public const int SectionSpacing = 10;
        public const int EdgeMargin = 10;
        public const int TopBarHeight = 100;
        public const int BottomBarHeight = 60;

        // Panel Dimensions
        public const int LeftPanelWidth = 380;
        public const int RightPanelWidth = 400;
        public const int ButtonHeight = 50;
        public const int ButtonSpacing = 10;

        // Section Heights
        public const int UnitDetailsSectionHeight = 180; // Reduced from 200 to fit more buttons
        public const int BuildQueueSectionHeight = 180; // Reduced from 200 to fit more buttons

        // Pass Turn Button
        public const int PassTurnButtonWidth = 200;
        public const int PassTurnButtonHeight = 60;
        public static readonly Color PassTurnNormalColor = new Color(0.2f, 0.4f, 0.2f);
        public static readonly Color PassTurnReadyColor = new Color(0f, 0.6f, 0f);
        public static readonly Color PassTurnPulseColor = new Color(0f, 1f, 0f);
    }
}
