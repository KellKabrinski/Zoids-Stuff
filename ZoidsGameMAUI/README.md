# Zoids Battle Game - MAUI Edition

A cross-platform mobile adaptation of the turn-based Zoids battle game, built with .NET MAUI for Android, iOS, Windows, and macOS.

## Features Implemented

### ✅ Core Game Architecture
- **Zoid Data Models**: Complete stats system with Fighting, Strength, Dexterity, Agility, Awareness
- **Battle System**: Attack calculations, defense mechanics, range-based combat (Melee, Close, Mid, Long)
- **Power System**: E-Shield, Concealment, Protection, and various weapon types
- **Save System**: Character data persistence with credits and owned Zoids

### ✅ Zoid Selection Interface
- **Browse Zoids**: View all available Zoids from JSON data
- **Filter Options**: Filter by power level (1-5, 6-10, 11-15, 16+) or affordable options
- **Sort Capabilities**: Sort by name or cost
- **Detailed Stats**: View complete Zoid statistics including:
  - Combat stats (Fighting, Strength, Dexterity, Agility, Awareness)
  - Defense values (Toughness, Parry, Dodge)
  - Movement speeds (Land, Water, Air)
  - Special powers and weapon systems
  - Purchase cost

### ✅ Turn-Based Battle System
- **Action Queue System**: Plan multiple actions per turn
- **Tactical Combat**: Distance-based engagement with range calculations
- **Battle Actions**: Attack, Move, Shield/Stealth toggles
- **Real-time Battle Log**: Detailed combat feedback with timestamps
- **Status Tracking**: HP, shield status, stealth status for both combatants
- **Position System**: Angle-based combat with flanking mechanics
- **AI Opponent**: Automated enemy decision making

### ✅ Battle Interface Features
- **Visual Status Display**: Real-time HP and system status
- **Distance Management**: Dynamic range calculation (Melee/Close/Mid/Long)
- **Movement Options**: Close distance, increase distance, flanking maneuvers
- **Special Systems**: Shield activation/deactivation, stealth systems
- **Turn Management**: Clear action queue, end turn execution
- **Battle Outcome**: Victory/defeat detection with appropriate messaging

### ✅ Game Economy
- **Credits System**: Start with 40,000 credits
- **Purchase Mechanics**: Buy Zoids with credit deduction
- **Affordable Filtering**: Show only Zoids within budget

### ✅ Mobile-Optimized UI
- **Touch-Friendly**: Large buttons and easy navigation
- **Responsive Layout**: Adapts to different screen sizes
- **Visual Feedback**: Selected Zoid details panel
- **Modern Design**: Uses .NET 9 Border controls instead of deprecated Frame

## Technical Architecture

### Services
- `ZoidDataService`: Loads Zoid statistics from JSON
- `SaveSystem`: Handles character data persistence
- `GameEngine`: Battle mechanics and calculations
- `BattleService`: Battle scenario creation and enemy generation

### Models
- `Zoid`: Complete Zoid implementation with battle state
- `ZoidData`: JSON data structure for Zoid statistics
- `CharacterData`: Player save data with credits and owned Zoids

### Views
- `MainPage`: Main menu with game options
- `ZoidSelectionPage`: Zoid browsing and selection interface
- `BattlePage`: Turn-based combat interface with action queue system

### ViewModels
- `ZoidSelectionViewModel`: MVVM pattern implementation for clean separation
- `BattleViewModel`: Complete battle state management and AI logic

## Data Source

The game uses `ConvertedZoidStats.json` containing comprehensive Zoid data including:
- 80+ different Zoid types
- Complete stat blocks for each unit
- Power levels and costs
- Special abilities and weapon systems

## Platform Support

- 🤖 **Android**: Primary target platform
- 🍎 **iOS**: Full compatibility
- 🪟 **Windows**: Desktop support
- 🍎 **macOS**: Desktop support

## Next Steps

### Planned Features
1. **Battle Interface**: Turn-based combat with visual feedback
2. **AI Opponents**: Computer-controlled enemy Zoids
3. **Campaign Mode**: Story-driven battles with progression
4. **Multiplayer**: Local or online PvP battles
5. **Customization**: Zoid modifications and upgrades

### UI Enhancements
1. **Zoid Images**: Visual representations of each Zoid
2. **Animations**: Battle effects and transitions
3. **Sound Effects**: Audio feedback for actions
4. **Themes**: Multiple UI color schemes

## Building and Running

```bash
# Build for all platforms
dotnet build

# Build for specific platform
dotnet build -f net9.0-android     # Android
dotnet build -f net9.0-ios         # iOS  
dotnet build -f net9.0-windows10.0.19041.0  # Windows
dotnet build -f net9.0-maccatalyst # macOS

# Run on Windows
dotnet run -f net9.0-windows10.0.19041.0
```

## Project Structure

```
ZoidsGameMAUI/
├── Models/           # Data models and game types
├── Services/         # Business logic and data access
├── Views/           # XAML pages and UI
├── ViewModels/      # MVVM view models
├── Platforms/       # Platform-specific code
└── ConvertedZoidStats.json  # Game data
```

## Game Flow

1. **Main Menu**: Choose New Game, Load Game, or Zoid Shop
2. **Zoid Selection**: Browse and select Zoids for battle or purchase
3. **Battle Setup**: Select "Start Battle" to enter combat
4. **Turn-Based Combat**: 
   - **Planning Phase**: Queue multiple actions (Attack, Move, Shield, Stealth)
   - **Execution Phase**: Watch actions resolve with detailed feedback
   - **Enemy Turn**: AI opponent takes actions automatically
   - **Victory/Defeat**: Battle ends when a Zoid is defeated
5. **Progression**: Earn credits and expand Zoid collection (Coming Soon)

---

*This is a fan project inspired by the Zoids franchise, built for educational and entertainment purposes.*
