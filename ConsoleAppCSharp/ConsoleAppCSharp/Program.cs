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


    // This class contains only data structures and utility methods
    // The actual entry point is now in App.xaml.cs for WPF mode
    public static class ProgramUtilities
    {
        public static List<ZoidData> LoadZoids(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<ZoidData>>(json)!;
        }
    }
}
