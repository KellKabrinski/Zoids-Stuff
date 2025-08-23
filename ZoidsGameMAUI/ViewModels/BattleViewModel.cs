using System.Collections.ObjectModel;
using System.ComponentModel;
using ZoidsGameMAUI.Models;
using ZoidsGameMAUI.Services;

namespace ZoidsGameMAUI.ViewModels
{
    public class BattleViewModel : INotifyPropertyChanged
    {
        private readonly GameEngine _gameEngine;
        
        public ObservableCollection<string> BattleLog { get; } = new();
        public ObservableCollection<string> ActionQueue { get; } = new();
        
        private Zoid _playerZoid;
        private Zoid _enemyZoid;
        private int _currentTurn = 1;
        private bool _isPlayerTurn = true;
        private bool _battleEnded = false;
        private double _currentDistance = 1000.0;
        private string _currentPhase = "Planning Phase";

        public BattleViewModel(GameEngine gameEngine, Zoid playerZoid, Zoid enemyZoid)
        {
            _gameEngine = gameEngine;
            _playerZoid = playerZoid;
            _enemyZoid = enemyZoid;
            
            InitializeBattle();
        }

        #region Properties

        public Zoid PlayerZoid
        {
            get => _playerZoid;
            set
            {
                if (_playerZoid != value)
                {
                    _playerZoid = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlayerZoidName));
                    OnPropertyChanged(nameof(PlayerZoidStatus));
                }
            }
        }

        public Zoid EnemyZoid
        {
            get => _enemyZoid;
            set
            {
                if (_enemyZoid != value)
                {
                    _enemyZoid = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EnemyZoidName));
                    OnPropertyChanged(nameof(EnemyZoidStatus));
                }
            }
        }

        public string PlayerZoidName => _playerZoid?.Name ?? "Unknown";
        public string EnemyZoidName => _enemyZoid?.Name ?? "Enemy";

        public string PlayerZoidStatus
        {
            get
            {
                if (_playerZoid == null) return "";
                var hpPercent = CalculateHpPercent(_playerZoid);
                return $"HP: {hpPercent}% | Shield: {(_playerZoid.ShieldOn ? "ON" : "OFF")} | Stealth: {(_playerZoid.StealthOn ? "ON" : "OFF")}";
            }
        }

        public string EnemyZoidStatus
        {
            get
            {
                if (_enemyZoid == null) return "";
                var hpPercent = CalculateHpPercent(_enemyZoid);
                return $"HP: {hpPercent}% | Shield: {(_enemyZoid.ShieldOn ? "ON" : "OFF")} | Stealth: {(_enemyZoid.StealthOn ? "ON" : "OFF")}";
            }
        }

        public int CurrentTurn
        {
            get => _currentTurn;
            set
            {
                if (_currentTurn != value)
                {
                    _currentTurn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TurnText));
                }
            }
        }

        public string TurnText => $"Turn {_currentTurn}";

        public bool IsPlayerTurn
        {
            get => _isPlayerTurn;
            set
            {
                if (_isPlayerTurn != value)
                {
                    _isPlayerTurn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanTakeAction));
                    OnPropertyChanged(nameof(CanEndTurn));
                }
            }
        }

        public string CurrentPhase
        {
            get => _currentPhase;
            set
            {
                if (_currentPhase != value)
                {
                    _currentPhase = value;
                    OnPropertyChanged();
                }
            }
        }

        public double CurrentDistance
        {
            get => _currentDistance;
            set
            {
                if (Math.Abs(_currentDistance - value) > 0.1)
                {
                    _currentDistance = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DistanceText));
                    OnPropertyChanged(nameof(RangeText));
                }
            }
        }

        public string DistanceText => $"Distance: {_currentDistance:F0}m";
        public string RangeText => $"Range: {_gameEngine.DetermineRange(_currentDistance)}";

        public string PlayerPositionText => $"Player: {_playerZoid?.Position ?? "Unknown"} ({_playerZoid?.Angle ?? 0:F0}°)";
        public string EnemyPositionText => $"Enemy: {_enemyZoid?.Position ?? "Unknown"} ({_enemyZoid?.Angle ?? 0:F0}°)";

        public bool CanTakeAction => _isPlayerTurn && !_battleEnded && _playerZoid?.Status != "defeated";
        public bool CanEndTurn => _isPlayerTurn && !_battleEnded;
        public bool CanUseShield => CanTakeAction && (_playerZoid?.HasShield() ?? false);
        public bool CanUseStealth => CanTakeAction && (_playerZoid?.HasStealth() ?? false);
        public bool HasQueuedActions => ActionQueue.Count > 0;

        public bool BattleEnded
        {
            get => _battleEnded;
            set
            {
                if (_battleEnded != value)
                {
                    _battleEnded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanTakeAction));
                    OnPropertyChanged(nameof(CanEndTurn));
                }
            }
        }

        #endregion

        #region Battle Management

        private void InitializeBattle()
        {
            // Reset battle state
            _playerZoid.Position = "neutral";
            _playerZoid.Angle = 0.0;
            _playerZoid.Dents = 0;
            _playerZoid.Status = "intact";
            _playerZoid.ShieldOn = false;
            _playerZoid.StealthOn = false;

            _enemyZoid.Position = "defensive";
            _enemyZoid.Angle = 180.0;
            _enemyZoid.Dents = 0;
            _enemyZoid.Status = "intact";
            _enemyZoid.ShieldOn = false;
            _enemyZoid.StealthOn = false;

            CurrentDistance = 1000.0;
            CurrentTurn = 1;
            IsPlayerTurn = true;
            CurrentPhase = "Planning Phase";
            BattleEnded = false;

            LogMessage("=== BATTLE START ===");
            LogMessage($"{PlayerZoidName} vs {EnemyZoidName}");
            LogMessage($"Initial distance: {CurrentDistance}m");
            LogMessage("Plan your actions, then End Turn to execute.");
        }

        public Task<bool> QueueAttackAsync()
        {
            if (!CanTakeAction) return Task.FromResult(false);

            var range = _gameEngine.DetermineRange(CurrentDistance);
            var attackDamage = GetAttackDamage(_playerZoid, range);

            if (attackDamage <= 0)
            {
                return Task.FromResult(false); // No attack available
            }

            ActionQueue.Add($"Attack ({range} - Dmg:{attackDamage})");
            LogMessage($"Queued: Attack with {range} range weapon");
            OnPropertyChanged(nameof(HasQueuedActions));
            return Task.FromResult(true);
        }

        public void QueueMove(string moveType, double? angle = null)
        {
            if (!CanTakeAction) return;

            string action = moveType switch
            {
                "Close" => "Move: Close Distance",
                "Away" => "Move: Increase Distance", 
                "Flank" => "Move: Flanking Maneuver",
                "Face" when angle.HasValue => $"Move: Face {angle:F0}°",
                _ => "Move: Unknown"
            };

            ActionQueue.Add(action);
            LogMessage($"Queued: {action}");
            OnPropertyChanged(nameof(HasQueuedActions));
        }

        public void QueueShieldToggle()
        {
            if (!CanUseShield) return;

            var action = _playerZoid.ShieldOn ? "Shield: Deactivate" : "Shield: Activate";
            ActionQueue.Add(action);
            LogMessage($"Queued: {action}");
            OnPropertyChanged(nameof(HasQueuedActions));
        }

        public void QueueStealthToggle()
        {
            if (!CanUseStealth) return;

            var action = _playerZoid.StealthOn ? "Stealth: Deactivate" : "Stealth: Activate";
            ActionQueue.Add(action);
            LogMessage($"Queued: {action}");
            OnPropertyChanged(nameof(HasQueuedActions));
        }

        public void ClearActions()
        {
            ActionQueue.Clear();
            LogMessage("All queued actions cleared.");
            OnPropertyChanged(nameof(HasQueuedActions));
        }

        public async Task ExecuteTurnAsync()
        {
            if (!CanEndTurn) return;

            LogMessage("=== EXECUTING TURN ===");
            CurrentPhase = "Execution Phase";

            // Execute player actions
            await ExecutePlayerActionsAsync();

            // Check for battle end
            if (CheckBattleEnd()) return;

            // Enemy turn
            IsPlayerTurn = false;
            CurrentPhase = "Enemy Turn";

            await Task.Delay(1000); // Brief pause

            // Execute enemy AI
            await ExecuteEnemyTurnAsync();

            // Check for battle end
            if (CheckBattleEnd()) return;

            // Next turn
            CurrentTurn++;
            IsPlayerTurn = true;
            CurrentPhase = "Planning Phase";

            LogMessage($"=== TURN {CurrentTurn} BEGINS ===");
            RefreshAllProperties();
        }

        #endregion

        #region Battle Execution

        private async Task ExecutePlayerActionsAsync()
        {
            foreach (var action in ActionQueue.ToList())
            {
                await ExecuteActionAsync(_playerZoid, _enemyZoid, action, true);
                await Task.Delay(500); // Brief delay between actions
            }
            ActionQueue.Clear();
            OnPropertyChanged(nameof(HasQueuedActions));
        }

        private async Task ExecuteEnemyTurnAsync()
        {
            LogMessage("Enemy is thinking...");
            await Task.Delay(1000);

            // Simple AI: Attack if in range, otherwise move closer
            var range = _gameEngine.DetermineRange(CurrentDistance);
            var attackDamage = GetAttackDamage(_enemyZoid, range);

            if (attackDamage > 0)
            {
                var result = _gameEngine.ProcessAttack(_enemyZoid, _playerZoid, range, CurrentDistance, _enemyZoid.Angle);
                LogMessage(result.Message);

                if (result.Success)
                {
                    RefreshAllProperties();
                }
            }
            else
            {
                // Move closer
                var oldDistance = CurrentDistance;
                CurrentDistance = Math.Max(0, CurrentDistance - _enemyZoid.GetSpeed("land"));
                LogMessage($"Enemy moves closer. Distance: {oldDistance:F0}m → {CurrentDistance:F0}m");
            }

            RefreshAllProperties();
        }

        private Task ExecuteActionAsync(Zoid actor, Zoid target, string action, bool isPlayer)
        {
            var actorName = isPlayer ? "You" : "Enemy";

            if (action.StartsWith("Attack"))
            {
                var range = _gameEngine.DetermineRange(CurrentDistance);
                var result = _gameEngine.ProcessAttack(actor, target, range, CurrentDistance, actor.Angle);
                LogMessage($"{actorName}: {result.Message}");
            }
            else if (action.StartsWith("Move: Close"))
            {
                var moveDistance = actor.GetSpeed("land");
                var oldDistance = CurrentDistance;
                CurrentDistance = Math.Max(0, CurrentDistance - moveDistance);
                LogMessage($"{actorName} moves closer. Distance: {oldDistance:F0}m → {CurrentDistance:F0}m");
            }
            else if (action.StartsWith("Move: Increase"))
            {
                var moveDistance = actor.GetSpeed("land");
                var oldDistance = CurrentDistance;
                CurrentDistance += moveDistance;
                LogMessage($"{actorName} moves away. Distance: {oldDistance:F0}m → {CurrentDistance:F0}m");
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

            RefreshAllProperties();
            return Task.CompletedTask;
        }

        #endregion

        #region Utility Methods

        private bool CheckBattleEnd()
        {
            if (_playerZoid.Status == "defeated")
            {
                BattleEnded = true;
                CurrentPhase = "Battle Ended";
                LogMessage("=== DEFEAT ===");
                LogMessage($"{PlayerZoidName} has been defeated!");
                return true;
            }
            else if (_enemyZoid.Status == "defeated")
            {
                BattleEnded = true;
                CurrentPhase = "Battle Ended";
                LogMessage("=== VICTORY ===");
                LogMessage($"{EnemyZoidName} has been defeated!");
                return true;
            }
            return false;
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

        private int CalculateHpPercent(Zoid zoid)
        {
            return Math.Max(0, 100 - (zoid.Dents * 10)); // Rough HP calculation
        }

        private void LogMessage(string message)
        {
            BattleLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(PlayerZoidStatus));
            OnPropertyChanged(nameof(EnemyZoidStatus));
            OnPropertyChanged(nameof(DistanceText));
            OnPropertyChanged(nameof(RangeText));
            OnPropertyChanged(nameof(PlayerPositionText));
            OnPropertyChanged(nameof(EnemyPositionText));
            OnPropertyChanged(nameof(CanTakeAction));
            OnPropertyChanged(nameof(CanEndTurn));
            OnPropertyChanged(nameof(CanUseShield));
            OnPropertyChanged(nameof(CanUseStealth));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
