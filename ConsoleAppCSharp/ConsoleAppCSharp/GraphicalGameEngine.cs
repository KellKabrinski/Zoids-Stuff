using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ZoidsBattle
{
    /// <summary>
    /// Example of how to implement a graphical version of the game engine
    /// This would be used in a WPF, WinForms, or other GUI application
    /// </summary>
    public class GraphicalGameEngine : GameEngine
    {
        // These would be references to your UI controls
        private Action<string> _displayMessageCallback;
        private Func<string[], int> _showChoiceDialogCallback;
        private Func<string, double> _getNumericInputCallback;
        private Func<bool> _askYesNoCallback;

        public GraphicalGameEngine(
            Action<string> displayMessage,
            Func<string[], int> showChoiceDialog,
            Func<string, double> getNumericInput,
            Func<bool> askYesNo)
        {
            _displayMessageCallback = displayMessage;
            _showChoiceDialogCallback = showChoiceDialog;
            _getNumericInputCallback = getNumericInput;
            _askYesNoCallback = askYesNo;
        }

        public override string ChooseBattleType()
        {
            var choices = new[] { "Land", "Water", "Air" };
            int choice = _showChoiceDialogCallback(choices);
            return choice switch
            {
                0 => "land",
                1 => "water",
                2 => "air",
                _ => "land"
            };
        }

        public override bool ChooseOpponentType()
        {
            var choices = new[] { "Player vs Player", "Player vs AI" };
            int choice = _showChoiceDialogCallback(choices);
            return choice == 1; // true for AI, false for PvP
        }

        public override Zoid ChoosePlayerZoid(IEnumerable<ZoidData> availableZoids, CharacterData playerData)
        {
            // Load character data
            string saveFile = "save1.json";
            CharacterData currentPlayerData = LoadOrCreateCharacterData(saveFile, playerData);

            if (currentPlayerData.Zoids.Count > 0)
            {
                // Show zoid selection dialog
                var zoidChoices = currentPlayerData.Zoids
                    .Select(z => $"{z.ZoidName} (PL {z.PowerLevel})")
                    .Concat(new[] { "Buy a new Zoid" })
                    .ToArray();

                int choice = _showChoiceDialogCallback(zoidChoices);

                if (choice == currentPlayerData.Zoids.Count) // Buy new zoid
                {
                    var newZoid = ShowZoidPurchaseDialog(availableZoids, currentPlayerData.credits);
                    if (newZoid != null)
                    {
                        currentPlayerData.Zoids.Add(newZoid);
                        currentPlayerData.credits -= newZoid.Cost;
                        SaveCharacterData(currentPlayerData, saveFile);
                        
                        // Update reference
                        UpdatePlayerDataReference(playerData, currentPlayerData);
                        return newZoid;
                    }
                    return currentPlayerData.Zoids[0]; // Fallback
                }
                else
                {
                    return currentPlayerData.Zoids[choice];
                }
            }
            else
            {
                _displayMessageCallback("You have no Zoids. Please buy a new one.");
                var newZoid = ShowZoidPurchaseDialog(availableZoids, currentPlayerData.credits);
                if (newZoid != null)
                {
                    currentPlayerData.Zoids.Add(newZoid);
                    currentPlayerData.credits -= newZoid.Cost;
                    SaveCharacterData(currentPlayerData, saveFile);
                    
                    // Update reference
                    UpdatePlayerDataReference(playerData, currentPlayerData);
                    return newZoid;
                }
                
                // This shouldn't happen in a real implementation
                throw new InvalidOperationException("No zoid selected");
            }
        }

        public override double GetStartingDistance()
        {
            return _getNumericInputCallback("Enter starting distance between Zoids in meters:");
        }

        public override PlayerAction GetPlayerAction(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState)
        {
            var action = new PlayerAction();

            if (currentZoid.Status == "stunned")
            {
                _displayMessageCallback("You are STUNNED! You cannot move or attack this turn.");
                ShowShieldAndStealthDialog(currentZoid, action);
                return action;
            }

            if (currentZoid.Status == "dazed")
            {
                _displayMessageCallback("You are DAZED! You may move OR attack, not both.");
                var choices = new[] { "Move", "Attack", "Skip" };
                int choice = _showChoiceDialogCallback(choices);
                
                if (choice == 0) // Move
                {
                    ShowMovementDialog(currentZoid, enemyZoid, distance, enemyDetected, gameState, action);
                }
                else if (choice == 1) // Attack
                {
                    action.ShouldAttack = true;
                }
                
                ShowShieldAndStealthDialog(currentZoid, action);
                return action;
            }

            // Normal turn
            var orderChoices = new[] { "Move first", "Attack first" };
            int orderChoice = _showChoiceDialogCallback(orderChoices);

            ShowShieldAndStealthDialog(currentZoid, action);

            if (orderChoice == 0) // Move first
            {
                ShowMovementDialog(currentZoid, enemyZoid, distance, enemyDetected, gameState, action);
                
                if (currentZoid.CanAttack(distance))
                {
                    action.ShouldAttack = _askYesNoCallback();
                }
                else
                {
                    _displayMessageCallback($"{currentZoid.ZoidName} cannot attack from this range!");
                }
            }
            else // Attack first
            {
                if (currentZoid.CanAttack(distance))
                {
                    action.ShouldAttack = _askYesNoCallback();
                }
                else
                {
                    _displayMessageCallback($"{currentZoid.ZoidName} cannot attack from this range!");
                }
                
                ShowMovementDialog(currentZoid, enemyZoid, distance, enemyDetected, gameState, action);
            }

            return action;
        }

        public override void DisplayMessage(string message)
        {
            _displayMessageCallback(message);
        }

        public override void DisplayZoidStatus(Zoid zoid, double distance)
        {
            var status = $"{zoid.ZoidName}'s status: Distance={distance:F1}, " +
                        $"Shield={(zoid.ShieldOn ? "ON" : "OFF")} (Rank={zoid.ShieldRank}), " +
                        $"Stealth={(zoid.StealthOn ? "ON" : "OFF")} (Rank={zoid.StealthRank}), " +
                        $"Dents={zoid.Dents}, Status={zoid.Status}";
            _displayMessageCallback(status);
        }

        public override void DisplayBattleStart(Zoid zoid1, Zoid zoid2)
        {
            _displayMessageCallback($"Player 1: {zoid1.ZoidName} vs Player 2: {zoid2.ZoidName}");
        }

        public override void DisplayTurnStart(Zoid currentZoid, int turnNumber)
        {
            _displayMessageCallback($"{currentZoid.ZoidName}'s turn!");
        }

        public override void DisplayBattleResult(Zoid winner, Zoid loser)
        {
            _displayMessageCallback($"{winner.ZoidName} wins the battle!");
        }

        public override bool AskPlayAgain()
        {
            // This could show a message box asking if the player wants to play again
            return _askYesNoCallback();
        }

        // Helper methods for the graphical implementation
        private CharacterData LoadOrCreateCharacterData(string saveFile, CharacterData fallback)
        {
            try
            {
                if (System.IO.File.Exists(saveFile))
                {
                    var loaded = CharacterData.LoadFromFile(saveFile);
                    _displayMessageCallback("Loaded character save data");
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                _displayMessageCallback($"Error loading character data: {ex.Message}");
            }
            
            var newData = new CharacterData();
            _displayMessageCallback("Created new character data");
            SaveCharacterData(newData, saveFile);
            return newData;
        }

        private void SaveCharacterData(CharacterData data, string saveFile)
        {
            try
            {
                data.SaveToFile(saveFile);
            }
            catch (Exception ex)
            {
                _displayMessageCallback($"Error saving character data: {ex.Message}");
            }
        }

        private void UpdatePlayerDataReference(CharacterData target, CharacterData source)
        {
            target.Name = source.Name;
            target.Zoids = source.Zoids;
            target.credits = source.credits;
        }

        private Zoid? ShowZoidPurchaseDialog(IEnumerable<ZoidData> availableZoids, int credits)
        {
            var affordable = availableZoids
                .Where(z => z.Cost <= credits)
                .OrderBy(z => z.PowerLevel)
                .ThenBy(z => z.Name)
                .ToList();

            if (!affordable.Any())
            {
                _displayMessageCallback("You cannot afford any Zoids!");
                return null;
            }

            var choices = affordable
                .Select(z => $"{z.Name} (PL {z.PowerLevel}) - ${z.Cost}")
                .ToArray();

            int choice = _showChoiceDialogCallback(choices);
            return new Zoid(affordable[choice]);
        }

        private void ShowMovementDialog(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState, PlayerAction action)
        {
            int speed = GetSpeed(currentZoid, gameState.BattleType);

            if (!enemyDetected)
            {
                var choices = new[] { "Search for Enemy", "Stand Still" };
                int choice = _showChoiceDialogCallback(choices);
                action.MovementType = choice == 0 ? MovementType.Search : MovementType.StandStill;
                return;
            }

            var movementChoices = new[] { "Close", "Retreat", "Circle Left", "Circle Right", "Stand Still" };
            int movementChoice = _showChoiceDialogCallback(movementChoices);

            switch (movementChoice)
            {
                case 0: // Close
                    action.MovementType = MovementType.Close;
                    action.MoveDistance = Math.Min(speed, _getNumericInputCallback($"Enter distance to move (0 to {speed}):"));
                    break;
                case 1: // Retreat
                    action.MovementType = MovementType.Retreat;
                    action.MoveDistance = Math.Min(speed, _getNumericInputCallback($"Enter distance to move (0 to {speed}):"));
                    break;
                case 2: // Circle Left
                case 3: // Circle Right
                    action.MovementType = MovementType.Circle;
                    double maxAngle = MaxCirclingAngle(speed, distance);
                    double angleChange = Math.Min(maxAngle, _getNumericInputCallback($"Enter degrees to circle (0 to {maxAngle:F1}):"));
                    action.AngleChange = movementChoice == 2 ? angleChange : -angleChange;
                    break;
                case 4: // Stand Still
                    action.MovementType = MovementType.StandStill;
                    break;
            }
        }

        private void ShowShieldAndStealthDialog(Zoid zoid, PlayerAction action)
        {
            if (zoid.HasShield())
            {
                _displayMessageCallback($"Shield is currently {(zoid.ShieldOn ? "ON" : "OFF")}");
                if (_askYesNoCallback())
                {
                    action.ToggleShield = true;
                }
            }

            if (zoid.HasStealth())
            {
                _displayMessageCallback($"Stealth is currently {(zoid.StealthOn ? "ON" : "OFF")}");
                if (_askYesNoCallback())
                {
                    action.ToggleStealth = true;
                }
            }
        }

        private static double MaxCirclingAngle(int speed, double distance)
        {
            if (distance <= 0.1) return 360;
            return Math.Min(360, (speed * 180.0) / (Math.PI * distance));
        }
    }
}
