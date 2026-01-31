# Selection System Testing Guide

## Quick Start Test

### Test 1: Single Unit Selection
1. Start the game
2. Left-click on a ship
3. **Expected**: Cyan pulsing ring appears under the ship
4. **Expected**: HUD shows ship details (HP, position, movement, etc.)

### Test 2: Drag Box Selection
1. Left-click and hold on empty space
2. Drag mouse to create a box over multiple ships
3. **Expected**: Green semi-transparent selection box appears
4. Release mouse button
5. **Expected**: All ships within box show cyan rings
6. **Expected**: HUD shows "[GROUP: 1/X]" and "Press TAB to cycle"

### Test 3: Tab Cycling
1. Box select 3 ships
2. Note the HUD shows details for one ship (active ship)
3. Press TAB
4. **Expected**: Counter changes from "1/3" to "2/3"
5. **Expected**: HUD updates to show different ship's details
6. Press TAB again
7. **Expected**: Counter changes to "3/3"
8. Press TAB once more
9. **Expected**: Counter cycles back to "1/3"

### Test 4: Multi-Unit Movement
1. Box select 2-3 ships
2. Right-click on an empty water tile
3. **Expected**: All selected ships show green rings (order set)
4. **Expected**: Path visualizations appear for all ships
5. **Expected**: HUD shows "Move order queued!"
6. Click "Submit Orders"
7. **Expected**: All ships move together

### Test 5: Selection Persistence During Movement
1. Select a ship and give it a move order
2. Submit orders
3. Watch the ship move
4. **Expected**: Selection ring follows the ship as it moves
5. **Expected**: Ring stays under ship even as it changes position

### Test 6: Mixed Selection (Click After Box)
1. Box select multiple ships
2. Left-click on empty space
3. **Expected**: All selections clear, rings disappear
4. Left-click on a single ship
5. **Expected**: Only that ship is selected (no group counter in HUD)

## Advanced Tests

### Test 7: Stacked Ships Selection
1. Move multiple ships to the same tile
2. Box select all stacked ships
3. **Expected**: All ships show selection rings
4. **Expected**: Rings are offset around the center of the tile
5. Give them all a move order
6. **Expected**: All rings turn green and stay positioned correctly

### Test 8: Order Status Visualization
1. Select 3 ships
2. Give ship A a move order (right-click)
3. **Expected**: Ship A's ring turns green
4. Give ship B a move order
5. **Expected**: Ship B's ring turns green
6. **Expected**: Ship C's ring stays cyan (no order)
7. Press TAB to cycle
8. **Expected**: Each ship's ring reflects its order status

### Test 9: Clear and Reselect
1. Select multiple ships
2. Give them orders (rings turn green)
3. Click empty space to deselect
4. **Expected**: All rings disappear
5. Box select the same ships again
6. **Expected**: Rings reappear as green (orders still queued)

### Test 10: Selection Box Edge Cases
1. Try dragging a very small box (< 5 pixels)
2. **Expected**: Acts as single click, not box select
3. Drag from bottom-right to top-left
4. **Expected**: Selection box still works correctly
5. Drag entirely over water (no ships)
6. **Expected**: Selection clears when released

## Performance Tests

### Test 11: Large Selection
1. Start a game with 5-10 ships
2. Box select all ships at once
3. **Expected**: All show rings without lag
4. Press TAB rapidly 20 times
5. **Expected**: Smooth cycling with no stutter
6. Right-click to move all ships
7. **Expected**: All paths calculate without delay

### Test 12: Rapid Selection Changes
1. Click ship A
2. Immediately click ship B
3. Click ship C
4. **Expected**: Each selection updates cleanly
5. **Expected**: No lingering rings from previous selections

## Bug Checks

### Check 1: Enemy Ships
1. Find an enemy ship
2. Try to box select it
3. **Expected**: Enemy ships are NOT selected
4. Box select area with both friendly and enemy ships
5. **Expected**: Only friendly ships are selected

### Check 2: Structure Selection
1. Find a shipyard or harbor
2. Box select over it
3. **Expected**: Structures are NOT included in ship selection
4. Left-click on structure
5. **Expected**: Ship selection clears, structure selected instead

### Check 3: Order Submission
1. Select multiple ships with orders
2. Submit orders
3. **Expected**: All rings disappear
4. **Expected**: unitsWithOrders is cleared
5. **Expected**: pendingMovePaths is cleared

### Check 4: Phase Changes
1. Select ships in Planning phase
2. Submit orders
3. Wait for Resolution phase
4. **Expected**: Selections are cleared
5. Return to Planning phase
6. **Expected**: Can select ships again normally

## Visual Verification Checklist

- [ ] Selection box is green and semi-transparent
- [ ] Selection box has visible border
- [ ] Selection rings are cyan for "waiting for order"
- [ ] Selection rings turn green when order is set
- [ ] Rings pulse at appropriate speed
- [ ] Rings follow ships during movement
- [ ] Rings are positioned correctly on stacked ships
- [ ] Multiple rings can exist simultaneously
- [ ] HUD shows group counter format: "[GROUP: X/Y]"
- [ ] HUD shows "Press TAB to cycle" hint
- [ ] Help text shows new controls

## Common Issues and Solutions

### Issue: Selection box doesn't appear
- **Check**: Are you dragging at least 5 pixels?
- **Check**: Is the game in Planning phase?

### Issue: Can't select ships
- **Check**: Are you trying to select enemy ships?
- **Check**: Is the game in Planning phase?

### Issue: Tab doesn't cycle
- **Check**: Do you have multiple ships selected?
- **Check**: Does the HUD show a group counter?

### Issue: Rings don't follow ships
- **Check**: Wait for ships to move during Resolution phase
- **Check**: Check Unity console for errors in UpdateUnitVisual

### Issue: Multiple rings appear jumbled
- **Check**: This is expected for stacked ships
- **Check**: Ships should have visual offsets as well

## Expected Console Output

When testing, you should see logs like:
```
[GameHUD] Box selected 3 units
[GameHUD] Set move orders for 3/3 selected units to (5, 10)
[GameHUD] Cycled to unit unit_1 (index 1/3)
[UnitRenderer] Updated unit unit_0 position to (5, 10)
```

## Automated Test Suggestions

If implementing automated tests, focus on:
1. `PerformBoxSelection()` with various screen rectangles
2. `HandleTabCycling()` cycling through list correctly
3. `SetSelectedUnitsDestination()` creating orders for all ships
4. `UpdateSelectionIndicators()` creating correct number of indicators
5. Selection clearing properly on phase changes
