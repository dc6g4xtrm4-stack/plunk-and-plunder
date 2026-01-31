# Combat UI Visual Improvements

## Overview
Enhanced the combat UI with improved visuals including dice dots, a top bar overlay layout, and a pulsing combat location indicator.

## Changes Made to DiceCombatUI.cs

### 1. Dice Display - Numbers to Dots
**Changed from:** Simple numbers ("1", "2", "3", etc.)
**Changed to:** Traditional dice face patterns using filled circle dots (●)

#### New Method: `GetDiceDots(int value)`
Returns authentic dice dot patterns:
- **1:** Single center dot
- **2:** Diagonal corners
- **3:** Diagonal line
- **4:** Four corners
- **5:** Four corners + center
- **6:** Two columns of three

The dots are arranged using newlines and spacing to create the classic dice face appearance.

### 2. Top Bar Overlay Layout
**Changed from:** Full-screen modal that covers the entire screen
**Changed to:** Compact top bar overlay (200px tall) anchored to the top of the screen

#### Layout Changes:
- **Modal Panel:** Now anchored to top of screen with 200px height
- **Dialog Box:** 1400px wide x 180px tall, horizontally centered in top bar
- **Title:** Left-aligned "COMBAT - Round X" text (28px font)
- **Attacker/Defender Panels:** Compact 280x160px panels positioned side-by-side
- **Result Text:** Positioned on right side with continue button
- **Overall:** All combat info visible in a horizontal bar without blocking the game view

### 3. Combat Location Indicator
**New Feature:** Pulsing red circle at the combat hex location

#### Implementation:
- **Visual:** Red semi-transparent cylinder (1.5 unit diameter, 0.1 unit height)
- **Position:** Placed at combat hex location, slightly above ground (0.3 units)
- **Animation:** Pulses between 0.4 and 0.8 alpha, with slight scale pulsing (1.5 to 1.7 units)
- **Lifecycle:** Appears when combat starts, disappears when player clicks Continue

#### New Methods:
- `ShowCombatIndicator()` - Creates and positions the indicator
- `PulseCombatIndicator()` - Coroutine that animates the pulsing effect
- `HideCombatIndicator()` - Hides the indicator when combat UI closes
- `OnDestroy()` - Cleanup to remove indicator on component destruction

### 4. Additional Improvements
- **Combat Location Tracking:** Stores `combatLocation` from attacker position
- **Compact Layout:** All UI elements rescaled and repositioned for horizontal layout
- **Better Spacing:** Dice spacing reduced to 70px for tighter display
- **Font Sizes:** Adjusted for readability in compact layout (20-28px range)
- **Line Spacing:** Set to 0.5 for better dot pattern rendering

## Visual Result
The combat UI now:
1. ✅ Shows dice as traditional dot patterns instead of numbers
2. ✅ Displays in a top bar that doesn't cover the game board
3. ✅ Highlights the combat location with a pulsing red indicator
4. ✅ Maintains all functionality (rolling animation, damage display, continue button)

## File Modified
- `Assets/Scripts/UI/DiceCombatUI.cs` (515 lines total)

## Testing Recommendations
1. Start a game and move units into combat
2. Verify dice show dot patterns during rolling and final results
3. Confirm combat UI appears as a top bar overlay
4. Check that the red pulsing circle appears at the combat hex
5. Ensure the indicator disappears when clicking Continue
6. Test with multiple combat rounds to verify indicator repositioning
