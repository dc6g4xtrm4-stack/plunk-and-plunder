# 🔄 Releasing Updates - Quick Guide

## When You Make Code Changes

Every time you update the code, you can create a new release:

### **Process:**

1. ✅ **Test your changes** in Unity Play Mode
2. ✅ **Build** → File → Build Settings → Build (overwrite or new folder)
3. ✅ **Package** → Run `create_client_build.bat`
4. ✅ **Upload** → New ZIP to cloud storage
5. ✅ **Notify** → Tell friends to download new version

---

## 📦 **Two Approaches**

### **Approach A: Simple Overwrite (Recommended for Testing)**

**How it works:**
- Build to same folder every time: `Builds/PlunkAndPlunder/`
- Run `create_client_build.bat` → creates timestamped ZIP
- Old ZIPs stay in `Builds/` folder for history

**Pros:**
- Simple, quick
- Automatic timestamping
- No folder management

**Cons:**
- Can't easily rollback to old version

**Best for:** Active development, frequent updates

---

### **Approach B: Version Folders (Organized)**

**How it works:**
- Build to version-specific folders:
  ```
  Builds/PlunkAndPlunder_v1/
  Builds/PlunkAndPlunder_v2/
  Builds/PlunkAndPlunder_v3/
  ```
- Run `create_client_build_versioned.bat` → prompts for version
- Keeps old builds available

**Pros:**
- Easy to rollback
- Clear version tracking
- Can compare builds

**Cons:**
- Takes more disk space
- Need to remember version numbers

**Best for:** Milestone releases, major changes

---

## 📢 **Notifying Friends of Updates**

### **Message Template:**

```
🎮 NEW VERSION AVAILABLE!

I've updated Plunk & Plunder with some fixes/improvements:

WHAT'S NEW:
- [List your changes here]
- Fixed multiplayer desync issue
- Improved ship combat
- Updated UI layout

DOWNLOAD:
[Your Google Drive / Dropbox link]

INSTALLATION:
1. Delete your old PlunkAndPlunder folder
2. Download the new ZIP
3. Extract and run PlunkAndPlunder.exe
4. Same connection process as before!

My IP hasn't changed: [YOUR_IP]:7777

Let me know if you have any issues!
```

---

## 🔢 **Version Numbering (Optional)**

If you want to track versions:

### **Semantic Versioning:**
```
v1.0.0 = Major.Minor.Patch

v1.0.0 → First playable version
v1.0.1 → Bug fixes
v1.1.0 → New feature added
v2.0.0 → Major overhaul
```

### **Simple Dating:**
```
2026-02-01_Alpha
2026-02-05_Alpha
2026-02-15_Beta
```

### **Named Releases:**
```
Alpha_1 → First test
Alpha_2 → Combat fixes
Beta_1 → Feature complete
```

Choose whatever makes sense for you!

---

## 🐛 **Update Workflow Example**

### **Scenario: You Fixed a Bug**

1. **Fix bug in Unity**
   - Test in Play Mode
   - Verify fix works

2. **Rebuild**
   ```
   File → Build Settings → Build
   Choose: Builds/PlunkAndPlunder/ (overwrite)
   ```

3. **Package**
   ```
   Run: create_client_build.bat
   Result: Builds/PlunkAndPlunder_Client_20260201_150000.zip
   ```

4. **Upload**
   - Upload new ZIP to Google Drive
   - Update share link (or create new one)

5. **Notify Friends**
   ```
   "Fixed the combat bug! Download new version:
   [link]

   Just delete the old folder and extract this one."
   ```

6. **Test with Friends**
   - Host a session
   - Verify bug is fixed
   - Collect feedback

---

## ⚠️ **Important Notes**

### **Version Compatibility:**
- ❗ **All players MUST use the same version**
- Mixed versions WILL cause desyncs
- If one person updates, everyone must update

### **Testing Before Release:**
Always test locally first:
1. Build the game
2. Run it on your machine
3. Host and join from same PC (127.0.0.1:7777)
4. Verify basic functionality works
5. **Then** share with friends

### **Keeping Old Builds:**
The `create_client_build.bat` script keeps all ZIPs:
```
Builds/
  ├── PlunkAndPlunder_Client_20260201_120000.zip (old)
  ├── PlunkAndPlunder_Client_20260201_143000.zip (old)
  └── PlunkAndPlunder_Client_20260201_150000.zip (newest)
```
You can always rollback if a new build breaks something!

---

## 🚀 **Quick Reference**

### **For Quick Updates:**
```batch
# In Unity: File → Build Settings → Build (overwrite)
# Then in CMD:
create_client_build.bat
# Upload the new ZIP, notify friends
```

### **For Versioned Releases:**
```batch
# In Unity: File → Build Settings → Build to Builds/PlunkAndPlunder_v2/
# Then in CMD:
create_client_build_versioned.bat
# Enter version: v2
# Upload the new ZIP, notify friends
```

---

## 📋 **Update Checklist**

Before releasing an update:
- [ ] Changes tested in Unity Play Mode
- [ ] Build completes without errors
- [ ] Tested build locally (single-player)
- [ ] Tested multiplayer (host + join from same PC)
- [ ] Created ZIP with build script
- [ ] Uploaded to cloud storage
- [ ] Prepared change notes for friends
- [ ] Notified all players
- [ ] Scheduled testing session

---

## 💡 **Pro Tips**

1. **Changelog:** Keep a text file listing changes
   - Helps friends know what's new
   - Useful for debugging
   - Good practice for Steam release

2. **Backup:** Keep the last working build
   - Don't delete old ZIPs immediately
   - If new build breaks, rollback quickly

3. **Test Incrementally:**
   - Small updates = easier to find bugs
   - Big updates = harder to debug

4. **Document Issues:**
   - Keep notes on what broke
   - Helps track down problems
   - Useful for Steam development

5. **Version in UI:**
   - Consider adding version number to main menu
   - Helps verify everyone's on same build
   - Example: "Plunk & Plunder v1.2.0"

---

## 🎯 **Example Update Cycle**

**Day 1: Initial Release**
- Build v1.0
- Share with 3 friends
- Test session: Find bugs

**Day 2: Bug Fixes**
- Fix critical bugs
- Build v1.1
- Share update: "Fixed combat crash"
- Test session: Verify fixes

**Day 5: New Feature**
- Add new ship type
- Build v2.0
- Share update: "New destroyer ship added!"
- Test session: Try new feature

**Day 10: Polish**
- Balance tweaks
- UI improvements
- Build v2.1
- Share update: "Polish pass"
- Final test session

---

**Remember:** You can update as often as you want! Just rebuild, repackage, and share. 🎮
