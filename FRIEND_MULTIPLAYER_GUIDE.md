# Plunk & Plunder - Multiplayer Testing Guide for Friends

Welcome! Your friend is testing their game and needs your help. This guide will help you connect and play together.

---

## What You Need

- **The game files** (ZIP file your friend sent you)
- **Your friend's IP address** (they'll give you this)
- **15 minutes** to set up and test

---

## Step 1: Install the Game

1. **Find the ZIP file** your friend sent you (probably in Downloads)
2. **Right-click** the ZIP file → **Extract All**
3. **Open the extracted folder**
4. You should see **PlunkAndPlunder.exe**
5. **Double-click** PlunkAndPlunder.exe to run

✅ The game should open and show a main menu

---

## Step 2: Get Your Friend's IP Address

Your friend needs to tell you their IP address. It will look like one of these:

### Option A: Same WiFi Network (Easy!)
```
Example: 192.168.1.100
```
Ask your friend to:
- Press **Windows Key + R**
- Type `cmd` and press Enter
- Type `ipconfig` and press Enter
- Find **IPv4 Address** (looks like 192.168.x.x)

### Option B: Internet Connection (Requires Port Forwarding)
```
Example: 203.0.113.42
```
Ask your friend to:
- Google "what is my ip"
- Share that public IP address
- Make sure they've set up port forwarding (they should know if they did this)

### Option C: Hamachi (Easiest for Internet!)
```
Example: 25.47.123.45
```
If your friend set up Hamachi:
- Download **LogMeIn Hamachi** (free): https://www.vpn.net/
- Install and create a free account
- Click **"Join an existing network"**
- Enter the network name and password your friend gives you
- Look for your friend's name in Hamachi - their IP is shown next to it

---

## Step 3: Connect to Your Friend's Game

### Your Friend Goes First:
**Your friend must do this BEFORE you can join:**
1. Run the game
2. Click **"Host Direct Connection"**
3. Wait in the lobby

### Then You Join:
1. **Run the game** (PlunkAndPlunder.exe)
2. **Look at the main menu** - you'll see a text box
3. **Type your friend's IP address** with `:7777` at the end

**Examples:**
- Same WiFi: `192.168.1.100:7777`
- Internet: `203.0.113.42:7777`
- Hamachi: `25.47.123.45:7777`

4. **Click "Join Direct Connection"**
5. **Wait a few seconds**

✅ You should see the lobby screen with your friend's name!

---

## Step 4: Start Playing

1. **Wait** for everyone to join
2. **Click the "Ready" button** (if there is one)
3. **Wait** for your friend (the host) to start the game
4. **Play!**

---

## Troubleshooting - "Cannot Connect" Error

### 1. Check the IP Address
- Did you type it **exactly** as your friend gave it?
- Did you include **:7777** at the end?
- No extra spaces before or after?

**Try retyping it carefully**

### 2. Is Your Friend Actually Hosting?
- Ask your friend: "Did you click 'Host Direct Connection'?"
- Ask: "Are you in the lobby screen?"
- Make sure they're hosting **before** you try to join

### 3. Test the Connection
- Open **Command Prompt** (Windows Key + R, type `cmd`, Enter)
- Type: `ping [YOUR_FRIEND'S_IP]` (without the :7777)
- Press Enter

**If you see "Reply from..."** ✅ Network is working
**If you see "Request timed out"** ❌ Network problem

### 4. Firewall Issues (for your friend to fix)
If ping works but you still can't connect, your friend might need to:
- Allow port 7777 through Windows Firewall
- Set up port forwarding on their router (if playing over internet)
- **OR use Hamachi instead** (easier!)

### 5. Try Hamachi Instead
If nothing works, Hamachi is the easiest solution:
- No port forwarding needed
- No firewall issues
- Just install, join network, and connect
- See "Option C: Hamachi" above

---

## During the Game

### If You Get Disconnected:
- Your friend (host) needs to keep their computer on
- If host disconnects, everyone loses connection
- You can try rejoining by clicking "Join Direct Connection" again

### If the Game Freezes:
- Check the console/debug logs
- Let your friend know - they're testing the game!
- Your feedback helps them fix bugs

---

## Quick Reference Card

```
╔════════════════════════════════════════╗
║   PLUNK & PLUNDER - JOIN GAME          ║
╠════════════════════════════════════════╣
║                                        ║
║  1. Run PlunkAndPlunder.exe            ║
║                                        ║
║  2. Type IP in text box:               ║
║     [FRIEND'S IP]:7777                 ║
║                                        ║
║  3. Click "Join Direct Connection"     ║
║                                        ║
║  4. Wait in lobby!                     ║
║                                        ║
╚════════════════════════════════════════╝
```

---

## Important Notes

⚠️ **Host Must Stay Connected**
- Your friend is the "host" - if they quit, everyone disconnects
- Ask them to keep their game open until everyone is done playing

⚠️ **This is a Test Build**
- Expect bugs! That's why you're testing
- Give your friend feedback on any issues
- Screenshots of errors are super helpful

⚠️ **Your Friend Needs Your Feedback**
- Does the connection work?
- Is the game smooth or laggy?
- Any desyncs (your game shows different things than theirs)?
- Any crashes or freezes?

---

## Three Connection Methods Summary

### 🏠 Same WiFi (Easiest if you're together)
**When**: Everyone is on the same network
**IP Type**: 192.168.x.x (local IP)
**Setup**: None needed
**Works**: ✅ Always

### 🌐 Internet (Port Forwarding Required)
**When**: Friends are at different locations
**IP Type**: Public IP (from "what is my ip")
**Setup**: Host needs port forwarding + firewall rules
**Works**: ✅ If configured correctly

### 🔧 Hamachi (Easiest for Internet)
**When**: Friends are at different locations
**IP Type**: 25.x.x.x (Hamachi virtual IP)
**Setup**: Install Hamachi, join network
**Works**: ✅ Always (no port forwarding needed!)

---

## Still Having Issues?

Contact your friend and tell them:
1. **What you tried** (which IP, which method)
2. **What error you saw** (screenshot if possible)
3. **Ping test results** (did it work or timeout?)

They can check the `MULTIPLAYER_TESTING_GUIDE.md` file for advanced troubleshooting.

---

## System Requirements

- **OS**: Windows 10 or newer
- **Network**: WiFi or Ethernet connection
- **Firewall**: May need to allow the game (Windows will ask)
- **Antivirus**: May need to allow the game (if it blocks it)

---

## Having Fun?

Your friend is working hard on this game! Let them know:
- What you liked
- What was confusing
- What could be better
- Any bugs you found

**Thanks for helping test!** 🎮

---

## For More Help

- **Detailed technical guide**: MULTIPLAYER_TESTING_GUIDE.md (in your friend's repo)
- **Quick start**: QUICK_MULTIPLAYER_START.md
- **Network issues**: Check Windows Firewall and router settings

---

**Version**: Direct Connection TCP (Port 7777)
**Last Updated**: 2026-02-01
