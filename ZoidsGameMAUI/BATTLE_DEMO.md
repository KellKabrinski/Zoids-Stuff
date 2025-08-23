# Battle System Demo

## How to Test the Battle Interface

1. **Start the Application**
   - Run the MAUI app on Windows: `dotnet run -f net9.0-windows10.0.19041.0`

2. **Navigate to Battle**
   - Click "New Game" or "Zoid Shop" on the main menu
   - Select any Zoid from the list (click on it)
   - Click "Select for Battle" 
   - Confirm to start the battle

3. **Battle Interface Overview**
   - **Top Section**: Shows both Zoids' HP, Shield, and Stealth status
   - **Middle Section**: Distance, range, and battle log
   - **Bottom Section**: Action buttons and controls

## Battle Actions

### Attack
- Click "Attack" to queue an attack action
- Damage depends on range (Melee/Close/Mid/Long)
- Success depends on attack roll vs defense

### Move
- **Close Distance**: Move closer to enemy (better for melee)
- **Increase Distance**: Move away (better for long-range)
- **Flanking**: Positional advantage
- **Change Facing**: Set precise angle (0-359°)

### Special Systems
- **Shield**: Toggle energy shield (if available)
- **Stealth**: Toggle stealth systems (if available)

### Turn Management
- **Queue Actions**: Click multiple action buttons
- **Clear Actions**: Remove all queued actions
- **End Turn**: Execute all queued actions

## Battle Flow Example

```
Turn 1 - Planning Phase:
1. Queue "Attack" (adds attack to queue)
2. Queue "Move: Close Distance" 
3. Queue "Shield: Activate"
4. Click "End Turn"

Execution Phase:
- Attack executes (roll vs defense)
- Move closer (distance decreases)
- Shield activates
- Enemy turn begins automatically

Turn 2 begins...
```

## Combat Mechanics

### Range System
- **Melee (0m)**: High damage, requires adjacent position
- **Close (0-500m)**: Moderate damage, good for most Zoids
- **Mid (500-1000m)**: Balanced range
- **Long (1000m+)**: Sniper range, lower accuracy

### Defense Calculation
- Base defense (Parry for melee, Dodge for ranged)
- Shield bonus (+Shield Rank)
- Stealth bonus (+Stealth Rank)  
- Angle modifier (rear attacks are easier)

### Status Effects
- **Intact**: No damage penalties
- **Dazed**: Minor penalties (1+ Toughness in damage)
- **Stunned**: Major penalties (2+ Toughness in damage)  
- **Defeated**: Battle ends (3+ Toughness in damage)

## Testing Tips

1. **Try Different Ranges**: Move in/out to test weapon effectiveness
2. **Use Shields Tactically**: Activate before enemy attacks
3. **Experiment with Angles**: Face different directions
4. **Queue Multiple Actions**: Plan complex turns
5. **Watch the Battle Log**: See detailed combat feedback

The battle system is fully functional and provides tactical depth while remaining mobile-friendly!
