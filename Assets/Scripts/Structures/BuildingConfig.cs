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
        public const int SHIP_MAX_HEALTH = 1; // MVP: simple 1 HP ships
        public const int UPGRADED_SHIP_MAX_HEALTH = 2; // Upgraded ships get more HP
    }
}
