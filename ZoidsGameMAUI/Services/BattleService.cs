using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Services
{
    public class BattleService
    {
        private readonly ZoidDataService _zoidDataService;
        
        public BattleService(ZoidDataService zoidDataService)
        {
            _zoidDataService = zoidDataService;
        }

        public async Task<Zoid> CreateRandomEnemyAsync(int? powerLevelRange = null, int? costRange = null)
        {
            var allZoids = await _zoidDataService.LoadZoidDataAsync();
            
            // Filter by power level if specified
            var availableZoids = powerLevelRange.HasValue 
                ? allZoids.Where(z => Math.Abs(z.PowerLevel - powerLevelRange.Value) <= 1).ToList()
                : allZoids;
            
            if (!availableZoids.Any())
            {
                availableZoids = allZoids; // Fallback to all zoids
            }
            var tempAvailableZoids = costRange.HasValue ?
                availableZoids.Where(z => Math.Abs(z.Cost - costRange.Value) <= 5000).ToList() :
                availableZoids;
            if (tempAvailableZoids.Any())
            {
                availableZoids = tempAvailableZoids;
            }
            var random = new Random();
            var selectedZoidData = availableZoids[random.Next(availableZoids.Count)];
            
            var enemy = new Zoid(selectedZoidData);
            
            // Assign random AI personality
            enemy.AIPersonality = random.Next(2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;
            
            return enemy;
        }

        public async Task<Zoid> CreateEnemyByNameAsync(string zoidName)
        {
            var zoidData = await _zoidDataService.GetZoidByNameAsync(zoidName);
            if (zoidData == null)
            {
                throw new ArgumentException($"Zoid '{zoidName}' not found.");
            }
            
            var enemy = new Zoid(zoidData);
            
            // Assign random AI personality
            var random = new Random();
            enemy.AIPersonality = random.Next(2) == 0 ? AIPersonality.Aggressive : AIPersonality.Defensive;
            
            return enemy;
        }

        public BattleScenario CreateBattleScenario(Zoid playerZoid, Zoid enemyZoid, string? terrain = null)
        {
            return new BattleScenario
            {
                PlayerZoid = playerZoid,
                EnemyZoid = enemyZoid,
                Terrain = terrain ?? "Standard",
                InitialDistance = DetermineInitialDistance(playerZoid, enemyZoid),
                BattleType = "Skirmish"
            };
        }

        private double DetermineInitialDistance(Zoid player, Zoid enemy)
        {
            // Start at mid-range for balanced encounters
            // Could be modified based on zoid types or preferences
            return 1000.0;
        }
    }

    public class BattleScenario
    {
        public Zoid PlayerZoid { get; set; } = new();
        public Zoid EnemyZoid { get; set; } = new();
        public string Terrain { get; set; } = "Standard";
        public double InitialDistance { get; set; } = 1000.0;
        public string BattleType { get; set; } = "Skirmish";
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }
}
