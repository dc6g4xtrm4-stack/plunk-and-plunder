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

        // Structure upgrade costs
        public const int UPGRADE_TO_NAVAL_YARD_COST = 300;      // Shipyard → Naval Yard
        public const int UPGRADE_TO_NAVAL_FORTRESS_COST = 800;  // Naval Yard → Naval Fortress

        // Galleon costs (only buildable at Naval Fortress)
        public const int BUILD_GALLEON_COST = 200;              // Galleons are expensive
        public const int GALLEON_BUILD_TIME = 5;                // Takes 5 turns to build
        public const int UPGRADE_GALLEON_SAILS_COST = 150;      // Enhanced sails for Galleons
        public const int UPGRADE_GALLEON_CANNONS_COST = 200;    // Enhanced cannons for Galleons
        public const int MAX_GALLEON_SAILS_UPGRADES = 3;        // Max additional sail upgrades (starts with 2)
        public const int MAX_GALLEON_CANNONS_UPGRADES = 3;      // Max additional cannon upgrades (starts with 7)

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

        // Galleon stats
        public const int GALLEON_MAX_HEALTH = 30; // Galleons start with 30 HP
        public const int GALLEON_BASE_SAILS = 2; // Galleons start with 2 sail upgrades
        public const int GALLEON_BASE_CANNONS = 7; // Galleons start with 7 cannons

        // Pirate stats
        public const int PIRATE_SHIP_MAX_HEALTH = 15; // Pirates are tougher
        public const int PIRATE_GOLD_REWARD_MIN = 10000; // Minimum gold for killing a pirate
        public const int PIRATE_GOLD_REWARD_MAX = 20000; // Maximum gold for killing a pirate
        public const int PIRATE_DAMAGE_MULTIPLIER = 2; // Pirates do 2x damage
    }
}
