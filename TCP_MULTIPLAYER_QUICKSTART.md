# 🎮 Plunk & Plunder - TCP Multiplayer Quick Start

## FOR YOU (The Host)

### Step 1: Build the Game

1. Open Unity
2. Go to **File → Build Settings**
3. Select **Windows** platform
4. Click **Build** and choose a folder (e.g., `Builds/PlunkAndPlunder_v1`)
5. Wait for build to complete

### Step 2: Get Your IP Address

**For LAN (same WiFi):**
1. Press `Windows + R`
2. Type `cmd` and press Enter
3. Type `ipconfig` and press Enter
4. Find **IPv4 Address** (looks like `192.168.1.100`)
5. Write it down!

**For Internet (different locations):**
1. Google "what is my ip" - write down your public IP
2. Configure port forwarding:
   - Log into your router (usually 192.168.1.1)
   - Find "Port Forwarding" settings
   - Forward **port 7777 TCP** to your PC's local IP
   - Save and restart router

### Step 3: Share the Game with Friends

1. **Zip the entire build folder** you created
2. Upload to Google Drive / Dropbox / WeTransfer
3. Send the link to your friends
4. **Tell them your IP address**:
   - LAN: `192.168.1.100:7777`
   - Internet: `YOUR_PUBLIC_IP:7777`

### Step 4: Host the Game

1. **Run** PlunkAndPlunder.exe
2. **Click** "Host Direct Connection"
3. **Wait** in the lobby for friends to join
4. You'll see a message: "Hosting on port 7777..."
5. **When everyone's in**, click "Start Game" (when implemented)

✅ **That's it! You're hosting!**

---

## FOR YOUR FRIENDS (Clients)

### Step 1: Install the Game

1. Download the ZIP file from your friend
2. **Extract** the ZIP file (Right-click → Extract All)
3. Open the extracted folder
4. Find **PlunkAndPlunder.exe**

### Step 2: Get the Host's IP

Ask your friend (the host) for their IP address. It will look like:
- **LAN (same WiFi):** `192.168.1.100:7777`
- **Internet:** `203.0.113.42:7777` (example)

### Step 3: Join the Game

1. **Run** PlunkAndPlunder.exe
2. **Type the host's IP** in the text box (include `:7777` at the end)
3. **Click** "Join Direct Connection"
4. **Wait** for connection...
5. You should see the lobby!

✅ **You're in!**

---

## 🔧 Troubleshooting

### "Cannot Connect" Error

**Quick Fixes:**
1. ✅ Is the IP address correct? (no typos, includes `:7777`)
2. ✅ Is the host actually running and in the lobby?
3. ✅ Are you on the same WiFi network? (for LAN play)
4. ✅ Did host set up port forwarding? (for internet play)

**Test Connection:**
```cmd
ping HOST_IP_WITHOUT_PORT
```
If you see "Reply from..." it's working!

### Firewall Blocking

**Host needs to:**
1. Open Windows Defender Firewall
2. Click "Allow an app through firewall"
3. Click "Change settings" → "Allow another app"
4. Add PlunkAndPlunder.exe
5. Check all boxes (Private, Public)

---

## 🌐 Easy Alternative: Use Hamachi

**No port forwarding needed!**

1. **Everyone** downloads Hamachi (free): https://vpn.net/
2. **Host** creates a network (name + password)
3. **Friends** join that network
4. Use Hamachi virtual IP (looks like `25.x.x.x:7777`)
5. Connect normally!

---

## 📋 Connection Methods Comparison

| Method | Difficulty | Best For | Setup Time |
|--------|-----------|----------|------------|
| **LAN (Same WiFi)** | ⭐ Easy | Local testing | 2 minutes |
| **Port Forwarding** | ⭐⭐⭐ Hard | Internet play | 15 minutes |
| **Hamachi VPN** | ⭐⭐ Medium | Internet play | 5 minutes |

**Recommendation:** Start with LAN testing, then use Hamachi for internet play.

---

## 🎯 Quick Reference Card

```
╔═══════════════════════════════════════════╗
║         HOSTING (You)                     ║
╠═══════════════════════════════════════════╣
║ 1. Build game in Unity                    ║
║ 2. Get IP: cmd → ipconfig                 ║
║ 3. Share build folder + IP with friends   ║
║ 4. Run game → "Host Direct Connection"    ║
║ 5. Wait for friends to join               ║
╚═══════════════════════════════════════════╝

╔═══════════════════════════════════════════╗
║         JOINING (Friends)                 ║
╠═══════════════════════════════════════════╣
║ 1. Extract ZIP from friend                ║
║ 2. Run PlunkAndPlunder.exe                ║
║ 3. Type: [FRIEND_IP]:7777                 ║
║ 4. Click "Join Direct Connection"         ║
║ 5. Wait for lobby screen                  ║
╚═══════════════════════════════════════════╝
```

---

## 📝 Build Checklist

Before sharing with friends, verify your build includes:
- [ ] PlunkAndPlunder.exe (main file)
- [ ] UnityPlayer.dll
- [ ] UnityCrashHandler64.exe
- [ ] PlunkAndPlunder_Data/ folder (complete)
- [ ] MonoBleedingEdge/ folder (complete)

**Zip size should be ~50-200 MB**

---

## 🆘 Still Having Issues?

1. **Check Unity Console** for errors (on both host and client)
2. **Enable debug logs** in TCPTransport.cs (line 54)
3. **Test on LAN first** before trying internet
4. **Use Hamachi** to avoid networking headaches
5. **Check router logs** for blocked connections

---

## ⚠️ Important Notes

- **Host must stay online**: If you (host) quit, everyone disconnects
- **This is for testing**: Production will use Steam (no port forwarding needed)
- **Max 4 players**: 1 host + 3 clients
- **No reconnection**: If someone disconnects, they must rejoin from lobby

---

## 🚀 Next Steps After Testing

Once TCP multiplayer works:
1. Collect feedback from friends
2. Note any desyncs or crashes
3. Test different scenarios (combat, building, etc.)
4. Document any bugs
5. Prepare for Steam integration

---

**Good luck with your multiplayer testing!** 🎮

If you have issues, reference `MULTIPLAYER_TESTING_GUIDE.md` for detailed troubleshooting.
