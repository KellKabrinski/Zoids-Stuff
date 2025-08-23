using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
        private string? _currentSaveFile = null; // Track the current save file for auto-save
        
        // Battle state
        private double _currentDistance = 1000.0;
        private double _userSelectedStartingDistance = 1000.0; // User's choice from setup screen
        private int _currentTurn = 1;
        private bool _isBattleActive = false;
        private bool _waitingForPlayerAction = false;
        private bool _isDebugMode = false; // Track if debug mode is enabled
        private Zoid? _currentBattleZoid;
        private Zoid? _enemyBattleZoid;
        private bool _currentPlayerIsPlayer1 = true; // Track which player is currently active
        
        // Movement selection state
        private MovementType _selectedMovementType = MovementType.StandStill;
        private double _selectedMoveDistance = 0.0;
        private double _selectedAngleChange = 0.0;
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

        // Action tracking for battle history
        private PlayerAction? _lastExecutedAction;
        private Zoid? _lastActiveZoid;
        private bool _isTrackingActionResults = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadZoids();
            InitializeGameEngine();
            UpdateCharacterDisplay();
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
            
            // Capture debug mode setting
            _isDebugMode = DebugModeCheckBox.IsChecked == true;

            // For AI mode, offer to load a character save file
            if (_isAIMode)
            {
                var result = MessageBox.Show("Would you like to load a saved character with purchased Zoids and credits?", 
                                           "Load Character", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    LoadCharacterForAI();
                }
            }

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
            
            // Check if we have a loaded character with save file for Player 1 in AI mode
            if (_isAIMode && _isPlayer1Turn && !string.IsNullOrEmpty(_currentSaveFile))
            {
                ShowLoadedCharacterZoids();
            }
            else
            {
                // Normal mode - show all Zoids for purchase
                ZoidListGrid.ItemsSource = _allZoids;
                ZoidListGrid.SelectedItem = null;
                SelectZoidButton.IsEnabled = false;
                BuyZoidButton.IsEnabled = false;
                
                // Set button states to show "All Zoids" is selected
                ShowAllZoidsButton.Background = System.Windows.Media.Brushes.LightBlue;
                ShowOwnedZoidsButton.Background = System.Windows.Media.Brushes.LightGray;
            }
            
            ClearZoidDetails();
        }

        private void ShowLoadedCharacterZoids()
        {
            // For loaded characters, show owned Zoids + affordable Zoids from all available
            var availableZoids = new List<ZoidData>();
            
            // Add owned Zoids first
            foreach (var ownedZoid in _playerData.Zoids)
            {
                var zoidData = _allZoids.FirstOrDefault(z => z.Name == ownedZoid.ZoidName);
                if (zoidData != null)
                {
                    availableZoids.Add(zoidData);
                }
            }
            
            // Add affordable Zoids that aren't already owned
            var ownedZoidNames = _playerData.Zoids.Select(z => z.ZoidName).ToHashSet();
            foreach (var zoidData in _allZoids.Where(z => !ownedZoidNames.Contains(z.Name) && _playerData.credits >= z.Cost))
            {
                availableZoids.Add(zoidData);
            }
            
            ZoidListGrid.ItemsSource = availableZoids;
            ZoidListGrid.SelectedItem = null;
            SelectZoidButton.IsEnabled = false;
            BuyZoidButton.IsEnabled = false;
            
            // Set button states to show we're in loaded character mode
            ShowAllZoidsButton.Background = System.Windows.Media.Brushes.LightGray;
            ShowOwnedZoidsButton.Background = System.Windows.Media.Brushes.LightGray;
        }

        private void ZoidListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ZoidListGrid.SelectedItem is ZoidData selectedZoid)
            {
                SelectZoidButton.IsEnabled = true;
                ShowZoidDetails(selectedZoid);
                
                // Enable buy button only if showing all Zoids and player can afford it
                bool showingAllZoids = ZoidListGrid.ItemsSource == _allZoids;
                bool canAfford = _playerData.credits >= selectedZoid.Cost;
                BuyZoidButton.IsEnabled = showingAllZoids && canAfford;
            }
            else
            {
                SelectZoidButton.IsEnabled = false;
                BuyZoidButton.IsEnabled = false;
                ClearZoidDetails();
            }
        }

        private void SelectZoid_Click(object sender, RoutedEventArgs e)
        {
            if (ZoidListGrid.SelectedItem is ZoidData selectedZoidData)
            {
                // Check if this is a loaded character in AI mode and they don't own this Zoid
                bool needsToPurchase = false;
                if (_isAIMode && _isPlayer1Turn && !string.IsNullOrEmpty(_currentSaveFile))
                {
                    var ownedZoidNames = _playerData.Zoids.Select(z => z.ZoidName).ToHashSet();
                    needsToPurchase = !ownedZoidNames.Contains(selectedZoidData.Name);
                }
                
                // If they need to purchase, handle the purchase first
                if (needsToPurchase)
                {
                    if (_playerData.credits >= selectedZoidData.Cost)
                    {
                        var result = MessageBox.Show($"You don't own {selectedZoidData.Name}. Purchase it now for {selectedZoidData.Cost:N0} credits?", 
                                                   "Purchase Required", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            var newZoid = new Zoid(selectedZoidData);
                            _playerData.Zoids.Add(newZoid);
                            _playerData.credits -= (int)selectedZoidData.Cost;
                            UpdateCharacterDisplay();
                            AutoSave();
                        }
                        else
                        {
                            return; // User cancelled purchase
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Not enough credits! You need {selectedZoidData.Cost:N0} credits but only have {_playerData.credits:N0}.", 
                                      "Insufficient Credits", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
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
            
            // Apply debug mode setting - hide/show battle log
            if (_isDebugMode)
            {
                ShowBattleLogInDebugMode();
            }
            else
            {
                HideBattleLogInReleaseMode();
            }
            
            Player1ZoidText.Text = $"Player 1: {_player1Zoid.ZoidName} (PL {_player1Zoid.PowerLevel})";
            Player2ZoidText.Text = $"Player 2: {_player2Zoid.ZoidName} (PL {_player2Zoid.PowerLevel})";
            
            BattleLogTextBox.Clear();
            if (_isDebugMode)
            {
                AddBattleLogMessage("=== BATTLE STARTING ===");
                AddBattleLogMessage("*** BATTLE LOG IS VISIBLE - YOU SHOULD SEE THIS MESSAGE ***");
            }
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
                    // Use the pre-selected Zoids instead of letting GameEngine choose
                    var result = _gameEngine!.RunBattleWithSelectedZoids(
                        _player1Zoid!, 
                        _player2Zoid!, 
                        _battleType, 
                        _userSelectedStartingDistance, 
                        _isAIMode, 
                        _playerData);
                    _playerData = result.PlayerData;
                    
                    // Show new battle button after battle ends
                    Dispatcher.Invoke(() =>
                    {
                        _isBattleActive = false;
                        BattleControlsPanel.Visibility = Visibility.Collapsed;
                        NewBattleButton.Visibility = Visibility.Visible;
                        AddBattleLogMessage("\n=== BATTLE COMPLETE ===");
                        
                        // Auto-save after battle completion
                        AutoSave();
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
            // Only show battle log messages when debug mode is enabled
            if (!_isDebugMode)
                return;
                
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

        private void NewCharacter_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Create a new character? This will clear current character data.", 
                                       "New Character", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Prompt for character name
                string? characterName = PromptForCharacterName();
                if (string.IsNullOrEmpty(characterName))
                {
                    return; // User cancelled
                }

                _playerData = new CharacterData();
                _playerData.Name = characterName;
                _currentSaveFile = null; // Clear any loaded save file
                UpdateCharacterDisplay();
                StatusText.Text = $"New character '{characterName}' created";
            }
        }

        private void RenameCharacter_Click(object sender, RoutedEventArgs e)
        {
            string? newName = PromptForCharacterName(_playerData.Name);
            if (!string.IsNullOrEmpty(newName) && newName != _playerData.Name)
            {
                _playerData.Name = newName;
                UpdateCharacterDisplay();
                AutoSave(); // Auto-save if we have a save file
                StatusText.Text = $"Character renamed to '{newName}'";
            }
        }

        private void LoadCharacter_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Load Character",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _playerData = CharacterData.LoadFromFile(openFileDialog.FileName);
                    UpdateCharacterDisplay();
                    StatusText.Text = $"Loaded character: {_playerData.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading character: {ex.Message}", "Load Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to load character";
                }
            }
        }

        private void SaveCharacter_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Character",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "json",
                FileName = _playerData.Name.Replace(" ", "_") + ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _playerData.SaveToFile(saveFileDialog.FileName);
                    StatusText.Text = $"Saved character: {_playerData.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving character: {ex.Message}", "Save Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to save character";
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zoids Battle Game\n\nA turn-based battle simulation featuring Zoids mechs.\n\nFeatures:\n• Character save/load system\n• AI opponents\n• Strategic movement and combat\n• Customizable battle conditions", 
                          "About Zoids Battle Game", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadCharacterForAI()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Load Character for AI Battle",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "json",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _playerData = CharacterData.LoadFromFile(openFileDialog.FileName);
                    _currentSaveFile = openFileDialog.FileName;
                    UpdateCharacterDisplay();
                    MessageBox.Show($"Loaded character: {_playerData.Name}\nCredits: {_playerData.credits:N0}\nOwned Zoids: {_playerData.Zoids.Count}", 
                                  "Character Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading character: {ex.Message}", "Load Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateCharacterDisplay()
        {
            CharacterNameText.Text = $"Character: {_playerData.Name}";
            CharacterCreditsText.Text = $"Credits: {_playerData.credits:N0}";
        }

        private void AutoSave()
        {
            if (!string.IsNullOrEmpty(_currentSaveFile))
            {
                try
                {
                    _playerData.SaveToFile(_currentSaveFile);
                }
                catch (Exception ex)
                {
                    // Silent save failure - don't interrupt gameplay with error messages
                    // Could add logging here if needed
                    Console.WriteLine($"Auto-save failed: {ex.Message}");
                }
            }
        }

        private string? PromptForCharacterName(string defaultName = "Zoid Pilot")
        {
            // Create a simple input dialog
            var inputDialog = new Window
            {
                Title = "Character Name",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.Margin = new Thickness(20);

            var label = new Label
            {
                Content = "Enter your character name:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var textBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 15),
                Text = defaultName
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            inputDialog.Content = grid;

            string? result = null;
            okButton.Click += (s, e) => 
            {
                var name = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    result = name;
                    inputDialog.DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Please enter a character name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            cancelButton.Click += (s, e) => 
            {
                inputDialog.DialogResult = false;
            };

            textBox.SelectAll();
            textBox.Focus();

            return inputDialog.ShowDialog() == true ? result : null;
        }

        private void AddZoidToCollection(ZoidData zoidData)
        {
            // Create a new Zoid from the selected ZoidData
            var newZoid = new Zoid(zoidData);
            
            // Check if player can afford this Zoid
            if (_playerData.credits >= zoidData.Cost)
            {
                var result = MessageBox.Show($"Purchase {zoidData.Name} for {zoidData.Cost:N0} credits?", 
                                           "Purchase Zoid", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _playerData.Zoids.Add(newZoid);
                    _playerData.credits -= (int)zoidData.Cost;
                    UpdateCharacterDisplay();
                    StatusText.Text = $"Purchased {zoidData.Name}";
                    
                    // Auto-save after purchase
                    AutoSave();
                }
            }
            else
            {
                MessageBox.Show($"Not enough credits! You need {zoidData.Cost:N0} credits but only have {_playerData.credits:N0}.", 
                              "Insufficient Credits", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowOwnedZoids()
        {
            if (_playerData.Zoids.Count == 0)
            {
                MessageBox.Show("You don't own any Zoids yet. Purchase Zoids from the available list to add them to your collection.", 
                              "No Zoids Owned", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowAllZoidsButton.Background = System.Windows.Media.Brushes.LightBlue;
                ShowOwnedZoidsButton.Background = System.Windows.Media.Brushes.LightGray;
                ZoidListGrid.ItemsSource = _allZoids;
                StatusText.Text = "Showing all available Zoids";
                return;
            }

            var ownedZoidData = _playerData.Zoids.Select(z => new ZoidData
            {
                Name = z.ZoidName,
                PowerLevel = z.PowerLevel,
                Cost = z.Cost,
                Stats = new Stats
                {
                    Fighting = z.Fighting,
                    Strength = z.Strength,
                    Dexterity = z.Dexterity,
                    Agility = z.Agility,
                    Awareness = z.Awareness
                },
                Defenses = new Defenses
                {
                    Toughness = z.Toughness,
                    Parry = z.Parry,
                    Dodge = z.Dodge
                },
                Movement = new MovementStats
                {
                    Land = z.Land,
                    Water = z.Water,
                    Air = z.Air
                },
                Powers = z.Powers
            }).ToList();

            ShowAllZoidsButton.Background = System.Windows.Media.Brushes.LightGray;
            ShowOwnedZoidsButton.Background = System.Windows.Media.Brushes.LightBlue;
            ZoidListGrid.ItemsSource = ownedZoidData;
            StatusText.Text = $"Showing {_playerData.Zoids.Count} owned Zoids";
        }

        private void ShowAllZoids_Click(object sender, RoutedEventArgs e)
        {
            ShowAllZoidsButton.Background = System.Windows.Media.Brushes.LightBlue;
            ShowOwnedZoidsButton.Background = System.Windows.Media.Brushes.LightGray;
            
            // If we have a loaded character, show only owned and affordable Zoids
            if (!string.IsNullOrEmpty(_currentSaveFile))
            {
                ShowLoadedCharacterZoids();
                StatusText.Text = "Showing owned and affordable Zoids";
            }
            else
            {
                ZoidListGrid.ItemsSource = _allZoids;
                StatusText.Text = "Showing all available Zoids";
            }
        }

        private void ShowOwnedZoids_Click(object sender, RoutedEventArgs e)
        {
            ShowOwnedZoids();
        }

        private void BuyZoid_Click(object sender, RoutedEventArgs e)
        {
            if (ZoidListGrid.SelectedItem is ZoidData selectedZoid)
            {
                AddZoidToCollection(selectedZoid);
            }
        }

        private void ExecuteAction_Click(object sender, RoutedEventArgs e)
        {
            if (!_waitingForPlayerAction)
                return;

            var action = GetPlayerActionFromUI();
            
            // Set up action tracking for battle history
            _lastExecutedAction = action;
            _lastActiveZoid = _currentBattleZoid;
            _isTrackingActionResults = true;
            
            // Log the player's chosen actions
            bool isPlayer1 = _currentPlayerIsPlayer1; // Use stored player info instead of object comparison
            string playerName = isPlayer1 ? "Player 1" : "Player 2";
            AddBattleLogMessage($"{playerName} executes: {GetActionDescription(action)}");
            AddBattleLogMessage("Processing actions and calculating results...");
            
            // Track human player round for history
            var humanRoundResult = new BattleRoundResult
            {
                RoundNumber = _battleHistory.Count + 1, // Use sequential count for round numbers
                PlayerName = playerName,
                IsAI = false,
                ActionDescription = GetActionDescription(action),
                ResultDescription = "Processing...", // Will be updated after battle engine processes
                DistanceAfter = _currentDistance,
                ZoidStatusAfter = $"{_currentBattleZoid?.ZoidName ?? "Unknown"}: {_currentBattleZoid?.Status ?? "Unknown"}"
            };
            _battleHistory.Add(humanRoundResult);
            UpdatePreviousRoundsDisplay();
            
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

                // Set angle change for circle movement (including direction)
                if (action.MovementType == MovementType.Circle)
                {
                    action.AngleChange = _selectedAngleChange;
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
                _selectedAngleChange = speedDialog.SelectedAngleChange;
                
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
                _selectedAngleChange = 0.0;
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
                _selectedAngleChange = 0.0;
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
                action.ActionSequence.Add(ActionType.Attack);
            }
            else if (distance > 500)
            {
                action.MovementType = MovementType.Close;
                action.MoveDistance = currentZoid.GetSpeed(_battleType) * 0.8; // 80% speed
                action.ShouldAttack = false;
                action.ActionSequence.Add(ActionType.Move);
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
                    action.ActionSequence.Add(ActionType.Move);
                }
                action.ShouldAttack = true;
                action.ActionSequence.Add(ActionType.Attack);
            }
            else
            {
                action.MovementType = MovementType.Close;
                action.MoveDistance = currentZoid.GetSpeed(_battleType) * 0.6; // 60% speed
                action.ShouldAttack = false;
                action.ActionSequence.Add(ActionType.Move);
            }

            // Shield logic
            if (currentZoid.HasShield() && !currentZoid.ShieldOn && enemyZoid.CanAttack(distance))
            {
                action.ToggleShield = true;
                action.ActionSequence.Add(ActionType.Shield);
            }

            // Stealth logic
            if (currentZoid.HasStealth() && !currentZoid.StealthOn && random.Next(0, 3) == 0)
            {
                action.ToggleStealth = true;
                action.ActionSequence.Add(ActionType.Stealth);
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
            _selectedAngleChange = 0.0;

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
            // Use the GameEngine's CurrentPlayer field which contains the exact player number
            bool isPlayer1 = (gameState.CurrentPlayer == 1);
            bool isAI = _isAIMode && !isPlayer1; // In AI mode, Player 2 is AI
            
            // Store current player for use in other methods
            _currentPlayerIsPlayer1 = isPlayer1;
            
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
                    
                    // Debug: Show detailed AI action information
                    AddBattleLogMessage($"DEBUG AI ACTION: MovementType={aiAction.MovementType}, MoveDistance={aiAction.MoveDistance:F1}, Actions=[{string.Join(", ", aiAction.ActionSequence)}]");
                    
                    // Set up action tracking for battle history (same as human players)
                    _lastExecutedAction = aiAction;
                    _lastActiveZoid = currentZoid;
                    _isTrackingActionResults = true;
                    
                    // Track AI round for history
                    var aiRoundResult = new BattleRoundResult
                    {
                        RoundNumber = _battleHistory.Count + 1, // Use sequential count for round numbers
                        PlayerName = playerName,
                        IsAI = true,
                        ActionDescription = GetActionDescription(aiAction),
                        ResultDescription = "Processing...", // Will be updated after battle engine processes
                        DistanceAfter = distance,
                        ZoidStatusAfter = $"{currentZoid.ZoidName}: {currentZoid.Status}"
                    };
                    _battleHistory.Add(aiRoundResult);
                    UpdatePreviousRoundsDisplay();
                    
                    // Hide controls for AI turn
                    BattleControlsPanel.Visibility = Visibility.Collapsed;
                    AddBattleLogMessage("DEBUG: Controls hidden for AI turn");
                    
                    // Add a small delay to prevent potential UI thread issues, then complete the action
                    Task.Delay(1000).ContinueWith(_ => {
                        Dispatcher.Invoke(() => {
                            _playerActionChoice.SetResult(aiAction);
                            _waitingForPlayerAction = false;
                        });
                    });
                }
                else
                {
                    // Human player - show controls
                    AddBattleLogMessage("HUMAN TURN: Showing controls for player input...");
                    AddBattleLogMessage($"=== {playerName}'s Turn ===");
                    AddBattleLogMessage($"Distance: {distance:F0}m | Enemy Detected: {(enemyDetected ? "YES" : "NO")}");
                    AddBattleLogMessage(">>> SHOWING BATTLE CONTROLS - Choose your actions! <<<");
                    AddBattleLogMessage($"DEBUG: About to call UpdateBattleControls for {playerName} (isPlayer1={isPlayer1})");
                    
                    // Show and update battle controls
                    UpdateBattleControls(currentZoid, isPlayer1);
                    // Note: UpdateBattleControls already sets BattleControlsPanel.Visibility = Visibility.Visible;
                    
                    AddBattleLogMessage("DEBUG: Controls should now be VISIBLE!");
                    AddBattleLogMessage("If you don't see controls, please report this issue!");
                }
            });
            
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
            
            // Update battle history with action results if we're tracking
            if (_isTrackingActionResults && _lastExecutedAction != null && _lastActiveZoid != null)
            {
                var enemyZoid = _lastActiveZoid == _player1Zoid ? _player2Zoid : _player1Zoid;
                if (enemyZoid != null)
                {
                    // Ensure battle history update happens on UI thread
                    Dispatcher.Invoke(() => {
                        UpdateBattleHistoryWithResults(_lastActiveZoid, enemyZoid, distance, _lastExecutedAction);
                    });
                }
                _isTrackingActionResults = false;
                _lastExecutedAction = null;
                _lastActiveZoid = null;
            }
            
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

        private void UpdateBattleHistoryWithResults(Zoid activeZoid, Zoid enemyZoid, double newDistance, PlayerAction action)
        {
            if (_battleHistory.Any())
            {
                var lastEntry = _battleHistory.Last();
                var results = new List<string>();
                
                // Calculate distance at time of attack
                // For simplicity and correctness: attacks always use the final distance after all movement
                double attackDistance = newDistance;
                
                // Movement results
                if (action.ActionSequence.Contains(ActionType.Move))
                {
                    var distanceChange = newDistance - lastEntry.DistanceAfter;
                    string movementResult = action.MovementType switch
                    {
                        MovementType.Close => $"Moved closer by {Math.Abs(distanceChange):F0}m",
                        MovementType.Retreat => $"Retreated by {Math.Abs(distanceChange):F0}m",
                        MovementType.Circle => "Circled around enemy",
                        MovementType.Search => "Searched the area",
                        _ => "Moved"
                    };
                    results.Add(movementResult);
                }
                else
                {
                    results.Add("Remained in position");
                }
                
                // Shield/Stealth results
                if (action.ActionSequence.Contains(ActionType.Shield))
                {
                    results.Add($"Shield {(activeZoid.ShieldOn ? "activated" : "deactivated")}");
                }
                if (action.ActionSequence.Contains(ActionType.Stealth))
                {
                    results.Add($"Stealth {(activeZoid.StealthOn ? "activated" : "deactivated")}");
                }
                
                // Attack results - use the correct distance for range calculation
                if (action.ActionSequence.Contains(ActionType.Attack))
                {
                    var range = GetRange(attackDistance);
                    if (enemyZoid.Dents > 0)
                    {
                        results.Add($"{range} attack hit! Enemy takes damage (Dents: {enemyZoid.Dents}/5)");
                    }
                    else
                    {
                        results.Add($"{range} attack missed");
                    }
                }
                
                // Update the last entry with results
                lastEntry.ResultDescription = string.Join("; ", results);
                lastEntry.DistanceAfter = newDistance;
                lastEntry.ZoidStatusAfter = $"{activeZoid.ZoidName}: Dents {activeZoid.Dents}/5, {activeZoid.Status}";
                
                UpdatePreviousRoundsDisplay();
            }
        }

        private Ranges GetRange(double distance)
        {
            if (distance == 0) return Ranges.Melee;
            if (distance <= 500) return Ranges.Close;
            if (distance <= 1000) return Ranges.Mid;
            return Ranges.Long;
        }

        private void DisplayBattleResult(Zoid winner, Zoid loser)
        {
            AddBattleLogMessage($"*** {winner.ZoidName} WINS! ***");
            AddBattleLogMessage($"{loser.ZoidName} has been defeated!");
        }

        private void HideBattleLogInReleaseMode()
        {
            // Hide the battle log GroupBox
            if (BattleLogGroupBox != null)
            {
                BattleLogGroupBox.Visibility = Visibility.Collapsed;
            }
            
            // Adjust the grid columns to give all space to the Previous Rounds panel
            if (BattleLogGrid != null && BattleLogGrid.ColumnDefinitions.Count >= 2)
            {
                BattleLogGrid.ColumnDefinitions[0].Width = new GridLength(0); // Hide battle log column
                BattleLogGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star); // Give all space to previous rounds
            }
        }
        
        private void ShowBattleLogInDebugMode()
        {
            // Show the battle log GroupBox
            if (BattleLogGroupBox != null)
            {
                BattleLogGroupBox.Visibility = Visibility.Visible;
            }
            
            // Restore the original grid column layout
            if (BattleLogGrid != null && BattleLogGrid.ColumnDefinitions.Count >= 2)
            {
                BattleLogGrid.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star); // Battle log column
                BattleLogGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star); // Previous rounds column
            }
        }
    }
}
