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
        _enemyZoid.Position = "defensive";
        
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
                
                PlayerPositionLabel.Text = $"Player Position: {_playerZoid.Position}";
                EnemyPositionLabel.Text = $"Enemy Position: {_enemyZoid.Position}";
                
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
        
        // Show simplified movement options
        var moveType = await DisplayActionSheet("Movement Type", "Cancel", null, 
            "Close Distance (Move In)", 
            "Increase Distance (Move Out)");

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
        LogMessage($"Enemy {_enemyZoid.Name} ({_enemyZoid.AIPersonality} personality) is thinking...");
        await Task.Delay(1000);
        
        // Personality-based AI decision making
        var decision = MakeAIDecision(_enemyZoid, _playerZoid);
        
        switch (decision.Action)
        {
            case "Attack":
                await ExecuteEnemyAttack(decision);
                break;
            case "Move":
                await ExecuteEnemyMove(decision);
                break;
            case "Shield":
                await ExecuteEnemyShield();
                break;
            case "Stealth":
                await ExecuteEnemyStealth();
                break;
            default:
                LogMessage("Enemy decides to wait this turn.");
                break;
        }
        
        UpdateUI();
    }

    private AIDecision MakeAIDecision(Zoid aiZoid, Zoid targetZoid)
    {
        var range = _gameEngine.DetermineRange(_currentDistance);
        var attackDamage = GetAttackDamage(aiZoid, range);
        var aiHealthPercent = CalculateHpPercent(aiZoid);
        var targetHealthPercent = CalculateHpPercent(targetZoid);
        
        // Decision weights based on personality
        var decision = new AIDecision();
        
        if (aiZoid.AIPersonality == AIPersonality.Aggressive)
        {
            // Aggressive AI: Prioritize offense and closing distance
            if (attackDamage > 0)
            {
                // Can attack - always prefer attacking when aggressive
                decision.Action = "Attack";
                decision.Reasoning = $"Aggressive: Attack available at {range} range for {attackDamage} damage";
            }
            else
            {
                // Can't attack - move aggressively closer
                var optimalDistance = GetOptimalAttackDistance(aiZoid);
                if (optimalDistance < _currentDistance)
                {
                    decision.Action = "Move";
                    decision.MoveDistance = Math.Min(aiZoid.GetSpeed("land"), (int)(_currentDistance - optimalDistance));
                    decision.Reasoning = $"Aggressive: Moving {decision.MoveDistance}m closer to attack range";
                }
                else
                {
                    // Already too close, back up slightly but stay aggressive
                    decision.Action = "Move";
                    decision.MoveDistance = -(int)(aiZoid.GetSpeed("land") * 0.3); // Small retreat
                    decision.Reasoning = "Aggressive: Minor repositioning to optimize attack";
                }
            }
            
            // Override with defensive actions only in critical health
            if (aiHealthPercent < 25 && !aiZoid.ShieldOn && aiZoid.HasShield())
            {
                decision.Action = "Shield";
                decision.Reasoning = "Aggressive: Critical health - emergency shield activation";
            }
        }
        else // Defensive personality
        {
            // Defensive AI: Prioritize survival and optimal positioning
            if (aiHealthPercent < 50)
            {
                // Low health - prioritize defense
                if (!aiZoid.ShieldOn && aiZoid.HasShield())
                {
                    decision.Action = "Shield";
                    decision.Reasoning = "Defensive: Low health - activating shield";
                }
                else if (!aiZoid.StealthOn && aiZoid.HasStealth())
                {
                    decision.Action = "Stealth";
                    decision.Reasoning = "Defensive: Low health - activating stealth";
                }
                else if (attackDamage > 0 && targetHealthPercent < aiHealthPercent)
                {
                    // Only attack if target is weaker
                    decision.Action = "Attack";
                    decision.Reasoning = $"Defensive: Calculated strike - target at {targetHealthPercent}% health";
                }
                else
                {
                    // Move to safer distance
                    decision.Action = "Move";
                    decision.MoveDistance = -(int)(aiZoid.GetSpeed("land") * 0.7); // Retreat
                    decision.Reasoning = "Defensive: Retreating to safer distance";
                }
            }
            else
            {
                // Healthy - measured approach
                var optimalDistance = GetOptimalAttackDistance(aiZoid);
                
                if (Math.Abs(_currentDistance - optimalDistance) > 200) // Not at optimal range
                {
                    decision.Action = "Move";
                    var targetMove = optimalDistance < _currentDistance ? 
                        Math.Min(aiZoid.GetSpeed("land"), (int)(_currentDistance - optimalDistance)) :
                        -Math.Min(aiZoid.GetSpeed("land"), (int)(optimalDistance - _currentDistance));
                    decision.MoveDistance = targetMove;
                    decision.Reasoning = $"Defensive: Moving to optimal range ({optimalDistance}m)";
                }
                else if (attackDamage > 0)
                {
                    // At good range and can attack
                    decision.Action = "Attack";
                    decision.Reasoning = $"Defensive: Positioned well - attacking for {attackDamage} damage";
                }
                else
                {
                    // Fine-tune positioning
                    decision.Action = "Move";
                    decision.MoveDistance = (int)(aiZoid.GetSpeed("land") * 0.2);
                    decision.Reasoning = "Defensive: Fine-tuning position";
                }
            }
        }
        
        return decision;
    }

    private double GetOptimalAttackDistance(Zoid zoid)
    {
        // Find the range with the best damage output
        var ranges = new[]
        {
            (Range: Ranges.Melee, Distance: 50.0, Damage: GetAttackDamage(zoid, Ranges.Melee)),
            (Range: Ranges.Close, Distance: 300.0, Damage: GetAttackDamage(zoid, Ranges.Close)),
            (Range: Ranges.Mid, Distance: 800.0, Damage: GetAttackDamage(zoid, Ranges.Mid)),
            (Range: Ranges.Long, Distance: 1500.0, Damage: GetAttackDamage(zoid, Ranges.Long))
        };
        
        var bestRange = ranges.OrderByDescending(r => r.Damage).FirstOrDefault();
        return bestRange.Distance;
    }

    private async Task ExecuteEnemyAttack(AIDecision decision)
    {
        var range = _gameEngine.DetermineRange(_currentDistance);
        var result = _gameEngine.ProcessAttack(_enemyZoid, _playerZoid, range, _currentDistance, 0);
        LogMessage($"Enemy: {result.Message} - {decision.Reasoning}");
        
        if (result.Success)
        {
            UpdateUI();
        }
        
        await Task.CompletedTask; // Satisfy async requirement
    }

    private async Task ExecuteEnemyMove(AIDecision decision)
    {
        var oldDistance = _currentDistance;
        
        if (decision.MoveDistance > 0)
        {
            _currentDistance = Math.Max(0, _currentDistance - decision.MoveDistance);
            LogMessage($"Enemy moves {decision.MoveDistance}m closer. Distance: {oldDistance:F0}m → {_currentDistance:F0}m - {decision.Reasoning}");
        }
        else
        {
            _currentDistance += Math.Abs(decision.MoveDistance);
            LogMessage($"Enemy moves {Math.Abs(decision.MoveDistance)}m away. Distance: {oldDistance:F0}m → {_currentDistance:F0}m - {decision.Reasoning}");
        }
        
        await Task.CompletedTask; // Satisfy async requirement
    }

    private async Task ExecuteEnemyShield()
    {
        if (_enemyZoid.HasShield() && !_enemyZoid.ShieldOn)
        {
            _gameEngine.ProcessShieldToggle(_enemyZoid);
            LogMessage($"Enemy activates shield! Shield strength: {_enemyZoid.ShieldRank}");
        }
        else
        {
            LogMessage("Enemy attempted to activate shield but it's not available or already active.");
        }
        
        await Task.CompletedTask; // Satisfy async requirement
    }

    private async Task ExecuteEnemyStealth()
    {
        if (_enemyZoid.HasStealth() && !_enemyZoid.StealthOn)
        {
            _gameEngine.ProcessStealthToggle(_enemyZoid);
            LogMessage($"Enemy activates stealth! Concealment rank: {_enemyZoid.StealthRank}");
        }
        else
        {
            LogMessage("Enemy attempted to activate stealth but it's not available or already active.");
        }
        
        await Task.CompletedTask; // Satisfy async requirement
    }

    private int CalculateHpPercent(Zoid zoid)
    {
        return Math.Max(0, 100 - (zoid.Dents * 10)); // Rough HP calculation
    }

    // Helper class for AI decision making
    public class AIDecision
    {
        public string Action { get; set; } = "Wait";
        public int MoveDistance { get; set; } = 0;
        public string Reasoning { get; set; } = "";
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
            
            var result = _gameEngine.ProcessAttack(actor, target, range, _currentDistance, 0);
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
