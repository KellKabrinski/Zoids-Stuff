using System.Text.Json.Serialization;

namespace ZoidsGameMAUI.Models
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
        [JsonPropertyName("Fighting")]
        public int Fighting { get; set; }
        
        [JsonPropertyName("Strength")]
        public int Strength { get; set; }
        
        [JsonPropertyName("Dexterity")]
        public int Dexterity { get; set; }
        
        [JsonPropertyName("Agility")]
        public int Agility { get; set; }
        
        [JsonPropertyName("Awareness")]
        public int Awareness { get; set; }
    }

    public class Defenses
    {
        [JsonPropertyName("Toughness")]
        public int Toughness { get; set; }
        
        [JsonPropertyName("Parry")]
        public int Parry { get; set; }
        
        [JsonPropertyName("Dodge")]
        public int Dodge { get; set; }
    }

    public class MovementStats
    {
        [JsonPropertyName("Land")]
        public double Land { get; set; }
        
        [JsonPropertyName("Water")]
        public double Water { get; set; }
        
        [JsonPropertyName("Air")]
        public double Air { get; set; }
    }

    public class Power
    {
        [JsonPropertyName("Type")]
        public required string Type { get; set; }
        
        [JsonPropertyName("Damage")]
        public int? Damage { get; set; }
        
        [JsonPropertyName("Rank")]
        public int? Rank { get; set; }
        
        [JsonPropertyName("Senses")]
        public List<string> Senses { get; set; } = new List<string>();
    }
}
