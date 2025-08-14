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

        [JsonPropertyName("Faction")]
        public string Faction { get; set; } = "Civilian";
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

        public static List<ZoidData> LoadZoidsFromCsv(string path)
        {
            var zoids = new List<ZoidData>();
            var lines = File.ReadAllLines(path);
            
            if (lines.Length == 0) return zoids;
            
            // Parse header to find column indices
            var header = lines[0].Split(',');
            var columnMap = new Dictionary<string, int>();
            
            for (int i = 0; i < header.Length; i++)
            {
                columnMap[header[i].Trim().ToLower()] = i;
            }
            
            // Process data rows
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length < header.Length) continue;
                    
                    var zoid = new ZoidData
                    {
                        Name = GetCsvValue(fields, columnMap, "name", ""),
                        PowerLevel = int.Parse(GetCsvValue(fields, columnMap, "power level", "0")),
                        Cost = double.Parse(GetCsvValue(fields, columnMap, "cost", "0")),
                        Faction = GetCsvValue(fields, columnMap, "faction", "Civilian")
                    };
                    
                    // Validate faction value
                    if (!IsValidFaction(zoid.Faction))
                    {
                        zoid.Faction = "Civilian";
                    }
                    
                    // Parse Stats if available
                    zoid.Stats = new Stats
                    {
                        Fighting = int.Parse(GetCsvValue(fields, columnMap, "fighting", "0")),
                        Strength = int.Parse(GetCsvValue(fields, columnMap, "strength", "0")),
                        Dexterity = int.Parse(GetCsvValue(fields, columnMap, "dexterity", "0")),
                        Agility = int.Parse(GetCsvValue(fields, columnMap, "agility", "0")),
                        Awareness = int.Parse(GetCsvValue(fields, columnMap, "awareness", "0"))
                    };
                    
                    // Parse Movement if available
                    zoid.Movement = new MovementStats
                    {
                        Land = double.Parse(GetCsvValue(fields, columnMap, "land", "0")),
                        Water = double.Parse(GetCsvValue(fields, columnMap, "water", "0")),
                        Air = double.Parse(GetCsvValue(fields, columnMap, "air", "0"))
                    };
                    
                    // Parse Defenses if available
                    zoid.Defenses = new Defenses
                    {
                        Toughness = int.Parse(GetCsvValue(fields, columnMap, "toughness", "0")),
                        Parry = int.Parse(GetCsvValue(fields, columnMap, "parry", "0")),
                        Dodge = int.Parse(GetCsvValue(fields, columnMap, "dodge", "0"))
                    };
                    
                    zoids.Add(zoid);
                }
                catch (Exception ex)
                {
                    // Skip invalid rows with a simple log
                    Console.WriteLine($"Warning: Could not parse row {i + 1}: {ex.Message}");
                }
            }
            
            return zoids;
        }
        
        private static bool IsValidFaction(string faction)
        {
            var validFactions = new[] { "Republic", "Empire", "Civilian" };
            return validFactions.Contains(faction, StringComparer.OrdinalIgnoreCase);
        }
        
        private static string GetCsvValue(string[] fields, Dictionary<string, int> columnMap, string columnName, string defaultValue)
        {
            if (columnMap.TryGetValue(columnName, out int index) && index < fields.Length)
            {
                return fields[index].Trim().Trim('"');
            }
            return defaultValue;
        }
        
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField); // Add the last field
            return result.ToArray();
        }
    }
}
