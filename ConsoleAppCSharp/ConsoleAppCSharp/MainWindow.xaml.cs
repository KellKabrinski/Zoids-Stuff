using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZoidsBattle
{
    public partial class MainWindow : Window
    {
        private List<ZoidData> _allZoids = new List<ZoidData>();
        private List<ZoidData> _filteredZoids = new List<ZoidData>();
        private WPFGameEngine? _gameEngine;
        private string _battleType = "land";
        private bool _isAIMode = false;
        private bool _isPlayer1Turn = true;
        private int _zoidChoiceCallCount = 0; // Track which zoid choice call this is
        private Zoid? _player1Zoid;
        private Zoid? _player2Zoid;
        private CharacterData _playerData = new CharacterData();
        
        // Battle state
        private double _currentDistance = 1000.0;
        private double _userSelectedStartingDistance = 1000.0; // User's choice from setup screen
        private int _currentTurn = 1;
        private bool _isBattleActive = false;
        private bool _waitingForPlayerAction = false;
        private Zoid? _currentBattleZoid;
        private Zoid? _enemyBattleZoid;
        
        // Movement selection state
        private MovementType _selectedMovementType = MovementType.StandStill;
        private double _selectedMoveDistance = 0.0;
        private double _projectedDistance = 1000.0; // Distance after movement for attack preview
        
        // Battle history tracking
        private List<BattleRoundResult> _battleHistory = new List<BattleRoundResult>();
        private class BattleRoundResult
        {
            public int RoundNumber { get; set; }
            public string PlayerName { get; set; } = "";
            public bool IsAI { get; set; }
            public string ActionDescription { get; set; } = "";
            public string ResultDescription { get; set; } = "";
            public double DistanceAfter { get; set; }
            public string ZoidStatusAfter { get; set; } = "";
        }

        // Task completion sources for async UI interactions
        private TaskCompletionSource<string>? _battleTypeChoice;
        private TaskCompletionSource<bool>? _opponentTypeChoice;
        private TaskCompletionSource<Zoid>? _zoidChoice;
        private TaskCompletionSource<double>? _distanceChoice;
        private TaskCompletionSource<PlayerAction>? _playerActionChoice;
        private TaskCompletionSource<bool>? _playAgainChoice;

        public MainWindow()
        {
            InitializeComponent();
            LoadZoids();
            InitializeGameEngine();
        }

        private void LoadZoids()
        {
            try
            {
                string jsonPath = "ConvertedZoidStats.json";
                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    _allZoids = JsonSerializer.Deserialize<List<ZoidData>>(json) ?? new List<ZoidData>();
                    StatusText.Text = $"Loaded {_allZoids.Count} Zoids";
                }
                else
                {
                    StatusText.Text = "Zoid data file not found";
                    MessageBox.Show("Could not find ConvertedZoidStats.json file. Please ensure it's in the application directory.", 
                                    "Missing Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading Zoids";
                MessageBox.Show($"Error loading Zoid data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeGameEngine()
        {
            _gameEngine = new WPFGameEngine(
                chooseBattleType: () => GetBattleTypeAsync(),
                chooseOpponentType: () => GetOpponentTypeAsync(),
                chooseZoid: (zoids, isAI) => GetZoidChoiceAsync(zoids, isAI),
                getStartingDistance: () => GetStartingDistanceAsync(),
                getPlayerAction: (current, enemy, distance, detected, state) => GetPlayerActionAsync(current, enemy, distance, detected, state),
                displayMessage: (message) => AddBattleLogMessage(message),
                displayZoidStatus: (zoid, distance) => DisplayZoidStatus(zoid, distance),
                displayBattleStart: (z1, z2) => DisplayBattleStart(z1, z2),
                displayTurnStart: (zoid, turn) => DisplayTurnStart(zoid, turn),
                displayBattleResult: (winner, loser) => DisplayBattleResult(winner, loser),
                askPlayAgain: () => GetPlayAgainAsync()
            );
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            // Determine battle type
            if (LandRadio.IsChecked == true) _battleType = "land";
            else if (WaterRadio.IsChecked == true) _battleType = "water";
            else if (AirRadio.IsChecked == true) _battleType = "air";

            // Determine game mode
            _isAIMode = AIRadio.IsChecked == true;

            // Filter zoids based on battle type
            _filteredZoids = FilterZoids(_allZoids, _battleType).ToList();

            if (!_filteredZoids.Any())
            {
                MessageBox.Show("No Zoids available for that environment!", "No Zoids", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Switch to zoid selection
            SetupPanel.Visibility = Visibility.Collapsed;
            ZoidSelectionPanel.Visibility = Visibility.Visible;
            
            _isPlayer1Turn = true;
            ShowZoidSelection();
        }

        private void ShowZoidSelection()
        {
            if (_isAIMode && !_isPlayer1Turn)
            {
                // AI turn - automatically select
                var aiZoid = SelectAIZoid();
                _player2Zoid = aiZoid;
                StartBattle();
                return;
            }

            string playerText = _isPlayer1Turn ? "Player 1: Choose your Zoid" : "Player 2: Choose your Zoid";
            PlayerSelectionText.Text = playerText;
            
            ZoidListGrid.ItemsSource = _filteredZoids;
            ZoidListGrid.SelectedItem = null;
            SelectZoidButton.IsEnabled = false;
            
            ClearZoidDetails();
        }

        private void ZoidListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ZoidListGrid.SelectedItem is ZoidData selectedZoid)
            {
                SelectZoidButton.IsEnabled = true;
                ShowZoidDetails(selectedZoid);
            }
            else
            {
                SelectZoidButton.IsEnabled = false;
                ClearZoidDetails();
            }
        }

        private void SelectZoid_Click(object sender, RoutedEventArgs e)
        {
            if (ZoidListGrid.SelectedItem is ZoidData selectedZoidData)
            {
                var selectedZoid = new Zoid(selectedZoidData);
                
                if (_isPlayer1Turn)
                {
                    _player1Zoid = selectedZoid;
                    
                    if (_isAIMode)
                    {
                        // AI mode - let AI select
                        _isPlayer1Turn = false;
                        ShowZoidSelection();
                    }
                    else
                    {
                        // PvP mode - switch to player 2
                        _isPlayer1Turn = false;
                        ShowZoidSelection();
                    }
                }
                else
                {
                    _player2Zoid = selectedZoid;
                    StartBattle();
                }
            }
        }

        private Zoid SelectAIZoid()
        {
            if (_player1Zoid == null)
                throw new InvalidOperationException("Player 1 must select a Zoid first");

            // Use the existing AI logic from GameEngine
            var aiCandidates = _filteredZoids
                .Where(z => Math.Abs(z.PowerLevel - _player1Zoid.PowerLevel) <= 1)
                .ToList();

            if (!aiCandidates.Any())
                aiCandidates = _filteredZoids.ToList();

            var random = new Random();
            var personality = random.Next(0, 2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;
            
            ZoidData aiPick;
            if (personality == AIPersonality.Defensive)
            {
                aiPick = aiCandidates
                    .OrderByDescending(z => z.Powers.Any(p => p.Type == "E-Shield" && p.Rank.HasValue && p.Rank.Value > 0))
                    .ThenBy(z => Math.Abs(z.PowerLevel - _player1Zoid.PowerLevel))
                    .ThenBy(_ => random.Next())
                    .First();
            }
            else
            {
                aiPick = aiCandidates
                    .OrderByDescending(z =>
                        z.Powers.Where(p =>
                            p.Type == "Melee" || p.Type == "Close-Range" || p.Type == "Mid-Range" || p.Type == "Long-Range")
                        .Select(p => p.Rank ?? 0).DefaultIfEmpty(0).Max())
                    .ThenBy(z => Math.Abs(z.PowerLevel - _player1Zoid.PowerLevel))
                    .ThenBy(_ => random.Next())
                    .First();
            }

            AddBattleLogMessage($"AI ({personality}) selects: {aiPick.Name} (PL {aiPick.PowerLevel})");
            return new Zoid(aiPick);
        }

        private void StartBattle()
        {
            if (_player1Zoid == null || _player2Zoid == null)
                return;

            // Reset zoid choice counter for new battle
            _zoidChoiceCallCount = 0;

            // Switch to battle view
            ZoidSelectionPanel.Visibility = Visibility.Collapsed;
            BattlePanel.Visibility = Visibility.Visible;
            
            Player1ZoidText.Text = $"Player 1: {_player1Zoid.ZoidName} (PL {_player1Zoid.PowerLevel})";
            Player2ZoidText.Text = $"Player 2: {_player2Zoid.ZoidName} (PL {_player2Zoid.PowerLevel})";
            
            BattleLogTextBox.Clear();
            AddBattleLogMessage("=== BATTLE STARTING ===");
            AddBattleLogMessage("*** BATTLE LOG IS VISIBLE - YOU SHOULD SEE THIS MESSAGE ***");
            AddBattleLogMessage($"Terrain: {_battleType.ToUpper()}");
            AddBattleLogMessage($"Mode: {(_isAIMode ? "Player vs AI" : "Player vs Player")}");
            AddBattleLogMessage($"Starting Distance: {_userSelectedStartingDistance:F0}m");
            AddBattleLogMessage("*** IF YOU CAN SEE THIS, THE BATTLE LOG IS WORKING ***");
            AddBattleLogMessage(""); // Empty line for spacing
            
            // Initialize battle state
            _isBattleActive = true;
            _currentDistance = _userSelectedStartingDistance;
            _currentTurn = 1;
            _waitingForPlayerAction = false;
            _battleHistory.Clear(); // Clear previous battle history
            
            // Initially hide battle controls
            BattleControlsPanel.Visibility = Visibility.Collapsed;
            
            // Initialize previous rounds display
            UpdatePreviousRoundsDisplay();
            
            // Reset zoids to base state
            _player1Zoid.ReturnToBaseState();
            _player2Zoid.ReturnToBaseState();
            
            // Initial status update
            UpdateZoidStatusDisplay();
            
            // Start the actual battle using the game engine
            Task.Run(() =>
            {
                try
                {
                    var result = _gameEngine!.RunBattle(_allZoids, _playerData);
                    _playerData = result.PlayerData;
                    
                    // Show new battle button after battle ends
                    Dispatcher.Invoke(() =>
                    {
                        _isBattleActive = false;
                        BattleControlsPanel.Visibility = Visibility.Collapsed;
                        NewBattleButton.Visibility = Visibility.Visible;
                        AddBattleLogMessage("\n=== BATTLE COMPLETE ===");
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddBattleLogMessage($"Error during battle: {ex.Message}");
                        _isBattleActive = false;
                        BattleControlsPanel.Visibility = Visibility.Collapsed;
                        NewBattleButton.Visibility = Visibility.Visible;
                    });
                }
            });
        }

        private void ShowZoidDetails(ZoidData zoid)
        {
            ZoidDetailsPanel.Children.Clear();
            
            var nameText = new TextBlock
            {
                Text = zoid.Name,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            ZoidDetailsPanel.Children.Add(nameText);
            
            AddDetailText($"Power Level: {zoid.PowerLevel}");
            AddDetailText($"Cost: ${zoid.Cost:N0}");
            AddDetailText("");
            
            AddDetailText("STATS:");
            AddDetailText($"Fighting: {zoid.Stats.Fighting}");
            AddDetailText($"Strength: {zoid.Stats.Strength}");
            AddDetailText($"Dexterity: {zoid.Stats.Dexterity}");
            AddDetailText($"Agility: {zoid.Stats.Agility}");
            AddDetailText($"Awareness: {zoid.Stats.Awareness}");
            AddDetailText("");
            
            AddDetailText("DEFENSES:");
            AddDetailText($"Toughness: {zoid.Defenses.Toughness}");
            AddDetailText($"Parry: {zoid.Defenses.Parry}");
            AddDetailText($"Dodge: {zoid.Defenses.Dodge}");
            AddDetailText("");
            
            AddDetailText("MOVEMENT:");
            AddDetailText($"Land: {zoid.Movement.Land}");
            AddDetailText($"Water: {zoid.Movement.Water}");
            AddDetailText($"Air: {zoid.Movement.Air}");
            AddDetailText("");
            
            if (zoid.Powers.Any())
            {
                AddDetailText("POWERS:");
                foreach (var power in zoid.Powers)
                {
                    string powerText = power.Type;
                    if (power.Rank.HasValue)
                        powerText += $" (Rank {power.Rank})";
                    if (power.Damage.HasValue)
                        powerText += $" (Damage {power.Damage})";
                    AddDetailText($"• {powerText}");
                }
            }
        }

        private void AddDetailText(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 1, 0, 1),
                TextWrapping = TextWrapping.Wrap
            };
            ZoidDetailsPanel.Children.Add(textBlock);
        }

        private void ClearZoidDetails()
        {
            ZoidDetailsPanel.Children.Clear();
            var placeholderText = new TextBlock
            {
                Text = "Select a Zoid to view details",
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            ZoidDetailsPanel.Children.Add(placeholderText);
        }

        private void AddBattleLogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                BattleLogTextBox.AppendText(message + Environment.NewLine);
                BattleLogTextBox.ScrollToEnd();
            });
        }

        private void NewBattle_Click(object sender, RoutedEventArgs e)
        {
            // Reset everything for a new battle
            _player1Zoid = null;
            _player2Zoid = null;
            _isPlayer1Turn = true;
            _isBattleActive = false;
            _waitingForPlayerAction = false;
            _currentDistance = _userSelectedStartingDistance; // Use user's selected distance
            _currentTurn = 1;
            _battleHistory.Clear(); // Clear battle history
            
            BattlePanel.Visibility = Visibility.Collapsed;
            BattleControlsPanel.Visibility = Visibility.Collapsed;
            SetupPanel.Visibility = Visibility.Visible;
            NewBattleButton.Visibility = Visibility.Collapsed;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ExecuteAction_Click(object sender, RoutedEventArgs e)
        {
            if (!_waitingForPlayerAction)
                return;

            var action = GetPlayerActionFromUI();
            
            // Log the player's chosen actions
            bool isPlayer1 = _currentBattleZoid == _player1Zoid;
            string playerName = isPlayer1 ? "Player 1" : "Player 2";
            AddBattleLogMessage($"{playerName} executes: {GetActionDescription(action)}");
            AddBattleLogMessage("Processing actions and calculating results...");
            
            _playerActionChoice?.SetResult(action);
            _waitingForPlayerAction = false;
        }

        private void UpdatePreviousRoundsDisplay()
        {
            PreviousRoundsPanel.Children.Clear();
            
            if (!_battleHistory.Any())
            {
                var placeholderText = new TextBlock
                {
                    Text = "No previous rounds yet",
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                PreviousRoundsPanel.Children.Add(placeholderText);
                return;
            }
            
            // Show the last 3-4 rounds to keep the display manageable
            var recentRounds = _battleHistory.TakeLast(4).ToList();
            
            foreach (var round in recentRounds)
            {
                var roundContainer = new Border
                {
                    BorderBrush = round.IsAI ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(5)
                };
                
                var roundContent = new StackPanel();
                
                // Round header
                var headerText = new TextBlock
                {
                    Text = $"Round {round.RoundNumber}: {round.PlayerName}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = round.IsAI ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.Blue
                };
                roundContent.Children.Add(headerText);
                
                // Player type indicator
                var typeText = new TextBlock
                {
                    Text = round.IsAI ? "🤖 AI" : "👤 Human",
                    FontSize = 10,
                    Foreground = round.IsAI ? System.Windows.Media.Brushes.DarkOrange : System.Windows.Media.Brushes.DarkBlue,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                roundContent.Children.Add(typeText);
                
                // Action description
                var actionText = new TextBlock
                {
                    Text = round.ActionDescription,
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                roundContent.Children.Add(actionText);
                
                // Result (if available)
                if (!string.IsNullOrEmpty(round.ResultDescription) && round.ResultDescription != "Executing...")
                {
                    var resultText = new TextBlock
                    {
                        Text = round.ResultDescription,
                        FontSize = 9,
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    roundContent.Children.Add(resultText);
                }
                
                roundContainer.Child = roundContent;
                PreviousRoundsPanel.Children.Add(roundContainer);
            }
        }

        private void AutoAction_Click(object sender, RoutedEventArgs e)
        {
            if (!_waitingForPlayerAction || _currentBattleZoid == null || _enemyBattleZoid == null)
                return;

            // Generate a comprehensive AI action with multiple action types
            var action = GenerateAutoActionSequence(_currentBattleZoid, _enemyBattleZoid, _currentDistance);
            _playerActionChoice?.SetResult(action);
            _waitingForPlayerAction = false;
            
            // Hide battle controls until next player's turn
            BattleControlsPanel.Visibility = Visibility.Collapsed;
        }

        private PlayerAction GenerateAutoActionSequence(Zoid currentZoid, Zoid enemyZoid, double distance)
        {
            var action = new PlayerAction();
            var random = new Random();
            
            // AI typically performs multiple actions per turn for tactical advantage
            var actionSequence = new List<ActionType>();
            
            // Always consider stealth first if available and not active
            if (currentZoid.StealthRank > 0 && !currentZoid.StealthOn)
            {
                actionSequence.Add(ActionType.Stealth);
                action.ToggleStealth = true;
            }
            
            // Consider shield activation if defensive situation
            if (currentZoid.ShieldRank > 0 && !currentZoid.ShieldOn && distance <= 1000)
            {
                actionSequence.Add(ActionType.Shield);
                action.ToggleShield = true;
            }
            
            // Movement strategy based on distance and capabilities
            bool shouldMove = false;
            if (distance > 800 && currentZoid.Melee > 0) // Close in for melee
            {
                action.MovementType = MovementType.Close;
                action.MoveDistance = currentZoid.GetSpeed(_battleType) * 0.8;
                shouldMove = true;
            }
            else if (distance < 300 && currentZoid.LongRange > currentZoid.Melee) // Back away for ranged
            {
                action.MovementType = MovementType.Retreat;
                action.MoveDistance = currentZoid.GetSpeed(_battleType) * 0.6;
                shouldMove = true;
            }
            else if (random.NextDouble() < 0.3) // 30% chance to circle
            {
                action.MovementType = MovementType.Circle;
                action.AngleChange = 45;
                shouldMove = true;
            }
            
            if (shouldMove)
                actionSequence.Add(ActionType.Move);
            
            // Always try to attack if possible
            if (currentZoid.CanAttack(distance))
            {
                actionSequence.Add(ActionType.Attack);
                action.ShouldAttack = true;
            }
            
            action.ActionSequence = actionSequence;
            return action;
        }

        private void AttackCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateAttackControls();
        }

        private PlayerAction GetPlayerActionFromUI()
        {
            var action = new PlayerAction();
            
            // Build action sequence from the ListBox order
            action.ActionSequence.Clear();
            foreach (string actionName in ActionSequenceListBox.Items)
            {
                if (Enum.TryParse<ActionType>(actionName, out ActionType actionType))
                {
                    action.ActionSequence.Add(actionType);
                }
            }

            // Set movement details if movement is included
            if (action.ActionSequence.Contains(ActionType.Move))
            {
                // Use the _selectedMovementType set by the movement speed dialog
                action.MovementType = _selectedMovementType;
                action.MoveDistance = _selectedMoveDistance;

                // Set appropriate angles for circle movement
                if (action.MovementType == MovementType.Circle)
                {
                    action.AngleChange = 45;
                }
            }

            // Set attack flag
            action.ShouldAttack = action.ActionSequence.Contains(ActionType.Attack);
            
            // Set shield toggle flag
            action.ToggleShield = action.ActionSequence.Contains(ActionType.Shield);
            
            // Set stealth toggle flag
            action.ToggleStealth = action.ActionSequence.Contains(ActionType.Stealth);

            return action;
        }

        private void ShowMovementSpeedSelection()
        {
            if (_currentBattleZoid == null)
                return;

            // Get the maximum speed for current terrain
            double maxSpeed = _currentBattleZoid.GetSpeed(_battleType);
            
            // Create and show a popup dialog for speed selection
            var speedDialog = new MovementSpeedDialog(maxSpeed, _selectedMovementType, _currentDistance, _currentBattleZoid);
            speedDialog.Owner = this;
            
            if (speedDialog.ShowDialog() == true)
            {
                _selectedMovementType = speedDialog.SelectedMovementType;
                _selectedMoveDistance = speedDialog.SelectedSpeed;
                
                // Calculate projected distance after movement
                _projectedDistance = CalculateProjectedDistance(_currentDistance, _selectedMovementType, _selectedMoveDistance);
                
                // Update the UI to show the projected attack range
                UpdateAttackControls();
            }
            else
            {
                // User cancelled - uncheck the movement checkbox
                EnableMovementCheckBox.IsChecked = false;
                _selectedMovementType = MovementType.StandStill;
                _selectedMoveDistance = 0.0;
                _projectedDistance = _currentDistance;
            }
        }

        private double CalculateProjectedDistance(double currentDistance, MovementType movementType, double moveDistance)
        {
            switch (movementType)
            {
                case MovementType.Close:
                    return Math.Max(0, currentDistance - moveDistance);
                case MovementType.Retreat:
                    return currentDistance + moveDistance;
                case MovementType.Circle:
                case MovementType.Search:
                    return currentDistance; // Distance doesn't change for these
                case MovementType.StandStill:
                default:
                    return currentDistance;
            }
        }

        private void ActionSelection_Changed(object sender, RoutedEventArgs e)
        {
            // Special handling for movement checkbox
            if (sender == EnableMovementCheckBox && EnableMovementCheckBox.IsChecked == true)
            {
                // Show movement speed selection popup
                ShowMovementSpeedSelection();
            }
            else if (sender == EnableMovementCheckBox && EnableMovementCheckBox.IsChecked == false)
            {
                // Reset movement selection
                _selectedMovementType = MovementType.StandStill;
                _selectedMoveDistance = 0.0;
                _projectedDistance = _currentDistance;
            }
            
            UpdateActionSequence();
            UpdateControlStates();
        }

        private void UpdateActionSequence()
        {
            // Get currently selected actions
            var selectedActions = new List<string>();
            
            if (EnableMovementCheckBox.IsChecked == true)
                selectedActions.Add("Move");
            if (EnableAttackCheckBox.IsChecked == true)
                selectedActions.Add("Attack");
            if (EnableShieldCheckBox.IsChecked == true)
                selectedActions.Add("Shield");
            if (EnableStealthCheckBox.IsChecked == true)
                selectedActions.Add("Stealth");

            // Update the sequence list, preserving existing order where possible
            var currentSequence = ActionSequenceListBox.Items.Cast<string>().ToList();
            var newSequence = new List<string>();

            // Keep existing items that are still selected, in their current order
            foreach (string action in currentSequence)
            {
                if (selectedActions.Contains(action))
                {
                    newSequence.Add(action);
                    selectedActions.Remove(action);
                }
            }

            // Add any newly selected actions
            newSequence.AddRange(selectedActions);

            // Update the ListBox
            ActionSequenceListBox.Items.Clear();
            foreach (string action in newSequence)
            {
                ActionSequenceListBox.Items.Add(action);
            }
        }

        private void UpdateControlStates()
        {
            // Update attack controls based on current distance and zoid capabilities
            UpdateAttackControls();
            
            // Update status displays
            if (_currentBattleZoid != null)
            {
                CurrentShieldStatusText.Text = $"Current: {(_currentBattleZoid.ShieldOn ? "Active" : "Inactive")}";
                ShieldRankText.Text = $"Rank: {_currentBattleZoid.ShieldRank}";
                EnableShieldCheckBox.IsEnabled = _currentBattleZoid.ShieldRank > 0;
                
                CurrentStealthStatusText.Text = $"Current: {(_currentBattleZoid.StealthOn ? "Active" : "Inactive")}";
                StealthRankText.Text = $"Rank: {_currentBattleZoid.StealthRank}";
                EnableStealthCheckBox.IsEnabled = _currentBattleZoid.StealthRank > 0;
                
                UpdateAttackRangeText();
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ActionSequenceListBox.SelectedIndex;
            if (selectedIndex > 0)
            {
                var item = ActionSequenceListBox.Items[selectedIndex];
                ActionSequenceListBox.Items.RemoveAt(selectedIndex);
                ActionSequenceListBox.Items.Insert(selectedIndex - 1, item);
                ActionSequenceListBox.SelectedIndex = selectedIndex - 1;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = ActionSequenceListBox.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < ActionSequenceListBox.Items.Count - 1)
            {
                var item = ActionSequenceListBox.Items[selectedIndex];
                ActionSequenceListBox.Items.RemoveAt(selectedIndex);
                ActionSequenceListBox.Items.Insert(selectedIndex + 1, item);
                ActionSequenceListBox.SelectedIndex = selectedIndex + 1;
            }
        }

        private void ClearSequence_Click(object sender, RoutedEventArgs e)
        {
            EnableMovementCheckBox.IsChecked = false;
            EnableAttackCheckBox.IsChecked = false;
            EnableShieldCheckBox.IsChecked = false;
            EnableStealthCheckBox.IsChecked = false;
            UpdateActionSequence();
        }

        private void StartingDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (StartingDistanceValueText != null)
            {
                _userSelectedStartingDistance = e.NewValue;
                StartingDistanceValueText.Text = e.NewValue.ToString("F0");
            }
        }

        private void ActionSequence_Drop(object sender, DragEventArgs e)
        {
            // Handle drag and drop reordering
            // This is a simplified implementation
        }

        private void ActionSequence_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private PlayerAction GenerateAutoAction(Zoid currentZoid, Zoid enemyZoid, double distance)
        {
            var action = new PlayerAction();
            var random = new Random();

            // Simple AI logic with proper distances and angles
            if (distance > 500 && currentZoid.CanAttack(distance))
            {
                action.MovementType = MovementType.StandStill;
                action.MoveDistance = 0;
                action.AngleChange = 0;
                action.ShouldAttack = true;
            }
            else if (distance > 500)
            {
                action.MovementType = MovementType.Close;
                action.MoveDistance = currentZoid.GetSpeed(_battleType) * 0.8; // 80% speed
                action.ShouldAttack = false;
            }
            else if (currentZoid.CanAttack(distance))
            {
                if (random.Next(0, 2) == 0)
                {
                    action.MovementType = MovementType.StandStill;
                    action.MoveDistance = 0;
                    action.AngleChange = 0;
                }
                else
                {
                    action.MovementType = MovementType.Circle;
                    action.AngleChange = random.Next(30, 90); // Random flanking angle
                }
                action.ShouldAttack = true;
            }
            else
            {
                action.MovementType = MovementType.Close;
                action.MoveDistance = currentZoid.GetSpeed(_battleType) * 0.6; // 60% speed
                action.ShouldAttack = false;
            }

            // Shield logic
            if (currentZoid.HasShield() && !currentZoid.ShieldOn && enemyZoid.CanAttack(distance))
            {
                action.ToggleShield = true;
            }

            // Stealth logic
            if (currentZoid.HasStealth() && !currentZoid.StealthOn && random.Next(0, 3) == 0)
            {
                action.ToggleStealth = true;
            }

            return action;
        }

        private void UpdateAttackControls()
        {
            if (_currentBattleZoid != null)
            {
                // Use projected distance if movement is selected, otherwise current distance
                double effectiveDistance = EnableMovementCheckBox.IsChecked == true ? _projectedDistance : _currentDistance;
                
                // Determine current range based on distance
                string currentRange = GetRangeName(effectiveDistance);
                string rangePrefix = EnableMovementCheckBox.IsChecked == true ? "After Movement: " : "Range: ";
                AttackRangeText.Text = $"{rangePrefix}{currentRange}";
                
                // Check if zoid can attack at effective distance
                bool canAttack = _currentBattleZoid.CanAttack(effectiveDistance);
                
                // Check for other attack restrictions
                bool isRestricted = _currentBattleZoid.ShieldOn && HasShield(_currentBattleZoid) ||
                                   _currentBattleZoid.Status == "stunned";
                
                EnableAttackCheckBox.IsEnabled = canAttack && !isRestricted;
                
                // Update status text
                if (!canAttack)
                {
                    AttackStatusText.Text = "No weapon for this range";
                }
                else if (isRestricted)
                {
                    AttackStatusText.Text = _currentBattleZoid.ShieldOn ? "Shield blocks attacks" : "Status prevents attack";
                }
                else
                {
                    string effectiveText = EnableMovementCheckBox.IsChecked == true ? " (after movement)" : "";
                    AttackStatusText.Text = $"Ready to attack{effectiveText}";
                }
            }
            else
            {
                AttackRangeText.Text = "Range: N/A";
                AttackStatusText.Text = "";
                EnableAttackCheckBox.IsEnabled = false;
            }
        }

        private string GetRangeName(double distance)
        {
            if (distance == 0) return "Melee (0m)";
            if (distance <= 500) return $"Close ({distance:F0}m)";
            if (distance <= 1000) return $"Mid ({distance:F0}m)";
            return $"Long ({distance:F0}m)";
        }

        private bool HasShield(Zoid zoid)
        {
            return zoid.ShieldRank > 0;
        }

        private void UpdateAttackRangeText()
        {
            // Simplified for new UI architecture
            if (_currentBattleZoid == null)
            {
                return;
            }

            // Attack range logic will be handled by the new action-based UI
        }

        private void UpdateBattleControls(Zoid currentZoid, bool isPlayer1)
        {
            _currentBattleZoid = currentZoid;
            _enemyBattleZoid = isPlayer1 ? _player2Zoid : _player1Zoid;
            
            // Update turn indicator
            CurrentTurnText.Text = isPlayer1 ? "Player 1's Turn" : "Player 2's Turn";
            
            // Reset action selections
            EnableMovementCheckBox.IsChecked = false;
            EnableAttackCheckBox.IsChecked = false;
            EnableShieldCheckBox.IsChecked = false;
            EnableStealthCheckBox.IsChecked = false;
            
            // Set default selections for movement combo box
            // Reset movement selection
            _selectedMovementType = MovementType.StandStill;
            _selectedMoveDistance = 0.0;

            // Update control states and displays
            UpdateControlStates();            // Clear action sequence
            ActionSequenceListBox.Items.Clear();
            
            // Show battle controls
            BattleControlsPanel.Visibility = Visibility.Visible;
        }

        private void UpdateZoidStatusDisplay()
        {
            if (_player1Zoid != null)
            {
                // Display dents directly instead of calculating health percentage
                Player1HealthText.Text = $"Dents: {_player1Zoid.Dents}/5";
                Player1HealthText.Foreground = _player1Zoid.Dents <= 1 ? System.Windows.Media.Brushes.Green : 
                                               _player1Zoid.Dents <= 3 ? System.Windows.Media.Brushes.Orange : 
                                               System.Windows.Media.Brushes.Red;
                
                Player1ShieldText.Text = $"Shield: {(_player1Zoid.ShieldOn ? "ON" : "OFF")} (Rank {_player1Zoid.ShieldRank})";
                Player1StealthText.Text = $"Stealth: {(_player1Zoid.StealthOn ? "ON" : "OFF")} (Rank {_player1Zoid.StealthRank})";
                Player1PositionText.Text = $"Status: {_player1Zoid.Status}";
                Player1DistanceText.Text = $"Distance: {_currentDistance:F0}m";
            }
            
            if (_player2Zoid != null)
            {
                // Display dents directly instead of calculating health percentage
                Player2HealthText.Text = $"Dents: {_player2Zoid.Dents}/5";
                Player2HealthText.Foreground = _player2Zoid.Dents <= 1 ? System.Windows.Media.Brushes.Green : 
                                               _player2Zoid.Dents <= 3 ? System.Windows.Media.Brushes.Orange : 
                                               System.Windows.Media.Brushes.Red;
                
                Player2ShieldText.Text = $"Shield: {(_player2Zoid.ShieldOn ? "ON" : "OFF")} (Rank {_player2Zoid.ShieldRank})";
                Player2StealthText.Text = $"Stealth: {(_player2Zoid.StealthOn ? "ON" : "OFF")} (Rank {_player2Zoid.StealthRank})";
                Player2PositionText.Text = $"Status: {_player2Zoid.Status}";
                Player2DistanceText.Text = $"Distance: {_currentDistance:F0}m";
            }
        }

        // Helper methods for filtering zoids
        private static IEnumerable<ZoidData> FilterZoids(IEnumerable<ZoidData> zoids, string battleType)
        {
            return zoids.Where(z => battleType switch
            {
                "land" => z.Movement.Land > 0,
                "water" => z.Movement.Water > 0,
                "air" => z.Movement.Air > 0,
                _ => false
            });
        }

        // Async callback implementations for the game engine
        private Task<string> GetBattleTypeAsync()
        {
            _battleTypeChoice = new TaskCompletionSource<string>();
            // The battle type is already determined from the UI
            _battleTypeChoice.SetResult(_battleType);
            return _battleTypeChoice.Task;
        }

        private Task<bool> GetOpponentTypeAsync()
        {
            _opponentTypeChoice = new TaskCompletionSource<bool>();
            // The opponent type is already determined from the UI
            _opponentTypeChoice.SetResult(_isAIMode);
            return _opponentTypeChoice.Task;
        }

        private Task<Zoid> GetZoidChoiceAsync(IEnumerable<ZoidData> availableZoids, bool isAIMode)
        {
            _zoidChoice = new TaskCompletionSource<Zoid>();
            
            if (isAIMode && !_isPlayer1Turn)
            {
                // AI's turn
                var aiZoid = SelectAIZoid();
                _zoidChoice.SetResult(aiZoid);
            }
            else
            {
                // PvP mode - use counter to determine which zoid to return
                _zoidChoiceCallCount++;
                
                if (_zoidChoiceCallCount == 1)
                {
                    // First call - return Player 1's zoid
                    if (_player1Zoid != null)
                        _zoidChoice.SetResult(_player1Zoid);
                    else
                    {
                        var fallback = new Zoid(availableZoids.First());
                        _zoidChoice.SetResult(fallback);
                    }
                }
                else
                {
                    // Second call - return Player 2's zoid
                    if (_player2Zoid != null)
                        _zoidChoice.SetResult(_player2Zoid);
                    else
                    {
                        var fallback = new Zoid(availableZoids.First());
                        _zoidChoice.SetResult(fallback);
                    }
                }
            }
            
            return _zoidChoice.Task;
        }

        private Task<double> GetStartingDistanceAsync()
        {
            _distanceChoice = new TaskCompletionSource<double>();
            
            // Use the user's selected starting distance from the setup screen
            AddBattleLogMessage($"Starting distance: {_userSelectedStartingDistance:F0} meters");
            _distanceChoice.SetResult(_userSelectedStartingDistance);
            
            return _distanceChoice.Task;
        }

        private Task<PlayerAction> GetPlayerActionAsync(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState)
        {
            _playerActionChoice = new TaskCompletionSource<PlayerAction>();
            _currentDistance = distance;
            
            // Update the battle state
            _waitingForPlayerAction = true;
            
            // Determine if this is AI mode and current player
            bool isPlayer1 = currentZoid == _player1Zoid;
            bool isAI = _isAIMode && !isPlayer1; // In AI mode, Player 2 is AI
            
            // Debug logging with object references
            string playerName = isPlayer1 ? "Player 1" : "Player 2";
            string modeText = isAI ? "AI" : "Human";
            
            AddBattleLogMessage($"DEBUG GetPlayerAction: currentZoid={currentZoid.ZoidName}, _player1Zoid={_player1Zoid?.ZoidName}, _player2Zoid={_player2Zoid?.ZoidName}");
            AddBattleLogMessage($"DEBUG GetPlayerAction: currentZoid==_player1Zoid? {currentZoid == _player1Zoid}, currentZoid==_player2Zoid? {currentZoid == _player2Zoid}");
            
            Dispatcher.Invoke(() =>
            {
                // Update distance and status displays first
                UpdateZoidStatusDisplay();
                
                // Enhanced debug message
                AddBattleLogMessage($"\n>>> {playerName} ({modeText}) turn starting <<<");
                AddBattleLogMessage($"DEBUG: _isAIMode={_isAIMode}, isPlayer1={isPlayer1}, isAI={isAI}");
                
                if (isAI)
                {
                    // AI action - generate automatically
                    AddBattleLogMessage("AI TURN: Hiding controls and generating automatic action...");
                    var aiAction = GenerateAutoAction(currentZoid, enemyZoid, distance);
                    AddBattleLogMessage($"AI performs action: {GetActionDescription(aiAction)}");
                    
                    // Track AI round for history
                    var aiRoundResult = new BattleRoundResult
                    {
                        RoundNumber = _currentTurn,
                        PlayerName = playerName,
                        IsAI = true,
                        ActionDescription = GetActionDescription(aiAction),
                        ResultDescription = "Processing...", // Will be updated after battle engine processes
                        DistanceAfter = distance,
                        ZoidStatusAfter = $"{currentZoid.ZoidName}: {currentZoid.Status}"
                    };
                    _battleHistory.Add(aiRoundResult);
                    UpdatePreviousRoundsDisplay();
                    
                    _playerActionChoice.SetResult(aiAction);
                    _waitingForPlayerAction = false;
                    
                    // Hide controls for AI turn
                    BattleControlsPanel.Visibility = Visibility.Collapsed;
                    AddBattleLogMessage("DEBUG: Controls hidden for AI turn");
                }
                else
                {
                    // Human player - show controls
                    AddBattleLogMessage("HUMAN TURN: Showing controls for player input...");
                    AddBattleLogMessage($"=== {playerName}'s Turn ===");
                    AddBattleLogMessage($"Distance: {distance:F0}m | Enemy Detected: {(enemyDetected ? "YES" : "NO")}");
                    AddBattleLogMessage(">>> SHOWING BATTLE CONTROLS - Choose your actions! <<<");
                    
                    // Show and update battle controls
                    UpdateBattleControls(currentZoid, isPlayer1);
                    BattleControlsPanel.Visibility = Visibility.Visible;
                    
                    AddBattleLogMessage("DEBUG: Controls should now be VISIBLE!");
                    AddBattleLogMessage("If you don't see controls, please report this issue!");
                }
            });
            
            // Increment turn counter for next round
            _currentTurn++;
            
            return _playerActionChoice.Task;
        }

        private string GetActionDescription(PlayerAction action)
        {
            var parts = new List<string>();
            
            // Movement description with details
            switch (action.MovementType)
            {
                case MovementType.Close:
                    parts.Add($"Close Distance ({action.MoveDistance:F0}m)");
                    break;
                case MovementType.Retreat:
                    parts.Add($"Retreat ({action.MoveDistance:F0}m)");
                    break;
                case MovementType.Circle:
                    parts.Add($"Circle Enemy ({action.AngleChange:F0}°)");
                    break;
                case MovementType.Search:
                    parts.Add($"Search ({action.MoveDistance:F0}m)");
                    break;
                case MovementType.StandStill:
                    parts.Add("Stand Still");
                    break;
                default:
                    parts.Add("No Movement");
                    break;
            }
            
            if (action.ShouldAttack) parts.Add("Attack");
            if (action.ToggleShield) parts.Add("Toggle Shield");
            if (action.ToggleStealth) parts.Add("Toggle Stealth");
            
            return string.Join(", ", parts);
        }

        private Task<bool> GetPlayAgainAsync()
        {
            _playAgainChoice = new TaskCompletionSource<bool>();
            
            // For now, return false to end the battle
            // In a full implementation, you'd show a dialog asking if they want to play again
            _playAgainChoice.SetResult(false);
            
            return _playAgainChoice.Task;
        }

        // Display methods
        private void DisplayZoidStatus(Zoid zoid, double distance)
        {
            AddBattleLogMessage($"DEBUG: DisplayZoidStatus called with distance={distance:F1}m, old _currentDistance={_currentDistance:F1}m");
            _currentDistance = distance;
            
            var status = $"{zoid.ZoidName} Status - Distance: {distance:F1}m, Shield: {(zoid.ShieldOn ? "ON" : "OFF")}, " +
                        $"Stealth: {(zoid.StealthOn ? "ON" : "OFF")}, Dents: {zoid.Dents}, Status: {zoid.Status}";
            AddBattleLogMessage(status);
            
            // Update the status display immediately
            Dispatcher.Invoke(() => {
                AddBattleLogMessage($"DEBUG: About to update UI with _currentDistance={_currentDistance:F1}m");
                UpdateZoidStatusDisplay();
                AddBattleLogMessage($"DEBUG: UI update completed");
            });
        }

        private void DisplayBattleStart(Zoid zoid1, Zoid zoid2)
        {
            AddBattleLogMessage($"BATTLE: {zoid1.ZoidName} vs {zoid2.ZoidName}");
        }

        private void DisplayTurnStart(Zoid currentZoid, int turnNumber)
        {
            AddBattleLogMessage($"--- Turn {turnNumber}: {currentZoid.ZoidName}'s turn ---");
        }

        private void DisplayBattleResult(Zoid winner, Zoid loser)
        {
            AddBattleLogMessage($"*** {winner.ZoidName} WINS! ***");
            AddBattleLogMessage($"{loser.ZoidName} has been defeated!");
        }
    }
}
