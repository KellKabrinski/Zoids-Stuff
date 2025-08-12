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
        private Zoid? _player1Zoid;
        private Zoid? _player2Zoid;
        private CharacterData _playerData = new CharacterData();

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

            // Switch to battle view
            ZoidSelectionPanel.Visibility = Visibility.Collapsed;
            BattlePanel.Visibility = Visibility.Visible;
            
            Player1ZoidText.Text = $"Player 1: {_player1Zoid.ZoidName} (PL {_player1Zoid.PowerLevel})";
            Player2ZoidText.Text = $"Player 2: {_player2Zoid.ZoidName} (PL {_player2Zoid.PowerLevel})";
            
            BattleLogTextBox.Clear();
            AddBattleLogMessage("=== BATTLE STARTING ===");
            AddBattleLogMessage($"Terrain: {_battleType.ToUpper()}");
            AddBattleLogMessage($"Mode: {(_isAIMode ? "Player vs AI" : "Player vs Player")}");
            
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
                        NewBattleButton.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddBattleLogMessage($"Error during battle: {ex.Message}");
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
            
            BattlePanel.Visibility = Visibility.Collapsed;
            SetupPanel.Visibility = Visibility.Visible;
            NewBattleButton.Visibility = Visibility.Collapsed;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
                // Player's turn - the zoid will be set when the player selects
                // For now, return the already selected zoid
                if (_isPlayer1Turn && _player1Zoid != null)
                    _zoidChoice.SetResult(_player1Zoid);
                else if (!_isPlayer1Turn && _player2Zoid != null)
                    _zoidChoice.SetResult(_player2Zoid);
                else
                {
                    // This shouldn't happen in our current flow, but provide a fallback
                    var firstZoid = new Zoid(availableZoids.First());
                    _zoidChoice.SetResult(firstZoid);
                }
            }
            
            return _zoidChoice.Task;
        }

        private Task<double> GetStartingDistanceAsync()
        {
            _distanceChoice = new TaskCompletionSource<double>();
            
            // For now, use a default distance. You could show a dialog here if needed.
            var defaultDistance = 1000.0;
            AddBattleLogMessage($"Starting distance: {defaultDistance} meters");
            _distanceChoice.SetResult(defaultDistance);
            
            return _distanceChoice.Task;
        }

        private Task<PlayerAction> GetPlayerActionAsync(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState)
        {
            _playerActionChoice = new TaskCompletionSource<PlayerAction>();
            
            // For now, implement a simple AI-like action for all players
            // In a full implementation, you'd show action selection UI here
            var action = new PlayerAction
            {
                MovementType = MovementType.StandStill,
                ShouldAttack = currentZoid.CanAttack(distance)
            };
            
            _playerActionChoice.SetResult(action);
            return _playerActionChoice.Task;
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
            var status = $"{zoid.ZoidName} Status - Distance: {distance:F1}m, Shield: {(zoid.ShieldOn ? "ON" : "OFF")}, " +
                        $"Stealth: {(zoid.StealthOn ? "ON" : "OFF")}, Dents: {zoid.Dents}, Status: {zoid.Status}";
            AddBattleLogMessage(status);
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
