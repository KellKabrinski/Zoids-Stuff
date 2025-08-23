using System.Text.Json;
using System.Text.Json.Serialization;
using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Services
{
    public class CharacterData
    {
        public string Name { get; set; } = "Default Character";
        
        [JsonPropertyName("Zoids")]
        public List<Zoid> Zoids { get; set; } = new List<Zoid>();
        
        public int Credits { get; set; } = 40000; // Default starting credits

        public CharacterData() { }

        public CharacterData(CharacterData save)
        {
            Name = save.Name;
            Zoids = save.Zoids;
            Credits = save.Credits;
        }

        public async Task SaveToFileAsync(string fileName)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Reset zoids to base state before saving
            foreach (var zoid in Zoids)
            {
                ResetZoidToBaseState(zoid);
            }

            string jsonString = JsonSerializer.Serialize(this, options);
            
            // Use platform-specific file access
            var documentsPath = FileSystem.AppDataDirectory;
            if (string.IsNullOrEmpty(documentsPath))
            {
                throw new InvalidOperationException("App data directory is not available.");
            }
            // Ensure documentsPath is not null before combining
            var filePath = Path.Combine(documentsPath ?? string.Empty, fileName);
            
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        public static async Task<CharacterData> LoadFromFileAsync(string fileName)
        {
            var documentsPath = FileSystem.AppDataDirectory;
            if (string.IsNullOrEmpty(documentsPath))
            {
                throw new InvalidOperationException("App data directory is not available.");
            }
            var filePath = Path.Combine(documentsPath, fileName);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {fileName} does not exist.");
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var characterData = JsonSerializer.Deserialize<CharacterData>(jsonString, options);
            if (characterData == null)
            {
                throw new InvalidOperationException("Failed to deserialize CharacterData from the file.");
            }

            return characterData;
        }

        private void ResetZoidToBaseState(Zoid zoid)
        {
            zoid.Position = "neutral";
            zoid.ShieldOn = false;
            zoid.StealthOn = false;
            zoid.Dents = 0;
            zoid.Angle = 0.0;
            zoid.Status = "intact";
            zoid.ShieldDisabled = false;
        }
    }

    public class SaveSystem
    {
        private readonly string _saveDirectory;

        public SaveSystem()
        {
            _saveDirectory = FileSystem.AppDataDirectory;
        }

        public Task<List<string>> GetSaveFilesAsync()
        {
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }

            var files = Directory.GetFiles(_saveDirectory, "*.json");
            return Task.FromResult(files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList());
        }

        public async Task<CharacterData> LoadCharacterAsync(string fileName)
        {
            var filePath = Path.Combine(_saveDirectory, $"{fileName}.json");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Save file {fileName} does not exist.");
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var characterData = JsonSerializer.Deserialize<CharacterData>(jsonString, options);
            if (characterData == null)
            {
                throw new InvalidOperationException("Failed to deserialize character data.");
            }

            return characterData;
        }

        public async Task SaveCharacterAsync(CharacterData character, string fileName)
        {
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }

            var filePath = Path.Combine(_saveDirectory, $"{fileName}.json");
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Reset zoids to base state before saving
            foreach (var zoid in character.Zoids)
            {
                ResetZoidToBaseState(zoid);
            }

            string jsonString = JsonSerializer.Serialize(character, options);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        public Task<bool> DeleteSaveAsync(string fileName)
        {
            var filePath = Path.Combine(_saveDirectory, $"{fileName}.json");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }

        private void ResetZoidToBaseState(Zoid zoid)
        {
            zoid.Position = "neutral";
            zoid.ShieldOn = false;
            zoid.StealthOn = false;
            zoid.Dents = 0;
            zoid.Angle = 0.0;
            zoid.Status = "intact";
            zoid.ShieldDisabled = false;
        }
    }
}
