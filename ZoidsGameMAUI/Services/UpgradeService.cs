using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Services
{
    public class UpgradeService
    {
        private readonly SaveSystem _saveSystem;
        
        // Standard M&M power point costs
        public const int CREDITS_PER_POWER_POINT = 1000;
        
        // Power level caps for different Zoid classes (based on their base power level)
        private readonly Dictionary<int, int> _powerLevelCaps = new()
        {
            { 1, 5 },    // Rookie Zoids can go up to PL 5
            { 2, 5 },
            { 3, 5 },
            { 4, 5 },
            { 5, 5 },
            { 6, 10 },   // Veteran Zoids can go up to PL 10
            { 7, 10 },
            { 8, 10 },
            { 9, 10 },
            { 10, 10 },
            { 11, 15 },  // Elite Zoids can go up to PL 15
            { 12, 15 },
            { 13, 15 },
            { 14, 15 },
            { 15, 15 },
            { 16, 20 },  // Legendary Zoids can go up to PL 20
            { 17, 20 },
            { 18, 20 },
            { 19, 20 },
            { 20, 20 }
        };

        public UpgradeService(SaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
        }

        public int GetPowerLevelCap(int basePowerLevel)
        {
            return _powerLevelCaps.GetValueOrDefault(basePowerLevel, basePowerLevel + 4);
        }

        public int CalculateStatUpgradeCost(int currentValue, int targetValue)
        {
            if (targetValue <= currentValue) return 0;
            
            // Each stat point costs 2 power points
            int powerPointsNeeded = (targetValue - currentValue) * 2;
            return powerPointsNeeded * CREDITS_PER_POWER_POINT;
        }

        public int CalculateDefenseUpgradeCost(int currentValue, int targetValue)
        {
            if (targetValue <= currentValue) return 0;
            
            // Each defense point costs 1 power point
            int powerPointsNeeded = targetValue - currentValue;
            return powerPointsNeeded * CREDITS_PER_POWER_POINT;
        }

        public int CalculatePowerUpgradeCost(int? currentRank, int targetRank,int modifier=1)
        {
            if (targetRank <= (currentRank ?? 0)) return 0;
            
            int rankDifference = targetRank - (currentRank ?? 0);
            return rankDifference * CREDITS_PER_POWER_POINT * modifier;
        }

        public int CalculateMovementUpgradeCost(double currentValue, double targetValue)
        {
            if (targetValue <= currentValue) return 0;
            
            // Movement is typically 1 power point per rank
            // Assuming each 30 units = 1 rank (standard M&M movement)
            int currentRank = (int)(currentValue / 30);
            int targetRank = (int)(targetValue / 30);
            int powerPointsNeeded = targetRank - currentRank;
            
            return Math.Max(0, powerPointsNeeded) * CREDITS_PER_POWER_POINT;
        }

        public bool CanUpgradeZoid(Zoid zoid, int additionalPowerPoints)
        {
            int currentPowerLevel = CalculateCurrentPowerLevel(zoid);
            int maxPowerLevel = GetPowerLevelCap(zoid.PowerLevel);
            
            // Rough estimate: 15 power points = 1 power level
            int projectedPowerLevel = currentPowerLevel + (additionalPowerPoints / 15);
            
            return projectedPowerLevel <= maxPowerLevel;
        }

        public int CalculateCurrentPowerLevel(Zoid zoid)
        {
            // This is a simplified calculation
            // In a full implementation, you'd calculate based on all stats and powers
            return zoid.PowerLevel;
        }

        public async Task<bool> UpgradeZoidStatAsync(CharacterData character, string zoidName, string statName, int newValue)
        {
            var ownedZoid = character.Zoids.FirstOrDefault(z => z.Name == zoidName);
            if (ownedZoid == null) return false;

            int currentValue = GetStatValue(ownedZoid, statName);
            int upgradeCost = CalculateStatUpgradeCost(currentValue, newValue);

            if (character.Credits < upgradeCost) return false;

            // Calculate power points used
            int powerPointsUsed = upgradeCost / CREDITS_PER_POWER_POINT;
            if (!CanUpgradeZoid(ownedZoid, powerPointsUsed)) return false;

            // Apply upgrade
            SetStatValue(ownedZoid, statName, newValue);
            character.Credits -= upgradeCost;
            
            await _saveSystem.SaveCharacterAsync(character, "current_save");
            return true;
        }

        public async Task<bool> UpgradeZoidPowerAsync(CharacterData character, string zoidName, string powerType, int newRank)
        {
            var ownedZoid = character.Zoids.FirstOrDefault(z => z.Name == zoidName);
            if (ownedZoid == null) return false;

            var existingPower = ownedZoid.Powers.FirstOrDefault(p => p.Type == powerType);
            int upgradeCost = CalculatePowerUpgradeCost(existingPower?.Rank, newRank);

            if (character.Credits < upgradeCost) return false;

            // Calculate power points used
            int powerPointsUsed = upgradeCost / CREDITS_PER_POWER_POINT;
            if (!CanUpgradeZoid(ownedZoid, powerPointsUsed)) return false;

            // Apply upgrade
            if (existingPower != null)
            {
                existingPower.Rank += newRank;
            }
            else
            {
                ownedZoid.Powers.Add(new Power
                {
                    Type = powerType,
                    Rank = newRank
                });
            }

            character.Credits -= upgradeCost;
            await _saveSystem.SaveCharacterAsync(character, "current_save");
            return true;
        }

        private int GetStatValue(Zoid zoid, string statName)
        {
            return statName.ToLower() switch
            {
                "fighting" => zoid.Fighting,
                "strength" => zoid.Strength,
                "dexterity" => zoid.Dexterity,
                "agility" => zoid.Agility,
                "awareness" => zoid.Awareness,
                "toughness" => zoid.Toughness,
                "parry" => zoid.Parry,
                "dodge" => zoid.Dodge,
                _ => 0
            };
        }

        private void SetStatValue(Zoid zoid, string statName, int value)
        {
            switch (statName.ToLower())
            {
                case "fighting": zoid.Fighting = value; break;
                case "strength": zoid.Strength = value; break;
                case "dexterity": zoid.Dexterity = value; break;
                case "agility": zoid.Agility = value; break;
                case "awareness": zoid.Awareness = value; break;
                case "toughness": zoid.Toughness = value; break;
                case "parry": zoid.Parry = value; break;
                case "dodge": zoid.Dodge = value; break;
            }
        }

        public List<string> GetAvailablePowerTypes()
        {
            return new List<string>
            {
                "Armor", "Melee", "Close-Range", "Mid-Range", "Long-Range", 
                "E-Shield", "Concealment", "Enhanced Speed", "Enhanced Senses",
                "Flight", "Swimming", "Burrowing", "Leaping", "Wall-Crawling"
            };
        }

        public List<string> GetUpgradeableStats()
        {
            return new List<string>
            {
                "Fighting", "Strength", "Dexterity", "Agility", "Awareness",
                "Toughness", "Parry", "Dodge"
            };
        }
    }
}
