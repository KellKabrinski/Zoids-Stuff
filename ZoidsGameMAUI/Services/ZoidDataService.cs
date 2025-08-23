using System.Text.Json;
using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Services
{
    public class ZoidDataService
    {
        private List<ZoidData>? _zoidData;

        public async Task<List<ZoidData>> LoadZoidDataAsync()
        {
            if (_zoidData != null)
                return _zoidData;

            try
            {
                // Load from app package
                using var stream = await FileSystem.OpenAppPackageFileAsync("ConvertedZoidStats.json");
                using var reader = new StreamReader(stream);
                var jsonString = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _zoidData = JsonSerializer.Deserialize<List<ZoidData>>(jsonString, options) ?? new List<ZoidData>();
                return _zoidData;
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Error loading Zoid data: {ex.Message}");
                return new List<ZoidData>();
            }
        }

        public async Task<ZoidData?> GetZoidByNameAsync(string name)
        {
            var data = await LoadZoidDataAsync();
            return data.FirstOrDefault(z => z.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<ZoidData>> GetAffordableZoidsAsync(int credits)
        {
            var data = await LoadZoidDataAsync();
            return data.Where(z => z.Cost <= credits).OrderBy(z => z.Cost).ToList();
        }

        public async Task<List<ZoidData>> GetZoidsByPowerLevelAsync(int minPower, int maxPower)
        {
            var data = await LoadZoidDataAsync();
            return data.Where(z => z.PowerLevel >= minPower && z.PowerLevel <= maxPower)
                      .OrderBy(z => z.PowerLevel)
                      .ToList();
        }

        public async Task<List<string>> GetAllZoidNamesAsync()
        {
            var data = await LoadZoidDataAsync();
            return data.Select(z => z.Name).OrderBy(name => name).ToList();
        }

        public async Task<ZoidData?> GetZoidDataAsync(string zoidName)
        {
            var data = await LoadZoidDataAsync();
            return data.FirstOrDefault(z => z.Name.Equals(zoidName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
