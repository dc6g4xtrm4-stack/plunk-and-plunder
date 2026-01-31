# Dice Dot Patterns Reference

This document shows the traditional dice face patterns implemented in the combat UI.

## Dice Face Patterns

Each dice value is represented using filled circle characters (●) arranged in traditional dice patterns:

```
┌─────────┬─────────┬─────────┐
│    1    │    2    │    3    │
├─────────┼─────────┼─────────┤
│         │ ●       │ ●       │
│   ●     │         │   ●     │
│         │       ● │       ● │
└─────────┴─────────┴─────────┘

┌─────────┬─────────┬─────────┐
│    4    │    5    │    6    │
├─────────┼─────────┼─────────┤
│ ●     ● │ ●     ● │ ●     ● │
│         │   ●     │ ●     ● │
│ ●     ● │ ●     ● │ ●     ● │
└─────────┴─────────┴─────────┘
```

## Pattern Details

### One (1)
- Single dot in the center
- Pattern: `\n  ●\n`

### Two (2)
- Diagonal from top-left to bottom-right
- Pattern: `●\n\n    ●`

### Three (3)
- Diagonal line through center
- Pattern: `●\n  ●\n    ●`

### Four (4)
- Four corners (2x2 grid)
- Pattern: `●   ●\n\n●   ●`

### Five (5)
- Four corners plus center dot
- Pattern: `●   ●\n  ●\n●   ●`

### Six (6)
- Two columns of three dots
- Pattern: `●   ●\n●   ●\n●   ●`

## In-Game Display

### During Rolling Animation
- Dice rapidly cycle through random dot patterns (1-6)
- Shaking animation adds to the rolling effect
- Duration: 1 second

### Final Results
- Each die shows its actual rolled value in dot form
- Attacker: 3 dice (left panel, blue background)
- Defender: 2 dice (middle-left panel, red background)
- Results displayed for player review before continuing

## Visual Example in Combat

```
┌────────────────────────────────────────────────────────────────────────────┐
│  COMBAT - Round 1      [Attacker Panel]  [Defender Panel]   Results    [Continue] │
│                        ●   ●  ●   ●  ●   ●    ●   ●  ●              │
│                        ●   ●        ●     ●   ●  Attacker wins!     │
│                        ●   ●  ●   ●  ●   ●    ●   ●  Defender -2 HP │
└────────────────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

- Font size: 28px
- Line spacing: 0.5 (compact)
- Text color: Black
- Background: White dice with slight transparency (0.9 alpha)
- Dice size: 60x60 pixels each
- Character used: Unicode filled circle (●) U+25CF
