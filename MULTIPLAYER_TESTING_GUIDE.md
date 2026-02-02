# Multiplayer Testing Guide

This guide explains how to test Plunk & Plunder with friends using direct TCP connections before your Steam release.

## Quick Start

### 1. Host a Game (You)

1. **Build the game** (File → Build Settings → Build)
2. **Find your local IP address**:
   - Windows: Open Command Prompt and run `ipconfig`
   - Look for "IPv4 Address" (e.g., `192.168.1.100`)
3. **Port forwarding** (if friends are outside your network):
   - Forward port **7777** TCP on your router to your PC
   - You may need to allow port 7777 through Windows Firewall
4. **Launch the game** and click **"Host Direct Connection"**
5. **Share your IP** with friends:
   - Local network: `192.168.1.X` (your local IP)
   - Internet: Your public IP (Google "what is my ip") + port forward setup

### 2. Join a Game (Your Friends)

1. **Get the build** from you (copy the entire build folder)
2. **Launch the game**
3. **Enter host IP** in the input field:
   - Local network: `192.168.1.100:7777`
   - Internet: `203.0.113.42:7777` (use actual public IP)
   - If using default port 7777, just enter the IP: `192.168.1.100`
4. **Click "Join Direct Connection"**

## Network Setup Options

### Option A: Local Network (LAN) - Easiest
**Best for**: Testing with friends in the same house/building

- No port forwarding needed
- Use local IP addresses (192.168.x.x or 10.0.x.x)
- Very fast and stable

**Steps**:
1. All computers on same WiFi/network
2. Host shares local IP (from `ipconfig`)
3. Friends join using that local IP

### Option B: Port Forwarding - Medium Difficulty
**Best for**: Testing with friends over the internet

**Router Configuration**:
1. Log into your router (usually 192.168.1.1 or 192.168.0.1)
2. Find "Port Forwarding" or "Virtual Server" settings
3. Create a new rule:
   - **Service Name**: Plunk and Plunder
   - **Port Range**: 7777-7777
   - **Protocol**: TCP
   - **Internal IP**: Your PC's local IP (from ipconfig)
4. Save and restart router if needed

**Windows Firewall**:
1. Open Windows Defender Firewall
2. Click "Advanced settings"
3. Click "Inbound Rules" → "New Rule"
4. Port → TCP → Specific port: 7777
5. Allow the connection
6. Apply to all profiles (Domain, Private, Public)

**Share with friends**:
- Your public IP (Google "what is my ip")
- Friends join using: `YOUR_PUBLIC_IP:7777`

### Option C: Virtual LAN (Hamachi/ZeroTier) - Easy Alternative
**Best for**: Avoiding port forwarding headaches

**Using Hamachi** (free for up to 5 players):
1. All players download and install LogMeIn Hamachi
2. Host creates a network
3. Friends join the network
4. Use Hamachi virtual IP addresses (looks like 25.x.x.x)
5. No port forwarding needed!

**Using ZeroTier** (free, unlimited players):
1. Create account at zerotier.com
2. Create a network
3. All players install ZeroTier and join network
4. Use ZeroTier virtual IPs

## Building for Distribution

### Windows Build

1. **File → Build Settings**
2. **Platform**: PC, Mac & Linux Standalone
3. **Target Platform**: Windows
4. **Architecture**: x86_64
5. **Click "Build"** and choose output folder
6. **Distribute**:
   - Zip the entire build folder
   - Share with friends via Google Drive, Dropbox, etc.

### Important Files to Include
- `PlunkAndPlunder.exe` (main executable)
- `UnityPlayer.dll`
- `UnityCrashHandler64.exe`
- `PlunkAndPlunder_Data/` (entire folder)
- `MonoBleedingEdge/` (entire folder)

## Testing Checklist

### Pre-Game Testing
- [ ] Host can see lobby screen
- [ ] Host's firewall allows port 7777
- [ ] Friends can ping host's IP
- [ ] Friends have the correct game build version

### In-Game Testing
- [ ] Friends appear in lobby
- [ ] All players can mark as "ready"
- [ ] Game starts when host clicks "Start"
- [ ] Turn orders are synced
- [ ] Game state updates properly
- [ ] Combat resolution is deterministic
- [ ] Game doesn't desync

### Known Limitations (Current Build)
- **Max 4 players** (1 host + 3 clients)
- **Host-authoritative**: If host disconnects, game ends
- **No reconnection**: If a client drops, they can't rejoin
- **No lobby browser**: Must share IP manually
- **No NAT punchthrough**: Port forwarding required for internet play

## Troubleshooting

### "Cannot connect" Error

**Check**:
1. Is host actually running and in lobby?
2. Is the IP address correct?
3. Is port 7777 open on host's firewall?
4. Is port forwarding configured correctly (if over internet)?
5. Try connecting to `127.0.0.1:7777` from host machine first (loopback test)

**Test connectivity**:
```bash
# From friend's computer, test if host port is reachable
telnet HOST_IP 7777

# Or use PowerShell
Test-NetConnection -ComputerName HOST_IP -Port 7777
```

### "Connection Lost" During Game

**Causes**:
- Host computer went to sleep
- Firewall blocked connection after initial handshake
- Router closed port due to inactivity
- Network instability

**Solutions**:
- Keep host computer active
- Set Windows power plan to "High Performance"
- Check router logs for connection drops

### Players Not Appearing in Lobby

**Check**:
1. Is NetworkManager processing events? (check console logs)
2. Are messages being sent/received? (enable Debug.Log in TCPTransport.cs)
3. Is lobby UI updating correctly?

### Desync Issues (Game State Differs)

This indicates a bug in deterministic game logic. Report with:
- Turn number where desync occurred
- All player action logs
- Host and client console logs

## Network Architecture (Technical)

### How It Works
- **Transport**: Custom TCP socket implementation
- **Port**: 7777 (TCP)
- **Pattern**: Host-Client (one player acts as server)
- **Message Format**: Length-prefixed JSON
- **Synchronization**: Host runs simulation, broadcasts results

### Message Flow
1. Client connects → sends `JOIN:playerId`
2. Host accepts → broadcasts player list
3. Players mark ready → `PlayerReady` message
4. Host starts game → broadcasts `GameStateUpdate`
5. Each turn:
   - Players submit orders → `OrdersSubmitted`
   - Host resolves turn → `TurnResolved`
   - Host broadcasts new state → `GameStateUpdate`

### Security Notes (For Testing Only!)
⚠️ **This implementation is for testing only** and lacks:
- No encryption (data sent in plaintext)
- No authentication (anyone can connect)
- No DDoS protection
- No cheat prevention

For the Steam release, use Steamworks P2P networking which includes:
- Encrypted communications
- Steam account authentication
- NAT traversal (no port forwarding)
- Built-in anti-cheat hooks

## Next Steps

After successful testing, you'll want to:

1. **Complete SteamTransport implementation** (Assets/Scripts/Networking/SteamTransport.cs)
   - Install Steamworks.NET package
   - Implement Steam lobby creation/joining
   - Use Steam P2P networking instead of TCP

2. **Add proper lobby browser**
   - Steam friends list integration
   - Lobby list with filters
   - Invite system

3. **Implement reconnection**
   - Save player session tokens
   - Allow rejoin after disconnect
   - Resume game state

4. **Add dedicated server support** (optional)
   - Linux headless builds
   - Persistent servers
   - Matchmaking system

## Support

If you encounter issues:
1. Check Unity console logs (both host and client)
2. Enable verbose logging in TCPTransport.cs
3. Test on local network first before internet
4. Verify firewall rules
5. Try Hamachi/ZeroTier as alternative

Good luck with your testing! 🎮
