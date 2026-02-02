# Multiplayer Guide

## Quick Setup (Local Network)

### Host Setup
1. Launch game → "Host Game"
2. Find your IP address:
   - Windows: Open cmd → `ipconfig` → Look for "IPv4 Address"
   - Mac/Linux: Open terminal → `ifconfig` → Look for inet address
3. Share your IP with other players

### Client Setup
1. Launch game → "Join Game"
2. Enter host's IP address
3. Click "Connect"
4. Wait for game to start

## TCP Multiplayer (Advanced)

### Port Configuration
- Default port: 7777
- Ensure firewall allows connections on this port

### Network Requirements
- All players on same local network (LAN)
- Or use port forwarding for internet play

### Troubleshooting
- **Connection Failed**: Check firewall settings
- **Disconnection**: Ensure stable network connection
- **Sync Issues**: All clients must have same game version

## Testing Multiplayer

### Local Test (Same Computer)
1. Build client executable: Run `create_client_build.bat`
2. Launch host from Unity editor
3. Launch client from build folder
4. Client connects to "localhost" or "127.0.0.1"

### LAN Test
1. Host shares IP address
2. Clients join using host's IP
3. Test gameplay and turn synchronization

## Implementation Details

### Network Architecture
- TCP-based transport layer
- Turn-based synchronization
- Deterministic game state
- All game logic runs on host, clients receive updates

### Files
- `TCPTransport.cs`: TCP networking implementation
- `NetworkManager.cs`: Game state synchronization
- `GameEngine.cs`: Deterministic game logic (no Unity dependencies)

## Known Issues
- Replay system not yet compatible with multiplayer
- No reconnection support if client disconnects
