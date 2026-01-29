namespace PlunkAndPlunder.Structures
{
    /// <summary>
    /// Configuration for building costs and capabilities
    /// </summary>
    public static class BuildingConfig
    {
        // Shipyard costs
        public const int BUILD_SHIP_COST = 50;
        public const int REPAIR_SHIP_COST = 20;
        public const int UPGRADE_SHIP_COST = 75;
        public const int DEPLOY_SHIPYARD_COST = 100;

        // Ship stats
        public const int SHIP_MAX_HEALTH = 1; // Base ships
        public const int UPGRADED_SHIP_TIER_2_MAX_HEALTH = 2; // Tier 2 upgraded ships
        public const int UPGRADED_SHIP_TIER_3_MAX_HEALTH = 3; // Tier 3 max upgraded ships
        public const int MAX_SHIP_TIER = 3; // Maximum upgrade tier
    }
}
