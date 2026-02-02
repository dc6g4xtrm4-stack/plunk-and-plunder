# Quick Multiplayer Testing - TL;DR

## For HOST (You)

1. **Build the game**: Unity → File → Build Settings → Build
2. **Get your IP**: Open Command Prompt → type `ipconfig` → find IPv4 Address
3. **Run game** → Click **"Host Direct Connection"**
4. **Share your IP** with friends (e.g., `192.168.1.100`)

### Port Forwarding (if friends are outside your network)
- Forward port **7777** on your router
- Allow port 7777 in Windows Firewall
- Share your **public IP** (Google "what is my ip")

## For FRIENDS (Clients)

1. **Get the game build** (ZIP file from host)
2. **Extract and run** PlunkAndPlunder.exe
3. **Type host's IP** in the text field (e.g., `192.168.1.100:7777`)
4. **Click "Join Direct Connection"**

## Troubleshooting

**Can't connect?**
- Make sure host clicked "Host Direct Connection" first
- Verify IP address is correct
- Check if on same network (LAN) or need port forwarding
- Try Hamachi/ZeroTier for easy virtual LAN

**Connection lost?**
- Host computer might have gone to sleep
- Firewall might be blocking
- Network interruption

## Alternative: Use Hamachi (Easiest)

1. All players install LogMeIn Hamachi (free)
2. Host creates a network
3. Friends join network
4. Use Hamachi virtual IP (25.x.x.x)
5. No port forwarding needed!

---

See **MULTIPLAYER_TESTING_GUIDE.md** for detailed setup and troubleshooting.
