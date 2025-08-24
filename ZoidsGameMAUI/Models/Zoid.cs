using ZoidsGameMAUI.Models;
using System.Text.Json.Serialization;

namespace ZoidsGameMAUI.Models
{
    public class Zoid
    {
        // Basic Info
        public string Name { get; set; } = string.Empty;

        // Stats
        public int Fighting { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Agility { get; set; }
        public int Awareness { get; set; }

        // Defenses
        public int Toughness { get; set; }
        public int Parry { get; set; }
        public int Dodge { get; set; }

        // Movement
        public int Land { get; set; }
        public int Water { get; set; }
        public int Air { get; set; }

        // Powers
        public List<Power> Powers { get; set; } = new List<Power>();
        public int Melee { get; set; }
        public int CloseRange { get; set; }
        public int MidRange { get; set; }
        public int LongRange { get; set; }
        public int ShieldRank { get; set; }
        public bool ShieldDisabled { get; set; } = false;
        public int StealthRank { get; set; }

        public int CloseCombat { get; set; }
        public int RangedCombat { get; set; }
        public int Armor { get; set; }

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
        public int Cost { get; set; }
        
        // AI Behavior
        public AIPersonality AIPersonality { get; set; } = AIPersonality.Aggressive;

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

            // Create a deep copy of the Powers list to avoid reference issues
            Powers = new List<Power>(data.Powers);
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

                if (power.Type == "Close Combat" && power.Rank.HasValue)
                {
                    CloseCombat = power.Rank.Value;
                }
                if (power.Type == "Ranged Combat" && power.Rank.HasValue)
                {
                    RangedCombat = power.Rank.Value;
                }
            }

            // Calculate best and worst ranges after all powers are processed
            var rangeDamages = new Dictionary<Ranges, int>
            {
                { Ranges.Melee, Melee },
                { Ranges.Close, CloseRange },
                { Ranges.Mid, MidRange },
                { Ranges.Long, LongRange }
            };
            BestRange = rangeDamages.OrderByDescending(kv => kv.Value).First().Key;
            WorstRange = rangeDamages.OrderBy(kv => kv.Value).First().Key;

            Cost = (int)data.Cost;
        }

        public Zoid()
        {
            Name = "Default Zoid";
            Powers = new List<Power>();
        }

        public bool HasShield() => ShieldRank > 0 && !ShieldDisabled;
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
            if (Melee > 0 && distance == 0) return true;
            if (CloseRange > 0 && distance <= 500) return true;
            if (MidRange > 0 && distance <= 1000) return true;
            if (LongRange > 0 && distance > 1000) return true;
            return false;
        }

        public string GetStatusText(double distance)
        {
            return $"{Name} | Distance: {distance:F1}m | Shield: {(ShieldOn ? "ON" : "OFF")} | " +
                   $"Stealth: {(StealthOn ? "ON" : "OFF")} | Dents: {Dents} | Status: {Status}";
        }

        public string GetStatsText()
        {
            return $"Fighting: {Fighting} | Strength: {Strength} | Dexterity: {Dexterity} | " +
                   $"Agility: {Agility} | Awareness: {Awareness}";
        }

        public string GetAttacksText()
        {
            return $"Melee: {(Melee > 0 ? Melee.ToString() : "-")} | " +
                   $"Close: {(CloseRange > 0 ? CloseRange.ToString() : "-")} | " +
                   $"Mid: {(MidRange > 0 ? MidRange.ToString() : "-")} | " +
                   $"Long: {(LongRange > 0 ? LongRange.ToString() : "-")}";
        }

        public void PrintStatus(double distance)
        {
            System.Diagnostics.Debug.WriteLine($"\n{Name}'s status: Distance={distance}, Shield={(ShieldOn ? "ON" : "OFF")} (Rank={ShieldRank}), Stealth={(StealthOn ? "ON" : "OFF")} (Rank={StealthRank}), Dents={Dents}, Status={Status}");
            System.Diagnostics.Debug.WriteLine($"Accuracy: Melee={Fighting + CloseCombat}, Ranged={Dexterity + RangedCombat}");
            System.Diagnostics.Debug.WriteLine($"Attacks: Melee={Melee}, Close={CloseRange}, Mid={MidRange}, Long={LongRange}");
            System.Diagnostics.Debug.WriteLine($"Angle: {Angle}° (0° is facing enemy)");
        }
    }
}
