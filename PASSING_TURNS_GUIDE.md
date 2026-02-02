# 🎮 Passing Turns & Build Queues - Quick Guide

## ✅ **Changes Made**

I've modified the game to allow you to **pass turns without moving ships**!

### What Changed:
1. **Pass Turn button is now always enabled** during Planning phase
2. **You can click it with no orders** → ships stay still, turn advances
3. **Build queues progress** every turn regardless of ship movement

---

## 🚢 **How to Pass Turn Without Moving Ships**

### **Option 1: Just Click Pass Turn (No Orders)**
1. During Planning phase
2. **Don't give any movement orders** to your ships
3. **Click "Pass Turn"** (top-right corner)
4. ✅ Turn advances, ships stay still, build queues progress

### **Option 2: Give Orders to Some Ships**
1. Move some ships (right-click to set destination)
2. Leave other ships without orders
3. **Click "Pass Turn"**
4. ✅ Ships with orders move, ships without orders stay still

---

## 🏗️ **How Build Queues Work**

Build queues **progress at the START of each turn**, not when you submit orders.

### **Turn Flow:**
```
Turn N:
1. Planning Phase starts
   ├─ Income awarded (100g per shipyard)
   └─ Build queues tick (ships under construction progress by 1 turn)

2. You give orders (or don't)

3. You click "Pass Turn"
   └─ Movement/combat happens

Turn N+1 begins:
   ├─ Income awarded again
   └─ Build queues tick again (another turn of progress)
```

### **Example: Building a Ship Without Moving**

**Turn 1:**
- Select your shipyard (click it)
- Click "Build Ship" button
- **Don't move any ships**
- Click "Pass Turn"
- ✅ Ship starts building (needs 3 turns)

**Turn 2:**
- Game starts: "Ship under construction: 2 turns remaining"
- **Don't move any ships**
- Click "Pass Turn"
- ✅ Ship construction continues

**Turn 3:**
- Game starts: "Ship under construction: 1 turn remaining"
- **Don't move any ships**
- Click "Pass Turn"
- ✅ Ship completes and spawns!

**Turn 4:**
- New ship appears in the water near your shipyard
- You can now move it

---

## 🎯 **Why This Is Useful**

### **1. Pure Economic Turns**
- Focus only on building/upgrading
- Don't waste time micromanaging ship positions
- Let build queues progress while you plan

### **2. Defensive Stance**
- Keep ships in defensive positions
- Build up your fleet
- Wait for enemy to come to you

### **3. Multi-Turn Construction**
- Queue multiple ships
- Pass several turns
- Ships complete without needing to move anything

### **4. Gold Accumulation**
- Pass turns to earn income
- Save up for expensive upgrades
- Build economic advantage

---

## 📋 **Turn Actions Reference**

| Action | How to Do It | Effect on Turn |
|--------|-------------|----------------|
| **Pass (No Orders)** | Click "Pass Turn" with no orders | Ships stay still, build queues progress, income awarded |
| **Pass (Auto Orders)** | Combat ships have default orders | Ships in combat continue attacking, others stay still |
| **Move Ships** | Right-click destination → Pass Turn | Selected ships move, others stay still |
| **Build Ships** | Click shipyard → Build Ship → Pass Turn | Queues ship (3 turns to complete), ships don't move |
| **Upgrade Ships** | Select ship at shipyard → Upgrade → Pass Turn | Ship upgrades immediately, doesn't move |
| **Deploy Shipyard** | Select ship at harbor → Deploy → Pass Turn | Ship converts to shipyard, other ships stay still |

---

## 🔄 **Build Queue Details**

### **Ship Construction:**
- **Cost:** 50 gold
- **Time:** 3 turns
- **Progress:** Automatic (1 turn per game turn)
- **Completion:** Ship spawns in water adjacent to shipyard

### **Construction Progress:**
```
Turn 1: Queue ship → "3 turns remaining"
Turn 2: Auto-tick → "2 turns remaining"
Turn 3: Auto-tick → "1 turn remaining"
Turn 4: Ship completes and spawns!
```

### **Multiple Ships in Queue:**
- Shipyards can queue multiple ships
- They build **one at a time** (sequential)
- Queue order: first in, first out

**Example:**
```
Turn 1: Queue Ship A → "Ship A: 3 turns"
Turn 2: Queue Ship B → "Ship A: 2 turns, Ship B: waiting"
Turn 4: Ship A completes → "Ship B: 3 turns"
Turn 7: Ship B completes
```

---

## 🧪 **Testing Scenarios**

### **Scenario 1: Pure Build Mode**
1. Start game
2. Click shipyard → Build Ship
3. Click "Pass Turn" (don't move anything)
4. Repeat 2 more times
5. ✅ Result: Ship completes, all units stayed still

### **Scenario 2: Mixed Actions**
1. Give movement order to Ship A (right-click destination)
2. Leave Ship B without orders
3. Click shipyard → Build Ship
4. Click "Pass Turn"
5. ✅ Result: Ship A moves, Ship B stays still, construction progresses

### **Scenario 3: Fast-Forward Economics**
1. Don't give any orders
2. Click "Pass Turn" 10 times quickly
3. ✅ Result: 10 turns pass, income earned 10x, build queues complete

---

## 💡 **Pro Tips**

1. **Combat Ships Auto-Continue:**
   - Ships in combat have default attack orders
   - They'll keep attacking unless you change their path
   - To retreat: right-click away from enemy

2. **Income Every Turn:**
   - You earn 100g per shipyard at turn start
   - Passing turns = passive income
   - Use this to save for upgrades

3. **Construction Starts Immediately:**
   - When you queue a ship, it starts that turn
   - Next turn it will show "2 turns remaining"
   - No delay

4. **Shipyard Selection:**
   - Click shipyard to see build queue
   - Shows all ships being built
   - Shows turn estimates for completion

5. **Auto-Resolve for Fast Testing:**
   - Press `R` key (if debug key is enabled)
   - Or click "Auto Resolve" button
   - Instantly passes turn with empty orders
   - Useful for testing build queues

---

## 🐛 **Troubleshooting**

### "Pass Turn button is greyed out"
- **Old version:** You need to rebuild the game with the changes I made
- **Fix:** Rebuild in Unity, then test

### "Ships aren't staying still"
- Check if ships have combat paths (they auto-attack)
- Right-click away from enemy to cancel combat
- Then pass turn

### "Build queue not progressing"
- Build queues tick at **turn start**, not when you submit
- Pass 1 turn → check shipyard → should show progress
- If not progressing: check Unity console for errors

### "Can't build ships"
- Check gold: Need 50g per ship
- Click shipyard first, then "Build Ship" button
- If button missing: Make sure shipyard is selected

---

## 🎓 **Understanding Turn Phases**

```
PLANNING PHASE (Your Turn)
├─ Turn starts (income + build queue tick happens here!)
├─ You can:
│  ├─ Give movement orders (or not)
│  ├─ Queue ships to build
│  ├─ Upgrade units
│  └─ Deploy shipyards
└─ Click "Pass Turn" (submits your orders)

RESOLVING PHASE (AI + Resolution)
├─ AI submits their orders
├─ All orders resolve simultaneously
├─ Combat happens
└─ Events logged

ANIMATING PHASE (Visual)
├─ Ships move visually
├─ Combat animations
└─ Events displayed

Back to PLANNING PHASE (New Turn)
└─ Loop continues
```

---

## 📊 **Example Turn Sequence**

```
🎮 TURN 1
Income: +100g (1 shipyard)
Action: Click shipyard → Build Ship (-50g)
Pass Turn: Click "Pass Turn" (no movement orders)
Result: Ship construction begins, gold = 50g

🎮 TURN 2
Income: +100g (150g total)
Action: None (just letting build queue progress)
Pass Turn: Click "Pass Turn"
Result: Ship 2/3 turns remaining, gold = 150g

🎮 TURN 3
Income: +100g (250g total)
Action: Click shipyard → Build Ship again (-50g)
Pass Turn: Click "Pass Turn"
Result: Ship A finishes (1/3 remaining), Ship B queued (3/3), gold = 200g

🎮 TURN 4
Income: +100g (300g total)
Action: Move new Ship A to explore
Pass Turn: Give Ship A orders, then "Pass Turn"
Result: Ship A moves, Ship B 2/3 remaining, gold = 300g
```

---

## ✅ **Summary**

**To pass turn without moving ships:**
- Just click "Pass Turn" with no orders given
- Ships stay still
- Build queues progress
- Income earned
- Turn advances

**To progress build queues:**
- They tick automatically at turn start
- No player action required
- Just pass turns and wait

**To do both economic + movement:**
- Give orders to ships you want to move
- Leave others without orders
- Queue builds/upgrades
- Click "Pass Turn"

---

**Easy!** 🎮
