using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZoidsBattle
{
    /// <summary>
    /// Console-based implementation of the Zoids battle game engine
    /// </summary>
    public class ConsoleGameEngine : GameEngine
    {
        public override string ChooseBattleType()
        {
            Console.WriteLine("\nChoose battle type:");
            Console.WriteLine("1: Land");
            Console.WriteLine("2: Water");
            Console.WriteLine("3: Air");
            while (true)
            {
                Console.Write("Battle type: ");
                var choice = Console.ReadLine();
                if (choice == "1") return "land";
                if (choice == "2") return "water";
                if (choice == "3") return "air";
                Console.WriteLine("Invalid input. Try again.");
            }
        }

        public override bool ChooseOpponentType()
        {
            Console.WriteLine("\nChoose opponent type:");
            Console.WriteLine("1: Player vs Player");
            Console.WriteLine("2: Player vs AI");
            while (true)
            {
                Console.Write("Enter choice: ");
                var input = Console.ReadLine();
                if (input == "1") return false; // PvP
                if (input == "2") return true;  // AI
                Console.WriteLine("Invalid input. Try again.");
            }
        }

        public override Zoid ChoosePlayerZoid(IEnumerable<ZoidData> availableZoids, CharacterData playerData)
        {
            // Handle save/load logic for AI mode
            string saveFile = "save1.json";
            CharacterData currentPlayerData = playerData;
            
            if (File.Exists(saveFile))
            {
                try
                {
                    currentPlayerData = CharacterData.LoadFromFile(saveFile);
                    Console.WriteLine("Loaded character save data");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading character data: {ex.Message}");
                    currentPlayerData = CreateNewCharacter(saveFile);
                }
            }
            else
            {
                currentPlayerData = CreateNewCharacter(saveFile);
            }

            // Copy updated data back to the parameter
            playerData.Name = currentPlayerData.Name;
            playerData.Zoids = currentPlayerData.Zoids;
            playerData.credits = currentPlayerData.credits;
            var zoidList = availableZoids.ToList();
            bool canBuyZoid = GetAffordableZoids(currentPlayerData.credits, false, zoidList).Count > 0;

            if (currentPlayerData.Zoids.Count > 0)
            {
                Console.WriteLine($"\nCurrent credits: {currentPlayerData.credits}");
                Console.WriteLine($"\nSelect a Zoid to use{(canBuyZoid ? " or buy a new one" : "")}:");
                for (int i = 0; i < currentPlayerData.Zoids.Count; i++)
                {
                    Console.WriteLine($"{i + 1}: {currentPlayerData.Zoids[i].ZoidName} (PL {currentPlayerData.Zoids[i].PowerLevel})");
                }
                if (canBuyZoid)
                {
                    Console.WriteLine($"{currentPlayerData.Zoids.Count + 1}: Buy a new Zoid");
                }

                int choice;
                int max = canBuyZoid ? currentPlayerData.Zoids.Count + 1 : currentPlayerData.Zoids.Count;
                while (true)
                {
                    Console.Write("Enter your choice: ");
                    if (int.TryParse(Console.ReadLine(), out choice) && choice >= 1 && choice <= max)
                        break;
                    Console.WriteLine("Invalid input. Try again.");
                }

                if (choice == currentPlayerData.Zoids.Count + 1)
                {
                    var newZoid = ChooseZoidFromList(availableZoids, 1, currentPlayerData.credits);
                    currentPlayerData.Zoids.Add(newZoid);
                    currentPlayerData.credits -= newZoid.Cost;
                    currentPlayerData.SaveToFile(saveFile);

                    // Update the reference
                    playerData.Zoids = currentPlayerData.Zoids;
                    playerData.credits = currentPlayerData.credits;

                    return newZoid;
                }
                else
                {
                    return currentPlayerData.Zoids[choice - 1];
                }
            }
            else
            {
                Console.WriteLine("You have no Zoids. Please buy a new one.");
                var newZoid = ChooseZoidFromList(availableZoids, 1, currentPlayerData.credits);
                currentPlayerData.Zoids.Add(newZoid);
                currentPlayerData.credits -= newZoid.Cost;
                currentPlayerData.SaveToFile(saveFile);

                // Update the reference
                playerData.Zoids = currentPlayerData.Zoids;
                playerData.credits = currentPlayerData.credits;

                return newZoid;
            }
        }

        public override double GetStartingDistance()
        {
            while (true)
            {
                Console.Write("\nEnter starting distance between Zoids in meters: ");
                if (double.TryParse(Console.ReadLine(), out double dist) && dist >= 0)
                    return dist;
                Console.WriteLine("Invalid input. Try again.");
            }
        }

        public override PlayerAction GetPlayerAction(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState)
        {
            var action = new PlayerAction();

            if (currentZoid.Status == "stunned")
            {
                Console.WriteLine("You are STUNNED! You cannot move or attack this turn.");
                HandleShieldAndStealthInput(currentZoid, action);
                return action;
            }

            if (currentZoid.Status == "dazed")
            {
                Console.WriteLine("You are DAZED! You may move OR attack, not both.");
                Console.Write("  Move (m) or Attack (a) or Skip (s)? ");
                string? choice = Console.ReadLine();
                
                if (choice?.ToLower().StartsWith('m') == true)
                {
                    GetMovementAction(currentZoid, enemyZoid, distance, enemyDetected, gameState, action);
                }
                else if (choice?.ToLower().StartsWith('a') == true)
                {
                    action.ShouldAttack = true;
                }
                
                HandleShieldAndStealthInput(currentZoid, action);
                return action;
            }

            // Normal turn - ask for action order
            Console.WriteLine($"Your current engagement range is {distance} meters. Would you like to move or attack first?");
            Console.Write("Move first (m) or Attack first (a): ");
            string? orderChoice = Console.ReadLine();

            if (orderChoice?.ToLower().StartsWith('m') == true)
            {
                // Move first, then attack
                HandleShieldAndStealthInput(currentZoid, action);
                GetMovementAction(currentZoid, enemyZoid, distance, enemyDetected, gameState, action);
                
                Console.WriteLine($"\n{currentZoid.ZoidName} is in {GetRange(distance)} range of {enemyZoid.ZoidName}.");
                if (currentZoid.CanAttack(distance))
                {
                    Console.Write("  Attack? (y/n): ");
                    if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                    {
                        action.ShouldAttack = true;
                    }
                }
                else
                {
                    Console.WriteLine($"{currentZoid.ZoidName} cannot attack {enemyZoid.ZoidName} from {GetRange(distance)} range!");
                }
            }
            else
            {
                // Attack first, then move
                HandleShieldAndStealthInput(currentZoid, action);
                
                Console.WriteLine($"\n{currentZoid.ZoidName} is in {GetRange(distance)} range of {enemyZoid.ZoidName}.");
                if (currentZoid.CanAttack(distance))
                {
                    Console.Write("  Attack? (y/n): ");
                    if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                    {
                        action.ShouldAttack = true;
                    }
                }
                else
                {
                    Console.WriteLine($"{currentZoid.ZoidName} cannot attack {enemyZoid.ZoidName} from {GetRange(distance)} range!");
                }
                
                GetMovementAction(currentZoid, enemyZoid, distance, enemyDetected, gameState, action);
            }

            return action;
        }

        public override void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        public override void DisplayZoidStatus(Zoid zoid, double distance)
        {
            zoid.PrintStatus(distance);
        }

        public override void DisplayBattleStart(Zoid zoid1, Zoid zoid2)
        {
            Console.WriteLine($"\nPlayer 1: {zoid1.ZoidName} vs Player 2: {zoid2.ZoidName}");
        }

        public override void DisplayTurnStart(Zoid currentZoid, int turnNumber)
        {
            Console.WriteLine($"\n{currentZoid.ZoidName}'s turn!");
        }

        public override void DisplayBattleResult(Zoid winner, Zoid loser)
        {
            Console.WriteLine($"{winner.ZoidName} wins!");
        }

        public override bool AskPlayAgain()
        {
            Console.Write("\nWould you like to fight again? (y/n): ");
            var again = Console.ReadLine();
            return again != null && again.Trim().ToLower().StartsWith('y');
        }

        // Helper methods specific to console implementation
        private CharacterData CreateNewCharacter(string saveFile)
        {
            var playerData = new CharacterData();
            Console.WriteLine("Created new character data");
            playerData.SaveToFile(saveFile);
            return playerData;
        }

        private Zoid ChooseZoidFromList(IEnumerable<ZoidData> zoids, int playerNum, int costLimit = 0, bool shopMode = false)
        {
            var sorted = zoids.OrderBy(z => (z.PowerLevel, z.Name)).ToList();
            sorted = GetAffordableZoids(costLimit, shopMode, sorted);

            Console.WriteLine("\nAvailable Zoids:");
            for (int i = 0; i < sorted.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {sorted[i].Name} (PL {sorted[i].PowerLevel})");
            }

            while (true)
            {
                Console.Write($"\nEnter number for Player {playerNum}: ");
                if (int.TryParse(Console.ReadLine(), out int idx) && idx >= 1 && idx <= sorted.Count)
                {
                    return new Zoid(sorted[idx - 1]);
                }
                Console.WriteLine("Invalid input. Try again.");
            }
        }

        public static List<ZoidData> GetAffordableZoids(int costLimit, bool shopMode, List<ZoidData> sorted)
        {
            if (costLimit > 0)
            {
                sorted = sorted.Where(z => z.Cost <= costLimit).ToList();
                if (shopMode)
                {
                    var shopZoids = sorted.OrderBy(_ => random.Next()).Take(5).ToList();
                    sorted = shopZoids;
                }
            }

            return sorted;
        }

        private void GetMovementAction(Zoid currentZoid, Zoid enemyZoid, double distance, bool enemyDetected, GameState gameState, PlayerAction action)
        {
            int speed = GetSpeed(currentZoid, gameState.BattleType);

            if (!enemyDetected)
            {
                Console.Write("Enemy is concealed! 1: Search for Enemy  2: Stand Still\nChoice: ");
                string? move = Console.ReadLine();
                if (move == "1")
                {
                    action.MovementType = MovementType.Search;
                }
                else
                {
                    action.MovementType = MovementType.StandStill;
                }
                return;
            }

            Console.WriteLine("Choose maneuver:");
            Console.Write("  1: Close\n  2: Retreat\n  3: Circle Left\n  4: Circle Right\n  5: Stand Still\n  Choice: ");
            string? choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    action.MovementType = MovementType.Close;
                    while (true)
                    {
                        Console.Write($"Enter distance to move (0 to {speed}): ");
                        if (double.TryParse(Console.ReadLine(), out double moveDistance) && moveDistance >= 0 && moveDistance <= speed)
                        {
                            action.MoveDistance = moveDistance;
                            break;
                        }
                        Console.WriteLine("Invalid distance. Try again.");
                    }
                    break;
                    
                case "2":
                    action.MovementType = MovementType.Retreat;
                    while (true)
                    {
                        Console.Write($"Enter distance to move (0 to {speed}): ");
                        if (double.TryParse(Console.ReadLine(), out double moveDistance) && moveDistance >= 0 && moveDistance <= speed)
                        {
                            action.MoveDistance = moveDistance;
                            break;
                        }
                        Console.WriteLine("Invalid distance. Try again.");
                    }
                    break;
                    
                case "3":
                case "4":
                    action.MovementType = MovementType.Circle;
                    double maxAngle = MaxCirclingAngle(speed, distance);
                    while (true)
                    {
                        Console.Write($"How many degrees do you want to circle? (0 to {maxAngle:F1}): ");
                        if (double.TryParse(Console.ReadLine(), out double angleChange) && angleChange >= 0 && angleChange <= maxAngle)
                        {
                            action.AngleChange = choice == "3" ? angleChange : -angleChange;
                            break;
                        }
                        Console.WriteLine("Invalid angle. Try again.");
                    }
                    break;
                    
                case "5":
                    action.MovementType = MovementType.StandStill;
                    break;
                    
                default:
                    action.MovementType = MovementType.StandStill;
                    break;
            }
        }

        private void HandleShieldAndStealthInput(Zoid zoid, PlayerAction action)
        {
            if (zoid.HasShield())
            {
                Console.WriteLine($"  Shield is currently {(zoid.ShieldOn ? "ON" : "OFF")}");
                Console.Write("  Toggle shield? (y/n): ");
                if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                    action.ToggleShield = true;
            }
            
            if (zoid.HasStealth())
            {
                Console.WriteLine($"  Stealth is currently {(zoid.StealthOn ? "ON" : "OFF")}");
                Console.Write("  Toggle stealth? (y/n): ");
                if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                    action.ToggleStealth = true;
            }
        }

        private static double MaxCirclingAngle(int speed, double distance)
        {
            if (distance <= 0.1) return 360;
            return Math.Min(360, (speed * 180.0) / (Math.PI * distance));
        }
    }
}
