using System.Collections.ObjectModel;
using ZoidsGameMAUI.Services;
using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Views;

[QueryProperty(nameof(PlayerZoidJson), "PlayerZoid")]
[QueryProperty(nameof(EnemyZoidJson), "EnemyZoid")]
[QueryProperty(nameof(ZoidName), "zoidName")]
[QueryProperty(nameof(PowerLevel), "powerLevel")]
public partial class BattlePage : ContentPage
{
    private readonly GameEngine _gameEngine;
    private readonly ZoidDataService _zoidDataService;
    private readonly SaveSystem _saveSystem;
    private readonly BattleService _battleService;
    private Zoid _playerZoid = new();
    private Zoid _enemyZoid = new();
    private ObservableCollection<string> _battleLog = new();
    private ObservableCollection<string> _actionQueue = new();
    private int _currentTurn = 1;
    private bool _isPlayerTurn = true;
    private bool _battleEnded = false;
    private double _currentDistance = 1000.0;

    public string PlayerZoidJson { get; set; } = "";
    public string EnemyZoidJson { get; set; } = "";
    public string ZoidName { get; set; } = "";
    public string PowerLevel { get; set; } = "";

    public BattlePage(GameEngine gameEngine, ZoidDataService zoidDataService, SaveSystem saveSystem, BattleService battleService)
    {
        InitializeComponent();
        _gameEngine = gameEngine;
        _zoidDataService = zoidDataService;
        _saveSystem = saveSystem;
        _battleService = battleService;
        
        // Set up the UI
        BattleLogView.ItemsSource = _battleLog;
        ActionQueueView.ItemsSource = _actionQueue;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        
        // Create Zoids using selected data or defaults
        await CreateZoidsAsync();
        InitializeBattle();
    }

    private async Task CreateZoidsAsync()
    {
        // Create player Zoid from selection or default
        if (!string.IsNullOrEmpty(ZoidName))
        {
            // Use selected Zoid
            var selectedZoidData = await _zoidDataService.GetZoidDataAsync(ZoidName);
            if (selectedZoidData != null)
            {
                _playerZoid = new Zoid(selectedZoidData);
                // Set battle state
                _playerZoid.Position = "neutral";
                _playerZoid.Angle = 0.0;
                _playerZoid.Dents = 0;
                _playerZoid.Status = "intact";
                _playerZoid.ShieldOn = false;
                _playerZoid.StealthOn = false;
            }
            else
            {
                await CreateDefaultPlayerZoid();
            }
        }
        else
        {
            await CreateDefaultPlayerZoid();
        }

        // Create enemy Zoid
        await CreateDefaultEnemyZoid();
    }

    private async Task CreateDefaultPlayerZoid()
    {
        // Create a default player Zoid (Shield Liger)
        var shieldLigerData = await _zoidDataService.GetZoidDataAsync("Shield Liger");
        if (shieldLigerData != null)
        {
            _playerZoid = new Zoid(shieldLigerData);
            // Set battle state
            _playerZoid.Position = "neutral";
            _playerZoid.Angle = 0.0;
            _playerZoid.Dents = 0;
            _playerZoid.Status = "intact";
            _playerZoid.ShieldOn = false;
            _playerZoid.StealthOn = false;
        }
    }

    private async Task CreateDefaultEnemyZoid()
    {
        try
        {
            // Use AI logic to select an appropriate enemy based on player's power level
            int playerPowerLevel = _playerZoid?.PowerLevel ?? 15; // Default to 15 if no player zoid
            _enemyZoid = await _battleService.CreateRandomEnemyAsync(playerPowerLevel);
            
            // Set battle state
            _enemyZoid.Position = "defensive"; 
            _enemyZoid.Angle = 180.0;
            _enemyZoid.Dents = 0;
            _enemyZoid.Status = "intact";
            
            LogMessage($"Enemy Zoid selected: {_enemyZoid.Name} (Power Level: {_enemyZoid.PowerLevel})");
            if (_playerZoid != null)
            {
                LogMessage($"Battle begins! {_playerZoid.Name} vs {_enemyZoid.Name}");
            }
            else
            {
                LogMessage($"Battle begins! Player vs {_enemyZoid.Name}");
            }
        }
        catch (Exception ex)
        {
            // Fallback to hardcoded Command Wolf if AI selection fails
            LogMessage($"Warning: AI enemy selection failed ({ex.Message}), using fallback enemy.");
            var enemyData = await _zoidDataService.GetZoidDataAsync("Command Wolf");
            if (enemyData != null)
            {
                _enemyZoid = new Zoid(enemyData);
                _enemyZoid.Position = "defensive"; 
                _enemyZoid.Angle = 180.0;
                _enemyZoid.Dents = 0;
                _enemyZoid.Status = "intact";
                LogMessage($"Fallback enemy selected: {_enemyZoid.Name}");
            }
        }
        
        // Ensure proper battle initialization
        _enemyZoid.ShieldOn = false;
        _enemyZoid.StealthOn = false;
    }

    private void InitializeBattle()
    {
        // Add null safety checks
        if (_playerZoid == null || _enemyZoid == null)
        {
            LogMessage("Error: Battle initialization failed - Zoids not properly created");
            return;
        }

        // Initialize positions
        _playerZoid.Position = "neutral";
        _playerZoid.Angle = 0.0;
        _enemyZoid.Position = "defensive";
        _enemyZoid.Angle = 180.0;
        
        // Reset battle state
        _playerZoid.Dents = 0;
        _playerZoid.Status = "intact";
        _playerZoid.ShieldOn = false;
        _playerZoid.StealthOn = false;
        
        _enemyZoid.Dents = 0;
        _enemyZoid.Status = "intact";
        _enemyZoid.ShieldOn = false;
        _enemyZoid.StealthOn = false;
        
        UpdateUI();
        LogMessage("=== BATTLE START ===");
        LogMessage($"{_playerZoid.Name} vs {_enemyZoid.Name}");
        LogMessage($"Initial distance: {_currentDistance}m");
        LogMessage("Planning phase - Queue your actions, then End Turn to execute.");
    }

    private void UpdateUI()
    {
        // Ensure UI updates happen on the main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Add null safety checks
                if (_playerZoid == null || _enemyZoid == null)
                {
                    return;
                }

                // Update Zoid status displays
                PlayerZoidName.Text = _playerZoid.Name;
                EnemyZoidName.Text = _enemyZoid.Name;
                
                var playerHpPercent = Math.Max(0, 100 - (_playerZoid.Dents * 10)); // Rough HP calculation
                var enemyHpPercent = Math.Max(0, 100 - (_enemyZoid.Dents * 10));
                
                PlayerZoidStatus.Text = $"HP: {playerHpPercent}% | Shield: {(_playerZoid.ShieldOn ? "ON" : "OFF")} | Stealth: {(_playerZoid.StealthOn ? "ON" : "OFF")}";
                EnemyZoidStatus.Text = $"HP: {enemyHpPercent}% | Shield: {(_enemyZoid.ShieldOn ? "ON" : "OFF")} | Stealth: {(_enemyZoid.StealthOn ? "ON" : "OFF")}";
                
                // Update battle situation
                TurnIndicator.Text = $"Turn {_currentTurn}";
                CurrentPhase.Text = _isPlayerTurn ? "Your Turn" : "Enemy Turn";
                
                DistanceLabel.Text = $"Distance: {_currentDistance:F0}m";
                RangeLabel.Text = $"Range: {_gameEngine.DetermineRange(_currentDistance)}";
                
                PlayerPositionLabel.Text = $"Player: {_playerZoid.Position} ({_playerZoid.Angle:F0}°)";
                EnemyPositionLabel.Text = $"Enemy: {_enemyZoid.Position} ({_enemyZoid.Angle:F0}°)";
                
                // Update button states
                var canAct = _isPlayerTurn && !_battleEnded && _playerZoid.Status != "defeated";
                AttackButton.IsEnabled = canAct;
                MoveButton.IsEnabled = canAct;
                ShieldButton.IsEnabled = canAct && _playerZoid.HasShield();
                StealthButton.IsEnabled = canAct && _playerZoid.HasStealth();
                
                ClearActionsButton.IsEnabled = _actionQueue.Count > 0;
                EndTurnButton.IsEnabled = _isPlayerTurn && !_battleEnded;
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating UI: {ex.Message}");
            }
        });
    }

    private async void OnAttackClicked(object sender, EventArgs e)
    {
        if (!_isPlayerTurn || _battleEnded) return;

        // Check if we have any attack capability at all
        bool hasAnyAttack = _playerZoid.Melee > 0 || _playerZoid.CloseRange > 0 || 
                          _playerZoid.MidRange > 0 || _playerZoid.LongRange > 0;
        
        if (!hasAnyAttack)
        {
            await DisplayAlert("No Attack", $"Your {_playerZoid.Name} has no weapons!", "OK");
            return;
        }

        // Calculate predicted position after any queued moves
        var predictedDistance = CalculatePredictedDistance();
        var predictedRange = _gameEngine.DetermineRange(predictedDistance);
        var predictedAttackDamage = GetAttackDamage(_playerZoid, predictedRange);
        
        string attackInfo;
        if (predictedAttackDamage > 0)
        {
            attackInfo = $"At predicted position ({predictedDistance:F0}m, {predictedRange} range): {predictedAttackDamage} damage available";
        }
        else
        {
            // Find best available attack
            var availableAttacks = new[]
            {
                (Ranges.Melee, GetAttackDamage(_playerZoid, Ranges.Melee)),
                (Ranges.Close, GetAttackDamage(_playerZoid, Ranges.Close)),
                (Ranges.Mid, GetAttackDamage(_playerZoid, Ranges.Mid)),
                (Ranges.Long, GetAttackDamage(_playerZoid, Ranges.Long))
            }.Where(r => r.Item2 > 0).OrderByDescending(r => r.Item2).ToList();

            if (availableAttacks.Any())
            {
                var best = availableAttacks.First();
                attackInfo = $"No attack at predicted position ({predictedDistance:F0}m, {predictedRange} range). Best available: {best.Item1} range ({best.Item2} damage)";
            }
            else
            {
                attackInfo = "No attacks available";
            }
        }

        var result = await DisplayAlert("Queue Attack", 
            $"Queue an attack action?\n\n{attackInfo}\n\nThe best available attack will be used when the action executes.", 
            "Queue Attack", "Cancel");
        
        if (result)
        {
            _actionQueue.Add("Attack");
            LogMessage($"Queued: Attack ({attackInfo})");
            UpdatePredictedDistanceDisplay();
        }
    }

    private async void OnMoveClicked(object sender, EventArgs e)
    {
        if (!_isPlayerTurn || _battleEnded) return;

        // Get maximum move speed for this Zoid
        var maxMoveSpeed = _playerZoid.GetSpeed("land");
        
        // Show movement type options first
        var moveType = await DisplayActionSheet("Movement Type", "Cancel", null, 
            "Close Distance (Move In)", 
            "Increase Distance (Move Out)", 
            "Change Position (Flanking)", 
            "Change Angle (Facing)");

        if (moveType == "Cancel" || moveType == null) return;

        if (moveType == "Close Distance (Move In)" || moveType == "Increase Distance (Move Out)")
        {
            // Allow player to choose movement distance
            var speedOptions = new List<string>();
            var speedValues = new List<int>();
            
            // Create speed options (1/4, 1/2, 3/4, full speed, custom)
            for (int i = 1; i <= 4; i++)
            {
                var speedFraction = (double)i / 4.0;
                var actualSpeed = (int)(maxMoveSpeed * speedFraction);
                if (actualSpeed > 0)
                {
                    var isClosing = moveType.Contains("Close");
                    var predictedDistance = isClosing 
                        ? Math.Max(0, _currentDistance - actualSpeed)
                        : _currentDistance + actualSpeed;
                    var predictedRange = _gameEngine.DetermineRange(predictedDistance);
                    
                    var speedName = i switch
                    {
                        1 => "Quarter Speed",
                        2 => "Half Speed", 
                        3 => "3/4 Speed",
                        4 => "Full Speed",
                        _ => $"{speedFraction:P0} Speed"
                    };
                    
                    speedOptions.Add($"{speedName} ({actualSpeed}m) → {predictedDistance:F0}m ({predictedRange})");
                    speedValues.Add(actualSpeed);
                }
            }
            
            // Add custom speed option
            speedOptions.Add("Custom Speed...");
            speedValues.Add(-1); // Special value to indicate custom speed
            
            if (!speedOptions.Any())
            {
                await DisplayAlert("No Movement", "Your Zoid has no movement capability!", "OK");
                return;
            }
            
            var selectedOption = await DisplayActionSheet("Movement Speed", "Cancel", null, speedOptions.ToArray());
            
            if (selectedOption != "Cancel" && selectedOption != null)
            {
                var selectedIndex = speedOptions.IndexOf(selectedOption);
                var selectedSpeed = speedValues[selectedIndex];
                
                // Handle custom speed selection
                if (selectedSpeed == -1)
                {
                    var customSpeedInput = await DisplayPromptAsync("Custom Speed", 
                        $"Enter movement distance (1-{maxMoveSpeed}m):", 
                        "OK", "Cancel", maxMoveSpeed.ToString());
                        
                    if (customSpeedInput != null && int.TryParse(customSpeedInput, out int customSpeed))
                    {
                        // Clamp to valid range
                        selectedSpeed = Math.Max(1, Math.Min(maxMoveSpeed, customSpeed));
                    }
                    else
                    {
                        return; // User canceled or entered invalid input
                    }
                }
                
                var isClosing = moveType.Contains("Close");
                var predictedDistance = isClosing 
                    ? Math.Max(0, _currentDistance - selectedSpeed)
                    : _currentDistance + selectedSpeed;
                var predictedRange = _gameEngine.DetermineRange(predictedDistance);
                
                var actionName = isClosing ? "Move: Close Distance" : "Move: Increase Distance";
                var direction = isClosing ? "closer to" : "away from";
                
                _actionQueue.Add($"{actionName}:{selectedSpeed}");
                LogMessage($"Queued: Move {direction} enemy ({selectedSpeed}m) (Current: {_currentDistance:F0}m → After move: {predictedDistance:F0}m, {predictedRange} range)");
                UpdatePredictedDistanceDisplay();
            }
        }
        else if (moveType == "Change Position (Flanking)")
        {
            _actionQueue.Add("Move: Flanking Maneuver");
            LogMessage("Queued: Flanking maneuver");
            UpdatePredictedDistanceDisplay();
        }
        else if (moveType == "Change Angle (Facing)")
        {
            var angle = await DisplayPromptAsync("Change Facing", "Enter new angle (0-359):", "OK", "Cancel", "0");
            if (angle != null && double.TryParse(angle, out double newAngle))
            {
                newAngle = ((newAngle % 360) + 360) % 360;
                _actionQueue.Add($"Move: Face {newAngle:F0}°");
                LogMessage($"Queued: Turn to face {newAngle:F0}°");
                UpdatePredictedDistanceDisplay();
            }
        }
    }

    private void OnShieldClicked(object sender, EventArgs e)
    {
        if (!_isPlayerTurn || _battleEnded || !_playerZoid.HasShield()) return;

        var action = _playerZoid.ShieldOn ? "Shield: Deactivate" : "Shield: Activate";
        _actionQueue.Add(action);
        LogMessage($"Queued: {action}");
        UpdatePredictedDistanceDisplay();
    }

    private void OnStealthClicked(object sender, EventArgs e)
    {
        if (!_isPlayerTurn || _battleEnded || !_playerZoid.HasStealth()) return;

        var action = _playerZoid.StealthOn ? "Stealth: Deactivate" : "Stealth: Activate";
        _actionQueue.Add(action);
        LogMessage($"Queued: {action}");
        UpdatePredictedDistanceDisplay();
    }

    private void OnClearActionsClicked(object sender, EventArgs e)
    {
        _actionQueue.Clear();
        LogMessage("All queued actions cleared.");
        UpdatePredictedDistanceDisplay();
    }

    private async void OnEndTurnClicked(object sender, EventArgs e)
    {
        if (!_isPlayerTurn || _battleEnded) return;

        LogMessage("=== EXECUTING TURN ===");
        
        // Execute player actions
        await ExecutePlayerActions();
        
        // Check for battle end
        if (await CheckBattleEndAsync()) return;
        
        // Enemy turn
        _isPlayerTurn = false;
        UpdateUI();
        
        await Task.Delay(1000); // Brief pause
        
        // Execute enemy AI
        await ExecuteEnemyTurn();
        
        // Check for battle end
        if (await CheckBattleEndAsync()) return;
        
        // Next turn
        _currentTurn++;
        _isPlayerTurn = true;
        UpdateUI();
        
        LogMessage($"=== TURN {_currentTurn} BEGINS ===");
    }

    private async Task ExecutePlayerActions()
    {
        foreach (var action in _actionQueue.ToList())
        {
            await ExecuteAction(_playerZoid, _enemyZoid, action, true);
            await Task.Delay(500); // Brief delay between actions
        }
        _actionQueue.Clear();
        UpdatePredictedDistanceDisplay(); // Clear the predicted distance display
    }

    private async Task ExecuteEnemyTurn()
    {
        LogMessage("Enemy is thinking...");
        await Task.Delay(1000);
        
        // Simple AI: Attack if in range, otherwise move closer
        var range = _gameEngine.DetermineRange(_currentDistance);
        var attackDamage = GetAttackDamage(_enemyZoid, range);
        
        if (attackDamage > 0)
        {
            var result = _gameEngine.ProcessAttack(_enemyZoid, _playerZoid, range, _currentDistance, _enemyZoid.Angle);
            LogMessage(result.Message);
            
            if (result.Success)
            {
                UpdateUI();
            }
        }
        else
        {
            // Move closer
            _currentDistance = Math.Max(0, _currentDistance - _enemyZoid.GetSpeed("land"));
            LogMessage($"Enemy moves closer. New distance: {_currentDistance:F0}m");
        }
        
        UpdateUI();
    }

    private Task ExecuteAction(Zoid actor, Zoid target, string action, bool isPlayer)
    {
        var actorName = isPlayer ? "You" : "Enemy";
        
        if (action == "Attack" || action.StartsWith("Attack"))
        {
            // Calculate range and damage based on current distance at execution time
            var range = _gameEngine.DetermineRange(_currentDistance);
            var attackDamage = GetAttackDamage(actor, range);
            
            if (attackDamage <= 0)
            {
                // Try to find any available attack at any range
                var availableRanges = new[]
                {
                    (Ranges.Melee, GetAttackDamage(actor, Ranges.Melee)),
                    (Ranges.Close, GetAttackDamage(actor, Ranges.Close)),
                    (Ranges.Mid, GetAttackDamage(actor, Ranges.Mid)),
                    (Ranges.Long, GetAttackDamage(actor, Ranges.Long))
                }.Where(r => r.Item2 > 0).ToList();
                
                if (availableRanges.Any())
                {
                    var bestRange = availableRanges.OrderByDescending(r => r.Item2).First();
                    LogMessage($"{actorName} cannot attack at {range} range (distance: {_currentDistance:F0}m). Best available: {bestRange.Item1} range with {bestRange.Item2} damage.");
                }
                else
                {
                    LogMessage($"{actorName} has no available attacks!");
                }
                return Task.CompletedTask;
            }
            
            var result = _gameEngine.ProcessAttack(actor, target, range, _currentDistance, actor.Angle);
            LogMessage($"{actorName}: {result.Message} (at {_currentDistance:F0}m, {range} range, {attackDamage} damage)");
        }
        else if (action.StartsWith("Move: Close"))
        {
            int moveDistance;
            
            // Check if action includes custom speed (format: "Move: Close Distance:150")
            if (action.Contains(":") && action.Split(':').Length > 1)
            {
                var parts = action.Split(':');
                if (int.TryParse(parts.Last(), out moveDistance))
                {
                    // Use custom speed
                }
                else
                {
                    // Fallback to default speed
                    moveDistance = actor.GetSpeed("land");
                }
            }
            else
            {
                // Use default speed for backward compatibility
                moveDistance = actor.GetSpeed("land");
            }
            
            _currentDistance = Math.Max(0, _currentDistance - moveDistance);
            LogMessage($"{actorName} moves closer ({moveDistance}m). Distance: {_currentDistance:F0}m");
            
            // Update UI to show distance change immediately
            DistanceLabel.Text = $"Distance: {_currentDistance:F0}m";
            RangeLabel.Text = $"Range: {_gameEngine.DetermineRange(_currentDistance)}";
        }
        else if (action.StartsWith("Move: Increase"))
        {
            int moveDistance;
            
            // Check if action includes custom speed (format: "Move: Increase Distance:150")
            if (action.Contains(":") && action.Split(':').Length > 1)
            {
                var parts = action.Split(':');
                if (int.TryParse(parts.Last(), out moveDistance))
                {
                    // Use custom speed
                }
                else
                {
                    // Fallback to default speed
                    moveDistance = actor.GetSpeed("land");
                }
            }
            else
            {
                // Use default speed for backward compatibility
                moveDistance = actor.GetSpeed("land");
            }
            
            _currentDistance += moveDistance;
            LogMessage($"{actorName} moves away ({moveDistance}m). Distance: {_currentDistance:F0}m");
            
            // Update UI to show distance change immediately
            DistanceLabel.Text = $"Distance: {_currentDistance:F0}m";
            RangeLabel.Text = $"Range: {_gameEngine.DetermineRange(_currentDistance)}";
        }
        else if (action.StartsWith("Move: Face"))
        {
            var parts = action.Split(' ');
            if (parts.Length > 2 && double.TryParse(parts[2].Replace("°", ""), out double angle))
            {
                actor.Angle = angle;
                LogMessage($"{actorName} turns to face {angle:F0}°");
            }
        }
        else if (action.StartsWith("Shield:"))
        {
            _gameEngine.ProcessShieldToggle(actor);
            var status = actor.ShieldOn ? "activated" : "deactivated";
            LogMessage($"{actorName} {status} shield");
        }
        else if (action.StartsWith("Stealth:"))
        {
            _gameEngine.ProcessStealthToggle(actor);
            var status = actor.StealthOn ? "activated" : "deactivated";
            LogMessage($"{actorName} {status} stealth");
        }

        return Task.CompletedTask;
    }

    private async Task<bool> CheckBattleEndAsync()
    {
        try
        {
            // Add null safety checks
            if (_playerZoid == null || _enemyZoid == null)
            {
                LogMessage("Error: Battle end check failed - Zoids are null");
                return true; // End battle to prevent further crashes
            }

            if (_playerZoid.Status == "defeated")
            {
                _battleEnded = true;
                LogMessage("=== DEFEAT ===");
                LogMessage($"{_playerZoid.Name} has been defeated!");
                UpdateUI();
                
                await Task.Delay(2000);
                await DisplayAlert("Battle Ended", "You have been defeated!", "OK");
                return true;
            }
            else if (_enemyZoid.Status == "defeated")
            {
                _battleEnded = true;
                LogMessage("=== VICTORY ===");
                LogMessage($"{_enemyZoid.Name} has been defeated!");
                LogMessage("");
                LogMessage("🏆 VICTORY REWARD 🏆");
                LogMessage("Awarding 5,000 credits for victory!");
                UpdateUI();
                
                // Award credits for victory
                await AwardVictoryCreditsAsync();
                
                await Task.Delay(2000);
                await DisplayAlert("Victory!", 
                    "Congratulations! You have won the battle!\n\n" +
                    "🎉 Victory Reward: 5,000 Credits! 🎉", 
                    "Collect Reward");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            LogMessage($"Error in battle end check: {ex.Message}");
            _battleEnded = true;
            return true; // End battle to prevent further issues
        }
    }

    private async Task AwardVictoryCreditsAsync()
    {
        try
        {
            // Load current character data
            var character = await _saveSystem.LoadCharacterAsync("current_save");
            if (character != null)
            {
                // Award 5,000 credits for victory
                var oldCredits = character.Credits;
                character.Credits += 5000;
                
                // Save the updated character data
                await _saveSystem.SaveCharacterAsync(character, "current_save");
                LogMessage($"Credits: {oldCredits:N0} → {character.Credits:N0} (+5,000)");
                LogMessage("Credits saved successfully!");
            }
            else
            {
                LogMessage("Warning: Could not load character data to award credits");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error awarding victory credits: {ex.Message}");
        }
    }

    private int GetAttackDamage(Zoid zoid, Ranges range)
    {
        return range switch
        {
            Ranges.Melee => zoid.Melee,
            Ranges.Close => zoid.CloseRange,
            Ranges.Mid => zoid.MidRange,
            Ranges.Long => zoid.LongRange,
            _ => 0
        };
    }

    private double CalculatePredictedDistance()
    {
        var predictedDistance = _currentDistance;
        var maxMoveSpeed = _playerZoid.GetSpeed("land");
        
        // Look through queued actions to predict final distance
        foreach (var action in _actionQueue)
        {
            if (action.StartsWith("Move: Close"))
            {
                int moveDistance;
                
                // Check if action includes custom speed (format: "Move: Close Distance:150")
                if (action.Contains(":") && action.Split(':').Length > 1)
                {
                    var parts = action.Split(':');
                    if (int.TryParse(parts.Last(), out moveDistance))
                    {
                        // Use custom speed
                    }
                    else
                    {
                        // Fallback to default speed
                        moveDistance = maxMoveSpeed;
                    }
                }
                else
                {
                    // Use default speed for backward compatibility
                    moveDistance = maxMoveSpeed;
                }
                
                predictedDistance = Math.Max(0, predictedDistance - moveDistance);
            }
            else if (action.StartsWith("Move: Increase"))
            {
                int moveDistance;
                
                // Check if action includes custom speed (format: "Move: Increase Distance:150")
                if (action.Contains(":") && action.Split(':').Length > 1)
                {
                    var parts = action.Split(':');
                    if (int.TryParse(parts.Last(), out moveDistance))
                    {
                        // Use custom speed
                    }
                    else
                    {
                        // Fallback to default speed
                        moveDistance = maxMoveSpeed;
                    }
                }
                else
                {
                    // Use default speed for backward compatibility
                    moveDistance = maxMoveSpeed;
                }
                
                predictedDistance += moveDistance;
            }
            // Note: Flanking and angle changes don't affect distance
        }
        
        return predictedDistance;
    }

    private void UpdatePredictedDistanceDisplay()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (_actionQueue.Count > 0)
                {
                    var predictedDistance = CalculatePredictedDistance();
                    var predictedRange = _gameEngine.DetermineRange(predictedDistance);
                    
                    if (Math.Abs(predictedDistance - _currentDistance) > 0.1) // Only show if different
                    {
                        PredictedDistanceLabel.Text = $"→ After moves: {predictedDistance:F0}m ({predictedRange} range)";
                        PredictedDistanceLabel.IsVisible = true;
                    }
                    else
                    {
                        PredictedDistanceLabel.IsVisible = false;
                    }
                }
                else
                {
                    PredictedDistanceLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating predicted distance: {ex.Message}");
            }
        });
    }

    private void LogMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _battleLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // Auto-scroll to bottom
            if (BattleLogView.ItemsSource is ObservableCollection<string> log && log.Count > 0)
            {
                BattleLogView.ScrollTo(log.Last(), position: ScrollToPosition.End, animate: true);
            }
        });
    }

    private async void OnExitBattleClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Exit Battle", 
            "Are you sure you want to exit the battle? Progress will be lost.", 
            "Yes", "No");
        
        if (result)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public bool HasQueuedActions => _actionQueue.Count > 0;
}
