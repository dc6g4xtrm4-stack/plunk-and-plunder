# Plunk and Plunder

A turn-based naval strategy game built in Unity. Command your fleet, build shipyards, upgrade ships, and destroy enemy strongholds!

## Download & Play

**Latest Release:** [Download PlunkAndPlunder-Windows.zip](https://github.com/dc6g4xtrm4-stack/plunk-and-plunder/releases/latest)

1. Download the latest release
2. Extract the ZIP file
3. Run `PlunkAndPlunder.exe`
4. See [MULTIPLAYER_SETUP.md](MULTIPLAYER_SETUP.md) for playing with friends!

## Quick Start

### Single Player
1. Launch game → "Start Game"
2. Select ship → Right-click to move → "Pass Turn"
3. Destroy all enemy shipyards to win!

### Multiplayer
1. **Host**: Click "Host" → Share your ngrok/IP address
2. **Client**: Enter host address → Click "Join"

**See [MULTIPLAYER_SETUP.md](MULTIPLAYER_SETUP.md) for detailed setup guide (ngrok, port forwarding, VPN options)**

## Game Features

- **Turn-Based Strategy**: Plan moves, submit orders, watch resolution
- **Ship Upgrades**: Enhance speed, firepower, and durability at shipyards
- **Deterministic Combat**: Predictable, skill-based naval warfare
- **Multiplayer**: TCP-based network play for 2-4 players
- **Replay System**: Record and playback entire matches
- **Modern UI**: Clean, consistent interface with contextual controls

## Documentation

### Getting Started
- [`docs/QUICKSTART.md`](docs/QUICKSTART.md) - Quick start guide
- [`PLUNK_AND_PLUNDER_RULEBOOK.md`](PLUNK_AND_PLUNDER_RULEBOOK.md) - Complete game rules

### Multiplayer
- [`MULTIPLAYER_SETUP.md`](MULTIPLAYER_SETUP.md) - **How to play with friends (ngrok, port forwarding, VPN)**
- [`docs/MULTIPLAYER.md`](docs/MULTIPLAYER.md) - Network setup and testing

### Development
- [`GITHUB_ACTIONS_SETUP.md`](GITHUB_ACTIONS_SETUP.md) - **Automated builds and releases**

### Technical
- [`docs/SYSTEMS.md`](docs/SYSTEMS.md) - System architecture and implementation
- [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md) - Debug guides and common issues
- [`CHANGELOG.md`](CHANGELOG.md) - Recent changes and version history

## Controls

- **Left Click**: Select unit/structure
- **Right Click**: Move selected unit
- **ESC**: Open in-game menu
- **Pass Turn Button**: Submit orders

## Game Mechanics

### Resources
- Earn **100 gold** per shipyard per turn
- Spend gold on ships (50g), shipyards (100g), upgrades (60-100g)

### Ships
- Move up to 2 tiles per turn
- Attack adjacent enemies automatically
- Upgrade at shipyards: Sails, Cannons, Max Life

### Shipyards
- Deploy on harbor tiles (100g)
- Build ships (50g, 3 turns)
- Upgrade your fleet

### Victory
- Eliminate all enemy shipyards!

## Development

### Built With
- Unity 6.0+
- C# .NET
- Unity UGUI

### Project Structure
```
Assets/Scripts/
├── Core/           # Game engine and managers
├── UI/             # HUD and menu systems
├── Combat/         # Combat and encounter systems
├── Networking/     # Multiplayer implementation
├── Replay/         # Replay recording/playback
└── ...             # See docs/SYSTEMS.md for full structure
```

### Building
- **Client Build**: Run `create_client_build.bat`
- **Development**: Open in Unity Editor

## Known Issues
- Replay system not compatible with multiplayer (yet)
- No reconnection if client disconnects

## License
[Add license information]

## Credits
[Add credits]

---

**Latest Update**: Ship upgrade system, HUD redesign, health bar fixes, and Pass Turn button improvements.
