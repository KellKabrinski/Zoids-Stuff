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

        public int CalculateDefenseUpgradeCost(string statName, int currentValue, int targetValue)
        {
            if (targetValue <= currentValue) return 0;
            
            // Toughness cannot be upgraded directly - it equals armor rank
            if (statName.ToLower() == "toughness") 
            {
                return 0; // Cannot upgrade toughness directly
            }
            
            // Each defense point costs 1 power point
            int powerPointsNeeded = targetValue - currentValue;
            return powerPointsNeeded * CREDITS_PER_POWER_POINT;
        }

        public int CalculatePowerUpgradeCost(string powerType, int? currentRank, int targetRank)
        {
            if (targetRank <= (currentRank ?? 0)) return 0;
            
            int rankDifference = targetRank - (currentRank ?? 0);
            int totalCost = 0;
            
            // Calculate base cost and any first-rank bonus costs
            switch (powerType.ToLower())
            {
                case "close-range":
                    // First rank costs extra 5 points, then 2 points per additional rank
                    if (currentRank == null || currentRank == 0)
                    {
                        totalCost += 5 + 2; // First rank: base cost + 5 extra + 2 for the rank
                        rankDifference--; // Already handled first rank
                    }
                    totalCost += rankDifference * 2; // 2 points per rank for remaining ranks
                    break;
                    
                case "mid-range":
                    // First rank costs extra 6 points, then 2 points per additional rank
                    if (currentRank == null || currentRank == 0)
                    {
                        totalCost += 6 + 2; // First rank: base cost + 6 extra + 2 for the rank
                        rankDifference--; // Already handled first rank
                    }
                    totalCost += rankDifference * 2; // 2 points per rank for remaining ranks
                    break;
                    
                case "long-range":
                    // First rank costs extra 7 points, then 2 points per additional rank
                    if (currentRank == null || currentRank == 0)
                    {
                        totalCost += 7 + 2; // First rank: base cost + 7 extra + 2 for the rank
                        rankDifference--; // Already handled first rank
                    }
                    totalCost += rankDifference * 2; // 2 points per rank for remaining ranks
                    break;
                    
                case "concealment":
                    // Half point per rank (round up)
                    totalCost = (int)Math.Ceiling(rankDifference * 0.5);
                    break;
                    
                default:
                    // Standard powers cost 1 point per rank
                    totalCost = rankDifference;
                    break;
            }
            
            return totalCost * CREDITS_PER_POWER_POINT;
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

        private bool IsDefenseStat(string statName)
        {
            var defenseStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Parry", "Dodge", "Toughness"
            };
            return defenseStats.Contains(statName);
        }

        public bool CanUpgradeZoid(Zoid zoid, int additionalPowerPoints)
        {
            // New power cap logic: Accuracy + Highest Damage ≤ 2 × Power Level
            return CheckAccuracyDamageCap(zoid, additionalPowerPoints);
        }

        private bool CheckAccuracyDamageCap(Zoid zoid, int additionalPowerPoints)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            // Check melee: Fighting + Melee damage
            int meleeTotal = zoid.Fighting + GetMeleeWeaponDamage(zoid);
            
            // Check ranged: Dexterity + highest ranged damage
            int rangedTotal = zoid.Dexterity + GetHighestRangedWeaponDamage(zoid);
            
            // Get the higher of the two combinations
            int currentMax = Math.Max(meleeTotal, rangedTotal);
            
            // For upgrades, we need to consider the potential impact
            // This is a conservative estimate - we assume the worst case
            int estimatedIncrease = Math.Min(additionalPowerPoints / 2, 5); // Conservative estimate
            
            int projectedMax = currentMax + estimatedIncrease;
            
            return projectedMax <= powerCap;
        }

        private int GetMeleeWeaponDamage(Zoid zoid)
        {
            int meleeDamage = zoid.Melee;
            
            // Also check powers for melee weapons
            foreach (var power in zoid.Powers)
            {
                if (power.Type == "Melee")
                {
                    meleeDamage = Math.Max(meleeDamage, power.Rank ?? 0);
                }
            }
            
            return meleeDamage;
        }

        private int GetHighestRangedWeaponDamage(Zoid zoid)
        {
            int highestRanged = 0;
            
            // Check ranged weapon stats
            highestRanged = Math.Max(highestRanged, zoid.CloseRange);
            highestRanged = Math.Max(highestRanged, zoid.MidRange);
            highestRanged = Math.Max(highestRanged, zoid.LongRange);
            
            // Also check powers for ranged weapons
            foreach (var power in zoid.Powers)
            {
                if (power.Type == "Close-Range" || power.Type == "Mid-Range" || power.Type == "Long-Range")
                {
                    highestRanged = Math.Max(highestRanged, power.Rank ?? 0);
                }
            }
            
            return highestRanged;
        }

        private int GetHighestWeaponDamage(Zoid zoid)
        {
            int highestDamage = 0;
            
            // Check all weapon ranks
            highestDamage = Math.Max(highestDamage, zoid.Melee);
            highestDamage = Math.Max(highestDamage, zoid.CloseRange);
            highestDamage = Math.Max(highestDamage, zoid.MidRange);
            highestDamage = Math.Max(highestDamage, zoid.LongRange);
            
            // Also check powers for weapon-type powers
            foreach (var power in zoid.Powers)
            {
                if (power.Type == "Melee" || power.Type == "Close-Range" || 
                    power.Type == "Mid-Range" || power.Type == "Long-Range")
                {
                    highestDamage = Math.Max(highestDamage, power.Rank ?? 0);
                }
            }
            
            return highestDamage;
        }

        private bool ValidateAccuracyUpgrade(Zoid zoid, int newAccuracy)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            // Check both melee and ranged combinations with the new accuracy
            int meleeTotal = newAccuracy + GetMeleeWeaponDamage(zoid);
            int rangedTotal = newAccuracy + GetHighestRangedWeaponDamage(zoid);
            
            // Both combinations must be within the cap
            return meleeTotal <= powerCap && rangedTotal <= powerCap;
        }

        private bool ValidateDexterityUpgrade(Zoid zoid, int newDexterity)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            // Check ranged combination with the new dexterity
            int rangedTotal = newDexterity + GetHighestRangedWeaponDamage(zoid);
            
            return rangedTotal <= powerCap;
        }

        private bool ValidateParryUpgrade(Zoid zoid, int newParry)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            // Check parry + toughness combination
            int defenseTotal = newParry + zoid.Toughness;
            
            return defenseTotal <= powerCap;
        }

        private bool ValidateDodgeUpgrade(Zoid zoid, int newDodge)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            // Check dodge + toughness combination
            int defenseTotal = newDodge + zoid.Toughness;
            
            return defenseTotal <= powerCap;
        }

        private bool ValidateArmorUpgrade(Zoid zoid, int newArmorRank)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            // Since Toughness = Armor rank, check both defense combinations with new toughness
            int newToughness = newArmorRank;
            int parryTotal = zoid.Parry + newToughness;
            int dodgeTotal = zoid.Dodge + newToughness;
            
            // Both combinations must be within the cap
            return parryTotal <= powerCap && dodgeTotal <= powerCap;
        }

        private bool ValidateWeaponUpgrade(Zoid zoid, string powerType, int newDamage)
        {
            int powerCap = 2 * zoid.PowerLevel;
            
            if (powerType == "Melee")
            {
                // For melee weapons, use Fighting as accuracy
                int meleeTotal = zoid.Fighting + newDamage;
                return meleeTotal <= powerCap;
            }
            else if (powerType == "Close-Range" || powerType == "Mid-Range" || powerType == "Long-Range")
            {
                // For ranged weapons, use Dexterity as accuracy
                int rangedTotal = zoid.Dexterity + newDamage;
                return rangedTotal <= powerCap;
            }
            
            return true; // Non-weapon powers don't affect this cap
        }

        private bool IsWeaponPower(string powerType)
        {
            return powerType == "Melee" || powerType == "Close-Range" || 
                   powerType == "Mid-Range" || powerType == "Long-Range";
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
            
            // Determine if this is an attribute or defense and calculate cost accordingly
            int upgradeCost;
            if (IsDefenseStat(statName))
            {
                upgradeCost = CalculateDefenseUpgradeCost(statName, currentValue, newValue);
            }
            else
            {
                upgradeCost = CalculateStatUpgradeCost(currentValue, newValue);
            }

            if (character.Credits < upgradeCost) return false;

            // Check power cap before applying stat upgrade
            if (statName.ToLower() == "fighting")
            {
                // For Fighting upgrades, check melee weapon combinations
                if (!ValidateAccuracyUpgrade(ownedZoid, newValue)) return false;
            }
            else if (statName.ToLower() == "dexterity")
            {
                // For Dexterity upgrades, check ranged weapon combinations
                if (!ValidateDexterityUpgrade(ownedZoid, newValue)) return false;
            }
            else if (statName.ToLower() == "parry")
            {
                // For Parry upgrades, check parry + toughness combination
                if (!ValidateParryUpgrade(ownedZoid, newValue)) return false;
            }
            else if (statName.ToLower() == "dodge")
            {
                // For Dodge upgrades, check dodge + toughness combination
                if (!ValidateDodgeUpgrade(ownedZoid, newValue)) return false;
            }

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
            int currentRank = existingPower?.Rank ?? 0;
                
            int upgradeCost = CalculatePowerUpgradeCost(powerType, currentRank, currentRank + newRank);

            if (character.Credits < upgradeCost) return false;

            // Check power cap for weapon-type powers
            if (IsWeaponPower(powerType))
            {
                int finalRank = currentRank + newRank;
                if (!ValidateWeaponUpgrade(ownedZoid, powerType, finalRank)) return false;
            }
            
            // Check power cap for armor upgrades (affects toughness and defense combinations)
            if (powerType.ToLower() == "armor")
            {
                int finalRank = currentRank + newRank;
                if (!ValidateArmorUpgrade(ownedZoid, finalRank)) return false;
            }

            // Calculate power points used
            int powerPointsUsed = upgradeCost / CREDITS_PER_POWER_POINT;
            if (!CanUpgradeZoid(ownedZoid, powerPointsUsed)) return false;

            // Apply upgrade
            if (existingPower != null)
            {
                existingPower.Rank = (existingPower.Rank ?? 0) + newRank;
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
                "Close Combat",      // Melee accuracy bonus
                "Ranged Combat",     // Ranged accuracy bonus  
                "Melee",            // Melee weapon damage
                "Close-Range",      // Close range weapon damage
                "Mid-Range",        // Mid range weapon damage
                "Long-Range",       // Long range weapon damage
                "E-Shield",         // Shield protection
                "Armor",           // Armor protection
                "Concealment"      // Stealth capability
            };
        }

        public List<string> GetUpgradeableStats()
        {
            return new List<string>
            {
                "Fighting", "Strength", "Dexterity", "Agility", "Awareness",
                "Parry", "Dodge"
                // Note: Toughness is not directly upgradeable - it equals armor rank
            };
        }
    }
}
