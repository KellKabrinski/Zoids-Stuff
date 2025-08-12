
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZoidsBattle
{


    public class CharacterData
    {
        public string Name { get; set; }
        [JsonPropertyName("Zoids")]
        public List<Zoid> Zoids { get; set; }
        public int credits { get; set; }

        public CharacterData()
        {
            Name = "Default Character";
            Zoids = new List<Zoid>();
            credits = 40000; // Default starting credits
        }
        public CharacterData(CharacterData save)
        {
            Name = save.Name;
            Zoids = save.Zoids;
            credits = save.credits;
        }

        public void SaveToFile(string fileName)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            foreach (var zoid in Zoids)
            {
                zoid.ReturnToBaseState();
            }

            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(fileName, jsonString);
        }
        public static CharacterData LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"The file {fileName} does not exist.");
            }
            string jsonString = File.ReadAllText(fileName);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true

            };
            var characterData = JsonSerializer.Deserialize<CharacterData>(jsonString, options);
            if (characterData == null)
            {
                throw new InvalidOperationException("Failed to deserialize CharacterData from the file.");
            }
            foreach (var zoid in characterData.Zoids)
            {
                zoid.CalculateBestAndWorstRange();
            }
            return characterData;
        }
    }

    
}