# HUD Redesign Testing Checklist

## Pre-Test Setup
- [ ] Open Unity project
- [ ] Let Unity compile scripts (check Console for errors)
- [ ] If compilation errors, fix missing meta files or namespace issues
- [ ] Start a new offline game (4 players)

## Visual Layout Tests

### Left Panel Position (CRITICAL)
- [ ] **Left panel visible at BOTTOM-LEFT corner**
  - Should be 10px from left edge
  - Should be 10px from bottom edge
  - Should have dark background with gold border
  - Should extend vertically almost to top bar

### Right Panel Position (Event Log)
- [ ] **Event log visible at BOTTOM-RIGHT corner**
  - Should be 10px from right edge
  - Should be 10px from bottom edge
  - Should have "EVENT LOG" header in gold
  - Should show events as they occur

### Top Bar
- [ ] **Top bar at very top of screen**
  - Full width
  - 100px tall
  - Shows: Turn number, Phase, Gold, Orders count
  - Submit Orders button on right
  - Auto-Resolve button next to it

### No Overlaps
- [ ] Left panel doesn't overlap game board
- [ ] Right panel doesn't overlap game board
- [ ] Top bar doesn't cover anything
- [ ] No UI elements hidden or cut off

## Player Stats Section (Top of Left Panel)

### Initial State
- [ ] "PLAYER STATS" header visible in gold
- [ ] All 4 players listed
- [ ] Each player shows:
  - [ ] Name (colored per player)
  - [ ] Gold amount with 💰 emoji
  - [ ] Ship count with ⛵ emoji
  - [ ] Shipyard count with 🏭 emoji

### Updates
- [ ] Gold updates when ships built
- [ ] Ship count updates when ships created/destroyed
- [ ] Shipyard count updates when deployed
- [ ] All players update, not just human player

## Unit Details Section (Middle of Left Panel)

### No Selection
- [ ] Shows "No selection" message

### Ship Selected
- [ ] Click a ship on the map
- [ ] Left panel updates immediately
- [ ] Shows:
  - [ ] Ship name (e.g., "SHIP: Theodore")
  - [ ] Owner name
  - [ ] Position coordinates
  - [ ] HP (e.g., "HP: 10/10")
  - [ ] Movement (e.g., "Movement: 3/3")
  - [ ] Sails and Cannons (e.g., "Sails: 0 | Cannons: 0")

### Shipyard Selected
- [ ] Click a shipyard on the map
- [ ] Left panel updates immediately
- [ ] Shows:
  - [ ] "STRUCTURE"
  - [ ] Type: SHIPYARD
  - [ ] Owner name
  - [ ] Position coordinates
  - [ ] Build queue count

## Build Queue Section

### Hidden by Default
- [ ] Build queue section NOT visible when ship selected
- [ ] Build queue section NOT visible when nothing selected

### Visible When Shipyard Selected
- [ ] Select a shipyard
- [ ] "BUILD QUEUE" section appears
- [ ] Empty queue shows "No ships in queue"

### With Ships Queued
- [ ] Click "Build Ship" button
- [ ] Ship immediately appears in queue
- [ ] Shows: "1. Ship"
- [ ] Shows: "Turns: 2/2" (or current turns)
- [ ] Build additional ships (up to 5)
- [ ] Queue shows all ships
- [ ] Counter updates (e.g., "Build Queue: 3/5")

## Action Buttons Section (Bottom of Left Panel)

### Button Labels
- [ ] "Deploy Shipyard (100g)"
- [ ] "Build Ship (50g)"
- [ ] "Upgrade Sails (60g)"
- [ ] "Upgrade Cannons (80g)"
- [ ] "Upgrade Max Life (100g)"

### Deploy Shipyard Button
**Enabled When:**
- [ ] Ship selected
- [ ] Ship owned by human player (Player 1)
- [ ] Ship on harbor tile (look for sandy beach tile)
- [ ] Player has 100+ gold
- [ ] No structure at that location

**Disabled When:**
- [ ] Ship not on harbor
- [ ] Insufficient gold (<100g)
- [ ] Already a structure there
- [ ] Not planning phase

**Click Test:**
- [ ] Move ship to harbor
- [ ] Button should enable
- [ ] Click "Deploy Shipyard"
- [ ] *(Order queued - check console for log)*

### Build Ship Button
**Enabled When:**
- [ ] Shipyard selected
- [ ] Shipyard owned by human player
- [ ] Queue not full (<5 ships)
- [ ] Player has 50+ gold
- [ ] Planning phase active

**Disabled When:**
- [ ] No shipyard selected
- [ ] Queue full (5/5)
- [ ] Insufficient gold (<50g)
- [ ] Enemy shipyard selected

**Click Test:**
- [ ] Select owned shipyard
- [ ] Click "Build Ship"
- [ ] Ship added to queue immediately
- [ ] Gold deducted (check top bar)
- [ ] Queue count updates
- [ ] Button disabled if queue full

### Upgrade Buttons
**Enabled When:**
- [ ] Ship selected and owned by human player
- [ ] Ship at owned shipyard (docked)
- [ ] Player has sufficient gold (60g/80g/100g)
- [ ] Upgrade not at max level
- [ ] Planning phase active

**Disabled When:**
- [ ] Ship not at shipyard
- [ ] Insufficient gold
- [ ] Already at max upgrades
- [ ] Enemy ship selected

**Click Tests:**
- [ ] Dock ship at shipyard (move ship to shipyard tile)
- [ ] Select the ship
- [ ] Upgrade buttons should enable (if gold sufficient)
- [ ] Click "Upgrade Sails"
- [ ] *(Order queued - check console for log)*

## Event Log Section

### Initial State
- [ ] Shows "Awaiting events..." or initial events
- [ ] Gold "EVENT LOG" header at top

### During Turn Resolution
- [ ] Submit orders
- [ ] Watch for events appearing
- [ ] Events show in format: "[Turn X] Event message"
- [ ] Log scrolls as more events added
- [ ] Can scroll up to see older events
- [ ] Shows last 50 events (not infinite)

### Event Examples
- [ ] Ship movements logged
- [ ] Combat encounters logged
- [ ] Ship builds completed logged
- [ ] Upgrades logged
- [ ] Gold awarded logged

## Top Bar Tests

### Turn and Phase
- [ ] Turn starts at "Turn: 1"
- [ ] Phase starts at "Phase: Planning"
- [ ] Phase changes to Resolution when orders submitted
- [ ] Phase changes back to Planning for next turn
- [ ] Turn increments each cycle

### Gold and Orders Counter
- [ ] Shows human player's gold (Player 1)
- [ ] Orders counter at 0 initially
- [ ] Orders counter increments when move orders set
- [ ] Format: "Gold: 100 | Orders: 0"
- [ ] Updates immediately when actions taken

### Submit Orders Button
- [ ] Disabled when no orders queued
- [ ] Enabled when at least 1 order queued
- [ ] Button lights up/pulses when all units have orders
- [ ] Click submits orders
- [ ] Phase changes to Resolution
- [ ] Game progresses to next turn

## Responsive Tests

### Window Resize
- [ ] Resize game window smaller
- [ ] Left panel adjusts height
- [ ] Right panel adjusts height
- [ ] Top bar adjusts width
- [ ] No elements cut off or hidden
- [ ] Everything still readable

### Different Resolutions
- [ ] Test at 1920x1080
- [ ] Test at 1280x720
- [ ] Test at windowed mode
- [ ] Panels scale appropriately

## Integration Tests

### Full Game Flow
1. [ ] Start new game
2. [ ] Select a ship
3. [ ] Right-click to move (path visualization appears)
4. [ ] Select another ship
5. [ ] Set another move order
6. [ ] Click "Submit Orders"
7. [ ] Watch turn resolution
8. [ ] Check event log fills with events
9. [ ] Check player stats update (gold awarded)
10. [ ] Planning phase starts again
11. [ ] Select ship at shipyard
12. [ ] Click upgrade button
13. [ ] Submit orders
14. [ ] Watch upgrade occur
15. [ ] Stats update correctly

### Build Flow
1. [ ] Select owned shipyard
2. [ ] Click "Build Ship" (costs 50g)
3. [ ] Check gold deducted immediately
4. [ ] Check ship in build queue
5. [ ] Submit orders (or wait turns)
6. [ ] After 2 turns, ship should spawn
7. [ ] Check ship count increments in player stats

### Deploy Flow
1. [ ] Select ship
2. [ ] Move ship to harbor tile (sandy beach)
3. [ ] "Deploy Shipyard" button should enable
4. [ ] Click button
5. [ ] Submit orders
6. [ ] Shipyard should appear on harbor
7. [ ] Ship should be consumed
8. [ ] Shipyard count increments in player stats

## Error Scenarios

### Insufficient Gold
- [ ] Spend gold until < 50g
- [ ] Try to build ship → button disabled ✓
- [ ] Verify error handling graceful

### Full Queue
- [ ] Queue 5 ships at shipyard
- [ ] "Build Ship" button disabled ✓
- [ ] Message shows queue full

### Max Upgrades
- [ ] Upgrade sails 3 times (max)
- [ ] "Upgrade Sails" button disabled ✓
- [ ] Shows "Max sails reached" or button stays disabled

## Console Checks

### No Errors
- [ ] No red errors in Unity Console
- [ ] No null reference exceptions
- [ ] No missing component warnings

### Expected Logs
- [ ] "[LeftPanelHUD] Initialized at bottom-left"
- [ ] "[LeftPanelHUD] Deploy Shipyard button clicked" (when clicked)
- [ ] "[LeftPanelHUD] Ship added to build queue"
- [ ] "[PlayerStatsHUD]" logs should NOT appear (replaced)

## Visual Polish

### Colors
- [ ] Gold color (#FFC832) used for headers
- [ ] Dark backgrounds semi-transparent
- [ ] Text white and readable
- [ ] Emojis display correctly (💰⛵🏭)

### Borders
- [ ] Panels have gold borders (2px)
- [ ] Borders visible and crisp
- [ ] Sections separated by lines

### Layout
- [ ] No awkward spacing
- [ ] Sections flow logically top to bottom
- [ ] Buttons aligned and sized consistently
- [ ] Text not cut off

## Known Issues to Document

- [ ] Note any visual glitches
- [ ] Note any functional bugs
- [ ] Note any performance issues
- [ ] Note any unexpected behavior

## Final Verification

- [ ] Left panel at (10px, 10px) from BOTTOM-LEFT ✓✓✓
- [ ] Right panel at (10px, 10px) from BOTTOM-RIGHT ✓✓✓
- [ ] All systems functional
- [ ] Game playable end-to-end
- [ ] No major bugs blocking gameplay

## Success Criteria Met?

✅ Left panel positioned correctly at bottom-left
✅ Event log positioned correctly at bottom-right
✅ Consistent styling throughout
✅ All functionality preserved or improved
✅ No overlapping UI elements
✅ Clean, maintainable code

**Test Date:** _________________
**Tested By:** _________________
**Unity Version:** _________________
**Result:** ☐ Pass ☐ Fail ☐ Needs Work

**Notes:**
