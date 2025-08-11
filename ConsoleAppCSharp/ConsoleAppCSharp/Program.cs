using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace ZoidsBattle
{
    public enum AIPersonality
    {
        Aggressive,
        Defensive
    }
    public enum Ranges
    {
        Melee,
        Close,
        Mid,
        Long
    }
    // Classes to match JSON structure
    public class ZoidData
    {
        [JsonPropertyName("Name")]
        public required string Name { get; set; }

        [JsonPropertyName("Stats")]
        public Stats Stats { get; set; } = new Stats();

        [JsonPropertyName("Defenses")]
        public Defenses Defenses { get; set; } = new Defenses();

        [JsonPropertyName("Movement")]
        public MovementStats Movement { get; set; } = new MovementStats();

        [JsonPropertyName("Powers")]
        public List<Power> Powers { get; set; } = new List<Power>();

        [JsonPropertyName("Power Level")]
        public int PowerLevel { get; set; }

        [JsonPropertyName("Cost")]
        public double Cost { get; set; }
    }

    public class Stats
    {
        public int Fighting { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Agility { get; set; }
        public int Awareness { get; set; }
    }

    public class Defenses
    {
        public int Toughness { get; set; }
        public int Parry { get; set; }
        public int Dodge { get; set; }
    }

    public class MovementStats
    {
        public double Land { get; set; }
        public double Water { get; set; }
        public double Air { get; set; }
    }

    public class Power
    {
        public required string Type { get; set; }
        public int? Damage { get; set; }
        public int? Rank { get; set; }
        [JsonPropertyName("Senses")]
        public List<string> Senses { get; set; } = new List<string>();
    }


    public static class Program
    {
        private static readonly Random random = new Random();

        private static void HandleAttack(Zoid current, Zoid enemy, Ranges range, bool enemyDetected)
        {
            bool didAttack = false;
            if (enemy.StealthOn && !enemyDetected)
            {
                Console.WriteLine("Target is concealed! 50% miss chance.");
                if (random.Next(0, 2) == 0)
                {
                    Console.WriteLine("Your attack misses the target's last known location!");
                    didAttack = true;
                    return;
                }
                else
                {
                    Console.WriteLine("You get lucky and land a hit despite concealment!");
                }
            }
            if (!didAttack)
            {
                Console.WriteLine($"{current.ZoidName} attacks {enemy.ZoidName} with a {range} attack!");
                int attackRoll = 0, defenseRoll = 0;
                int damage = range switch
                {
                    Ranges.Melee => current.Melee,
                    Ranges.Close => current.CloseRange,
                    Ranges.Mid => current.MidRange,
                    _ => current.LongRange
                };
                attackRoll = RollD20() + (range == Ranges.Melee ? current.Fighting + current.CloseCombat : current.Dexterity + current.RangedCombat);
                defenseRoll = 10 + (range == Ranges.Melee ? enemy.Parry : enemy.Dodge);

                bool hit = attackRoll >= defenseRoll;
                if (hit)
                {
                    Console.WriteLine($"Attack roll: {attackRoll} vs Defense roll: {defenseRoll}");
                    Console.WriteLine($"{current.ZoidName} hits {enemy.ZoidName} for {damage} damage!");
                    if (enemy.HasShield() && enemy.ShieldOn && IsAttackInShieldArc(current, enemy))
                    {
                        int shieldRoll = RollD20() + enemy.ShieldRank;
                        if (shieldRoll >= damage + 15)
                        {
                            enemy.ShieldDisabled = true;
                            enemy.ShieldOn = false;
                            Console.WriteLine($"{enemy.ZoidName}'s shield is disabled!");
                        }
                    }
                    else
                    {
                        int toughRoll = RollD20() + enemy.Toughness - enemy.Dents;
                        Console.WriteLine($"Enemy toughness roll: {toughRoll} (Toughness: {enemy.Toughness}, Dents: {enemy.Dents})");
                        int diff = damage + 15 - toughRoll;
                        if (diff <= 0)
                            Console.WriteLine($"{enemy.ZoidName} successfully defends against the attack!");
                        else if (diff < 5 || diff < 10)
                        {
                            Console.WriteLine($"{enemy.ZoidName} takes a {(diff < 5 ? "minor" : "moderate")} hit!");
                            enemy.Dents++;
                            Console.WriteLine($"{enemy.ZoidName} receives a DENT! (Total dents: {enemy.Dents})");
                            if (diff >= 5)
                            {
                                Console.WriteLine($"{enemy.ZoidName} is now DAZED! ");
                                enemy.Status = "dazed";
                            }
                        }
                        else if (diff < 15)
                        {
                            Console.WriteLine($"{enemy.ZoidName} takes a heavy hit!");
                            enemy.Dents++;
                            Console.WriteLine($"{enemy.ZoidName} receives a DENT! (Total dents: {enemy.Dents})");
                            Console.WriteLine($"{enemy.ZoidName} is now STUNNED! ");
                            enemy.Status = "stunned";
                        }
                        else
                        {
                            Console.WriteLine($"{enemy.ZoidName} takes a critical hit!");
                            enemy.Dents++;
                            Console.WriteLine($"{enemy.ZoidName} receives a DENT! (Total dents: {enemy.Dents})");
                            Console.WriteLine($"{enemy.ZoidName} is now DEFEATED! ");
                            enemy.Status = "defeated";
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{current.ZoidName} misses the attack on {enemy.ZoidName}!");
                }
                didAttack = true;
            }
        }

        public static void Main()
        {
            var zoids = LoadZoids("ConvertedZoidStats.json");
            var battleType = PickBattleType();
            var filtered = FilterZoids(zoids, battleType);
            CharacterData playerData = new CharacterData();
            if (!filtered.Any())
            {
                Console.WriteLine("No Zoids available for that environment!");
                return;
            }

            Console.WriteLine("\nChoose opponent type:");
            Console.WriteLine("1: Player vs Player");
            Console.WriteLine("2: Player vs AI");
            int opponentType = 0;
            while (true)
            {
                Console.Write("Enter choice: ");
                var input = Console.ReadLine();
                if (input == "1" || input == "2")
                {
                    opponentType = int.Parse(input);
                    break;
                }
                Console.WriteLine("Invalid input. Try again.");
            }
            bool aiMode = opponentType == 2;
            Zoid z1, z2;
            global::System.Int32 winner;
            playerData = ChooseZoidsAndBattle(battleType, filtered, playerData, aiMode, out z1, out z2, out winner);

            if (winner == 1)
            {
                Console.WriteLine($"{z1.ZoidName} wins!");
                playerData.credits += 5000;
            }
            else
            {
                Console.WriteLine($"{z2.ZoidName} wins!");
            }

            Console.Write("\nWould you like to fight again? (y/n): ");
            var again = Console.ReadLine();
            while (again != null && again.Trim().ToLower().StartsWith('y'))
            {
                battleType = PickBattleType();
                filtered = FilterZoids(zoids, battleType);
                if (!filtered.Any())
                {
                    Console.WriteLine("No Zoids available for that environment!");
                    break;
                }
                playerData = ChooseZoidsAndBattle(battleType, filtered, playerData, aiMode, out z1, out z2, out winner);
                if (winner == 1)
                {
                    Console.WriteLine($"{z1.ZoidName} wins!");
                    playerData.credits += 5000;
                }
                else
                {
                    Console.WriteLine($"{z2.ZoidName} wins!");
                }
                Console.Write("\nWould you like to fight again? (y/n): ");
                again = Console.ReadLine();
            }

            Console.WriteLine("Updating Save...");
            playerData.SaveToFile("save1.json");
        }

        private static CharacterData ChooseZoidsAndBattle(string battleType, IEnumerable<ZoidData> filtered, CharacterData playerData, bool aiMode, out Zoid z1, out Zoid z2, out int winner)
        {
            AIPersonality personality = random.Next(0, 2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;

            z1 = new Zoid();
            z2 = new Zoid();
            playerData = ChooseZoids(filtered, playerData, aiMode, out z1, out z2, personality);

            Console.WriteLine($"\nPlayer 1: {z1.ZoidName} vs Player 2: {z2.ZoidName}");
            winner = GameLoop(z1, z2, battleType, aiMode, personality);
           z1.ReturnToBaseState();
           z2.ReturnToBaseState();
            return playerData;
        }

        private static CharacterData ChooseZoids(IEnumerable<ZoidData> filtered, CharacterData playerData, global::System.Boolean aiMode, out Zoid z1, out Zoid z2, AIPersonality personality)
        {
            if (!aiMode) { z1 = ChooseZoid(filtered, 1); }
            else
            {
                string saveFile = "save1.json";
                if (File.Exists(saveFile))
                {
                    try
                    {
                        playerData = CharacterData.LoadFromFile(saveFile);
                        Console.WriteLine("Loaded character save data");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading character data: {ex.Message}");
                        playerData = NewCharacter(saveFile);
                    }
                }
                else
                {
                    playerData = NewCharacter(saveFile);
                }
                if (playerData.Zoids.Count > 0)
                {
                    Console.WriteLine("\nSelect a Zoid to use or buy a new one:");
                    for (int i = 0; i < playerData.Zoids.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}: {playerData.Zoids[i].ZoidName} (PL {playerData.Zoids[i].PowerLevel})");
                    }
                    Console.WriteLine($"{playerData.Zoids.Count + 1}: Buy a new Zoid");
                    int choice;
                    while (true)
                    {
                        Console.Write("Enter your choice: ");
                        if (int.TryParse(Console.ReadLine(), out choice) && choice >= 1 && choice <= playerData.Zoids.Count + 1)
                            break;
                        Console.WriteLine("Invalid input. Try again.");
                    }
                    if (choice == playerData.Zoids.Count + 1)
                    {
                        z1 = ChooseZoid(filtered, 1, playerData.credits);
                        playerData.Zoids.Add(z1);
                        playerData.credits -= z1.Cost;
                        playerData.SaveToFile(saveFile);
                    }
                    else
                    {
                        z1 = playerData.Zoids[choice - 1];
                    }
                }
                else
                {
                    Console.WriteLine("You have no Zoids. Please buy a new one.");
                    z1 = ChooseZoid(filtered, 1, playerData.credits);
                    playerData.Zoids.Add(z1);
                    playerData.credits -= z1.Cost;
                    playerData.SaveToFile(saveFile);
                }


            }
            z2 = new Zoid();
            if (!aiMode)
            {
                z2 = ChooseZoid(filtered, 2);
            }
            else
            {
                Zoid playerZoid = z1;
                // AI chooses a Zoid within one power level of the player
                var aiCandidates = filtered
                    .Where(z => Math.Abs(z.PowerLevel - playerZoid.PowerLevel) <= 1)
                    .ToList();

                if (!aiCandidates.Any())
                {
                    // fallback: pick any zoid
                    aiCandidates = filtered.ToList();
                }

                ZoidData aiPick;
                if (personality == AIPersonality.Defensive)
                {
                    // Prefer Zoids with shields
                    aiPick = aiCandidates
                        .OrderByDescending(z => z.Powers.Any(p => p.Type == "E-Shield" && p.Rank.HasValue && p.Rank.Value > 0))
                        .ThenBy(z => Math.Abs(z.PowerLevel - playerZoid.PowerLevel))
                        .ThenBy(_ => random.Next())
                        .First();
                }
                else
                {
                    // Prefer Zoids with highest weapon damage
                    aiPick = aiCandidates
                        .OrderByDescending(z =>
                            z.Powers.Where(p =>
                                p.Type == "Melee" || p.Type == "Close-Range" || p.Type == "Mid-Range" || p.Type == "Long-Range")
                            .Select(p => p.Rank ?? 0).DefaultIfEmpty(0).Max())
                        .ThenBy(z => Math.Abs(z.PowerLevel - playerZoid.PowerLevel))
                        .ThenBy(_ => random.Next())
                        .First();
                }

                z2 = new Zoid(aiPick);
                Console.WriteLine($"AI ({personality}) selects: {z2.ZoidName} (PL {z2.PowerLevel})");
            }

            return playerData;
        }

        private static CharacterData NewCharacter(string saveFile)
        {
            CharacterData playerData = new CharacterData();
            Console.WriteLine("Created new character data");
            playerData.SaveToFile(saveFile);
            return playerData;
        }

        private static List<ZoidData> LoadZoids(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<ZoidData>>(json)!;
        }

        private static string PickBattleType()
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

        private static Zoid ChooseZoid(IEnumerable<ZoidData> zoids, int playerNum, int costLimit=0, bool shopMode = false)
        {
            var sorted = zoids.OrderBy(z => (z.PowerLevel, z.Name)).ToList();
            if (costLimit > 0)
            {
                sorted = sorted.Where(z => z.Cost <= costLimit).ToList();
                if (shopMode)
                {
                    var shopZoids = sorted.OrderBy(_ => random.Next()).Take(5).ToList();
                    sorted = shopZoids;
                }
            }
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

        private static (int, int) PickFirst(Zoid z1, Zoid z2)
        {
            int first = random.Next(1, 3);
            Console.WriteLine($"\n{(first == 1 ? z1.ZoidName : z2.ZoidName)} goes first!\n");
            return first == 1 ? (1, 2) : (2, 1);
        }

        private static double GetStartingDistance()
        {
            while (true)
            {
                Console.Write("\nEnter starting distance between Zoids in meters: ");
                if (double.TryParse(Console.ReadLine(), out double dist) && dist >= 0)
                    return dist;
                Console.WriteLine("Invalid input. Try again.");
            }
        }

        private static Ranges GetRange(double distance)
        {
            if (distance == 0) return Ranges.Melee;
            if (distance <= 500) return Ranges.Close;
            if (distance <= 1000) return Ranges.Mid;
            return Ranges.Long;
        }

        private static bool IsAttackInShieldArc(Zoid attacker, Zoid defender)
        {
            double rel = (attacker.Angle - defender.Angle) % 360;
            if (rel > 180) rel = 360 - rel;
            return Math.Abs(rel) <= 45;
        }

        private static int RollD20() => random.Next(1, 21);

        private static bool SearchCheck(Zoid searcher, Zoid target)
        {
            int roll = RollD20();
            int total = roll + searcher.Awareness;
            int dc = (target.HasStealth() && target.StealthOn && target.StealthRank > 0) ? 5 + target.StealthRank : 0;
            Console.WriteLine($"  Search Check: d20({roll}) + Awareness({searcher.Awareness}) = {total} vs DC {dc}");
            if (total >= dc)
            {
                Console.WriteLine("  Enemy detected!");
                return true;
            }
            Console.WriteLine("  You fail to locate the enemy!");
            return false;
        }

        private static double MaxCirclingAngle(int speed, double distance)
        {
            if (distance <= 0.1) return 360;
            return Math.Min(360, (speed * 180.0) / (Math.PI * distance));
        }

        private static void ShieldAndStealth(Zoid zoid)
        {
            if (zoid.HasShield())
            {
                Console.WriteLine($"  Shield is currently {(zoid.ShieldOn ? "ON" : "OFF")}");
                Console.Write("  Toggle shield? (y/n): ");
                if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                    zoid.ShieldOn = !zoid.ShieldOn;
            }
            if (zoid.HasStealth())
            {
                Console.WriteLine($"  Stealth is currently {(zoid.StealthOn ? "ON" : "OFF")}");
                Console.Write("  Toggle stealth? (y/n): ");
                if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                    zoid.StealthOn = !zoid.StealthOn;
            }
        }

        private static void Movement(string battleType, double distance, ref bool didMove, ref bool enemyDetected, Zoid zoid, Zoid enemy, out double newDistance)
        {
            double moveDistance = 0;
            newDistance = distance;

            int speed = battleType switch
            {
                "land" => zoid.GetSpeed("land"),
                "water" => zoid.GetSpeed("water"),
                "air" => zoid.GetSpeed("air"),
                _ => 0
            };
            if (!enemyDetected)
            {
                Console.Write("Enemy is concealed! 1: Search for Enemy  2: Stand Still\nChoice: ");
                string? move = Console.ReadLine();
                if (move == "1")
                {
                    bool closer = random.Next(0, 2) == 0;
                    if (closer)
                        newDistance = Math.Max(0, distance - speed * 0.5);
                    else
                        distance += speed * 0.5;
                    didMove = true;
                    enemyDetected = SearchCheck(zoid, enemy);
                }
                else
                {
                    enemyDetected = SearchCheck(zoid, enemy);
                    didMove = false;
                }
                return;
            }

            Console.WriteLine("Choose maneuver:");
            Console.Write("  1: Close\n  2: Retreat\n  3: Circle Left\n  4: Circle Right\n  5: Stand Still\n  Choice: ");
            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    while (true)
                    {
                        Console.Write($"Enter distance to move (0 to {speed}): ");
                        if (double.TryParse(Console.ReadLine(), out moveDistance) && moveDistance >= 0 && moveDistance <= speed)
                            break;
                        Console.WriteLine("Invalid distance. Try again.");
                    }
                    newDistance = Math.Max(0, distance - moveDistance);
                    zoid.Position = "close";
                    didMove = true;
                    break;
                case "2":
                    while (true)
                    {
                        Console.Write($"Enter distance to move (0 to {speed}): ");
                        if (double.TryParse(Console.ReadLine(), out moveDistance) && moveDistance >= 0 && moveDistance <= speed)
                            break;
                        Console.WriteLine("Invalid distance. Try again.");
                    }
                    newDistance += moveDistance;
                    zoid.Position = "retreat";
                    didMove = true;
                    break;
                case "3":
                case "4":
                    {
                        double maxAngle = MaxCirclingAngle(speed, distance);
                        double angleChange;
                        while (true)
                        {
                            Console.Write($"How many degrees do you want to circle? (0 to {maxAngle:F1}): ");
                            if (double.TryParse(Console.ReadLine(), out angleChange) && angleChange >= 0 && angleChange <= maxAngle)
                                break;
                            Console.WriteLine("Invalid angle. Try again.");
                        }
                        if (choice == "3")
                            zoid.Angle = (zoid.Angle + angleChange) % 360;
                        else
                            zoid.Angle = (zoid.Angle - angleChange + 360) % 360;
                        zoid.Position = "circle";
                        didMove = true;
                    }
                    break;
                case "5":
                    zoid.Position = "stand still";
                    didMove = false;
                    break;
                default:
                    didMove = false;
                    break;
            }
        }

        private static void AIMovement(string battleType, Zoid zoid, Zoid enemy, double distance, ref bool enemyDetected, AIPersonality personality, out bool didMove)
        {
            double moveDistance = 0;
            int speed = battleType switch
            {
                "land" => zoid.GetSpeed("land"),
                "water" => zoid.GetSpeed("water"),
                "air" => zoid.GetSpeed("air"),
                _ => 0
            };
            Ranges currentRange = GetRange(distance);
            if (enemy.StealthOn && !enemyDetected)
            {
                Console.WriteLine($"{zoid.ZoidName} is searching for {enemy.ZoidName}...");
                enemyDetected = SearchCheck(zoid, enemy);
                if (!enemyDetected)
                {
                    Console.WriteLine($"{zoid.ZoidName} cannot locate {enemy.ZoidName}!");
                    int direction = random.Next(0, 2);
                    if (direction == 0)
                    {
                        moveDistance = Math.Max(0, distance - speed * 0.5);
                        Console.WriteLine($"{zoid.ZoidName} is moving closer to the last known location of {enemy.ZoidName}.");
                    }
                    else
                    {
                        moveDistance = distance + speed * 0.5;
                        Console.WriteLine($"{zoid.ZoidName} is moving away from the last known location of {enemy.ZoidName}.");
                    }
                    didMove = true;
                    return;
                }
                else { }
            }
            // If enemy is detected, try to move to the optimal range, based on personality
            NormalMove(zoid, enemy, ref distance, personality, ref moveDistance, speed, out didMove);
            return;

        }

        private static void NormalMove(Zoid zoid, Zoid enemy, ref double distance, AIPersonality personality, ref double moveDistance, int speed, out bool didMove)
        {
            didMove = false;
            if (personality == AIPersonality.Aggressive)
            {
                // Agressive AI will try to move to its most powerful range
                double targetDistance = zoid.BestRange switch
                {
                    Ranges.Melee => 0,
                    Ranges.Close => 500,
                    Ranges.Mid => 1000,
                    Ranges.Long => 1500,
                    _ => throw new NotImplementedException()
                };

                if (distance > targetDistance)
                {
                    moveDistance = Math.Min(speed, distance - targetDistance);
                    Console.WriteLine($"{zoid.ZoidName} moves closer by {moveDistance} meters.");
                    distance -= moveDistance;
                    didMove = true;
                }
                else if (distance < targetDistance)
                {
                    moveDistance = Math.Min(speed, targetDistance - distance);
                    Console.WriteLine($"{zoid.ZoidName} retreats by {moveDistance} meters.");
                    distance += moveDistance;
                    didMove = true;
                }
                else
                {
                    Console.WriteLine($"{zoid.ZoidName} holds position.");
                    didMove = false;
                }
            }
            else if (personality == AIPersonality.Defensive)
            {
                double targetDistance = enemy.WorstRange switch
                {
                    Ranges.Melee => 0,
                    Ranges.Close => 500,
                    Ranges.Mid => 1000,
                    Ranges.Long => 1500,
                    _ => throw new NotImplementedException()
                };

                // Defensive AI will try to move to enemy's worst range that it can attack from
                if (!zoid.CanAttack(targetDistance))
                {
                    // Find the closest range that zoid can attack from among enemy's worst and further
                    var possibleRanges = new[] { Ranges.Melee, Ranges.Close, Ranges.Mid, Ranges.Long }
                        .Where(r => zoid.CanAttack(r switch
                        {
                            Ranges.Melee => 0,
                            Ranges.Close => 500,
                            Ranges.Mid => 1000,
                            Ranges.Long => 1500,
                            _ => 0
                        }))
                        .OrderBy(r => Math.Abs((r switch
                        {
                            Ranges.Melee => 0,
                            Ranges.Close => 500,
                            Ranges.Mid => 1000,
                            Ranges.Long => 1500,
                            _ => 0
                        }) - targetDistance))
                        .FirstOrDefault();

                    targetDistance = possibleRanges switch
                    {
                        Ranges.Melee => 0,
                        Ranges.Close => 500,
                        Ranges.Mid => 1000,
                        Ranges.Long => 1500,
                        _ => targetDistance
                    };
                }

                if (distance > targetDistance)
                {
                    moveDistance = Math.Min(speed, distance - targetDistance);
                    Console.WriteLine($"{zoid.ZoidName} moves closer by {moveDistance} meters.");
                    distance -= moveDistance;
                    didMove = true;
                }
                else if (distance < targetDistance)
                {
                    moveDistance = Math.Min(speed, targetDistance - distance);
                    Console.WriteLine($"{zoid.ZoidName} retreats by {moveDistance} meters.");
                    distance += moveDistance;
                    didMove = true;
                }
                else
                {
                    Console.WriteLine($"{zoid.ZoidName} holds position.");
                    didMove = false;
                }
            }
        }

        private static void AIStealthAndShield(Zoid zoid, Zoid enemy, AIPersonality personality, bool enemyDetected, double distance)
        {
            Ranges currentRange = GetRange(distance);
            if (!zoid.StealthOn) zoid.StealthOn = true; // AI always tries to use stealth if available
            Console.WriteLine($"{zoid.ZoidName} activates stealth mode.");
            if (zoid.HasShield())
            {
                if (personality == AIPersonality.Defensive)
                {
                    Ranges range = GetRange(distance);
                    int myDamage = range switch
                    {
                        Ranges.Melee => zoid.Melee,
                        Ranges.Close => zoid.CloseRange,
                        Ranges.Mid => zoid.MidRange,
                        _ => zoid.LongRange
                    };
                    int enemyDamage = range switch
                    {
                        Ranges.Melee => enemy.Melee,
                        Ranges.Close => enemy.CloseRange,
                        Ranges.Mid => enemy.MidRange,
                        _ => enemy.LongRange
                    };
                    if (myDamage <= enemyDamage)
                    {
                        zoid.ShieldOn = true;
                        Console.WriteLine($"{zoid.ZoidName} activates its energy shield.");
                    }
                    else
                    {
                        if (zoid.ShieldOn)
                        {
                            Console.WriteLine($"{zoid.ZoidName} deactivates its energy shield to attack.");
                            zoid.ShieldOn = false;
                        }
                    }
                }
                else
                {
                    // Aggressive AI only activates shield if it can't attack from current range
                    Ranges range = GetRange(distance);
                    if (!zoid.CanAttack(distance))
                    {
                        if (!zoid.ShieldOn)
                        {
                            zoid.ShieldOn = true;
                            Console.WriteLine($"{zoid.ZoidName} activates its energy shield.");
                        }
                    }
                    else
                    {
                        if (zoid.ShieldOn)
                        {
                            Console.WriteLine($"{zoid.ZoidName} deactivates its energy shield.");
                            zoid.ShieldOn = false;
                        }
                    }
                }
            }
        }
        private static void AIAttack(Zoid current, Zoid enemy, double distance, bool enemyDetected, bool didMove)
        {
            // AI will always attack if it can, unless it is stunned or dazed and moved
            if (current.ShieldOn)
            {
                Console.WriteLine($"{current.ZoidName} cannot attack while shield is on.");
                return;
            }
            if (current.Status == "stunned")
            {
                Console.WriteLine($"{current.ZoidName} is stunned and cannot attack this turn.");
                return;
            }
            if (current.Status == "dazed" && didMove)
            {
                Console.WriteLine($"{current.ZoidName} is dazed and cannot attack after moving.");
                return;
            }
            Ranges range = GetRange(distance);
            HandleAttack(current, enemy, range, enemyDetected);
        }

        private static void Attack(Zoid current, Zoid enemy, double distance, bool enemyDetected, bool didMove)
        {
            if (current.ShieldOn && current.HasShield())
            {
                Console.WriteLine($"{current.ZoidName} cannot attack while shield is on.");
                return;
            }

            if (!(current.Status == "dazed" && didMove))
            {
                Ranges range = GetRange(distance);
                Console.WriteLine($"\n{current.ZoidName} is in {range} range of {enemy.ZoidName}.");
                if (!current.CanAttack(distance))
                {
                    Console.WriteLine($"{current.ZoidName} cannot attack {enemy.ZoidName} from {range} range!");
                    Console.WriteLine($"{current.ZoidName} skips the attack phase.");
                    return;
                }
                Console.Write("  Attack? (y/n): ");
                if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                {
                    HandleAttack(current, enemy, range, enemyDetected);
                }
            }
        }
        private static int GameLoop(Zoid z1, Zoid z2, string battleType, bool aiMode = false, AIPersonality personality = AIPersonality.Aggressive)
        {

            var zoids = new Dictionary<int, Zoid> { { 1, z1 }, { 2, z2 } };

            (int first, int second) = PickFirst(z1, z2);
            int[] order = [first, second];

            int turn = 0;
            double distance = GetStartingDistance();
            double movedDistance = 0;
            // Initialize Zoid statuses
            z1.Status="intact";
            z2.Status="intact";

            while (z1.Status != "defeated" && z2.Status != "defeated")
            {
                int player = order[turn % 2];
                var current = zoids[player];
                var enemy = zoids[player == 1 ? 2 : 1];

                bool enemyDetected = true;
                bool didMove = false;

                if (aiMode && player == 2)
                {
                    // AI will always try to move first
                    enemyDetected = SearchCheck(current, enemy);
                    AIMovement(battleType, current, enemy, distance, ref enemyDetected, personality, out didMove);
                    AIStealthAndShield(current, enemy, personality, enemyDetected, distance);
                    AIAttack(current, enemy, distance, enemyDetected, didMove);

                    if (current.Status == "stunned") current.Status = "dazed";
                    else if (current.Status == "dazed") current.Status = "intact";
                    turn++;
                    continue;
                }

                if (enemy.StealthOn)
                {
                    Console.WriteLine($"\n{enemy.ZoidName} is in stealth mode!");
                    enemyDetected = SearchCheck(current, enemy);
                    if (!enemyDetected)
                        Console.WriteLine($"{current.ZoidName} cannot locate {enemy.ZoidName}!");
                }

                current.PrintStatus(distance);
                Console.WriteLine($"\nCurrent distance between Zoids: {distance:F1} meters");
                Console.WriteLine($"{current.ZoidName}'s turn!");

                string priorStatus = current.Status;
                if (current.Status == "stunned")
                {
                    Console.WriteLine("You are STUNNED! You cannot move or attack this turn.");
                    ShieldAndStealth(current);
                    current.Status = "dazed";
                    turn++;
                    continue;
                }
                if (current.Status == "dazed")
                {
                    Console.WriteLine("You are DAZED! You may move OR attack, not both.");
                    Console.Write("  Move (m) or Attack (a) or Skip (s)? ");
                    string? choice = Console.ReadLine();
                    if (choice?.ToLower().StartsWith('m') == true)
                        Movement(battleType, distance, ref didMove, ref enemyDetected, current, enemy, out movedDistance);
                    else if (choice?.ToLower().StartsWith('a') == true)
                        Attack(current, enemy, distance, enemyDetected, false);
                    ShieldAndStealth(current);
                }
                else
                {
                    ShieldAndStealth(current);
                    Console.WriteLine($"Your current engagement range is {distance} meters. Would you like to move or attack first?");
                    string? choice = Console.ReadLine();
                    if (choice?.ToLower().StartsWith('m') == true)
                    {
                        Movement(battleType, distance, ref didMove, ref enemyDetected, current, enemy, out movedDistance);
                        distance = movedDistance;
                        Attack(current, enemy, distance, enemyDetected, didMove);
                    }
                    else
                    {
                        Attack(current, enemy, distance, enemyDetected, didMove);
                        Movement(battleType, distance, ref didMove, ref enemyDetected, current, enemy, out movedDistance);
                        distance = movedDistance;
                    }
                }

                //Status Cleanup and advance turn
                if (priorStatus == "stunned") current.Status = "dazed";
                else if (priorStatus == "dazed") current.Status = "intact";
                turn++;
            }
            if (z1.Status == "defeated")
            {
                return 2;
            }
            return 1;
        }
    }
}
