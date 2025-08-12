# Zoids Battle Game Engine Refactor

This refactoring separates the game logic from user interface concerns, making the engine reusable across different UI frameworks (console, WPF, WinForms, web, mobile, etc.).

## Architecture Overview

### Core Components

1. **`GameEngine.cs`** - Abstract base class containing all game logic
2. **`GameTypes.cs`** - Supporting data structures (`GameState`, `PlayerAction`, `BattleResult`, etc.)
3. **`ConsoleGameEngine.cs`** - Console-specific implementation
4. **`GraphicalGameEngine.cs`** - UI framework agnostic implementation for graphical apps
5. **`WPFExample.cs`** - Examples showing how to integrate with different UI frameworks

### Key Benefits

- **Separation of Concerns**: Game logic is completely separate from UI code
- **Reusability**: The same engine can be used with any UI framework
- **Testability**: Game logic can be easily unit tested without UI dependencies
- **Maintainability**: UI changes don't affect game logic and vice versa
- **Extensibility**: New UI frameworks can be supported by implementing the abstract methods

## How to Use

### For Console Applications

```csharp
var gameEngine = new ConsoleGameEngine();
var zoids = LoadZoids("ConvertedZoidStats.json");
CharacterData playerData = new CharacterData();

do
{
    var result = gameEngine.RunBattle(zoids, playerData);
    playerData = result.PlayerData;
} while (gameEngine.AskPlayAgain());
```

### For Graphical Applications

```csharp
var gameEngine = new GraphicalGameEngine(
    displayMessage: (msg) => LogTextBox.AppendText(msg + "\\n"),
    showChoiceDialog: (choices) => ShowChoiceDialog(choices),
    getNumericInput: (prompt) => GetNumericInput(prompt),
    askYesNo: () => MessageBox.Show("Continue?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes
);

var result = gameEngine.RunBattle(availableZoids, playerData);
```

## Abstract Methods to Implement

When creating a new UI implementation, you need to implement these abstract methods:

- `ChooseBattleType()` - Let user choose land/water/air battle
- `ChooseOpponentType()` - Let user choose PvP or AI opponent
- `ChoosePlayerZoid()` - Let user select or purchase a Zoid
- `GetStartingDistance()` - Get the initial distance between Zoids
- `GetPlayerAction()` - Get the player's action for their turn
- `DisplayMessage()` - Show a message to the user
- `DisplayZoidStatus()` - Show current Zoid status
- `DisplayBattleStart()` - Show battle beginning
- `DisplayTurnStart()` - Show whose turn it is
- `DisplayBattleResult()` - Show battle outcome
- `AskPlayAgain()` - Ask if player wants another battle

## UI Framework Examples

### WPF Implementation
```csharp
private void DisplayMessage(string message)
{
    Dispatcher.Invoke(() => {
        LogTextBox.AppendText(message + Environment.NewLine);
    });
}

private int ShowChoiceDialog(string[] choices)
{
    var dialog = new ChoiceDialog("Select option:", choices);
    return dialog.ShowDialog() == true ? dialog.SelectedIndex : 0;
}
```

### WinForms Implementation
```csharp
private void DisplayMessage(string message)
{
    if (logTextBox.InvokeRequired)
    {
        logTextBox.Invoke(() => DisplayMessage(message));
        return;
    }
    logTextBox.AppendText(message + Environment.NewLine);
}
```

### Web/Blazor Implementation
```csharp
private void DisplayMessage(string message)
{
    battleLog.Add(message);
    InvokeAsync(StateHasChanged);
}

private async Task<int> ShowChoiceDialog(string[] choices)
{
    return await JSRuntime.InvokeAsync<int>("showChoiceDialog", choices);
}
```

## Game Flow

1. **Initialize** - Load Zoids data and create game engine
2. **Choose Battle Type** - Land, water, or air
3. **Choose Opponent** - PvP or AI
4. **Select Zoids** - Player chooses Zoid (handles save/load for AI mode)
5. **Battle Loop** - Turn-based combat until one Zoid is defeated
6. **Results** - Show winner and update player data
7. **Repeat** - Ask if player wants another battle

## Data Structures

### `PlayerAction`
- `MovementType` - None, Close, Retreat, Circle, Search, StandStill
- `MoveDistance` - How far to move
- `AngleChange` - For circling maneuvers
- `ShouldAttack` - Whether to attack this turn
- `ToggleShield` - Whether to toggle shield state
- `ToggleStealth` - Whether to toggle stealth state

### `GameState`
- `BattleType` - land, water, or air
- `Distance` - Current distance between Zoids
- `TurnNumber` - Current turn count
- `IsAIMode` - Whether playing against AI

### `BattleResult`
- `Winner` - The winning Zoid
- `PlayerData` - Updated character data with credits/Zoids
- `Player1Zoid` - First player's Zoid
- `Player2Zoid` - Second player's Zoid

## Migration from Original Code

The original `Program.cs` has been simplified to just:
1. Load Zoids data
2. Create console game engine
3. Run battles in a loop
4. Save character data

All the complex game logic has been moved to the `GameEngine` base class, making it reusable and testable.

## Testing

The refactored architecture makes unit testing much easier:

```csharp
[Test]
public void TestBattleLogic()
{
    var mockEngine = new MockGameEngine();
    var testZoids = CreateTestZoids();
    var playerData = new CharacterData();
    
    var result = mockEngine.RunBattle(testZoids, playerData);
    
    Assert.IsNotNull(result.Winner);
}
```

You can create mock implementations of the abstract methods to test specific scenarios without any UI dependencies.
