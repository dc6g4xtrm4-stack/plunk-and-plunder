# Encounter System Testing Guide

## Quick Test Checklist

This guide provides step-by-step instructions to verify the Encounter System implementation.

---

## Setup

### Prerequisites
1. Unity project opened in Unity Editor
2. All scripts compiled without errors
3. Main scene loaded

### Initial Verification
```bash
# Check all files are present
ls Assets/Scripts/UI/EncounterUI.cs
ls Assets/Scripts/Combat/Encounter.cs
ls Assets/Scripts/Rendering/ContestedTilePulse.cs

# Verify no compilation errors
# Open Unity Editor → Wait for compilation → Check Console
```

---

## Test 1: Headless Simulation (Automated)

This verifies the core encounter logic works without UI.

### Steps:
1. Open Unity Editor
2. Navigate to: `Assets/Scenes/MainScene`
3. Find the `SimulationBootstrap` object in hierarchy
4. Run a 100-turn headless simulation

### Expected Results:
✅ Simulation completes without errors
✅ Console shows encounter detection logs:
   - `[ENCOUNTER_RESOLUTION] X encounters detected`
   - `[ENCOUNTER_RESOLUTION] PASSING encounter at...`
   - `[ENCOUNTER_RESOLUTION] ENTRY encounter at...`
✅ AI makes decisions automatically
✅ No null reference exceptions

### Console Log Sample:
```
[ENCOUNTER_RESOLUTION] 2 encounters detected, resolving...
[ENCOUNTER_RESOLUTION] PASSING encounter at edge (3,4) <-> (4,4) with 2 units
[ENCOUNTER_RESOLUTION] PASSING encounter decisions:
[ENCOUNTER_RESOLUTION]   Unit unit_5 (P0): health=8/10, decision=PROCEED
[ENCOUNTER_RESOLUTION]   Unit unit_7 (P1): health=10/10, decision=ATTACK
[ENCOUNTER_RESOLUTION] Generated 3 encounter resolution events
```

---

## Test 2: PASSING Encounter - Peaceful Swap

### Setup:
1. Start a new offline game (Human vs AI)
2. Move your ship to position (5,5)
3. Wait for AI ship to move to position (6,5)

### Test Steps:
1. Issue move order: Your ship (5,5) → (6,5)
2. AI should plan move: AI ship (6,5) → (5,5)
3. Click "Submit Orders"

### Expected Results:
✅ EncounterUI modal appears
✅ Shows: "PASSING ENCOUNTER"
✅ Shows: "Ships crossing paths - choose action"
✅ Two buttons visible: "PROCEED" (green) and "ATTACK" (red)
✅ Ship info shows HP and location

### Test Action:
1. Click "PROCEED"

### Expected Outcome:
✅ Status text shows "Proceeding" (green)
✅ UI hides after AI decides
✅ Animation shows ships swapping positions
✅ Your ship ends at (6,5)
✅ AI ship ends at (5,5)
✅ No damage to either ship

---

## Test 3: PASSING Encounter - Combat

### Setup:
Same as Test 2

### Test Steps:
Same as Test 2, but this time:

### Test Action:
1. Click "ATTACK"

### Expected Outcome:
✅ Status text shows "Attacking" (red)
✅ UI hides after AI decides
✅ Combat occurs on edge between tiles
✅ Both ships remain in original positions
✅ Ships take damage (visible in unit health)
✅ Combat animation plays

---

## Test 4: ENTRY Encounter - All Yield

### Setup:
1. Start a new offline game
2. Position your ship at (5,5)
3. Position AI ships at (6,5) and (7,5)

### Test Steps:
1. Issue move order: Your ship (5,5) → (8,5)
2. Let AI plan moves to same destination (8,5)
3. Click "Submit Orders"

### Expected Results:
✅ EncounterUI modal appears
✅ Shows: "ENTRY ENCOUNTER"
✅ Shows: "Ships contesting tile - choose action"
✅ Two buttons visible: "YIELD" (green) and "ATTACK" (red)
✅ Tile coordinate shown: (8,5)

### Test Action:
1. Click "YIELD"

### Expected Outcome:
✅ Status text shows "Yielding" (green)
✅ No ships move to (8,5)
✅ All ships remain in original positions
✅ No combat occurs

---

## Test 5: ENTRY Encounter - Single Attacker

### Setup:
Same as Test 4

### Test Steps:
Same as Test 4, but this time:

### Test Action:
1. Click "ATTACK"
2. Ensure AI ships yield (health-based logic)

### Expected Outcome:
✅ Your ship moves to (8,5)
✅ AI ships remain in original positions
✅ No combat occurs
✅ You claim the tile

---

## Test 6: ENTRY Encounter - Contested Tile

### Setup:
Same as Test 4, but ensure multiple ships will attack:
- Your ship has high health (8+/10)
- AI ship also has high health (8+/10)

### Test Steps:
1. Issue move order to same tile
2. Click "Submit Orders"

### Test Action:
1. Click "ATTACK"
2. Wait for AI to decide (should also attack due to high health)

### Expected Outcome:
✅ Status text shows "Attacking" (red)
✅ Pairwise combat occurs between all attackers
✅ All ships remain in original positions
✅ Tile (8,5) shows **RED PULSING BORDER**
✅ Console shows: "Tile (8,5) is now contested"

### Next Turn:
1. Click "Submit Orders" (no new orders needed)

### Expected Continued Outcome:
✅ Red pulse still visible on (8,5)
✅ Another round of combat occurs
✅ Ships continue fighting each turn
✅ When only 1 ship remains, it claims the tile
✅ Red pulse disappears
✅ When 0 ships remain, tile becomes free

---

## Test 7: Contested Tile Resolution

### Continuation of Test 6:
After contested tile is created, wait for resolution.

### Expected Resolution Scenarios:

**Scenario A: One survivor**
✅ Last surviving ship moves to contested tile
✅ Red pulse disappears
✅ Console: "Tile (8,5) claimed by unit unit_X"

**Scenario B: All destroyed**
✅ Tile becomes free
✅ Red pulse disappears
✅ Console: "Tile (8,5) no longer contested"

---

## Test 8: Multiple Simultaneous Encounters

### Setup:
1. Create a complex scenario with 4+ ships
2. Plan moves that trigger multiple encounters

### Expected Results:
✅ EncounterUI shows all local player ships
✅ Each ship has independent decision buttons
✅ Decisions can be made in any order
✅ UI remains visible until all decisions made
✅ All encounters resolve correctly

---

## Test 9: AI Decision Verification

### Setup:
1. Watch AI ship in headless simulation logs
2. Check decision logic

### Expected AI Behavior:
✅ Health < 50% → PROCEED (PASSING) or YIELD (ENTRY)
✅ Health >= 50% → ATTACK
✅ Decisions are deterministic (same health = same decision)
✅ Console logs show: "AI unit unit_X decided Y (health: A/B)"

---

## Test 10: Regression - Old Collision System

Verify backward compatibility.

### Steps:
1. Load an old save file (if available)
2. OR disable encounter detection temporarily
3. Verify old collision system still works

### Expected Results:
✅ Old CollisionYieldUI appears if no encounters detected
✅ Old collision resolution still functions
✅ No crashes or errors

---

## Performance Tests

### Test 11: 1000-Turn Simulation

```bash
# Run in Unity Editor
# Set SimulationBootstrap to 1000 turns
# Click Play
```

### Expected Results:
✅ Simulation completes in reasonable time (< 5 minutes)
✅ No memory leaks (check Profiler)
✅ No infinite loops
✅ Game log written successfully

### Test 12: Multiple Contested Tiles

### Setup:
Create scenario with 5+ contested tiles simultaneously

### Expected Results:
✅ All tiles show red pulse
✅ No performance degradation
✅ All contests resolve eventually
✅ No visual glitches

---

## Visual Verification

### Test 13: Contested Tile Pulse

1. Create a contested tile (Test 6)
2. Observe visual effect

### Checklist:
✅ Red border visible around hex tile
✅ Border pulses (alpha and width change)
✅ Pulse is smooth (no flickering)
✅ Pulse height slightly above tile surface
✅ Pulse disappears when contest resolves

### Test 14: EncounterUI Appearance

1. Trigger any encounter
2. Inspect UI elements

### Checklist:
✅ Modal background is semi-transparent black
✅ Dialog box is dark gray, centered
✅ Title is yellow: "ENCOUNTER DETECTED!"
✅ Description text is clear and readable
✅ Buttons are green (PROCEED/YIELD) and red (ATTACK)
✅ Ship info shows: unit ID, location, HP
✅ Status text updates correctly
✅ UI layout is not overlapping

---

## Edge Cases

### Test 15: Same-Owner Units

### Setup:
Move two of your own ships to same tile

### Expected Results:
✅ No encounter triggered
✅ Ships peacefully stack on same tile
✅ Console: "X friendly units moving to Y - allowing peaceful stacking"

### Test 16: Destroyed Unit in Encounter

### Setup:
1. Create encounter
2. Unit gets destroyed before decision made (complex scenario)

### Expected Results:
✅ No crash
✅ Encounter resolves with remaining units
✅ Destroyed unit ignored

---

## Debugging Tips

### If Encounter Not Detected:
1. Check console for: `[TurnResolver] PASSING ENCOUNTER` or `ENTRY ENCOUNTER`
2. Verify ships are enemy-owned (not same player)
3. Check ship positions are actually swapping (PASSING) or same destination (ENTRY)

### If UI Not Appearing:
1. Check console for: `[EncounterUI] ShowEncounters called`
2. Verify local player has units in encounter
3. Check GameManager.encounterUI is not null
4. Verify GamePhase is CollisionResolution

### If Contested Tile Pulse Missing:
1. Check console for: `[TurnAnimator] Animating contested tile created`
2. Verify HexRenderer.UpdateContestedTiles() was called
3. Check ContestedTilePulse component added to tile GameObject
4. Verify LineRenderer is initialized

### If Decisions Not Submitted:
1. Check console for: `[GameManager] Unit X decision: Y`
2. Verify SubmitEncounterDecision() is called
3. Check encounter.AwaitingPlayerChoices is false after all decide

---

## Success Criteria

### All Tests Pass:
- ✅ Test 1-10: Core functionality verified
- ✅ Test 11-12: Performance acceptable
- ✅ Test 13-14: Visuals correct
- ✅ Test 15-16: Edge cases handled

### Zero Critical Bugs:
- No crashes
- No null reference exceptions
- No infinite loops
- No data corruption

### User Experience:
- UI is intuitive
- Buttons are clearly labeled
- Visual feedback is immediate
- Animations are smooth

---

## Completion Report Template

After testing, fill out this report:

```
# Encounter System Test Results

Date: ___________
Tester: ___________

## Test Results
- [ ] Test 1: Headless Simulation
- [ ] Test 2: PASSING Peaceful Swap
- [ ] Test 3: PASSING Combat
- [ ] Test 4: ENTRY All Yield
- [ ] Test 5: ENTRY Single Attacker
- [ ] Test 6: ENTRY Contested Tile
- [ ] Test 7: Contested Resolution
- [ ] Test 8: Multiple Encounters
- [ ] Test 9: AI Decisions
- [ ] Test 10: Backward Compatibility
- [ ] Test 11: 1000-Turn Simulation
- [ ] Test 12: Multiple Contested Tiles
- [ ] Test 13: Pulse Visual
- [ ] Test 14: UI Appearance
- [ ] Test 15: Same-Owner Units
- [ ] Test 16: Edge Cases

## Bugs Found
1. _______________ (Severity: High/Medium/Low)
2. _______________
3. _______________

## Performance Notes
- 1000-turn simulation time: _____ seconds
- Memory usage: _____ MB
- Frame rate with 5 contested tiles: _____ FPS

## Overall Assessment
- [ ] Ready for production
- [ ] Needs minor fixes
- [ ] Needs major fixes

## Notes
_______________________________________________
_______________________________________________
_______________________________________________
```

---

## Next Steps After Testing

1. **If All Tests Pass:**
   - Mark Phase 10 as complete
   - Create git commit with comprehensive message
   - Proceed to cleanup (remove old collision system)

2. **If Issues Found:**
   - Log bugs in issue tracker
   - Prioritize by severity
   - Fix critical bugs immediately
   - Re-test after fixes

3. **Documentation:**
   - Update player-facing documentation
   - Add encounter system to rulebook
   - Create tutorial/help screens

---

## Quick Command Reference

```bash
# Check compilation
# Unity Editor → Console → Look for errors

# Run headless test
# Unity Editor → Scenes → MainScene → Play

# View logs
cat game_log.txt

# Check git status
git status

# Stage changes
git add Assets/Scripts/

# Commit (after testing passes)
git commit -m "Implement Encounter System (Phases 1-10)"
```

---

## Contact

For questions or issues:
1. Check ENCOUNTER_SYSTEM_IMPLEMENTATION_SUMMARY.md
2. Review inline code comments
3. Consult ENCOUNTER_SYSTEM.md (original design doc)
4. Ask the implementation team

**Happy Testing!** 🚀
