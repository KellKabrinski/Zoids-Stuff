using System;
using System.Collections.Generic;
using System.Linq;

namespace ZoidsBattle
{
    /// <summary>
    /// Example of how to integrate the GraphicalGameEngine with a UI application
    /// This shows the pattern for creating UI callbacks without actual WPF dependencies
    /// </summary>
    public class UIGameExample
    {
        private GraphicalGameEngine _gameEngine = null!;
        private List<ZoidData> _availableZoids = null!;
        private CharacterData _playerData = null!;

        public UIGameExample()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Load Zoids data (you would implement LoadZoids based on your data source)
            _availableZoids = LoadZoids("ConvertedZoidStats.json");
            _playerData = new CharacterData();

            // Create the game engine with UI callbacks
            _gameEngine = new GraphicalGameEngine(
                displayMessage: DisplayMessage,
                showChoiceDialog: ShowChoiceDialog,
                getNumericInput: GetNumericInput,
                askYesNo: AskYesNo
            );
        }

        public void StartBattle()
        {
            try
            {
                var result = _gameEngine.RunBattle(_availableZoids, _playerData);
                _playerData = result.PlayerData;
                
                // Save updated player data
                _playerData.SaveToFile("save1.json");
                
                // Ask if player wants to play again
                if (_gameEngine.AskPlayAgain())
                {
                    // Start another battle
                    StartBattle();
                }
            }
            catch (Exception ex)
            {
                DisplayMessage($"Error during battle: {ex.Message}");
            }
        }

        // UI Callback implementations - these would be replaced with actual UI calls
        private void DisplayMessage(string message)
        {
            // In a real implementation, this would update your UI
            // For example: LogTextBox.AppendText(message + Environment.NewLine);
            // Or: battleLogList.Add(new BattleLogEntry(message));
            // Or: await DisplayAlert("Battle Log", message, "OK");
            Console.WriteLine("[UI MESSAGE] " + message);
        }

        private int ShowChoiceDialog(string[] choices)
        {
            // In a real implementation, this would show a dialog or choice UI
            // For example: return await DisplayActionSheet("Choose", "Cancel", null, choices);
            // Or: return choiceDialog.ShowDialog(choices);
            
            Console.WriteLine("[UI CHOICE] Choose from:");
            for (int i = 0; i < choices.Length; i++)
            {
                Console.WriteLine($"  {i + 1}: {choices[i]}");
            }
            
            // For demo purposes, just return first choice
            // In real implementation, this would wait for user input
            return 0;
        }

        private double GetNumericInput(string prompt)
        {
            // In a real implementation, this would show an input dialog
            // For example: return await DisplayPromptAsync("Input", prompt);
            // Or: return numericInputDialog.ShowDialog(prompt);
            
            Console.WriteLine($"[UI INPUT] {prompt}");
            
            // For demo purposes, return a default value
            // In real implementation, this would wait for user input
            return 1000.0; // Default distance
        }

        private bool AskYesNo()
        {
            // In a real implementation, this would show a confirmation dialog
            // For example: return await DisplayAlert("Confirm", "Continue?", "Yes", "No");
            // Or: return MessageBox.Show("Continue?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes;
            
            Console.WriteLine("[UI CONFIRM] Continue? (assuming Yes for demo)");
            
            // For demo purposes, return true
            // In real implementation, this would wait for user input
            return true;
        }

        private List<ZoidData> LoadZoids(string path)
        {
            try
            {
                var json = System.IO.File.ReadAllText(path);
                return System.Text.Json.JsonSerializer.Deserialize<List<ZoidData>>(json) ?? new List<ZoidData>();
            }
            catch
            {
                // Return empty list if file doesn't exist or can't be loaded
                return new List<ZoidData>();
            }
        }
    }

    /// <summary>
    /// Example showing how different UI frameworks could implement the callbacks
    /// </summary>
    public static class UIFrameworkExamples
    {
        /// <summary>
        /// Example for WPF implementation
        /// </summary>
        public static class WPFExample
        {
            // In a WPF application, you might implement the callbacks like this:
            /*
            private void DisplayMessage(string message)
            {
                Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(message + Environment.NewLine);
                    LogTextBox.ScrollToEnd();
                });
            }

            private int ShowChoiceDialog(string[] choices)
            {
                int result = -1;
                Dispatcher.Invoke(() =>
                {
                    var dialog = new ChoiceDialog("Select an option:", choices);
                    if (dialog.ShowDialog() == true)
                    {
                        result = dialog.SelectedIndex;
                    }
                });
                return Math.Max(0, result);
            }

            private double GetNumericInput(string prompt)
            {
                double result = 0;
                Dispatcher.Invoke(() =>
                {
                    var dialog = new NumericInputDialog(prompt);
                    if (dialog.ShowDialog() == true)
                    {
                        result = dialog.Value;
                    }
                });
                return result;
            }

            private bool AskYesNo()
            {
                bool result = false;
                Dispatcher.Invoke(() =>
                {
                    var msgResult = MessageBox.Show("Continue?", "Confirmation", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    result = msgResult == MessageBoxResult.Yes;
                });
                return result;
            }
            */
        }

        /// <summary>
        /// Example for WinForms implementation
        /// </summary>
        public static class WinFormsExample
        {
            // In a WinForms application, you might implement the callbacks like this:
            /*
            private void DisplayMessage(string message)
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => DisplayMessage(message)));
                    return;
                }
                logTextBox.AppendText(message + Environment.NewLine);
            }

            private int ShowChoiceDialog(string[] choices)
            {
                using var dialog = new ChoiceForm("Select an option:", choices);
                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedIndex : 0;
            }

            private double GetNumericInput(string prompt)
            {
                using var dialog = new NumericInputForm(prompt);
                return dialog.ShowDialog() == DialogResult.OK ? dialog.Value : 0;
            }

            private bool AskYesNo()
            {
                return MessageBox.Show("Continue?", "Confirmation", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            }
            */
        }

        /// <summary>
        /// Example for Xamarin.Forms/MAUI implementation
        /// </summary>
        public static class XamarinExample
        {
            // In a Xamarin.Forms/MAUI application, you might implement the callbacks like this:
            /*
            private void DisplayMessage(string message)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    battleLogList.Add(new BattleLogEntry { Message = message, Timestamp = DateTime.Now });
                });
            }

            private async Task<int> ShowChoiceDialog(string[] choices)
            {
                var result = await DisplayActionSheet("Choose", "Cancel", null, choices);
                return Array.IndexOf(choices, result);
            }

            private async Task<double> GetNumericInput(string prompt)
            {
                var result = await DisplayPromptAsync("Input", prompt, keyboard: Keyboard.Numeric);
                return double.TryParse(result, out var value) ? value : 0;
            }

            private async Task<bool> AskYesNo()
            {
                return await DisplayAlert("Confirm", "Continue?", "Yes", "No");
            }
            */
        }
    }
}
