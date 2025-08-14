# Battle Controls Guide

## Overview
The Zoids Battle Game now features comprehensive player controls during battle, allowing for strategic decision-making and tactical gameplay.

## Battle Screen Layout

### 1. Zoid Status Display (Top Section)
- **Real-time status** for both Player 1 and Player 2 Zoids
- **Health percentage** (calculated from dents: 100% - (dents × 20%))
- **Shield status** and rank
- **Stealth status** and rank
- **Current battle position/status**
- **Distance between Zoids**

### 2. Battle Log (Middle Section)
- Scrollable log of all battle events
- Turn-by-turn action descriptions
- Combat results and status changes
- Clear visual feedback for player actions

### 3. Battle Controls (Interactive Section)
Appears when it's a human player's turn (hidden during AI turns)

#### Movement Controls
- **Stand Still**: Stay in current position
- **Close Distance**: Move closer to the enemy
- **Retreat**: Move away from the enemy
- **Circle Enemy**: Move to flank the enemy
- **Search**: Look for hidden/stealthed enemies

#### Combat Controls
- **Attack Enemy** checkbox: Enable/disable attacking
- **Attack Type dropdown**: Choose attack type (populated based on Zoid's capabilities)
  - Melee (0m range)
  - Close Range (≤500m)
  - Mid Range (≤1000m)
  - Long Range (>1000m)
- **Range indicator**: Shows current distance and whether attacks can hit

#### Shield Controls
- **Toggle Shield** checkbox: Turn shield on/off
- **Status display**: Current shield state
- **Rank display**: Shield effectiveness level
- Only enabled for Zoids with shield capabilities

#### Stealth Controls
- **Toggle Stealth** checkbox: Turn stealth on/off
- **Status display**: Current stealth state
- **Rank display**: Stealth effectiveness level
- Only enabled for Zoids with stealth capabilities

### 4. Action Buttons
- **Execute Actions**: Confirm and execute all selected actions
- **Auto Action**: Let the AI choose optimal actions for this turn

## Gameplay Flow

### Turn Sequence
1. **Turn Start**: Player is presented with current battle status
2. **Action Selection**: Player chooses movement, combat, and special actions
3. **Action Execution**: Player clicks "Execute Actions" to confirm
4. **Resolution**: Actions are processed and results displayed
5. **Next Turn**: Control passes to the next player/AI

### Player vs Player Mode
- Both players take turns making decisions
- Full control over all actions for each player
- Strategic planning required for optimal play

### Player vs AI Mode
- Human player gets full control interface
- AI automatically makes decisions (displayed in battle log)
- Mix of strategic human play vs. automated AI tactics

## Strategic Considerations

### Movement Strategy
- **Closing distance** allows for more powerful melee attacks
- **Retreating** can help with long-range combat or escape
- **Circling** can provide tactical advantages and dodge bonuses
- **Searching** is useful when the enemy is using stealth

### Combat Strategy
- Different attack types have different ranges and effectiveness
- Consider enemy shields when planning attacks
- Balance between attacking and defensive positioning

### Shield Usage
- Shields provide damage reduction but may have energy costs
- Toggle strategically based on incoming threats
- Higher rank shields provide better protection

### Stealth Usage
- Stealth can make you harder to detect and hit
- May prevent enemy from attacking effectively
- Balance stealth with offensive capabilities

## Controls Summary

| Control | Purpose | When Available |
|---------|---------|----------------|
| Movement Radios | Choose movement type | Always (human turns) |
| Attack Checkbox | Enable/disable attacking | Always (human turns) |
| Attack Type Combo | Select attack method | When attack is enabled |
| Shield Toggle | Turn shield on/off | Zoids with shields only |
| Stealth Toggle | Turn stealth on/off | Zoids with stealth only |
| Execute Actions | Confirm turn actions | When waiting for input |
| Auto Action | Let AI decide | When waiting for input |

## Status Indicators

### Health Display
- **Green**: >60% health
- **Orange**: 30-60% health  
- **Red**: <30% health

### Range Display
- **Green**: "CAN ATTACK" - within range
- **Red**: "OUT OF RANGE" - too far for selected attack

### Shield/Stealth Status
- Shows current ON/OFF state
- Displays capability rank (0 = not available)

## Tips for New Players

1. **Study your Zoid**: Check what attack types and special abilities you have
2. **Watch the distance**: Different attacks work at different ranges
3. **Use shields defensively**: Turn them on when expecting enemy attacks
4. **Stealth for positioning**: Use stealth to get into better tactical positions
5. **Auto Action**: Use this feature to learn AI tactics and strategies
6. **Status monitoring**: Keep an eye on both Zoid status displays for tactical information

The battle system now provides deep tactical gameplay while remaining accessible to new players through the Auto Action feature and clear visual feedback.
