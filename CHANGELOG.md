# Changelog

All notable changes to "Plunk & Plunder" will be documented in this file.

## [1.0.1] - 2026-01-29 - Unity 6 Upgrade

### Changed
- **Upgraded to Unity 6000.3.5f2** from Unity 2022.3
- Updated all package dependencies to Unity 6 versions
- Project settings upgraded to Unity 6 format (serializedVersion 28)

### Added
- `UNITY6_UPGRADE.md` - Comprehensive Unity 6 upgrade documentation
- `CHANGELOG.md` - This file
- Unity 6 Multiplayer Center package (1.0.1)
- Enhanced backward compatibility notes in README

### Package Updates
- `com.unity.ugui`: 1.0.0 → 2.0.0 (uGUI improvements)
- `com.unity.timeline`: 1.7.5 → 1.8.10
- `com.unity.visualscripting`: 1.9.0 → 1.9.9
- `com.unity.collab-proxy`: 2.0.5 → 2.11.2
- `com.unity.feature.development`: 1.0.1 → 1.0.2

### Technical
- All 36 C# scripts verified compatible with Unity 6
- No breaking changes or deprecated API usage
- Maintained backward compatibility with Unity 2022.3 LTS+
- Performance improvements from Unity 6's enhanced IL2CPP and rendering

### Documentation Updates
- Updated README.md with Unity 6 prerequisites
- Updated PROJECT_SUMMARY.md with Unity version info
- Updated troubleshooting section for Unity 6
- Added Unity 6 benefits and migration notes

## [1.0.0] - 2026-01-29 - Initial Release

### Added
- Complete Unity project structure with 36 C# scripts
- **Core Systems**: GameManager, GameState, GameEvents, GameBootstrap
- **Map Systems**: Hex grid with axial coordinates, procedural island generation, A* pathfinding
- **Gameplay Systems**: Units, structures, orders, deterministic turn resolution
- **Player Systems**: Player management, AI controller with simple AI
- **Networking**: Interface-based transport (OfflineTransport + SteamTransport stub)
- **Rendering**: Hex renderer, unit renderer, camera controller
- **UI Systems**: Code-driven uGUI (main menu, lobby, game HUD, event log, tooltips)

### Features
- 4-player simultaneous turn-based gameplay
- Procedurally generated maps (~500 sea tiles, 20-30 islands)
- Deterministic turn resolution (network-safe)
- Simple combat system (adjacent enemies destroy each other)
- AI opponents that move toward enemies and harbors
- Offline play (1 human + 3 AI)
- Steamworks networking architecture (ready for integration)

### Documentation
- Comprehensive README.md with setup instructions
- PROJECT_SUMMARY.md with architecture overview
- Complete code documentation and comments

---

## Version History

- **1.0.1** (2026-01-29): Unity 6 upgrade
- **1.0.0** (2026-01-29): Initial MVP release

## Future Roadmap

### Planned Features
- [ ] Full Steamworks.NET integration
- [ ] Harbor capture mechanics
- [ ] Shipyard building and ship construction
- [ ] Multiple ship types (scouts, battleships, etc.)
- [ ] Advanced combat with HP and damage
- [ ] Fog of war
- [ ] Sound effects and music
- [ ] Visual effects and animations
- [ ] Better AI with tactical planning

### Technical Improvements
- [ ] Migrate to Addressables
- [ ] Implement Unity 6 Entities (DOTS) for large-scale battles
- [ ] Enhanced multiplayer with Unity Netcode for GameObjects
- [ ] Replay system
- [ ] Save/load functionality

---

**Current Version**: 1.0.1
**Unity Version**: 6000.3.5f2
**Status**: Stable, ready for testing
