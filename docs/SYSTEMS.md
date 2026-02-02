# Technical Systems Documentation

## Core Architecture

### GameEngine (Deterministic Core)
- Pure C# class with no Unity dependencies
- Contains ALL game rule logic
- Used by GameManager (UI), HeadlessSimulation (batch), and NetworkManager (multiplayer)
- Location: `Assets/Scripts/Core/GameEngine.cs`

### Game Loop
1. **Planning Phase**: Players submit orders
2. **Resolution Phase**: Orders validated and executed
3. **Combat Phase**: Encounters and combat resolved
4. **Turn Advance**: Income awarded, construction processed

## Major Systems

### HUD System (Redesigned)
**Components:**
- `TopBarHUD`: Turn info, resources, Pass Turn button
- `LeftPanelHUD`: Player stats, selection details, action buttons
- `HUDStyles`: Centralized styling constants
- `HUDLayoutManager`: UI creation helpers

**Features:**
- Modern Unity layout system (no hardcoded positions)
- Context-sensitive action buttons
- Integrated player stats
- Consistent gold/dark theme

### Replay System
**Recording:**
- `ReplayManager`: Records all game events to log file
- `SimulationLogParser`: Parses event logs
- `ReplayStateReconstructor`: Rebuilds game state from events

**Playback:**
- `ReplayControlsUI`: Playback controls (play, pause, speed, scrub)
- Frame-by-frame or continuous playback
- Speed control (0.5x to 4x)

**Files:**
- Logs stored in: `AppData/LocalLow/DefaultCompany/Plunk and Plunder/Logs/`
- Format: `gameplay_YYYY-MM-DD_HH-MM-SS.log`

### Encounter System (Combat Redesign)
**OLD System:** Simple collision detection
**NEW System:** Pre-combat decision framework

**Encounter Types:**
- `PASSING`: Units moving through each other → decide ATTACK or PROCEED
- `ENTRY`: Unit entering occupied tile → decide ATTACK or YIELD

**Implementation:**
- `Encounter.cs`: Encounter decision tracking
- `GameEngine.ResolveEncounters()`: Decision resolution
- AI automatically decides based on health (yield if < 50%)

### Construction System
**Components:**
- `ConstructionManager`: Singleton managing all construction jobs
- `ConstructionProcessor`: Processes queues each turn
- `ConstructionValidator`: Validates build commands

**Features:**
- Queue multiple ships per shipyard
- 3-turn build time per ship
- Cost: 50 gold per ship
- Automatic completion and unit spawning

### Combat System
**Resolution:**
- `TurnResolver.ResolveCombat()`: Deterministic combat logic
- Ships attack with cannons (damage = cannon count)
- Adjacent combat after movement resolution
- Combat results displayed in `CombatResultsHUD`

**Mechanics:**
- Simultaneous damage application
- Health tracking per unit
- Death checks after combat
- Shipyard destruction eliminates player

## File Structure

```
Assets/Scripts/
├── Core/           # Game engine, managers, bootstrap
├── UI/             # HUD components, menus, screens
├── Rendering/      # Visual rendering (ships, buildings, tiles)
├── Combat/         # Encounter and combat systems
├── Construction/   # Construction queue and validation
├── Replay/         # Replay recording and playback
├── Networking/     # TCP transport and network manager
├── Orders/         # Order types and validation
├── Resolution/     # Turn resolution and combat
├── Simulation/     # Headless simulation mode
├── Map/            # Hex grid, pathfinding, generation
├── Units/          # Unit data and management
├── Structures/     # Structure data and management
└── Players/        # Player data and management
```

## Order Validation Pipeline
1. `GameHUD`: Player creates order (move, upgrade, build, etc.)
2. `GameManager.SubmitOrders()`: Batches orders
3. `GameEngine.ValidateOrders()`: Switch statement by order type
4. `OrderValidator.ValidateXXXOrder()`: Specific validation logic
5. `TurnResolver.ResolveXXXOrders()`: Executes valid orders

## Event System
- All game actions generate events (move, combat, build, etc.)
- Events stored in `GameState.eventHistory`
- UI subscribes to events for visual feedback
- Replay system serializes events to log file

## Testing
- See `docs/TROUBLESHOOTING.md` for debug guides
- Use Unity editor for rapid iteration
- Build client with `create_client_build.bat` for multiplayer testing
