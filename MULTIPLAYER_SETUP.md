# Multiplayer Setup Guide - Plunk & Plunder

## Quick Start with ngrok (Easiest - No Port Forwarding!)

### Step 1: Download ngrok
1. Go to https://ngrok.com/download
2. Sign up for free account
3. Download ngrok for Windows
4. Extract `ngrok.exe` to a folder (e.g., `C:\ngrok\`)

### Step 2: Authenticate ngrok (One-time setup)
1. Get your authtoken from https://dashboard.ngrok.com/get-started/your-authtoken
2. Open Command Prompt and run:
   ```bash
   ngrok config add-authtoken YOUR_TOKEN_HERE
   ```

### Step 3: Start the Tunnel
1. Open Command Prompt
2. Navigate to ngrok folder: `cd C:\ngrok`
3. Run:
   ```bash
   ngrok tcp 7777
   ```

4. You'll see output like this:
   ```
   Session Status: online
   Forwarding: tcp://0.tcp.ngrok.io:12345 -> localhost:7777
   ```

5. **Copy the forwarding address!** Example: `0.tcp.ngrok.io:12345`

### Step 4: Host the Game
1. Launch Plunk & Plunder
2. Click **"Host"** button
3. Share the ngrok address with your friend: `0.tcp.ngrok.io:12345`

### Step 5: Friend Joins
1. Friend launches Plunk & Plunder
2. Enters your ngrok address in the text field: `0.tcp.ngrok.io:12345`
3. Clicks **"Join"**
4. Connected! 🎮

---

## Alternative: Direct Connection with Port Forwarding

If you prefer not to use ngrok or want a permanent setup:

### For Host:
1. **Port Forward on Router:**
   - Access router admin (usually `192.168.1.1`)
   - Find "Port Forwarding" or "Virtual Server"
   - Add rule: External Port 7777 → Internal Port 7777 → Your PC's local IP
   - Protocol: TCP

2. **Allow Windows Firewall:**
   - Windows Security → Firewall → Advanced Settings
   - Inbound Rules → New Rule → Port → TCP → 7777 → Allow

3. **Get Public IP:**
   - Visit https://whatismyipaddress.com
   - Copy your public IP (e.g., `73.45.123.89`)

4. **Host Game:**
   - Click "Host" in game
   - Share your public IP with port: `73.45.123.89:7777`

### For Friend:
1. Enter host's IP:Port in text field
2. Click "Join"

---

## Alternative: Hamachi/Radmin VPN (Virtual LAN)

Both players install VPN software to create virtual local network:

### Option A: Hamachi
1. Download from https://vpn.net
2. Both players install and create/join same network
3. Use Hamachi IP (looks like `25.x.x.x:7777`)

### Option B: Radmin VPN (Free, Unlimited)
1. Download from https://www.radmin-vpn.com
2. Both players install and join same network
3. Use Radmin VPN IP with port 7777

---

## Troubleshooting

### "Connection Failed"
- ✅ Make sure host clicked "Host" BEFORE friend tries to join
- ✅ Check firewall isn't blocking port 7777
- ✅ If using ngrok, make sure tunnel is running
- ✅ Verify the IP:Port format is correct (no spaces, includes :7777)

### "Timeout"
- ✅ Host's game must be running and in lobby
- ✅ Check Windows Firewall settings
- ✅ Try switching who hosts

### ngrok Session Expired
- Free tier: 8-hour session limit
- Just restart ngrok to get new address
- Consider paid tier ($8/month) for stable addresses

---

## Recommended Setup

**For quick testing / playing with friends:**
→ **Use ngrok** (easiest, no router config)

**For regular play sessions:**
→ **Use Radmin VPN** (free, unlimited, stable IPs)

**For permanent hosting:**
→ **Port forwarding** (most reliable, no third-party software)

---

## Port Information

- **Default Port:** 7777 (TCP)
- **Protocol:** TCP Direct Connection
- **Max Players:** 4

---

Need help? The game will show status messages at the bottom of the screen when hosting/joining.

Happy plundering! ⚓🏴‍☠️
