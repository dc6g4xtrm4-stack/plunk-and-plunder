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
        public const int UPGRADE_SHIP_COST = 75; // Legacy - kept for backward compatibility
        public const int DEPLOY_SHIPYARD_COST = 100;

        // Ship upgrade costs (specific types)
        public const int UPGRADE_SAILS_COST = 60; // Bigger Sails - increases movement
        public const int UPGRADE_CANNONS_COST = 80; // Bigger Cannons - increases combat effectiveness
        public const int UPGRADE_MAX_LIFE_COST = 100; // More Max Life - increases max health

        // Build queue
        public const int SHIP_BUILD_TIME = 3; // Ships take 3 turns to build
        public const int MAX_QUEUE_SIZE = 5; // Maximum 5 items in build queue

        // Ship stats
        public const int SHIP_MAX_HEALTH = 10; // Base ships start with 10 HP
        public const int UPGRADED_SHIP_TIER_2_MAX_HEALTH = 20; // Tier 2 upgraded ships
        public const int UPGRADED_SHIP_TIER_3_MAX_HEALTH = 30; // Tier 3 max upgraded ships
        public const int MAX_SHIP_TIER = 30; // Maximum upgrade tier (30 HP)
        public const int MAX_SAILS_UPGRADES = 5; // Maximum sail upgrades per ship
        public const int MAX_CANNONS_UPGRADES = 5; // Maximum cannon upgrades per ship
    }
}
