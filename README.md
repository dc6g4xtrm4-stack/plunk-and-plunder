# Plunk & Plunder

A Civilization-style simultaneous-turn tactics game built in Unity with Steamworks networking support.

## Overview

**Plunk & Plunder** is a hex-grid naval tactics game where 2-4 players command fleets across a procedurally generated island-dotted ocean. Players plan their moves simultaneously, with all actions resolving deterministically at the end of each turn. The last player standing wins!

### Key Features

- **Simultaneous Turn System**: All players plan moves during the Planning phase; moves resolve deterministically during Resolution
- **Hex Grid Navigation**: Ships move on sea and harbor tiles using A* pathfinding
- **Procedural Map Generation**: Islands with harbors generated from a seed for reproducibility
- **Deterministic Resolution**: Identical inputs always produce identical results across all clients
- **AI Opponents**: Simple AI that moves toward enemies and harbors
- **Offline & Online**: Play locally vs AI or host/join Steam lobbies
- **Code-Driven UI**: All UI created programmatically via UIBootstrapper

## Game Design

### Map

- **~500 sea tiles** arranged in a rough circular pattern
- **20-30 islands** each consisting of 4-8 hex tiles (LAND + HARBOR tiles)
- Ships can move on SEA and HARBOR tiles, but not LAND

### Players

- **Default: 4 players** (1 human + 3 AI for offline mode)
- Designed for future network multiplayer (AI can be replaced by remote players)

### Units & Structures

- **SHIP**: Basic unit that can move and engage in combat
- **HARBOR**: Structure on islands that ships can dock at (future: capture mechanics)
- **SHIPYARD** (placeholder): Future structure for building/upgrading ships

### Turn System

1. **Planning Phase**: Players queue move orders for their ships
2. **Commit Phase**: Players submit orders (AI auto-submits)
3. **Resolution Phase**: All orders resolve simultaneously using deterministic rules:
   - Movement conflicts (same destination): Both units bounce back
   - Swap positions: Allowed
   - Adjacent enemies after movement: Both destroyed (simple combat)

### Win Condition

Eliminate all opponents (last player with ships remaining wins)

## Architecture

### Core Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Hex Coordinate System** | Axial (q, r) | Clean, efficient, well-documented |
| **UI Framework** | uGUI (programmatic) | Unity-standard, good networking support |
| **Collision Resolution** | Bounce back | Deterministic and simple |
| **Combat** | Mutual destruction | MVP-friendly, easy to extend |
| **Elimination** | No ships remaining | Clear win condition |

### Project Structure

```
Assets/Scripts/
├── Core/               # GameManager, GameState, GameEvents
├── Map/                # HexCoord, HexGrid, MapGenerator, Pathfinding
├── Units/              # Unit, Ship, UnitManager
├── Structures/         # Structure, Harbor, StructureManager
├── Orders/             # IOrder, MoveOrder, OrderValidator
├── Resolution/         # TurnResolver, DeterministicRandom
├── Players/            # Player, PlayerManager
├── AI/                 # AIController, SimpleAI
├── Networking/         # INetworkTransport, OfflineTransport, SteamTransport
├── UI/                 # UIBootstrapper, MainMenuUI, LobbyUI, GameHUD, etc.
├── Rendering/          # HexRenderer, UnitRenderer, CameraController
└── Utilities/          # SerializationHelper
```

### Key Systems

#### GameManager
Central state machine controlling game flow:
```
MainMenu → Lobby → Planning → Resolving → Planning → ... → GameOver
```

#### TurnResolver
Deterministic turn resolution engine:
- Sorts all orders by unit ID
- Resolves movements (detects collisions)
- Resolves combat (adjacent enemies)
- Checks for player elimination

#### Networking Layer
Interface-based transport system:
- `OfflineTransport`: Local play with AI
- `SteamTransport`: Steamworks lobby + P2P (stub implementation)

## Setup Instructions

### Prerequisites

- **Unity 6 (6000.3.5f2)** or newer (also compatible with Unity 2022.3 LTS+)
- **Windows PC** (game targets Windows)
- **(Optional) Steamworks.NET** for online multiplayer

> **Note**: This project has been upgraded to Unity 6 with improved performance and uGUI 2.0. See [UNITY6_UPGRADE.md](UNITY6_UPGRADE.md) for details.

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/plunk-and-plunder.git
   cd plunk-and-plunder
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Add" and select the `plunk-and-plunder` folder
   - Open the project (Unity will import assets)

3. **Open the main scene**
   - Navigate to `Assets/Scenes/MainScene.unity`
   - Double-click to open

### Running Offline

1. **Press Play** in Unity Editor
2. Click **"Play Offline (1 Human + 3 AI)"** in the main menu
3. The game will generate a random map and start with you as Player 0 vs 3 AI opponents

### Controls

- **Left Click**: Select your ship
- **Right Click**: Set destination for selected ship
- **WASD / Arrow Keys**: Pan camera
- **Mouse Wheel**: Zoom in/out
- **Middle Mouse Drag**: Pan camera
- **Submit Orders Button**: End your turn (top-right during Planning phase)
- **Auto-Resolve Button**: Debug tool to skip to resolution

### Playing the Game

1. **Select a ship** (left-click on your red cylinder units)
2. **Right-click** on the map to set a destination
3. Ships will find a path to the destination (up to 3 tiles per turn for MVP)
4. Click **"Submit Orders"** when ready
5. AI players will auto-submit, then the turn resolves
6. Watch the **Event Log** (bottom-right) to see what happened
7. Repeat until only one player remains!

### Combat Rules

- Ships that end a turn **adjacent to an enemy** will engage in combat
- Combat is **mutual destruction**: both ships are destroyed
- Avoid moving next to enemies unless you want to trade!

## Steam Integration (Future)

The project includes stub implementation for Steamworks networking.

### To Enable Steam:

1. **Install Steamworks.NET**
   - Download from: https://github.com/rlabrecque/Steamworks.NET
   - Import into `Assets/Plugins/`

2. **Configure Steam App ID**
   - Create `steam_appid.txt` in project root with your app ID
   - Update `SteamTransport.cs` to initialize Steamworks

3. **Implement P2P Networking**
   - Complete the `SteamTransport` class with:
     - `SteamMatchmaking` for lobbies
     - `SteamNetworking` for P2P messages
     - Callbacks for lobby events

4. **Host/Join Flow**
   - Click "Host Game (Steam)" to create a lobby
   - Share lobby ID with friends
   - Friends click "Join Game (Steam)" and enter lobby ID

### Network Architecture

- **Host Authoritative**: Host collects orders, resolves turn, broadcasts results
- **Deterministic Validation**: Clients can validate results independently
- **State Synchronization**: Full game state sent after each resolution

## Debug Features

### Map Seed
Control map generation for testing:
```csharp
GameManager.Instance.debugMapSeed = 12345;
```

### Auto-Resolve
Skip planning and immediately resolve the turn (in GameHUD)

### Deterministic Logging
Enable detailed turn resolution logs:
```csharp
GameManager.Instance.enableDeterministicLogging = true;
```

## Known Limitations

### MVP Constraints
1. **Single Move Per Turn**: Units move to final destination in one step (no tick-by-tick pathfinding)
2. **Simple Combat**: Adjacent enemies destroy each other (no HP, damage, or tactics)
3. **No Structure Interaction**: Harbors exist but can't be captured/used yet
4. **Fixed Player Count**: Always 4 players in offline mode
5. **Steam Stub**: Steamworks integration requires manual setup

### Technical Debt
1. **Serialization**: Dictionary-based state not fully JSON-serializable (needs custom converter)
2. **Unit Movement**: Could support multi-step paths with tick-based resolution
3. **UI Polish**: Tooltips, hover effects, and animations are minimal
4. **AI**: Simple AI doesn't consider tactics or avoid enemies strategically

## Future Enhancements

### Gameplay
- [ ] Harbor capture mechanics
- [ ] Shipyards that build new ships
- [ ] Ship upgrades and HP system
- [ ] Multiple ship types (fast scouts, heavy battleships)
- [ ] Fog of war / limited vision
- [ ] Resource collection and economy

### Technical
- [ ] Full Steamworks integration
- [ ] Replay system (save/load turn sequences)
- [ ] Spectator mode
- [ ] Match history and statistics
- [ ] Better AI with tactical planning

### Polish
- [ ] Ship movement animations
- [ ] Combat visual effects
- [ ] Sound effects and music
- [ ] Better camera controls (focus on action)
- [ ] Minimap
- [ ] Player avatars and customization

## Development

### Adding New Order Types

1. Create new order class implementing `IOrder`:
   ```csharp
   public class AttackOrder : IOrder
   {
       public string unitId { get; set; }
       public int playerId { get; set; }
       public HexCoord target;
       public OrderType GetOrderType() => OrderType.Attack;
   }
   ```

2. Add validation in `OrderValidator`
3. Handle resolution in `TurnResolver.ResolveTurn()`
4. Update UI to allow issuing the order

### Adding New Unit Types

1. Add to `UnitType` enum
2. Update `UnitManager.CreateUnit()` to set appropriate stats
3. Update `UnitRenderer` to use different visuals
4. Implement type-specific behavior in `TurnResolver`

### Modifying Map Generation

Edit `MapGenerator.GenerateMap()`:
```csharp
MapGenerator mapGen = new MapGenerator(seed);
HexGrid grid = mapGen.GenerateMap(
    numSeaTiles: 1000,     // Bigger ocean
    numIslands: 50,        // More islands
    minIslandSize: 2,
    maxIslandSize: 12
);
```

## Troubleshooting

### Unity won't open the project
- Ensure you're using Unity 6 (6000.0.35f2) or Unity 2022.3 LTS+
- Delete `Library/` folder and reopen
- If upgrading from an older Unity version, let Unity reimport all assets

### Scripts have errors
- Check that all `.cs` files are in correct folders
- Verify namespace imports
- Try Assets → Reimport All

### No UI appears
- Ensure MainScene.unity has GameBootstrap object
- Check that UIBootstrapper is being instantiated
- Verify Canvas is in ScreenSpaceOverlay mode

### Game freezes during turn resolution
- Check for infinite loops in TurnResolver
- Enable deterministic logging to see what's happening
- Verify pathfinding isn't timing out

### AI doesn't move
- Check that AIController is initialized
- Verify AI players are marked as `PlayerType.AI`
- Look for errors in SimpleAI.GenerateOrders()

## Credits

**Design & Implementation**: Built with Unity 6 (compatible with Unity 2022.3 LTS+)

**Architecture Pattern**: Interface-based networking, deterministic turn resolution

**Hex Grid Math**: Based on Red Blob Games hex grid guide

## License

This project is provided as-is for educational and development purposes.

---

**Version**: 1.0 MVP (Unity 6)
**Target Platform**: Windows PC (Steam)
**Unity Version**: 6000.3.5f2 (Unity 6)
**Backward Compatible**: Unity 2022.3 LTS+
**Last Updated**: January 2026
