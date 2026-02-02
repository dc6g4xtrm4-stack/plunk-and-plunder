# Replay System - Quick Start Guide

## How to Test the Replay System (2 minutes)

### Step 1: Generate a Simulation
1. Open Unity Editor
2. Press Play
3. Click **"Run AI Simulation (4 AI, 100 turns)"**
4. Wait ~10 seconds for simulation to complete
5. Stop playback

### Step 2: Run the Replay
1. Press Play again
2. Click **"Replay Latest Simulation"**
3. Watch the replay!

### Step 3: Try the Controls
- Click **"|| Pause"** to pause
- Click **"▶ Resume"** to continue
- Click speed buttons: **0.5x**, **1x**, **2x**, **5x**, **10x**
- Click **"Exit to Main Menu"** when done

## What You'll See

- Map renders with 4 players' starting positions
- 4 shipyards (one per player)
- 4 ships (one per player)
- Turn counter advancing: "Turn X/100"
- Progress bar filling up

## What You WON'T See (Yet)

- Ship movements (needs GameSimulator logging)
- Combat animations (needs GameSimulator logging)
- New ships spawning (needs GameSimulator logging)

**This is expected!** Current simulation logs only contain collision events, which don't animate.

## If Something Goes Wrong

### "No simulation logs found"
→ Run a simulation first (Step 1)

### Replay button not visible
→ Check Console for compilation errors

### Map doesn't appear
→ Check Console for parsing errors

### Turns advance too fast
→ This is normal with no animations. Try slower speed (0.5x)

## Files to Check

- **Simulation logs**: Project root folder, `simulation_*.txt`
- **Code**: `Assets/Scripts/Replay/`
- **Testing guide**: `REPLAY_SYSTEM_TESTING.md`
- **Full docs**: `REPLAY_SYSTEM_IMPLEMENTATION.md`

## Next Steps After Testing

1. Verify basic functionality works
2. Review Console logs for errors
3. Read `REPLAY_SYSTEM_TESTING.md` for comprehensive tests
4. Enhance GameSimulator to log more events
5. Test again to see full animations

## Questions?

Check the full documentation:
- Implementation details → `REPLAY_SYSTEM_IMPLEMENTATION.md`
- Testing procedures → `REPLAY_SYSTEM_TESTING.md`
