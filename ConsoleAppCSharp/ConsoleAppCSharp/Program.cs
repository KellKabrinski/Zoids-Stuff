using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace ZoidsBattle
{
    public enum AIPersonality {
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

    public class Zoid
    {
        // Basic Info
        public string Name { get; private set; }

        // Stats
        public int Fighting { get; private set; }
        public int Strength { get; private set; }
        public int Dexterity { get; private set; }
        public int Agility { get; private set; }
        public int Awareness { get; private set; }

        // Defenses
        public int Toughness { get; private set; }
        public int Parry { get; private set; }
        public int Dodge { get; private set; }

        // Movement
        public int Land { get; private set; }
        public int Water { get; private set; }
        public int Air { get; private set; }

        // Powers
        public List<Power> Powers { get; private set; }
        public int Melee { get; private set; }
        public int CloseRange { get; private set; }
        public int MidRange { get; private set; }
        public int LongRange { get; private set; }
        public int ShieldRank { get; private set; }
        public bool ShieldDisabled { get; set; } = false;
        public int StealthRank { get; private set; }

        public int CloseCombat { get; private set; }
        public int RangedCombat { get; private set; }
        public int Armor { get; private set; }

        // Battle state
        public string Position { get; set; } = "neutral";
        public bool ShieldOn { get; set; } = false;
        public bool StealthOn { get; set; } = false;
        public int Dents { get; set; } = 0;
        public double Angle { get; set; } = 0.0;
        public string Status { get; set; } = "intact";  // intact, dazed, stunned, defeated
        
        public Ranges BestRange { get; set; }
        public Ranges WorstRange { get; set; }
        public int PowerLevel { get; set; }

        public Zoid(ZoidData data)
        {
            Name = data.Name;
            Fighting = data.Stats.Fighting;
            Strength = data.Stats.Strength;
            Dexterity = data.Stats.Dexterity;
            Agility = data.Stats.Agility;
            Awareness = data.Stats.Awareness;
            PowerLevel = data.PowerLevel;

            Toughness = data.Defenses.Toughness;
            Parry = data.Defenses.Parry;
            Dodge = data.Defenses.Dodge;

            Land = (int)data.Movement.Land;
            Water = (int)data.Movement.Water;
            Air = (int)data.Movement.Air;

            Powers = data.Powers;
            foreach (Power power in Powers)
            {
                if (power.Type == "E-Shield" && power.Rank.HasValue)
                {
                    ShieldRank = power.Rank.Value;
                }
                if (power.Type == "Concealment" && power.Rank.HasValue)
                {
                    StealthRank = power.Rank.Value;
                }
                if (power.Type == "Armor" && power.Rank.HasValue)
                {
                    Armor = power.Rank.Value;
                }
                if (power.Type == "Melee" && power.Rank.HasValue)
                {
                    Melee = power.Rank.Value;
                }
                if (power.Type == "Close-Range" && power.Rank.HasValue)
                {
                    CloseRange = power.Rank.Value;
                }
                if (power.Type == "Mid-Range" && power.Rank.HasValue)
                {
                    MidRange = power.Rank.Value;
                }
                if (power.Type == "Long-Range" && power.Rank.HasValue)
                {
                    LongRange = power.Rank.Value;
                }

                var rangeDamages = new Dictionary<Ranges, int>
                {
                    { Ranges.Melee, Melee },
                    { Ranges.Close, CloseRange },
                    { Ranges.Mid, MidRange },
                    { Ranges.Long, LongRange }
                };
                BestRange = rangeDamages.OrderByDescending(kv => kv.Value).First().Key;
                WorstRange = rangeDamages.OrderBy(kv => kv.Value).First().Key;

                if (power.Type == "Close Combat" && power.Rank.HasValue)
                {
                    CloseCombat = power.Rank.Value;
                }
                if (power.Type == "Ranged Combat" && power.Rank.HasValue)
                {
                    RangedCombat = power.Rank.Value;
                }
            }
        }

        public Zoid()
        {
            Name = "Default Zoid";
            Powers = new List<Power>();
        }

        public bool HasShield() => ShieldRank>0 && !ShieldDisabled;
        public bool HasStealth() => StealthRank > 0;
        public int GetSpeed(string battleType) => battleType.ToLower() switch
        {
            "land" => Land,
            "water" => Water,
            "air" => Air,
            _ => 0
        };

        public bool CanAttack(double distance)
        {
            if (Melee>0 && distance == 0) return true;
            if (CloseRange>0 && distance <= 500) return true;
            if (MidRange>0 && distance <= 1000) return true;
            if (LongRange>0 && distance > 1000) return true;
            return false;
        }

        public void PrintStatus(double distance)
        {
            Console.WriteLine($"\n{Name}'s status: Distance={distance}, Shield={(ShieldOn ? "ON" : "OFF")} (Rank={ShieldRank}), Stealth={(StealthOn ? "ON" : "OFF")} (Rank={StealthRank}), Dents={Dents}, Status={Status}");
            Console.WriteLine($"Accuracy: Melee={Fighting  + CloseCombat}, Ranged={Dexterity+ RangedCombat}");
            Console.WriteLine($"Attacks: Melee={Melee}, Close={CloseRange}, Mid={MidRange}, Long={LongRange}");
            Console.WriteLine($"Angle: {Angle}° (0° is facing enemy)");
        }
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
                Console.WriteLine($"{current.Name} attacks {enemy.Name} with a {range} attack!");
                int attackRoll = 0, defenseRoll = 0;
                int damage = range switch
                {
                    Ranges.Melee => current.Melee,
                    Ranges.Close => current.CloseRange,
                    Ranges.Mid => current.MidRange,
                    _ => current.LongRange
                };
                attackRoll = RollD20() + (range == Ranges.Melee ? current.Fighting+current.CloseCombat : current.Dexterity+current.RangedCombat);
                defenseRoll = 10 + (range == Ranges.Melee ? enemy.Parry : enemy.Dodge);

                bool hit = attackRoll >= defenseRoll;
                if (hit)
                {
                    Console.WriteLine($"Attack roll: {attackRoll} vs Defense roll: {defenseRoll}");
                    Console.WriteLine($"{current.Name} hits {enemy.Name} for {damage} damage!");
                    if (enemy.HasShield() && enemy.ShieldOn && IsAttackInShieldArc(current, enemy))
                    {
                        int shieldRoll = RollD20() + enemy.ShieldRank;
                        if (shieldRoll >= damage + 15)
                        {
                            enemy.ShieldDisabled = true;
                            enemy.ShieldOn = false;
                            Console.WriteLine($"{enemy.Name}'s shield is disabled!");
                        }
                    }
                    else
                    {
                        int toughRoll = RollD20() + enemy.Toughness - enemy.Dents;
                        Console.WriteLine($"Enemy toughness roll: {toughRoll} (Toughness: {enemy.Toughness}, Dents: {enemy.Dents})");
                        int diff = damage + 15 - toughRoll;
                        if (diff <= 0)
                            Console.WriteLine($"{enemy.Name} successfully defends against the attack!");
                        else if (diff < 5 || diff < 10)
                        {
                            Console.WriteLine($"{enemy.Name} takes a {(diff < 5 ? "minor" : "moderate")} hit!");
                            enemy.Dents++;
                            Console.WriteLine($"{enemy.Name} receives a DENT! (Total dents: {enemy.Dents})");
                            if (diff >= 5)
                            {
                                Console.WriteLine($"{enemy.Name} is now DAZED! ");
                                enemy.Status = "dazed";
                            }
                        }
                        else if (diff < 15)
                        {
                            Console.WriteLine($"{enemy.Name} takes a heavy hit!");
                            enemy.Dents++;
                            Console.WriteLine($"{enemy.Name} receives a DENT! (Total dents: {enemy.Dents})");
                            Console.WriteLine($"{enemy.Name} is now STUNNED! ");
                            enemy.Status = "stunned";
                        }
                        else
                        {
                            Console.WriteLine($"{enemy.Name} takes a critical hit!");
                            enemy.Dents++;
                            Console.WriteLine($"{enemy.Name} receives a DENT! (Total dents: {enemy.Dents})");
                            Console.WriteLine($"{enemy.Name} is now DEFEATED! ");
                            enemy.Status = "defeated";
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{current.Name} misses the attack on {enemy.Name}!");
                }
                didAttack = true;
            }
        }

        public static void Main()
        {
            var zoids = LoadZoids("ConvertedZoidStats.json");
            var battleType = PickBattleType();
            var filtered = FilterZoids(zoids, battleType);
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
            AIPersonality personality = random.Next(0, 2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;

            var z1 = ChooseZoid(filtered, 1);
            var z2 = new Zoid();
            if (!aiMode)
            {
                z2 = ChooseZoid(filtered, 2);
            }
            else
            {
                // AI chooses a Zoid within one power level of the player
                var aiCandidates = filtered
                    .Where(z => Math.Abs(z.PowerLevel - z1.PowerLevel) <= 1)
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
                        .ThenBy(z => Math.Abs(z.PowerLevel - z1.PowerLevel))
                        .ThenBy(z => z.Name)
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
                        .ThenBy(z => Math.Abs(z.PowerLevel - z1.PowerLevel))
                        .ThenBy(z => z.Name)
                        .First();
                }

                z2 = new Zoid(aiPick);
                Console.WriteLine($"AI ({personality}) selects: {z2.Name} (PL {z2.PowerLevel})");
            }

            Console.WriteLine($"\nPlayer 1: {z1.Name} vs Player 2: {z2.Name}");
            GameLoop(z1, z2, battleType, aiMode, personality);
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

        private static Zoid ChooseZoid(IEnumerable<ZoidData> zoids, int playerNum)
        {
            var sorted = zoids.OrderBy(z => (z.PowerLevel, z.Name)).ToList();
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
            Console.WriteLine($"\n{(first == 1 ? z1.Name : z2.Name)} goes first!\n");
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
            int dc = (target.HasStealth() && target.StealthOn && target.StealthRank>0) ? 5 + target.StealthRank : 0;
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

        private static void Movement(string battleType, double distance, ref bool didMove, ref bool enemyDetected,Zoid zoid, Zoid enemy, out double newDistance)
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
                    enemyDetected = SearchCheck(zoid,enemy);
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
                Console.WriteLine($"{zoid.Name} is searching for {enemy.Name}...");
                enemyDetected = SearchCheck(zoid, enemy);
                if (!enemyDetected)
                {
                    Console.WriteLine($"{zoid.Name} cannot locate {enemy.Name}!");
                    int direction = random.Next(0, 2);
                    if (direction == 0)
                    {
                        moveDistance = Math.Max(0, distance - speed * 0.5);
                        Console.WriteLine($"{zoid.Name} is moving closer to the last known location of {enemy.Name}.");
                    }
                    else
                    {
                        moveDistance = distance + speed * 0.5;
                        Console.WriteLine($"{zoid.Name} is moving away from the last known location of {enemy.Name}.");
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
                    Console.WriteLine($"{zoid.Name} moves closer by {moveDistance} meters.");
                    distance -= moveDistance;
                    didMove = true;
                }
                else if (distance < targetDistance)
                {
                    moveDistance = Math.Min(speed, targetDistance - distance);
                    Console.WriteLine($"{zoid.Name} retreats by {moveDistance} meters.");
                    distance += moveDistance;
                    didMove = true;
                }
                else
                {
                    Console.WriteLine($"{zoid.Name} holds position.");
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
                    Console.WriteLine($"{zoid.Name} moves closer by {moveDistance} meters.");
                    distance -= moveDistance;
                    didMove = true;
                }
                else if (distance < targetDistance)
                {
                    moveDistance = Math.Min(speed, targetDistance - distance);
                    Console.WriteLine($"{zoid.Name} retreats by {moveDistance} meters.");
                    distance += moveDistance;
                    didMove = true;
                }
                else
                {
                    Console.WriteLine($"{zoid.Name} holds position.");
                    didMove = false;
                }
            }
        }

        private static void AIStealthAndShield(Zoid zoid, Zoid enemy, AIPersonality personality, bool enemyDetected, double distance)
        {
            Ranges currentRange = GetRange(distance);
            if (!zoid.StealthOn) zoid.StealthOn = true; // AI always tries to use stealth if available
            Console.WriteLine($"{zoid.Name} activates stealth mode.");
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
                        Console.WriteLine($"{zoid.Name} activates its energy shield.");
                    }
                    else
                    {
                        if (zoid.ShieldOn)
                        {
                            Console.WriteLine($"{zoid.Name} deactivates its energy shield to attack.");
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
                            Console.WriteLine($"{zoid.Name} activates its energy shield.");
                        }
                    }
                    else
                    {
                        if (zoid.ShieldOn)
                        {
                            Console.WriteLine($"{zoid.Name} deactivates its energy shield.");
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
                Console.WriteLine($"{current.Name} cannot attack while shield is on.");
                return;
            }
            if (current.Status == "stunned")
            {
                Console.WriteLine($"{current.Name} is stunned and cannot attack this turn.");
                return;
            }
            if (current.Status == "dazed" && didMove)
            {
                Console.WriteLine($"{current.Name} is dazed and cannot attack after moving.");
                return;
            }
            Ranges range = GetRange(distance);
            HandleAttack(current, enemy, range, enemyDetected);
        }

        private static void Attack(Zoid current, Zoid enemy, double distance, bool enemyDetected, bool didMove)
        {
            if (current.ShieldOn && current.HasShield())
            {
                Console.WriteLine($"{current.Name} cannot attack while shield is on.");
                return;
            }

            if (!(current.Status == "dazed" && didMove))
            {
                Ranges range = GetRange(distance);
                Console.WriteLine($"\n{current.Name} is in {range} range of {enemy.Name}.");
                if (!current.CanAttack(distance))
                {
                    Console.WriteLine($"{current.Name} cannot attack {enemy.Name} from {range} range!");
                    Console.WriteLine($"{current.Name} skips the attack phase.");
                    return;
                }
                Console.Write("  Attack? (y/n): ");
                if (Console.ReadLine()?.ToLower().StartsWith('y') == true)
                {
                    HandleAttack(current, enemy, range, enemyDetected);
                }
            }
        }
        private static void GameLoop(Zoid z1, Zoid z2, string battleType, bool aiMode = false, AIPersonality personality = AIPersonality.Aggressive)
        {

            var zoids = new Dictionary<int, Zoid> { { 1, z1 }, { 2, z2 } };
            
            (int first, int second) = PickFirst(z1, z2);
            int[] order = [first, second];
            
            int turn = 0;
            double distance = GetStartingDistance();
            double movedDistance = 0;

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
                    Console.WriteLine($"\n{enemy.Name} is in stealth mode!");
                    enemyDetected = SearchCheck(current, enemy);
                    if (!enemyDetected)
                        Console.WriteLine($"{current.Name} cannot locate {enemy.Name}!");
                }

                current.PrintStatus(distance);
                Console.WriteLine($"\nCurrent distance between Zoids: {distance:F1} meters");
                Console.WriteLine($"{current.Name}'s turn!");

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
        }
    }
}
